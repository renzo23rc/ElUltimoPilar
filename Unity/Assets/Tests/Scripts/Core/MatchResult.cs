using System;

/// <summary>
/// Immutable result published when a match reaches a terminal state.
/// </summary>
public sealed class MatchResult
{
    public MatchState Outcome { get; }
    public PilarHealthSnapshot PilarHealth { get; }

    public MatchResult(MatchState outcome, PilarHealthSnapshot pilarHealth)
    {
        if (outcome != MatchState.Victory && outcome != MatchState.Defeat)
            throw new ArgumentOutOfRangeException(nameof(outcome), "A match result must have a terminal outcome.");
        if (!pilarHealth.IsValid)
            throw new ArgumentException("A match result requires a valid Pilar health snapshot.", nameof(pilarHealth));

        Outcome = outcome;
        PilarHealth = pilarHealth;
    }
}
