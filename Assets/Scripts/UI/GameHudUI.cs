using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Binds an authored HUD prefab (HUD + Pause + End panels) to gameplay signals.
    /// Does NOT build UI — element references are assigned on the prefab. Buttons
    /// raise <see cref="GameSignals"/> intents the <see cref="GameController"/> acts on.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameHudUI : MonoBehaviour
    {
        [Header("HUD")]
        public TMP_Text scoreText, distanceText, multiplierText, driftCoinsText, driftCoinsPendingText, countdownText, nearMissText;
        public RectTransform healthFill, boostFill;
        public Button pauseButton, boostButton;

        [Header("Pause")]
        public GameObject pausePanel;
        public Button resumeButton, restartButton, homeButton;

        [Header("End")]
        public GameObject endPanel;
        public TMP_Text endScore, endBest, endCoins, endXp, endNewBest;
        public Button retryButton, endHomeButton;

        GameController controller;

        void Awake() => controller = FindFirstObjectByType<GameController>();

        void OnEnable()
        {
            Bind(pauseButton, () => GameSignals.RaisePauseToggled());
            Bind(boostButton, () => { if (controller) controller.ActivateBoost(); });
            Bind(resumeButton, () => GameSignals.RaiseResumeRequested());
            Bind(restartButton, () => GameSignals.RaiseRestartRequested());
            Bind(retryButton, () => GameSignals.RaiseRestartRequested());
            Bind(homeButton, GoHome);
            Bind(endHomeButton, GoHome);

            GameSignals.ScoreChanged      += OnScore;
            GameSignals.DistanceChanged   += OnDistance;
            GameSignals.MultiplierChanged += OnMultiplier;
            GameSignals.DriftCoinsChanged += OnDriftCoins;
            GameSignals.HealthChanged     += OnHealth;
            GameSignals.BoostChanged      += OnBoost;
            GameSignals.CountdownTick     += OnCountdownTick;
            GameSignals.CountdownGo        += OnCountdownGo;
            GameSignals.NearMiss          += OnNearMiss;
            GameSignals.RunStarted        += ShowHud;
            GameSignals.RunReady          += ShowHud;
            GameSignals.Resumed           += ShowHud;
            GameSignals.Revived           += ShowHud;
            GameSignals.RestartRequested  += ShowHud;
            GameSignals.Paused            += ShowPause;
            GameSignals.RunFailed         += OnRunFailed;

            ShowHud();
            Hide(scoreText);
            Hide(distanceText);
            Hide(multiplierText);
            Hide(driftCoinsPendingText);
            Hide(nearMissText);
            Hide(countdownText);
        }

        void OnDisable()
        {
            GameSignals.ScoreChanged      -= OnScore;
            GameSignals.DistanceChanged   -= OnDistance;
            GameSignals.MultiplierChanged -= OnMultiplier;
            GameSignals.DriftCoinsChanged -= OnDriftCoins;
            GameSignals.HealthChanged     -= OnHealth;
            GameSignals.BoostChanged      -= OnBoost;
            GameSignals.CountdownTick     -= OnCountdownTick;
            GameSignals.CountdownGo        -= OnCountdownGo;
            GameSignals.NearMiss          -= OnNearMiss;
            GameSignals.RunStarted        -= ShowHud;
            GameSignals.RunReady          -= ShowHud;
            GameSignals.Resumed           -= ShowHud;
            GameSignals.Revived           -= ShowHud;
            GameSignals.RestartRequested  -= ShowHud;
            GameSignals.Paused            -= ShowPause;
            GameSignals.RunFailed         -= OnRunFailed;
        }

        static void Bind(Button b, System.Action a) { if (b) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => a()); } }
        static void Hide(Component c) { if (c) c.gameObject.SetActive(false); }
        static void Show(Component c) { if (c) c.gameObject.SetActive(true); }
        static void Fill(RectTransform fill, float v) { if (fill) { var a = fill.anchorMax; a.x = Mathf.Clamp01(v); fill.anchorMax = a; } }

        void OnScore(int cur, int best) { Hide(scoreText); }
        void OnDistance(float m) { Hide(distanceText); }
        void OnDriftCoins(int total, int pending)
        {
            if (driftCoinsText) driftCoinsText.text = total.ToString();

            if (driftCoinsPendingText)
            {
                bool showPending = pending > 0;
                driftCoinsPendingText.gameObject.SetActive(showPending);
                if (showPending) driftCoinsPendingText.text = "+" + pending;
            }
        }
        void OnHealth(float h01) { Fill(healthFill, h01); }
        void OnBoost(float b01) { Fill(boostFill, b01); if (boostButton) boostButton.interactable = b01 >= 0.999f; }

        void OnMultiplier(float mult, int combo)
        {
            if (!multiplierText) return;
            bool show = combo > 0 && mult > 1.0001f;
            multiplierText.gameObject.SetActive(show);
            if (show) multiplierText.text = "x" + mult.ToString("0.0");
        }

        void OnCountdownTick(int s) { if (!countdownText) return; CancelInvoke(nameof(HideCountdown)); countdownText.text = s.ToString(); Show(countdownText); }
        void OnCountdownGo() { if (!countdownText) return; countdownText.text = "GO!"; Show(countdownText); CancelInvoke(nameof(HideCountdown)); Invoke(nameof(HideCountdown), 0.7f); }
        void HideCountdown() => Hide(countdownText);
        void OnNearMiss() { if (!nearMissText) return; Show(nearMissText); CancelInvoke(nameof(HideNearMiss)); Invoke(nameof(HideNearMiss), 0.6f); }
        void HideNearMiss() => Hide(nearMissText);

        void ShowHud()
        {
            Time.timeScale = 1f;
            if (pausePanel) pausePanel.SetActive(false);
            if (endPanel) endPanel.SetActive(false);
            Hide(scoreText);
            Hide(distanceText);
            Hide(multiplierText);
            Hide(driftCoinsPendingText);
        }
        void ShowPause() { if (pausePanel) pausePanel.SetActive(true); if (endPanel) endPanel.SetActive(false); }

        void OnRunFailed(string reason)
        {
            if (pausePanel) pausePanel.SetActive(false);
            if (endPanel) endPanel.SetActive(true);
            Hide(scoreText);
            Hide(distanceText);
            Hide(multiplierText);
            Hide(driftCoinsPendingText);
            Time.timeScale = 0f;
            var r = GameApp.Instance != null ? GameApp.Instance.LastRun : default;
            int distanceMeters = Mathf.Max(0, Mathf.FloorToInt(r.distanceMeters));
            int bestDistanceMeters = Mathf.Max(0, Mathf.FloorToInt(r.bestDistanceMeters));
            if (endScore) endScore.text = distanceMeters + " m";
            if (endBest) endBest.text = "BEST " + bestDistanceMeters + " m";
            if (endNewBest) endNewBest.gameObject.SetActive(r.isNewBest && distanceMeters > 0);
            if (endCoins) endCoins.text = "+" + r.coins;
            if (endXp) endXp.text = "+" + r.xp + " XP";
        }

        void GoHome() { Time.timeScale = 1f; GameSignals.RaiseQuitRequested(); SceneFlow.GoToMenu(); }
    }
}
