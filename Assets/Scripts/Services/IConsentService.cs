using System;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Seam for ad consent (GDPR/CCPA CMP, iOS ATT). No CMP SDK is chosen yet
    /// (depends on the ads network decision — see
    /// Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md). The stub defaults to
    /// non-personalized/unresolved so nothing here silently claims consent
    /// that was never actually granted.
    ///
    /// TODO(Ads): replace with a real CMP-backed implementation (e.g. Google
    /// UMP if AdMob is chosen) before any live ad request ships. Keep
    /// Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md in sync with this file.
    /// </summary>
    public interface IConsentService
    {
        bool IsConsentResolved { get; }
        bool CanRequestPersonalizedAds { get; }
        void RequestConsent(Action onResolved);
    }

    public class StubConsentService : IConsentService
    {
        public bool IsConsentResolved => false;
        public bool CanRequestPersonalizedAds => false;

        public void RequestConsent(Action onResolved)
        {
            Debug.Log("[Consent][stub] RequestConsent — no CMP installed; treating as unresolved/non-personalized.");
            onResolved?.Invoke();
        }
    }
}
