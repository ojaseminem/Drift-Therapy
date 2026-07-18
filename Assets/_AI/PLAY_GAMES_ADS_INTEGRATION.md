# Play Games + Ads Integration — Tracking Doc

> Single source of truth for the real Google Play Games Services and LevelPlay
> ads mediation SDK wiring. **Read this before touching either — it exists so
> that work requires no fresh research.**
>
> **Standing rule:** any time a row in the call-site table changes, update it
> in the same change.

---

## 1. Current state (both SDKs installed and wired to real services)

- **`com.google.play.games` (GPGS v2.1.0)** is imported (`Assets/GooglePlayGames/`).
  `PlatformServices.PlayGames` is `GooglePlayGamesService` (real), not a stub.
- **`com.unity.services.levelplay` (9.5.0)** is installed via Package Manager.
  `PlatformServices.Ads` is `LevelPlayAdsService` (real), not a stub.
- Both real services **no-op safely** until the one-time, account-specific
  setup below is completed by whoever owns the Play Console / LevelPlay
  dashboard accounts — neither can crash the game in the meantime.
- **Leaderboard** design (deliberately NOT changed): `LeaderboardProvider.Current`
  stays `LocalLeaderboardService` (device-local top 10, shown in-app) —
  `GameController.HandleRunFailed()` now ALSO calls
  `PlatformServices.PlayGames.SubmitScore("top_runs", ...)` alongside it. The
  "View Global" button in `LeaderboardPopup` opens Play Games' own native
  leaderboard UI (`ShowLeaderboardUI("top_runs")`) — there is no synchronous
  API to pull GPGS scores into our own list, so the two leaderboards
  (in-app-local vs. native-global) are intentionally separate, not merged.
  `StubPlayGamesLeaderboardService.cs` is unused/left in place as a documented
  dead-end for this reason.
- **Achievements**: `GameApp.TryUnlockAchievement(id)` is the single entry
  point (locally deduped via `PlayerData.unlockedAchievementIds`, then
  forwards to `PlatformServices.PlayGames.UnlockAchievement`). All ids live in
  `Assets/Scripts/Meta/AchievementIds.cs` — **26 achievements total**, listed
  in full in §3.
- **Ads are live** (no longer Phase 4 placeholders):
  - `GameController.HandleReviveRequested()` gates the revive behind
    `PlatformServices.Ads.ShowRewarded`, but only once `GameApp.AdsUnlocked`
    (see below) — both the ad-fail path and the not-yet-unlocked path fall
    through to a free revive (`DoRevive()`), so a bad ad load or a new
    player never gets stranded.
  - `GameHudUI.ProceedWithPossibleAd` gates the game-over screen's Retry/Home
    buttons behind `PlatformServices.Ads.ShowInterstitial` (only the game-over
    buttons — the pause menu's Home/Restart are untouched). If no ad is
    allowed/ready, a brief "LOADING..." overlay (`GameHudUI.loadingPanel`)
    plays instead so the tap always feels like it went somewhere.
  - `GameApp.AdsUnlocked` (`Data.totalRunsCompleted > 10`) is a hard gate in
    front of both — a player's first 10 completed runs never show any ad,
    interstitial or rewarded.
- **IAP is real** (unchanged): `Assets/Scripts/Services/IAPService.cs`, Unity
  IAP classic API, `remove_ads` non-consumable + all `CurrencyPackDef` packs.
- **Consent is still a stub** (`IConsentService`/`StubConsentService`) —
  needs a real CMP (e.g. Google UMP) before Ads.Init() should really gate on
  it. Not blocking today since LevelPlay itself is also unconfigured.
- **Settings** has a "Reset Progress" button (`SettingsPopup.resetDataButton`,
  tap-to-arm/second-tap-confirms) that calls `GameApp.ClearSavedData()` — handy
  for testing the 10-run ad-free grace period without waiting 10 real runs.
- Android `applicationId`: `com.turtlegameworks.drifttherapy`.

## 2. Call-site table — final state

| # | File : Method | State |
|---|---|---|
| 1 | `PlatformServices.cs : Init()` | ✅ Real `GooglePlayGamesService`/`LevelPlayAdsService` constructed; `SignIn(null)` called on boot |
| 2 | `GameController.cs : HandleRunFailed()` | ✅ Submits to both `LeaderboardProvider.Current` (local) and `PlatformServices.PlayGames.SubmitScore("top_runs", ...)` (online); also reports the 3 per-run trial metrics and unlocks several achievements |
| 3 | `GameApp.cs : ClaimMission()` | ✅ Routes through `TryUnlockAchievement(def.achievementId)` |
| 4 | `GameApp.cs` various | ✅ Achievement unlocks spread across `SubmitRun`, `TryBuy`, `TryBuySkin`, `TryBuyAttachment`, `TryBuyBooster`, `AddCoins`, `AddGems`, `GrantCurrencyPack`, `SetRemoveAdsOwned`, `IncrementFenceScreech` |
| 5 | `LeaderboardPopup.cs : Start()` | ✅ Unchanged code, now functions for real — "View Global" shows once `IsAvailable` (already true) |
| 6 | `GameController.cs : HandleReviveRequested()` | ✅ Gated behind `Ads.ShowRewarded` + `GameApp.AdsUnlocked`, non-punishing fallback to free revive |
| 7 | `GameHudUI.cs : ProceedWithPossibleAd()` | ✅ Game-over Retry/Home gated behind `Ads.ShowInterstitial` + `AdsUnlocked`, loading-screen fallback otherwise |
| 8 | `PlatformServices.cs : Init()` (Consent gate) | ⏳ Still `StubConsentService` — real CMP not yet chosen/wired |
| 9 | `IAPService.cs` | Working (classic Unity IAP API), v5 migration optional/not urgent |

## 3. Manual setup still required (account-specific — cannot be done from code)

**Google Play Games:**
1. Link a Play Console app to `com.turtlegameworks.drifttherapy`, create an
   OAuth 2.0 **Web application** client ID under that project's Google Cloud
   Console (Play Console ▶ Play Games Services ▶ Setup ▶ Configuration).
2. In the Unity Editor: **Window ▶ Google Play Games ▶ Setup ▶ Android
   Setup**, paste that Client ID (and the linked Project ID). This patches
   `Assets/Plugins/Android/AndroidManifest.xml` with the required meta-data
   and generates the resource files GPGS needs — cannot be done without step 1.
3. In the Play Console, create the leaderboard with id **`top_runs`** (must
   match exactly — already hardcoded at every call site above) and all 26
   achievements listed in §3a using the exact id strings in `AchievementIds.cs`.
4. Rebuild and test sign-in on a real device/Play-Store-track build — GPGS
   does not function in the Unity Editor or on non-Android platforms (both
   `GooglePlayGamesService` methods and `LeaderboardPopup`'s button already
   handle that gracefully; there's nothing more to change here).

### 3a. Full achievement list (create these 26 in the Play Console)

All ids are in `Assets/Scripts/Meta/AchievementIds.cs` — the string there is
final and must match exactly what you create in the Play Console.

| Id | Name (suggested) | Unlock condition |
|---|---|---|
| `achievement_road_warrior` | Road Warrior | 10,000m lifetime distance |
| `achievement_near_miss_master` | Near Miss Master | 100 near misses lifetime |
| `achievement_coin_collector` | Coin Collector | 5,000 coins earned lifetime |
| `achievement_dedicated_drifter` | Dedicated Drifter | 50 runs completed |
| `achievement_combo_chaser` | Combo Chaser | 300 combo, lifetime |
| `achievement_first_drift` | First Drift | Complete your first run |
| `achievement_speed_demon` | Speed Demon | 2,000m in a single run |
| `achievement_garage_collector` | Garage Collector | Own 5+ vehicles |
| `achievement_full_house` | Full House | Own every vehicle |
| `achievement_supporter` | Supporter | Purchase Remove Ads |
| `achievement_first_mile` | First Mile | 1,000m lifetime distance |
| `achievement_marathoner` | Marathoner | 50,000m lifetime distance |
| `achievement_road_legend` | Road Legend | 100,000m lifetime distance |
| `achievement_endless_horizon` | Endless Horizon | 5,000m in a single run |
| `achievement_drift_king` | Drift King | 20x combo in a single run |
| `achievement_daredevil` | Daredevil | 10 near misses in a single run |
| `achievement_coin_baron` | Coin Baron | Hold 10,000 coins at once |
| `achievement_gem_hunter` | Gem Hunter | Hold 100 gems at once |
| `achievement_veteran_drifter` | Veteran Drifter | 200 runs completed |
| `achievement_daily_devotee` | Daily Devotee | 7-day login streak |
| `achievement_streak_master` | Streak Master | 30-day login streak |
| `achievement_fashionista` | Fashionista | Own 5 vehicle skins total |
| `achievement_tuner` | Tuner | Own 3 cosmetic attachments |
| `achievement_trailblazer` | Trailblazer | Own 3 booster skins |
| `achievement_first_purchase` | Big Spender | Complete your first real-money purchase |
| `achievement_wall_hugger` | Wall Hugger | 10 lifetime fence screeches (no crash) |

**LevelPlay (Unity Ads Mediation):**
1. Create/verify your app in the [LevelPlay dashboard](https://platform.unity.com)
   and copy its **Android App Key**.
2. Open `Assets/Resources/LevelPlayConfig.asset` in the Inspector and paste
   the App Key into `Android App Key`. Also paste the **interstitial** and
   **rewarded** ad unit ids from the dashboard's Ad Units page into the
   matching fields — `LevelPlayAdsService` reads all three from this one asset.
3. Run **LevelPlay ▶ Network Manager** (top Unity menu, added by the
   package) and install whichever demand-side ad network adapters you've
   configured in the dashboard (AdMob, Meta Audience Network, etc.) — the
   base SDK ships with no adapters installed by default.
4. Once the App Key is set, `LevelPlayAdsService.Init()` will actually call
   `LevelPlay.Init(...)` and preload both ad types automatically — still no
   gameplay code shows them yet (see §4).

## 4. Ad placement — live

Both hooks are wired: `GameController.HandleReviveRequested()` (rewarded) and
`GameHudUI.ProceedWithPossibleAd()` on the game-over Retry/Home buttons
(interstitial). Both respect `GameApp.AdsUnlocked` (first 10 runs are always
ad-free) and both fall back gracefully rather than blocking the player when
no ad is ready. No banner is used — see the original ad-placement report
delivered in-session for the full reasoning.

## 5. Do not regress

- `LevelPlayAdsService.ShowRewarded`'s `onFailed` callback fires if the ad
  isn't ready OR if the player closes it without completing — whoever wires
  `HandleReviveRequested` for real must design an explicit non-punishing
  fallback (e.g. still allow revive, or offer a coins-cost revive instead) per
  the original "do not regress" note — don't strand the player on ad-load
  failure.
