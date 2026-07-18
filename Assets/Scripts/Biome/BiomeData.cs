using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Data asset for one environment biome in a Drift Therapy run.
/// Create via: Assets → Create → Drift Therapy → Biome Data
///
/// One run streams multiple biomes in sequence:
///   [City] → [Desert] → [Mountains] → [Night City] → ...
///
/// Only visual data lives here — road geometry is always the same spline system.
/// </summary>
[CreateAssetMenu(fileName = "BiomeData", menuName = "Drift Therapy/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Header("Identity")]
    public string biomeName = "Unnamed Biome";

    [Header("Trigger")]
    [Tooltip("Player distance (metres) at which this biome begins to blend in. " +
             "Used directly in fixed-order mode; overwritten at runtime from segmentDuration when shuffled.")]
    public float startDistance = 0f;
    [Tooltip("Metres over which to cross-fade from the previous biome.")]
    public float transitionLength = 80f;
    [Tooltip("Metres this biome runs for before the next one begins, when RoadBiomeManager shuffles the order per run.")]
    public float segmentDuration = 400f;

    [Header("Road Materials")]
    [Tooltip("Asphalt surface material — slot 0 on road segments.")]
    public Material asphaltMaterial;
    [Tooltip("Kerb material — slot 1 on road segments.")]
    public Material kerbMaterial;

    [Header("Atmosphere")]
    public Color  fogColor        = new Color(0.5f, 0.5f, 0.55f, 1f);
    [Range(0f, 0.05f)]
    public float  fogDensity      = 0.005f;
    public Color  ambientSkyColor = new Color(0.3f, 0.35f, 0.45f);
    public Color  ambientEquatorColor = new Color(0.25f, 0.28f, 0.32f);

    [Header("Ground")]
    [Tooltip("Flat ground plane material (the infinite floor).")]
    public Material groundMaterial;

    [Header("Sky")]
    [Tooltip("Skybox material — leave null to use the scene default.")]
    public Material skyboxMaterial;

    [Header("Road Feel (optional per-biome overrides)")]
    [Tooltip("Multiplier on road half-width. 1 = standard.")]
    [Range(0.6f, 1.4f)]
    public float roadWidthMultiplier = 1f;
    [Tooltip("Multiplier on curve intensity. 1 = standard, <1 = gentler.")]
    [Range(0.5f, 1.5f)]
    public float curvatureMultiplier = 1f;

    [Header("Post-Processing")]
    [Tooltip("Optional VolumeProfile to blend in during this biome.")]
    public VolumeProfile postProcessProfile;

    [Header("Scenery")]
    [Tooltip("Scattered props (buildings, trees, rocks, ...) for this mood. Optional — leave null for no scenery.")]
    public BiomeEnvironmentSet environmentSet;
}
