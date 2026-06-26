using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the pause screen (<c>Pause.uxml</c>). Visibility is owned by
/// <see cref="UIRoot"/> (shown on <see cref="GameSignals.Paused"/>, hidden on
/// Resumed/RunStarted); this presenter only wires the buttons to UI intents and
/// defensively hides its own root when the pause state ends.
///
/// Element contract (must match Pause.uxml):
///   Button "dt-resume-btn"        — RaiseResumeRequested
///   Button "dt-restart-btn-pause" — RaiseRestartRequested
///   Button "dt-quit-btn"          — RaiseQuitRequested
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PausePresenter : MonoBehaviour
{
    private UIDocument document;
    private Button resumeButton;
    private Button restartButton;
    private Button quitButton;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        VisualElement root = document.rootVisualElement;

        resumeButton = root.Q<Button>("dt-resume-btn");
        restartButton = root.Q<Button>("dt-restart-btn-pause");
        quitButton = root.Q<Button>("dt-quit-btn");

        if (resumeButton != null) resumeButton.clicked += OnResumeClicked;
        if (restartButton != null) restartButton.clicked += OnRestartClicked;
        if (quitButton != null) quitButton.clicked += OnQuitClicked;

        GameSignals.Resumed += OnHide;
        GameSignals.RunStarted += OnHide;
    }

    private void OnDisable()
    {
        if (resumeButton != null) resumeButton.clicked -= OnResumeClicked;
        if (restartButton != null) restartButton.clicked -= OnRestartClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;

        GameSignals.Resumed -= OnHide;
        GameSignals.RunStarted -= OnHide;
    }

    // ── Button intents ──────────────────────────────────────────────────────
    private void OnResumeClicked() => GameSignals.RaiseResumeRequested();
    private void OnRestartClicked() => GameSignals.RaiseRestartRequested();
    private void OnQuitClicked() => GameSignals.RaiseQuitRequested();

    /// <summary>
    /// Defensive self-hide. UIRoot already toggles document visibility; this is
    /// a harmless belt-and-braces in case a presenter is used standalone.
    /// </summary>
    private void OnHide()
    {
        if (document != null && document.rootVisualElement != null)
        {
            document.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}
