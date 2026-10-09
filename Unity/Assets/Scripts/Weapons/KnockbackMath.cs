using UnityEngine;

/// <summary>
/// Calculates melee knockback velocities; it holds no state and no balance values.
/// </summary>
public static class KnockbackMath
{
    /// <summary>
    /// Calculates the knockback velocity that pushes a target away from the attacker.
    /// </summary>
    /// <param name="attackerPosition">The attacker's world position.</param>
    /// <param name="attackerForward">The fallback direction when both positions overlap.</param>
    /// <param name="targetPosition">The target's world position.</param>
    /// <param name="speedMetersPerSecond">The horizontal knockback speed.</param>
    /// <param name="liftRatio">The vertical speed as a ratio of the horizontal speed.</param>
    /// <returns>The velocity change to apply to the target.</returns>
    public static Vector3 CalculateVelocity(
        Vector3 attackerPosition,
        Vector3 attackerForward,
        Vector3 targetPosition,
        float speedMetersPerSecond,
        float liftRatio)
    {
        Vector3 direction = targetPosition - attackerPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            direction = attackerForward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        Vector3 horizontal = direction.normalized * speedMetersPerSecond;
        return horizontal + (Vector3.up * (speedMetersPerSecond * liftRatio));
    }
}
