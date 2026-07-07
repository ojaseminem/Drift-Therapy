using System.Collections.Generic;

namespace DriftTherapy
{
    /// <summary>
    /// Seam for ranked-run storage. <see cref="LocalLeaderboardService"/> is
    /// the real, working implementation today (device-local top 10); Phase 3
    /// swaps <see cref="LeaderboardProvider.Current"/> to an online/composite
    /// implementation once Play Games lands — callers never change.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>True once entries reflect a real online leaderboard (always false locally).</summary>
        bool IsOnline { get; }

        void Submit(float distanceMeters, string vehicleId);
        IReadOnlyList<LeaderboardEntry> GetTop(int count);
    }

    /// <summary>Static seam holding the active <see cref="ILeaderboardService"/>.</summary>
    public static class LeaderboardProvider
    {
        public static ILeaderboardService Current { get; set; } = new LocalLeaderboardService();
    }
}
