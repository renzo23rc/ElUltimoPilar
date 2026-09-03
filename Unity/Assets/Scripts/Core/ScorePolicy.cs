using System;

/// <summary>
/// Converts the factual Pilar health percentage into the final integer score.
/// </summary>
public static class ScorePolicy
{
    private const double MinimumPercentage = 0d;
    private const double MaximumPercentage = 100d;

    public static int Calculate(PilarHealthSnapshot pilarHealth)
    {
        return Calculate(pilarHealth.RemainingPercentage);
    }

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
