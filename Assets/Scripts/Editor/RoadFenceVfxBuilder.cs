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
            main.duration = 0.3f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.5f, 0.1f));
            main.gravityModifier = 1.2f;
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14, 22) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
#endif
