using System;
using System.Collections.Generic;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Inert online-leaderboard placeholder. Not wired into
    /// <see cref="LeaderboardProvider"/> yet (it still points at
    /// <see cref="LocalLeaderboardService"/>) — this exists so the seam
    /// compiles and is ready to combine with/replace the local list once
    /// Play Games lands.
    ///
    /// TODO(PlayGames): back this with
    /// PlatformServices.PlayGames.SubmitScore/ShowLeaderboardUI once GPGS is
    /// installed, then flip LeaderboardProvider.Current. Keep
    /// Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md in sync with this file.
    /// </summary>
    public class StubPlayGamesLeaderboardService : ILeaderboardService
    {
        public bool IsOnline => false;

        public void Submit(float distanceMeters, string vehicleId)
        {
            Debug.Log("[PlayGames][stub] Leaderboard Submit — no-op.");
        }

        public IReadOnlyList<LeaderboardEntry> GetTop(int count) => Array.Empty<LeaderboardEntry>();
    }
}
