namespace DriftTherapy
{
    /// <summary>
    /// Static bootstrap exposing the platform-service seams
    /// (<see cref="PlayGames"/>, <see cref="Ads"/>, <see cref="IAP"/>).
    /// <see cref="PlayGames"/> is real (Google Play Games Services v2,
    /// com.google.play.games), <see cref="Ads"/> is real (LevelPlay /
    /// Unity Ads Mediation, com.unity.services.levelplay), <see cref="IAP"/> is
    /// real (com.unity.purchasing — see Assets/Resources/BillingMode.json).
    /// Both PlayGames and Ads still require one-time account-specific setup
    /// only you can do (Play Console app/OAuth client; LevelPlay dashboard App
    /// Key) — until then they no-op safely rather than crash.
    ///
    /// See Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md for the setup checklist.
    /// </summary>
    public static class PlatformServices
    {
        public static IPlayGamesService PlayGames { get; private set; } = new GooglePlayGamesService();
        public static IAdsService Ads { get; private set; } = new LevelPlayAdsService();
        public static IIAPService IAP { get; private set; } = new IAPService();
        public static IConsentService Consent { get; private set; } = new StubConsentService();

        static bool initialized;

        /// <summary>Idempotent — safe to call from GameApp.Awake() every load.</summary>
        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            // Consent gate goes first in a real integration — request it before
            // any ad network initializes. TODO(Ads): once a real CMP is wired
            // (Consent is still a stub), don't call Ads.Init() until
            // IsConsentResolved is true.
            Consent.RequestConsent(null);
            Ads.Init();
            IAP.Init();
            PlayGames.SignIn(null);
        }
    }
}
