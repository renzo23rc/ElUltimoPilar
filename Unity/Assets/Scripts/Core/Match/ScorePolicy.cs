using System;

/// <summary>
/// Converts the factual Pilar health percentage into the final integer score.
/// </summary>
public static class ScorePolicy
{
    private const double MinimumPercentage = 0d;
    private const double MaximumPercentage = 100d;

    /// <summary>
    /// Calculates a score from a valid Pilar health snapshot.
    /// </summary>
    /// <param name="pilarHealth">The Pilar health snapshot.</param>
    /// <returns>The rounded score from zero to one hundred.</returns>
    public static int Calculate(PilarHealthSnapshot pilarHealth)
    {
        if (!pilarHealth.IsValid)
                throw new ArgumentException("Score calculation requires a valid Pilar health snapshot.", nameof(pilarHealth));

        return Calculate(pilarHealth.RemainingPercentage);
    }

    /// <summary>
    /// Calculates a score from a remaining health percentage.
    /// </summary>
    /// <param name="remainingPercentage">The remaining health percentage.</param>
    /// <returns>The rounded score from zero to one hundred.</returns>
    public static int Calculate(float remainingPercentage)
    {
            if (float.IsNaN(remainingPercentage) || float.IsInfinity(remainingPercentage))
                throw new ArgumentOutOfRangeException(nameof(remainingPercentage), "Percentage must be finite.");

        double percentage = remainingPercentage;
        percentage = Math.Max(MinimumPercentage, Math.Min(MaximumPercentage, percentage));

        // The value is non-negative after clamping, so midpoint values round upward.
        return (int)Math.Round(percentage, MidpointRounding.AwayFromZero);
    }
}
