namespace DriftTherapy
{
    /// <summary>
    /// Static bootstrap exposing the platform-service seams
    /// (<see cref="PlayGames"/>, <see cref="Ads"/>, <see cref="IAP"/>).
    /// PlayGames/Ads default to inert stubs so the game compiles and behaves
    /// identically with zero external SDKs installed — swapping a stub for a
    /// real implementation (once packages are imported) is a one-line change
    /// here. <see cref="IAP"/> is real (com.unity.purchasing is already
    /// installed and configured — see Assets/Resources/BillingMode.json).
    ///
    /// TODO(PlayGames/Ads): see Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md for
    /// the full swap-in checklist. Keep that doc in sync with this file.
    /// </summary>
    public static class PlatformServices
    {
        public static IPlayGamesService PlayGames { get; private set; } = new StubPlayGamesService();
        public static IAdsService Ads { get; private set; } = new StubAdsService();
        public static IIAPService IAP { get; private set; } = new IAPService();
        public static IConsentService Consent { get; private set; } = new StubConsentService();

        static bool initialized;

        /// <summary>Idempotent — safe to call from GameApp.Awake() every load.</summary>
        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            // Consent gate goes first in a real integration — request it before
            // any ad network initializes. TODO(Ads): once a CMP is wired, don't
            // call Ads.Init() until IsConsentResolved is true.
            Consent.RequestConsent(null);
            Ads.Init();
            IAP.Init();
            // TODO(PlayGames): sign in on boot once GPGS is installed.
            // Keep Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md in sync with this call site.
            PlayGames.SignIn(null);
        }
    }
}
