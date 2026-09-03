/// <summary>
/// States that a match can occupy during its lifecycle.
/// </summary>
public enum MatchState
{
    WaitingToStart,
    Playing,
    Paused,
    Victory,
    Defeat
}
