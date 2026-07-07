# Progress Log

## 2026-07-08 — Meta progression, leaderboard, DOTween juice, Phase 1/2 roadmap items

Executed `LEVEL_UP_GDD.md`'s roadmap: meta progression + leaderboard + Play
Games/Ads scaffolding (pulled forward), then Phase 1 core-gameplay items and
the Phase 2 UI items called out in that GDD. Verified live via Unity MCP
(play mode, screenshots, `execute_code` signal simulation, EditMode tests) —
not just read-and-hope.

**Meta progression / leaderboard / platform seams:**
- `MissionDef`/`GameApp` mission-catalog state, `MissionTracker`, real
  `MissionsPopup`/`TrialsPopup` (daily challenge + streak, cumulative
  missions, per-run trials — 13 assets authored via `MissionCatalogBuilder`
  and assigned to `GameApp.missions`).
- Local leaderboard (`ILeaderboardService`/`LocalLeaderboardService`/
  `LeaderboardProvider`), ranked by distance meters, `LeaderboardPopup` —
  seam ready to swap to Play Games in Phase 3.
- `IPlayGamesService`/`IAdsService`/`PlatformServices` as inert, zero-SDK-
  dependency stubs; every future call site TODO-commented and tracked in
  `Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md`.
- DOTween Pro game-feel pass (`UiJuice` shared helper, `CoinFlyEffect` pool,
  popup open/close, end-screen reveal, punches, bar fills) — all on unscaled
  time (the game sets `Time.timeScale = 0` on pause/fail, which the first
  draft of this work got wrong for the boost bar — fixed to snap instead of
  tween, since `BoostChanged` fires every frame during a drift).

**Phase 1 — core gameplay:**
- Combo tiers (`ScoreSystem.ComboTier`: Drift/Chain/Inferno/Legend) —
  presentation only, shown in the HUD multiplier text with tier color.
- Near-miss chaining: consecutive near-misses within a 3s window escalate
  the bonus (`ScoreSystem.RegisterNearMiss(atTime)`); `GameSignals.NearMiss`
  now carries the chain count so the HUD can show "NEAR MISS x3!".
- Traffic fairness audit: `TrafficDirector`'s lane-fairness algorithm
  reviewed and found sound (no changes needed). Found and removed
  `NearMissDetector.cs` — dead code, explicitly superseded by `TrafficSensor`
  per its own doc comment, zero references anywhere.
- Biome content: already fully authored (3 biomes, distinct materials/
  skyboxes/fog, staggered thresholds) from earlier work — nothing to add.
- Camera feedback: `DriftFollowCamera` now applies a combo-tier FOV boost +
  directional tilt. Tire-smoke/skid intensity scaling was **deliberately
  skipped** — it lives inside the vendored ACC_Lite wheel/FX plugin, and
  touching third-party physics/VFX internals without a live device pass to
  verify felt too risky for this token-optimized session.

**Phase 2 — UI:**
- `SceneLoader`: fade-to-black → async load → fade-in, replacing the old
  hard-cut `SceneManager.LoadScene`. `SceneFlow`'s public API is unchanged.
- `SafeAreaFitter` + a `UIBuilder.NewScreen()` change so every screen's
  content parents under a safe-area child automatically — zero changes
  needed at any other UIBuilder call site.

**Phase 3/4 (Play Games / Ads):** confirmed nothing further is possible
without Play Console access / an ads-network decision — both are fully
scaffolded and documented, waiting on external input.

**Bugs caught by actually running this in the editor (not just reading code):**
- `PopupHandler.Open()` killed an outgoing popup's fade tween using the
  wrong key (the GameObject instead of its CanvasGroup) — `Destroy()` ran
  while DOTween was still mid-tween. Fixed by tracking the CanvasGroup
  reference explicitly.
- Broke `ScoreSystemTests` by changing `RegisterNearMiss()`'s signature —
  fixed the two existing call sites and added coverage for the new chaining/
  tier behavior (25/25 EditMode tests pass).

**Still open (needs external input, not more coding):**
- Play Console app + OAuth client (Play Games Services).
- Ad mediation network decision (AdMob vs LevelPlay vs AppLovin MAX).
- A real device/editor profiler pass before shipping — flagged, not silently
  skipped, per this session's token-optimization instruction.

## 2026-06-26 — P0 core-loop implementation (best-wins first)

Implemented the highest-leverage slice of `FEATURES_PLAN.md` (P0: close the core
loop) plus the UI foundation. Built as committed source on branch `dev-roads`.
The Unity MCP bridge was **offline**, so this is **unverified-in-editor source** —
it compiles against the documented APIs but must be opened in Unity to confirm
compilation, generate `.meta` files, and wire the scene.

### What shipped (commits on `dev-roads`)

1. `docs:` plans, state analysis, Unity MCP setup notes.
2. `feat(gameplay): P0 core` — `RunStateMachine`, `ScoreSystem`, traffic
   pool/spawner/agent + EditMode tests (pre-existing scaffolding, committed).
3. `feat(ui): vendor design system` — `Assets/DesignSystem/` (UI Toolkit, MIT).
4. `feat(gameplay): GameSignals` — decoupled static event hub (gameplay ⇄ UI).
5. `feat(gameplay): scene glue` — `GameController`, `DifficultyDirector`,
   `PlayerCollisionDetector`, `NearMissDetector`, `SaveService`.
6. `feat(ui): screens` — HUD / EndRun / Pause UXML+USS + presenters + `UIRoot`.

### How it fits together

`GameController` owns `RunStateMachine` + `ScoreSystem`. Each frame while running
it accrues distance, builds/breaks the drift combo from `HyperDriftCarController.
DriftActive`, and persists best score via `SaveService`. Collisions
(`PlayerCollisionDetector`) fail the run; close passes (`NearMissDetector`) score
near-misses. Everything is broadcast through `GameSignals`; the UI Toolkit
presenters subscribe to update the HUD and end/pause screens, and raise intent
signals (restart/revive/pause) back to the controller. `DifficultyDirector` ramps
`RoadSegmentPool.DifficultyT` over distance.

### Pending — requires the live Unity bridge (see UNITY_MCP_COPLAY.md)

These cannot be done headlessly and are the next session's first tasks:

- [ ] Open Unity; confirm clean compile; commit the generated `.meta` files.
- [ ] **Add a Tests assembly definition** so EditMode tests compile/run
      (pure C# classes would need their own asmdef to be referenced — currently
      everything is in Assembly-CSharp; assembly restructuring was deliberately
      *not* done blind). Then run tests via MCP `run_tests`.
- [ ] Wire `DriftEndless.unity` per `Assets/Scripts/Game/INTEGRATION_GAMEPLAY.md`
      and `Assets/UI/INTEGRATION_UI.md` (GameController refs; player detectors +
      trigger collider; traffic layer/tag; PanelSettings + 3 UIDocuments).
- [ ] Author traffic prefab + assign to `TrafficPool.agentPrefab`.
- [ ] Author 1–2 `BiomeData` assets (empty today).
- [ ] Play-test the loop on device; tune touch feel.

### Known coordination contracts (don't break)

- Gameplay must raise the final `ScoreChanged` **before** `RunFailed`, then
  `ReviveOffered` separately — `EndRunPresenter` relies on this ordering.
- Near-miss trigger collider must be separate from the car's solid collider, and
  both must collide with traffic in the Physics matrix.
- New scripts have **no `.meta` yet** — Unity generates them on first import;
  commit them afterward.

### Note on git

This session's commits were written via a sandboxed git metadata dir because the
project mount blocks `unlink` (git couldn't clear its own lock files in place).
The real repo's `.git` had stale `index.lock`/`HEAD.lock` left from the first
commit — **delete `.git/index.lock` and `.git/HEAD.lock` on Windows** if a future
in-place `git` command complains about a running process.
