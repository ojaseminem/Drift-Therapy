using System;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Inert placeholder for <see cref="IPlayGamesService"/>. Every call
    /// no-ops (logs only) so the rest of the game behaves exactly as it does
    /// today with no Play Games package installed.
    ///
    /// TODO(PlayGames): replace with a real GPGS-backed implementation once
    /// the package + Play Console app/OAuth client exist. See
    /// Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md — keep it in sync with this file.
    /// </summary>
    public class StubPlayGamesService : IPlayGamesService
    {
        public bool IsAvailable => false;
        public bool IsSignedIn => false;

        public void SignIn(Action<bool> onDone)
        {
            Debug.Log("[PlayGames][stub] SignIn requested — no-op until GPGS is installed.");
            onDone?.Invoke(false);
        }

        public void SubmitScore(string leaderboardId, long score)
        {
            Debug.Log($"[PlayGames][stub] SubmitScore({leaderboardId}, {score}) — no-op.");
        }

        public void ShowLeaderboardUI(string leaderboardId)
        {
            Debug.Log($"[PlayGames][stub] ShowLeaderboardUI({leaderboardId}) — no-op.");
        }

        public void UnlockAchievement(string achievementId)
        {
            Debug.Log($"[PlayGames][stub] UnlockAchievement({achievementId}) — no-op.");
        }
    }
}
