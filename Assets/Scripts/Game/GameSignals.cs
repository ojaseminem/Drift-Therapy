using System;
using UnityEngine;

/// <summary>
/// Central, decoupled event hub for Drift Therapy.
///
/// Gameplay systems RAISE signals; UI / audio / analytics SUBSCRIBE to them.
/// UI raises *intent* signals (button presses); the GameController subscribes
/// and drives the run lifecycle. Nothing here references concrete UI or
/// gameplay types, so subsystems never depend on each other directly.
///
/// Contract owners:
///   - GameController (gameplay glue) calls the Raise* state/score methods.
///   - Presenters (UI Toolkit) subscribe to state/score events and call the
///     Raise* intent methods from button callbacks.
///
/// Keep this allocation-light: these are plain C# multicast delegates.
/// </summary>
public static class GameSignals
{
    // ── Run lifecycle (gameplay → listeners) ────────────────────────────────
    /// <summary>A new run has begun.</summary>
    public static event Action RunStarted;
    /// <summary>The run ended. Payload = failure reason (e.g. "collision").</summary>
    public static event Action<string> RunFailed;
    /// <summary>A fresh run is armed and ready (after restart / boot).</summary>
    public static event Action RunReady;
    /// <summary>A revive is available to the player (e.g. rewarded ad offer).</summary>
    public static event Action ReviveOffered;
    /// <summary>The player was revived and the run continues.</summary>
    public static event Action Revived;
    /// <summary>The run was paused.</summary>
    public static event Action Paused;
    /// <summary>The run resumed from pause.</summary>
    public static event Action Resumed;

    // ── Scoring / HUD (gameplay → listeners) ────────────────────────────────
    /// <summary>Current score and best score changed. (current, best)</summary>
    public static event Action<int, int> ScoreChanged;
    /// <summary>Drift multiplier / combo changed. (multiplier, comboCount)</summary>
    public static event Action<float, int> MultiplierChanged;
    /// <summary>Distance travelled this run changed, in metres.</summary>
    public static event Action<float> DistanceChanged;
    /// <summary>A near-miss with traffic was registered.</summary>
    public static event Action NearMiss;
    /// <summary>Normalised difficulty (0..1) changed.</summary>
    public static event Action<float> DifficultyChanged;

    // ── Countdown (gameplay → listeners) ────────────────────────────────────
    /// <summary>Pre-run countdown ticked. Payload = seconds remaining (3,2,1).</summary>
    public static event Action<int> CountdownTick;
    /// <summary>Countdown finished — the run is going. ("GO!")</summary>
    public static event Action CountdownGo;

    // ── Intents (UI → GameController) ───────────────────────────────────────
    /// <summary>Player asked to start the run (tap to start).</summary>
    public static event Action StartRequested;
    /// <summary>Player asked to restart (end screen / pause).</summary>
    public static event Action RestartRequested;
    /// <summary>Player asked to take the revive (rewarded ad).</summary>
    public static event Action ReviveRequested;
    /// <summary>Player toggled pause.</summary>
    public static event Action PauseToggled;
    /// <summary>Player asked to resume from pause.</summary>
    public static event Action ResumeRequested;
    /// <summary>Player asked to quit to menu / main.</summary>
    public static event Action QuitRequested;

    // ── Raisers: lifecycle ──────────────────────────────────────────────────
    public static void RaiseRunStarted() => RunStarted?.Invoke();
    public static void RaiseRunFailed(string reason) => RunFailed?.Invoke(reason);
    public static void RaiseRunReady() => RunReady?.Invoke();
    public static void RaiseReviveOffered() => ReviveOffered?.Invoke();
    public static void RaiseRevived() => Revived?.Invoke();
    public static void RaisePaused() => Paused?.Invoke();
    public static void RaiseResumed() => Resumed?.Invoke();

    // ── Raisers: scoring ────────────────────────────────────────────────────
    public static void RaiseScoreChanged(int current, int best) => ScoreChanged?.Invoke(current, best);
    public static void RaiseMultiplierChanged(float multiplier, int comboCount) => MultiplierChanged?.Invoke(multiplier, comboCount);
    public static void RaiseDistanceChanged(float metres) => DistanceChanged?.Invoke(metres);
    public static void RaiseNearMiss() => NearMiss?.Invoke();
    public static void RaiseDifficultyChanged(float t01) => DifficultyChanged?.Invoke(t01);
    public static void RaiseCountdownTick(int secondsRemaining) => CountdownTick?.Invoke(secondsRemaining);
    public static void RaiseCountdownGo() => CountdownGo?.Invoke();

    // ── Raisers: intents ────────────────────────────────────────────────────
    public static void RaiseStartRequested() => StartRequested?.Invoke();
    public static void RaiseRestartRequested() => RestartRequested?.Invoke();
    public static void RaiseReviveRequested() => ReviveRequested?.Invoke();
    public static void RaisePauseToggled() => PauseToggled?.Invoke();
    public static void RaiseResumeRequested() => ResumeRequested?.Invoke();
    public static void RaiseQuitRequested() => QuitRequested?.Invoke();

    /// <summary>
    /// Clears every subscriber. Called automatically before the first scene
    /// loads so that "Enter Play Mode (no domain reload)" does not leave stale
    /// handlers from a previous session attached.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAll()
    {
        RunStarted = null; RunFailed = null; RunReady = null;
        ReviveOffered = null; Revived = null; Paused = null; Resumed = null;
        ScoreChanged = null; MultiplierChanged = null; DistanceChanged = null;
        NearMiss = null; DifficultyChanged = null;
        CountdownTick = null; CountdownGo = null;
        StartRequested = null; RestartRequested = null; ReviveRequested = null;
        PauseToggled = null; ResumeRequested = null; QuitRequested = null;
    }
}
