# Next Steps

> Superseded the 2026-06 version of this doc, which referenced UI Toolkit —
> the project moved to uGUI/TextMeshPro (see `Assets/Scripts/Editor/UIBuilder.cs`).
> See `Assets/_AI/PROGRESS_LOG.md` (2026-07-08 entry) for what just shipped.

## Done (core loop + meta progression + Phase 1/2 roadmap items)
- Drift/steer core loop, endless road, biomes, traffic v2 (director/fairness/
  follow-gap), collectibles, near-miss/collision detection, run state machine.
- Missions/trials/daily-challenge system, local leaderboard, DOTween game-feel
  pass, combo tiers, near-miss chaining, loading-screen transitions, safe-area
  layout.
- Play Games/Ads scaffolded as stubs (`Assets/Scripts/Services/`), tracked in
  `Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md`.

## Needs external input (not more coding)
1. Play Console app + OAuth client, then install `com.google.play.games` and
   swap `PlatformServices.PlayGames` from the stub.
2. Pick an ads mediation SDK (AdMob / LevelPlay / AppLovin MAX per
   `MONETIZATION_PLAN.md`), install it, swap `PlatformServices.Ads`.
3. A real device/editor Profiler pass (frame time, GC alloc/frame, DOTween
   active-tween count under real play) — deliberately deferred so far to keep
   sessions token-light; do this before a release build.

## Good next coding sessions (Phase 4/5 remainder)
1. IAP remove-ads via the already-installed `com.unity.purchasing` (once a
   revive/interstitial flow exists to gate).
2. Consent/UMP flow before any ad request.
3. Audio integration pass per `AUDIO_PLAN.md` (needs audio assets first).
4. Settings screen + onboarding (P2 in `FEATURES_PLAN.md`, not yet built).
5. Tire-smoke/skid FX intensity tied to combo tier — deliberately skipped in
   the 2026-07-08 pass because it lives inside the vendored ACC_Lite wheel/FX
   plugin (`Assets/ACC_Lite/Scripts/Game/VisualAndSounds/FXController.cs`,
   `Wheel.cs`); needs a live device/editor pass to verify before touching it.
6. QA pass per `QA_PLAN.md`, release build per `RELEASE_PLAN.md`.

## Risk areas (still true)
- Touch steering must feel immediate, not floaty — needs a real device pass.
- Endless road spawning must avoid visible seams.
- Traffic fairness (`TrafficDirector`) was code-reviewed and looks sound, but
  only a real play session can confirm it *feels* fair.
