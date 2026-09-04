using NUnit.Framework;

public class EnemyHealthTests
{
    private const float EmptyHealth = 0f;
    private const float StandardHealth = 100f;
    private const float OverkillHealth = 150f;
    private const float NegativeHealth = -25f;
    private const float EmptyFraction = 0f;
    private const float FullFraction = 1f;

    [Test]
    public void ZeroCurrentHealthMapsToEmptyFraction()
    {
        float fraction = EnemyHealthBar.CalculateHealthFraction(EmptyHealth, StandardHealth);

        Assert.That(fraction, Is.EqualTo(EmptyFraction));
    }

    [Test]
    public void MaximumCurrentHealthMapsToFullFraction()
    {
        float fraction = EnemyHealthBar.CalculateHealthFraction(StandardHealth, StandardHealth);

        Assert.That(fraction, Is.EqualTo(FullFraction));
    }

    [Test]
    public void OverkillCurrentHealthIsClampedToFullFraction()
    {
        float fraction = EnemyHealthBar.CalculateHealthFraction(OverkillHealth, StandardHealth);

        Assert.That(fraction, Is.EqualTo(FullFraction));
    }

    [Test]
    public void NegativeCurrentHealthMapsToEmptyFraction()
    {
        float fraction = EnemyHealthBar.CalculateHealthFraction(NegativeHealth, StandardHealth);

        Assert.That(fraction, Is.EqualTo(EmptyFraction));
    }

    [Test]
    public void ZeroMaximumHealthMapsToEmptyFraction()
    {
        float fraction = EnemyHealthBar.CalculateHealthFraction(StandardHealth, EmptyHealth);

        Assert.That(fraction, Is.EqualTo(EmptyFraction));
    }

    [Test]
    public void InvalidMaximumHealthMapsToEmptyFraction()
    {
        var invalidMaximumHealthValues = new[]
        {
            -StandardHealth,
            float.NaN,
            float.PositiveInfinity,
            float.NegativeInfinity
        };

        foreach (float invalidMaximumHealth in invalidMaximumHealthValues)
        {
            float fraction = EnemyHealthBar.CalculateHealthFraction(StandardHealth, invalidMaximumHealth);

            Assert.That(fraction, Is.EqualTo(EmptyFraction));
        }
    }
}
