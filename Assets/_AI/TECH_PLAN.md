# Drift Therapy — Technical Plan

> Architecture, performance, and engineering plan. Unity `6000.3.7f1`, URP,
> Android/iOS. Builds on existing systems in `Assets/Scripts/`.

---

## 1. Architecture overview

Keep the current clean split and grow it deliberately:

```
Assets/Scripts/
  Drift/   → player control, input, camera        (exists)
  Road/    → procedural road, meshing, pooling     (exists)
  Biome/   → biome data + transitions              (exists, needs assets)
  Game/    → run-state, scoring, difficulty (NEW)
  Traffic/ → spawner, pool, simple AI       (NEW)
  UI/      → UI Toolkit documents/presenters (NEW)
  Services/→ save, analytics, ads, IAP       (NEW)
```

**Principles**
- **Event-driven, low-coupling.** A central run-state machine raises events
  (run started/failed/restarted, near-miss, combo changed); UI/audio/analytics
  subscribe. Avoid `Update()`-polling across systems.
- **Pool everything that spawns.** Road already pools; traffic must too. Zero
  per-frame allocation in the hot loop.
- **Data-driven tuning.** Difficulty, scoring weights, and biomes as
  ScriptableObjects (the `BiomeData` pattern is the model to follow).
- **Reuse, don't reinvent.** `EndlessTrackManager` pooling → traffic pool;
  `RoadCurveGenerator` curvature ramp → folded into the difficulty director.

---

## 2. Core systems to build

1. **RunStateMachine** (`Game/`) — `Ready/Running/Failed/Reviving/Restarting`;
   owns lifecycle, raises events, resets pools and score on restart.
2. **ScoreSystem** (`Game/`) — distance base + drift-combo multiplier + near-miss
   bonus; reads drift state from `HyperDriftCarController`.
3. **DifficultyDirector** (`Game/`) — single source ramping speed, traffic density,
   curvature vs distance/time; data-tunable; subsumes existing curvature ramps.
4. **TrafficSpawner + TrafficPool + TrafficAgent** (`Traffic/`) — pooled spawn
   ahead / recycle behind on the curved road; archetypes; avoids unfair spawns.
5. **Collision/NearMiss** — collision → fail event; proximity-without-contact →
   near-miss event.
6. **HUD / EndScreen / Pause** (`UI/`) — Unity UI Toolkit, event-subscribed,
   portrait-first; see `UI_TOOLKIT_PLAN.md`.
7. **Services** — SaveService (best score, settings, remove-ads), AnalyticsService,
   AdsService, IAPService (Unity Purchasing already installed).

---

## 3. Performance plan (mobile-first)

- **Targets:** 60 fps mid-tier, 30 fps floor low-end; stable frame time over peak fps.
- **Allocation:** zero GC alloc in the run loop; pool segments, traffic, VFX,
  audio sources; cache components; use `MaterialPropertyBlock` (biome manager
  already does).
- **Rendering:** SRP Batcher on; GPU instancing for traffic/road; texture atlases
  per biome; LODs/culling for far geometry; tight particle budgets.
- **Physics:** WheelColliders (ACC_Lite) are the heaviest cost — keep wheel count
  and physics step sane; fixed timestep tuned for stable drift; traffic uses
  cheap kinematic movement, not full physics, where possible.
- **Profiling cadence:** profile on a real low/mid device at each milestone, not
  just in-editor. Watch frame time, GC, draw calls, physics, overdraw.

---

## 4. Build, tooling & MCP

- **Unity MCP:** two bridges installed (CoderGamester + CoplayDev). Pick one
  primary to avoid duplicate-tool confusion — see `UNITY_MCP_COPLAY.md`.
- **UI Toolkit:** all first-party game UI uses Unity UI Toolkit. Adopt
  `sinanata/unity-ui-document-design-system` as `Assets/DesignSystem`, keep
  Drift-specific UXML/USS under `Assets/UI/`, and drive screens through thin
  presenters in `Assets/Scripts/UI/`. See `UI_TOOLKIT_PLAN.md`.
- **Build profiles:** maintain validated Android + iOS profiles
  (`Assets/Settings/Build Profiles`); script CI builds when practical.
- **Source hygiene:** confirm `DriftEndless.unity` canonical; remove `_Recovery/`
  scenes; ensure `.gitignore` excludes `Library/`, `Temp/`, `obj/`, `Builds/`,
  `Logs/`. (Note: many `*.csproj` and `.sln` files are generated — keep ignored.)
- **Vendor isolation:** keep ACC_Lite untouched as a vendor folder; prune unused
  gear/RPM/racing-demo logic from the *game* code paths, not the package.

---

## 5. Data & persistence

- Local save (JSON/PlayerPrefs-backed service) for best score, settings, remove-ads.
- Cloud save optional post-launch.
- Analytics events defined before soft launch (run start/end, distance, score,
  death cause, revive, ad shown, IAP) — see `RELEASE_PLAN.md`/`MONETIZATION_PLAN.md`.

---

## 6. Testing

- Edit/Play-mode tests for scoring math, run-state transitions, pool integrity,
  and UI Toolkit document/presenter smoke tests (Unity Test Framework is
  installed). MCP bridge can run tests.
- See `QA_PLAN.md` for the full strategy and device matrix.

---

## 7. Technical risks

- **WheelCollider cost on low-end** — profile early; simplify if needed.
- **Procedural seams / pop-in** — validate spawn-ahead/recycle distances on device.
- **Two MCP bridges** — port/tool overlap; designate a primary.
- **Stale notes & recovery scenes** — reconcile `_AI/` notes with real code; clean
  up `_Recovery/`.
- **ACC_Lite coupling** — the new controller depends on `CarController`; keep that
  seam well-defined so physics can be tuned or swapped without rewriting gameplay.
