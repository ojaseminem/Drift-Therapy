# Drift Therapy — QA & Test Plan

> Test strategy from vertical slice to launch. Unity Test Framework is installed;
> the MCP bridge can run tests and read the console.

---

## 1. Test pyramid

- **Automated (fast, many):** Edit-mode unit tests for pure logic — scoring math,
  combo build/break, difficulty curve values, run-state transitions, pool
  integrity (no leaks, correct recycle).
- **Play-mode (medium):** integration of run lifecycle, traffic spawn/recycle,
  collision→fail, near-miss firing, biome transition triggers.
- **Manual / device (few, high-value):** feel, performance, monetization flows,
  store-build smoke tests.

---

## 2. What to automate first

| Area | Test | Why |
|---|---|---|
| Scoring | distance accrual, combo multiplier up/reset, near-miss bonus | Core fairness; easy to regress |
| Run-state | Ready→Running→Failed→Restart / Revive branch | Loop must never soft-lock |
| Pools | road + traffic spawn/recycle, no allocation, no leak | Perf + correctness |
| Difficulty | ramp values monotonic & bounded | Prevents impossible runs |
| Save | best score / remove-ads persist & restore | Data integrity |
| UI Toolkit | UIDocuments load, required named elements exist, buttons dispatch expected events | Prevents UI soft-locks |

---

## 3. Manual test focus (the things automation can't judge)

- **Touch feel** — steering immediate, not floaty; drift initiation reliable
  (tap vs hold); the make-or-break test, run it every milestone on a real phone.
- **UI Toolkit layout** — HUD/end/pause/settings screens render through
  `UIDocument`, respect safe areas, and keep 48 px mobile touch targets.
- **Difficulty fairness** — no unavoidable traffic spawns; ramp feels fair.
- **Readability at speed** — road edges, traffic, combo state parse in portrait.
- **Seam/pop-in** — no visible road seams or biome snap.
- **Juice** — feedback (smoke, shake, audio, haptics) lands and isn't excessive.

---

## 4. Performance testing

- Profile on **low, mid, high** devices each milestone.
- Watch: frame time stability, GC allocation in the run loop (target zero),
  draw calls, physics cost (WheelColliders), overdraw, memory, battery/thermal
  on long sessions.
- Gate: 60 fps mid-tier, 30 fps floor low-end, crash-free ≥ 99% before soft launch.

---

## 5. Device matrix (representative)

- **Android:** 1 low-end (2–3GB RAM), 1 mid, 1 recent flagship; mixed aspect
  ratios incl. notches/cutouts; verify portrait safe-areas.
- **iOS:** 1 older supported iPhone, 1 recent; verify notch/Dynamic Island
  safe-areas and silent-switch audio behavior.

---

## 6. Monetization & compliance QA

- Interstitial frequency cap behaves; suppressed for remove-ads buyers.
- Rewarded revive grants correctly; no reward on ad failure/cancel.
- IAP purchase + **restore** works; entitlement persists across reinstall.
- Consent (GDPR/CCPA/ATT) prompts fire correctly; no personalized ads pre-consent.
- Store compliance: Data Safety / Privacy labels / age rating present.

---

## 7. Release QA gates

- **M1 (slice):** loop is playable start-to-restart on device; no soft-locks.
- **M3 (monetization):** ad/IAP sandbox flows pass; telemetry verified; no crashes.
- **M4 (soft launch):** crash-free ≥ 99%, retention/perf gates met (`RELEASE_PLAN.md`).
- **M5 (global):** full device-matrix pass; staged-rollout crash monitoring green.

---

## 8. Process

- CI runs Edit/Play-mode tests on each push where practical (or via MCP locally).
- Bug triage by severity: **S1** crash/soft-lock/data-loss, **S2** broken core
  loop, **S3** feel/visual, **S4** polish. S1/S2 block the milestone.
- Keep a living regression checklist; every fixed S1/S2 gets a test.
- Verify console is **error-free** before each build (MCP `unity://logs/error`).
- First-party UI regressions should be fixed in UI Toolkit UXML/USS/presenters,
  not by adding new Canvas/TMP screens.

---

## 9. QA risks

- **Feel is subjective & device-dependent** — test on hardware, with several
  testers, every milestone; don't trust the editor.
- **Procedural edge cases** — rare seeds can produce unfair roads/spawns; fuzz
  seeds and add bounds tests.
- **Long-session stability** — endless runner = memory/thermal creep; soak-test.
