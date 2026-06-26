using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Drives the game-over screen (<c>EndRun.uxml</c>).
///
/// On <see cref="GameSignals.RunFailed"/> it fills the final/best score, toggles
/// the "New Best!" badge, and (initially) hides the revive button. When a revive
/// becomes available via <see cref="GameSignals.ReviveOffered"/>, the revive
/// button is shown and enabled. Button taps raise the matching UI intents.
///
/// NOTE: This presenter only fills data + manages the revive button. The screen
/// is shown/hidden by <see cref="UIRoot"/> based on the run lifecycle; here we
/// also defensively hide our own root on RunStarted/Revived so stale data never
/// flashes when a new run begins.
///
/// Element contract (must match EndRun.uxml):
///   Label  "dt-final-score"    — final score this run
///   Label  "dt-best-score"     — best score
///   Label  "dt-newbest-badge"  — "New Best!" pill, hidden unless final==best&gt;0
///   Button "dt-restart-btn"    — RaiseRestartRequested
///   Button "dt-revive-btn"     — RaiseReviveRequested (hidden/disabled until offered)
///   Button "dt-removeads-btn"  — monetization placeholder (TODO)
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class EndRunPresenter : MonoBehaviour
{
    private UIDocument document;
    private Label finalScoreLabel;
    private Label bestScoreLabel;
    private Label newBestBadge;
    private Button restartButton;
    private Button reviveButton;
    private Button removeAdsButton;

    /// <summary>Latest score/best seen via ScoreChanged; used to fill the end screen at fail time.</summary>
    private int lastScore;
    private int lastBest;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        VisualElement root = document.rootVisualElement;

        finalScoreLabel = root.Q<Label>("dt-final-score");
        bestScoreLabel = root.Q<Label>("dt-best-score");
        newBestBadge = root.Q<Label>("dt-newbest-badge");
        restartButton = root.Q<Button>("dt-restart-btn");
        reviveButton = root.Q<Button>("dt-revive-btn");
        removeAdsButton = root.Q<Button>("dt-removeads-btn");

        if (restartButton != null) restartButton.clicked += OnRestartClicked;
        if (reviveButton != null) reviveButton.clicked += OnReviveClicked;
        if (removeAdsButton != null) removeAdsButton.clicked += OnRemoveAdsClicked;

        GameSignals.ScoreChanged += OnScoreChanged;
        GameSignals.RunFailed += OnRunFailed;
        GameSignals.ReviveOffered += OnReviveOffered;
        GameSignals.RunStarted += OnRunStartedOrRevived;
        GameSignals.Revived += OnRunStartedOrRevived;

        // Default: revive not available until offered.
        HideRevive();
    }

    private void OnDisable()
    {
        if (restartButton != null) restartButton.clicked -= OnRestartClicked;
        if (reviveButton != null) reviveButton.clicked -= OnReviveClicked;
        if (removeAdsButton != null) removeAdsButton.clicked -= OnRemoveAdsClicked;

        GameSignals.ScoreChanged -= OnScoreChanged;
        GameSignals.RunFailed -= OnRunFailed;
        GameSignals.ReviveOffered -= OnReviveOffered;
        GameSignals.RunStarted -= OnRunStartedOrRevived;
        GameSignals.Revived -= OnRunStartedOrRevived;
    }

    // ── Signal handlers ─────────────────────────────────────────────────────

    /// <summary>Cache the running score so we have it ready when the run fails.</summary>
    private void OnScoreChanged(int current, int best)
    {
        lastScore = current;
        lastBest = best;
    }

    private void OnRunFailed(string reason)
    {
        // reason is available for future telemetry / contextual copy.
        int final = lastScore;
        int best = lastBest;

        SetText(finalScoreLabel, final.ToString(CultureInfo.InvariantCulture));
        SetText(bestScoreLabel, best.ToString(CultureInfo.InvariantCulture));

        bool isNewBest = final > 0 && final == best;
        SetVisible(newBestBadge, isNewBest);

        // Each new game-over starts with revive hidden; ReviveOffered re-shows it.
        HideRevive();
    }

    private void OnReviveOffered()
    {
        if (reviveButton == null)
        {
            return;
        }

        reviveButton.SetEnabled(true);
        SetVisible(reviveButton, true);
    }

    private void OnRunStartedOrRevived()
    {
        // Reset transient bits so the screen is clean next time it shows.
        SetVisible(newBestBadge, false);
        HideRevive();
    }

    // ── Button intents ──────────────────────────────────────────────────────
    private void OnRestartClicked() => GameSignals.RaiseRestartRequested();
    private void OnReviveClicked() => GameSignals.RaiseReviveRequested();

    private void OnRemoveAdsClicked()
    {
        // TODO(monetization): wire up the IAP "remove ads" flow when the store
        // layer lands. For now this is a no-op placeholder so the button exists
        // in the layout without triggering an unintended quit.
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void HideRevive()
    {
        if (reviveButton != null)
        {
            reviveButton.SetEnabled(false);
            SetVisible(reviveButton, false);
        }
    }

    private static void SetText(Label label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    private static void SetVisible(VisualElement el, bool visible)
    {
        if (el == null)
        {
            return;
        }
        el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
