/// <summary>
/// Inspector-tuned movement values of one player, captured per frame for the motor.
/// </summary>
public readonly struct PlayerMovementSettings
{
    /// <summary>Creates a movement settings snapshot.</summary>
    public PlayerMovementSettings(
        float moveSpeedMetersPerSecond,
        float gravityMetersPerSecondSquared,
        float jumpHeightMeters,
        float coyoteTimeSeconds,
        float zoneGravityMetersPerSecondSquared,
        float zoneEntryImpulseMetersPerSecond,
        float zoneMoveSpeedMetersPerSecond)
    {
        MoveSpeedMetersPerSecond = moveSpeedMetersPerSecond;
        GravityMetersPerSecondSquared = gravityMetersPerSecondSquared;
        JumpHeightMeters = jumpHeightMeters;
        CoyoteTimeSeconds = coyoteTimeSeconds;
        ZoneGravityMetersPerSecondSquared = zoneGravityMetersPerSecondSquared;
        ZoneEntryImpulseMetersPerSecond = zoneEntryImpulseMetersPerSecond;
        ZoneMoveSpeedMetersPerSecond = zoneMoveSpeedMetersPerSecond;
    }

    /// <summary>Gets the ground movement speed.</summary>
    public float MoveSpeedMetersPerSecond { get; }
    /// <summary>Gets the normal gravity (negative is down).</summary>
    public float GravityMetersPerSecondSquared { get; }
    /// <summary>Gets the jump height outside gravity zones.</summary>
    public float JumpHeightMeters { get; }
    /// <summary>Gets the grace time to jump after leaving the ground.</summary>
    public float CoyoteTimeSeconds { get; }
    /// <summary>Gets the reduced gravity inside a gravity zone.</summary>
    public float ZoneGravityMetersPerSecondSquared { get; }
    /// <summary>Gets the upward push applied when entering a gravity zone.</summary>
    public float ZoneEntryImpulseMetersPerSecond { get; }
    /// <summary>Gets the movement speed inside a gravity zone.</summary>
    public float ZoneMoveSpeedMetersPerSecond { get; }
}
