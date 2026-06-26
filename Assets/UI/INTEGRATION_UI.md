# Drift Therapy — UI Toolkit Integration Guide

This document explains how to assemble the game UI in the Unity Editor once the
Editor/MCP bridge is back. Nothing here auto-wires; a human (or the bridge)
must create the PanelSettings asset and the scene GameObjects described below.

## Files created

UXML (`Assets/UI/Documents/`)
- `HUD.uxml` — in-run overlay (distance, multiplier badge, big score, pause, near-miss flash)
- `EndRun.uxml` — game-over modal (final/best score, New Best badge, Restart/Revive/Remove Ads)
- `Pause.uxml` — pause modal (Resume/Restart/Quit)

USS (`Assets/UI/Styles/`)
- `DriftTheme.uss` — Drift Therapy token overrides + screen layout helpers
  (HUD anchoring, big score type, near-miss flash, `.dt-hidden`, `.mobile` sizing)

C# presenters (`Assets/Scripts/UI/`)
- `UIRoot.cs` — bootstrap + screen visibility coordinator
- `HudPresenter.cs`
- `EndRunPresenter.cs`
- `PausePresenter.cs`
- `SafeAreaBinder.cs` — static safe-area → padding helper

## Architecture: three UIDocuments, one per screen

Each screen (HUD, End Run, Pause) is its **own `UIDocument`** with its **own
presenter** on the **same GameObject**. Rationale: every presenter queries only
its own document root, so element name lookups can never collide between
screens, and each screen can be authored/iterated independently. `UIRoot` owns
which screen is visible by flipping each document's
`rootVisualElement.style.display` (Flex/None).

> Alternative considered: one document hosting all three screens as container
> elements. Rejected because it couples all presenters to one DOM and makes
> name collisions (e.g. two `dt-restart-btn`) a real risk — the pause restart
> button is deliberately named `dt-restart-btn-pause` to keep that option open,
> but the shipped design uses separate documents.

### PanelSettings

1. Create one `PanelSettings` asset: `Assets/UI/UIPanelSettings.asset`
   (Project window → Create → UI Toolkit → Panel Settings Asset).
2. Recommended config for a portrait mobile hypercasual:
   - Scale Mode: **Constant Physical Size** or **Scale With Screen Size**
   - If Scale With Screen Size: Reference Resolution `1080 x 1920`,
     Screen Match Mode **Match Width Or Height**, Match = `0.5`.
   - Clear Color: leave the panel transparent so gameplay shows through.
3. All three UIDocuments reference this **same** PanelSettings asset.
4. Sort Order (PanelSettings is shared, so set per-UIDocument
   `Sort Order` field instead): HUD `0`, Pause `10`, End Run `20`, so overlays
   render above the HUD.

### Scene GameObjects

Create a `UI` GameObject hierarchy:

```
UI (empty GameObject)
├── HUD            → UIDocument (Source Asset = HUD.uxml,    PanelSettings, SortOrder 0)
│                    + HudPresenter
│                    + UIRoot   (lives here; this UIDocument is its required component)
├── EndRun         → UIDocument (Source Asset = EndRun.uxml, PanelSettings, SortOrder 20)
│                    + EndRunPresenter
└── Pause          → UIDocument (Source Asset = Pause.uxml,  PanelSettings, SortOrder 10)
                     + PausePresenter
```

Wiring in the Inspector:
- **UIRoot** (on the HUD GameObject): assign `Hud Document` (leave blank to use
  the UIDocument on the same GameObject), `End Run Document`, `Pause Document`.
- Each presenter has **no** serialized references — it auto-grabs the
  `UIDocument` on its own GameObject via `GetComponent`.
- `HudPresenter.nearMissFlashMs` (default 600) controls the flash duration.

## `.mobile` and safe area

- On `OnEnable`, `UIRoot` adds the `.mobile` class to each document root when
  `Screen.width < 768`. `DriftTheme.uss` uses `.mobile` to bump button/icon
  tap-target heights to 48px.
- `UIRoot` then calls `SafeAreaBinder.Apply(root)` per document, which writes
  `Screen.safeArea` insets as inline padding (top/left/right/bottom). It is safe
  to re-call on orientation change. The UXML already adds `.mobile` to its root
  too, so styling is correct even before the runtime pass.

## Visibility lifecycle (driven by UIRoot from GameSignals)

| Signal                         | HUD  | End Run | Pause |
|--------------------------------|------|---------|-------|
| boot / `RunReady`              | off  | off     | off   |
| `RunStarted`/`Resumed`/`Revived` | on | off     | off   |
| `RestartRequested`             | on   | off     | off   |
| `RunFailed(reason)`            | off  | on      | off   |
| `Paused`                       | on   | off     | on    |

## Named elements each presenter expects

HudPresenter (`HUD.uxml`):
- `dt-score` (Label), `dt-distance` (Label), `dt-multiplier` (Label, hidden at x1.0),
  `dt-nearmiss` (Label, hidden), `dt-pause-btn` (Button)

EndRunPresenter (`EndRun.uxml`):
- `dt-final-score` (Label), `dt-best-score` (Label), `dt-newbest-badge` (Label, hidden),
  `dt-restart-btn` (Button), `dt-revive-btn` (Button, hidden until `ReviveOffered`),
  `dt-removeads-btn` (Button)

PausePresenter (`Pause.uxml`):
- `dt-resume-btn`, `dt-restart-btn-pause`, `dt-quit-btn` (all Button)

## Gameplay-side contract notes for the GameController

- **Score on the end screen**: `EndRunPresenter` caches the latest
  `ScoreChanged(current, best)` and displays those values on `RunFailed`. Make
  sure the final `ScoreChanged` is raised **before** `RaiseRunFailed(...)` so the
  end screen shows the correct final/best. "New Best!" lights up when
  `final == best && final > 0`.
- **Multiplier badge** hides when `multiplier <= 1.0` or `comboCount <= 0`.
- **Remove Ads** is currently a no-op placeholder (see TODO in
  `EndRunPresenter.OnRemoveAdsClicked`). Wire to IAP later; it intentionally does
  **not** call `RaiseQuitRequested`.
- `RunFailed` does **not** auto-offer revive — raise `ReviveOffered` separately
  to show/enable the revive button.
