using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns sparse mechanic collectibles ahead on the road and repairs the vehicle
/// when the player drives over one. Pooled, allocation-free per frame.
/// </summary>
[DisallowMultipleComponent]
public class CollectibleSpawner : MonoBehaviour
{
    [SerializeField] RoadSegmentPool road;
    [SerializeField] Transform player;
    [SerializeField] VehicleHealth vehicle;
    [SerializeField] Collectible prefab;

    [Header("Repair")]
    [SerializeField] float repairAmount = 35f;

    [Header("Placement")]
    [SerializeField] float spawnAheadMin = 120f;
    [SerializeField] float spawnAheadMax = 260f;
    [SerializeField] float minSpacing = 180f;
    [SerializeField] float pickupRadius = 2.8f;
    [SerializeField] float recycleBehind = 60f;
    [SerializeField] int   maxActive = 3;

    readonly Queue<Collectible> pool = new Queue<Collectible>();
    readonly List<Collectible> active = new List<Collectible>(8);
    float nextArc;

    void Update()
    {
        if (!road || !player || !prefab) return;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var c = active[i];
            if (!c || !c.Advance(road)) { Recycle(i); continue; }

            if (Vector3.Distance(player.position, c.transform.position) < pickupRadius)
            {
                if (vehicle) vehicle.Repair(repairAmount);
                GameSignals.RaiseCollected();
                Recycle(i);
                continue;
            }
            if (c.ArcDistance < road.DistanceTravelled - recycleBehind) Recycle(i);
        }

        if (active.Count >= maxActive) return;

        if (nextArc < road.DistanceTravelled + spawnAheadMin)
            nextArc = road.DistanceTravelled + spawnAheadMin;
        if (nextArc > road.DistanceTravelled + spawnAheadMax) return;

        if (road.TrySampleAtArcDistance(nextArc, out var s))
        {
            var c = Acquire();
            float off = Random.Range(-1f, 1f) * Mathf.Max(0f, s.HalfWidth - 1f);
            c.Spawn(nextArc, off);
            c.Advance(road);
            active.Add(c);
            nextArc += minSpacing + Random.Range(0f, 120f);
        }
    }

    Collectible Acquire()
    {
        var c = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
        c.gameObject.SetActive(true);
        return c;
    }

    void Recycle(int i)
    {
        var c = active[i];
        active.RemoveAt(i);
        if (!c) return;
        c.gameObject.SetActive(false);
        pool.Enqueue(c);
    }
}
