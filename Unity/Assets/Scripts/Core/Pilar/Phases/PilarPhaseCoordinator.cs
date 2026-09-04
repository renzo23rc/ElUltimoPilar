using System;
using System.Collections.Generic;

namespace UltimoPilar.Core.Pilar
{
    /// <summary>Coordinates phase calculation and mirrors the current Pilar phase.</summary>
    public sealed class PilarPhaseCoordinator
{
    private float[] thresholds;
    private PilarPhasePolicy phasePolicy;
    private int currentPhase;

    /// <summary>Creates a coordinator with the supplied thresholds and current phase.</summary>
    public PilarPhaseCoordinator(float[] thresholds, int currentPhase)
    {
        if (thresholds == null)
        {
            throw new ArgumentNullException(nameof(thresholds));
        }

        phasePolicy = new PilarPhasePolicy(thresholds);
        this.thresholds = (float[])thresholds.Clone();
        this.currentPhase = currentPhase;
    }

    /// <summary>Gets the phase mirrored by the coordinator.</summary>
    public int CurrentPhase => currentPhase;

    /// <summary>Calculates and records every phase step needed for the supplied health.</summary>
    public IReadOnlyList<int> StepToward(float currentHealth)
    {
        int targetPhase = phasePolicy.CalculatePhase(currentHealth);
        IReadOnlyList<int> steps = phasePolicy.GetSteps(currentPhase, targetPhase);

        foreach (int phase in steps)
        {
            currentPhase = phase;
        }

        currentPhase = targetPhase;
        return steps;
    }

    /// <summary>Rebuilds the policy only when threshold values have changed.</summary>
    public void UpdateThresholds(float[] updatedThresholds)
    {
        if (updatedThresholds == null)
        {
            throw new ArgumentNullException(nameof(updatedThresholds));
        }

        if (ThresholdsMatch(updatedThresholds))
        {
            return;
        }

        phasePolicy = new PilarPhasePolicy(updatedThresholds);
        thresholds = (float[])updatedThresholds.Clone();
    }

    /// <summary>Resets the mirrored phase without replacing the policy.</summary>
    public void ResetTo(int phase)
    {
        currentPhase = phase;
    }

    private bool ThresholdsMatch(float[] updatedThresholds)
    {
        if (updatedThresholds.Length != thresholds.Length)
        {
            return false;
        }

        for (int index = 0; index < thresholds.Length; index++)
        {
            if (thresholds[index] != updatedThresholds[index])
            {
                return false;
            }
        }

        return true;
    }
}
}
