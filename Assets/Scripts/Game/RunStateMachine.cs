using System;

public class RunStateMachine
{
    readonly int maxReviveCount;
    int remainingRevives;

    public RunStateMachine(int reviveCount = 1)
    {
        maxReviveCount = Math.Max(0, reviveCount);
        remainingRevives = maxReviveCount;
        CurrentState = RunState.Ready;
    }

    public RunState CurrentState { get; private set; }
    public string LastFailureReason { get; private set; }
    public bool CanRevive => CurrentState == RunState.Failed && remainingRevives > 0;

    public event Action<RunState> StateChanged;
    public event Action RunStarted;
    public event Action<string> RunFailed;
    public event Action RestartRequested;

    public bool StartRun()
    {
        if (CurrentState != RunState.Ready && CurrentState != RunState.Reviving)
        {
            return false;
        }

        LastFailureReason = null;
        TransitionTo(RunState.Running);
        RunStarted?.Invoke();
        return true;
    }

    public bool FailRun(string reason)
    {
        if (CurrentState != RunState.Running)
        {
            return false;
        }

        LastFailureReason = reason;
        TransitionTo(RunState.Failed);
        RunFailed?.Invoke(reason);
        return true;
    }

    public bool Revive()
    {
        if (!CanRevive)
        {
            return false;
        }

        remainingRevives--;
        TransitionTo(RunState.Reviving);
        return true;
    }

    public bool RequestRestart()
    {
        if (CurrentState == RunState.Restarting)
        {
            return false;
        }

        TransitionTo(RunState.Restarting);
        RestartRequested?.Invoke();
        return true;
    }

    public bool MarkRestarted()
    {
        if (CurrentState != RunState.Restarting)
        {
            return false;
        }

        LastFailureReason = null;
        remainingRevives = maxReviveCount;
        TransitionTo(RunState.Ready);
        return true;
    }

    void TransitionTo(RunState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
        StateChanged?.Invoke(CurrentState);
    }
}
