using System;

/// <summary>
/// Pure state machine for match lifecycle transitions.
/// Wave count is supplied by the composition root; this type owns no gameplay balance.
/// </summary>
public sealed class MatchFlow
{
    private readonly int totalWaves;

    /// <summary>
    /// Gets the current match state.
    /// </summary>
    public MatchState State { get; private set; }
    public MatchState CurrentState => State;

    /// <summary>
    /// Gets the current wave number.
    /// </summary>
    public int CurrentWave { get; private set; }

    /// <summary>
    /// Gets the configured total number of waves.
    /// </summary>
    public int TotalWaves => totalWaves;

    /// <summary>
    /// Initializes a match flow with the configured wave count.
    /// </summary>
    /// <param name="totalWaves">The total number of waves.</param>
    public MatchFlow(int totalWaves)
    {
        if (totalWaves < 0)
            throw new ArgumentOutOfRangeException(nameof(totalWaves), "Total waves cannot be negative.");

        this.totalWaves = totalWaves;
        Reset();
    }

    /// <summary>
    /// Starts the match.
    /// </summary>
    /// <returns><see langword="true"/> when the transition succeeds.</returns>
    public bool Start()
    {
        if (State != MatchState.WaitingToStart)
            return false;

        State = MatchState.Playing;
        return true;
    }

    /// <summary>
    /// Pauses a playing match.
    /// </summary>
    /// <returns><see langword="true"/> when the transition succeeds.</returns>
    public bool Pause()
    {
        if (State != MatchState.Playing)
            return false;

        State = MatchState.Paused;
        return true;
    }

    /// <summary>
    /// Resumes a paused match.
    /// </summary>
    /// <returns><see langword="true"/> when the transition succeeds.</returns>
    public bool Resume()
    {
        if (State != MatchState.Paused)
            return false;

        State = MatchState.Playing;
        return true;
    }

    /// <summary>
    /// Attempts to advance to the next wave.
    /// </summary>
    /// <returns><see langword="true"/> when a wave starts.</returns>
    public bool TryStartNextWave()
    {
        if (State != MatchState.Playing)
            return false;

        if (CurrentWave >= totalWaves)
        {
            State = MatchState.Victory;
            return false;
        }

        CurrentWave++;
        return true;
    }

    /// <summary>
    /// Transitions the match to victory.
    /// </summary>
    /// <returns><see langword="true"/> when the transition succeeds.</returns>
    public bool SetVictory()
    {
        if (!CanFinish())
            return false;

        State = MatchState.Victory;
        return true;
    }

    /// <summary>
    /// Transitions the match to defeat.
    /// </summary>
    /// <returns><see langword="true"/> when the transition succeeds.</returns>
    public bool SetDefeat()
    {
        if (!CanFinish())
            return false;

        State = MatchState.Defeat;
        return true;
    }

    /// <summary>
    /// Resets the flow to its initial state.
    /// </summary>
    public void Reset()
    {
        State = MatchState.WaitingToStart;
        CurrentWave = 0;
    }

    bool CanFinish()
    {
        return State == MatchState.Playing || State == MatchState.Paused;
    }
}
