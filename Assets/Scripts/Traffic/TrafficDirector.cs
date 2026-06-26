using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns all road traffic. Replaces the old TrafficSpawner + TrafficPool.
///
/// Responsibilities:
///   • Lanes — derived from the road's current half-width (2/4/8-lane sections).
///     Left half = ONCOMING (faces the player), right half = SAME-DIRECTION.
///   • Pooling — per archetype, weighted random selection, no per-spawn GC.
///   • Spacing — never stacks two cars in one lane closer than a min gap.
///   • Fairness — always keeps at least N forward lanes open in the spawn band so
///     the player is never walled in by an unavoidable row.
///   • Follow-gap — a car closing on a slower car in its lane caps its speed to the
///     leader's, so traffic never drives through itself.
/// </summary>
[DisallowMultipleComponent]
public class TrafficDirector : MonoBehaviour
{
    public readonly struct Lane
    {
        public readonly float Offset;     // signed lateral offset from road centre
        public readonly int   Direction;  // +1 forward, -1 oncoming
        public Lane(float offset, int direction) { Offset = offset; Direction = direction; }
    }

    [Header("References")]
    [SerializeField] RoadSegmentPool road;
    [SerializeField] Transform player;
    [SerializeField] Transform container;                 // parent for pooled instances
    [SerializeField] TrafficVehicleType[] vehicleTypes;

    [Header("Lanes")]
    [SerializeField] float laneWidth = 3.5f;
    [SerializeField] float edgePadding = 0.6f;

    [Header("Spawning")]
    [SerializeField] float spawnAheadMin = 110f;          // beyond the camera view
    [SerializeField] float spawnAheadMax = 200f;
    [SerializeField] float recycleBehind = 70f;
    [SerializeField] float minGapInLane = 24f;            // metres between cars sharing a lane
    [SerializeField] int   maxActive = 16;
    [Range(0f, 1f)]
    [SerializeField] float density = 0.6f;                // fraction of lanes filled on average
    [SerializeField] int   spawnsPerFrame = 2;

    [Header("Behaviour")]
    [SerializeField] bool  followGap = true;
    [SerializeField] float followDistance = 16f;          // start matching leader within this gap

    [Header("Fairness")]
    [Tooltip("Minimum same-direction (forward) lanes kept open within the spawn band so the player always has a path.")]
    [SerializeField] int   minOpenForwardLanes = 1;
    [SerializeField] float fairnessBand = 26f;            // longitudinal band for the open-lane test

    // ── Pools ────────────────────────────────────────────────────────────────
    readonly Dictionary<TrafficVehicleType, Queue<TrafficAgent>> pools =
        new Dictionary<TrafficVehicleType, Queue<TrafficAgent>>();
    readonly List<TrafficAgent> active = new List<TrafficAgent>(32);
    readonly List<Lane> lanes = new List<Lane>(8);

    float totalWeight;

    void Awake()
    {
        if (vehicleTypes != null)
            foreach (var t in vehicleTypes)
                if (t != null) totalWeight += Mathf.Max(0f, t.spawnWeight);
    }

    void Update()
    {
        if (!road || !player || vehicleTypes == null || vehicleTypes.Length == 0)
            return;

        float dt = Time.deltaTime;
        TickAgents(dt);
        SpawnToFill();
    }

    // ── Movement & recycling ───────────────────────────────────────────────────
    void TickAgents(float dt)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var a = active[i];
            if (!a) { active.RemoveAt(i); continue; }

            float? cap = followGap ? LeaderSpeedCap(a) : null;
            if (!a.Advance(road, dt, cap)) { RecycleAt(i); continue; }

            if (a.ArcDistance < road.DistanceTravelled - recycleBehind)
                RecycleAt(i);
        }
    }

    float? LeaderSpeedCap(TrafficAgent a)
    {
        float bestGap = float.MaxValue;
        TrafficAgent leader = null;
        for (int i = 0; i < active.Count; i++)
        {
            var b = active[i];
            if (b == a || !b || b.Direction != a.Direction) continue;
            if (Mathf.Abs(b.LaneOffset - a.LaneOffset) > laneWidth * 0.5f) continue;

            float gap = (b.ArcDistance - a.ArcDistance) * a.Direction; // >0 = ahead in travel dir
            if (gap > 0f && gap < bestGap) { bestGap = gap; leader = b; }
        }

        if (leader != null && bestGap < followDistance + a.Length)
            return leader.Speed;   // match the slower car ahead
        return null;
    }

    // ── Spawning ───────────────────────────────────────────────────────────────
    void SpawnToFill()
    {
        if (!road.TrySampleAtArcDistance(road.DistanceTravelled + spawnAheadMin, out var probe))
            return;

        int laneCount = BuildLanes(probe.HalfWidth);
        int target = Mathf.Clamp(Mathf.RoundToInt(laneCount * density) + 1, 2, maxActive);

        int tries = spawnsPerFrame;
        while (active.Count < target && tries-- > 0)
            if (!TrySpawnOne()) break;
    }

    bool TrySpawnOne()
    {
        float arc = road.DistanceTravelled + Random.Range(spawnAheadMin, spawnAheadMax);
        if (!road.TrySampleAtArcDistance(arc, out var sample))
            return false;

        int laneCount = BuildLanes(sample.HalfWidth);
        if (laneCount < 2) return false;

        var lane = lanes[Random.Range(0, laneCount)];

        if (!LaneClear(lane, arc)) return false;
        if (lane.Direction > 0 && !ForwardPathStaysOpen(lane, arc)) return false;

        var type = PickType();
        if (type == null) return false;

        var agent = Acquire(type);
        if (!agent) return false;

        agent.Spawn(arc, lane.Offset, lane.Direction, type.RandomSpeed());
        if (type.colorVariants != null && type.colorVariants.Length > 0)
            agent.ApplyColor(type.colorVariants[Random.Range(0, type.colorVariants.Length)]);

        Quaternion rot = lane.Direction < 0
            ? sample.Rotation * Quaternion.Euler(0f, 180f, 0f)
            : sample.Rotation;
        agent.transform.SetPositionAndRotation(sample.Position + sample.Right * lane.Offset, rot);

        active.Add(agent);
        return true;
    }

    bool LaneClear(Lane lane, float arc)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var b = active[i];
            if (!b || b.Direction != lane.Direction) continue;
            if (Mathf.Abs(b.LaneOffset - lane.Offset) > laneWidth * 0.5f) continue;
            if (Mathf.Abs(b.ArcDistance - arc) < minGapInLane) return false;
        }
        return true;
    }

    /// <summary>True if placing a forward car here still leaves enough open forward lanes in the band.</summary>
    bool ForwardPathStaysOpen(Lane lane, float arc)
    {
        int forwardLanes = 0;
        for (int i = 0; i < lanes.Count; i++) if (lanes[i].Direction > 0) forwardLanes++;
        if (forwardLanes <= minOpenForwardLanes) return true; // nothing we can do; don't deadlock spawning

        // Count distinct forward lanes already blocked within the band around this arc.
        int blocked = 0;
        for (int li = 0; li < lanes.Count; li++)
        {
            if (lanes[li].Direction <= 0) continue;
            if (Mathf.Abs(lanes[li].Offset - lane.Offset) < 0.01f) continue; // this lane handled below
            if (LaneBlockedInBand(lanes[li], arc)) blocked++;
        }
        // +1 for the lane we're about to occupy.
        return (blocked + 1) <= (forwardLanes - minOpenForwardLanes);
    }

    bool LaneBlockedInBand(Lane lane, float arc)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var b = active[i];
            if (!b || b.Direction != lane.Direction) continue;
            if (Mathf.Abs(b.LaneOffset - lane.Offset) > laneWidth * 0.5f) continue;
            if (Mathf.Abs(b.ArcDistance - arc) < fairnessBand) return true;
        }
        return false;
    }

    // ── Lane geometry (public for tests) ───────────────────────────────────────
    /// <summary>The lanes built by the most recent <see cref="BuildLanes"/> call.</summary>
    public IReadOnlyList<Lane> Lanes => lanes;

    /// <summary>Rebuilds the <see cref="lanes"/> list for a road half-width; returns lane count.</summary>
    public int BuildLanes(float halfWidth)
    {
        lanes.Clear();
        int count = LaneCount(halfWidth);
        float span = laneWidth * count;
        float usable = Mathf.Max(0f, halfWidth - edgePadding);
        for (int i = 0; i < count; i++)
        {
            float center = -span * 0.5f + laneWidth * (i + 0.5f);
            center = Mathf.Clamp(center, -usable, usable);
            lanes.Add(new Lane(center, center < 0f ? -1 : 1));
        }
        return count;
    }

    public int LaneCount(float halfWidth)
    {
        int count = Mathf.RoundToInt((halfWidth * 2f) / Mathf.Max(0.5f, laneWidth));
        if (count < 2) count = 2;
        if ((count & 1) == 1) count--;   // even split per direction
        return count;
    }

    // ── Pool ───────────────────────────────────────────────────────────────────
    TrafficVehicleType PickType()
    {
        if (totalWeight <= 0f) return null;
        float r = Random.value * totalWeight;
        foreach (var t in vehicleTypes)
        {
            if (t == null) continue;
            r -= Mathf.Max(0f, t.spawnWeight);
            if (r <= 0f) return t;
        }
        return vehicleTypes[vehicleTypes.Length - 1];
    }

    TrafficAgent Acquire(TrafficVehicleType type)
    {
        if (!type.prefab) return null;

        if (!pools.TryGetValue(type, out var q))
        {
            q = new Queue<TrafficAgent>();
            pools[type] = q;
        }

        TrafficAgent agent = q.Count > 0 ? q.Dequeue() : CreateInstance(type);
        if (!agent) return null;
        agent.gameObject.SetActive(true);
        return agent;
    }

    TrafficAgent CreateInstance(TrafficVehicleType type)
    {
        var go = Instantiate(type.prefab, container ? container : transform);
        var agent = go.GetComponent<TrafficAgent>();
        if (!agent) agent = go.AddComponent<TrafficAgent>();
        agent.Init(type);
        return agent;
    }

    void RecycleAt(int index)
    {
        var agent = active[index];
        active.RemoveAt(index);
        if (!agent) return;

        agent.gameObject.SetActive(false);
        agent.transform.SetParent(container ? container : transform, false);
        if (agent.Type != null && pools.TryGetValue(agent.Type, out var q))
            q.Enqueue(agent);
    }
}
