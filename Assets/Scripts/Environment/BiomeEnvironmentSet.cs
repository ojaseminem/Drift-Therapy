using UnityEngine;

/// <summary>
/// Data asset describing the scattered scenery for one mood/biome.
/// Create via: Assets → Create → Drift Therapy → Biome Environment Set
///
/// Purely data — adding a new mood's scenery never requires new code,
/// just a new asset referencing new prop prefabs/categories.
/// </summary>
[CreateAssetMenu(fileName = "EnvSet", menuName = "Drift Therapy/Biome Environment Set")]
public class BiomeEnvironmentSet : ScriptableObject
{
    [System.Serializable]
    public class PropCategory
    {
        public string name = "Category";

        [Tooltip("Random variant is picked from this list for each placement.")]
        public GameObject[] prefabVariants;

        [Tooltip("Metres between placement attempts along the road.")]
        public float minSpacing = 8f;

        [Range(0f, 1f)]
        [Tooltip("Probability that a placement attempt actually spawns something.")]
        public float density = 0.6f;

        [Tooltip("Distance range from the road edge to place this prop.")]
        public Vector2 sideOffsetRange = new Vector2(4f, 20f);

        [Tooltip("Place on both sides of the road, or only the right side.")]
        public bool bothSides = true;

        public Vector2 scaleJitter = new Vector2(0.85f, 1.15f);
        public Vector2 yRotationJitterDeg = new Vector2(0f, 360f);

        [Tooltip("Disable for distant/background props to save fill-rate on mobile/WebGL.")]
        public bool castShadows = true;
    }

    public PropCategory[] categories;
}
