using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Activates URP (assigns Mobile_RPAsset) and upgrades all Standard materials to URP Lit.
/// Tools → Drift Therapy → Activate URP + Upgrade Materials
/// </summary>
public static class URPActivator
{
    const string MobileRPAssetPath = "Assets/Settings/Mobile_RPAsset.asset";

    [MenuItem("Tools/Drift Therapy/Activate URP + Upgrade Materials")]
    public static void ActivateAndUpgrade()
    {
        // ── 1. Load URP pipeline asset ─────────────────────────────────────
        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRPAssetPath);
        if (urpAsset == null)
        {
            Debug.LogError("[URPActivator] Cannot find " + MobileRPAssetPath);
            return;
        }

        // ── 2. Assign as active pipeline ───────────────────────────────────
        GraphicsSettings.defaultRenderPipeline = urpAsset;

        // Assign per quality level
        int prev = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urpAsset;
        }
        QualitySettings.SetQualityLevel(prev, false);

        Debug.Log("[URPActivator] Active pipeline set to: " + urpAsset.name);

        // ── 3. Upgrade materials ───────────────────────────────────────────
        var urpLit   = Shader.Find("Universal Render Pipeline/Lit");
        var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");

        if (urpLit == null)
        {
            Debug.LogError("[URPActivator] URP shaders not found after pipeline assignment.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material");
        int upgraded = 0, already = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string sn = mat.shader != null ? mat.shader.name : "";

            // Skip package materials (immutable), already URP, UI, hidden, TMP
            if (path.StartsWith("Packages/") ||
                sn.Contains("Universal Render Pipeline") ||
                sn.StartsWith("Hidden/") ||
                sn.StartsWith("TextMeshPro") ||
                sn.StartsWith("UI/") ||
                sn.StartsWith("Sprites/"))
            {
                already++;
                continue;
            }

            // Capture Standard properties before swap
            Color   col       = mat.HasProperty("_Color")      ? mat.GetColor("_Color")      : Color.white;
            float   metallic  = mat.HasProperty("_Metallic")   ? mat.GetFloat("_Metallic")   : 0f;
            float   gloss     = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
            Texture mainTex   = mat.HasProperty("_MainTex")    ? mat.GetTexture("_MainTex")  : null;
            int     mode      = mat.HasProperty("_Mode")       ? (int)mat.GetFloat("_Mode")  : 0;

            bool isUnlit = sn.Contains("Unlit");
            mat.shader = isUnlit ? urpUnlit : urpLit;

            mat.SetColor("_BaseColor", col);
            if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);

            if (!isUnlit)
            {
                mat.SetFloat("_Metallic",   metallic);
                mat.SetFloat("_Smoothness", gloss);

                // Transparency mode
                if (mode == 1)      // Cutout
                {
                    mat.SetFloat("_AlphaClip", 1f);
                    if (mat.HasProperty("_Cutoff"))
                        mat.SetFloat("_Cutoff", mat.GetFloat("_Cutoff"));
                }
                else if (mode >= 2) // Fade / Transparent
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.renderQueue = 3000;
                }
            }

            EditorUtility.SetDirty(mat);
            upgraded++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[URPActivator] ✓ Done. Pipeline: {urpAsset.name} | Upgraded: {upgraded} | Already URP: {already}");
    }
}
