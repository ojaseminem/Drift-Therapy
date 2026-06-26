using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Manages environment biome transitions during a run.
///
/// Usage:
///   1. Create BiomeData assets (Assets → Create → Drift Therapy → Biome Data).
///   2. Add them to the Biomes list in order, setting startDistance on each.
///   3. Add RoadBiomeManager to the RoadSystem GO.
///   4. Assign it to RoadSegmentPool.biomeManager.
///
/// At runtime the manager cross-fades materials, fog, sky, and post-processing
/// as the player passes each biome's startDistance threshold.
///
/// Road segments are live-updated via MaterialPropertyBlock — no GC.
/// </summary>
[DisallowMultipleComponent]
public class RoadBiomeManager : MonoBehaviour
{
    [Header("Biomes — ordered by startDistance")]
    [SerializeField] BiomeData[] biomes;

    [Header("Scene references")]
    [SerializeField] Renderer   groundRenderer;   // the infinite floor plane
    [SerializeField] Volume     postProcessVolume; // URP global volume (optional)

    [Header("Transition")]
    [SerializeField] float crossfadeSpeed = 2f;   // lerp speed multiplier

    // ── Runtime state ─────────────────────────────────────────────────────
    int   currentBiomeIndex;
    float blendT;                // 0=fully previous biome, 1=fully current

    BiomeData activeBiome;
    BiomeData previousBiome;

    // Cache all active road segment renderers for live material updates
    readonly List<Renderer> roadRenderers = new List<Renderer>();

    MaterialPropertyBlock propBlock;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        // Make sure exponential distance fog is on so per-biome fogColor/fogDensity
        // actually render — the scene's lighting settings may have fog disabled.
        RenderSettings.fog = true;
        if (RenderSettings.fogMode == FogMode.Linear)
            RenderSettings.fogMode = FogMode.Exponential;

        if (biomes == null || biomes.Length == 0)
        {
            Debug.LogWarning("[BiomeManager] No biomes assigned.");
            enabled = false;
            return;
        }

        // Sort biomes by distance just in case they're out of order
        System.Array.Sort(biomes, (a, b) => a.startDistance.CompareTo(b.startDistance));

        activeBiome   = biomes[0];
        previousBiome = biomes[0];
        blendT        = 1f;

        ApplyBiomeImmediate(activeBiome);
    }

    // Called every frame by RoadSegmentPool
    public void OnDistanceUpdate(float distance)
    {
        if (biomes == null || biomes.Length == 0) return;

        // Check if we should advance to next biome
        int nextIndex = currentBiomeIndex + 1;
        if (nextIndex < biomes.Length && distance >= biomes[nextIndex].startDistance)
        {
            previousBiome      = activeBiome;
            currentBiomeIndex  = nextIndex;
            activeBiome        = biomes[currentBiomeIndex];
            blendT             = 0f;

            Debug.Log($"[BiomeManager] Entering biome: {activeBiome.biomeName} at {distance:F0}m");
        }

        // Advance blend
        if (blendT < 1f)
        {
            float transLen = Mathf.Max(1f, activeBiome.transitionLength);
            float progress = (distance - activeBiome.startDistance) / transLen;
            blendT = Mathf.Clamp01(progress * crossfadeSpeed);
            ApplyBlend(blendT);
        }
    }

    // Called by RoadSegmentPool when a new segment is activated
    public void RegisterSegmentRenderer(Renderer r)
    {
        if (!roadRenderers.Contains(r)) roadRenderers.Add(r);
        ApplyMaterialsToRenderer(r, activeBiome, blendT);
    }

    public void UnregisterSegmentRenderer(Renderer r)
    {
        roadRenderers.Remove(r);
    }

    // ── Biome application ─────────────────────────────────────────────────
    void ApplyBiomeImmediate(BiomeData b)
    {
        if (b == null) return;
        ApplyAtmosphere(b, null, 1f);
        foreach (var r in roadRenderers) ApplyMaterialsToRenderer(r, b, 1f);
        if (groundRenderer && b.groundMaterial)
            groundRenderer.sharedMaterial = b.groundMaterial;
    }

    void ApplyBlend(float t)
    {
        ApplyAtmosphere(activeBiome, previousBiome, t);
        foreach (var r in roadRenderers) ApplyMaterialsToRenderer(r, activeBiome, t);
    }

    void ApplyAtmosphere(BiomeData to, BiomeData from, float t)
    {
        Color fog     = from != null ? Color.Lerp(from.fogColor,         to.fogColor,         t) : to.fogColor;
        float fogD    = from != null ? Mathf.Lerp(from.fogDensity,       to.fogDensity,       t) : to.fogDensity;
        Color ambient = from != null ? Color.Lerp(from.ambientSkyColor,  to.ambientSkyColor,  t) : to.ambientSkyColor;

        RenderSettings.fogColor          = fog;
        RenderSettings.fogDensity        = fogD;
        RenderSettings.ambientSkyColor   = ambient;

        // Skybox swap at midpoint (hard cut, not cross-fade — skyboxes can't lerp trivially)
        if (t > 0.5f && to.skyboxMaterial != null)
            RenderSettings.skybox = to.skyboxMaterial;

        // Post-process volume weight
        if (postProcessVolume != null && to.postProcessProfile != null)
        {
            postProcessVolume.profile = to.postProcessProfile;
            postProcessVolume.weight  = t;
        }
    }

    void ApplyMaterialsToRenderer(Renderer r, BiomeData b, float t)
    {
        if (r == null || b == null) return;

        // Swap shared materials (asphalt=index0, kerb=index1)
        var mats = r.sharedMaterials;
        bool changed = false;

        if (b.asphaltMaterial != null && mats.Length > 0 && mats[0] != b.asphaltMaterial)
        { mats[0] = b.asphaltMaterial; changed = true; }

        if (b.kerbMaterial != null && mats.Length > 1 && mats[1] != b.kerbMaterial)
        { mats[1] = b.kerbMaterial; changed = true; }

        if (changed) r.sharedMaterials = mats;
    }

    // ── Biome accessors (for curve generator multipliers) ─────────────────
    public float CurrentRoadWidthMultiplier  => activeBiome != null ? Mathf.Lerp(previousBiome?.roadWidthMultiplier ?? 1f,  activeBiome.roadWidthMultiplier,  blendT) : 1f;
    public float CurrentCurvatureMultiplier  => activeBiome != null ? Mathf.Lerp(previousBiome?.curvatureMultiplier ?? 1f, activeBiome.curvatureMultiplier, blendT) : 1f;
}
