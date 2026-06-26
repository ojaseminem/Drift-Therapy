using System.Collections;
using UnityEngine;

namespace DriftTherapy
{

/// <summary>
/// The scene brain for a Drift Therapy run. Owns the (plain-C#) run state
/// machine and score system, listens to UI intents on <see cref="GameSignals"/>,
/// reads gameplay state from the car / road / detectors, and broadcasts state &amp;
/// score changes back out through <see cref="GameSignals"/>.
///
/// Nothing in here references concrete UI: the UI raises intent signals, this
/// controller reacts, and presenters subscribe to the resulting state signals.
/// </summary>
[DisallowMultipleComponent]
public class GameController : MonoBehaviour
{
    [Header("Gameplay References")]
    [SerializeField] HyperDriftCarController car;
    [SerializeField] RoadSegmentPool road;
    [SerializeField] Transform player;
    [SerializeField] PlayerCollisionDetector collisionDetector;
    [SerializeField] NearMissDetector nearMissDetector;
    [Tooltip("Optional — drives road difficulty from distance. Null is fine.")]
    [SerializeField] DifficultyDirector difficulty;

    [Header("Run Tuning")]
    [SerializeField] int reviveCount = 1;
    [SerializeField] bool autoStartOnLoad = true;
    [Tooltip("Seconds of 3-2-1 countdown before the run begins (and car unlocks).")]
    [SerializeField] int countdownSeconds = 3;

    [Header("Score Tuning")]
    [SerializeField] float comboStepDistance = 25f;
    [SerializeField] float multiplierStep = 0.5f;
    [SerializeField] int nearMissBonus = 250;

    RunStateMachine runState;
    ScoreSystem score;

    float lastDistance;
    int bestScore;
    bool paused;
    Coroutine countdownRoutine;

    // ── Lifecycle ───────────────────────────────────────────────────────────
    void Awake()
    {
        runState = new RunStateMachine(reviveCount);
        score = new ScoreSystem(comboStepDistance, multiplierStep, nearMissBonus);

        // Track best ourselves: ScoreSystem.BestScore is in-memory only.
        bestScore = Mathf.Max(SaveService.GetBestScore(), score.BestScore);
    }

    void OnEnable()
    {
        // UI intents → controller.
        GameSignals.StartRequested   += HandleStartRequested;
        GameSignals.RestartRequested += HandleRestartRequested;
        GameSignals.ReviveRequested  += HandleReviveRequested;
        GameSignals.PauseToggled     += HandlePauseToggled;
        GameSignals.ResumeRequested  += HandleResumeRequested;
        GameSignals.QuitRequested    += HandleQuitRequested;

        // State machine → controller.
        runState.StateChanged += HandleStateChanged;
        runState.RunStarted   += HandleRunStarted;
        runState.RunFailed    += HandleRunFailed;

        // Score → controller.
        score.ScoreChanged += HandleScoreChanged;

        // Detectors → controller.
        if (collisionDetector != null)
        {
            collisionDetector.Hit += HandleCollisionHit;
        }

        if (nearMissDetector != null)
        {
            nearMissDetector.NearMissed += HandleNearMissed;
        }
    }

    void OnDisable()
    {
        GameSignals.StartRequested   -= HandleStartRequested;
        GameSignals.RestartRequested -= HandleRestartRequested;
        GameSignals.ReviveRequested  -= HandleReviveRequested;
        GameSignals.PauseToggled     -= HandlePauseToggled;
        GameSignals.ResumeRequested  -= HandleResumeRequested;
        GameSignals.QuitRequested    -= HandleQuitRequested;

        if (runState != null)
        {
            runState.StateChanged -= HandleStateChanged;
            runState.RunStarted   -= HandleRunStarted;
            runState.RunFailed    -= HandleRunFailed;
        }

        if (score != null)
        {
            score.ScoreChanged -= HandleScoreChanged;
        }

        if (collisionDetector != null)
        {
            collisionDetector.Hit -= HandleCollisionHit;
        }

        if (nearMissDetector != null)
        {
            nearMissDetector.NearMissed -= HandleNearMissed;
        }
    }

    void Start()
    {
        // Always boot with a sane timescale.
        Time.timeScale = 1f;
        paused = false;

        ApplyVehicle();

        lastDistance = road != null ? road.DistanceTravelled : 0f;

        // Publish the initial score/best so the HUD has values immediately.
        GameSignals.RaiseScoreChanged(score.CurrentScore, bestScore);

        if (autoStartOnLoad)
        {
            BeginCountdown();
        }
        else
        {
            GameSignals.RaiseRunReady();
        }
    }

    // ── Countdown ───────────────────────────────────────────────────────────
    void BeginCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
        }
        countdownRoutine = StartCoroutine(CountdownThenStart());
    }

    IEnumerator CountdownThenStart()
    {
        // Hold the car still and keep us out of Running until "GO!".
        Time.timeScale = 1f;
        paused = false;
        if (car != null) car.ControlsEnabled = false;

        GameSignals.RaiseRunReady();

        for (int s = Mathf.Max(1, countdownSeconds); s > 0; s--)
        {
            GameSignals.RaiseCountdownTick(s);
            yield return new WaitForSecondsRealtime(1f);
        }

        GameSignals.RaiseCountdownGo();

        if (car != null) car.ControlsEnabled = true;
        lastDistance = road != null ? road.DistanceTravelled : 0f;
        runState.StartRun();
        countdownRoutine = null;
    }

    // ── Per-frame gameplay accumulation ─────────────────────────────────────
    void Update()
    {
        if (paused || runState == null || runState.CurrentState != RunState.Running)
        {
            return;
        }

        if (road == null)
        {
            return;
        }

        float total = road.DistanceTravelled;
        float delta = total - lastDistance;

        if (delta > 0f)
        {
            score.AddDistance(delta);

            if (car != null && car.DriftActive)
            {
                score.BuildDriftCombo(delta);
            }
            else
            {
                score.ResetCombo();
            }

            GameSignals.RaiseDistanceChanged(total);
            GameSignals.RaiseMultiplierChanged(score.DriftMultiplier, score.ComboCount);
        }

        if (difficulty != null)
        {
            difficulty.Tick(total);
        }

        lastDistance = total;
    }

    // ── Intent handlers ─────────────────────────────────────────────────────
    void HandleStartRequested()
    {
        // Only meaningful from Ready; run a fresh countdown then start.
        if (runState.CurrentState == RunState.Ready && countdownRoutine == null)
        {
            BeginCountdown();
        }
    }

    void HandleRestartRequested()
    {
        // Make restart feel instant: tear down, re-arm, and immediately run again.
        Time.timeScale = 1f;
        paused = false;

        runState.RequestRestart();   // → Restarting (fires RestartRequested)
        score.ResetRun();

        if (difficulty != null)
        {
            difficulty.ResetDifficulty();
        }

        lastDistance = road != null ? road.DistanceTravelled : 0f;

        runState.MarkRestarted();    // → Ready
        runState.StartRun();         // → Running

        GameSignals.RaiseScoreChanged(score.CurrentScore, bestScore);
    }

    void HandleReviveRequested()
    {
        // Revive() only succeeds from Failed with revives remaining.
        if (!runState.Revive())
        {
            return;
        }

        // Brief invuln so the same traffic does not instantly re-kill.
        if (collisionDetector != null)
        {
            collisionDetector.StartGrace();
        }

        // Resume the run (Reviving → Running) and announce it.
        if (runState.StartRun())
        {
            Time.timeScale = 1f;
            paused = false;
            lastDistance = road != null ? road.DistanceTravelled : 0f;
            GameSignals.RaiseRevived();
        }
    }

    void HandlePauseToggled()
    {
        if (paused)
        {
            ResumeInternal();
            return;
        }

        if (runState.CurrentState != RunState.Running)
        {
            return;
        }

        Time.timeScale = 0f;
        paused = true;
        GameSignals.RaisePaused();
    }

    void HandleResumeRequested()
    {
        if (paused)
        {
            ResumeInternal();
        }
    }

    void ResumeInternal()
    {
        Time.timeScale = 1f;
        paused = false;
        GameSignals.RaiseResumed();
    }

    void HandleQuitRequested()
    {
        // Minimal handling: ensure timescale is restored so any subsequent scene
        // load / menu is not stuck frozen. Scene routing is owned elsewhere.
        Time.timeScale = 1f;
        paused = false;
    }

    // ── State machine handlers ──────────────────────────────────────────────
    void HandleStateChanged(RunState newState)
    {
        if (newState == RunState.Ready)
        {
            GameSignals.RaiseRunReady();
        }
    }

    void HandleRunStarted()
    {
        GameSignals.RaiseRunStarted();
    }

    void HandleRunFailed(string reason)
    {
        score.FinalizeRun();

        int final = score.FinalScore;

        // Reward: coins scale with score, XP a touch slower. Banked to the wallet.
        int coins = Mathf.Max(0, Mathf.RoundToInt(final * 0.10f));
        int xp    = Mathf.Max(1, Mathf.RoundToInt(final * 0.05f));

        if (GameApp.Instance != null)
        {
            GameApp.Instance.SubmitRun(final, coins, xp);
            bestScore = GameApp.Instance.Data.highScore;
        }
        else if (final > bestScore)
        {
            bestScore = final;
            SaveService.SetBestScore(bestScore);
            SaveService.Save();
        }

        GameSignals.RaiseRunFailed(reason);
        GameSignals.RaiseScoreChanged(final, bestScore);

        if (runState.CanRevive)
        {
            GameSignals.RaiseReviveOffered();
        }
    }

    // ── Vehicle skin ─────────────────────────────────────────────────────────
    void ApplyVehicle()
    {
        var sel = GameApp.Instance != null ? GameApp.Instance.Selected : null;
        if (sel == null || player == null) return;

        var mpb = new MaterialPropertyBlock();
        foreach (var r in player.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r.transform.name != "SunLineGTE") continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", sel.bodyColor);
            mpb.SetColor("_Color", sel.bodyColor);
            r.SetPropertyBlock(mpb);
        }
    }

    // ── Score handler ───────────────────────────────────────────────────────
    void HandleScoreChanged()
    {
        if (score.CurrentScore > bestScore)
        {
            bestScore = score.CurrentScore;
        }

        GameSignals.RaiseScoreChanged(score.CurrentScore, bestScore);
    }

    // ── Detector handlers ───────────────────────────────────────────────────
    void HandleCollisionHit(GameObject trafficGo)
    {
        if (runState.CurrentState == RunState.Running)
        {
            runState.FailRun("collision");
        }
    }

    void HandleNearMissed(TrafficAgent agent)
    {
        if (runState.CurrentState != RunState.Running)
        {
            return;
        }

        score.RegisterNearMiss();
        GameSignals.RaiseNearMiss();
    }
}
}
