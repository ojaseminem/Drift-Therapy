namespace DriftTherapy
{
    /// <summary>
    /// Static bootstrap exposing the platform-service seams
    /// (<see cref="PlayGames"/>, <see cref="Ads"/>, <see cref="Leaderboard"/>).
    /// Defaults to inert stubs so the game compiles and behaves identically
    /// with zero external SDKs installed. Swapping a stub for a real
    /// implementation (once packages are imported) is a one-line change here.
    ///
    /// TODO(PlayGames/Ads): see Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md for
    /// the full swap-in checklist. Keep that doc in sync with this file.
    /// </summary>
    public static class PlatformServices
    {
        public static IPlayGamesService PlayGames { get; private set; } = new StubPlayGamesService();
        public static IAdsService Ads { get; private set; } = new StubAdsService();

        static bool initialized;

        /// <summary>Idempotent — safe to call from GameApp.Awake() every load.</summary>
        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            Ads.Init();
            // TODO(PlayGames): sign in on boot once GPGS is installed.
            // Keep Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md in sync with this call site.
            PlayGames.SignIn(null);
        }
    }
}
