using System;
using UnityEngine;

/// <summary>
/// Vehicle condition. Collisions deal damage instead of ending the run; damage
/// degrades handling/speed (via <see cref="HyperDriftCarController.SetDamage01"/>).
/// Mechanic collectibles repair it. The run only ends when fully wrecked.
/// </summary>
[DisallowMultipleComponent]
public class VehicleHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float damagePerHit = 22f;
    [SerializeField] float graceSeconds = 0.8f;
    [SerializeField] HyperDriftCarController car;

    float health;
    float graceUntil = -1f;

    public float Health01 => maxHealth > 0f ? health / maxHealth : 0f;
    public bool IsTotaled => health <= 0f;
    public bool InGrace => Time.time < graceUntil;
    public float DamagePerHit => damagePerHit;

    public event Action<float> HealthChanged;  // 0..1
    public event Action Totaled;

    void Awake()
    {
        if (!car) car = GetComponent<HyperDriftCarController>();
        health = maxHealth;
        Apply();
    }

    void Start() => HealthChanged?.Invoke(Health01);

    public void TakeDamage(float amount)
    {
        if (InGrace || health <= 0f) return;
        health = Mathf.Max(0f, health - amount);
        graceUntil = Time.time + Mathf.Max(0f, graceSeconds);
        Apply();
        HealthChanged?.Invoke(Health01);
        if (health <= 0f) Totaled?.Invoke();
    }

    public void Repair(float amount)
    {
        if (health <= 0f) return;
        health = Mathf.Min(maxHealth, health + amount);
        Apply();
        HealthChanged?.Invoke(Health01);
    }

    /// <summary>Temporary invulnerability (used by boost).</summary>
    public void GrantGrace(float seconds) => graceUntil = Mathf.Max(graceUntil, Time.time + seconds);

    public void ResetHealth()
    {
        health = maxHealth;
        graceUntil = -1f;
        Apply();
        HealthChanged?.Invoke(Health01);
    }

    void Apply() { if (car) car.SetDamage01(1f - Health01); }
}
