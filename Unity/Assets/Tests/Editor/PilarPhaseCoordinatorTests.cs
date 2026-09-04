using NUnit.Framework;
using UltimoPilar.Core.Pilar;

public class PilarPhaseCoordinatorTests
{
    [Test]
    public void StepsForwardThroughEveryIntermediatePhase()
    {
        PilarPhaseCoordinator coordinator = CreateCoordinator(1);

        Assert.That(coordinator.StepToward(0f), Is.EqualTo(new[] { 2, 3, 4 }));
        Assert.That(coordinator.CurrentPhase, Is.EqualTo(4));
    }

    [Test]
    public void StepsBackwardThroughEveryIntermediatePhase()
    {
        PilarPhaseCoordinator coordinator = CreateCoordinator(4);

        Assert.That(coordinator.StepToward(100f), Is.EqualTo(new[] { 3, 2, 1 }));
        Assert.That(coordinator.CurrentPhase, Is.EqualTo(1));
    }

    [Test]
    public void ReturnsEmptyWhenHealthRemainsInCurrentPhase()
    {
        PilarPhaseCoordinator coordinator = CreateCoordinator(2);

        Assert.That(coordinator.StepToward(75f), Is.Empty);
        Assert.That(coordinator.CurrentPhase, Is.EqualTo(2));
    }

    [Test]
    public void ClampsInvalidCurrentPhaseBeforeStepping()
    {
        PilarPhaseCoordinator coordinator = CreateCoordinator(99);

        Assert.That(coordinator.StepToward(100f), Is.EqualTo(new[] { 3, 2, 1 }));
        Assert.That(coordinator.CurrentPhase, Is.EqualTo(1));
    }

    private static PilarPhaseCoordinator CreateCoordinator(int currentPhase)
    {
        return new PilarPhaseCoordinator(new[] { 75f, 50f, 25f }, currentPhase);
    }
}
