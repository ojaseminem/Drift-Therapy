using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// In-game UI (code-driven uGUI): HUD (score / distance / multiplier / pause /
    /// countdown / near-miss), the Pause modal, and the End modal (score, best,
    /// coins + XP earned, retry / home). Driven entirely by <see cref="GameSignals"/>;
    /// buttons raise intents the <see cref="GameController"/> acts on.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameHudUI : MonoBehaviour
    {
        Transform uiRoot;
        GameObject pausePanel, endPanel;
        TMP_Text scoreText, distanceText, multiplierText, countdownText, nearMissText;
        TMP_Text endScore, endBest, endNewBest, endCoins, endXp;

        void Start()
        {
            Build();
            Subscribe();
            ShowHud();
        }

        void OnDestroy() => Unsubscribe();

        // ── Build ────────────────────────────────────────────────────────────
        void Build()
        {
            var canvas = UIFactory.CreateCanvas("GameCanvas", 10);
            uiRoot = canvas.transform;

            // HUD layer (transparent, non-blocking).
            var hud = UIFactory.Panel(uiRoot, "HUD", new Color(0, 0, 0, 0));
            hud.raycastTarget = false;
            UIFactory.Stretch((RectTransform)hud.transform);

            distanceText = UIFactory.Text(hud.transform, "Distance", "0 m", 40, UIFactory.TextCol, TextAlignmentOptions.TopLeft);
            UIFactory.Box(distanceText.rectTransform, new Vector2(0, 1), new Vector2(420, 60), new Vector2(40, -60));

            scoreText = UIFactory.Text(hud.transform, "Score", "0", 92, UIFactory.TextCol, TextAlignmentOptions.Top);
            UIFactory.Box(scoreText.rectTransform, new Vector2(0.5f, 1), new Vector2(700, 130), new Vector2(0, -50));

            multiplierText = UIFactory.Text(hud.transform, "Mult", "x1.0", 46, UIFactory.Accent2, TextAlignmentOptions.Top);
            UIFactory.Box(multiplierText.rectTransform, new Vector2(0.5f, 1), new Vector2(320, 60), new Vector2(0, -185));
            multiplierText.gameObject.SetActive(false);

            var pauseBtn = UIFactory.Button(hud.transform, "Pause", "II", UIFactory.PanelCol, UIFactory.TextCol,
                () => GameSignals.RaisePauseToggled(), 40);
            UIFactory.Box((RectTransform)pauseBtn.transform, new Vector2(1, 1), new Vector2(100, 100), new Vector2(-40, -50));

            countdownText = UIFactory.Text(hud.transform, "Countdown", "", 200, UIFactory.Accent, TextAlignmentOptions.Center);
            UIFactory.Box(countdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600, 300), new Vector2(0, 120));
            countdownText.gameObject.SetActive(false);

            nearMissText = UIFactory.Text(hud.transform, "NearMiss", "NEAR MISS!", 52, UIFactory.Accent2, TextAlignmentOptions.Center);
            UIFactory.Box(nearMissText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700, 80), new Vector2(0, -160));
            nearMissText.gameObject.SetActive(false);

            // Pause modal.
            var pBody = BuildModal("PausePanel", "PAUSED", out pausePanel);
            StackButton(UIFactory.Button(pBody, "Resume", "RESUME", UIFactory.Accent, UIFactory.Ink, () => GameSignals.RaiseResumeRequested(), 44), 0);
            StackButton(UIFactory.Button(pBody, "Restart", "RESTART", UIFactory.Coin, UIFactory.Ink, () => GameSignals.RaiseRestartRequested(), 44), 1);
            StackButton(UIFactory.Button(pBody, "Home", "HOME", UIFactory.PanelCol, UIFactory.TextCol, GoHome, 44), 2);
            pausePanel.SetActive(false);

            // End modal.
            var eBody = BuildModal("EndPanel", "RUN OVER", out endPanel);
            endScore = UIFactory.Text(eBody, "Score", "0", 110, UIFactory.TextCol);
            UIFactory.Box(endScore.rectTransform, new Vector2(0.5f, 1), new Vector2(640, 140), new Vector2(0, -120));
            endBest = UIFactory.Text(eBody, "Best", "BEST 0", 40, UIFactory.Coin);
            UIFactory.Box(endBest.rectTransform, new Vector2(0.5f, 1), new Vector2(640, 60), new Vector2(0, -260));
            endNewBest = UIFactory.Text(eBody, "NewBest", "★ NEW BEST ★", 42, UIFactory.Accent2);
            UIFactory.Box(endNewBest.rectTransform, new Vector2(0.5f, 1), new Vector2(640, 60), new Vector2(0, -320));
            endNewBest.gameObject.SetActive(false);
            endCoins = UIFactory.Text(eBody, "Coins", "+0 coins", 44, UIFactory.Coin);
            UIFactory.Box(endCoins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640, 60), new Vector2(0, 50));
            endXp = UIFactory.Text(eBody, "Xp", "+0 XP", 44, UIFactory.Accent);
            UIFactory.Box(endXp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640, 60), new Vector2(0, -20));
            UIFactory.Box((RectTransform)UIFactory.Button(eBody, "Retry", "RETRY", UIFactory.Coin, UIFactory.Ink, () => GameSignals.RaiseRestartRequested(), 50).transform,
                new Vector2(0.5f, 0), new Vector2(520, 140), new Vector2(0, 210));
            UIFactory.Box((RectTransform)UIFactory.Button(eBody, "Home", "HOME", UIFactory.PanelCol, UIFactory.TextCol, GoHome, 44).transform,
                new Vector2(0.5f, 0), new Vector2(520, 110), new Vector2(0, 60));
            endPanel.SetActive(false);
        }

        Transform BuildModal(string name, string title, out GameObject panelGo)
        {
            var scrim = UIFactory.Panel(uiRoot, name, new Color(0, 0, 0, 0.72f));
            UIFactory.Stretch((RectTransform)scrim.transform);
            panelGo = scrim.gameObject;

            var panel = UIFactory.Panel(scrim.transform, "Card", UIFactory.PanelCol);
            UIFactory.Stretch((RectTransform)panel.transform, 60, 60, 300, 300);

            var t = UIFactory.Text(panel.transform, "Title", title, 60, UIFactory.Accent);
            UIFactory.Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(600, 90), new Vector2(0, -40));
            return panel.transform;
        }

        void StackButton(Button b, int index)
        {
            UIFactory.Box((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(520, 120),
                new Vector2(0, 120 - index * 150));
        }

        // ── Signals ──────────────────────────────────────────────────────────
        void Subscribe()
        {
            GameSignals.ScoreChanged      += OnScore;
            GameSignals.DistanceChanged   += OnDistance;
            GameSignals.MultiplierChanged += OnMultiplier;
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
        }

        void Unsubscribe()
        {
            GameSignals.ScoreChanged      -= OnScore;
            GameSignals.DistanceChanged   -= OnDistance;
            GameSignals.MultiplierChanged -= OnMultiplier;
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

        void OnScore(int current, int best) { if (scoreText) scoreText.text = current.ToString(); }
        void OnDistance(float m) { if (distanceText) distanceText.text = Mathf.Max(0, Mathf.FloorToInt(m)) + " m"; }

        void OnMultiplier(float mult, int combo)
        {
            if (!multiplierText) return;
            bool show = combo > 0 && mult > 1.0001f;
            multiplierText.gameObject.SetActive(show);
            if (show) multiplierText.text = "x" + mult.ToString("0.0");
        }

        void OnCountdownTick(int s)
        {
            if (!countdownText) return;
            CancelInvoke(nameof(HideCountdown));
            countdownText.text = s.ToString();
            countdownText.gameObject.SetActive(true);
        }

        void OnCountdownGo()
        {
            if (!countdownText) return;
            countdownText.text = "GO!";
            countdownText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideCountdown));
            Invoke(nameof(HideCountdown), 0.7f);
        }

        void HideCountdown() { if (countdownText) countdownText.gameObject.SetActive(false); }

        void OnNearMiss()
        {
            if (!nearMissText) return;
            nearMissText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideNearMiss));
            Invoke(nameof(HideNearMiss), 0.6f);
        }

        void HideNearMiss() { if (nearMissText) nearMissText.gameObject.SetActive(false); }

        // ── Screen state ─────────────────────────────────────────────────────
        void ShowHud()
        {
            Time.timeScale = 1f;
            if (pausePanel) pausePanel.SetActive(false);
            if (endPanel) endPanel.SetActive(false);
        }

        void ShowPause()
        {
            if (pausePanel) pausePanel.SetActive(true);
            if (endPanel) endPanel.SetActive(false);
        }

        void OnRunFailed(string reason)
        {
            if (pausePanel) pausePanel.SetActive(false);
            if (endPanel) endPanel.SetActive(true);
            Time.timeScale = 0f;   // freeze gameplay behind the end screen

            var r = GameApp.Instance != null ? GameApp.Instance.LastRun : default;
            if (endScore) endScore.text = r.score.ToString();
            if (endBest) endBest.text = "BEST " + r.best;
            if (endNewBest) endNewBest.gameObject.SetActive(r.isNewBest && r.score > 0);
            if (endCoins) endCoins.text = "+" + r.coins + " coins";
            if (endXp) endXp.text = "+" + r.xp + " XP";
        }

        void GoHome()
        {
            Time.timeScale = 1f;
            GameSignals.RaiseQuitRequested();
            SceneFlow.GoToMenu();
        }
    }
}
