using System;

/// <summary>
/// Unity-free choice of the next enemy of a wave. Special enemies wait for their share of the wave
/// (Weavers after a third, Nests after half, Colossi at the end); regular enemies follow priority order.
/// </summary>
public static class WaveEnemySelector
{
    private const int NestWaveShareDivisor = 2;
    private const int WeaverWaveShareDivisor = 3;

    private static readonly WaveEnemyType[] FallbackOrder =
    {
        WaveEnemyType.Runner,
        WaveEnemyType.Artillery,
        WaveEnemyType.Explosive
    };

    /// <summary>
    /// Selects the next enemy and consumes one unit of its remaining count in the configuration.
    /// </summary>
    /// <param name="config">The private copy of the wave configuration; its counters are decremented.</param>
    /// <param name="spawnedCount">The enemies already spawned in this wave.</param>
    /// <param name="isAvailable">Whether a prefab exists for an enemy type.</param>
    /// <param name="selected">The selected enemy type.</param>
    /// <returns><see langword="false"/> when no enemy type is available at all.</returns>
    public static bool TrySelect(
        EnemySpawner.ConfigOleada config,
        int spawnedCount,
        Func<WaveEnemyType, bool> isAvailable,
        out WaveEnemyType selected)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (isAvailable == null)
            throw new ArgumentNullException(nameof(isAvailable));

        if (config.colosos > 0 && spawnedCount >= config.cantidadTotal - config.colosos && isAvailable(WaveEnemyType.Colossus))
        {
            config.colosos--;
            selected = WaveEnemyType.Colossus;
            return true;
        }

        if (config.nidos > 0 && spawnedCount >= config.cantidadTotal / NestWaveShareDivisor && isAvailable(WaveEnemyType.Nest))
        {
            config.nidos--;
            selected = WaveEnemyType.Nest;
            return true;
        }

        if (config.tejedores > 0 && spawnedCount >= config.cantidadTotal / WeaverWaveShareDivisor && isAvailable(WaveEnemyType.Weaver))
        {
            config.tejedores--;
            selected = WaveEnemyType.Weaver;
            return true;
        }

        if (config.explosivos > 0 && isAvailable(WaveEnemyType.Explosive))
        {
            config.explosivos--;
            selected = WaveEnemyType.Explosive;
            return true;
        }

        if (config.artilleros > 0 && isAvailable(WaveEnemyType.Artillery))
        {
            config.artilleros--;
            selected = WaveEnemyType.Artillery;
            return true;
        }

        if (config.corredores > 0 && isAvailable(WaveEnemyType.Runner))
        {
            config.corredores--;
            selected = WaveEnemyType.Runner;
            return true;
        }

        // Sin cupo específico: cualquier enemigo básico disponible completa la oleada.
        foreach (WaveEnemyType fallback in FallbackOrder)
        {
            if (isAvailable(fallback))
            {
                selected = fallback;
                return true;
            }
        }

        selected = default;
        return false;
    }
}
