using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Distance-based player↔traffic detection. Replaces the old physics-trigger
/// detectors (whose child-collider trigger callbacks were routed to the car's
/// Rigidbody, making the wide near-miss zone register as a collision). Here a
/// tight player-centric box = a real hit; a wider box (without a hit) = a
/// near-miss, fired once when the car finally clears the zone.
/// </summary>
[DisallowMultipleComponent]
public class TrafficSensor : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] TrafficDirector traffic;
    [SerializeField] HyperDriftCarController car;

    [Header("Hit box (player-centric half extents, m)")]
    [SerializeField] float hitHalfWidth = 1.4f;
    [SerializeField] float hitHalfLength = 3.6f;

    [Header("Near-miss box (half extents, m)")]
    [SerializeField] float nearHalfWidth = 2.7f;
    [SerializeField] float nearHalfLength = 4.4f;

    [Header("Tuning")]
    [SerializeField] float minSpeedKphForNearMiss = 25f;
    [SerializeField] float graceSeconds = 0.8f;

    float graceUntil = -1f;
    readonly HashSet<TrafficAgent> near = new HashSet<TrafficAgent>();
    readonly HashSet<TrafficAgent> hit = new HashSet<TrafficAgent>();
    readonly List<TrafficAgent> leaving = new List<TrafficAgent>(16);

    /// <summary>Fired once when traffic overlaps the hit box. Payload = the traffic GameObject.</summary>
    public event Action<GameObject> Hit;
    /// <summary>Fired once when traffic clears the near box without having hit.</summary>
    public event Action<TrafficAgent> NearMissed;

    public bool InGrace => Time.time < graceUntil;
    public void StartGrace() => graceUntil = Time.time + Mathf.Max(0f, graceSeconds);

    /// <summary>Called by PlayerVehicleSpawner right after spawning the selected vehicle.</summary>
    public void SetPlayer(Transform player, HyperDriftCarController car)
    {
        this.player = player;
        this.car = car;
    }

    void Update()
    {
        if (!player || traffic == null) return;
        var agents = traffic.Active;
        if (agents == null) return;

        Vector3 pos = player.position, fwd = player.forward, right = player.right;

        // 1) hits + near membership for active agents
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (!a || !a.gameObject.activeInHierarchy) continue;

            Vector3 d = a.transform.position - pos;
            float lon = Mathf.Abs(Vector3.Dot(d, fwd));
            float lat = Mathf.Abs(Vector3.Dot(d, right));

            if (lon < hitHalfLength && lat < hitHalfWidth)
            {
                if (hit.Add(a) && !InGrace) Hit?.Invoke(a.gameObject);
            }
            if (lon < nearHalfLength && lat < nearHalfWidth)
                near.Add(a);
        }

        // 2) near-miss for agents that have left the near box (or were recycled)
        leaving.Clear();
        foreach (var a in near)
        {
            bool gone = a == null || !a.gameObject.activeInHierarchy;
            if (!gone)
            {
                Vector3 d = a.transform.position - pos;
                float lon = Mathf.Abs(Vector3.Dot(d, fwd));
                float lat = Mathf.Abs(Vector3.Dot(d, right));
                gone = !(lon < nearHalfLength && lat < nearHalfWidth);
            }
            if (gone) leaving.Add(a);
        }

        for (int i = 0; i < leaving.Count; i++)
        {
            var a = leaving[i];
            near.Remove(a);
            bool wasHit = hit.Remove(a);
            if (!wasHit && a != null && (car == null || car.SpeedKph >= minSpeedKphForNearMiss))
                NearMissed?.Invoke(a);
        }
    }
}
