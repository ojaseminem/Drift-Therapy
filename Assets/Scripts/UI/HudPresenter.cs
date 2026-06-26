using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds the in-run HUD (<c>HUD.uxml</c>) to <see cref="GameSignals"/> score /
/// distance / multiplier / near-miss events, and raises the pause intent when
/// the pause button is tapped.
///
/// Element contract (must match HUD.uxml):
///   Label  "dt-score"       — current run score
///   Label  "dt-distance"    — distance, formatted "1234 m"
///   Label  "dt-multiplier"  — drift multiplier, "x2.5"; hidden at x1.0 / combo 0
///   Label  "dt-nearmiss"    — transient flash, hidden by default
///   Button "dt-pause-btn"   — raises PauseToggled
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HudPresenter : MonoBehaviour
{
    [Tooltip("Milliseconds the near-miss flash stays visible.")]
    [SerializeField] private long nearMissFlashMs = 600;

    private UIDocument document;
    private Label scoreLabel;
    private Label distanceLabel;
    private Label multiplierLabel;
    private Label nearMissLabel;
    private Button pauseButton;

    /// <summary>Handle to the pending hide-flash task so repeated near-misses reset the timer.</summary>
    private IVisualElementScheduledItem nearMissHideTask;

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        VisualElement root = document.rootVisualElement;

        scoreLabel = root.Q<Label>("dt-score");
        distanceLabel = root.Q<Label>("dt-distance");
        multiplierLabel = root.Q<Label>("dt-multiplier");
        nearMissLabel = root.Q<Label>("dt-nearmiss");
        pauseButton = root.Q<Button>("dt-pause-btn");

        if (pauseButton != null)
        {
            pauseButton.clicked += OnPauseClicked;
        }

        GameSignals.ScoreChanged += OnScoreChanged;
        GameSignals.DistanceChanged += OnDistanceChanged;
        GameSignals.MultiplierChanged += OnMultiplierChanged;
        GameSignals.NearMiss += OnNearMiss;

        // Reset to a clean visual state.
        SetText(scoreLabel, "0");
        SetText(distanceLabel, "0 m");
        Hide(nearMissLabel);
        Hide(multiplierLabel);
    }

    private void OnDisable()
    {
        if (pauseButton != null)
        {
            pauseButton.clicked -= OnPauseClicked;
        }

        GameSignals.ScoreChanged -= OnScoreChanged;
        GameSignals.DistanceChanged -= OnDistanceChanged;
        GameSignals.MultiplierChanged -= OnMultiplierChanged;
        GameSignals.NearMiss -= OnNearMiss;

        nearMissHideTask = null;
    }

    private void OnPauseClicked()
    {
        GameSignals.RaisePauseToggled();
    }

    // ── Signal handlers ─────────────────────────────────────────────────────
    private void OnScoreChanged(int current, int best)
    {
        SetText(scoreLabel, current.ToString(CultureInfo.InvariantCulture));
    }

    private void OnDistanceChanged(float metres)
    {
        int m = Mathf.Max(0, Mathf.FloorToInt(metres));
        SetText(distanceLabel, m.ToString(CultureInfo.InvariantCulture) + " m");
    }

    private void OnMultiplierChanged(float multiplier, int comboCount)
    {
        // Hide the badge at rest (x1.0 / no combo); show it once a combo builds.
        if (comboCount <= 0 || multiplier <= 1.0001f)
        {
            Hide(multiplierLabel);
            return;
        }

        SetText(multiplierLabel, "x" + multiplier.ToString("0.0", CultureInfo.InvariantCulture));
        Show(multiplierLabel);
    }

    private void OnNearMiss()
    {
        if (nearMissLabel == null)
        {
            return;
        }

        Show(nearMissLabel);

        // Restart the hide timer on every near-miss so rapid misses keep it lit.
        nearMissHideTask?.Pause();
        nearMissHideTask = nearMissLabel.schedule
            .Execute(() => Hide(nearMissLabel))
            .StartingIn(nearMissFlashMs);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static void SetText(Label label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    private static void Show(VisualElement el)
    {
        if (el != null)
        {
            el.RemoveFromClassList("dt-hidden");
            el.style.display = DisplayStyle.Flex;
        }
    }

    private static void Hide(VisualElement el)
    {
        if (el != null)
        {
            el.style.display = DisplayStyle.None;
        }
    }
}
