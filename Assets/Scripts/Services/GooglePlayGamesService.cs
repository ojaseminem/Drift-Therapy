using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Real Google Play Games Services (GPGS v2, com.google.play.games) backed
    /// implementation of <see cref="IPlayGamesService"/>. Replaces
    /// <see cref="StubPlayGamesService"/> in <see cref="PlatformServices"/>.
    ///
    /// GPGS only functions on an actual Android device/build signed with the
    /// keystore registered to the linked Play Console app — every real call
    /// here is a no-op off-Android (Editor Play Mode, other platforms) so the
    /// game keeps working exactly as before anywhere GPGS can't actually run.
    ///
    /// Requires one-time manual setup only the account owner can do (Play
    /// Console app + OAuth client, then Window ▶ Google Play Games ▶ Setup ▶
    /// Android Setup in the Editor) — see Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md.
    /// Until that setup is completed, Authenticate() will fail gracefully
    /// (onDone invoked with false) rather than crash.
    /// </summary>
    public class GooglePlayGamesService : IPlayGamesService
    {
        static bool IsSupportedPlatform => Application.platform == RuntimePlatform.Android;

        public bool IsAvailable => true;
        public bool IsSignedIn => IsSupportedPlatform && PlayGamesPlatform.Instance != null && PlayGamesPlatform.Instance.IsAuthenticated();

        public GooglePlayGamesService()
        {
            if (!IsSupportedPlatform) return;
            PlayGamesPlatform.Activate();
        }

        public void SignIn(Action<bool> onDone)
        {
            if (!IsSupportedPlatform)
            {
                onDone?.Invoke(false);
                return;
            }

            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                bool ok = status == SignInStatus.Success;
                if (!ok) Debug.Log($"[PlayGames] Sign-in did not complete: {status}");
                onDone?.Invoke(ok);
            });
        }

        public void SubmitScore(string leaderboardId, long score)
        {
            if (!IsSignedIn || string.IsNullOrEmpty(leaderboardId)) return;
            PlayGamesPlatform.Instance.ReportScore(score, leaderboardId, success =>
            {
                if (!success) Debug.LogWarning($"[PlayGames] SubmitScore failed for leaderboard '{leaderboardId}'.");
            });
        }

        public void ShowLeaderboardUI(string leaderboardId)
        {
            if (!IsSignedIn) return;
            if (string.IsNullOrEmpty(leaderboardId)) PlayGamesPlatform.Instance.ShowLeaderboardUI();
            else PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
        }

        public void UnlockAchievement(string achievementId)
        {
            if (!IsSignedIn || string.IsNullOrEmpty(achievementId)) return;
            PlayGamesPlatform.Instance.UnlockAchievement(achievementId, success =>
            {
                if (!success) Debug.LogWarning($"[PlayGames] UnlockAchievement failed for '{achievementId}'.");
            });
        }
    }
}
