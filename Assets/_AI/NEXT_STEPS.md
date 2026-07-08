# Next Steps

> Superseded the 2026-06 version of this doc, which referenced UI Toolkit —
> the project moved to uGUI/TextMeshPro (see `Assets/Scripts/Editor/UIBuilder.cs`).
> See `Assets/_AI/PROGRESS_LOG.md` (2026-07-08 entries) for what just shipped.

## Done
- Drift/steer core loop, endless road, biomes, traffic v2 (director/fairness/
  follow-gap), collectibles, near-miss/collision detection, run state machine.
- Missions/trials/daily-challenge system, local leaderboard, DOTween game-feel
  pass, combo tiers, near-miss chaining, loading-screen transitions, safe-area
  layout.
- Settings screen (music/SFX/haptics toggles, remove-ads, restore purchases),
  onboarding popup (first-run only).
- Real IAP (`Assets/Scripts/Services/IAPService.cs`) for the remove-ads
  non-consumable — functional today, verified in Editor Fake Store.
- Combo-tier smoke tint (`Assets/Scripts/Drift/ComboSmokeFx.cs`) — the
  tire-smoke item from the morning session, done via a non-invasive
  `FXController` public-API hook (no ACC_Lite files touched).
- Play Games/Ads/Consent all scaffolded as stubs (`Assets/Scripts/Services/`),
  tracked in `Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md`.

## Needs external input (not more coding)
1. Play Console app + OAuth client, then install `com.google.play.games` and
   swap `PlatformServices.PlayGames` from the stub.
2. Pick an ads mediation SDK (AdMob / LevelPlay / AppLovin MAX per
   `MONETIZATION_PLAN.md`), install it, swap `PlatformServices.Ads`, and wire
   a real CMP into `PlatformServices.Consent` (e.g. Google UMP if AdMob).
3. A real device/editor Profiler pass (frame time, GC alloc/frame, DOTween
   active-tween count under real play) — deliberately deferred so far to keep
   sessions token-light; do this before a release build.
4. Real remove-ads/rewarded-ad IAP store listing text, pricing, and a
   completed purchase flow test on a real Google Play test track (Editor
   Fake Store confirms the code path; it can't confirm the real store UI).

## Good next coding sessions
1. Wire `PlatformServices.Ads.ShowInterstitial`/`ShowRewarded` for real once
   a network is picked (`GameController.HandleReviveRequested`,
   `GameHudUI.GoHome` already have the TODO-commented landing spots).
2. Audio integration pass per `AUDIO_PLAN.md` (needs UI/SFX audio assets —
   engine/skid sounds already exist and are wired via ACC_Lite; near-miss
   cue, combo escalation, crash, and UI sounds do not exist yet).
3. QA pass per `QA_PLAN.md`, release build per `RELEASE_PLAN.md`.
4. Optional IAP v5 API migration (`IAPService.cs` uses the classic
   `IStoreListener` API — deprecated but fully supported; no urgency).

## Risk areas (still true)
- Touch steering must feel immediate, not floaty — needs a real device pass.
- Endless road spawning must avoid visible seams.
- Traffic fairness (`TrafficDirector`) was code-reviewed and looks sound, but
  only a real play session can confirm it *feels* fair.
- Settings' volume toggles are binary (on/off), not continuous sliders — a
  deliberate MVP simplification; revisit if the game grows an audio mix that
  needs finer control.
