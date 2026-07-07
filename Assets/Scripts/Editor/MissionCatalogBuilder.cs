#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DriftTherapy.EditorTools
{
    /// <summary>
    /// Authors the default mission/trial/daily-pool catalog as real, editable
    /// <see cref="MissionDef"/> assets (no hand-authored YAML — mirrors
    /// <see cref="UIBuilder"/>'s "author via code, not by hand" convention).
    /// Run via menu: Drift Therapy ▶ Build Default Mission Catalog. Re-runnable
    /// — updates existing assets in place by id instead of duplicating them.
    ///
    /// After running, assign the generated assets into the <c>GameApp</c>
    /// prefab/scene instance's <c>missions</c> field in the Inspector — this
    /// script only authors the assets, it does not wire the catalog reference
    /// (that's a scene/prefab edit, out of scope for an editor script).
    /// </summary>
    public static class MissionCatalogBuilder
    {
        const string Dir = "Assets/Data/Missions";

        [MenuItem("Drift Therapy/Build Default Mission Catalog")]
        public static void Build()
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);

            // ── Cumulative (lifetime) ────────────────────────────────────────
            Author("cum_distance_10k", "Road Warrior", "Travel 10,000m in total.",
                MissionScope.Cumulative, MissionMetric.DistanceMeters, 10000, coins: 500);
            Author("cum_nearmiss_100", "Near Miss Master", "Register 100 near misses.",
                MissionScope.Cumulative, MissionMetric.NearMisses, 100, coins: 400, gems: 5);
            Author("cum_coins_5000", "Coin Collector", "Earn 5,000 drift coins.",
                MissionScope.Cumulative, MissionMetric.CoinsEarned, 5000, gems: 10);
            Author("cum_runs_50", "Dedicated Drifter", "Complete 50 runs.",
                MissionScope.Cumulative, MissionMetric.RunsCompleted, 50, coins: 600, xp: 200);
            Author("cum_combo_300", "Combo Chaser", "Build big combo chains, over time.",
                MissionScope.Cumulative, MissionMetric.DriftCombo, 300, gems: 8);

            // ── Daily pool (one rolled active per day) ──────────────────────
            Author("daily_distance_500", "Distance Dash", "Travel 500m today.",
                MissionScope.Daily, MissionMetric.DistanceMeters, 500, coins: 150);
            Author("daily_nearmiss_10", "Close Calls", "Register 10 near misses today.",
                MissionScope.Daily, MissionMetric.NearMisses, 10, coins: 150);
            Author("daily_coins_300", "Coin Rush", "Earn 300 drift coins today.",
                MissionScope.Daily, MissionMetric.CoinsEarned, 300, gems: 3);
            Author("daily_runs_3", "Warm Up", "Complete 3 runs today.",
                MissionScope.Daily, MissionMetric.RunsCompleted, 3, coins: 200);

            // ── Per-run trials ───────────────────────────────────────────────
            Author("trial_distance_800", "Long Haul", "Reach 800m in a single run.",
                MissionScope.PerRunTrial, MissionMetric.DistanceMeters, 800, coins: 300);
            Author("trial_nearmiss_5", "Thread the Needle", "5 near misses in one run.",
                MissionScope.PerRunTrial, MissionMetric.NearMisses, 5, coins: 250);
            Author("trial_combo_8", "Combo Streak", "Reach an 8x combo chain in one run.",
                MissionScope.PerRunTrial, MissionMetric.DriftCombo, 8, coins: 300);
            Author("trial_distance_1500", "Marathon Drifter", "Reach 1,500m in a single run.",
                MissionScope.PerRunTrial, MissionMetric.DistanceMeters, 1500, gems: 8);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MissionCatalogBuilder] Built/updated default mission catalog in " + Dir);
        }

        static void Author(string id, string title, string description, MissionScope scope,
            MissionMetric metric, int target, int coins = 0, int gems = 0, int xp = 0, string achievementId = "")
        {
            string path = Dir + "/" + id + ".asset";
            var def = AssetDatabase.LoadAssetAtPath<MissionDef>(path);
            bool isNew = def == null;
            if (isNew) def = ScriptableObject.CreateInstance<MissionDef>();

            def.id = id;
            def.title = title;
            def.description = description;
            def.scope = scope;
            def.metric = metric;
            def.targetValue = target;
            def.rewardCoins = coins;
            def.rewardGems = gems;
            def.rewardXp = xp;
            def.achievementId = achievementId;

            if (isNew) AssetDatabase.CreateAsset(def, path);
            else EditorUtility.SetDirty(def);
        }
    }
}
#endif
