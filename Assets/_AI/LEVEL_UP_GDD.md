# Drift Therapy — "Beat Double Drift" GDD (Level-Up Pass)

> Companion to `Drift Therapy GDD.md`. That doc defines the base game (already
> matches Double Drift's shape). This doc defines **what we add on top** and
> **the order we build it in**. Base controls are locked — do not change them.

---

## 1. Baseline: what Double Drift (Voodoo) does

Double Drift is a minimal hypercasual endless drifter:
- Portrait, semi top-down, constant forward speed.
- One-touch drift (hold to drift/steer into the slide), no accel/brake.
- Endless curved road, light traffic to dodge, any hit = instant game over.
- Coin pickups mid-run, spent on a small garage of cosmetic cars.
- Score = distance, no real combo depth, no near-miss system, no biomes.
- Interstitial after run, rewarded revive, that's the entire loop.

It works because the controls feel great, but it is *shallow*: no scoring
depth, no visual variety, no reason to come back except "one more try."

## 2. Our current state (already ahead of the baseline)

Already implemented per `PROGRESS_LOG.md` / recent commits:
- Drift + steering controller (`HyperDriftCarController`), endless track
  (`EndlessTrackManager`), procedural curved road with biomes (`RoadBiomeManager`).
- Traffic v2: director, archetypes, lanes, fairness, follow-gap
  (`TrafficDirector`, `TrafficAgent`, `TrafficSensor`).
- Collectibles (`Collectible`, `CollectibleSpawner`), near-miss detection
  (`NearMissDetector`), scoring (`ScoreSystem`), difficulty director,
  run state machine, vehicle health/collision.
- Basic HUD, garage popup, main menu popup framework already scaffolded
  (`GameHudUI`, `GaragePopup`, `MainMenuUI`, `Popup`/`PopupHandler`).

**So the "shallow" gap vs. Double Drift is already closing on systems.** What's
missing is *depth of feel*, *meta hooks that pull players back*, and
*production polish* (UI scalability, notch safety, platform services, ads).

## 3. Ideas to make it better than Double Drift

Grouped by theme, not yet prioritized (roadmap in §4 does that):

**Scoring & moment-to-moment depth**
- Combo tiers with visible names/colors (e.g. Drift → Chain → Inferno) instead
  of a flat multiplier number.
- Near-miss chains (consecutive close passes escalate bonus, like Double
  Drift never has).
- Risk/reward lane choice: tighter gaps = bigger multiplier, telegraphed by
  road/traffic layout, not random.

**Meta progression (the retention lever Double Drift barely uses)**
- Persistent currency + garage with stat-neutral cosmetic cars (skins, trims,
  smoke color) — cosmetic only, ties into IAP per `MONETIZATION_PLAN.md`.
- Daily challenge / streak (log in → 1 modifier run, e.g. "no near-misses",
  "double traffic") for a bonus reward.
- Lightweight mission set ("drift 500m in one run", "3 near-misses in a run")
  surfaced on the end screen, not a wall of menus.
- Best-score ghost or simple leaderboard (local first, Play Games later).

**Environment & juice (visual differentiation)**
- Biome variety already scaffolded (`BiomeData`) — lean into it: distinct
  lighting/skybox/road dressing per biome, not just palette swaps.
- Speed-based camera FOV/tilt escalation, hit-stop on crash, screen shake on
  near-miss, tire smoke color tied to combo tier.
- Weather/time-of-day variation as a later biome axis (post-launch).

**Systemic fairness Double Drift gets wrong**
- Difficulty director already exists — keep tuning so difficulty ramps feel
  authored, not random; never spawn an unavoidable traffic gap (traffic
  "fairness" work already started, keep it as a hard acceptance bar).

**Production polish Double Drift doesn't need to worry about (we do, since
we're aiming higher):**
- Scalable UI (see §4 Phase 2) with anchor/pivot discipline for different
  aspect ratios and tablets.
- Notch/cutout-safe safe-area layout.
- Proper loading/transition screens instead of hard scene cuts.
- Google Play Games Services (sign-in, cloud save, leaderboards,
  achievements) — Double Drift has none of this depth.

---

## 4. Build roadmap (phase order as agreed)

Each phase ends with an explicit approval checkpoint before moving to the next.
Optimization is a standing priority in every phase — prefer data-driven /
pooled / batched approaches; any perf shortcut or hack gets called out and
approved before landing, not folded in silently.

### Phase 1 — Core gameplay improvements
- Combo tier system + near-miss chaining (scoring depth from §3).
- Difficulty/traffic fairness tuning pass (no unavoidable hits).
- Biome content pass: 2–3 authored `BiomeData` assets with distinct dressing.
- Feedback layer: tire smoke/skid/camera tilt/speed lines wired to combo state.
- Missions/daily-challenge data model (logic only; UI comes in Phase 2).

### Phase 2 — UI screens & popups
- Main menu, HUD, pause, end-screen/results, garage, settings, daily
  challenge, mission list — all built on the existing `Popup`/`PopupHandler`
  pattern for consistency.
- Loading/transition screen between scenes (mask hitches, no hard cuts).
- **Scalability contract:** every screen laid out with explicit anchors +
  pivots for multi-aspect-ratio portrait phones and tablets; no fixed pixel
  positions. Safe-area component respecting notches/cutouts/gesture bars on
  all root canvases.
- Tab/list patterns (garage, missions) built to scale to N items without
  layout rework.

### Phase 3 — Google Play Games Services integration (finalization)
- Sign-in, cloud save (tie into `SaveService`), leaderboards (best score),
  achievements (tied to missions).
- Verify Play Console prerequisites (app signing, package, store listing
  requirements) before wiring — flag anything needing manual console setup.

### Phase 4 — Ads & other revenue
- Implement `MONETIZATION_PLAN.md` MVP: AdsService (interstitial + rewarded
  revive), IAPService (remove-ads), consent/compliance (CMP, ATT if iOS ever
  ships), analytics events.
- Frequency-cap and placement rules exactly as specified in that plan —
  retention over short-term ARPDAU.

### Phase 5 — Final polish & game feel
- Juice pass: hit-stop, screen shake tuning, combo flourishes, haptics.
- Audio integration pass per `AUDIO_PLAN.md`.
- Performance pass (draw calls, GC allocs, pooling audit) — the optimization
  checkpoint before store submission.
- QA pass per `QA_PLAN.md`, release build per `RELEASE_PLAN.md`.

---

## 5. Working rules for this pass

- Base controls (drag-steer + tap/hold drift, no accel/brake) are locked —
  do not redesign them, only tune feel.
- Optimization is a first-class constraint: favor pooling, batching, and
  data-driven tuning over runtime allocation; any "smart hack" trading
  correctness/generality for performance must be flagged and approved before
  landing.
- No editor play-mode testing loops to verify each change — validate through
  code review and targeted checks; save interactive testing for milestones
  the user explicitly wants to see.
- UI must be anchor/pivot-correct and safe-area aware from the first screen
  built, not retrofitted later.
