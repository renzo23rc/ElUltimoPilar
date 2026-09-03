using System;

/// <summary>
/// Immutable result published when a match reaches a terminal state.
/// </summary>
public sealed class MatchResult
{
    /// <summary>
    /// Gets the terminal match outcome.
    /// </summary>
    public MatchState Outcome { get; }

    /// <summary>
    /// Gets the factual Pilar health snapshot.
    /// </summary>
    public PilarHealthSnapshot PilarHealth { get; }

    /// <summary>
    /// Gets the score calculated from the Pilar health snapshot.
    /// </summary>
    public int Score { get; }

    /// <summary>
    /// Initializes a terminal match result.
    /// </summary>
    /// <param name="outcome">The terminal outcome.</param>
    /// <param name="pilarHealth">The factual Pilar health snapshot.</param>
    public MatchResult(MatchState outcome, PilarHealthSnapshot pilarHealth)
    {
        if (outcome != MatchState.Victory && outcome != MatchState.Defeat)
            throw new ArgumentOutOfRangeException(nameof(outcome), "A match result must have a terminal outcome.");
        if (!pilarHealth.IsValid)
            throw new ArgumentException("A match result requires a valid Pilar health snapshot.", nameof(pilarHealth));

        Outcome = outcome;
        PilarHealth = pilarHealth;
        Score = ScorePolicy.Calculate(pilarHealth);
    }
}
