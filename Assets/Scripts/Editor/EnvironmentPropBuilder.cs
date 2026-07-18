#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DriftTherapy.EditorTools
{
    /// <summary>
    /// Authors placeholder low-poly scenery (prop prefabs + BiomeEnvironmentSet assets)
    /// and the 4 new mood BiomeData assets (Urban, Rural, Desert, Snow), then wires the
    /// environment sets into all 7 biomes (existing City/Bridge/Hills included).
    ///
    /// Run via menu: Drift Therapy ▶ Generate Placeholder Environment Props.
    /// Re-runnable — updates existing assets in place instead of duplicating them.
    /// Every prefab/material here is a placeholder: swap them out later purely by
    /// replacing references on the same BiomeEnvironmentSet assets, no code changes.
    /// </summary>
    public static class EnvironmentPropBuilder
    {
        const string PrefabDir = "Assets/Prefabs/Environment";
        const string MaterialDir = "Assets/Materials/Environment";
        const string EnvSetDir = "Assets/Data/Environments";
        const string BiomeDir = "Assets/Data/Biomes";
        static readonly Shader LitShader = Shader.Find("Universal Render Pipeline/Lit");

        [MenuItem("Drift Therapy/Generate Placeholder Environment Props")]
        public static void Build()
        {
            EnsureDir(PrefabDir);
            EnsureDir(MaterialDir);
            EnsureDir(EnvSetDir);
            EnsureDir(BiomeDir);

            BuildMoodEnvironment("City", BuildCityCategories);
            BuildMoodEnvironment("Bridge", BuildBridgeCategories);
            BuildMoodEnvironment("Hills", BuildHillsCategories);
            BuildMoodEnvironment("Urban", BuildUrbanCategories);
            BuildMoodEnvironment("Rural", BuildRuralCategories);
            BuildMoodEnvironment("Desert", BuildDesertCategories);
            BuildMoodEnvironment("Snow", BuildSnowCategories);

            WireExistingBiome("Biome_City", "City");
            WireExistingBiome("Biome_Bridge", "Bridge");
            WireExistingBiome("Biome_Hills", "Hills");

            AuthorNewBiome("Biome_Urban", "Urban", "Urban", 900f, 100f,
                fog: new Color(0.5f, 0.48f, 0.45f), ambientSky: new Color(0.42f, 0.4f, 0.38f), ambientEq: new Color(0.28f, 0.27f, 0.25f),
                groundColor: new Color(0.32f, 0.32f, 0.33f));

            AuthorNewBiome("Biome_Rural", "Rural", "Rural", 900f, 100f,
                fog: new Color(0.55f, 0.6f, 0.45f), ambientSky: new Color(0.45f, 0.5f, 0.4f), ambientEq: new Color(0.3f, 0.34f, 0.25f),
                groundColor: new Color(0.28f, 0.4f, 0.18f));

            AuthorNewBiome("Biome_Desert", "Desert", "Desert", 900f, 100f,
                fog: new Color(0.85f, 0.7f, 0.45f), ambientSky: new Color(0.7f, 0.58f, 0.4f), ambientEq: new Color(0.5f, 0.4f, 0.28f),
                groundColor: new Color(0.76f, 0.62f, 0.4f));

            AuthorNewBiome("Biome_Snow", "Snow", "Snow Pass", 900f, 100f,
                fog: new Color(0.8f, 0.85f, 0.9f), ambientSky: new Color(0.75f, 0.8f, 0.85f), ambientEq: new Color(0.55f, 0.6f, 0.65f),
                groundColor: new Color(0.85f, 0.87f, 0.9f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnvironmentPropBuilder] Generated placeholder scenery for 7 moods.");
        }

        // ── Per-mood category definitions ───────────────────────────────────
        static BiomeEnvironmentSet.PropCategory[] BuildCityCategories(string mood)
        {
            return new[]
            {
                BuildingCategory(mood, "Buildings", 3, minHeight: 6f, maxHeight: 22f, spacing: 14f, density: 0.75f, offset: new Vector2(6f, 24f)),
                StreetlightCategory(mood, spacing: 20f, density: 0.5f, offset: new Vector2(4f, 5.5f)),
            };
        }

        static BiomeEnvironmentSet.PropCategory[] BuildBridgeCategories(string mood)
        {
            return new[]
            {
                RailingCategory(mood, spacing: 6f, density: 1f, offset: new Vector2(4f, 4.5f)),
            };
        }

        static BiomeEnvironmentSet.PropCategory[] BuildHillsCategories(string mood)
        {
            return new[]
            {
                TreeCategory(mood, "Trees", new Color(0.18f, 0.42f, 0.16f), spacing: 10f, density: 0.65f, offset: new Vector2(5f, 26f)),
                RockCategory(mood, new Color(0.45f, 0.44f, 0.4f), spacing: 14f, density: 0.4f, offset: new Vector2(5f, 20f)),
            };
        }

        static BiomeEnvironmentSet.PropCategory[] BuildUrbanCategories(string mood)
        {
            return new[]
            {
                BuildingCategory(mood, "Buildings", 3, minHeight: 4f, maxHeight: 10f, spacing: 9f, density: 0.85f, offset: new Vector2(5f, 16f)),
                StreetlightCategory(mood, spacing: 14f, density: 0.6f, offset: new Vector2(4f, 5f)),
                FenceCategory(mood, new Color(0.35f, 0.3f, 0.25f), spacing: 5f, density: 0.5f, offset: new Vector2(4f, 4.5f)),
            };
        }

        static BiomeEnvironmentSet.PropCategory[] BuildRuralCategories(string mood)
        {
            return new[]
            {
                TreeCategory(mood, "Trees", new Color(0.22f, 0.5f, 0.2f), spacing: 12f, density: 0.5f, offset: new Vector2(6f, 24f)),
                FenceCategory(mood, new Color(0.45f, 0.35f, 0.2f), spacing: 5f, density: 0.7f, offset: new Vector2(4f, 4.5f)),
                HaystackCategory(mood, spacing: 22f, density: 0.35f, offset: new Vector2(6f, 20f)),
            };
        }

        static BiomeEnvironmentSet.PropCategory[] BuildDesertCategories(string mood)
        {
            return new[]
            {
                RockCategory(mood, new Color(0.6f, 0.42f, 0.28f), spacing: 12f, density: 0.5f, offset: new Vector2(5f, 22f)),
                CactusCategory(mood, spacing: 14f, density: 0.35f, offset: new Vector2(5f, 20f)),
            };
        }

        static BiomeEnvironmentSet.PropCategory[] BuildSnowCategories(string mood)
        {
            return new[]
            {
                TreeCategory(mood, "Trees", new Color(0.75f, 0.8f, 0.78f), spacing: 11f, density: 0.6f, offset: new Vector2(5f, 26f)),
                RockCategory(mood, new Color(0.65f, 0.66f, 0.68f), spacing: 16f, density: 0.3f, offset: new Vector2(5f, 20f)),
                SnowdriftCategory(mood, spacing: 10f, density: 0.4f, offset: new Vector2(4f, 14f)),
            };
        }

        // ── Category builders (create prefabs + return a PropCategory) ──────
        static BiomeEnvironmentSet.PropCategory BuildingCategory(string mood, string name, int variantCount, float minHeight, float maxHeight, float spacing, float density, Vector2 offset)
        {
            var mat = GetOrCreateMaterial(mood, "Building", new Color(0.55f, 0.53f, 0.5f));
            var prefabs = new GameObject[variantCount];
            for (int i = 0; i < variantCount; i++)
            {
                float t = variantCount > 1 ? i / (float)(variantCount - 1) : 0f;
                float height = Mathf.Lerp(minHeight, maxHeight, t);
                prefabs[i] = GetOrCreatePrefab(mood, $"Building_{i}", () => BuildBox(new Vector3(4f, height, 4f), mat, new Vector3(0f, height * 0.5f, 0f)));
            }
            return new BiomeEnvironmentSet.PropCategory
            {
                name = name, prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(0.9f, 1.1f),
                yRotationJitterDeg = new Vector2(0f, 360f), castShadows = true,
            };
        }

        static BiomeEnvironmentSet.PropCategory TreeCategory(string mood, string name, Color foliageColor, float spacing, float density, Vector2 offset)
        {
            var trunkMat = GetOrCreateMaterial(mood, "TreeTrunk", new Color(0.32f, 0.22f, 0.14f));
            var foliageMat = GetOrCreateMaterial(mood, "TreeFoliage", foliageColor);
            var prefabs = new[]
            {
                GetOrCreatePrefab(mood, "Tree_0", () => BuildTree(trunkMat, foliageMat, 3f, 2.5f)),
                GetOrCreatePrefab(mood, "Tree_1", () => BuildTree(trunkMat, foliageMat, 4f, 3.2f)),
            };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = name, prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(0.8f, 1.25f),
                yRotationJitterDeg = new Vector2(0f, 360f), castShadows = true,
            };
        }

        static BiomeEnvironmentSet.PropCategory RockCategory(string mood, Color color, float spacing, float density, Vector2 offset)
        {
            var mat = GetOrCreateMaterial(mood, "Rock", color);
            var prefabs = new[]
            {
                GetOrCreatePrefab(mood, "Rock_0", () => BuildRock(mat, new Vector3(1.4f, 1.0f, 1.2f))),
                GetOrCreatePrefab(mood, "Rock_1", () => BuildRock(mat, new Vector3(2.2f, 1.6f, 1.8f))),
            };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = "Rocks", prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(0.8f, 1.4f),
                yRotationJitterDeg = new Vector2(0f, 360f), castShadows = false,
            };
        }

        static BiomeEnvironmentSet.PropCategory StreetlightCategory(string mood, float spacing, float density, Vector2 offset)
        {
            var poleMat = GetOrCreateMaterial(mood, "StreetlightPole", new Color(0.15f, 0.15f, 0.16f));
            var lampMat = GetOrCreateMaterial(mood, "StreetlightLamp", new Color(0.95f, 0.9f, 0.6f));
            var prefabs = new[] { GetOrCreatePrefab(mood, "Streetlight_0", () => BuildStreetlight(poleMat, lampMat)) };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = "Streetlights", prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(0.95f, 1.05f),
                yRotationJitterDeg = new Vector2(0f, 0f), castShadows = true,
            };
        }

        static BiomeEnvironmentSet.PropCategory RailingCategory(string mood, float spacing, float density, Vector2 offset)
        {
            var mat = GetOrCreateMaterial(mood, "Railing", new Color(0.4f, 0.42f, 0.45f));
            var prefabs = new[] { GetOrCreatePrefab(mood, "Railing_0", () => BuildBox(new Vector3(0.3f, 1.2f, spacing), mat, new Vector3(0f, 0.6f, 0f))) };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = "Railings", prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(1f, 1f),
                yRotationJitterDeg = new Vector2(0f, 0f), castShadows = false,
            };
        }

        static BiomeEnvironmentSet.PropCategory FenceCategory(string mood, Color color, float spacing, float density, Vector2 offset)
        {
            var mat = GetOrCreateMaterial(mood, "Fence", color);
            var prefabs = new[] { GetOrCreatePrefab(mood, "Fence_0", () => BuildBox(new Vector3(0.15f, 1f, spacing * 0.9f), mat, new Vector3(0f, 0.5f, 0f))) };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = "Fences", prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(1f, 1f),
                yRotationJitterDeg = new Vector2(0f, 0f), castShadows = false,
            };
        }

        static BiomeEnvironmentSet.PropCategory HaystackCategory(string mood, float spacing, float density, Vector2 offset)
        {
            var mat = GetOrCreateMaterial(mood, "Haystack", new Color(0.75f, 0.65f, 0.25f));
            var prefabs = new[] { GetOrCreatePrefab(mood, "Haystack_0", () => BuildCylinder(mat, radius: 1.2f, height: 1.6f)) };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = "Haystacks", prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(0.85f, 1.2f),
                yRotationJitterDeg = new Vector2(0f, 360f), castShadows = true,
            };
        }

        static BiomeEnvironmentSet.PropCategory CactusCategory(string mood, float spacing, float density, Vector2 offset)
        {
            var mat = GetOrCreateMaterial(mood, "Cactus", new Color(0.25f, 0.45f, 0.28f));
            var prefabs = new[] { GetOrCreatePrefab(mood, "Cactus_0", () => BuildCactus(mat)) };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = "Cacti", prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(0.8f, 1.3f),
                yRotationJitterDeg = new Vector2(0f, 360f), castShadows = true,
            };
        }

        static BiomeEnvironmentSet.PropCategory SnowdriftCategory(string mood, float spacing, float density, Vector2 offset)
        {
            var mat = GetOrCreateMaterial(mood, "Snowdrift", new Color(0.92f, 0.94f, 0.96f));
            var prefabs = new[] { GetOrCreatePrefab(mood, "Snowdrift_0", () => BuildRock(mat, new Vector3(2f, 0.6f, 1.6f))) };
            return new BiomeEnvironmentSet.PropCategory
            {
                name = "Snowdrifts", prefabVariants = prefabs, minSpacing = spacing, density = density,
                sideOffsetRange = offset, bothSides = true, scaleJitter = new Vector2(0.8f, 1.3f),
                yRotationJitterDeg = new Vector2(0f, 360f), castShadows = false,
            };
        }

        // ── Primitive assembly helpers ───────────────────────────────────────
        static GameObject BuildBox(Vector3 size, Material mat, Vector3 localOffset)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(go);
            go.transform.localScale = size;
            go.transform.localPosition = localOffset;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return WrapAtOrigin(go);
        }

        static GameObject BuildRock(Material mat, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(go);
            go.transform.localScale = scale;
            go.transform.localPosition = new Vector3(0f, scale.y * 0.5f, 0f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return WrapAtOrigin(go);
        }

        static GameObject BuildCylinder(Material mat, float radius, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(go);
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return WrapAtOrigin(go);
        }

        static GameObject BuildTree(Material trunkMat, Material foliageMat, float trunkHeight, float foliageRadius)
        {
            var root = new GameObject("Tree");

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(trunk);
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localScale = new Vector3(0.4f, trunkHeight * 0.5f, 0.4f);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(foliage);
            foliage.transform.SetParent(root.transform, false);
            foliage.transform.localScale = Vector3.one * (foliageRadius * 2f);
            foliage.transform.localPosition = new Vector3(0f, trunkHeight + foliageRadius * 0.6f, 0f);
            foliage.GetComponent<Renderer>().sharedMaterial = foliageMat;

            return root;
        }

        static GameObject BuildStreetlight(Material poleMat, Material lampMat)
        {
            var root = new GameObject("Streetlight");

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(pole);
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.15f, 2.5f, 0.15f);
            pole.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(lamp);
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
            lamp.transform.localPosition = new Vector3(0f, 5.1f, 0f);
            lamp.GetComponent<Renderer>().sharedMaterial = lampMat;

            return root;
        }

        static GameObject BuildCactus(Material mat)
        {
            var root = new GameObject("Cactus");

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(trunk);
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localScale = new Vector3(0.5f, 1.4f, 0.5f);
            trunk.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            trunk.GetComponent<Renderer>().sharedMaterial = mat;

            var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(arm);
            arm.transform.SetParent(root.transform, false);
            arm.transform.localScale = new Vector3(0.3f, 0.6f, 0.3f);
            arm.transform.localPosition = new Vector3(0.5f, 1.8f, 0f);
            arm.GetComponent<Renderer>().sharedMaterial = mat;

            return root;
        }

        static GameObject WrapAtOrigin(GameObject go)
        {
            var root = new GameObject(go.name);
            go.transform.SetParent(root.transform, false);
            return root;
        }

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col) Object.DestroyImmediate(col);
        }

        // ── Asset persistence helpers ─────────────────────────────────────
        static GameObject GetOrCreatePrefab(string mood, string name, System.Func<GameObject> factory)
        {
            string dir = $"{PrefabDir}/{mood}";
            EnsureDir(dir);
            string path = $"{dir}/{name}.prefab";

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var temp = factory();
            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return prefab;
        }

        static Material GetOrCreateMaterial(string mood, string name, Color color)
        {
            string dir = $"{MaterialDir}/{mood}";
            EnsureDir(dir);
            string path = $"{dir}/M_{mood}_{name}.mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(LitShader) { name = $"M_{mood}_{name}" };
            mat.SetColor("_BaseColor", color);
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void BuildMoodEnvironment(string mood, System.Func<string, BiomeEnvironmentSet.PropCategory[]> categoryFactory)
        {
            string path = $"{EnvSetDir}/EnvSet_{mood}.asset";
            var set = AssetDatabase.LoadAssetAtPath<BiomeEnvironmentSet>(path);
            bool isNew = set == null;
            if (isNew) set = ScriptableObject.CreateInstance<BiomeEnvironmentSet>();

            set.categories = categoryFactory(mood);

            if (isNew) AssetDatabase.CreateAsset(set, path);
            else EditorUtility.SetDirty(set);
        }

        static void WireExistingBiome(string assetName, string mood)
        {
            string biomePath = $"{BiomeDir}/{assetName}.asset";
            var biome = AssetDatabase.LoadAssetAtPath<BiomeData>(biomePath);
            if (biome == null)
            {
                Debug.LogWarning($"[EnvironmentPropBuilder] {biomePath} not found — skipping environment wiring.");
                return;
            }

            var envSet = AssetDatabase.LoadAssetAtPath<BiomeEnvironmentSet>($"{EnvSetDir}/EnvSet_{mood}.asset");
            biome.environmentSet = envSet;
            EditorUtility.SetDirty(biome);
        }

        static void AuthorNewBiome(string assetName, string mood, string displayName, float segmentDuration, float transitionLength,
            Color fog, Color ambientSky, Color ambientEq, Color groundColor)
        {
            string path = $"{BiomeDir}/{assetName}.asset";
            var biome = AssetDatabase.LoadAssetAtPath<BiomeData>(path);
            bool isNew = biome == null;
            if (isNew) biome = ScriptableObject.CreateInstance<BiomeData>();

            var baseAsphalt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_Asphalt.mat");
            var baseKerb = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Road/M_Kerb.mat");
            var groundMat = GetOrCreateMaterial(mood, "Ground", groundColor);

            biome.biomeName = displayName;
            biome.transitionLength = transitionLength;
            biome.segmentDuration = segmentDuration;
            biome.asphaltMaterial = baseAsphalt;
            biome.kerbMaterial = baseKerb;
            biome.fogColor = fog;
            biome.fogDensity = 0.006f;
            biome.ambientSkyColor = ambientSky;
            biome.ambientEquatorColor = ambientEq;
            biome.groundMaterial = groundMat;
            biome.roadWidthMultiplier = 1f;
            biome.curvatureMultiplier = 1f;
            biome.environmentSet = AssetDatabase.LoadAssetAtPath<BiomeEnvironmentSet>($"{EnvSetDir}/EnvSet_{mood}.asset");

            if (isNew) AssetDatabase.CreateAsset(biome, path);
            else EditorUtility.SetDirty(biome);
        }

        static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
#endif
