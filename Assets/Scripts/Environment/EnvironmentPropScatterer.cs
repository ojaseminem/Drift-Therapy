using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scatters biome-specific scenery (buildings, trees, rocks, ...) along the road,
/// driven by the same segment spawn/recycle lifecycle as RoadSegmentPool.
///
/// Depends one-way on RoadBiomeManager (reads ActiveBiome/PreviousBiome/BlendT) —
/// the biome system works standalone without this component, same as today.
///
/// Usage:
///   1. Add this component next to RoadBiomeManager on the RoadSystem GameObject.
///   2. Assign it to RoadSegmentPool.propScatterer and this.biomeManager.
///   3. Populate BiomeData.environmentSet on each biome asset.
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentPropScatterer : MonoBehaviour
{
    [SerializeField] RoadBiomeManager biomeManager;

    // One pool per prefab, shared across all biomes/segments/categories.
    readonly Dictionary<GameObject, ObjectPool> poolsByPrefab = new Dictionary<GameObject, ObjectPool>();

    // Instances spawned per segment, so recycling a segment returns them all to their pools.
    readonly Dictionary<GameObject, List<(GameObject prefab, GameObject instance)>> instancesBySegment
        = new Dictionary<GameObject, List<(GameObject, GameObject)>>();

    public void OnSegmentSpawned(GameObject segmentGo, IReadOnlyList<ProceduralRoadMesher.SamplePoint> samples, Vector3 origin)
    {
        if (samples == null || samples.Count < 2) return;

        BiomeEnvironmentSet envSet = ResolveActiveEnvironmentSet();
        if (envSet == null || envSet.categories == null) return;

        // Deterministic per-segment seed so placement never drifts on reuse.
        int seed = Mathf.RoundToInt(samples[0].DistanceAlongSpline * 100f);
        var rng = new System.Random(seed);

        var spawned = new List<(GameObject, GameObject)>();

        foreach (var category in envSet.categories)
        {
            if (category.prefabVariants == null || category.prefabVariants.Length == 0) continue;
            ScatterCategory(category, samples, rng, segmentGo.transform, spawned);
        }

        if (spawned.Count > 0)
            instancesBySegment[segmentGo] = spawned;
    }

    public void OnSegmentRecycled(GameObject segmentGo)
    {
        if (!instancesBySegment.TryGetValue(segmentGo, out var spawned)) return;

        foreach (var (prefab, instance) in spawned)
        {
            if (poolsByPrefab.TryGetValue(prefab, out var pool))
                pool.Release(instance);
        }

        spawned.Clear();
        instancesBySegment.Remove(segmentGo);
    }

    BiomeEnvironmentSet ResolveActiveEnvironmentSet()
    {
        if (biomeManager == null) return null;

        // Mirrors RoadBiomeManager's own skybox hard-cut rule: swap at the transition midpoint
        // rather than blending two prop sets simultaneously.
        BiomeData source = biomeManager.BlendT > 0.5f ? biomeManager.ActiveBiome : biomeManager.PreviousBiome;
        return source != null ? source.environmentSet : null;
    }

    void ScatterCategory(
        BiomeEnvironmentSet.PropCategory category,
        IReadOnlyList<ProceduralRoadMesher.SamplePoint> samples,
        System.Random rng,
        Transform parent,
        List<(GameObject, GameObject)> spawned)
    {
        float startArc = samples[0].DistanceAlongSpline;
        float endArc   = samples[samples.Count - 1].DistanceAlongSpline;
        float spacing  = Mathf.Max(0.5f, category.minSpacing);

        for (float arc = startArc; arc < endArc; arc += spacing)
        {
            if (rng.NextDouble() > category.density) continue;

            if (!TrySample(samples, arc, out Vector3 pos, out Vector3 right, out float halfWidth, out float localRadius)) continue;

            int sideCount = category.bothSides ? 2 : 1;
            for (int side = 0; side < sideCount; side++)
            {
                float sign   = side == 0 ? 1f : -1f;
                // sideOffsetRange is measured from the road EDGE, not the centerline —
                // add halfWidth so props never land on the drivable surface. On a tight
                // turn, also clamp how far past the edge we reach so props can't fold
                // across onto the road on the inside of the curve.
                float desiredExtra = Mathf.Lerp(category.sideOffsetRange.x, category.sideOffsetRange.y, (float)rng.NextDouble());
                float safeExtra = RoadGeometrySafety.ClampReachBeyondEdge(desiredExtra, halfWidth, localRadius);
                float offset = halfWidth + safeExtra;
                Vector3 worldPos = pos + right * (sign * offset);

                float yRotDeg = Mathf.Lerp(category.yRotationJitterDeg.x, category.yRotationJitterDeg.y, (float)rng.NextDouble());
                Quaternion rot = Quaternion.Euler(0f, yRotDeg, 0f);

                GameObject prefab = category.prefabVariants[rng.Next(category.prefabVariants.Length)];
                if (prefab == null) continue;

                ObjectPool pool = GetOrCreatePool(prefab);
                GameObject instance = pool.Get(worldPos, rot, parent);

                float scale = Mathf.Lerp(category.scaleJitter.x, category.scaleJitter.y, (float)rng.NextDouble());
                instance.transform.localScale = Vector3.one * scale;

                ApplyShadowMode(instance, category.castShadows);

                spawned.Add((prefab, instance));
            }
        }
    }

    static bool TrySample(IReadOnlyList<ProceduralRoadMesher.SamplePoint> samples, float arc, out Vector3 position, out Vector3 right, out float halfWidth, out float localRadius)
    {
        for (int i = 0; i < samples.Count - 1; i++)
        {
            var a = samples[i];
            var b = samples[i + 1];
            if (arc < a.DistanceAlongSpline || arc > b.DistanceAlongSpline) continue;

            float span = b.DistanceAlongSpline - a.DistanceAlongSpline;
            float t    = span > 0f ? (arc - a.DistanceAlongSpline) / span : 0f;

            position    = Vector3.Lerp(a.Position, b.Position, t);
            right       = Quaternion.Slerp(a.Rotation, b.Rotation, t) * Vector3.right;
            halfWidth   = Mathf.Lerp(a.HalfWidth, b.HalfWidth, t);
            localRadius = RoadGeometrySafety.EstimateRadius(a.Rotation, b.Rotation, span);
            return true;
        }

        position    = default;
        right       = default;
        halfWidth   = default;
        localRadius = default;
        return false;
    }

    ObjectPool GetOrCreatePool(GameObject prefab)
    {
        if (!poolsByPrefab.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool(prefab);
            poolsByPrefab[prefab] = pool;
        }
        return pool;
    }

    static void ApplyShadowMode(GameObject instance, bool castShadows)
    {
        ShadowCastingMode mode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            renderer.shadowCastingMode = mode;
    }
}
