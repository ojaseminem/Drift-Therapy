# Drift Therapy — Monetization Plan

> Aligned to GDD §10. Model: **ad-supported hypercasual with light IAP.**
> Unity Purchasing (`com.unity.purchasing 5.2.1`) is already installed;
> `BillingMode.json` present. An ads SDK is **not** yet integrated.

---

## 1. Strategy

Hypercasual lives on **volume × retention × ad monetization**, with IAP as a
secondary smoothing layer. The job is to monetize without breaking the 1–3 min
"one more run" loop. Tune in soft launch with real data — never guess at global.

---

## 2. Revenue streams

| Stream | Mechanic | Notes |
|---|---|---|
| **Interstitial ads** | Shown after a run ends | Frequency-capped; the biggest lever and the biggest retention risk |
| **Rewarded ads** | 1 revive per run | Player opts in for value; high-quality, non-intrusive revenue |
| **Remove-ads IAP** | One-time (or subscription per GDD) | Removes interstitials; keeps rewarded as opt-in |
| **Premium currency** | Soft IAP | Sink for cosmetics; post-MVP |
| **Cosmetic cars** | Buy with currency or direct IAP | Pure cosmetic; no pay-to-win (no competitive advantage in an endless scorer) |

---

## 3. Placement rules (retention-safe defaults — validate in soft launch)

- **Interstitial:** after run end, **frequency-capped** (e.g. not every run; min
  gap of N runs or M seconds) and **never** mid-run. Suppressed for remove-ads buyers.
- **Rewarded revive:** offered once per run on the fail screen; clearly optional;
  continues the *same* run with brief invulnerability.
- **No forced ads on first sessions** — let new players reach the hook before any ad.
- **Respect consent** — GDPR/CCPA/ATT prompts before personalized ads.

---

## 4. Implementation plan

1. **AdsService** abstraction over the chosen mediation SDK (e.g. Unity LevelPlay/
   ironSource, AppLovin MAX, or AdMob) — interstitial + rewarded, frequency cap,
   consent gating. Keep mediation behind the interface so it's swappable.
2. **IAPService** on Unity Purchasing — remove-ads product, restore purchases,
   receipt validation; persists entitlement via SaveService.
3. **Revive flow** wired into the `RunStateMachine` (`Reviving` state) and
   presented through Unity UI Toolkit screens per `UI_TOOLKIT_PLAN.md`.
4. **Consent & compliance** — CMP, ATT (iOS), Play Data Safety, age rating.
5. **Analytics** — ad shown/clicked/filled, rewarded completion, revive taken,
   IAP funnel, ARPDAU; tie to retention cohorts.

---

## 5. KPIs to read in soft launch

- **Retention:** D1 / D7 (gate before scaling UA).
- **Engagement:** session length, runs/session, sessions/day.
- **Monetization:** ARPDAU, ad ARPDAU vs IAP ARPDAU, eCPM/fill by network,
  rewarded opt-in rate, remove-ads conversion.
- **Health:** crash-free rate, ad-load failure rate.

Decision rule: if interstitials lift ARPDAU but tank D1/D7, **back off frequency**
— retention compounds, a single extra ad does not.

---

## 6. User acquisition (post product-market-fit)

- Don't scale UA until soft-launch retention + monetization clear your bar.
- Creative built from the hook: drift flow, near-miss tension, combo payoff,
  satisfying restarts. Short, loopable, sound-off-readable.
- Track CPI vs LTV; only scale channels where LTV > CPI with margin.

---

## 7. Risks

- **Interstitial over-frequency** — the #1 hypercasual retention killer.
- **Ad SDK not yet integrated** — schedule in M3 (`RELEASE_PLAN.md`); mediation
  setup + consent takes longer than expected.
- **iOS ATT** opt-out depresses eCPM — plan for it.
- **Pay-to-win perception** — keep all car purchases cosmetic.
- **Compliance gaps** — missing Data Safety/ATT/age-rating blocks store release.
