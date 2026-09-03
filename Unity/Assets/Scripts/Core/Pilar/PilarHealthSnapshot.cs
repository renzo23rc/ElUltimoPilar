using System;

/// <summary>
/// Immutable factual snapshot of the Pilar health at a match boundary.
/// </summary>
public readonly struct PilarHealthSnapshot : IEquatable<PilarHealthSnapshot>
{
    private const float PercentageUnit = 100f;
    private const float MinimumHealth = 0f;

    /// <summary>Gets the remaining health.</summary>
    public float Remaining { get; }
    /// <summary>Gets the maximum health.</summary>
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

    /// <summary>
    /// Creates a validated immutable health snapshot.
    /// </summary>
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

    /// <summary>Determines whether this snapshot equals another snapshot.</summary>
    public bool Equals(PilarHealthSnapshot other)
    {
        return Remaining.Equals(other.Remaining) && Maximum.Equals(other.Maximum);
    }

    /// <summary>Determines whether this snapshot equals another object.</summary>
    public override bool Equals(object obj)
    {
        return obj is PilarHealthSnapshot other && Equals(other);
    }

    /// <summary>Returns the hash code for this snapshot.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Remaining.GetHashCode() * 397) ^ Maximum.GetHashCode();
        }
    }

    /// <summary>Determines whether two snapshots are equal.</summary>
    public static bool operator ==(PilarHealthSnapshot left, PilarHealthSnapshot right)
    {
        return left.Equals(right);
    }

    /// <summary>Determines whether two snapshots are different.</summary>
    public static bool operator !=(PilarHealthSnapshot left, PilarHealthSnapshot right)
    {
        return !left.Equals(right);
    }

    private static bool IsValidHealth(float remaining, float maximum)
    {
        return IsFinite(remaining) &&
            IsFinite(maximum) &&
            maximum > MinimumHealth &&
            remaining >= MinimumHealth &&
            remaining <= maximum;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
