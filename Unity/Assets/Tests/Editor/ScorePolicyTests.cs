using NUnit.Framework;

public class ScorePolicyTests
{
    private const float MaximumHealth = 100f;

    [TestCase(0f, 0)]
    [TestCase(49.49f, 49)]
    [TestCase(49.5f, 50)]
    [TestCase(50f, 50)]
    [TestCase(50.49f, 50)]
    [TestCase(50.5f, 51)]
    [TestCase(99.5f, 100)]
    [TestCase(100f, 100)]
    public void CalculateRoundsRemainingPercentageWithUpwardMidpoint(float remainingPercentage, int expectedScore)
    {
        var snapshot = new PilarHealthSnapshot(remainingPercentage, MaximumHealth);

        Assert.That(ScorePolicy.Calculate(snapshot), Is.EqualTo(expectedScore));
    }

    [TestCase(-25f, 0)]
    [TestCase(125f, 100)]
    public void CalculateClampsPercentageBeforeRounding(float remainingPercentage, int expectedScore)
    {
        Assert.That(ScorePolicy.Calculate(remainingPercentage), Is.EqualTo(expectedScore));
    }

    [Test]
    public void CalculateRejectsInvalidHealthSnapshot()
    {
        Assert.That(() => ScorePolicy.Calculate(default(PilarHealthSnapshot)),
                Throws.TypeOf<System.ArgumentException>());
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void CalculateRejectsNonFinitePercentage(float remainingPercentage)
    {
        Assert.That(() => ScorePolicy.Calculate(remainingPercentage),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }
}
