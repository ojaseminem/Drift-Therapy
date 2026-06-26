using UnityEngine;

/// <summary>
/// Authoring data for one traffic vehicle archetype (e.g. Sedan, Truck, Van).
/// The <see cref="TrafficDirector"/> pools instances of <see cref="prefab"/> and
/// configures each spawn from these values. Visual-only data lives here; movement
/// is driven by the director along the road spline.
///
/// Create via: Assets → Create → Drift Therapy → Traffic Vehicle Type
/// </summary>
[CreateAssetMenu(fileName = "TrafficVehicle", menuName = "Drift Therapy/Traffic Vehicle Type")]
public class TrafficVehicleType : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Sedan";

    [Header("Prefab")]
    [Tooltip("Visual + collider prefab. Should sit on the Traffic layer, tagged 'Traffic'. " +
             "A TrafficAgent is added automatically if missing.")]
    public GameObject prefab;

    [Header("Footprint (metres)")]
    [Tooltip("Half-width of the body — used to keep the car inside its lane.")]
    public float halfWidth = 0.9f;
    [Tooltip("Body length — used for spacing / follow-gap so cars never overlap.")]
    public float length = 4.5f;

    [Header("Speed (m/s)")]
    public Vector2 speedRange = new Vector2(12f, 19f);

    [Header("Selection")]
    [Tooltip("Relative likelihood of this archetype being chosen. 0 = never.")]
    [Min(0f)] public float spawnWeight = 1f;

    [Header("Variety")]
    [Tooltip("Optional body colour variants applied via MaterialPropertyBlock (no new materials). " +
             "Leave empty to keep the prefab's material.")]
    public Color[] colorVariants;

    public float RandomSpeed() => Random.Range(speedRange.x, speedRange.y);
}
