using UnityEngine;

/// <summary>
/// Scrolls road material UVs to give the impression of forward motion.
/// Attach to the same GameObject as RoadSegmentPool or to a dedicated manager.
/// Materials must use a shader that reads _MainTex_ST (tiling/offset) — URP Lit works.
/// </summary>
public class RoadVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Material asphaltMaterial;
    [SerializeField] Material kerbMaterial;

    [Header("Scroll speed (UV units per second)")]
    [SerializeField] float asphaltScrollSpeed = 1.4f;
    [SerializeField] float kerbScrollSpeed = 1.4f;

    float asphaltOffset;
    float kerbOffset;

    MaterialPropertyBlock asphaltBlock;
    MaterialPropertyBlock kerbBlock;

    // Cache renderer references set by RoadSegmentPool at runtime
    Renderer[] managedRenderers;

    void Awake()
    {
        asphaltBlock = new MaterialPropertyBlock();
        kerbBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        asphaltOffset += asphaltScrollSpeed * Time.deltaTime;
        kerbOffset += kerbScrollSpeed * Time.deltaTime;

        if (asphaltMaterial != null)
            asphaltMaterial.SetTextureOffset("_BaseMap", new Vector2(0f, -asphaltOffset));

        if (kerbMaterial != null)
            kerbMaterial.SetTextureOffset("_BaseMap", new Vector2(0f, -kerbOffset));
    }
}
