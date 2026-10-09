using System;
using NUnit.Framework;

public class WaveEnemySelectorTests
{
    private static readonly Func<WaveEnemyType, bool> AllAvailable = type => true;

    private static EnemySpawner.ConfigOleada Config(
        int total, int runners = 0, int artillery = 0, int explosives = 0, int weavers = 0, int nests = 0, int colossi = 0)
    {
        return new EnemySpawner.ConfigOleada
        {
            cantidadTotal = total,
            corredores = runners,
            artilleros = artillery,
            explosivos = explosives,
            tejedores = weavers,
            nidos = nests,
            colosos = colossi
        };
    }

    [Test]
    public void RegularEnemiesFollowExplosiveArtilleryRunnerPriority()
    {
        EnemySpawner.ConfigOleada config = Config(3, runners: 1, artillery: 1, explosives: 1);

        WaveEnemySelector.TrySelect(config, 0, AllAvailable, out WaveEnemyType first);
        WaveEnemySelector.TrySelect(config, 1, AllAvailable, out WaveEnemyType second);
        WaveEnemySelector.TrySelect(config, 2, AllAvailable, out WaveEnemyType third);

        Assert.That(first, Is.EqualTo(WaveEnemyType.Explosive));
        Assert.That(second, Is.EqualTo(WaveEnemyType.Artillery));
        Assert.That(third, Is.EqualTo(WaveEnemyType.Runner));
    }

    [Test]
    public void SelectionConsumesTheChosenCount()
    {
        EnemySpawner.ConfigOleada config = Config(2, runners: 2);

        WaveEnemySelector.TrySelect(config, 0, AllAvailable, out _);

        Assert.That(config.corredores, Is.EqualTo(1));
    }

    [TestCase(8, false)]
    [TestCase(9, true)]
    public void ColossusWaitsForTheEndOfTheWave(int spawned, bool expectColossus)
    {
        EnemySpawner.ConfigOleada config = Config(10, runners: 9, colossi: 1);

        WaveEnemySelector.TrySelect(config, spawned, AllAvailable, out WaveEnemyType selected);

        Assert.That(selected == WaveEnemyType.Colossus, Is.EqualTo(expectColossus));
    }

    [TestCase(4, WaveEnemyType.Runner)]
    [TestCase(5, WaveEnemyType.Nest)]
    public void NestWaitsForHalfOfTheWave(int spawned, WaveEnemyType expected)
    {
        EnemySpawner.ConfigOleada config = Config(10, runners: 9, nests: 1);

        WaveEnemySelector.TrySelect(config, spawned, AllAvailable, out WaveEnemyType selected);

        Assert.That(selected, Is.EqualTo(expected));
    }

    [TestCase(2, WaveEnemyType.Runner)]
    [TestCase(3, WaveEnemyType.Weaver)]
    public void WeaverWaitsForAThirdOfTheWave(int spawned, WaveEnemyType expected)
    {
        EnemySpawner.ConfigOleada config = Config(9, runners: 8, weavers: 1);

        WaveEnemySelector.TrySelect(config, spawned, AllAvailable, out WaveEnemyType selected);

        Assert.That(selected, Is.EqualTo(expected));
    }

    [Test]
    public void MissingPrefabSkipsToTheNextAvailableType()
    {
        EnemySpawner.ConfigOleada config = Config(2, runners: 1, explosives: 1);

        WaveEnemySelector.TrySelect(config, 0, type => type != WaveEnemyType.Explosive, out WaveEnemyType selected);

        Assert.That(selected, Is.EqualTo(WaveEnemyType.Runner));
        Assert.That(config.explosivos, Is.EqualTo(1));
    }

    [Test]
    public void ExhaustedCountsFallBackToAnyBasicEnemy()
    {
        EnemySpawner.ConfigOleada config = Config(5);

        bool selected = WaveEnemySelector.TrySelect(config, 0, type => type == WaveEnemyType.Artillery, out WaveEnemyType type);

        Assert.That(selected, Is.True);
        Assert.That(type, Is.EqualTo(WaveEnemyType.Artillery));
    }

    [Test]
    public void NoAvailableTypeReturnsFalse()
    {
        bool selected = WaveEnemySelector.TrySelect(Config(1, runners: 1), 0, type => false, out _);

        Assert.That(selected, Is.False);
    }

    [Test]
    public void NullConfigurationIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => WaveEnemySelector.TrySelect(null, 0, AllAvailable, out _));
    }
}
