using NUnit.Framework;

public class MatchFlowTests
{
    [Test]
    public void NewFlowStartsWaitingAtWaveZero()
    {
        var flow = new MatchFlow(3);

        Assert.That(flow.State, Is.EqualTo(MatchState.WaitingToStart));
        Assert.That(flow.CurrentWave, Is.Zero);
    }

    [Test]
    public void StartAndNextWaveMoveTheFlowIntoPlaying()
    {
        var flow = new MatchFlow(3);

        Assert.That(flow.Start(), Is.True);
        Assert.That(flow.State, Is.EqualTo(MatchState.Playing));
        Assert.That(flow.CurrentWave, Is.Zero);

        Assert.That(flow.TryStartNextWave(), Is.True);
        Assert.That(flow.CurrentWave, Is.EqualTo(1));
        Assert.That(flow.State, Is.EqualTo(MatchState.Playing));
    }

    [Test]
    public void PauseAndResumePreserveTheCurrentWave()
    {
        var flow = new MatchFlow(3);
        flow.Start();
        flow.TryStartNextWave();

        Assert.That(flow.Pause(), Is.True);
        Assert.That(flow.State, Is.EqualTo(MatchState.Paused));
        Assert.That(flow.CurrentWave, Is.EqualTo(1));
        Assert.That(flow.TryStartNextWave(), Is.False);
        Assert.That(flow.CurrentWave, Is.EqualTo(1));

        Assert.That(flow.Resume(), Is.True);
        Assert.That(flow.State, Is.EqualTo(MatchState.Playing));
    }

    [Test]
    public void StartingPastTheConfiguredLastWaveTransitionsToVictory()
    {
        var flow = new MatchFlow(2);
        flow.Start();
        flow.TryStartNextWave();
        flow.TryStartNextWave();

        Assert.That(flow.CurrentWave, Is.EqualTo(2));
        Assert.That(flow.TryStartNextWave(), Is.False);
        Assert.That(flow.State, Is.EqualTo(MatchState.Victory));
    }

    [Test]
    public void VictoryIsTerminalUntilReset()
    {
        var flow = new MatchFlow(3);
        flow.Start();

        Assert.That(flow.SetVictory(), Is.True);
        Assert.That(flow.State, Is.EqualTo(MatchState.Victory));
        Assert.That(flow.Pause(), Is.False);

        flow.Reset();

        Assert.That(flow.State, Is.EqualTo(MatchState.WaitingToStart));
        Assert.That(flow.CurrentWave, Is.Zero);
    }

    [Test]
    public void DefeatIsTerminalUntilReset()
    {
        var flow = new MatchFlow(3);
        flow.Start();

        Assert.That(flow.SetDefeat(), Is.True);
        Assert.That(flow.State, Is.EqualTo(MatchState.Defeat));
        Assert.That(flow.Resume(), Is.False);
        Assert.That(flow.Start(), Is.False);

        flow.Reset();

        Assert.That(flow.State, Is.EqualTo(MatchState.WaitingToStart));
        Assert.That(flow.CurrentWave, Is.Zero);
    }
}
