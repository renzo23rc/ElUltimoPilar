using System;
using System.Collections.Generic;

/// <summary>
/// Unity-free registration and state boundary for the players in a match.
/// </summary>
public sealed class PlayerRoster<TPlayer> where TPlayer : class, IPlayerRosterMember
{
    public const int MinimumCapacity = 1;
    public const int MaximumCapacity = 4;

    private readonly List<TPlayer> players;
    private readonly IReadOnlyList<TPlayer> readOnlyPlayers;

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

    public int Capacity { get; }
    public IReadOnlyList<TPlayer> Players => readOnlyPlayers;
    public int Count => players.Count;
    public int DownedCount => CountPlayers(isDowned: true);
    public int StandingCount => CountPlayers(isDowned: false);
    public bool AreAllDowned => Count > 0 && DownedCount == Count;

    public bool Register(TPlayer player)
    {
        if (player == null || players.Contains(player) || Count >= Capacity)
            return false;

        players.Add(player);
        return true;
    }

    public bool Unregister(TPlayer player)
    {
        return player != null && players.Remove(player);
    }

    public void ReplenishWaveAmmo()
    {
        foreach (var player in players)
            player.ReplenishWaveAmmo();
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
