# Drift Therapy — Art Plan

> Art direction, asset pipeline, and production plan. Aligned to the GDD visual
> target: **stylized, vibrant, lowpoly**, portrait, semi top-down / over-shoulder.

---

## 1. Art direction

**Pillars**
- **Readable first.** In portrait at speed, the player must instantly parse road
  edges, traffic, and drift state. Silhouette and color contrast beat detail.
- **Vibrant & stylized.** Saturated, flat-ish lowpoly with strong rim/ambient
  light. No photoreal textures; lean on color, gradient skies, and lighting.
- **Motion-forward.** The art sells *speed and flow*: speed lines, smoke, skid
  marks, camera tilt, biome color shifts.
- **Calm-but-energetic ("therapy").** The name implies a flow-state vibe —
  pleasing palettes, smooth transitions, satisfying feedback over chaos.

**Camera consequence:** because the camera is semi top-down/over-shoulder, the
**tops and upper sides** of cars and the **road surface** get the most screen
time. Budget detail there; under-detail undersides and far geometry.

---

## 2. Visual targets by element

| Element | Direction | Notes |
|---|---|---|
| Player car | Hero asset, clean lowpoly, bold color, clear roof read | Base on ACC_Lite `SunLineGTE_Drift`; restyle, simplify |
| Traffic cars/trucks | Distinct silhouettes & colors vs player; muted so player pops | 2–3 archetypes, recolor variants |
| Road | High-contrast edges, lane/kerb markings readable from above | `M_Asphalt`, `M_Kerb` already in project |
| Biomes | Each = distinct palette + sky + fog + accent props | Hills, Mountains, Bridges, City outskirts/alleys |
| VFX | Tire smoke, skid marks, speed lines, near-miss flash, crash burst | `FXController` exists in ACC_Lite |
| UI | Unity UI Toolkit, minimal, bold, large tap targets, portrait-first | See `UI_TOOLKIT_PLAN.md`; score/distance/multiplier + end screen |
| Lighting | Stylized URP, biome-driven via `RoadBiomeManager` | Crossfades fog/sky/post per distance |

---

## 3. Biome art (system is ready, content is empty)

`RoadBiomeManager` + `BiomeData` already cross-fade materials, fog, sky, and
post-processing by distance — **but no `BiomeData` assets exist yet**. Highest-
leverage art task after the core loop: author biomes.

Per biome, define: ground/road palette, sky/gradient, fog color+density,
post-processing profile, accent prop set, and `startDistance`.

Suggested first three (ship order): **City outskirts/alleys** (golden hour),
**Hills** (green→amber dusk), **Bridge** (blue hour, water/skyline). Author as
`Assets → Create → Drift Therapy → Biome Data`.

---

## 4. VFX plan

Reuse ACC_Lite's `FXController` (smoke, skid trail, backfire) and wire to game
events, then extend:

- **Drift smoke** — intensity scales with drift angle/combo.
- **Skid marks** — persistent decals during drift; fade over distance.
- **Speed lines** — screen-edge streaks scaling with speed/combo.
- **Near-miss** — brief flash/whoosh + light bloom pulse on close pass.
- **Crash** — impact burst, debris pop, hit-stop + shake.
- **Combo escalation** — color/particle tiers as multiplier climbs.

Keep all VFX pooled and URP-batched; mobile particle budgets are tight.

---

## 5. UI art

- Use Unity UI Toolkit (`UIDocument`, UXML, USS) for all first-party game UI.
- Build on the shared design system described in `UI_TOOLKIT_PLAN.md`.
- Avoid new Canvas/TMP UI for game screens; legacy ACC_Lite UI is vendor/demo
  reference only.

- Portrait-first layout; thumb-safe zones at the bottom.
- In-run HUD: large score, distance, animated multiplier — minimal chrome.
- End screen: final + best score, single bold **Restart** (instant), revive
  prompt (rewarded ad), remove-ads entry point.
- Style: flat, high-contrast, rounded, vibrant accent matching active biome.
- Use UI Toolkit text styles and USS typography tokens for crisp scalable text.

---

## 6. Asset pipeline & technical art

- **Lowpoly modeling** in Blender → FBX → Unity; shared stylized URP materials;
  texture atlases per biome to cut draw calls.
- **Naming/folders:** consolidate game art under `Assets/Art/` (Cars, Traffic,
  Road, Biomes, VFX, UI); keep ACC_Lite isolated as vendor.
- **Performance budgets (mobile portrait):**
  - Player car ≤ ~3–5k tris; traffic ≤ ~1.5–3k tris.
  - Aggressive draw-call batching (SRP Batcher / GPU instancing for traffic/road).
  - Target 60 fps on mid-tier devices; 30 fps floor on low-end.
- **Decals & particles** pooled; no per-frame allocation.
- **Lighting** baked where static; biome changes via `RoadBiomeManager` at runtime.

---

## 7. Production order (art)

1. **Restyle player car** (hero read) + confirm camera framing.
2. **Author biome #1** end-to-end (palette/sky/fog/post/props) to prove the pipeline.
3. **Traffic archetypes** (2–3) with color variants and clear silhouettes.
4. **VFX pass** (smoke, skids, speed lines, near-miss, crash).
5. **HUD + end screen** art in UI Toolkit, using `UI_TOOLKIT_PLAN.md`.
6. **Biomes #2–3.**
7. **Juice/polish** (shake, hit-stop, combo flourishes), then store assets
   (icon, screenshots, preview video).

---

## 8. Risks

- **Readability at speed in portrait** — validate every asset in-context, moving,
  on a phone, not in the editor viewport.
- **Mobile perf** — lowpoly still needs batching discipline; profile early.
- **ACC_Lite visual mismatch** — its demo cars are racing-styled; restyle rather
  than ship as-is so the look is cohesive and owned.
- **Biome pop-in** — tune `crossfadeSpeed` and `startDistance` so transitions read
  as smooth, not abrupt.
