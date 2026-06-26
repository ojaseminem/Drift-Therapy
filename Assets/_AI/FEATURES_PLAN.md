# Drift Therapy — Features Plan

> Prioritized feature backlog. Grounded in `STATE_ANALYSIS.md` and the GDD.
> Priority key: **P0** = needed to close the core loop, **P1** = needed for a
> shippable soft-launch build, **P2** = retention/polish, **P3** = post-launch.

---

## Guiding principle

The game's hook is **risk → reward → instant retry**. Build features in the
order that makes the loop *playable* first, *good* second, *deep* third. Do not
add meta-systems before the 1–3 minute core loop feels great on a real device.

---

## P0 — Close the core loop (vertical slice)

These five features turn "a car driving forever" into "a game".

1. **Traffic system**
   - Pooled traffic spawner that places AI vehicles ahead of the player and
     recycles them behind, fitting the curved (non-lane) road.
   - 2–3 vehicle archetypes (car, truck) with slight speed/lateral variation.
   - Spawn density driven by a difficulty curve (distance/time based).
   - Acceptance: traffic appears continuously without seams, never spawns in an
     unavoidable position, recycles cleanly, no GC spikes.

2. **Collision & fail detection**
   - Detect player↔traffic and player↔off-road. Any collision ends the run.
   - Acceptance: a crash reliably triggers the fail state within one frame and
     stops scoring.

3. **Run-state machine**
   - States: `Ready → Running → Failed → (Revive?) → Restart`.
   - Owns start, fail, restart, and the revive branch.
   - Acceptance: a full run can be played start-to-restart with no manual setup.

4. **Scoring model**
   - Distance (base), drift multiplier (continuous drift builds combo; breaking
     drift resets), near-miss bonus (close pass without contact).
   - Acceptance: score increases while driving, multiplier climbs while drifting,
     near-miss fires on close passes.

5. **Minimal HUD + end screen**
   - In-run: score, distance, current multiplier.
   - End: final score, best score, **instant** restart button.
   - Must use Unity UI Toolkit (`UIDocument`, UXML, USS) and the project design
     system plan in `UI_TOOLKIT_PLAN.md`.
   - Acceptance: readable in portrait, restart is one tap and immediate, no
     first-party Canvas/TMP dependency.

**Exit criteria for P0:** road spawns forever, car drifts well, traffic appears,
score builds, crash ends the run, restart is instant. (This is the "best next
milestone" from `NEXT_STEPS.md`.)

---

## P1 — Make it shippable (soft-launch build)

6. **Near-miss system polish** — tuned trigger distance, screen feedback, combo
   coupling, audio cue.
7. **Difficulty director** — single system ramping speed, traffic density, and
   curvature together over a run; data-tunable.
8. **Biome content** — author 2–3 `BiomeData` assets (e.g. Hills, City outskirts,
   Bridge) and wire distance thresholds so transitions read as intentional.
9. **Feedback layer** — tire smoke, skid marks, camera tilt, speed lines, hit
   reaction. Most assets exist in ACC_Lite/`FXController`; wire to game events.
10. **Persistence** — save/load best score, settings, remove-ads flag.
11. **Pause & resume** — mobile-friendly UI Toolkit pause, resume,
    quit-to-restart.
12. **Audio integration** — drift-intensity-scaled engine/skid, near-miss cue,
    combo escalation, crash, UI (see `AUDIO_PLAN.md`).
13. **Monetization MVP** — interstitial after run, one rewarded revive per run,
    remove-ads IAP (see `MONETIZATION_PLAN.md`).
14. **Analytics events** — run start/end, distance, score, deaths-by-cause,
    revive taken, ad shown (needed to read soft-launch data).

---

## P2 — Retention & polish

15. **Cosmetic cars** — selectable skins/vehicles (cosmetic only; ties to IAP).
16. **Daily reward / streak** — lightweight return hook.
17. **Missions / goals** — "drift X meters", "Y near-misses in one run".
18. **Juice pass** — screen shake, hit-stop, combo flourishes, vibration.
19. **Onboarding** — first-run teach of steer + tap/hold drift, no wall of text.
20. **Settings** — UI Toolkit sound, haptics, quality, sensitivity.

---

## P3 — Post-launch

21. Additional biomes and vehicle archetypes.
22. Events / leaderboards / weekly challenges.
23. Premium currency sink and store.
24. Localization.

---

## Dependency notes

- P0.4 (scoring) depends on P0.1 (traffic) for near-miss and on the drift state
  already exposed by `HyperDriftCarController`.
- P1.7 (difficulty director) should subsume the curvature ramps already in
  `RoadCurveGenerator` rather than duplicating them.
- P1.13 (monetization) depends on P0.3 (run-state) for the revive branch and
  interstitial timing.
- Reuse existing systems: `EndlessTrackManager`'s pooling pattern should be the
  template for the traffic pool; the biome manager is ready and only needs assets.
