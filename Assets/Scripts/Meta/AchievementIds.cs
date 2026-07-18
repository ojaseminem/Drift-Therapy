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
    }
}
