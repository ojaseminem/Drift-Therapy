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
  `Assets/Scripts/Meta/AchievementIds.cs`. 10 achievements total:
  - 5 mission-linked (unlocked via `GameApp.ClaimMission`, ids already set on
    the 5 `Cumulative`-scope `MissionDef` assets in `Assets/Data/Missions/`):
    Road Warrior, Near Miss Master, Coin Collector, Dedicated Drifter, Combo Chaser.
  - 5 standalone milestones, unlocked directly at their trigger point: First
    Drift (first run ever), Speed Demon (2,000m in one run —
    `GameController.HandleRunFailed`), Garage Collector (own 5+ vehicles),
    Full House (own every vehicle), Supporter (Remove Ads purchased — both
    in `GameApp.cs`).
- **IAP is real** (unchanged): `Assets/Scripts/Services/IAPService.cs`, Unity
  IAP classic API, `remove_ads` non-consumable + all `CurrencyPackDef` packs.
- **Consent is still a stub** (`IConsentService`/`StubConsentService`) —
  needs a real CMP (e.g. Google UMP) before Ads.Init() should really gate on
  it. Not blocking today since LevelPlay itself is also unconfigured.
- Android `applicationId`: `com.turtlegameworks.drifttherapy`.

## 2. Call-site table — final state

| # | File : Method | State |
|---|---|---|
| 1 | `PlatformServices.cs : Init()` | ✅ Real `GooglePlayGamesService`/`LevelPlayAdsService` constructed; `SignIn(null)` called on boot |
| 2 | `GameController.cs : HandleRunFailed()` | ✅ Submits to both `LeaderboardProvider.Current` (local) and `PlatformServices.PlayGames.SubmitScore("top_runs", ...)` (online); also unlocks First Drift / Speed Demon |
| 3 | `GameApp.cs : ClaimMission()` | ✅ Routes through `TryUnlockAchievement(def.achievementId)` |
| 4 | `GameApp.cs : TryBuy()` / `SetRemoveAdsOwned()` | ✅ New standalone achievement unlocks (Garage Collector, Full House, Supporter) |
| 5 | `LeaderboardPopup.cs : Start()` | ✅ Unchanged code, now functions for real — "View Global" shows once `IsAvailable` (already true) |
| 6 | `GameController.cs : HandleReviveRequested()` | ⏸ **Untouched on purpose** — still an unconditional free revive, no `Ads.ShowRewarded` call. See §4, Phase 4. |
| 7 | `GameHudUI.cs` end-panel home/restart handlers | ⏸ **Untouched on purpose** — no `Ads.ShowInterstitial` call anywhere yet. See §4, Phase 4. |
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
   match exactly — already hardcoded at every call site above) and the 10
   achievements listed in §1 using the exact id strings in `AchievementIds.cs`.
4. Rebuild and test sign-in on a real device/Play-Store-track build — GPGS
   does not function in the Unity Editor or on non-Android platforms (both
   `GooglePlayGamesService` methods and `LeaderboardPopup`'s button already
   handle that gracefully; there's nothing more to change here).

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

## 4. Ad placement — deliberately not wired yet

Two real gameplay hooks exist but are intentionally left alone for now:
`GameController.HandleReviveRequested()` (still an unconditional free revive)
and `GameHudUI.cs`'s end-panel Home/Restart buttons (no interstitial call).
Wiring these is "Phase 4" — a monetization/UX decision, not a technical one.
See the ad-placement strategy delivered separately in-session for the
recommended approach (interstitial frequency-capping, rewarded revive with a
non-punishing failure fallback per §5 below) before flipping these on.

## 5. Do not regress

- `LevelPlayAdsService.ShowRewarded`'s `onFailed` callback fires if the ad
  isn't ready OR if the player closes it without completing — whoever wires
  `HandleReviveRequested` for real must design an explicit non-punishing
  fallback (e.g. still allow revive, or offer a coins-cost revive instead) per
  the original "do not regress" note — don't strand the player on ad-load
  failure.
