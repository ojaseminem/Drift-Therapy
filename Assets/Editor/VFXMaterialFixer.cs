using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Fixes VFX materials (particles, trails, skid marks) that were broken by the
/// URP upgrade — they were set to Opaque/Lit when they need Transparent/Particles/Unlit.
///
/// Tools → Drift Therapy → Fix VFX Materials
/// </summary>
public static class VFXMaterialFixer
{
    // URP blend mode enum values
    const int Surface_Opaque      = 0;
    const int Surface_Transparent = 1;

    const int Blend_Alpha    = 0;   // SrcAlpha, OneMinusSrcAlpha
    const int Blend_Additive = 3;   // One, One

    const int SrcBlend_SrcAlpha = 5;
    const int DstBlend_OneMinusSrcAlpha = 10;
    const int SrcBlend_One = 1;
    const int DstBlend_One = 1;

    [MenuItem("Tools/Drift Therapy/Fix VFX Materials")]
    public static void FixAll()
    {
        var particlesUnlit  = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var particlesLit    = Shader.Find("Universal Render Pipeline/Particles/Lit");

        if (particlesUnlit == null)
        {
            Debug.LogError("[VFXFixer] URP Particles/Unlit shader not found. Is URP installed?");
            return;
        }

        int fixed_ = 0;

        // ── Explicit targeted fixes ─────────────────────────────────────
        fixed_ += FixParticlesMat(
            "Assets/ACC_Lite/Materials/World/Particles01.mat",
            particlesUnlit,
            blendMode: Blend_Additive,   // smoke is additive — soft glow
            baseColor: new Color(0.7f, 0.7f, 0.7f, 0.55f),
            doubleSided: true);

        fixed_ += FixParticlesMat(
            "Assets/ACC_Lite/Materials/World/TrailMaterial.mat",
            particlesUnlit,
            blendMode: Blend_Alpha,      // skid marks are alpha-blended
            baseColor: new Color(0.1f, 0.1f, 0.1f, 0.75f),
            doubleSided: false);

        // ── Scan for any other materials still on wrong shaders ─────────
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            string sn = mat.shader.name;

            // Any material using Particles/Lit that has transparency hints → convert to Unlit
            if (sn == "Universal Render Pipeline/Particles/Lit" ||
                sn == "Universal Render Pipeline/Particles/Simple Lit")
            {
                // Only fix if it's set opaque (Surface=0) — the URP upgrader's mistake
                if (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") < 0.5f)
                {
                    ApplyTransparency(mat, particlesUnlit, Blend_Additive);
                    EditorUtility.SetDirty(mat);
                    Debug.Log($"[VFXFixer] Auto-fixed particle material: {path}");
                    fixed_++;
                }
            }

            // Legacy Built-in particle shaders that slipped through
            if (sn.StartsWith("Particles/") || sn == "Mobile/Particles/Additive")
            {
                Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color   col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                mat.shader = particlesUnlit;
                if (tex != null) mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", col);
                ApplyTransparency(mat, particlesUnlit, Blend_Additive);
                EditorUtility.SetDirty(mat);
                Debug.Log($"[VFXFixer] Converted legacy particle shader: {path}");
                fixed_++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VFXFixer] ✓ Fixed {fixed_} VFX material(s).");
    }

    static int FixParticlesMat(string path, Shader shader, int blendMode,
                                Color baseColor, bool doubleSided)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Debug.LogWarning($"[VFXFixer] Not found: {path}");
            return 0;
        }

        // Preserve existing texture
        Texture existingTex = null;
        if (mat.HasProperty("_BaseMap"))  existingTex = mat.GetTexture("_BaseMap");
        if (existingTex == null && mat.HasProperty("_MainTex")) existingTex = mat.GetTexture("_MainTex");

        mat.shader = shader;

        if (existingTex != null) mat.SetTexture("_BaseMap", existingTex);
        mat.SetColor("_BaseColor", baseColor);

        ApplyTransparency(mat, shader, blendMode);

        // Double-sided for smoke (visible from all angles)
        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", doubleSided ? 0f : 2f); // 0=Off, 2=Back

        // Vertex color mode — lets particle system tint by lifetime color
        if (mat.HasProperty("_ColorMode"))
            mat.SetFloat("_ColorMode", 0f); // Multiply

        EditorUtility.SetDirty(mat);
        Debug.Log($"[VFXFixer] Fixed: {Path.GetFileName(path)} → Particles/Unlit Transparent");
        return 1;
    }

    static void ApplyTransparency(Material mat, Shader shader, int blendMode)
    {
        mat.SetFloat("_Surface", Surface_Transparent);
        mat.SetFloat("_ZWrite",  0f);
        mat.renderQueue = 3000; // Transparent queue

        if (blendMode == Blend_Additive)
        {
            mat.SetFloat("_Blend",    Blend_Additive);
            mat.SetFloat("_SrcBlend", SrcBlend_One);
            mat.SetFloat("_DstBlend", DstBlend_One);
            mat.SetFloat("_SrcBlendAlpha", SrcBlend_One);
            mat.SetFloat("_DstBlendAlpha", DstBlend_One);
        }
        else // Alpha
        {
            mat.SetFloat("_Blend",    Blend_Alpha);
            mat.SetFloat("_SrcBlend", SrcBlend_SrcAlpha);
            mat.SetFloat("_DstBlend", DstBlend_OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", SrcBlend_One);
            mat.SetFloat("_DstBlendAlpha", DstBlend_OneMinusSrcAlpha);
        }

        // Enable transparency keywords
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");

        // Update render type tag
        mat.SetOverrideTag("RenderType", "Transparent");
    }
}
