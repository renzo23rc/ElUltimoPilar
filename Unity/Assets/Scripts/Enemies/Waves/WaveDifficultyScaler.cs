using System;

/// <summary>
/// Unity-free scaling of wave enemy counts by the number of players.
/// Regular enemies scale; structures and mini-bosses (Nest, Colossus) stay fixed.
/// </summary>
public static class WaveDifficultyScaler
{
    private const int SinglePlayer = 1;
    private const float NeutralMultiplier = 1f;
    private const double RoundingTolerance = 1e-6;

    /// <summary>Gets the enemy multiplier for the player count.</summary>
    /// <param name="playerCount">The number of registered players.</param>
    /// <param name="extraRatioPerPlayer">The extra enemy ratio added by each player beyond the first.</param>
    /// <returns>1 for a single player, growing linearly with each extra player.</returns>
    public static float GetMultiplier(int playerCount, float extraRatioPerPlayer)
    {
        int extraPlayers = Math.Max(0, playerCount - SinglePlayer);
        return NeutralMultiplier + extraPlayers * Math.Max(0f, extraRatioPerPlayer);
    }

    /// <summary>Scales a count, rounding up so every extra player adds pressure.</summary>
    /// <param name="baseCount">The count for one player.</param>
    /// <param name="multiplier">The multiplier from <see cref="GetMultiplier"/>.</param>
    /// <returns>The scaled count; non-positive counts are returned unchanged.</returns>
    public static int ScaleCount(int baseCount, float multiplier)
    {
        if (baseCount <= 0 || multiplier <= NeutralMultiplier)
        {
            return baseCount;
        }

        return (int)Math.Ceiling(baseCount * (double)multiplier - RoundingTolerance);
    }

    /// <summary>Scales a wave configuration in place for the player count.</summary>
    /// <param name="config">The wave configuration to scale (already a private copy).</param>
    /// <param name="playerCount">The number of registered players.</param>
    /// <param name="extraRatioPerPlayer">The extra enemy ratio added by each player beyond the first.</param>
    public static void Apply(EnemySpawner.ConfigOleada config, int playerCount, float extraRatioPerPlayer)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        float multiplier = GetMultiplier(playerCount, extraRatioPerPlayer);
        if (multiplier <= NeutralMultiplier)
        {
            return;
        }

        config.corredores = ScaleCount(config.corredores, multiplier);
        config.artilleros = ScaleCount(config.artilleros, multiplier);
        config.explosivos = ScaleCount(config.explosivos, multiplier);
        config.tejedores = ScaleCount(config.tejedores, multiplier);

        int typedTotal = config.corredores + config.artilleros + config.explosivos
            + config.tejedores + config.nidos + config.colosos;
        config.cantidadTotal = Math.Max(ScaleCount(config.cantidadTotal, multiplier), typedTotal);
    }
}
