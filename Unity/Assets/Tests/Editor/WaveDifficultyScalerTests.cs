using System;
using NUnit.Framework;

public class WaveDifficultyScalerTests
{
    private const float ExtraRatio = 0.35f;
    private const float Tolerance = 0.0001f;

    [TestCase(1, 1f)]
    [TestCase(2, 1.35f)]
    [TestCase(3, 1.7f)]
    [TestCase(4, 2.05f)]
    public void MultiplierGrowsLinearlyWithExtraPlayers(int players, float expected)
    {
        Assert.That(WaveDifficultyScaler.GetMultiplier(players, ExtraRatio), Is.EqualTo(expected).Within(Tolerance));
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void NoPlayersOrInvalidCountsDoNotReduceDifficulty(int players)
    {
        Assert.That(WaveDifficultyScaler.GetMultiplier(players, ExtraRatio), Is.EqualTo(1f));
    }

    [Test]
    public void NegativeRatioIsIgnored()
    {
        Assert.That(WaveDifficultyScaler.GetMultiplier(4, -1f), Is.EqualTo(1f));
    }

    [TestCase(4, 1.35f, 6)]
    [TestCase(4, 1f, 4)]
    [TestCase(10, 1.7f, 17)]
    [TestCase(0, 2f, 0)]
    [TestCase(1, 1.35f, 2)]
    public void CountsRoundUp(int baseCount, float multiplier, int expected)
    {
        Assert.That(WaveDifficultyScaler.ScaleCount(baseCount, multiplier), Is.EqualTo(expected));
    }

    [Test]
    public void SinglePlayerLeavesTheWaveUnchanged()
    {
        EnemySpawner.ConfigOleada config = CreateConfig();

        WaveDifficultyScaler.Apply(config, 1, ExtraRatio);

        Assert.That(config.cantidadTotal, Is.EqualTo(10));
        Assert.That(config.corredores, Is.EqualTo(4));
    }

    [Test]
    public void RegularEnemiesScaleButNestsAndColossiStayFixed()
    {
        EnemySpawner.ConfigOleada config = CreateConfig();

        WaveDifficultyScaler.Apply(config, 2, ExtraRatio);

        Assert.That(config.corredores, Is.EqualTo(6));
        Assert.That(config.artilleros, Is.EqualTo(3));
        Assert.That(config.explosivos, Is.EqualTo(2));
        Assert.That(config.tejedores, Is.EqualTo(2));
        Assert.That(config.nidos, Is.EqualTo(1));
        Assert.That(config.colosos, Is.EqualTo(1));
        Assert.That(config.cantidadTotal, Is.EqualTo(15));
    }

    [Test]
    public void TotalNeverFallsBelowTheSumOfTypedEnemies()
    {
        EnemySpawner.ConfigOleada config = CreateConfig();
        config.cantidadTotal = 1;

        WaveDifficultyScaler.Apply(config, 4, ExtraRatio);

        int typed = config.corredores + config.artilleros + config.explosivos + config.tejedores + config.nidos + config.colosos;
        Assert.That(config.cantidadTotal, Is.EqualTo(typed));
    }

    [Test]
    public void NullConfigIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => WaveDifficultyScaler.Apply(null, 2, ExtraRatio));
    }

    private static EnemySpawner.ConfigOleada CreateConfig()
    {
        return new EnemySpawner.ConfigOleada
        {
            numeroOleada = 7,
            cantidadTotal = 10,
            corredores = 4,
            artilleros = 2,
            explosivos = 1,
            tejedores = 1,
            nidos = 1,
            colosos = 1
        };
    }
}
