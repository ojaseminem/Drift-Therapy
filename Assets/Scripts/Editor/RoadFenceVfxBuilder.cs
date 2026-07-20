#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DriftTherapy.EditorTools
{
    /// <summary>
    /// Authors the placeholder fence material and the pooled spark-impact VFX
    /// prefab used by the roadside fence / screech system.
    ///
    /// Run via menu: Drift Therapy ▶ Generate Fence & Spark VFX.
    /// Re-runnable — updates the existing assets in place.
    /// </summary>
    public static class RoadFenceVfxBuilder
    {
        const string MaterialDir = "Assets/Materials/Road";
        const string VfxPrefabDir = "Assets/Prefabs/VFX";

        [MenuItem("Drift Therapy/Generate Fence & Spark VFX")]
        public static void Build()
        {
            EnsureDir(MaterialDir);
            EnsureDir(VfxPrefabDir);

            Material fenceMat = BuildFenceMaterial();
            GameObject sparkPrefab = BuildSparkPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RoadFenceVfxBuilder] Generated fence material + spark VFX prefab.");
        }

        static Material BuildFenceMaterial()
        {
            string path = $"{MaterialDir}/M_Fence.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader) { name = "M_Fence" };
            mat.SetColor("_BaseColor", new Color(0.55f, 0.56f, 0.58f));
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.35f);
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static GameObject BuildSparkPrefab()
        {
            string path = $"{VfxPrefabDir}/Spark_Impact.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("Spark_Impact");
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
            // Hot white-yellow core fading toward orange — reads as a real metal-on-metal spark, not a dim dot.
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.98f, 0.85f), new Color(1f, 0.55f, 0.1f));
            main.gravityModifier = 1.3f;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.05f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            // Color-over-lifetime fade to transparent so sparks don't pop out of existence.
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var alphaGradient = new Gradient();
            alphaGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = alphaGradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            // Stretched billboards read as directional spark streaks instead of plain
            // dots, and read much more clearly at drift speed.
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.12f;
            renderer.lengthScale = 3f;
            renderer.sharedMaterial = BuildSparkMaterial();

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(go.transform, false);
            var light = flashGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.4f);
            light.range = 6f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            var flash = go.AddComponent<ImpactFlash>();
            var soFlash = new SerializedObject(flash);
            soFlash.FindProperty("flashLight").objectReferenceValue = light;
            soFlash.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static Material BuildSparkMaterial()
        {
            string path = $"{MaterialDir}/M_Spark_Additive.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            // URP's particle-unlit shader with a manual additive blend state — the old
            // Default-Particle.mat is a Built-in RP "Legacy Shaders/..." asset that
            // doesn't render correctly (or additively) under URP, which was the real
            // reason the spark effect read as faint/washed out.
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var mat = new Material(shader) { name = "M_Spark_Additive" };
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 2f);   // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.SetColor("_BaseColor", Color.white);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
#endif
