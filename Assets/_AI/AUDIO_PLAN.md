# Drift Therapy — Audio Plan

> Aligned to GDD §8 (Feedback/Audio). Goal: audio that *sells flow and risk* and
> scales with the drift/combo system. ACC_Lite already provides a `CarSoundController`
> base to build on.

---

## 1. Audio pillars

- **Flow over noise.** The "therapy" vibe wants a satisfying, musical drift —
  not an aggressive sim engine. Smooth, escalating, rewarding.
- **Risk telegraphing.** Near-miss and rising difficulty should be *audible*.
- **Feedback that teaches.** Sound confirms drift start, combo growth, near-miss,
  and crash without the player looking at the HUD.

---

## 2. Sound map

| Event | Sound | Behavior |
|---|---|---|
| Engine | Continuous loop | Pitch/volume scale with speed; subtle since speed is near-constant |
| Drift start | Skid onset + tire squeal | Triggered when `HyperDriftCarController` enters drift |
| Drift sustain | Skid loop | Intensity scales with drift angle / combo tier |
| Drift release | Squeal tail | On straighten |
| Near-miss | Whoosh + cue stinger | On close pass without contact; pitch rises with combo |
| Combo escalation | Layered musical tier | Each multiplier tier adds a layer / raises key |
| Crash | Impact + debris | One-shot; ducks music; leads into fail state |
| UI | Tap, confirm, restart | Crisp, short |
| Ambient | Per-biome bed | Crossfades with biome (wind, city, water) |

---

## 3. Music

- **Adaptive/layered bed** that builds with the combo multiplier — the longer the
  flow, the fuller the track. Resets on crash/combo break.
- Loopable, low-fatigue (sessions are short and replayed often).
- Consider a calm baseline that intensifies with risk — reinforces "therapy →
  flow → tension → release".

---

## 4. Implementation

- **Build on ACC_Lite `CarSoundController`** for engine/skid; route everything
  through an **Audio Mixer** with groups: Music, SFX (Drift, Traffic, UI), Ambient.
- **Drive audio from game events**, not polling: hook drift state, combo tier,
  near-miss, crash, biome change.
- **Ducking:** crash and key cues duck music briefly.
- **Mobile hygiene:** compressed clips, limited simultaneous voices, pooled
  AudioSources, respect silent switch (iOS), pause on focus loss.
- **Haptics pairing:** light haptic on drift start/near-miss, stronger on crash
  (coordinate with `MONETIZATION_PLAN.md`/settings for opt-out).

---

## 5. Settings & accessibility

- Independent Music / SFX volume sliders; master mute.
- Haptics toggle.
- Settings UI must be built with Unity UI Toolkit per `UI_TOOLKIT_PLAN.md`.
- Audio must never be required to play — all critical info also shown on HUD.

---

## 6. Production order

1. Wire engine + drift skid from `CarSoundController` to the new controller.
2. Near-miss cue + crash one-shot (ties to scoring/fail).
3. Combo-tier music layers.
4. Per-biome ambient beds.
5. UI sounds + settings + haptics.
6. Mix pass on a real device (phone speaker *and* headphones).

---

## 7. Risks

- **Repetition fatigue** — short loops replayed constantly; vary skid/near-miss
  samples and keep music evolving.
- **Phone-speaker mix** — must read on tiny mono speakers, not just headphones.
- **Voice limits** — traffic-dense moments can exceed mobile voice budgets; cap
  and prioritize.
