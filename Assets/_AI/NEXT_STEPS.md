# Next Steps

> Refreshed 2026-07-26 via Cowork audit. Supersedes the 2026-07-08 version — the core loop,
> meta progression, garage, and monetization scaffolding described as "next" there are now
> done and verified (25/25 EditMode tests, live scene checks). The project's focus is moving
> from **features** to **art**.

## Done (feature-complete)
- Drift/steer core loop, endless procedural road, traffic (spawn/fairness/pooling),
  near-miss + collision detection, run state machine, restart/revive flow.
- Scoring: distance, drift combo tiers, near-miss chaining, live HUD.
- Garage: modular vehicles, 3D carousel, purchase confirmation, gem currency.
- Missions/trials/daily-challenge, local leaderboard.
- Settings (audio/haptics toggles, restore purchases), first-run onboarding, fade scene
  transitions, safe-area-correct layout.
- Real IAP for remove-ads (verified in Editor Fake Store).
- Ads / Play Games / consent fully scaffolded as stubs, ready to wire once external
  accounts/decisions exist.
- DOTween game-feel pass (popups, HUD, camera, combo tiers).

## Now: art phase
Ojas is shifting focus to art. Highest-leverage order, per `ART_PLAN.md`:
1. **Restyle the player car** (hero read, confirm camera framing) — base on ACC_Lite
   `SunLineGTE_Drift`, don't ship the vendor look as-is.
2. **Author biome #1 end-to-end** (palette, sky, fog, post-processing, accent props) via
   `Assets -> Create -> Drift Therapy -> Biome Data` — proves the pipeline; the
   `RoadBiomeManager` code is ready and waiting on data. Suggested order: City
   outskirts/alleys (golden hour) -> Hills (green->amber dusk) -> Bridge (blue hour).
2. **Traffic archetypes** (2-3 silhouettes, recolor variants, muted vs. the player car).
3. **VFX pass**: drift smoke intensity by combo, persistent skid decals, speed lines,
   near-miss flash, crash burst.
4. **HUD + end screen visual polish** to match the biome palette.
5. **Biomes #2-3**, then juice/polish pass, then store assets (icon, screenshots, preview
   video).

Performance budgets to respect while restyling (see `ART_PLAN.md` section 6): player car
≤3-5k tris, traffic ≤1.5-3k tris, 60fps target on mid-tier, 30fps floor on low-end, pooled
decals/particles only.

## Needs external input (not art, not code)
1. Play Console app + OAuth client, then swap `PlatformServices.PlayGames` from its stub.
2. Pick an ads mediation SDK (LevelPlay is already installed — just needs an App Key /
   account setup) and wire a consent flow.
3. A real device/editor Profiler pass (frame time, GC alloc/frame, DOTween tween count)
   before any release build.
4. Real remove-ads/rewarded-ad store listing text + a completed purchase test on a real
   Google Play test track.

## Good next coding sessions (once art assets exist, or in parallel)
1. Wire `PlatformServices.Ads.ShowInterstitial`/`ShowRewarded` once a network is picked.
2. Audio integration pass per `AUDIO_PLAN.md` (needs the SFX assets first).
3. QA pass per `QA_PLAN.md`, then release build per `RELEASE_PLAN.md`.
4. Optional: migrate `IAPService.cs` off the deprecated (but supported) classic IAP API.

## Risk areas (still open, unchanged)
- Touch steering feel is only validated in-editor/simulator — needs a real device pass.
- Traffic fairness logic reads as sound in code review but hasn't had a long real-device
  playtest.
- Biome pop-in will need `crossfadeSpeed`/`startDistance` tuning once real biome content
  exists — validate in motion on a phone, not the editor viewport.
