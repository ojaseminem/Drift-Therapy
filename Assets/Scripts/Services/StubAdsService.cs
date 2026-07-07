using System;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Inert placeholder for <see cref="IAdsService"/>. <see cref="ShowRewarded"/>
    /// immediately succeeds so the existing free-revive flow in
    /// <c>GameController.HandleReviveRequested</c> is unchanged until Phase 4
    /// deliberately wires a real ad network. Do not "improve" this stub to
    /// fail or delay — that would silently regress today's revive UX.
    ///
    /// TODO(Ads): replace with a real mediation-SDK-backed implementation
    /// (network TBD — see Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md). Keep
    /// that doc in sync with this file.
    /// </summary>
    public class StubAdsService : IAdsService
    {
        public void Init()
        {
            Debug.Log("[Ads][stub] Init — no ad network installed yet.");
        }

        public bool IsInterstitialReady => true;

        public void ShowInterstitial(Action onClosed)
        {
            Debug.Log("[Ads][stub] ShowInterstitial — no-op, closing immediately.");
            onClosed?.Invoke();
        }

        public bool IsRewardedReady => true;

        public void ShowRewarded(Action onRewarded, Action onFailed)
        {
            Debug.Log("[Ads][stub] ShowRewarded — auto-granting reward (preserves current free-revive UX).");
            onRewarded?.Invoke();
        }
    }
}
