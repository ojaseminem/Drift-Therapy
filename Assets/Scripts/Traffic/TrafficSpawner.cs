using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RoadSegmentPool road;
    [SerializeField] TrafficPool pool;
    [SerializeField] Transform player;

    [Header("Spawning")]
    [SerializeField] int targetActiveCount = 5;
    [SerializeField] float spawnAheadMin = 45f;
    [SerializeField] float spawnAheadMax = 140f;
    [SerializeField] float recycleBehindDistance = 35f;
    [SerializeField] float minSpawnSpacing = 18f;
    [SerializeField] Vector2 speedRangeMps = new Vector2(14f, 24f);

    [Header("Lane Safety")]
    [SerializeField] float centerSafetyHalfWidth = 1f;
    [SerializeField] float edgePadding = 0.25f;

    readonly List<TrafficAgent> activeAgents = new List<TrafficAgent>(16);
    float nextSpawnArc;

    public float CenterSafetyHalfWidth
    {
        get => centerSafetyHalfWidth;
        set => centerSafetyHalfWidth = Mathf.Max(0f, value);
    }

    public float EdgePadding
    {
        get => edgePadding;
        set => edgePadding = Mathf.Max(0f, value);
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float deltaTime)
    {
        if (!road || !pool || !player)
            return;

        UpdateAgents(deltaTime);
        SpawnAhead();
    }

    public float ChooseLateralOffset(float roadHalfWidth)
    {
        float usableHalfWidth = Mathf.Max(0f, roadHalfWidth - edgePadding);
        if (usableHalfWidth <= 0f)
            return 0f;

        float reservedHalfWidth = Mathf.Min(centerSafetyHalfWidth, usableHalfWidth);
        if (usableHalfWidth <= reservedHalfWidth)
            return Random.Range(-usableHalfWidth, usableHalfWidth);

        float side = Random.value < 0.5f ? -1f : 1f;
        return side * Random.Range(reservedHalfWidth, usableHalfWidth);
    }

    void UpdateAgents(float deltaTime)
    {
        for (int i = activeAgents.Count - 1; i >= 0; i--)
        {
            var agent = activeAgents[i];
            if (!agent.Tick(road, deltaTime))
            {
                ReleaseAt(i);
                continue;
            }

            if (agent.ArcDistance < road.DistanceTravelled - recycleBehindDistance)
                ReleaseAt(i);
        }
    }

    void SpawnAhead()
    {
        float minSpawnArc = road.DistanceTravelled + spawnAheadMin;
        float maxSpawnArc = road.DistanceTravelled + Mathf.Max(spawnAheadMin, spawnAheadMax);

        if (nextSpawnArc < minSpawnArc)
            nextSpawnArc = minSpawnArc;

        while (activeAgents.Count < targetActiveCount && nextSpawnArc <= maxSpawnArc)
        {
            if (!road.TrySampleAtArcDistance(nextSpawnArc, out var sample))
                break;

            var agent = pool.Acquire();
            if (!agent)
                break;

            float offset = ChooseLateralOffset(sample.HalfWidth);
            float speed = Random.Range(speedRangeMps.x, speedRangeMps.y);

            agent.Configure(nextSpawnArc, offset, speed);
            agent.transform.SetPositionAndRotation(sample.Position + sample.Right * offset, sample.Rotation);
            activeAgents.Add(agent);

            nextSpawnArc += Mathf.Max(minSpawnSpacing, agent.BodyLength * 2f);
        }
    }

    void ReleaseAt(int index)
    {
        var agent = activeAgents[index];
        activeAgents.RemoveAt(index);
        pool.Release(agent);
    }
}
