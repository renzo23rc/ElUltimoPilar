/// <summary>
/// Defines the player state and wave-ammunition operations used by the roster.
/// </summary>
public interface IPlayerRosterMember
{
    /// <summary>Gets whether the player is downed.</summary>
    bool IsDowned { get; }
    /// <summary>Restores the player's wave ammunition.</summary>
    void ReplenishWaveAmmo();
}
