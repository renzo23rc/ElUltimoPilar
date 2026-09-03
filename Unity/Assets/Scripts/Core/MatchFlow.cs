using System;

/// <summary>
/// Pure state machine for match lifecycle transitions.
/// Wave count is supplied by the composition root; this type owns no gameplay balance.
/// </summary>
public sealed class MatchFlow
{
    private readonly int totalWaves;

    public MatchState State { get; private set; }
    public MatchState CurrentState => State;
    public int CurrentWave { get; private set; }
    public int TotalWaves => totalWaves;

    public MatchFlow(int totalWaves)
    {
        if (totalWaves < 0)
            throw new ArgumentOutOfRangeException(nameof(totalWaves), "Total waves cannot be negative.");

        this.totalWaves = totalWaves;
        Reset();
    }

    public bool Start()
    {
        if (State != MatchState.WaitingToStart)
            return false;

        State = MatchState.Playing;
        return true;
    }

    public bool Pause()
    {
        if (State != MatchState.Playing)
            return false;

        State = MatchState.Paused;
        return true;
    }

    public bool Resume()
    {
        if (State != MatchState.Paused)
            return false;

        State = MatchState.Playing;
        return true;
    }

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

    public bool SetVictory()
    {
        if (!CanFinish())
            return false;

        State = MatchState.Victory;
        return true;
    }

    public bool SetDefeat()
    {
        if (!CanFinish())
            return false;

        State = MatchState.Defeat;
        return true;
    }

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
