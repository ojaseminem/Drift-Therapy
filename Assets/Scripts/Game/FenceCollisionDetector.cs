using System;
using UnityEngine;

/// <summary>
/// Detects the player car contacting a roadside fence (<see cref="FenceSurface"/>)
/// and classifies each contact as a minor screech or a run-ending crash. Lives on
/// the player car GameObject (the one with the Rigidbody), same placement
/// convention as <see cref="PlayerCollisionDetector"/>.
///
/// Severity is the speed component driving straight INTO the wall (relative
/// velocity projected onto the contact normal) — a fast graze along the fence
/// has a low normal component and reads as a screech, while a hard, more
/// head-on hit has a high normal component and reads as a crash. This single
/// metric naturally folds both impact angle and impact speed together.
/// </summary>
[DisallowMultipleComponent]
public class FenceCollisionDetector : MonoBehaviour
{
    [Header("Severity")]
    [Tooltip("Impact speed (m/s) driving straight into the fence at or above which a hit is a crash, not a screech.")]
    [SerializeField] float crashNormalSpeedThreshold = 6f;
    [Tooltip("Seconds between repeated screech events while continuously scraping, so contact doesn't spam every physics tick.")]
    [SerializeField] float screechCooldown = 0.2f;

    float screechCooldownUntil = -1f;
    bool crashed;

    /// <summary>Raised on a minor scrape. Payload = world contact point + contact normal.</summary>
    public event Action<Vector3, Vector3> Screeched;
    /// <summary>Raised once on a hard hit. Payload = world contact point.</summary>
    public event Action<Vector3> Crashed;

    /// <summary>Call on revive/restart so a fresh run can crash again.</summary>
    public void ResetState() => crashed = false;

    void OnCollisionEnter(Collision collision) => Evaluate(collision);
    void OnCollisionStay(Collision collision) => Evaluate(collision);

    void Evaluate(Collision collision)
    {
        if (crashed || collision == null || collision.collider == null || collision.contactCount == 0) return;
        if (collision.collider.GetComponentInParent<FenceSurface>() == null) return;

        ContactPoint contact = collision.GetContact(0);
        float normalSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));

        if (normalSpeed >= crashNormalSpeedThreshold)
        {
            crashed = true;
            Crashed?.Invoke(contact.point);
            return;
        }

        if (Time.time < screechCooldownUntil) return;
        screechCooldownUntil = Time.time + screechCooldown;
        Screeched?.Invoke(contact.point, contact.normal);
    }
}
