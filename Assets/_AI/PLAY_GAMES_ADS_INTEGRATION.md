# Play Games + Ads Integration — Tracking Doc

> Single source of truth for wiring the real Google Play Games Services and
> ads mediation SDKs into the stub scaffolding already in the codebase.
> **Read this before doing any Play Games/Ads work — it exists so that work
> requires no fresh research.**
>
> **Standing rule:** any time a TODO call site listed below is edited, update
> its row in this table in the same change. Search the repo for
> `TODO(PlayGames` / `TODO(Ads` to find every site if this table ever drifts.

---

## 1. Current state (green field)

- No Play Games / GPGS package installed. No ads mediation SDK installed.
  Confirmed via repo-wide search — see `LEVEL_UP_GDD.md` build history.
- All game code talks to two interfaces only:
  `Assets/Scripts/Services/IPlayGamesService.cs` and
  `Assets/Scripts/Services/IAdsService.cs`.
- `Assets/Scripts/Services/PlatformServices.cs` is the single swap point:
  `PlatformServices.PlayGames` / `PlatformServices.Ads`, defaulted to
  `StubPlayGamesService` / `StubAdsService`.
- **Leaderboard** has its own seam:
  `Assets/Scripts/Meta/ILeaderboardService.cs` /
  `Assets/Scripts/Meta/LeaderboardProvider.cs`. `LeaderboardProvider.Current`
  is `LocalLeaderboardService` (device-local top 10, real and working today).
  `Assets/Scripts/Services/StubPlayGamesLeaderboardService.cs` exists but is
  **not** wired into `LeaderboardProvider` yet — that flip happens in step 3
  below.
- Android `applicationId`: `com.turtlegameworks.drifttherapy`
  (`Assets/Scripts/Editor/AndroidReleaseBuilder.cs`). App Bundle + upload
  keystore already configured — Play Console-ready.

## 2. Call-site table

| # | File : Method | Current stub behavior | Real call that replaces it | Status |
|---|---|---|---|---|
| 1 | `GameApp.cs : Awake()` (via `PlatformServices.Init()`) | Logs, does nothing | `Social.localUser.Authenticate(...)` / GPGS sign-in | Pending |
| 2 | `GameController.cs : HandleRunFailed()` | Local submit only (real, working) | Add `PlatformServices.PlayGames.SubmitScore("top_runs", ...)` alongside the existing local submit | Pending |
| 3 | `GameApp.cs : ClaimMission()` | Logs, does nothing (`def.achievementId` may be empty — no-op either way) | `PlatformServices.PlayGames.UnlockAchievement(def.achievementId)` (interface unchanged; only the underlying implementation needs swapping) | Pending |
| 4 | `LeaderboardPopup.cs : Start()` | "View Global" button hidden (`IsAvailable == false`) | Show button once `IsAvailable == true`; already calls `ShowLeaderboardUI("top_runs")` | Pending |
| 5 | `GameController.cs : HandleReviveRequested()` | Free revive, unchanged (no ad shown) | Gate behind `PlatformServices.Ads.ShowRewarded(onRewarded: () => runState.Revive()-path, onFailed: ...)` | Pending (Phase 4) |
| 6 | `GameHudUI.cs` end-panel home/restart handlers | No ad shown | Frequency-capped `PlatformServices.Ads.ShowInterstitial(...)` before `SceneFlow.GoToMenu()`/restart | Pending (Phase 4) |
| 7 | `PlatformServices.cs : Init()` | Calls `Ads.Init()` (stub, logs only) | Real mediation SDK init (App ID from step 4 below) | Pending |

## 3. What to paste in later

**Play Games Services:**
- Requires a Play Console app linked to `com.turtlegameworks.drifttherapy`
  and an OAuth client — needs the user's console access, not code.
- Install `com.google.play.games` (GPGS v2) via Package Manager / git URL.
- Paste in: the generated `GPGSIds`/`Constants.cs` (leaderboard id — use
  `"top_runs"` to match the call sites above — and any achievement ids
  matching `MissionDef.achievementId` values authored in
  `Assets/Data/Missions/`).
- Implement a `GooglePlayGamesService : IPlayGamesService` and assign it in
  `PlatformServices.PlayGames`'s initializer (replace `new
  StubPlayGamesService()`).
- Implement a `GooglePlayGamesLeaderboardService : ILeaderboardService`
  (or extend `StubPlayGamesLeaderboardService`) and flip
  `LeaderboardProvider.Current` once signed in.

**Ads (network still undecided — candidates in `MONETIZATION_PLAN.md`:
Unity LevelPlay/ironSource, AppLovin MAX, AdMob):**
- Install the chosen mediation SDK once picked.
- Paste in: the network's App ID into `AndroidManifest.xml` (via the
  package's Unity settings asset), and interstitial/rewarded ad unit IDs.
- Implement a real `IAdsService` and assign it in `PlatformServices.Ads`'s
  initializer (replace `new StubAdsService()`).
- **Test IDs for local sanity-checking before real ad units exist** — Google's
  official AdMob test units (safe regardless of final network, since most
  mediation SDKs honor Google's test creatives):
  - App ID: `ca-app-pub-3940256099942544~3347511713`
  - Banner: `ca-app-pub-3940256099942544/6300978111`
  - Interstitial: `ca-app-pub-3940256099942544/1033173712`
  - Rewarded: `ca-app-pub-3940256099942544/5224354917`

## 4. Do not regress

- `StubAdsService.ShowRewarded` auto-succeeds today so the free-revive flow
  works with no ad network installed. When wiring #5 above for real, make
  sure the real implementation's failure path still lets the player continue
  the run in some form the design accepts (don't silently strand a failed
  ad load with no fallback) — this is a design decision for whoever wires
  Phase 4, not something to guess at now.
