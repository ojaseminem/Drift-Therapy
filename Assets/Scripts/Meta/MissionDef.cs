using UnityEngine;

namespace DriftTherapy
{
    /// <summary>How a mission's progress is scoped.</summary>
    public enum MissionScope
    {
        /// <summary>Lifetime total, shown in MISSIONS. Never resets.</summary>
        Cumulative,
        /// <summary>One active at a time, rolled from a pool daily, shown in MISSIONS.</summary>
        Daily,
        /// <summary>Evaluated against a single run's peak value, shown in TRIALS.</summary>
        PerRunTrial,
    }

    /// <summary>What a mission tracks progress against.</summary>
    public enum MissionMetric
    {
        DistanceMeters,
        NearMisses,
        DriftCombo,
        CoinsEarned,
        RunsCompleted,
    }

    /// <summary>
    /// A mission, daily challenge, or per-run trial definition. Progress and
    /// claim state live in <see cref="GameApp"/>'s <c>PlayerData</c>, keyed by
    /// <see cref="id"/> — this asset only describes the target and reward.
    ///
    /// Create via: Assets → Create → Drift Therapy → Mission
    /// </summary>
    [CreateAssetMenu(fileName = "Mission", menuName = "Drift Therapy/Mission")]
    public class MissionDef : ScriptableObject
    {
        [Tooltip("Stable id stored in the save file. Keep unique and unchanging.")]
        public string id = "mission_id";
        public string title = "Mission";
        public string description = "Do the thing.";

        public MissionScope scope = MissionScope.Cumulative;
        public MissionMetric metric = MissionMetric.DistanceMeters;

        [Tooltip("Progress value needed to complete this mission.")]
        public int targetValue = 100;

        public int rewardCoins = 0;
        public int rewardGems = 0;
        public int rewardXp = 0;

        [Tooltip("Optional Play Games achievement id unlocked on claim. Empty = none.")]
        public string achievementId = "";
    }
}
