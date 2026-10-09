using System;

/// <summary>
/// Unity-free countdown between the end of a wave and the start of the next one.
/// </summary>
public sealed class WaveIntermission
{
    /// <summary>Gets whether the countdown is running.</summary>
    public bool IsWaiting { get; private set; }

    /// <summary>Gets the remaining seconds, or zero when idle.</summary>
    public float RemainingSeconds { get; private set; }

    /// <summary>Starts the countdown.</summary>
    /// <param name="durationSeconds">The pause length in seconds.</param>
    public void Begin(float durationSeconds)
    {
        if (float.IsNaN(durationSeconds))
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be a number.");

        IsWaiting = true;
        RemainingSeconds = durationSeconds;
    }

    /// <summary>Advances the countdown.</summary>
    /// <param name="deltaSeconds">The elapsed time in seconds.</param>
    /// <returns><see langword="true"/> once, on the tick that finishes the countdown.</returns>
    public bool Tick(float deltaSeconds)
    {
        if (!IsWaiting)
            return false;

        RemainingSeconds -= deltaSeconds;
        if (RemainingSeconds > 0f)
            return false;

        Cancel();
        return true;
    }

    /// <summary>Stops the countdown without finishing it.</summary>
    public void Cancel()
    {
        IsWaiting = false;
        RemainingSeconds = 0f;
    }
}
