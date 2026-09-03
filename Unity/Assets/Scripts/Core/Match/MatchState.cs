/// <summary>
/// States that a match can occupy during its lifecycle.
/// </summary>
public enum MatchState
{
    /// <summary>
    /// The match is waiting to start.
    /// </summary>
    WaitingToStart,

    /// <summary>
    /// The match is actively playing.
    /// </summary>
    Playing,

    /// <summary>
    /// The match is paused.
    /// </summary>
    Paused,

    /// <summary>
    /// The match has been won.
    /// </summary>
    Victory,

    /// <summary>
    /// The match has been lost.
    /// </summary>
    Defeat
}
