using NUnit.Framework;

public class AutomaticWaveConfigFactoryTests
{
    private const float Tolerance = 0.0001f;
    private const float MinimumSpawnIntervalSeconds = 0.7f;

    [TestCase(1, 8, 4, 0, 0, 0, 0, 0)]
    [TestCase(2, 10, 5, 1, 0, 0, 0, 0)]
    [TestCase(3, 12, 6, 2, 1, 0, 0, 0)]
    [TestCase(4, 14, 8, 3, 2, 1, 0, 0)]
    [TestCase(5, 18, 9, 4, 2, 1, 1, 0)]
    [TestCase(8, 28, 12, 7, 4, 2, 1, 1)]
    [TestCase(10, 32, 14, 9, 5, 2, 1, 1)]
    public void CurveMatchesTheBalancedWaveComposition(
        int wave, int total, int runners, int artillery, int explosives, int weavers, int nests, int colossi)
    {
        EnemySpawner.ConfigOleada config = AutomaticWaveConfigFactory.Create(wave);

        Assert.That(config.numeroOleada, Is.EqualTo(wave));
        Assert.That(config.cantidadTotal, Is.EqualTo(total));
        Assert.That(config.corredores, Is.EqualTo(runners));
        Assert.That(config.artilleros, Is.EqualTo(artillery));
        Assert.That(config.explosivos, Is.EqualTo(explosives));
        Assert.That(config.tejedores, Is.EqualTo(weavers));
        Assert.That(config.nidos, Is.EqualTo(nests));
        Assert.That(config.colosos, Is.EqualTo(colossi));
    }

    [TestCase(7, 22, 9)]
    [TestCase(20, 52, 19)]
    public void RunnersAreTrimmedWhenTypedCountsExceedTheTotal(int wave, int total, int runners)
    {
        EnemySpawner.ConfigOleada config = AutomaticWaveConfigFactory.Create(wave);

        int typed = config.corredores + config.artilleros + config.explosivos
            + config.tejedores + config.nidos + config.colosos;
        Assert.That(config.cantidadTotal, Is.EqualTo(total));
        Assert.That(config.corredores, Is.EqualTo(runners));
        Assert.That(typed, Is.LessThanOrEqualTo(config.cantidadTotal));
    }

    [TestCase(1, 1.72f)]
    [TestCase(10, 1f)]
    [TestCase(20, MinimumSpawnIntervalSeconds)]
    public void SpawnIntervalShrinksUntilItsMinimum(int wave, float expectedSeconds)
    {
        Assert.That(AutomaticWaveConfigFactory.Create(wave).intervaloSpawn, Is.EqualTo(expectedSeconds).Within(Tolerance));
    }
}
