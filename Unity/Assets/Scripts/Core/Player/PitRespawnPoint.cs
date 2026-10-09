using UnityEngine;

/// <summary>
/// Finds where a player who fell into a circular pit should reappear: the closest point
/// just outside the pit, on the side where they fell in.
/// </summary>
public static class PitRespawnPoint
{
    private const float MinimumDirectionMagnitude = 0.0001f;

    /// <summary>Returns the horizontal position on the pit rim, pushed outward by a margin.</summary>
    /// <param name="pitCenter">The center of the pit.</param>
    /// <param name="lethalRadiusMeters">The horizontal radius at which the pit kills.</param>
    /// <param name="fallPosition">Where the player was when the fall started.</param>
    /// <param name="fallbackDirection">The direction used when the player fell exactly at the center.</param>
    /// <param name="marginMeters">The distance outside the lethal radius.</param>
    /// <returns>The rim position, with the height of <paramref name="fallPosition"/>.</returns>
    public static Vector3 FindRimPosition(
        Vector3 pitCenter,
        float lethalRadiusMeters,
        Vector3 fallPosition,
        Vector3 fallbackDirection,
        float marginMeters)
    {
        Vector3 direction = Flatten(fallPosition - pitCenter);
        if (direction.magnitude < MinimumDirectionMagnitude)
        {
            direction = Flatten(fallbackDirection);
        }

        direction = direction.magnitude < MinimumDirectionMagnitude ? Vector3.forward : direction.normalized;
        Vector3 rim = pitCenter + (direction * (lethalRadiusMeters + marginMeters));
        rim.y = fallPosition.y;
        return rim;
    }

    private static Vector3 Flatten(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }
}
