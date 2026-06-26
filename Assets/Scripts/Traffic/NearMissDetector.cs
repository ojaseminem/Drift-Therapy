using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects "near miss" events: traffic that passes through a generous trigger
/// volume around the player without an actual collision. Lives on the player
/// car, with a child trigger collider (larger than the car body) feeding the
/// OnTrigger callbacks.
///
/// Each <see cref="TrafficAgent"/> is counted at most once while inside the
/// volume; it is forgotten on exit so the same agent can score again only after
/// leaving and re-entering. An optional speed guard suppresses near-misses when
/// the player is crawling.
/// </summary>
[DisallowMultipleComponent]
public class NearMissDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform player;
    [Tooltip("Optional — used to gate near-misses below a minimum speed.")]
    [SerializeField] HyperDriftCarController car;

    [Header("Tuning")]
    [Tooltip("Minimum player speed (kph) required for a near-miss to count.")]
    [SerializeField] float minSpeedKphForNearMiss = 25f;

    // Agents currently inside the trigger that have already been scored.
    readonly HashSet<TrafficAgent> countedAgents = new HashSet<TrafficAgent>();

    /// <summary>Raised when a qualifying near-miss with traffic occurs.</summary>
    public event Action<TrafficAgent> NearMissed;

    void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        var agent = other.GetComponentInParent<TrafficAgent>();
        if (agent == null)
        {
            return;
        }

        if (countedAgents.Contains(agent))
        {
            return;
        }

        if (car != null && car.SpeedKph < minSpeedKphForNearMiss)
        {
            // Still record presence so we don't re-fire the instant speed rises.
            countedAgents.Add(agent);
            return;
        }

        countedAgents.Add(agent);
        NearMissed?.Invoke(agent);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null)
        {
            return;
        }

        var agent = other.GetComponentInParent<TrafficAgent>();
        if (agent != null)
        {
            countedAgents.Remove(agent);
        }
    }

    void OnDisable()
    {
        countedAgents.Clear();
    }
}
