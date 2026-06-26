# Drift Therapy — Release Plan

> Milestone roadmap from the current vertical slice to global launch.
> Platforms: Android & iOS. Current version: `0.1.0`.
> Durations are indicative for a small team and should be re-baselined to your capacity.

---

## Milestone overview

| # | Milestone | Version | Goal | Rough effort |
|---|---|---|---|---|
| M0 | Foundation (done/in progress) | 0.1.x | Driving + road + camera feel | — |
| M1 | **Playable vertical slice** | 0.2.0 | Core loop closed & fun on device | ~2–3 wks |
| M2 | **Content & feel pass** | 0.3.0 | Biomes, difficulty director, juice | ~2–3 wks |
| M3 | **Monetization & analytics** | 0.4.0 | Ads, IAP, telemetry wired | ~1–2 wks |
| M4 | **Soft launch (beta)** | 0.9.0 | Limited-geo live, read metrics | ~2 wks + live |
| M5 | **Global launch** | 1.0.0 | Worldwide store release | ~2 wks |
| M6 | **Live ops** | 1.x | Content cadence, events, tuning | ongoing |

---

## M1 — Playable vertical slice (`0.2.0`)

**Definition of done:** road spawns forever, car drifts smoothly, traffic
appears, score builds (distance + drift combo + near-miss), crash ends the run,
restart is instant — verified on a physical Android device.

- Build all **P0** features (see `FEATURES_PLAN.md`).
- Ship P0 UI as Unity UI Toolkit screens following `UI_TOOLKIT_PLAN.md`.
- First real-device touch-feel test; tune steering responsiveness.
- Internal playtest: is the 1–3 min loop fun and retry-compelling?
- **Gate:** if the loop isn't fun, stop and iterate before adding content.

## M2 — Content & feel pass (`0.3.0`)

- Author 2–3 biomes; wire transitions.
- Difficulty director (speed/density/curvature ramp).
- Feedback/juice pass (smoke, skids, tilt, speed lines, shake, haptics).
- Audio integration (drift intensity, near-miss, combo, crash).
- Onboarding first-run teach.
- Pause/settings/onboarding UI remains UI Toolkit based and uses the same design
  system tokens/components as P0.
- **Gate:** session length and "one more run" feel are trending right in playtests.

## M3 — Monetization & analytics (`0.4.0`)

- Interstitial after run; one rewarded revive per run; remove-ads IAP.
- Analytics events (run start/end, score, death cause, revive, ad shown, IAP).
- Persistence (best score, settings, remove-ads).
- Store-readiness: privacy policy, data-safety/ATT disclosures, age rating.
- **Gate:** ad/IAP flows tested in sandbox; no crashes; telemetry verified.

## M4 — Soft launch / beta (`0.9.0`)

- Release to 1–2 test geographies (common choices: Canada, Philippines, Nordics)
  via Google Play + TestFlight/App Store limited.
- Instrument and read: **D1/D7 retention, session length, runs/session, ARPDAU,
  crash-free rate, ad fill/eCPM.**
- Iterate on tuning, difficulty, and monetization placement from real data.
- **Gate (typical targets, adjust to your bar):** D1 ≥ ~35%, crash-free ≥ 99%,
  stable session length, monetization signals positive.

## M5 — Global launch (`1.0.0`)

- Store assets: icon, screenshots, preview video, localized listings (key locales),
  ASO keywords.
- Final UI Toolkit pass for safe areas, localization readiness, readable HUD, and
  store/IAP/consent screens.
- Final QA pass across a device matrix (see `QA_PLAN.md`).
- Staged rollout (e.g. 10% → 50% → 100% on Google Play) with crash monitoring.
- Marketing beat aligned to launch (see `MONETIZATION_PLAN.md` UA notes).

## M6 — Live ops (`1.x`)

- Regular content drops (biomes, cars), events/challenges, A/B tuning of
  difficulty and monetization, and a steady cadence informed by analytics.

---

## Cross-cutting release requirements

- **Versioning:** keep `bundleVersion` (currently `0.1.0`) and build numbers in
  lock-step with these milestones.
- **Build profiles:** validated Android + iOS profiles (an Android build profile
  already exists under `Assets/Settings/Build Profiles`).
- **Compliance:** Google Play Data Safety, Apple Privacy Nutrition Labels + ATT
  prompt, COPPA/age-rating, ad-network consent (GDPR/CCPA) before soft launch.
- **Source hygiene:** confirm `DriftEndless.unity` is canonical; remove
  `_Recovery/` scenes; ensure `.gitignore` covers `Library/`, `Temp/`, `Builds/`.

---

## Top release risks

- **Loop fun is unproven** — M1 gate exists specifically to de-risk this.
- **Touch feel on device** — test early and often; the whole pitch depends on it.
- **Monetization vs retention balance** — interstitial frequency can kill
  retention; tune in soft launch, not at global launch.
- **Scope creep** — defer all P2/P3 until the M1 loop is fun.
