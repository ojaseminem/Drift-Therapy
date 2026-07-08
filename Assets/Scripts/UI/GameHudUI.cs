using DG.Tweening;
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

        [Tooltip("Player car transform, for the coin-fly effect's screen-space origin. Same object as GameController.player.")]
        public Transform player;
        [Tooltip("Pooled coin-fly component, usually on the HUD Canvas root.")]
        public CoinFlyEffect coinFly;

        [Header("Pause")]
        public GameObject pausePanel;
        public Button resumeButton, restartButton, homeButton;

        [Header("End")]
        public GameObject endPanel;
        public TMP_Text endScore, endBest, endCoins, endXp, endNewBest;
        public Button retryButton, endHomeButton;
        public CanvasGroup resultBannerGroup, distanceCardGroup, rewardsGroup, retryGroup, endHomeGroup;

        GameController controller;
        int lastCommittedCoinsTotal;

        void Awake() => controller = FindFirstObjectByType<GameController>();

        /// <summary>Called by PlayerVehicleSpawner right after spawning the selected vehicle.</summary>
        public void SetPlayer(Transform player) => this.player = player;

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
            GameSignals.Collected         += OnCollected;
            GameSignals.RunStarted        += ShowHud;
            GameSignals.RunReady          += OnRunReady;
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
            GameSignals.Collected         -= OnCollected;
            GameSignals.RunStarted        -= ShowHud;
            GameSignals.RunReady          -= OnRunReady;
            GameSignals.Resumed           -= ShowHud;
            GameSignals.Revived           -= ShowHud;
            GameSignals.RestartRequested  -= ShowHud;
            GameSignals.Paused            -= ShowPause;
            GameSignals.RunFailed         -= OnRunFailed;

            DOTween.Kill("EndReveal");
        }

        static void Bind(Button b, System.Action a) { if (b) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => a()); } }
        static void Hide(Component c) { if (c) c.gameObject.SetActive(false); }
        static void Show(Component c) { if (c) c.gameObject.SetActive(true); }

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

            // `total` only jumps in discrete commits (drift-end / near-miss); `pending`
            // changes every Update() frame while drifting and must never trigger a fly.
            if (total > lastCommittedCoinsTotal && coinFly != null && player != null && driftCoinsText != null)
            {
                Vector2 fromScreenPos = Camera.main != null
                    ? (Vector2)Camera.main.WorldToScreenPoint(player.position)
                    : (Vector2)driftCoinsText.rectTransform.position;
                coinFly.Play(fromScreenPos, driftCoinsText.rectTransform);
            }
            lastCommittedCoinsTotal = total;
        }
        // Health only changes on damage (rare) — a tween reads clearly here.
        void OnHealth(float h01) { UiJuice.FillTo(healthFill, h01, 0.25f); }

        // Boost fires every Update() frame while drifting (GameController.Update()) —
        // tweening here would kill+recreate a DOTween tween every frame for no visual
        // benefit (the value is already updating continuously). Snap instantly, same
        // as before this pass.
        static void SnapFill(RectTransform fill, float v) { if (fill) { var a = fill.anchorMax; a.x = Mathf.Clamp01(v); fill.anchorMax = a; } }
        void OnBoost(float b01) { SnapFill(boostFill, b01); if (boostButton) boostButton.interactable = b01 >= 0.999f; }

        // Cosmetic combo-tier presentation (name + color) on top of ScoreSystem.ComboTier —
        // pure presentation, does not affect scoring math.
        static readonly Color ComboDriftColor = new Color(0.18f, 0.85f, 0.78f);
        static readonly Color ComboChainColor = new Color(1f, 0.30f, 0.62f);
        static readonly Color ComboInfernoColor = new Color(1f, 0.55f, 0.15f);
        static readonly Color ComboLegendColor = new Color(1f, 0.80f, 0.20f);

        static string ComboTierLabel(ScoreSystem.ComboTier t)
        {
            if (t == ScoreSystem.ComboTier.Chain) return "CHAIN";
            if (t == ScoreSystem.ComboTier.Inferno) return "INFERNO";
            if (t == ScoreSystem.ComboTier.Legend) return "LEGEND";
            return "DRIFT";
        }

        static Color ComboTierColor(ScoreSystem.ComboTier t)
        {
            if (t == ScoreSystem.ComboTier.Chain) return ComboChainColor;
            if (t == ScoreSystem.ComboTier.Inferno) return ComboInfernoColor;
            if (t == ScoreSystem.ComboTier.Legend) return ComboLegendColor;
            return ComboDriftColor;
        }

        void OnMultiplier(float mult, int combo)
        {
            if (!multiplierText) return;
            bool show = combo > 0 && mult > 1.0001f;
            bool wasShown = multiplierText.gameObject.activeSelf;
            multiplierText.gameObject.SetActive(show);
            if (show)
            {
                var tier = ScoreSystem.GetComboTier(combo);
                multiplierText.text = ComboTierLabel(tier) + " x" + mult.ToString("0.0");
                multiplierText.color = ComboTierColor(tier);
                if (!wasShown) UiJuice.PunchScale(multiplierText.rectTransform, 0.2f);
            }
        }

        void OnCountdownTick(int s)
        {
            if (!countdownText) return;
            CancelInvoke(nameof(HideCountdown));
            countdownText.text = s.ToString();
            Show(countdownText);
            UiJuice.PunchScale(countdownText.rectTransform, 0.2f);
        }
        void OnCountdownGo()
        {
            if (!countdownText) return;
            countdownText.text = "GO!";
            Show(countdownText);
            UiJuice.PunchScale(countdownText.rectTransform, 0.25f);
            CancelInvoke(nameof(HideCountdown));
            Invoke(nameof(HideCountdown), 0.7f);
        }
        void HideCountdown() => Hide(countdownText);
        void OnNearMiss(int chain)
        {
            if (!nearMissText) return;
            nearMissText.text = chain > 1 ? "NEAR MISS x" + chain + "!" : "NEAR MISS!";
            Show(nearMissText);
            UiJuice.PunchScale(nearMissText.rectTransform, 0.25f);
            CancelInvoke(nameof(HideNearMiss));
            Invoke(nameof(HideNearMiss), 0.6f);
        }
        void HideNearMiss() => Hide(nearMissText);
        void OnCollected() => UiJuice.PunchScale(healthFill, 0.15f);

        void OnRunReady()
        {
            lastCommittedCoinsTotal = 0;
            ShowHud();
        }

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

            PlayEndReveal();
        }

        /// <summary>
        /// Staggers the end-panel children in (banner → distance → rewards →
        /// buttons). Killed by id at the top so a rapid fail→revive→fail cycle
        /// can't stack sequences. Runs on unscaled time — Time.timeScale is 0
        /// while this plays (see UiJuice's class doc).
        /// </summary>
        void PlayEndReveal()
        {
            DOTween.Kill("EndReveal");

            var groups = new[] { resultBannerGroup, distanceCardGroup, rewardsGroup, retryGroup, endHomeGroup };
            var seq = DOTween.Sequence().SetId("EndReveal").SetUpdate(true);
            float t = 0f;
            foreach (var g in groups)
            {
                if (g == null) continue;
                g.alpha = 0f;
                seq.Insert(t, g.DOFade(1f, 0.2f).SetUpdate(true));
                t += 0.07f;
            }
        }

        // TODO(Ads): Phase 4 interstitial landing spot — show a frequency-capped
        // PlatformServices.Ads.ShowInterstitial(...) here before SceneFlow.GoToMenu()
        // once a real ad network is wired. Keep Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md
        // in sync with this call site.
        void GoHome() { Time.timeScale = 1f; GameSignals.RaiseQuitRequested(); SceneFlow.GoToMenu(); }
    }
}
