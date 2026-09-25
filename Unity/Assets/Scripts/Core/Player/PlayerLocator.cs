using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Finds registered players by distance for targeting, pickups, and revives.</summary>
public static class PlayerLocator
{
    /// <summary>Returns the closest player that satisfies the optional filter and range.</summary>
    /// <param name="players">The candidate players.</param>
    /// <param name="position">The reference position.</param>
    /// <param name="filter">An optional predicate every candidate must satisfy.</param>
    /// <param name="maxDistanceMeters">The maximum accepted distance in meters.</param>
    /// <returns>The closest matching player, or <see langword="null"/>.</returns>
    public static PlayerController FindClosest(
        IReadOnlyList<PlayerController> players,
        Vector3 position,
        Func<PlayerController, bool> filter = null,
        float maxDistanceMeters = float.MaxValue)
    {
        if (players == null)
        {
            return null;
        }

        PlayerController closest = null;
        float closestDistance = float.MaxValue;
        foreach (PlayerController player in players)
        {
            if (player == null || (filter != null && !filter(player)))
            {
                continue;
            }

            float distance = Vector3.Distance(position, player.transform.position);
            if (distance <= maxDistanceMeters && distance < closestDistance)
            {
                closestDistance = distance;
                closest = player;
            }
        }

        return closest;
    }

    /// <summary>Returns the closest registered player, falling back to any scene player without a manager.</summary>
    /// <param name="position">The reference position.</param>
    /// <returns>The closest player, or <see langword="null"/> when none exists.</returns>
    public static PlayerController FindClosestRegistered(Vector3 position)
    {
        GameManager manager = GameManager.Instance;
        if (manager != null && manager.PlayerCount > 0)
        {
            return FindClosest(manager.Players, position);
        }

        return UnityEngine.Object.FindFirstObjectByType<PlayerController>();
    }
}
