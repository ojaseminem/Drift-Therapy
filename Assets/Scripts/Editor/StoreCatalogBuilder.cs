#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DriftTherapy.EditorTools
{
    /// <summary>
    /// Authors placeholder low-poly cosmetic attachments (spoilers, underglow) and
    /// booster trail-color skins for the Store screen.
    ///
    /// Run via menu: Drift Therapy ▶ Generate Store Catalog (Attachments &amp; Boosters).
    /// Re-runnable — updates existing assets in place instead of duplicating them.
    /// Every prefab/material here is a placeholder: swap them out later purely by
    /// replacing references on the same CosmeticAttachmentDef/BoosterSkinDef assets,
    /// no code changes.
    /// </summary>
    public static class StoreCatalogBuilder
    {
        const string PrefabDir = "Assets/Prefabs/Attachments";
        const string MaterialDir = "Assets/Materials/Attachments";
        const string AttachmentDir = "Assets/Data/Store/Attachments";
        const string BoosterDir = "Assets/Data/Store/Boosters";
        const string CurrencyDir = "Assets/Data/Store/Currency";
        static readonly Shader LitShader = Shader.Find("Universal Render Pipeline/Lit");

        [MenuItem("Drift Therapy/Generate Store Catalog (Attachments & Boosters)")]
        public static void Build()
        {
            EnsureDir(PrefabDir);
            EnsureDir(MaterialDir);
            EnsureDir(AttachmentDir);
            EnsureDir(BoosterDir);
            EnsureDir(CurrencyDir);

            AuthorAttachment("Spoiler_Racing", "Racing Spoiler", "Spoiler", new Color(0.08f, 0.08f, 0.09f), price: 800, gemPrice: 0, ownedByDefault: false, () => BuildSpoiler(new Color(0.08f, 0.08f, 0.09f)));
            AuthorAttachment("Spoiler_Carbon", "Carbon Spoiler", "Spoiler", new Color(0.15f, 0.16f, 0.18f), price: 0, gemPrice: 25, ownedByDefault: false, () => BuildSpoiler(new Color(0.15f, 0.16f, 0.18f)));
            AuthorAttachment("Underglow_Blue", "Blue Underglow", "Underglow", new Color(0.15f, 0.55f, 1f), price: 600, gemPrice: 0, ownedByDefault: false, () => BuildUnderglow(new Color(0.15f, 0.55f, 1f)));
            AuthorAttachment("Underglow_Pink", "Pink Underglow", "Underglow", new Color(1f, 0.2f, 0.65f), price: 600, gemPrice: 0, ownedByDefault: false, () => BuildUnderglow(new Color(1f, 0.2f, 0.65f)));
            AuthorAttachment("Underglow_Green", "Toxic Underglow", "Underglow", new Color(0.35f, 1f, 0.3f), price: 0, gemPrice: 20, ownedByDefault: false, () => BuildUnderglow(new Color(0.35f, 1f, 0.3f)));

            AuthorBooster("Booster_Default", "Classic Smoke", new Color(0.75f, 0.75f, 0.75f, 0.55f), price: 0, gemPrice: 0, ownedByDefault: true);
            AuthorBooster("Booster_Inferno", "Inferno Trail", new Color(1f, 0.4f, 0.1f, 0.75f), price: 500, gemPrice: 0, ownedByDefault: false);
            AuthorBooster("Booster_Toxic", "Toxic Trail", new Color(0.35f, 1f, 0.3f, 0.7f), price: 500, gemPrice: 0, ownedByDefault: false);
            AuthorBooster("Booster_Neon", "Neon Trail", new Color(0.85f, 0.25f, 1f, 0.7f), price: 0, gemPrice: 20, ownedByDefault: false);
            AuthorBooster("Booster_Ice", "Ice Trail", new Color(0.6f, 0.9f, 1f, 0.65f), price: 500, gemPrice: 0, ownedByDefault: false);

            AuthorCurrencyPack("Coins_Small", "coins_small", "Small Drift Coin Pack", CurrencyType.Coins, 1000, bonusPercent: 0, fallbackPrice: "$0.99");
            AuthorCurrencyPack("Coins_Medium", "coins_medium", "Medium Drift Coin Pack", CurrencyType.Coins, 5500, bonusPercent: 10, fallbackPrice: "$2.99");
            AuthorCurrencyPack("Coins_Large", "coins_large", "Large Drift Coin Pack", CurrencyType.Coins, 12000, bonusPercent: 20, fallbackPrice: "$5.99");
            AuthorCurrencyPack("Gems_Small", "gems_small", "Small Gem Pack", CurrencyType.Gems, 50, bonusPercent: 0, fallbackPrice: "$0.99");
            AuthorCurrencyPack("Gems_Large", "gems_large", "Large Gem Pack", CurrencyType.Gems, 300, bonusPercent: 15, fallbackPrice: "$4.99");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StoreCatalogBuilder] Generated 5 attachments + 5 booster skins + 5 currency packs.");
        }

        // ── Attachments ──────────────────────────────────────────────────────
        static GameObject BuildSpoiler(Color color)
        {
            var mat = GetOrCreateMaterial("Spoiler_" + ColorKey(color), color, emissive: false);
            var root = new GameObject("Spoiler");

            var strutL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(strutL);
            strutL.transform.SetParent(root.transform, false);
            strutL.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
            strutL.transform.localPosition = new Vector3(-0.5f, 0.09f, 0f);
            strutL.GetComponent<Renderer>().sharedMaterial = mat;

            var strutR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(strutR);
            strutR.transform.SetParent(root.transform, false);
            strutR.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
            strutR.transform.localPosition = new Vector3(0.5f, 0.09f, 0f);
            strutR.GetComponent<Renderer>().sharedMaterial = mat;

            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(wing);
            wing.transform.SetParent(root.transform, false);
            wing.transform.localScale = new Vector3(1.3f, 0.06f, 0.35f);
            wing.transform.localPosition = new Vector3(0f, 0.21f, 0f);
            wing.GetComponent<Renderer>().sharedMaterial = mat;

            return root;
        }

        static GameObject BuildUnderglow(Color color)
        {
            var mat = GetOrCreateMaterial("Underglow_" + ColorKey(color), color, emissive: true);
            var root = new GameObject("Underglow");

            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(glow);
            glow.transform.SetParent(root.transform, false);
            glow.transform.localScale = new Vector3(1.6f, 0.03f, 3.2f);
            glow.transform.localPosition = Vector3.zero;
            glow.GetComponent<Renderer>().sharedMaterial = mat;

            return root;
        }

        static void AuthorAttachment(string id, string displayName, string slotId, Color color, int price, int gemPrice, bool ownedByDefault, System.Func<GameObject> factory)
        {
            string prefabPath = $"{PrefabDir}/{id}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                var temp = factory();
                prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
                Object.DestroyImmediate(temp);
            }

            string defPath = $"{AttachmentDir}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CosmeticAttachmentDef>(defPath);
            bool isNew = def == null;
            if (isNew) def = ScriptableObject.CreateInstance<CosmeticAttachmentDef>();

            def.id = id;
            def.displayName = displayName;
            def.slotId = slotId;
            def.attachmentPrefab = prefab;
            def.price = price;
            def.gemPrice = gemPrice;
            def.ownedByDefault = ownedByDefault;

            if (isNew) AssetDatabase.CreateAsset(def, defPath);
            else EditorUtility.SetDirty(def);
        }

        // ── Boosters ─────────────────────────────────────────────────────────
        static void AuthorBooster(string id, string displayName, Color trailColor, int price, int gemPrice, bool ownedByDefault)
        {
            string path = $"{BoosterDir}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<BoosterSkinDef>(path);
            bool isNew = def == null;
            if (isNew) def = ScriptableObject.CreateInstance<BoosterSkinDef>();

            def.id = id;
            def.displayName = displayName;
            def.trailColor = trailColor;
            def.price = price;
            def.gemPrice = gemPrice;
            def.ownedByDefault = ownedByDefault;

            if (isNew) AssetDatabase.CreateAsset(def, path);
            else EditorUtility.SetDirty(def);
        }

        // ── Currency packs ───────────────────────────────────────────────────
        static void AuthorCurrencyPack(string id, string productId, string displayName, CurrencyType currencyType, int amount, int bonusPercent, string fallbackPrice)
        {
            string path = $"{CurrencyDir}/{id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CurrencyPackDef>(path);
            bool isNew = def == null;
            if (isNew) def = ScriptableObject.CreateInstance<CurrencyPackDef>();

            def.id = id;
            def.productId = productId;
            def.displayName = displayName;
            def.currencyType = currencyType;
            def.amount = amount;
            def.bonusPercent = bonusPercent;
            def.fallbackPriceText = fallbackPrice;

            if (isNew) AssetDatabase.CreateAsset(def, path);
            else EditorUtility.SetDirty(def);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        static Material GetOrCreateMaterial(string name, Color color, bool emissive)
        {
            string path = $"{MaterialDir}/M_{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(LitShader) { name = "M_" + name };
            mat.SetColor("_BaseColor", color);
            mat.enableInstancing = true;

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.5f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static string ColorKey(Color c) => Mathf.RoundToInt(c.r * 255) + "_" + Mathf.RoundToInt(c.g * 255) + "_" + Mathf.RoundToInt(c.b * 255);

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col) Object.DestroyImmediate(col);
        }

        static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
#endif
