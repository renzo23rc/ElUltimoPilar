using System;

/// <summary>
/// Unity-free balance curve used when a wave has no configuration in the Inspector.
/// Balanceo B1: 10 oleadas escalables, 12-20 min totales (~70-120 s por oleada);
/// oleada 1 ~8 enemigos (~12 s de spawn), oleada 10 ~32 enemigos (~28 s de spawn) + combate.
/// </summary>
public static class AutomaticWaveConfigFactory
{
    private const int BaseEnemyCount = 6;
    private const int EnemiesPerWave = 2;
    private const int MidGameWave = 5;
    private const int MidGameBonusEnemies = 2;
    private const int LateGameWave = 8;
    private const int LateGameBonusEnemies = 4;
    private const int BaseRunners = 3;
    private const int RunnersPerWave = 1;
    private const int ExtraRunnerWave = 4;
    private const int ExtraRunners = 1;
    private const int FirstArtilleryWave = 2;
    private const int FirstExplosiveWave = 3;
    private const int ExplosiveWaveDivisor = 2;
    private const int FirstWeaverWave = 4;
    private const int SecondWeaverWave = 7;
    private const int FirstNestWave = 5;
    private const int FirstColossusWave = 7;
    private const int MinimumRunners = 1;
    private const float BaseSpawnIntervalSeconds = 1.8f;
    private const float SpawnIntervalReductionPerWaveSeconds = 0.08f;
    private const float MinimumSpawnIntervalSeconds = 0.7f;

    /// <summary>Creates the automatic configuration of a wave.</summary>
    /// <param name="wave">The one-based wave number.</param>
    /// <returns>A new configuration whose typed counts never exceed its total.</returns>
    public static EnemySpawner.ConfigOleada Create(int wave)
    {
        var config = new EnemySpawner.ConfigOleada
        {
            numeroOleada = wave,
            cantidadTotal = BaseEnemyCount + (wave * EnemiesPerWave)
                + (wave >= MidGameWave ? MidGameBonusEnemies : 0)
                + (wave >= LateGameWave ? LateGameBonusEnemies : 0),
            corredores = BaseRunners + (wave * RunnersPerWave) + (wave >= ExtraRunnerWave ? ExtraRunners : 0),
            artilleros = Math.Max(0, wave - (FirstArtilleryWave - 1)),
            // Menos spam explosivo: uno cada dos oleadas desde la tercera.
            explosivos = wave >= FirstExplosiveWave ? ((wave - (FirstExplosiveWave - 1)) / ExplosiveWaveDivisor) + 1 : 0,
            tejedores = (wave >= FirstWeaverWave ? 1 : 0) + (wave >= SecondWeaverWave ? 1 : 0),
            nidos = wave >= FirstNestWave ? 1 : 0,
            colosos = wave >= FirstColossusWave ? 1 : 0,
            // 1.7 s -> 1.0 s: evita una masacre instantánea al principio.
            intervaloSpawn = Math.Max(
                MinimumSpawnIntervalSeconds,
                BaseSpawnIntervalSeconds - (wave * SpawnIntervalReductionPerWaveSeconds))
        };

        // Si los tipos superan el total, se recortan corredores (el selector cubre el resto).
        int nonRunners = config.artilleros + config.explosivos + config.tejedores + config.nidos + config.colosos;
        if (config.corredores + nonRunners > config.cantidadTotal)
        {
            config.corredores = Math.Max(MinimumRunners, config.cantidadTotal - nonRunners);
        }

        return config;
    }
}
