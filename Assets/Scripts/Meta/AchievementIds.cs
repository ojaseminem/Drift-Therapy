namespace DriftTherapy
{
    /// <summary>
    /// Central catalog of Google Play Games achievement string ids used by this game.
    /// These are invented here as stable, descriptive names — you must create matching
    /// achievements in the Play Console using these exact id strings (Play Console
    /// generates its own opaque achievement id per entry; paste that generated id back
    /// here, replacing the placeholder string, once each achievement exists there).
    /// </summary>
    public static class AchievementIds
    {
        // Mission-linked — unlocked via GameApp.ClaimMission() when the matching
        // cumulative MissionDef.achievementId is claimed. See MissionCatalogBuilder.
        public const string RoadWarrior = "achievement_road_warrior";           // cum_distance_10k — 10,000m lifetime
        public const string NearMissMaster = "achievement_near_miss_master";    // cum_nearmiss_100 — 100 near misses
        public const string CoinCollector = "achievement_coin_collector";       // cum_coins_5000 — 5,000 coins earned
        public const string DedicatedDrifter = "achievement_dedicated_drifter"; // cum_runs_50 — 50 runs completed
        public const string ComboChaser = "achievement_combo_chaser";           // cum_combo_300 — 300 combo, lifetime

        // Standalone milestones — unlocked directly by GameApp/GameController at the
        // moment they occur, via GameApp.TryUnlockAchievement().
        public const string FirstDrift = "achievement_first_drift";             // first run ever completed
        public const string SpeedDemon = "achievement_speed_demon";             // 2,000m in a single run
        public const string GarageCollector = "achievement_garage_collector";   // own 5+ vehicles
        public const string FullHouse = "achievement_full_house";               // own every vehicle in the catalog
        public const string Supporter = "achievement_supporter";                // purchased Remove Ads

        // Distance milestones (lifetime)
        public const string FirstMile = "achievement_first_mile";               // 1,000m lifetime
        public const string Marathoner = "achievement_marathoner";              // 50,000m lifetime
        public const string RoadLegend = "achievement_road_legend";             // 100,000m lifetime
        public const string EndlessHorizon = "achievement_endless_horizon";     // 5,000m in a single run

        // Skill (single run)
        public const string DriftKing = "achievement_drift_king";              // 20x combo in a single run
        public const string Daredevil = "achievement_daredevil";               // 10 near misses in a single run

        // Currency (balance held at once)
        public const string CoinBaron = "achievement_coin_baron";               // hold 10,000 coins at once
        public const string GemHunter = "achievement_gem_hunter";               // hold 100 gems at once

        // Dedication
        public const string VeteranDrifter = "achievement_veteran_drifter";     // 200 runs lifetime
        public const string DailyDevotee = "achievement_daily_devotee";         // 7-day login streak
        public const string StreakMaster = "achievement_streak_master";        // 30-day login streak

        // Collection
        public const string Fashionista = "achievement_fashionista";           // own 5 vehicle skins total
        public const string Tuner = "achievement_tuner";                       // own 3 cosmetic attachments
        public const string Trailblazer = "achievement_trailblazer";           // own 3 booster skins

        // Monetization / misc
        public const string FirstPurchase = "achievement_first_purchase";      // first real-money purchase
        public const string WallHugger = "achievement_wall_hugger";            // 10 lifetime fence screeches (no crash)
    }
}
