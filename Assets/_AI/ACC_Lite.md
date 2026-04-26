# ACC_Lite Analysis

## What it is good for
- WheelCollider-based arcade car physics.
- Existing drift feel and slip detection.
- Smoke, skid trail, and backfire feedback.
- A ready car prefab set with drift tuning.

## Best reusable pieces
- `CarController.cs`
- `Wheel.cs`
- `PG_WheelCollider.cs`
- `FXController.cs`
- `CarSoundController.cs`
- `BodyTilt.cs`
- `Drift` prefab variants under `Assets/ACC_Lite/Prefabs/Cars/`

## Why it helps the new game
- It already has a stable steering + slip foundation.
- It already exposes wheel friction tuning.
- Drift-tuned prefab values are a good starting point for hypercasual handling.

## What does not match the new game yet
- It is built like a racing demo, not an endless runner.
- It expects older input flow through `UserControl`.
- It includes gear / RPM logic that is not central to the new design.
- It does not provide endless road streaming or traffic AI.

## Useful tuning facts
- `SunLineGTE_Drift` is the best starting prefab for slide-heavy handling.
- `SunLineGTE_Race` is more planted and less drift-friendly.
- The drift prefab already reduces sideways grip on some wheels.

## Practical conclusion
- Keep ACC_Lite as the physics and feedback base.
- Replace the old gameplay loop with hypercasual endless-run systems.
