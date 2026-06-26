# Drift Therapy - UI Toolkit Plan

> Canonical UI plan for Drift Therapy. All first-party game UI should use Unity
> UI Toolkit (`UIDocument`, UXML, USS, and C# presenters), built on the
> `sinanata/unity-ui-document-design-system` package as the shared design
> system. Legacy ACC_Lite Canvas/TMP UI remains vendor/demo reference only.

References:
- Design system showcase: https://sinanata.github.io/unity-ui-document-design-system/
- Repository: https://github.com/sinanata/unity-ui-document-design-system

> **Status (2026-06-26):** the design system is **vendored** into
> `Assets/DesignSystem/` (copy approach per §2.2 — tokens, 15 USS modules,
> 63 SVG icons, `DesignSystemRuntime.cs`, MIT license). It imports on next Unity
> open; confirm a clean compile then. §2 steps 1–2 are therefore done.
> The Unity MCP bridge was **offline** at last check (`read_console` →
> "No Unity Editor instances found"), so in-editor verification and screen
> building are pending a live bridge — see `UNITY_MCP_COPLAY.md`.

---

## 1. UI Principles

- **UI Toolkit only for game UI.** New HUD, menus, end screens, pause, settings,
  revive, store, consent, and onboarding screens must be `UIDocument` based.
- **Design-system first.** Use design tokens, component classes, icons, mobile
  rules, and runtime helper from `Assets/DesignSystem/` instead of one-off USS.
- **Portrait-first, touch-first.** The game targets mobile portrait play; roots
  should opt into `.mobile` sizing where appropriate and honor safe areas.
- **Gameplay stays readable.** In-run UI must be sparse, high contrast, and
  anchored to safe zones without blocking the road, car, traffic, or drift line.
- **Event-driven presenters.** UI reads game state through events/view models
  from `RunStateMachine`, `ScoreSystem`, ads/IAP, settings, and save services.
  Avoid gameplay polling from UI components.
- **No vendor UI coupling.** ACC_Lite Canvas/TMP controls are not the app UI
  foundation. Reuse visual/audio/gameplay systems where useful, but replace
  demo UI surfaces with first-party Toolkit documents.

---

## 2. Design System Adoption

The referenced design system is a drop-in Unity 6 UI Toolkit library using:

- `UIDocument`, UXML, and USS.
- A master `DesignSystem.uss` with imported token/component styles.
- Runtime support via `DesignSystemRuntime.cs`.
- The `.ds-root` screen root convention.
- Tokenized colors, radii, spacing, motion, and typography.
- Components for buttons, inputs, tabs, toggles, checks, sliders, progress,
  modals, toasts, empty states, bottom sheets, dialogs, steppers, pagination,
  loading states, notification badges, avatars, navigation, and scrollbars.
- SVG icon classes and tinting through UI Toolkit background image tint.
- A `.mobile` root class for touch target and layout scaling.

Recommended integration for this project:

1. Vendor the repository outside `Assets/`, e.g.
   `Vendor/unity-ui-document-design-system`.
2. Link or copy only `Assets/DesignSystem` into this Unity project.
3. Keep `Assets/DesignSystem` isolated from Drift Therapy game-specific styles.
4. Add Drift Therapy overrides in `Assets/UI/Styles/DriftTheme.uss`.
5. Import the master stylesheet first, then project overrides.

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <Style src="project://database/Assets/DesignSystem/Resources/UI/Styles/DesignSystem/DesignSystem.uss" />
  <Style src="project://database/Assets/UI/Styles/DriftTheme.uss" />
  <ui:VisualElement class="ds-root drift-root mobile">
    <!-- Screen content -->
  </ui:VisualElement>
</ui:UXML>
```

Use the design system as a foundation, not a visual prison: Drift Therapy should
override palette, typography scale, and HUD-specific spacing to feel vibrant,
stylized, and flow-state oriented rather than generic dark app chrome.

---

## 3. Proposed UI Folder Structure

```text
Assets/
  DesignSystem/                         # Drop-in third-party design system
  UI/
    Documents/
      HUD.uxml
      EndRun.uxml
      Pause.uxml
      Settings.uxml
      Revive.uxml
      Store.uxml
      Onboarding.uxml
    Styles/
      DriftTheme.uss                    # Game token overrides and screen polish
      HUD.uss
      EndRun.uss
      Pause.uss
      Settings.uss
      Revive.uss
      Store.uss
      Onboarding.uss
    PanelSettings/
      MobilePanelSettings.asset
    Icons/
      drift.svg
      near-miss.svg
      multiplier.svg
      revive.svg
      no-ads.svg
  Scripts/
    UI/
      DriftUIDocumentBootstrap.cs
      SafeAreaUtility.cs
      HUDPresenter.cs
      EndRunPresenter.cs
      PausePresenter.cs
      SettingsPresenter.cs
      RevivePresenter.cs
      StorePresenter.cs
      OnboardingPresenter.cs
```

Screen UXML owns hierarchy. Screen USS owns layout and visual styling. Presenter
C# owns binding, events, localization keys, visibility, and input callbacks.

---

## 4. UI Architecture

### Runtime Layer

- A scene-level `UIDocument` can host one root UI shell, or separate documents
  can be used for HUD and modal overlays. Prefer one root document for P0/P1 to
  keep ordering and safe-area handling simple.
- `DriftUIDocumentBootstrap` should:
  - Ensure the document root has `.ds-root`, `.drift-root`, and `.mobile`.
  - Apply safe-area padding variables or classes.
  - Resolve common root containers by name.
  - Register global show/hide state for HUD, modals, pause, and end screen.
- The design system runtime should remain responsible for its own helper
  behavior, such as toggle knobs and animated loading states.

### Presenter Layer

Use thin presenters per screen:

- `HUDPresenter`: score, distance, multiplier, near-miss flash, pause button.
- `EndRunPresenter`: final score, best score, restart, revive entry, remove-ads.
- `PausePresenter`: resume, restart, settings.
- `SettingsPresenter`: music/SFX sliders, haptics toggle, sensitivity, quality.
- `RevivePresenter`: rewarded revive availability, loading, success/failure.
- `StorePresenter`: remove-ads, cosmetic entry points, restore purchases.
- `OnboardingPresenter`: first-run steering and drift prompts.

Presenters subscribe to game/service events and update named UI Toolkit
elements. They do not own scoring, run lifecycle, ads, IAP, or persistence.

### Game Event Contracts

Future gameplay systems should expose UI-friendly events:

- Run started, failed, restarted, paused, resumed, revive offered, revived.
- Score changed, best score changed, multiplier changed.
- Near-miss triggered, combo tier changed.
- Ad load state changed, rewarded completed/canceled/failed.
- Settings changed and saved.

---

## 5. Screen Plan

### P0 Screens

1. **HUD**
   - Score, distance, multiplier, minimal pause affordance.
   - Optional transient near-miss/combo text.
   - No large cards during gameplay.

2. **End run**
   - Final score, best score, instant restart.
   - Placeholder slot for rewarded revive, disabled until monetization exists.
   - Remove-ads entry point may appear later when IAP is active.

3. **Ready/start overlay**
   - Minimal tap/hold prompt or countdown, hidden immediately on run start.

### P1 Screens

4. **Pause**
   - Resume, restart, settings.

5. **Settings**
   - Music, SFX, haptics, sensitivity, quality.

6. **Revive**
   - One rewarded revive per run, clear unavailable/loading/error states.

7. **Onboarding**
   - First-run touch/drift coaching with short prompts, not a text-heavy manual.

### P2/P3 Screens

8. **Store/cosmetics**
   - Remove-ads, cosmetic car browsing, restore purchases.

9. **Missions/daily rewards**
   - Built from the same UI Toolkit components and presenters.

---

## 6. Visual Direction

Base design-system tokens should be rethemed for Drift Therapy:

- Dark translucent surfaces for pause/end/settings overlays.
- High-energy accent colors tied to biome/multiplier state.
- Large numeric HUD typography with strong contrast.
- Minimal borders and restrained panels in gameplay.
- Motion feedback for score/multiplier changes, near-miss pulses, and fail state.
- Touch targets at least 48 px in mobile mode.

Avoid heavy nested cards, excessive menu chrome, or UI that covers the driving
line. The HUD should feel like a light overlay on a fast, readable playfield.

---

## 7. Implementation Order

1. Import or link the design system.
2. Create `Assets/UI/` folder structure and `MobilePanelSettings.asset`.
3. Add `DriftTheme.uss` with Drift Therapy token overrides.
4. Build the P0 `HUD.uxml` + `HUD.uss`.
5. Build the P0 `EndRun.uxml` + `EndRun.uss`.
6. Add `DriftUIDocumentBootstrap`, `HUDPresenter`, and `EndRunPresenter`.
7. Wire presenters to `RunStateMachine` and `ScoreSystem` once those exist.
8. Add pause/settings/revive after the core loop is playable.
9. Replace or remove ACC_Lite demo Canvas UI from the canonical scene.
10. Add PlayMode/UI smoke tests for document loading and button flows.

---

## 8. Testing and QA

- Verify every UXML loads with the design-system USS attached.
- Verify `.mobile` layout on common portrait aspect ratios and safe areas.
- Verify HUD text does not overlap gameplay-critical regions.
- Verify buttons can be tapped by touch and keyboard/gamepad focus where useful.
- Verify restart, pause, settings, and revive flows do not soft-lock run state.
- Verify no Console errors after entering Play Mode.
- Verify UI changes do not allocate every frame in the running HUD.

---

## 9. Risks

- **Design system import drift:** follow the repo's import guidance; avoid
  importing the showcase/demo project into `Assets/`.
- **Duplicate GUIDs:** if using a submodule, link only `Assets/DesignSystem` and
  keep the host repository outside Unity's asset import path.
- **Canvas leftovers:** vendor ACC_Lite UI may still exist in prefabs/scenes; keep
  it isolated until replaced in the canonical scene.
- **Mobile safe-area mistakes:** test on notched devices early.
- **Over-styled HUD:** gameplay readability beats component showcase fidelity.
