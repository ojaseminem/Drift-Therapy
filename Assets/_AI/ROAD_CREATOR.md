# Road Creator — Procedural Spline Road System

## Reference Analysis: LinkedIn Road Builder Tool

The LinkedIn screenshot shows a **spline-based runtime road authoring tool** with the following observable traits:

### Architecture
- **Segment inventory** split into categories: Road, Markers, Side
  - Road types: Straight, Sweeper, Ramp, Bank Turn, Jump, Wall Ride, Rally Road, Hairpin, Crest, Bridge Span, Plastic Sprint, Loop Entry, Loop Exit, Corkscrew, Fork Starter, Fast Chicane
  - Markers: Start, Checkpoint, Finish
  - Side rails: Guardrail, Barrier
- **Per-section manipulation gizmos** directly in 3D space — coloured pill/sphere handles for: Roll, Pitch, Yaw, Width, Height, Length, Bank
- **Build / Edit / View** mode split
- **Validation layer** — shows blocking errors (e.g. Missing start marker) before allowing export/play
- **Shape panel** (meters/degrees) for numeric overrides
- **Mesh panel** with runtime preview
- Road segments connect end-to-end along a continuous **Catmull-Rom or Bezier spline**

### Key Design Principles
1. Roads are described as **typed arc-segments** on a spline spine, not raw geometry
2. Each segment has **independent twist/bank/pitch** modifiers
3. The tool is both an **editor-time authoring tool** AND supports **runtime preview**
4. Sections counter shows total count (e.g. "Sections: 2")
5. The coloured gizmo system maps directly to Hermite tangent control

---

## Drift Therapy: Road Creator Plan

### Goal
Replace the current flat-segment `EndlessTrackManager` with a spline-based procedural road generator that:
- Creates smooth, banked road curves with no visible seams
- Supports biome/difficulty variation (road width, curvature, banking)
- Is mobile-performant (mesh pooling, LOD-friendly)
- Looks like the second reference screenshot: asphalt, yellow centre lines, red/white kerbs

---

## System Overview

```
SplineRoadBuilder          — runtime spline curve builder (Catmull-Rom)
ProceduralRoadMesher       — cross-section extrusion along spline
RoadSegmentPool            — recycles meshes behind the player
RoadCurveGenerator         — procedural curve parameter generator
RoadVisuals                — material/UV manager for asphalt, kerbs, markings
```

---

## 1. SplineRoadBuilder.cs

**What it does:** Maintains a chain of `SplineNode` structs. Each node carries:

```csharp
struct SplineNode
{
    Vector3 position;
    Quaternion rotation;
    float width;      // road half-width in metres (default 5m)
    float banking;    // roll angle in degrees (banked curves)
    float pitch;      // vertical angle (ramps, crests)
}
```

The builder uses **Catmull-Rom interpolation** between nodes to produce a smooth world-space curve. It exposes:
- `AddNode(SplineNode)` — appends node at the far end
- `RemoveFirstNode()` — trims behind the player
- `SampleAt(float t, out Vector3 pos, out Quaternion rot, out float width, out float banking)` — continuous sample

---

## 2. ProceduralRoadMesher.cs

**What it does:** Extrudes a cross-section profile along the spline to produce a Unity `Mesh`. The cross-section (per sample point) is:

```
[left kerb] [left lane] [centre line] [right lane] [right kerb]
```

Profile vertices per ring (11 vertices):
```
v0  left kerb outer
v1  left kerb inner
v2  left road edge
v3  left centre
v4  road centre
v5  right centre
v6  right road edge
v7  right kerb inner
v8  right kerb outer
v9  (normal vertex for kerb top)
...
```

Submesh layout:
- **Submesh 0** — asphalt (grey material)
- **Submesh 1** — centre line (yellow dashed, UV-scrolled)
- **Submesh 2** — kerb stripes (red/white alternating, via UV offset)

UV layout: U = cross-section position (0–1 left→right), V = distance along road (world units / repeat tile). This gives automatic tiling as the road extends.

**Key method:**
```csharp
void RebuildMesh(List<SplineNode> sampledPoints)
```
Allocates ring arrays, fills vertex/UV/triangle buffers, assigns to `MeshFilter`.

---

## 3. RoadSegmentPool.cs

**What it does:** Manages a fixed pool of `MeshFilter + MeshRenderer + MeshCollider` GameObjects. Each pooled segment covers a fixed arc length (e.g. 40 m of spline). As the player moves forward:

- **Spawn ahead:** Dequeue a free segment, call `ProceduralRoadMesher.RebuildMesh` with the next 40 m of spline samples, position it.
- **Recycle behind:** When a segment's end point is more than N metres behind the player, return it to the free queue.

Pool size = `ceil(totalVisibleRoadLength / segmentArcLength) + 2` (double buffer).

This replaces the current `EndlessTrackManager` transform-based approach with actual curved, meshed road geometry.

---

## 4. RoadCurveGenerator.cs

**What it does:** Procedurally generates the next `SplineNode` as the player advances. Implements the "feel" of the road.

Parameters exposed in Inspector:
```
[Header("Curvature")]
float minYaw = -6f             // degrees per node
float maxYaw = 6f
float yawSmoothing = 0.18f     // lerp factor (low = sweeping, high = sharp)
float maxAccumulatedYaw = 40f  // prevents spiral

[Header("Banking")]
float bankPerDegreeYaw = 0.4f  // auto-bank from curve amount
float maxBankAngle = 12f

[Header("Width")]
float roadWidth = 5f           // constant for now, later varies by biome

[Header("Difficulty")]
float difficultyT = 0f         // 0..1, driven externally by game state
```

**Difficulty curve:** As `difficultyT` increases (distance driven):
- Yaw range widens → tighter corners
- Banking can increase → more camber
- Width narrows slightly → less room to drift

**Seed:** Uses Unity `Random.InitState(seed)` so runs are reproducible during testing.

---

## 5. RoadVisuals.cs

**What it does:** Manages materials and shader properties:

- `_TileDistance` on asphalt material — scrolled via `MaterialPropertyBlock` each frame to animate dashes
- Kerb alternating color — driven by V-coordinate in shader using `fmod(v, kerbStripeLength)`
- Centre line dash pattern — UV offset per frame for "moving road" feel

Shader approach: Use URP Lit with custom tiling/offset, or a simple custom graph in Shader Graph.

---

## 6. Integration with Existing Code

Current `EndlessTrackManager.cs` uses rigid segment GameObjects with no mesh generation. The new system **replaces** it:

| Old | New |
|-----|-----|
| `EndlessTrackManager` | `RoadSegmentPool` (orchestrator) |
| `roadTemplate` GO | Procedurally meshed segment |
| Yaw offset per segment | `RoadCurveGenerator` continuous spline |
| No banking | Per-node banking from `SplineNode.banking` |
| No cross-section | Full kerb + asphalt + markings cross-section |

The `HyperDriftCarController` stays on the spline implicitly — the road surface is physical geometry so `WheelCollider` works unchanged.

---

## 7. File Plan

```
Assets/
  Scripts/
    Road/
      SplineRoadBuilder.cs
      ProceduralRoadMesher.cs
      RoadSegmentPool.cs
      RoadCurveGenerator.cs
      RoadVisuals.cs
  Materials/
    Road/
      M_Asphalt.mat
      M_CentreLine.mat
      M_Kerb.mat
```

---

## 8. Open Loops From AI Docs (NEXT_STEPS alignment)

The road system directly unblocks or touches these items:

| NEXT_STEPS item | Road system impact |
|---|---|
| Endless road spawning must avoid visible seams | Spline + continuous meshing eliminates seams |
| Lane-free traffic patterns that fit road curvature | `RoadCurveGenerator` exports curvature data for traffic steering |
| Difficulty scaling (road curvature increases) | `difficultyT` param in `RoadCurveGenerator` |
| Biomes (Hills, Mountains, City) | Node pitch + banking variation per biome zone |

Items **not** addressed by the road system (still open):
- Traffic spawning with pooling
- Near-miss detection
- Drift combo and score HUD
- Run state machine (start / fail / restart / revive)
- Touch steering feel tuning
- Mobile pause and restart flow

---

## 9. Implementation Order

1. **`SplineRoadBuilder`** — pure data, no Unity dependencies, easy to unit test
2. **`ProceduralRoadMesher`** — build flat road first (no banking), validate visually
3. **`RoadCurveGenerator`** — hook up to mesher, confirm curves feel good in editor play
4. **`RoadSegmentPool`** — replace `EndlessTrackManager`, confirm no seams at segment joins
5. **`RoadVisuals`** — add materials, UV scrolling, kerb colours
6. **Banking pass** — enable `SplineNode.banking`, re-test `WheelCollider` on camber

---

## 10. Performance Budget (Mobile Target)

| Item | Target |
|---|---|
| Active road mesh vertices | < 3 000 per segment |
| Active segments in scene | 8–10 |
| Mesh rebuilds per frame | 0–1 (only on segment swap) |
| Draw calls (road) | 3 (one per submesh material, GPU instanced) |
| MeshCollider update | Only when segment activated, not every frame |

Technique: Pre-allocate vertex arrays at pool init (avoid GC), use `Mesh.SetVertices(NativeArray)` if targeting Unity 2021+.

---

## 11. Quick-Start: Minimal Viable Road (Phase 1)

To get a visual result fast without the full system:

1. Create `SplineRoadBuilder.cs` with 4-node circular buffer
2. Create `ProceduralRoadMesher.cs` generating only the asphalt submesh (no kerbs)
3. Wire to a single persistent `MeshFilter` that rebuilds every N metres
4. Confirm car drives on the surface
5. Then layer in kerbs, banking, pooling

This gives a playable curved road in ~200 lines of code before the full architecture is complete.

---

## 12. Implementation Status

### Scripts written — `Assets/Scripts/Road/`

| File | Purpose | Status |
|---|---|---|
| `SplineRoadBuilder.cs` | Catmull-Rom spline data structure, pure C# | Done |
| `RoadCurveGenerator.cs` | Procedural node generation with difficulty scaling | Done |
| `ProceduralRoadMesher.cs` | Cross-section extrusion → Unity Mesh (2 submeshes) | Done |
| `RoadSegmentPool.cs` | Pool orchestrator, replaces EndlessTrackManager | Done |
| `RoadVisuals.cs` | UV-scroll asphalt + kerb materials | Done |

### Setup steps in Unity Editor

1. Open `DriftEndless` scene.
2. Create an empty GameObject `RoadSystem`.
3. Add `RoadSegmentPool` component to it.
4. Assign `Player` reference (the PlayerCar transform).
5. Create 2 materials: `M_Asphalt` and `M_Kerb`, assign to `Road Materials` array.
6. Add `RoadVisuals` to the same GO, assign both materials.
7. Disable (or delete) the old `TrackManager` + `RoadTemplate` GameObjects.
8. Press Play — curved banked road should stream ahead of the car.

### Tuning starting point
- `Segment Arc Length` = 40 m, `Pool Size` = 10 covers ~400 m visible road.
- `Min/Max Yaw` = ±5° for gentle intro curves; raise to ±10° at `DifficultyT = 1`.
- `Samples Per Segment` = 20 gives smooth curves at low vertex cost (~180 verts/segment).

### Known gaps (next pass)
- Centre line dashes need a third submesh or a shader-driven pattern on submesh 0.
- `MeshCollider` rebuild is synchronous — move to `Physics.BakeMesh` + job if hitching.
- Biome variation (pitch, width change) not yet wired to game state beyond `DifficultyT`.
