# Project Scope

> **Build status (2026-07-26): feature-complete first playable, 25/25 tests passing. Now in the art phase.** See `CURRENT_STATE.md` / `NEXT_STEPS.md`.

## One-line pitch
Portrait hypercasual endless drift racer with constant forward motion, analog steering, tap/hold drift, traffic avoidance, combo scoring, and fast restart.

## Core loop
Drive -> Drift -> Dodge -> Build combo -> Increase difficulty -> Crash -> Restart

## Mandatory controls
- Touch: finger drag left/right for steering.
- Touch: tap or hold for drift.
- Desktop: `A` / `D` for steering through the Input System.
- No normal accel/brake loop for the player.

## Core game systems
- Constant forward speed.
- Drift is both movement and scoring.
- Traffic with increasing density and variation.
- Collision ends the run.
- Distance, drift multiplier, and near-miss scoring.
- Endless procedural road chunks.

## Feel goals
- Smooth, responsive, arcade handling.
- Easy to learn, hard to master.
- Short sessions, high replay.
- Minimal Unity UI Toolkit UI, strong feedback.

## Visual target
- Semi top-down / over-shoulder camera.
- Stylized, vibrant, lowpoly look.
- Smoke, skid marks, camera tilt, speed lines.

## Non-goals for now
- Lap racing.
- Manual gear shifting.
- Complex simulation handling.
- Large menu meta-game before the core loop feels good.
- New first-party Canvas/TMP UI; game UI should follow `UI_TOOLKIT_PLAN.md`.
