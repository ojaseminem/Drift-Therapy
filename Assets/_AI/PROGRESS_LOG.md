# Progress Log

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
