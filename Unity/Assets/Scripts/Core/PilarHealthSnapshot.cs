using System;

/// <summary>
/// Immutable factual snapshot of the Pilar health at a match boundary.
/// </summary>
public readonly struct PilarHealthSnapshot : IEquatable<PilarHealthSnapshot>
{
    private const float PercentageUnit = 100f;

    public float Remaining { get; }
    public float Maximum { get; }

    /// <summary>
    /// Indicates whether this value contains a valid health snapshot.
    /// The default value of a value type is not a valid snapshot.
    /// </summary>
    public bool IsValid => IsValidHealth(Remaining, Maximum);

    /// <summary>
    /// Returns the remaining health as a ratio between zero and one.
    /// </summary>
    public float RemainingRatio => IsValid ? Remaining / Maximum : 0f;

    /// <summary>
    /// Returns the factual remaining health percentage, not a gameplay metric.
    /// </summary>
    public float RemainingPercentage => RemainingRatio * PercentageUnit;

    public PilarHealthSnapshot(float remaining, float maximum)
    {
        if (!IsValidHealth(remaining, maximum))
            throw new ArgumentOutOfRangeException(nameof(remaining), "Pilar health must be finite, non-negative, and no greater than a positive finite maximum.");

        Remaining = remaining;
        Maximum = maximum;
    }

    /// <summary>
    /// Attempts to create a valid snapshot without throwing for malformed adapter input.
    /// </summary>
    public static bool TryCreate(float remaining, float maximum, out PilarHealthSnapshot snapshot)
    {
        if (!IsValidHealth(remaining, maximum))
        {
            snapshot = default(PilarHealthSnapshot);
            return false;
        }

        snapshot = new PilarHealthSnapshot(remaining, maximum);
        return true;
    }

    public bool Equals(PilarHealthSnapshot other)
    {
        return Remaining.Equals(other.Remaining) && Maximum.Equals(other.Maximum);
    }

    public override bool Equals(object obj)
    {
        return obj is PilarHealthSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Remaining.GetHashCode() * 397) ^ Maximum.GetHashCode();
        }
    }

    public static bool operator ==(PilarHealthSnapshot left, PilarHealthSnapshot right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PilarHealthSnapshot left, PilarHealthSnapshot right)
    {
        return !left.Equals(right);
    }

    private static bool IsValidHealth(float remaining, float maximum)
    {
        return IsFinite(remaining) &&
            IsFinite(maximum) &&
            maximum > 0f &&
            remaining >= 0f &&
            remaining <= maximum;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
