using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Real LevelPlay (Unity Ads Mediation, formerly ironSource) backed implementation
    /// of <see cref="IAdsService"/>. Replaces <see cref="StubAdsService"/> in
    /// <see cref="PlatformServices"/>.
    ///
    /// Fully initializes and preloads interstitial/rewarded ads so the SDK is "hot" and
    /// ready — but nothing in gameplay code calls <see cref="ShowInterstitial"/> or
    /// <see cref="ShowRewarded"/> yet (by design; see the ad-placement report). This
    /// class is the plumbing, not the decision of where ads appear.
    ///
    /// Reads its App Key / ad unit ids from Assets/Resources/LevelPlayConfig.asset (see
    /// <see cref="LevelPlayConfig"/>) — stays inert (logs, no-ops) until that asset has
    /// real values from your LevelPlay dashboard account, which only you can generate.
    /// </summary>
    public class LevelPlayAdsService : IAdsService
    {
        LevelPlayConfig config;
        LevelPlayInterstitialAd interstitialAd;
        LevelPlayRewardedAd rewardedAd;
        bool initStarted;

        Action pendingInterstitialClosed;
        Action pendingRewardedGranted;
        Action pendingRewardedFailed;
        bool rewardedPending;

        public void Init()
        {
            if (initStarted) return;
            initStarted = true;

            config = Resources.Load<LevelPlayConfig>("LevelPlayConfig");
            if (config == null || string.IsNullOrEmpty(config.androidAppKey))
            {
                Debug.LogWarning("[LevelPlay] No App Key configured (Assets/Resources/LevelPlayConfig.asset) — " +
                                  "ads stay uninitialized until one is set from your LevelPlay dashboard.");
                return;
            }

            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed += HandleInitFailed;
            LevelPlay.Init(config.androidAppKey);
        }

        void HandleInitSuccess(LevelPlayConfiguration cfg)
        {
            Debug.Log("[LevelPlay] Init succeeded.");
            SetUpInterstitial();
            SetUpRewarded();
        }

        void HandleInitFailed(LevelPlayInitError error)
        {
            Debug.LogWarning($"[LevelPlay] Init failed: {error}");
        }

        void SetUpInterstitial()
        {
            if (string.IsNullOrEmpty(config.interstitialAdUnitId)) return;

            interstitialAd = new LevelPlayInterstitialAd(config.interstitialAdUnitId, new LevelPlayInterstitialAd.Config.Builder().Build());
            interstitialAd.OnAdLoadFailed += err => Debug.LogWarning($"[LevelPlay] Interstitial load failed: {err}");
            interstitialAd.OnAdDisplayFailed += (info, err) => Debug.LogWarning($"[LevelPlay] Interstitial display failed: {err}");
            interstitialAd.OnAdClosed += _ =>
            {
                var cb = pendingInterstitialClosed;
                pendingInterstitialClosed = null;
                interstitialAd.LoadAd(); // preload the next one immediately
                cb?.Invoke();
            };
            interstitialAd.LoadAd();
        }

        void SetUpRewarded()
        {
            if (string.IsNullOrEmpty(config.rewardedAdUnitId)) return;

            rewardedAd = new LevelPlayRewardedAd(config.rewardedAdUnitId, new LevelPlayRewardedAd.Config.Builder().Build());
            rewardedAd.OnAdLoadFailed += err => Debug.LogWarning($"[LevelPlay] Rewarded load failed: {err}");
            rewardedAd.OnAdDisplayFailed += (info, err) =>
            {
                Debug.LogWarning($"[LevelPlay] Rewarded display failed: {err}");
                FinishRewarded(granted: false);
            };
            rewardedAd.OnAdRewarded += (info, reward) => FinishRewarded(granted: true);
            // Closed without OnAdRewarded firing first (player skipped/backed out) = not granted.
            rewardedAd.OnAdClosed += _ => FinishRewarded(granted: false);
            rewardedAd.LoadAd();
        }

        void FinishRewarded(bool granted)
        {
            // OnAdClosed always fires after OnAdRewarded — guard so a granted reward
            // doesn't get double-processed (and double-preloaded) when OnAdClosed follows.
            if (!rewardedPending) return;
            rewardedPending = false;

            var grantedCb = pendingRewardedGranted;
            var failedCb = pendingRewardedFailed;
            pendingRewardedGranted = null;
            pendingRewardedFailed = null;
            rewardedAd.LoadAd(); // preload the next one immediately

            if (granted) grantedCb?.Invoke();
            else failedCb?.Invoke();
        }

        public bool IsInterstitialReady => interstitialAd != null && interstitialAd.IsAdReady();

        public void ShowInterstitial(Action onClosed)
        {
            if (interstitialAd == null || !interstitialAd.IsAdReady())
            {
                onClosed?.Invoke();
                return;
            }

            pendingInterstitialClosed = onClosed;
            interstitialAd.ShowAd(null);
        }

        public bool IsRewardedReady => rewardedAd != null && rewardedAd.IsAdReady();

        public void ShowRewarded(Action onRewarded, Action onFailed)
        {
            if (rewardedAd == null || !rewardedAd.IsAdReady())
            {
                onFailed?.Invoke();
                return;
            }

            rewardedPending = true;
            pendingRewardedGranted = onRewarded;
            pendingRewardedFailed = onFailed;
            rewardedAd.ShowAd(null);
        }
    }
}
