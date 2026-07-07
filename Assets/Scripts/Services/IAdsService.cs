using System;

namespace DriftTherapy
{
    /// <summary>
    /// Seam for the (not-yet-chosen) ads mediation SDK — interstitial +
    /// rewarded only, matching GDD/MONETIZATION_PLAN scope. See
    /// Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md for candidate networks and
    /// official Google test ad unit IDs. Keep that doc in sync with this file.
    /// </summary>
    public interface IAdsService
    {
        void Init();

        bool IsInterstitialReady { get; }
        /// <summary>Shows an interstitial if ready; <paramref name="onClosed"/> always fires, even if not shown.</summary>
        void ShowInterstitial(Action onClosed);

        bool IsRewardedReady { get; }
        /// <summary>Shows a rewarded ad; calls <paramref name="onRewarded"/> on completion or <paramref name="onFailed"/> otherwise.</summary>
        void ShowRewarded(Action onRewarded, Action onFailed);
    }
}
