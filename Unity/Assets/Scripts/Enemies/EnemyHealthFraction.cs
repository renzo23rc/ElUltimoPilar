using UnityEngine;

/// <summary>Converts enemy health into a safe 0–1 bar fill, treating invalid maximums as empty.</summary>
internal static class EnemyHealthFraction
{
    private const float EmptyFraction = 0f;
    private const float FullFraction = 1f;

    public static float Calculate(float currentHealth, float maximumHealth)
    {
        if (!IsValidMaximum(maximumHealth) || float.IsNaN(currentHealth) || currentHealth <= EmptyFraction)
        {
            return EmptyFraction;
        }

        if (currentHealth >= maximumHealth)
        {
            return FullFraction;
        }

        return Mathf.Clamp01(currentHealth / maximumHealth);
    }

    private static bool IsValidMaximum(float maximumHealth)
    {
        return !float.IsNaN(maximumHealth)
            && !float.IsInfinity(maximumHealth)
            && maximumHealth > EmptyFraction;
    }
}
