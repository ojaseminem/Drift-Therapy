# Drift Therapy — Game State Analysis

> Snapshot: 2026-06-25 · Unity `6000.3.7f1` · URP · `bundleVersion 0.1.0` · TurtleGameWorks
> Source of truth: live project scan (Assets, scripts, scenes, settings) cross-checked against `_AI/` notes and the GDD.

---

## 1. Where the project actually is

Drift Therapy is a **portrait mobile hypercasual endless drift racer**. The
build target is Android/iOS, the design is locked in the GDD, and the project is
mid-way through its **first playable vertical slice**. The driving and road
systems are real and reasonably sophisticated; the *game* around them (traffic,
scoring, run-state, UI, monetization) is mostly not built yet.

Bottom line: **strong engine-feel foundation, missing game loop closure.**

---

## 2. What is implemented (verified in code)

**Driving / feel**
- `HyperDriftCarController.cs` — endless-drift controller layered on ACC_Lite's
  `CarController` physics. Holds near-constant forward speed (target ~95 kph),
  converts steer input into drift, and dynamically re-tunes front/rear wheel grip
  while drifting (`rearSideGripInDrift 0.62`, etc.). This is a real, tuned first pass.
- `DriftInputSystemReader.cs` — Input System reader; touch drag for steering,
  `A`/`D` on desktop, tap/hold drift. Legacy ACC `UserControl` disabled in the new scene.
- `DriftFollowCamera.cs` — semi top-down / over-shoulder follow camera.

**Procedural road (more advanced than the notes imply)**
- `RoadCurveGenerator.cs` — **section-based** curve generation (Straight →
  GentleCurve → MediumCurve → TightCurve → Chicanes) with banking. Deliberately
  avoids per-node jitter so players can read and set up drifts. This is the
  standout system.
- `EndlessTrackManager.cs` — pooled segment streaming (prewarm 14, pool 26,
  spawn-ahead 220m, recycle-behind 80m).
- `ProceduralRoadMesher.cs`, `SplineRoadBuilder.cs`, `RoadSegmentPool.cs`,
  `RoadVisuals.cs` — meshing, spline build, pooling, visuals.

**Environment**
- `RoadBiomeManager.cs` + `BiomeData.cs` — biome system that cross-fades
  materials, fog, sky, and post-processing by distance, updating live segments
  via `MaterialPropertyBlock` (no GC). **Code exists; no `BiomeData` assets are
  authored yet** (`Assets/Data/Biomes` is empty).

**Scene & infra**
- `Assets/Scenes/DriftEndless.unity` is the working scene (`PlayerCar`,
  `RoadTemplate`, `TrackManager`, `DriftCamera`, `FXController`).
- ACC_Lite imported as the physics/FX/audio base (WheelColliders, smoke, skid
  trails, body tilt, car sound). `SunLineGTE_Drift` prefab is the drift-tuned base.
- `com.unity.purchasing` (IAP) installed; `BillingMode.json` present. Analytics
  module present. Existing Unity MCP bridge (CoderGamester, port 8090) confirmed
  working; CoplayDev bridge now added.

---

## 3. What is missing vs the GDD

| GDD system | Status | Note |
|---|---|---|
| Constant forward motion | ✅ Done | In `HyperDriftCarController` |
| Analog steering + tap/hold drift | ✅ Done | Input System wired |
| Endless procedural road | ✅ Done | Strong section-based generator |
| Biome transitions | 🟡 Code only | No `BiomeData` assets authored |
| **Traffic (cars/trucks, rising density)** | ❌ Missing | No spawner, no traffic AI, no pooling |
| **Collision = run ends** | ❌ Missing | No run-state / fail handling |
| **Scoring (distance / drift combo / near-miss)** | ❌ Missing | No score model, no near-miss detection |
| **HUD (score, distance, multiplier)** | ❌ Missing | No in-game UI |
| **End screen (final, best, instant restart)** | ❌ Missing | No flow |
| **Run lifecycle (start/fail/restart/revive)** | ❌ Missing | Core loop not closed |
| Difficulty scaling (speed/density/curvature) | 🟡 Partial | Curvature ramps exist; speed/density scaling not driven |
| Audio (drift intensity, near-miss, combo) | 🟡 Partial | ACC sounds exist; not wired to game events |
| Monetization (interstitial, rewarded revive, IAP) | ❌ Missing | Purchasing installed but no ads SDK, no flow |
| Persistence (best score, remove-ads) | ❌ Missing | No save layer |

---

## 4. Critical gap: the loop isn't closed

The single most important fact: **a player cannot currently lose, score, or
restart.** Everything in "What is implemented" supports *driving*, but the
hypercasual hook — risk, reward, and the instant retry — does not exist yet. The
shortest path to a testable game is closing this loop (traffic → near-miss/score
→ crash → restart), not adding more environment polish.

---

## 5. Risks & technical debt

- **Stale internal notes.** `_AI/CURRENT_STATE.md` points scripts at
  `Assets/_Project/Scripts/...`; the real path is `Assets/Scripts/...`. Notes
  also undersell the road/biome systems. Treat code as truth; refresh the notes.
- **Two MCP bridges installed.** CoderGamester + CoplayDev. Different servers,
  different ports — fine, but duplicate tools can confuse the assistant. Pick a
  primary (see `UNITY_MCP_COPLAY.md`).
- **`_Recovery/` scenes** (`0.unity`, `0 (1).unity`) suggest a prior crash/recovery.
  Confirm `DriftEndless.unity` is the canonical scene and clean up recovery cruft.
- **ACC_Lite is a racing demo, not a runner.** Gear/RPM logic is dead weight for
  this design; keep physics + FX, prune the rest to avoid confusion.
- **Touch feel is unvalidated on device.** The whole pitch rests on steering
  feeling immediate, not floaty. This needs real-device testing early.
- **No analytics/telemetry events** defined yet — needed before soft launch to
  read retention and tuning.
- **Biome data unauthored** — the system will look empty until `BiomeData`
  assets exist.

---

## 6. Recommended immediate focus (the next two weeks)

1. **Close the core loop:** traffic spawner (pooled) → collision → run-state
   (start/fail/restart) → instant retry.
2. **Add scoring:** distance base + drift combo multiplier + near-miss detection,
   surfaced in a minimal HUD.
3. **Device test** the touch steering and constant-speed-with-drift feel; tune.
4. **Author 1–2 BiomeData assets** so the environment reads as intentional.
5. Refresh `_AI/CURRENT_STATE.md` and `NEXT_STEPS.md` to match reality.

Detailed sequencing lives in `FEATURES_PLAN.md` and `RELEASE_PLAN.md`.
