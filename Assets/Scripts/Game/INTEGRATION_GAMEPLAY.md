# Gameplay Glue — DriftEndless Scene Wiring

This document describes how to wire the gameplay-glue scripts by hand in the
`DriftEndless` scene. Do this in the Unity Editor once the MCP bridge is back.
All scripts use the global namespace, so no `using` is required to reference them.

## Components & where they go

### 1. `GameController` — on a dedicated `GameController` GameObject
Create an empty GameObject named `GameController` at the scene root and add the
`GameController` component. Assign its serialized references:

| Field                 | Assign                                                        | Required |
|-----------------------|--------------------------------------------------------------|----------|
| `car`                 | The player car (its `HyperDriftCarController`)               | Yes      |
| `road`                | The `RoadSegmentPool` object (RoadSystem)                    | Yes      |
| `player`              | The player car `Transform` (root)                           | Yes      |
| `trafficSpawner`      | The `TrafficSpawner` object                                  | Optional |
| `collisionDetector`   | The `PlayerCollisionDetector` on the player car             | Yes      |
| `nearMissDetector`    | The `NearMissDetector` on the player car                    | Yes      |
| `difficulty`          | The `DifficultyDirector` (see below)                        | Optional |
| `reviveCount`         | Default 1                                                    | tuning   |
| `autoStartOnLoad`     | `true` for instant play; `false` to wait for StartRequested | tuning   |
| `comboStepDistance`   | Default 25                                                   | tuning   |
| `multiplierStep`      | Default 0.5                                                  | tuning   |
| `nearMissBonus`       | Default 250                                                  | tuning   |

`GameController` creates its own `RunStateMachine` and `ScoreSystem` in `Awake`
(plain C#, not components). It drives everything through `GameSignals`.

### 2. `DifficultyDirector` — on the `GameController` GO (or RoadSystem)
Add `DifficultyDirector` to the `GameController` GameObject (simplest) or to the
RoadSystem object. Assign:

| Field                          | Assign / Value                         |
|--------------------------------|----------------------------------------|
| `road`                         | The `RoadSegmentPool`                   |
| `fullDifficultyDistance`       | Default 2500 (metres to reach t = 1)    |
| `difficultyCurve`              | Linear by default; remap if desired     |
| `maxSpeedMultiplier`           | Default 1.35                            |
| `maxTrafficDensityMultiplier`  | Default 2                              |

Then drag this same component into `GameController.difficulty`. The controller
calls `Tick(distance)` each frame while Running; the director writes
`road.DifficultyT` and raises `GameSignals.DifficultyChanged`. Other systems may
read `SpeedMultiplier` / `TrafficDensityMultiplier` if desired (not auto-applied).

### 3. `PlayerCollisionDetector` — on the player car
Add to the player car GameObject (the one with the car colliders / rigidbody).

| Field          | Value                                                          |
|----------------|----------------------------------------------------------------|
| `trafficMask`  | Layer mask covering the traffic layer (preferred)             |
| `trafficTag`   | `"Traffic"` (fallback if mask is empty / not matched)         |
| `graceSeconds` | Default 0.3 — post-revive invulnerability window              |

It fires `Hit(GameObject)` on `OnCollisionEnter` and `OnTriggerEnter` when the
other object matches the mask or tag. The layer mask is checked first; the tag is
a fallback. `GameController.HandleReviveRequested` calls `StartGrace()`.

### 4. `NearMissDetector` — on the player car, fed by a CHILD trigger collider
Add to the player car root. It relies on `OnTriggerEnter`/`OnTriggerExit`, so the
player needs a **child GameObject with a trigger collider larger than the car
body** (e.g. a wider box collider with `Is Trigger = true`). Put the
`NearMissDetector` component on whichever object receives those trigger callbacks
— simplest is to put it on the same child that holds the trigger collider, OR on
the root if the root holds the trigger. (Unity routes OnTrigger* to the component
on the GameObject whose collider triggered.)

| Field                    | Assign / Value                                  |
|--------------------------|-------------------------------------------------|
| `player`                 | Player car `Transform`                          |
| `car`                    | `HyperDriftCarController` (optional speed gate) |
| `minSpeedKphForNearMiss` | Default 25                                       |

It fires `NearMissed(TrafficAgent)` once per agent per pass. Traffic must carry a
`TrafficAgent` component (found via `GetComponentInParent`). Ensure the physics
layer collision matrix allows the trigger volume to overlap traffic colliders.

## Physics / layer setup
- Create (or reuse) a `Traffic` layer and assign it to traffic prefabs.
- Set `PlayerCollisionDetector.trafficMask` to that layer, and/or tag traffic
  prefabs `"Traffic"`.
- The near-miss trigger volume and the traffic colliders must be on layers that
  collide in the Physics matrix; otherwise `OnTriggerEnter` never fires.
- Keep the solid car collider (for real collisions) separate from the larger
  near-miss trigger so a near-miss is not also a collision.

## Signal flow recap
- UI buttons call `GameSignals.RaiseStartRequested` / `RaiseRestartRequested` /
  `RaiseReviveRequested` / `RaisePauseToggled` / `RaiseResumeRequested` /
  `RaiseQuitRequested`.
- `GameController` reacts and broadcasts: `RunStarted`, `RunFailed`, `RunReady`,
  `ReviveOffered`, `Revived`, `Paused`, `Resumed`, `ScoreChanged`,
  `MultiplierChanged`, `DistanceChanged`, `NearMiss`, `DifficultyChanged`.
- HUD / end-screen presenters subscribe to those to update the view.

## Notes
- `SaveService` is static (no GameObject). Best score persists via PlayerPrefs.
- Pause sets `Time.timeScale = 0`; the controller always restores it to 1 on
  restart / revive / resume / quit.
- `TrafficSpawner` ticks itself via its own `Update` — `GameController` does not
  drive it. It is referenced only so the controller can be extended later.
