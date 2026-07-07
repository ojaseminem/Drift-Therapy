using System;

namespace DriftTherapy
{
    /// <summary>
    /// Seam for Google Play Games Services (sign-in, leaderboards, achievements,
    /// cloud save). Shaped to mirror the real GPGS v2 API
    /// (<c>PlayGamesPlatform.Instance</c> / <c>Social.localUser</c>) so swapping
    /// <see cref="PlatformServices.PlayGames"/> from <see cref="StubPlayGamesService"/>
    /// to a real implementation is a single-file change.
    ///
    /// See Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md for the full integration
    /// checklist and test/placeholder IDs. Keep that doc in sync with this file.
    /// </summary>
    public interface IPlayGamesService
    {
        /// <summary>True once a real GPGS implementation is wired in (always false for the stub).</summary>
        bool IsAvailable { get; }
        bool IsSignedIn { get; }

        void SignIn(Action<bool> onDone);
        void SubmitScore(string leaderboardId, long score);
        void ShowLeaderboardUI(string leaderboardId);
        void UnlockAchievement(string achievementId);
    }
}
