using System;
using System.Collections.Generic;

/// <summary>
/// Unity-free registration and state boundary for the players in a match.
/// </summary>
public sealed class PlayerRoster<TPlayer> where TPlayer : class, IPlayerRosterMember
{
    /// <summary>Minimum supported player capacity.</summary>
    public const int MinimumCapacity = 1;
    /// <summary>Maximum supported player capacity.</summary>
    public const int MaximumCapacity = 4;

    private const int NotFoundIndex = -1;

    private readonly List<TPlayer> players;
    private readonly IReadOnlyList<TPlayer> readOnlyPlayers;

    /// <summary>
    /// Creates a roster with the specified capacity.
    /// </summary>
    public PlayerRoster(int capacity)
    {
        if (capacity < MinimumCapacity || capacity > MaximumCapacity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                $"Player roster capacity must be between {MinimumCapacity} and {MaximumCapacity}.");
        }

        Capacity = capacity;
        players = new List<TPlayer>(capacity);
        readOnlyPlayers = players.AsReadOnly();
    }

    /// <summary>Gets the roster capacity.</summary>
    public int Capacity { get; }
    /// <summary>Gets registered players in insertion order.</summary>
    public IReadOnlyList<TPlayer> Players => readOnlyPlayers;
    /// <summary>Gets the number of registered players.</summary>
    public int Count => players.Count;
    /// <summary>Gets the number of downed players.</summary>
    public int DownedCount => CountPlayers(isDowned: true);
    /// <summary>Gets the number of standing players.</summary>
    public int StandingCount => CountPlayers(isDowned: false);
    /// <summary>Gets whether every registered player is downed.</summary>
    public bool AreAllDowned => Count > 0 && DownedCount == Count;

    /// <summary>Registers a player when capacity and uniqueness permit it.</summary>
    public bool Register(TPlayer player)
    {
        if (player == null || IsRegistered(player) || Count >= Capacity)
            return false;

        players.Add(player);
        return true;
    }

    /// <summary>Removes a registered player.</summary>
    public bool Unregister(TPlayer player)
    {
        var playerIndex = FindRegisteredPlayerIndex(player);
        if (playerIndex == NotFoundIndex)
            return false;

        players.RemoveAt(playerIndex);
        return true;
    }

    /// <summary>Restores ammunition for every registered player.</summary>
    public void ReplenishWaveAmmo()
    {
        foreach (var player in players)
            player.ReplenishWaveAmmo();
    }

    private bool IsRegistered(TPlayer player)
    {
        return FindRegisteredPlayerIndex(player) != NotFoundIndex;
    }

    private int FindRegisteredPlayerIndex(TPlayer player)
    {
        if (player == null)
            return NotFoundIndex;

        for (var playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            if (ReferenceEquals(players[playerIndex], player))
                return playerIndex;
        }

        return NotFoundIndex;
    }

    private int CountPlayers(bool isDowned)
    {
        var count = 0;
        foreach (var player in players)
        {
            if (player.IsDowned == isDowned)
                count++;
        }

        return count;
    }
}
