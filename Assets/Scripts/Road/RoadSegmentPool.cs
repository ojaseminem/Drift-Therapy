using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spline-based endless road orchestrator. Replaces EndlessTrackManager.
/// Each pooled segment gets a fresh procedural mesh when activated ahead of the player.
/// </summary>
[DisallowMultipleComponent]
public class RoadSegmentPool : MonoBehaviour
{
    public readonly struct RoadSample
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Forward;
        public readonly Vector3 Right;
        public readonly float HalfWidth;
        public readonly float ArcDistance;

        public RoadSample(Vector3 position, Quaternion rotation, float halfWidth, float arcDistance)
        {
            Position = position;
            Rotation = rotation;
            Forward = rotation * Vector3.forward;
            Right = rotation * Vector3.right;
            HalfWidth = halfWidth;
            ArcDistance = arcDistance;
        }
    }

    [Header("References")]
    [SerializeField] Transform  player;
    [SerializeField] Material[] roadMaterials; // [0] asphalt, [1] kerb

    /// <summary>Called by PlayerVehicleSpawner right after spawning the selected vehicle (runs in Awake, before this component's Start()).</summary>
    public void SetPlayer(Transform player) => this.player = player;

    [Header("Pool")]
    [SerializeField] int   poolSize            = 12;
    [SerializeField] float segmentArcLength    = 40f;   // metres per mesh chunk
    [SerializeField] int   samplesPerSegment   = 22;    // extrusion rings per chunk
    [SerializeField] float spawnAheadDistance  = 200f;
    [SerializeField] float recycleDistance     = 80f;   // behind player

    [Header("Curve Generation")]
    [SerializeField] bool  randomizeSeed  = true;
    [SerializeField] int   seed           = 42;
    [SerializeField] RoadCurveGenerator.Settings curveSettings = RoadCurveGenerator.Settings.Default;

    [Header("Difficulty")]
    [Range(0f,1f)] public float DifficultyT;

    // Biome hook — assign a RoadBiomeManager to enable environment transitions
    [Header("Biome")]
    [SerializeField] RoadBiomeManager biomeManager;

    // Spline
    SplineRoadBuilder       spline;
    RoadCurveGenerator      curveGen;
    const float             NodeSpacing = 8f;
    float                   splineBuiltLength;

    // Distance tracking — how far the player has travelled along the road
    float                   distanceTravelled;
    Vector3                 lastPlayerPos;
    public float            DistanceTravelled => distanceTravelled;

    // Pool
    struct ActiveSeg
    {
        public GameObject Go;
        public Vector3    EndPos;   // world pos of last sample
        public float      EndArc;
    }

    Queue<GameObject>      freePool       = new Queue<GameObject>();
    LinkedList<ActiveSeg>  activeSegs     = new LinkedList<ActiveSeg>();
    float                  nextStartArc;

    // ────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (!player)
        {
            Debug.LogError("RoadSegmentPool: Player reference missing.");
            enabled = false;
            return;
        }

        int usedSeed = randomizeSeed ? System.Environment.TickCount : seed;
        spline        = new SplineRoadBuilder();
        curveGen      = new RoadCurveGenerator(curveSettings, usedSeed, player.position);
        lastPlayerPos = player.position;

        SeedInitialSpline();

        // Pre-warm pool
        int needed = Mathf.CeilToInt(spawnAheadDistance / segmentArcLength) + 2;
        int total  = Mathf.Max(poolSize, needed + 2);
        for (int i = 0; i < total; i++)
            freePool.Enqueue(CreateSegment(i));

        // Spawn from behind player all the way to spawnAheadDistance in front
        int totalToSpawn = Mathf.CeilToInt((spawnAheadDistance + segmentArcLength + segmentArcLength) / segmentArcLength);
        for (int i = 0; i < totalToSpawn; i++)
            SpawnNext();
    }

    void Update()
    {
        if (!player) return;
        curveGen.DifficultyT = DifficultyT;

        // Track distance travelled
        float moved = Vector3.Distance(player.position, lastPlayerPos);
        distanceTravelled += moved;
        lastPlayerPos      = player.position;

        // Notify biome manager and apply its multipliers to curve gen
        if (biomeManager)
        {
            biomeManager.OnDistanceUpdate(distanceTravelled);
            curveGen.WidthMultiplier = biomeManager.CurrentRoadWidthMultiplier;
        }

        // Spawn ahead. Grow the spline per-segment so nextStartArc can never
        // outrun the built spline (which would clamp SplinePosAtArc to a fixed
        // end node and spin this loop forever). A guard caps work per frame.
        int guard = 0;
        float distToNext = Vector3.Distance(player.position, SplinePosAtArc(nextStartArc));
        while (distToNext < spawnAheadDistance && guard++ < 64)
        {
            float need = nextStartArc + segmentArcLength + NodeSpacing * 2f;
            while (splineBuiltLength < need) ExtendSpline();

            if (!SpawnNext()) break;
            distToNext = Vector3.Distance(player.position, SplinePosAtArc(nextStartArc));
        }

        RecyclePassed();
    }

    // ── Spline management ───────────────────────────────────────────────
    void SeedInitialSpline()
    {
        // Start the spline one full segment BEHIND the player so there is
        // solid road beneath the car the moment the scene loads.
        float behindDist = segmentArcLength + NodeSpacing;
        Vector3 startPos = player.position - Vector3.forward * behindDist;

        // Lay down straight nodes from behind the player up to the player
        int straightCount = Mathf.CeilToInt(behindDist / NodeSpacing) + 2;
        for (int i = 0; i < straightCount; i++)
        {
            Vector3 nodePos = startPos + Vector3.forward * (i * NodeSpacing);
            spline.AddNode(new SplineRoadBuilder.SplineNode(
                nodePos, Quaternion.identity,
                curveSettings.BaseRoadHalfWidth, 0f, 0f));
            splineBuiltLength += NodeSpacing;
        }

        // Now let the curve generator extend ahead
        int extra = Mathf.CeilToInt(spawnAheadDistance / NodeSpacing) + 6;
        for (int i = 0; i < extra; i++) ExtendSpline();
    }

    void ExtendSpline()
    {
        spline.AddNode(curveGen.NextNode(NodeSpacing));
        splineBuiltLength += NodeSpacing;
    }

    // ── Segment spawn ───────────────────────────────────────────────────
    bool SpawnNext()
    {
        if (freePool.Count == 0)
        {
            Debug.LogWarning("RoadSegmentPool: pool empty — growing.");
            freePool.Enqueue(CreateSegment(poolSize++));
        }

        float startArc = nextStartArc;
        float endArc   = startArc + segmentArcLength;

        var samples = SampleRange(startArc, endArc);
        if (samples == null || samples.Count < 2) return false;

        var go = freePool.Dequeue();
        go.SetActive(true);

        Vector3 origin = samples[0].Position;

        // ① Set transform BEFORE rebuilding mesh so origin is correct
        go.transform.SetPositionAndRotation(origin, Quaternion.identity);

        // ② Rebuild mesh relative to origin
        go.GetComponent<ProceduralRoadMesher>().RebuildMesh(samples, origin);

        // ③ Update collider with new mesh
        var col = go.GetComponent<MeshCollider>();
        if (col) col.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;

        // ④ Notify biome manager so it can apply correct materials
        if (biomeManager)
            biomeManager.RegisterSegmentRenderer(go.GetComponent<Renderer>());

        activeSegs.AddLast(new ActiveSeg
        {
            Go     = go,
            EndPos = samples[samples.Count - 1].Position,
            EndArc = endArc
        });

        nextStartArc = endArc;
        return true;
    }

    // ── Segment recycle ─────────────────────────────────────────────────
    void RecyclePassed()
    {
        while (activeSegs.Count > 0)
        {
            var first = activeSegs.First.Value;

            // Recycle when the segment's END is more than recycleDistance behind player
            Vector3 toEnd    = first.EndPos - player.position;
            float   dotFwd   = Vector3.Dot(toEnd, player.forward);   // negative = behind
            float   distance = toEnd.magnitude;

            bool behindPlayer = dotFwd < 0f;
            bool farEnough    = distance > recycleDistance;

            if (!(behindPlayer && farEnough)) break;

            if (biomeManager)
                biomeManager.UnregisterSegmentRenderer(first.Go.GetComponent<Renderer>());
            first.Go.SetActive(false);
            freePool.Enqueue(first.Go);
            activeSegs.RemoveFirst();
        }
    }

    // ── Spline sampling ─────────────────────────────────────────────────
    List<ProceduralRoadMesher.SamplePoint> SampleRange(float startArc, float endArc)
    {
        var result  = new List<ProceduralRoadMesher.SamplePoint>(samplesPerSegment);
        float step  = (endArc - startArc) / (samplesPerSegment - 1);
        int   nodes = spline.NodeCount;
        if (nodes < 2) return null;

        float tMax = nodes - 1;

        for (int i = 0; i < samplesPerSegment; i++)
        {
            float arc = startArc + i * step;
            // Use arc / NodeSpacing directly — stable regardless of splineBuiltLength
            float t   = Mathf.Clamp(arc / NodeSpacing, 0f, tMax);

            if (!spline.SampleAt(t, out Vector3 pos, out Quaternion rot,
                                    out float width, out float banking)) continue;

            result.Add(new ProceduralRoadMesher.SamplePoint
            {
                Position           = pos,
                Rotation           = rot,
                HalfWidth          = width,
                Banking            = banking,
                DistanceAlongSpline = arc
            });
        }
        return result;
    }

    Vector3 SplinePosAtArc(float arc)
    {
        if (spline.NodeCount < 2) return player.position;
        float t = Mathf.Clamp(arc / NodeSpacing, 0f, spline.NodeCount - 1);
        spline.SampleAt(t, out Vector3 pos, out _, out _, out _);
        return pos;
    }

    // ── Pool creation ───────────────────────────────────────────────────
    public bool TrySampleAtArcDistance(float arcDistance, out RoadSample sample)
    {
        sample = default;

        if (spline == null || spline.NodeCount < 2)
            return false;

        float t = Mathf.Clamp(arcDistance / NodeSpacing, 0f, spline.NodeCount - 1);
        if (!spline.SampleAt(t, out Vector3 pos, out Quaternion rot, out float halfWidth, out _))
            return false;

        sample = new RoadSample(pos, rot, halfWidth, arcDistance);
        return true;
    }

    public bool TrySampleAhead(float aheadDistance, out RoadSample sample)
    {
        return TrySampleAtArcDistance(distanceTravelled + Mathf.Max(0f, aheadDistance), out sample);
    }

    GameObject CreateSegment(int idx)
    {
        var go = new GameObject($"RoadSeg_{idx:00}");
        go.transform.SetParent(transform);
        go.SetActive(false);

        go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = roadMaterials;

        go.AddComponent<ProceduralRoadMesher>();
        go.AddComponent<MeshCollider>();

        return go;
    }
}
