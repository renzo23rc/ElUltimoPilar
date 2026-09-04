using NUnit.Framework;
using UltimoPilar.Core.Pilar;

public class PilarPhasePolicyTests
{
    [Test]
    public void CalculatesPhasesAtThresholdEdges()
    {
        var policy = new PilarPhasePolicy(new[] { 75f, 50f, 25f });

        Assert.That(policy.CalculatePhase(100f), Is.EqualTo(1));
        Assert.That(policy.CalculatePhase(75f), Is.EqualTo(2));
        Assert.That(policy.CalculatePhase(50f), Is.EqualTo(3));
        Assert.That(policy.CalculatePhase(25f), Is.EqualTo(4));
        Assert.That(policy.CalculatePhase(0f), Is.EqualTo(4));
    }

    [Test]
    public void ReturnsOrderedIntermediatePhases()
    {
        var policy = new PilarPhasePolicy(new[] { 75f, 50f, 25f });

        Assert.That(policy.GetSteps(1, 4), Is.EqualTo(new[] { 2, 3, 4 }));
        Assert.That(policy.GetSteps(4, 1), Is.EqualTo(new[] { 3, 2, 1 }));
        Assert.That(policy.GetSteps(2, 2), Is.Empty);
    }

    [Test]
    public void ClampsInvalidPhaseValues()
    {
        var policy = new PilarPhasePolicy(new[] { 75f, 50f, 25f });

        Assert.That(policy.GetSteps(0, 99), Is.EqualTo(new[] { 2, 3, 4 }));
        Assert.That(policy.GetSteps(99, 0), Is.EqualTo(new[] { 3, 2, 1 }));
    }

    [Test]
    public void RejectsInvalidThresholdOrder()
    {
        Assert.That(() => new PilarPhasePolicy(new[] { 25f, 50f }),
            Throws.TypeOf<System.ArgumentException>());
        Assert.That(() => new PilarPhasePolicy(new[] { 75f, float.NaN, 25f }),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void RejectsInvalidHealthValues()
    {
        var policy = new PilarPhasePolicy(new[] { 75f, 50f, 25f });

        Assert.That(() => policy.CalculatePhase(float.NaN),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => policy.CalculatePhase(float.PositiveInfinity),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }
}
