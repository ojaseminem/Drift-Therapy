using System.Collections.Generic;
using NUnit.Framework;

public class RunStateMachineTests
{
    [Test]
    public void StartRun_FromReady_TransitionsToRunningAndRaisesEvents()
    {
        var machine = new RunStateMachine();
        var visitedStates = new List<RunState>();
        int runStartedCount = 0;

        machine.StateChanged += visitedStates.Add;
        machine.RunStarted += () => runStartedCount++;

        machine.StartRun();

        Assert.That(machine.CurrentState, Is.EqualTo(RunState.Running));
        Assert.That(runStartedCount, Is.EqualTo(1));
        Assert.That(visitedStates, Is.EqualTo(new[] { RunState.Running }));
    }

    [Test]
    public void FailRun_FromRunning_TransitionsToFailedAndStoresReason()
    {
        var machine = new RunStateMachine();
        string failedReason = null;

        machine.StartRun();
        machine.RunFailed += reason => failedReason = reason;

        machine.FailRun("traffic-hit");

        Assert.That(machine.CurrentState, Is.EqualTo(RunState.Failed));
        Assert.That(machine.LastFailureReason, Is.EqualTo("traffic-hit"));
        Assert.That(failedReason, Is.EqualTo("traffic-hit"));
        Assert.That(machine.CanRevive, Is.True);
    }

    [Test]
    public void Revive_FromFailed_ConsumesReviveAndReturnsToRunningOnStart()
    {
        var machine = new RunStateMachine(reviveCount: 1);

        machine.StartRun();
        machine.FailRun("wipeout");

        machine.Revive();

        Assert.That(machine.CurrentState, Is.EqualTo(RunState.Reviving));
        Assert.That(machine.CanRevive, Is.False);

        machine.StartRun();

        Assert.That(machine.CurrentState, Is.EqualTo(RunState.Running));
    }

    [Test]
    public void RequestRestart_TransitionsToRestartingAndMarkRestartedReturnsToReady()
    {
        var machine = new RunStateMachine();
        int restartRequestedCount = 0;

        machine.StartRun();
        machine.FailRun("barrier");
        machine.RestartRequested += () => restartRequestedCount++;

        machine.RequestRestart();

        Assert.That(machine.CurrentState, Is.EqualTo(RunState.Restarting));
        Assert.That(restartRequestedCount, Is.EqualTo(1));

        machine.MarkRestarted();

        Assert.That(machine.CurrentState, Is.EqualTo(RunState.Ready));
        Assert.That(machine.LastFailureReason, Is.Null);

        machine.StartRun();
        machine.FailRun("second-crash");

        Assert.That(machine.CanRevive, Is.True);
    }
}
