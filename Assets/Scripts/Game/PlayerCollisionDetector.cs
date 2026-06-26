using System;
using UnityEngine;

/// <summary>
/// Detects collisions between the player car and traffic. Lives on the player
/// car GameObject (the one with colliders / rigidbody). Fires <see cref="Hit"/>
/// once per qualifying contact so <c>GameController</c> can fail the run.
///
/// A short grace window (see <see cref="StartGrace"/>) lets a revive briefly
/// ignore hits so the player is not immediately re-killed by the same traffic.
/// Works with both solid collisions and trigger overlaps.
/// </summary>
[DisallowMultipleComponent]
public class PlayerCollisionDetector : MonoBehaviour
{
    [Header("Matching")]
    [Tooltip("Layers considered traffic. If empty, falls back to the tag check.")]
    [SerializeField] LayerMask trafficMask;
    [Tooltip("Tag considered traffic when the layer mask does not match.")]
    [SerializeField] string trafficTag = "Traffic";

    [Header("Grace")]
    [Tooltip("Seconds of invulnerability granted by StartGrace (e.g. after a revive).")]
    [SerializeField] float graceSeconds = 0.3f;

    float graceUntil = -1f;

    /// <summary>Raised when the player hits traffic. Payload = the traffic GameObject.</summary>
    public event Action<GameObject> Hit;

    /// <summary>True while the post-revive grace window is active.</summary>
    public bool IsInGrace => Time.time < graceUntil;

    /// <summary>Begins a brief window during which hits are ignored.</summary>
    public void StartGrace()
    {
        graceUntil = Time.time + Mathf.Max(0f, graceSeconds);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.collider != null)
        {
            Evaluate(collision.collider.gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            Evaluate(other.gameObject);
        }
    }

    void Evaluate(GameObject other)
    {
        if (IsInGrace || other == null)
        {
            return;
        }

        if (!IsTraffic(other))
        {
            return;
        }

        Hit?.Invoke(other);
    }

    bool IsTraffic(GameObject other)
    {
        // Layer mask takes priority when configured.
        if (trafficMask.value != 0 && (trafficMask.value & (1 << other.layer)) != 0)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(trafficTag) && other.CompareTag(trafficTag))
        {
            return true;
        }

        return false;
    }
}
