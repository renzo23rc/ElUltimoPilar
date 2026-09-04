using NUnit.Framework;
using UltimoPilar.Arena;

public class ArenaPhaseStateTests
{
    private const int InitialPhase = 1;
    private const int GravityPhase = 3;
    private const int EmergencyPhase = 4;

    [Test]
    public void EnqueueMissingProducesForwardOrderedPhases()
    {
        var state = new ArenaPhaseState();

        state.EnqueueMissing(InitialPhase, EmergencyPhase);

        Assert.That(DequeueAll(state), Is.EqualTo(new[] { 2, GravityPhase, EmergencyPhase }));
    }

    [Test]
    public void ProgressionIsIrreversible()
    {
        var state = new ArenaPhaseState();

        state.MarkActivated(GravityPhase);
        state.MarkActivated(InitialPhase);
        state.EnqueueMissing(GravityPhase, InitialPhase);

        Assert.That(state.CurrentPhase, Is.EqualTo(GravityPhase));
        Assert.That(state.ActivatedPhases, Has.Member(GravityPhase));
        Assert.That(state.ActivatedPhases, Has.No.Member(InitialPhase));
        Assert.That(state.TryDequeue(out _), Is.False);
    }

    [Test]
    public void DuplicatePhasesAreSuppressedAcrossQueueAndActivation()
    {
        var state = new ArenaPhaseState();

        state.EnqueueMissing(InitialPhase, EmergencyPhase);
        state.EnqueueMissing(InitialPhase, EmergencyPhase);

        Assert.That(DequeueAll(state), Is.EqualTo(new[] { 2, GravityPhase, EmergencyPhase }));

        state.MarkActivated(2);
        state.MarkActivated(GravityPhase);
        state.MarkActivated(EmergencyPhase);
        state.EnqueueMissing(InitialPhase, EmergencyPhase);

        Assert.That(state.TryDequeue(out _), Is.False);
    }

    [Test]
    public void ResetClearsQueueAndActivatedPhases()
    {
        var state = new ArenaPhaseState();

        state.EnqueueMissing(InitialPhase, EmergencyPhase);
        state.MarkActivated(GravityPhase);
        state.Reset();

        Assert.That(state.CurrentPhase, Is.EqualTo(InitialPhase));
        Assert.That(state.ActivatedPhases, Is.Empty);
        Assert.That(state.TryDequeue(out _), Is.False);
    }

    [Test]
    public void InvalidPhasesAreIgnored()
    {
        var state = new ArenaPhaseState();

        state.EnqueueMissing(0, EmergencyPhase);
        state.EnqueueMissing(InitialPhase, 0);
        state.MarkActivated(0);
        state.MarkActivated(-1);

        Assert.That(state.CurrentPhase, Is.EqualTo(InitialPhase));
        Assert.That(state.ActivatedPhases, Is.Empty);
        Assert.That(state.TryDequeue(out _), Is.False);
    }

    private static int[] DequeueAll(ArenaPhaseState state)
    {
        var phases = new System.Collections.Generic.List<int>();
        while (state.TryDequeue(out int phase))
        {
            phases.Add(phase);
        }

        return phases.ToArray();
    }
}
