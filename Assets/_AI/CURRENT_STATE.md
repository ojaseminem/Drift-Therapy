# Current State

> Refreshed 2026-07-26 via Cowork audit (live scan of scenes/scripts/tests, cross-checked
> against PROGRESS_LOG.md). This supersedes the 2026-06 version of this file, which was
> written before the core loop closed and pointed at a stale script path.

## Status in one line
**Feature-complete first playable.** The full hypercasual loop is built, wired, and passing
tests. Remaining work is external setup (ads/Play Games), an audio pass, device QA, and —
per Ojas's current priority — **art**.

## Verified via live project scan
- 3 scenes in Build Settings: `MainMenu` (0), `DriftEndless` (1), `Garage` (2).
- 86 C# scripts across `Assets/Scripts/{Drift, Road, Environment, Biome, Traffic, Game,
  Meta, Services, UI, Editor, Utility}`.
- EditMode tests: **25/25 passing** (last run 2026-07-26).
- Console clean of errors; only expected warnings (LevelPlay has no App Key configured
  yet, IAP fake-store callback notices — both expected pre-launch, not bugs).

## What's implemented
**Core loop (closed)**
- `HyperDriftCarController` + `DriftInputSystemReader` + `DriftFollowCamera` — constant-speed
  drift driving, touch-drag steering, tap/hold drift, desktop A/D fallback.
- `EndlessTrackManager` / `RoadCurveGenerator` / `RoadSegmentPool` — pooled, section-based
  procedural road (Straight -> Gentle -> Medium -> Tight -> Chicane), no visible seams.
- `TrafficDirector` / `TrafficAgent` / `TrafficSensor` — traffic spawning with lane fairness,
  reviewed and sound.
- `PlayerCollisionDetector`, `RunStateMachine`, `RunState` — collision ends the run; run
  lifecycle (start/fail/restart/revive) fully wired.
- `ScoreSystem` — distance + drift combo tiers (Drift/Chain/Inferno/Legend) + near-miss
  chaining (escalating bonus within a 3s window).
- `GameHudUI`, end/pause screens — HUD shows score/distance/multiplier live.

## Meta / progression / monetization
- `GameApp`, `VehicleDef`, `PlayerVehicleSpawner` — modular vehicle system; Garage scene with
  3D carousel (`SnapCarousel`, `GarageVehicleDisplay`), purchase confirmation, gem currency.
- `MissionDef` / `MissionTracker` — daily challenge, cumulative missions, per-run trials (13
  authored assets).
- `LocalLeaderboardService` / `LeaderboardPopup` — ranked by distance; seam ready to swap to
  Play Games Services in Phase 3.
- `IAPService` — real Unity IAP (classic `IStoreListener`) wired for `remove_ads`
  non-consumable; verified in Editor Fake Store.
- `PlatformServices` (`IAdsService`, `IPlayGamesService`, `IConsentService`) — inert stubs,
  zero-SDK-dependency, every call site TODO-tagged in `PLAY_GAMES_ADS_INTEGRATION.md`.
  LevelPlay package is integrated but has no App Key yet (needs an ads network decision).
- `SettingsPopup` (audio/haptics toggles, restore purchases), `OnboardingPopup` (first-run
  only), `SceneLoader` (fade transitions), `SafeAreaFitter` (notch/safe-area correct).
- DOTween Pro game-feel pass across popups, HUD, combo tiers, camera FOV/tilt.

## Known gaps (not blocking a playable build)
- **BiomeData assets**: `RoadBiomeManager`/`BiomeData` system is fully coded but **no biome
  content is authored yet** (`Assets/Data/Biomes` is empty). This is now an **art** task, not
  a code task — see `ART_PLAN.md` section 3.
- **Ads network**: LevelPlay installed but unconfigured (no App Key) — needs Ojas to pick a
  mediation network and get an ads account set up.
- **Play Games Services**: stubbed, needs a Play Console app + OAuth client before it can go
  live.
- **Audio**: engine/skid sounds work via ACC_Lite; near-miss/combo/crash/UI SFX and adaptive
  music are not implemented yet (`AUDIO_PLAN.md`).
- **Device QA**: touch-feel and performance have only been validated in-editor / Device
  Simulator, not on a real phone yet (`QA_PLAN.md`).
- Two Unity MCP bridges are installed (CoderGamester + CoplayDev) — harmless but worth
  picking a primary eventually (`UNITY_MCP_COPLAY.md`).

## Where things live
- Gameplay: `Assets/Scripts/{Drift, Road, Traffic, Game, Environment, Biome}`
- Meta/UI/services: `Assets/Scripts/{Meta, UI, Services}`
- Scenes: `Assets/Scenes/{MainMenu, DriftEndless, Garage}.unity`
- Design docs (this folder): `Assets/_AI/`
- Vendor car physics/FX base: `Assets/ACC_Lite` (restyle for final art, don't extend logic)
