using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural pooled road manager for endless driving.
/// Uses a disabled template segment and recycles pooled segments ahead of the player.
/// </summary>
[DisallowMultipleComponent]
public class EndlessTrackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject roadTemplate;
    [SerializeField] Transform player;

    [Header("Pool")]
    [SerializeField] int initialSegmentCount = 14;
    [SerializeField] int poolSize = 26;
    [SerializeField] float spawnAheadDistance = 220f;
    [SerializeField] float recycleBehindDistance = 80f;

    [Header("Shape")]
    [SerializeField] float segmentLengthOverride = 0f;
    [SerializeField] float minTurnPerSegment = -4.5f;
    [SerializeField] float maxTurnPerSegment = 4.5f;
    [SerializeField] float turnChangeLerp = 0.22f;
    [SerializeField] float maxTurnOffset = 32f;

    [Header("Random")]
    [SerializeField] bool randomizeSeed = true;
    [SerializeField] int seed = 12345;

    readonly Queue<GameObject> freeSegments = new Queue<GameObject>();
    readonly LinkedList<SegmentState> activeSegments = new LinkedList<SegmentState>();

    float segmentLength;
    float halfSegmentLength;
    float baseYaw;
    float yawOffset;
    float currentTurn;
    Vector3 nextSpawnPoint;

    struct SegmentState
    {
        public GameObject Segment;
        public Vector3 EndPoint;
        public Vector3 Forward;
    }

    void Start()
    {
        ResolveReferences();
        if (roadTemplate == null || player == null)
        {
            enabled = false;
            Debug.LogError("EndlessTrackManager requires RoadTemplate and Player references.");
            return;
        }

        if (randomizeSeed)
        {
            seed = System.Environment.TickCount;
        }
        Random.InitState(seed);

        PrepareTemplateAndLength();
        BuildPool();
        SpawnInitialSegments();
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        while (Vector3.Distance(player.position, nextSpawnPoint) < spawnAheadDistance)
        {
            SpawnNextSegment();
        }

        RecyclePassedSegments();
    }

    void ResolveReferences()
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (roadTemplate == null)
        {
            var found = GameObject.Find("RoadTemplate");
            if (found != null)
            {
                roadTemplate = found;
            }
        }
    }

    void PrepareTemplateAndLength()
    {
        baseYaw = roadTemplate.transform.eulerAngles.y;
        yawOffset = 0f;
        currentTurn = 0f;

        segmentLength = segmentLengthOverride > 0.01f ? segmentLengthOverride : EstimateSegmentLength(roadTemplate);
        segmentLength = Mathf.Max(8f, segmentLength);
        halfSegmentLength = segmentLength * 0.5f;

        Vector3 templateForward = roadTemplate.transform.forward;
        nextSpawnPoint = roadTemplate.transform.position - templateForward * halfSegmentLength;

        roadTemplate.SetActive(false);
    }

    float EstimateSegmentLength(GameObject template)
    {
        var renderers = template.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return 20f;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return Mathf.Max(10f, bounds.size.z);
    }

    void BuildPool()
    {
        int safePoolSize = Mathf.Max(poolSize, initialSegmentCount + 4);
        for (int i = 0; i < safePoolSize; i++)
        {
            var segment = Instantiate(roadTemplate, transform);
            segment.name = $"RoadSegment_{i:00}";
            segment.SetActive(false);
            freeSegments.Enqueue(segment);
        }
    }

    void SpawnInitialSegments()
    {
        int count = Mathf.Max(4, initialSegmentCount);
        for (int i = 0; i < count; i++)
        {
            SpawnNextSegment();
        }
    }

    void SpawnNextSegment()
    {
        if (freeSegments.Count == 0)
        {
            var extra = Instantiate(roadTemplate, transform);
            extra.name = $"RoadSegment_{transform.childCount:00}";
            extra.SetActive(false);
            freeSegments.Enqueue(extra);
        }

        GameObject segment = freeSegments.Dequeue();
        segment.SetActive(true);

        currentTurn = Mathf.Lerp(currentTurn, Random.Range(minTurnPerSegment, maxTurnPerSegment), turnChangeLerp);
        yawOffset = Mathf.Clamp(yawOffset + currentTurn, -maxTurnOffset, maxTurnOffset);

        float worldYaw = baseYaw + yawOffset;
        Quaternion rotation = Quaternion.Euler(0f, worldYaw, 0f);

        Vector3 start = nextSpawnPoint;
        Vector3 end = start + rotation * Vector3.forward * segmentLength;
        Vector3 center = (start + end) * 0.5f;

        segment.transform.SetPositionAndRotation(center, rotation);

        activeSegments.AddLast(new SegmentState
        {
            Segment = segment,
            EndPoint = end,
            Forward = rotation * Vector3.forward
        });

        nextSpawnPoint = end;
    }

    void RecyclePassedSegments()
    {
        while (activeSegments.Count > 0)
        {
            var firstNode = activeSegments.First;
            SegmentState first = firstNode.Value;

            float passedDistance = Vector3.Dot(player.position - first.EndPoint, first.Forward);
            if (passedDistance < recycleBehindDistance)
            {
                break;
            }

            activeSegments.RemoveFirst();
            first.Segment.SetActive(false);
            freeSegments.Enqueue(first.Segment);
        }
    }
}
