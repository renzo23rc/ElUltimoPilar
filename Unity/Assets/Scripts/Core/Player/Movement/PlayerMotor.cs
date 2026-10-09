using UnityEngine;

/// <summary>
/// Moves one player's CharacterController: walking, gravity, jumping, and the floaty gravity-zone state.
/// Tunable values arrive through <see cref="PlayerMovementSettings"/>; the motor owns only motion state.
/// </summary>
public sealed class PlayerMotor
{
    private const float GroundedVerticalSpeed = -2f;
    private const float GravityZoneGroundedSpeed = -0.5f;
    private const float GravityZoneMaximumFallSpeed = -3f;
    private const float GroundHeightMeters = 1.2f;
    private const float ZoneJumpMultiplier = 2.2f;
    private const float JumpVelocityFactor = -2f;
    private const float DoubleJumpGuardSeconds = 999f;
    private const float ZoneBobFrequencyHertz = 2.5f;
    private const float ZoneBobSpeedMetersPerSecond = 0.8f;
    private const float ZoneFloatFrequencyHertz = 3f;
    private const float ZoneFloatAccelerationMetersPerSecondSquared = 9f;

    private readonly CharacterController controller;
    private readonly Transform body;
    private Vector3 verticalVelocity;
    private float airborneSeconds;

    /// <summary>Creates a motor for the supplied controller and body.</summary>
    public PlayerMotor(CharacterController controller, Transform body)
    {
        this.controller = controller;
        this.body = body;
    }

    /// <summary>Gets whether the player is floating inside a gravity zone.</summary>
    public bool InGravityZone { get; private set; }

    /// <summary>Gets whether the controller exists and is enabled.</summary>
    public bool CanMove => controller != null && controller.enabled;

    /// <summary>Moves on the horizontal plane relative to the reference orientation.</summary>
    /// <param name="reference">The transform whose forward and right axes drive movement.</param>
    /// <param name="moveX">The lateral input.</param>
    /// <param name="moveY">The forward input.</param>
    /// <param name="settings">The movement settings.</param>
    /// <param name="speedFactor">The slowdown multiplier.</param>
    public void Move(Transform reference, float moveX, float moveY, PlayerMovementSettings settings, float speedFactor)
    {
        if (controller == null)
        {
            return;
        }

        Vector3 forward = reference.forward;
        Vector3 right = reference.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        float baseSpeed = InGravityZone ? settings.ZoneMoveSpeedMetersPerSecond : settings.MoveSpeedMetersPerSecond;
        Vector3 movement = (forward * moveY + right * moveX) * (baseSpeed * speedFactor);
        // Flotación en zona: vaivén vertical expresado como velocidad (independiente de los FPS).
        if (InGravityZone)
        {
            movement.y += Mathf.Sin(Time.time * ZoneBobFrequencyHertz) * ZoneBobSpeedMetersPerSecond;
        }

        controller.Move(movement * Time.deltaTime);
    }

    /// <summary>Applies gravity for one frame and moves vertically.</summary>
    public void ApplyGravity(PlayerMovementSettings settings)
    {
        float gravity = CurrentGravity(settings);
        if (controller.isGrounded)
        {
            airborneSeconds = 0f;
            if (verticalVelocity.y < 0)
            {
                verticalVelocity.y = InGravityZone ? GravityZoneGroundedSpeed : GroundedVerticalSpeed;
            }
        }
        else
        {
            airborneSeconds += Time.deltaTime;
            verticalVelocity.y += gravity * Time.deltaTime;
            // En zona, limitar la caída y sumar una flotación oscilante.
            if (InGravityZone)
            {
                verticalVelocity.y = Mathf.Max(verticalVelocity.y, GravityZoneMaximumFallSpeed);
                verticalVelocity.y += Mathf.Sin(Time.time * ZoneFloatFrequencyHertz)
                    * ZoneFloatAccelerationMetersPerSecondSquared * Time.deltaTime;
            }
        }

        controller.Move(verticalVelocity * Time.deltaTime);
    }

    /// <summary>Jumps when grounded, within coyote time, or falling close to the floor.</summary>
    public void TryJump(PlayerMovementSettings settings)
    {
        // El respaldo por altura cubre isGrounded inestable, pero solo cayendo: evita saltos en el aire al subir.
        bool nearGround = body.position.y <= GroundHeightMeters && verticalVelocity.y <= 0f;
        bool canJump = controller.isGrounded || airborneSeconds < settings.CoyoteTimeSeconds || nearGround;
        if (!canJump)
        {
            return;
        }

        float height = InGravityZone ? settings.JumpHeightMeters * ZoneJumpMultiplier : settings.JumpHeightMeters;
        verticalVelocity.y = Mathf.Sqrt(height * JumpVelocityFactor * CurrentGravity(settings));
        airborneSeconds = DoubleJumpGuardSeconds;
    }

    /// <summary>Enters the gravity zone and pushes the player upward.</summary>
    /// <returns><see langword="true"/> when the player was outside the zone.</returns>
    public bool EnterGravityZone(PlayerMovementSettings settings)
    {
        if (InGravityZone)
        {
            return false;
        }

        InGravityZone = true;
        verticalVelocity.y = settings.ZoneEntryImpulseMetersPerSecond;
        return true;
    }

    /// <summary>Leaves the gravity zone.</summary>
    /// <returns><see langword="true"/> when the player was inside the zone.</returns>
    public bool ExitGravityZone()
    {
        if (!InGravityZone)
        {
            return false;
        }

        InGravityZone = false;
        return true;
    }

    /// <summary>Cancels any vertical speed, as after a revive.</summary>
    public void StopVerticalMotion()
    {
        verticalVelocity.y = 0;
    }

    /// <summary>Clears every motion state, including the gravity zone.</summary>
    public void Reset()
    {
        InGravityZone = false;
        airborneSeconds = 0f;
        verticalVelocity = Vector3.zero;
    }

    /// <summary>Moves the body to a world position, through the controller when it is enabled.</summary>
    public void MoveTo(Vector3 position)
    {
        if (CanMove)
        {
            controller.Move(position - body.position);
        }
        else
        {
            body.position = position;
        }
    }

    /// <summary>Places the body at a pose, bypassing the controller.</summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        // El CharacterController pisa las asignaciones directas de transform si queda habilitado.
        bool controllerEnabled = CanMove;
        if (controllerEnabled)
        {
            controller.enabled = false;
        }

        body.SetPositionAndRotation(position, rotation);
        if (controllerEnabled)
        {
            controller.enabled = true;
        }
    }

    private float CurrentGravity(PlayerMovementSettings settings)
    {
        return InGravityZone ? settings.ZoneGravityMetersPerSecondSquared : settings.GravityMetersPerSecondSquared;
    }
}
