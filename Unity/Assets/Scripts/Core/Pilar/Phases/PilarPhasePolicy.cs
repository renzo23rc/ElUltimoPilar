using System;
using System.Collections.Generic;

namespace UltimoPilar.Core.Pilar;

/// <summary>Calculates Pilar phases from ordered health thresholds.</summary>
public sealed class PilarPhasePolicy
{
    private const int FirstPhase = 1;
    private const int PhaseOffset = 1;
    private readonly float[] thresholds;

    /// <summary>Creates a policy with thresholds ordered from highest to lowest health.</summary>
    public PilarPhasePolicy(float[] thresholds)
    {
        if (thresholds == null || thresholds.Length == 0)
        {
            throw new ArgumentException("At least one phase threshold is required.", nameof(thresholds));
        }

        for (int index = 0; index < thresholds.Length; index++)
        {
            if (float.IsNaN(thresholds[index]) || float.IsInfinity(thresholds[index]))
            {
                throw new ArgumentOutOfRangeException(nameof(thresholds), "Thresholds must be finite.");
            }

            if (index > 0 && thresholds[index] >= thresholds[index - 1])
            {
                throw new ArgumentException("Thresholds must be strictly descending.", nameof(thresholds));
            }
        }

        this.thresholds = (float[])thresholds.Clone();
    }

    /// <summary>Returns the one-based phase for the supplied health value.</summary>
    public int CalculatePhase(float currentHealth)
    {
        if (float.IsNaN(currentHealth) || float.IsInfinity(currentHealth))
        {
            throw new ArgumentOutOfRangeException(nameof(currentHealth), "Health must be finite.");
        }

        int phase = FirstPhase;
        foreach (float threshold in thresholds)
        {
            if (currentHealth <= threshold)
            {
                phase += PhaseOffset;
            }
        }

        return phase;
    }

    /// <summary>Returns every phase between the clamped current and target phases.</summary>
    public IReadOnlyList<int> GetSteps(int currentPhase, int targetPhase)
    {
        int maximumPhase = thresholds.Length + FirstPhase;
        int clampedCurrent = ClampPhase(currentPhase, maximumPhase);
        int clampedTarget = ClampPhase(targetPhase, maximumPhase);
        if (clampedCurrent == clampedTarget)
        {
            return Array.Empty<int>();
        }

        int direction = Math.Sign(clampedTarget - clampedCurrent);
        List<int> steps = new List<int>(Math.Abs(clampedTarget - clampedCurrent));

        for (int phase = clampedCurrent + direction;
             phase != clampedTarget + direction;
             phase += direction)
        {
            steps.Add(phase);
        }

        return steps;
    }

    private static int ClampPhase(int phase, int maximumPhase)
    {
        return Math.Min(Math.Max(phase, FirstPhase), maximumPhase);
    }
}
