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
    [SerializeField] TrafficSensor sensor;
    [Tooltip("Optional — drives road difficulty from distance. Null is fine.")]
    [SerializeField] DifficultyDirector difficulty;

    [Header("Run Tuning")]
    [SerializeField] int reviveCount = 1;
    [SerializeField] bool autoStartOnLoad = true;
    [Tooltip("Once moving, if damage drags speed below this the run ends (stalled).")]
    [SerializeField] float minGameOverSpeedKph = 25f;
    [Tooltip("Seconds of 3-2-1 countdown before the run begins (and car unlocks).")]
    [SerializeField] int countdownSeconds = 3;

    [Header("Score Tuning")]
    [SerializeField] float comboStepDistance = 25f;
    [SerializeField] float multiplierStep = 0.5f;
    [SerializeField] int nearMissBonus = 250;

    [Header("Vehicle / Economy / Boost")]
    [SerializeField] VehicleHealth vehicle;
    [SerializeField] float driftCoinRate = 0.6f;            // pending drift coins per drift metre
    [SerializeField] float driftCommitGraceSeconds = 0.75f;
    [SerializeField] int nearMissDriftCoins = 5;
    [SerializeField] float boostFillPerDriftMetre = 0.004f;
    [SerializeField] float boostFillPerNearMiss = 0.12f;
    [SerializeField] float boostDuration = 2.5f;

    int driftCoinsTotal;
    float driftCoinsPendingRaw;
    float driftTrailInactiveTime;
    float boostFill;
    public float BoostFill01 => boostFill;
    public int DriftCoinsTotal => driftCoinsTotal;

    RunStateMachine runState;
    ScoreSystem score;

    float lastDistance;
    int bestScore;
    float bestDistanceMeters;
    bool paused;
    Coroutine countdownRoutine;

    // ── Lifecycle ───────────────────────────────────────────────────────────
    void Awake()
    {
        runState = new RunStateMachine(reviveCount);
        score = new ScoreSystem(comboStepDistance, multiplierStep, nearMissBonus);

        // Track best ourselves: ScoreSystem.BestScore is in-memory only.
        bestScore = Mathf.Max(SaveService.GetBestScore(), score.BestScore);
        bestDistanceMeters = GameApp.Instance != null
            ? Mathf.Max(0f, GameApp.Instance.Data.bestDistanceMeters)
            : Mathf.Max(0f, SaveService.GetBestScore());

        if (!vehicle && player != null) vehicle = player.GetComponent<VehicleHealth>();
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

        // Sensor → controller.
        if (sensor != null)
        {
            sensor.Hit += HandleCollisionHit;
            sensor.NearMissed += HandleNearMissed;
        }

        if (vehicle != null)
        {
            vehicle.Totaled += HandleTotaled;
            vehicle.HealthChanged += GameSignals.RaiseHealthChanged;
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

        if (sensor != null)
        {
            sensor.Hit -= HandleCollisionHit;
            sensor.NearMissed -= HandleNearMissed;
        }

        if (vehicle != null)
        {
            vehicle.Totaled -= HandleTotaled;
            vehicle.HealthChanged -= GameSignals.RaiseHealthChanged;
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
        ResetRunSystems();

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

        bool trailFxActive = car != null && car.TrailFxActive;

        // Stall-out: when damage has capped the car's top speed below the floor it can no
        // longer keep going. Uses the damage-capped target (not instantaneous speed), so
        // hard drifting that momentarily scrubs speed never triggers a false game over.
        if (car != null && car.TargetSpeedKph < minGameOverSpeedKph)
        {
            runState.FailRun("stalled");
            return;
        }

        float total = road.DistanceTravelled;
        float delta = total - lastDistance;

        if (delta > 0f)
        {
            score.AddDistance(delta);

            if (trailFxActive)
            {
                score.BuildDriftCombo(delta);
                driftCoinsPendingRaw += delta * driftCoinRate;
                driftTrailInactiveTime = 0f;
                boostFill = Mathf.Min(1f, boostFill + delta * boostFillPerDriftMetre);
                GameSignals.RaiseBoostChanged(boostFill);
                GameSignals.RaiseDriftCoinsChanged(driftCoinsTotal, GetPendingDriftCoins());
            }
            else
            {
                if (score.ComboCount > 0 || driftCoinsPendingRaw > 0f)
                {
                    driftTrailInactiveTime += Time.deltaTime;
                    if (driftTrailInactiveTime >= driftCommitGraceSeconds)
                    {
                        CommitDriftCombo();
                    }
                }
            }

            GameSignals.RaiseDistanceChanged(total);
            GameSignals.RaiseMultiplierChanged(score.DriftMultiplier, score.ComboCount);
        }
        else if (!trailFxActive && (score.ComboCount > 0 || driftCoinsPendingRaw > 0f))
        {
            driftTrailInactiveTime += Time.deltaTime;
            if (driftTrailInactiveTime >= driftCommitGraceSeconds)
            {
                CommitDriftCombo();
            }
        }
        else if (trailFxActive)
        {
            driftTrailInactiveTime = 0f;
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
        // Clean, reliable restart: reload the gameplay scene from scratch (fresh car,
        // road, traffic, countdown). Restore timescale first so the reload isn't frozen.
        Time.timeScale = 1f;
        paused = false;
        SceneFlow.Reload();
    }

    void HandleReviveRequested()
    {
        // TODO(Ads): gate this behind PlatformServices.Ads.ShowRewarded(...) once a
        // real ad network is wired (Phase 4). StubAdsService currently
        // auto-grants so today's free-revive behavior is unchanged. Keep
        // Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md in sync with this call site.

        // Revive() only succeeds from Failed with revives remaining.
        if (!runState.Revive())
        {
            return;
        }

        // Brief invuln so the same traffic does not instantly re-kill.
        if (sensor != null)
        {
            sensor.StartGrace();
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
        CommitDriftCombo();
        score.FinalizeRun();

        float finalDistance = road != null ? road.DistanceTravelled : lastDistance;
        int final = Mathf.RoundToInt(finalDistance);

        int coins = driftCoinsTotal;
        int xp    = Mathf.Max(1, Mathf.RoundToInt(finalDistance * 0.05f));

        if (GameApp.Instance != null)
        {
            GameApp.Instance.SubmitRun(finalDistance, coins, xp);
            bestDistanceMeters = GameApp.Instance.Data.bestDistanceMeters;

            LeaderboardProvider.Current.Submit(finalDistance, GameApp.Instance.Data.selectedVehicleId);
            // TODO(PlayGames): also submit to the online leaderboard once GPGS is
            // installed (PlatformServices.PlayGames.SubmitScore). Keep
            // Assets/_AI/PLAY_GAMES_ADS_INTEGRATION.md in sync with this call site.
        }
        else if (finalDistance > bestDistanceMeters)
        {
            bestDistanceMeters = finalDistance;
            SaveService.SetBestScore(Mathf.RoundToInt(bestDistanceMeters));
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
        if (runState.CurrentState != RunState.Running) return;

        // Any tight traffic overlap is a crash. Near-misses are handled separately by TrafficSensor.
        var agent = trafficGo != null ? trafficGo.GetComponentInParent<TrafficAgent>() : null;
        if (agent != null)
        {
            runState.FailRun("crash");
            return;
        }

        if (vehicle != null)
        {
            vehicle.TakeDamage(vehicle.DamagePerHit);
            GameSignals.RaiseDamaged();
        }
        else
        {
            runState.FailRun("collision");
        }
    }

    void HandleTotaled()
    {
        if (runState.CurrentState == RunState.Running)
            runState.FailRun("totaled");
    }

    void CommitDriftCombo()
    {
        if (score.ComboCount <= 0 && driftCoinsPendingRaw <= 0f)
        {
            return;
        }

        if (driftCoinsPendingRaw > 0f)
        {
            driftCoinsTotal += Mathf.RoundToInt(driftCoinsPendingRaw * score.DriftMultiplier);
        }

        driftCoinsPendingRaw = 0f;
        driftTrailInactiveTime = 0f;
        score.ResetCombo();
        GameSignals.RaiseDriftCoinsChanged(driftCoinsTotal, 0);
    }

    /// <summary>Spend a full boost meter for a speed surge + brief invulnerability.</summary>
    public void ActivateBoost()
    {
        if (boostFill < 0.999f || car == null) return;
        car.ActivateBoost(boostDuration);
        if (vehicle != null) vehicle.GrantGrace(boostDuration);
        boostFill = 0f;
        GameSignals.RaiseBoostChanged(0f);
    }

    void ResetRunSystems()
    {
        if (vehicle != null) vehicle.ResetHealth();
        driftCoinsTotal = 0;
        driftCoinsPendingRaw = 0f;
        driftTrailInactiveTime = 0f;
        boostFill = 0f;
        GameSignals.RaiseDriftCoinsChanged(0, 0);
        GameSignals.RaiseBoostChanged(0f);
    }

    void HandleNearMissed(TrafficAgent agent)
    {
        if (runState.CurrentState != RunState.Running)
        {
            return;
        }

        score.RegisterNearMiss();
        boostFill = Mathf.Min(1f, boostFill + boostFillPerNearMiss);
        GameSignals.RaiseBoostChanged(boostFill);
        driftCoinsTotal += nearMissDriftCoins;
        GameSignals.RaiseDriftCoinsChanged(driftCoinsTotal, GetPendingDriftCoins());
        GameSignals.RaiseNearMiss();
    }

    int GetPendingDriftCoins()
    {
        if (driftCoinsPendingRaw <= 0f)
        {
            return 0;
        }

        return Mathf.RoundToInt(driftCoinsPendingRaw * score.DriftMultiplier);
    }
}
}
