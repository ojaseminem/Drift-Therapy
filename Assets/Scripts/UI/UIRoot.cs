using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Top-level UI coordinator for Drift Therapy.
///
/// ARCHITECTURE — three separate <see cref="UIDocument"/>s, one per screen
/// (HUD, End Run, Pause). Each screen has its own presenter that queries only
/// its own document, which keeps element lookups simple and collision-free.
/// UIRoot owns visibility: it shows exactly one (or zero) overlay at a time by
/// flipping each document's <c>rootVisualElement.style.display</c>, and drives
/// that from the <see cref="GameSignals"/> run lifecycle.
///
/// Visibility matrix:
///   RunStarted / Resumed / Revived  → HUD visible, End + Pause hidden
///   RunReady                        → all overlays hidden (HUD optional, kept hidden until run starts)
///   RunFailed                       → End visible (HUD stays for backdrop-free overlay; Pause hidden)
///   Paused                          → Pause visible
///
/// MOBILE / SAFE AREA — on enable, if Screen.width &lt; 768 we add `.mobile` to
/// each document root for touch sizing, then delegate notch insets to
/// <see cref="SafeAreaBinder"/>.
///
/// Wire the three UIDocument components in the inspector (see INTEGRATION_UI.md).
/// This component requires the HUD UIDocument on its own GameObject.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIRoot : MonoBehaviour
{
    [Header("Screen documents")]
    [Tooltip("The in-run HUD. Defaults to the UIDocument on this GameObject.")]
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private UIDocument endRunDocument;
    [SerializeField] private UIDocument pauseDocument;

    private const int MobileWidthThreshold = 768;

    private void OnEnable()
    {
        if (hudDocument == null)
        {
            hudDocument = GetComponent<UIDocument>();
        }

        ApplyMobileAndSafeArea(hudDocument);
        ApplyMobileAndSafeArea(endRunDocument);
        ApplyMobileAndSafeArea(pauseDocument);

        GameSignals.RunStarted += HandleRunStarted;
        GameSignals.RunReady += HandleRunReady;
        GameSignals.RunFailed += HandleRunFailed;
        GameSignals.Paused += HandlePaused;
        GameSignals.Resumed += HandleResumed;
        GameSignals.Revived += HandleRevived;
        GameSignals.RestartRequested += HandleRestartRequested;

        // Boot state: nothing in front of the road.
        ShowOnly(null);
    }

    private void OnDisable()
    {
        GameSignals.RunStarted -= HandleRunStarted;
        GameSignals.RunReady -= HandleRunReady;
        GameSignals.RunFailed -= HandleRunFailed;
        GameSignals.Paused -= HandlePaused;
        GameSignals.Resumed -= HandleResumed;
        GameSignals.Revived -= HandleRevived;
        GameSignals.RestartRequested -= HandleRestartRequested;
    }

    // ── Lifecycle handlers ──────────────────────────────────────────────────
    private void HandleRunStarted() => ShowGameplay();
    private void HandleResumed() => ShowGameplay();
    private void HandleRevived() => ShowGameplay();
    private void HandleRestartRequested() => ShowGameplay();
    private void HandleRunReady() => ShowOnly(null);
    private void HandleRunFailed(string reason) => ShowOnly(endRunDocument);
    private void HandlePaused() => ShowOnly(pauseDocument);

    /// <summary>HUD visible, both overlays hidden.</summary>
    private void ShowGameplay()
    {
        SetVisible(hudDocument, true);
        SetVisible(endRunDocument, false);
        SetVisible(pauseDocument, false);
    }

    /// <summary>
    /// Shows the given overlay document (or none) and hides the others. The HUD
    /// is left visible behind overlays so the score/distance read through.
    /// </summary>
    private void ShowOnly(UIDocument overlay)
    {
        SetVisible(endRunDocument, overlay == endRunDocument);
        SetVisible(pauseDocument, overlay == pauseDocument);
        // Keep HUD visible whenever an overlay isn't the End screen.
        SetVisible(hudDocument, overlay != endRunDocument);
    }

    private static void SetVisible(UIDocument doc, bool visible)
    {
        if (doc == null || doc.rootVisualElement == null)
        {
            return;
        }

        doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private static void ApplyMobileAndSafeArea(UIDocument doc)
    {
        if (doc == null || doc.rootVisualElement == null)
        {
            return;
        }

        VisualElement root = doc.rootVisualElement;
        if (Screen.width < MobileWidthThreshold)
        {
            root.AddToClassList("mobile");
        }

        SafeAreaBinder.Apply(root);
    }
}
