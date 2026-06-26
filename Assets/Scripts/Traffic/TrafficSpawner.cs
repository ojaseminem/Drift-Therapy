using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lane-aware traffic spawner. Lanes are derived from the road's current
/// half-width (which itself varies between 2/4/8-lane sections). Lanes on the
/// left half carry ONCOMING traffic (facing the player); lanes on the right half
/// carry SAME-DIRECTION traffic the player overtakes. A 2-lane road is one of
/// each. Wider roads add more lanes — and more oncoming cars — each way.
/// </summary>
[DisallowMultipleComponent]
public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RoadSegmentPool road;
    [SerializeField] TrafficPool pool;
    [SerializeField] Transform player;

    [Header("Lanes")]
    [SerializeField] float laneWidth = 3.5f;
    [SerializeField] float edgePadding = 0.6f;

    [Header("Spawning")]
    [SerializeField] int   maxActiveCount = 9;
    [SerializeField] float spawnAheadMin = 60f;
    [SerializeField] float spawnAheadMax = 190f;
    [SerializeField] float recycleBehindDistance = 45f;
    [SerializeField] float minLaneSpacing = 24f;     // min arc gap between cars sharing a lane
    [SerializeField] int   spawnTriesPerFrame = 4;

    [Header("Speeds (m/s)")]
    [SerializeField] Vector2 forwardSpeedMps  = new Vector2(12f, 19f); // same direction (overtaken)
    [SerializeField] Vector2 oncomingSpeedMps = new Vector2(15f, 24f); // toward the player

    readonly List<TrafficAgent> activeAgents = new List<TrafficAgent>(24);

    void Update() => Tick(Time.deltaTime);

    public void Tick(float deltaTime)
    {
        if (!road || !pool || !player)
            return;

        UpdateAgents(deltaTime);
        SpawnToFill();
    }

    void UpdateAgents(float deltaTime)
    {
        for (int i = activeAgents.Count - 1; i >= 0; i--)
        {
            var agent = activeAgents[i];
            if (!agent || !agent.Tick(road, deltaTime))
            {
                ReleaseAt(i);
                continue;
            }

            if (agent.ArcDistance < road.DistanceTravelled - recycleBehindDistance)
                ReleaseAt(i);
        }
    }

    void SpawnToFill()
    {
        // Density scales with how many lanes the road has just ahead.
        float aheadArc = road.DistanceTravelled + spawnAheadMin;
        if (!road.TrySampleAtArcDistance(aheadArc, out var probe))
            return;

        int laneCount = LaneCount(probe.HalfWidth);
        int target = Mathf.Clamp(laneCount + 1, 2, maxActiveCount);

        int tries = spawnTriesPerFrame;
        while (activeAgents.Count < target && tries-- > 0)
        {
            if (!TrySpawnOne())
                break;
        }
    }

    bool TrySpawnOne()
    {
        float arc = road.DistanceTravelled + Random.Range(spawnAheadMin, spawnAheadMax);
        if (!road.TrySampleAtArcDistance(arc, out var sample))
            return false;

        int laneCount = LaneCount(sample.HalfWidth);
        if (laneCount < 2)
            return false;

        int   lane      = Random.Range(0, laneCount);
        float offset    = LaneOffset(lane, laneCount, sample.HalfWidth);
        int   direction = offset < 0f ? -1 : 1;     // left = oncoming, right = same-direction

        if (!LaneClear(offset, direction, arc))
            return false;

        var agent = pool.Acquire();
        if (!agent)
            return false;

        float speed = direction < 0
            ? Random.Range(oncomingSpeedMps.x, oncomingSpeedMps.y)
            : Random.Range(forwardSpeedMps.x, forwardSpeedMps.y);

        agent.Configure(arc, offset, speed, direction);

        Quaternion rot = direction < 0
            ? sample.Rotation * Quaternion.Euler(0f, 180f, 0f)
            : sample.Rotation;
        agent.transform.SetPositionAndRotation(sample.Position + sample.Right * offset, rot);

        activeAgents.Add(agent);
        return true;
    }

    // ── Lane geometry (public for tests) ─────────────────────────────────────
    public int LaneCount(float halfWidth)
    {
        // Lanes follow the road's design width (2-lane≈7m, 4-lane≈14m, 8-lane≈28m).
        // Edge lanes are inset by LaneOffset's clamp so they never hang off the road.
        int lanes = Mathf.RoundToInt((halfWidth * 2f) / Mathf.Max(0.5f, laneWidth));
        if (lanes < 2) lanes = 2;
        if ((lanes & 1) == 1) lanes--;   // even, so each direction gets equal lanes
        return lanes;
    }

    public float LaneOffset(int lane, int laneCount, float halfWidth)
    {
        // Lane block centred on the road: left half negative (oncoming), right positive.
        float usable = Mathf.Max(0f, halfWidth - edgePadding);
        float span   = laneWidth * laneCount;
        float start  = -span * 0.5f;
        float center = start + laneWidth * (lane + 0.5f);
        return Mathf.Clamp(center, -usable, usable);
    }

    bool LaneClear(float offset, int direction, float arc)
    {
        for (int i = 0; i < activeAgents.Count; i++)
        {
            var a = activeAgents[i];
            if (!a || a.Direction != direction)
                continue;
            if (Mathf.Abs(a.LateralOffset - offset) > laneWidth * 0.5f)
                continue;
            if (Mathf.Abs(a.ArcDistance - arc) < minLaneSpacing)
                return false;
        }
        return true;
    }

    void ReleaseAt(int index)
    {
        var agent = activeAgents[index];
        activeAgents.RemoveAt(index);
        pool.Release(agent);
    }
}
