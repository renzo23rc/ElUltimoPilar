using System;

/// <summary>Tracks P1's draw, held, and holster phases independently of Unity frame timing.</summary>
public sealed class PlayerWeaponSequence
{
    private const float BoundaryToleranceSeconds = 0.00001f;

    private enum Phase
    {
        Ready,
        Drawing,
        Holstering,
        Returning
    }

    private readonly float readySeconds;
    private readonly float clipSeconds;
    private readonly float rifleTransitionSeconds;
    private readonly float bazookaTransitionSeconds;
    private Phase phase;
    private WeaponSystem.TipoArma? pendingWeapon;

    /// <summary>Creates a sequence with its currently equipped weapon in the ready pose.</summary>
    public PlayerWeaponSequence(WeaponSystem.TipoArma initialWeapon, float readySeconds, float clipSeconds)
    {
        if (float.IsNaN(readySeconds) || float.IsInfinity(readySeconds)
            || float.IsNaN(clipSeconds) || float.IsInfinity(clipSeconds)
            || readySeconds <= 0f || clipSeconds <= readySeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(readySeconds), "The ready pose must be inside the clip.");
        }

        this.readySeconds = readySeconds;
        this.clipSeconds = clipSeconds;
        Reset(initialWeapon);
    }

    /// <summary>Creates a sequence with a draw and holster duration for each firearm.</summary>
    public PlayerWeaponSequence(
        WeaponSystem.TipoArma initialWeapon,
        float readySeconds,
        float clipSeconds,
        float rifleTransitionSeconds,
        float bazookaTransitionSeconds)
        : this(initialWeapon, readySeconds, clipSeconds)
    {
        if (float.IsNaN(rifleTransitionSeconds) || float.IsInfinity(rifleTransitionSeconds)
            || float.IsNaN(bazookaTransitionSeconds) || float.IsInfinity(bazookaTransitionSeconds)
            || rifleTransitionSeconds <= 0f || bazookaTransitionSeconds <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(rifleTransitionSeconds), "Transition durations must be positive.");
        }

        this.rifleTransitionSeconds = rifleTransitionSeconds;
        this.bazookaTransitionSeconds = bazookaTransitionSeconds;
    }

    /// <summary>Gets the weapon whose selection has actually been committed.</summary>
    public WeaponSystem.TipoArma CurrentWeapon { get; private set; }

    /// <summary>Gets the latest requested weapon, including a pending selection.</summary>
    public WeaponSystem.TipoArma RequestedWeapon => pendingWeapon ?? CurrentWeapon;

    /// <summary>Gets the current position in the original draw-and-holster clip.</summary>
    public float ClipTimeSeconds { get; private set; }

    /// <summary>Gets whether attacks must wait for the draw or holster to finish.</summary>
    public bool ActionBlocked => phase != Phase.Ready;

    /// <summary>Gets whether the committed weapon uses the firearm pose.</summary>
    public bool HasFirearm => IsFirearm(CurrentWeapon);

    /// <summary>Resets to a ready weapon and discards any queued selection.</summary>
    public void Reset(WeaponSystem.TipoArma weapon)
    {
        CurrentWeapon = weapon;
        pendingWeapon = null;
        phase = Phase.Ready;
        ClipTimeSeconds = IsFirearm(weapon) ? readySeconds : 0f;
    }

    /// <summary>Starts drawing the currently equipped firearm from the beginning.</summary>
    public void BeginDraw()
    {
        pendingWeapon = null;
        if (IsFirearm(CurrentWeapon))
        {
            ClipTimeSeconds = 0f;
            phase = Phase.Drawing;
        }
    }

    /// <summary>Requests a weapon and returns true only when it commits immediately.</summary>
    public bool Request(WeaponSystem.TipoArma weapon, out WeaponSystem.TipoArma committed)
    {
        committed = CurrentWeapon;
        if (phase == Phase.Drawing)
        {
            pendingWeapon = weapon == CurrentWeapon ? null : weapon;
            return false;
        }

        if (phase == Phase.Holstering || phase == Phase.Returning)
        {
            pendingWeapon = weapon == CurrentWeapon ? null : weapon;
            phase = pendingWeapon.HasValue ? Phase.Holstering : Phase.Returning;
            return false;
        }

        if (weapon == CurrentWeapon)
        {
            return false;
        }

        if (IsFirearm(CurrentWeapon))
        {
            pendingWeapon = weapon;
            phase = Phase.Holstering;
            return false;
        }

        Commit(weapon);
        committed = weapon;
        return true;
    }

    /// <summary>Advances the clip and reports a selection committed during this step.</summary>
    public bool Advance(float deltaSeconds, out WeaponSystem.TipoArma committed)
    {
        committed = CurrentWeapon;
        if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds <= 0f)
        {
            return false;
        }

        bool changed = false;
        float remaining = deltaSeconds;
        while (remaining > BoundaryToleranceSeconds && phase != Phase.Ready)
        {
            float boundary = phase == Phase.Holstering ? clipSeconds : readySeconds;
            float distance = Math.Abs(boundary - ClipTimeSeconds);
            float clipSpeed = GetClipSpeed();
            float secondsToBoundary = distance / clipSpeed;
            if (remaining + BoundaryToleranceSeconds < secondsToBoundary)
            {
                float clipAdvance = remaining * clipSpeed;
                ClipTimeSeconds += phase == Phase.Returning ? -clipAdvance : clipAdvance;
                break;
            }

            ClipTimeSeconds = boundary;
            remaining = Math.Max(0f, remaining - secondsToBoundary);
            if (phase == Phase.Holstering)
            {
                if (pendingWeapon.HasValue)
                {
                    committed = pendingWeapon.Value;
                    Commit(committed);
                    changed = true;
                }
                else
                {
                    phase = Phase.Returning;
                }
            }
            else
            {
                phase = Phase.Ready;
                if (pendingWeapon.HasValue && pendingWeapon.Value != CurrentWeapon)
                {
                    phase = Phase.Holstering;
                }
            }
        }

        return changed;
    }

    private float GetClipSpeed()
    {
        float transitionSeconds = CurrentWeapon == WeaponSystem.TipoArma.Area
            ? bazookaTransitionSeconds
            : rifleTransitionSeconds;
        if (transitionSeconds <= 0f)
        {
            return 1f;
        }

        float clipSegmentSeconds = phase == Phase.Drawing ? readySeconds : clipSeconds - readySeconds;
        return clipSegmentSeconds / transitionSeconds;
    }

    private void Commit(WeaponSystem.TipoArma weapon)
    {
        CurrentWeapon = weapon;
        pendingWeapon = null;
        if (IsFirearm(weapon))
        {
            ClipTimeSeconds = 0f;
            phase = Phase.Drawing;
        }
        else
        {
            ClipTimeSeconds = 0f;
            phase = Phase.Ready;
        }
    }

    private static bool IsFirearm(WeaponSystem.TipoArma weapon)
    {
        return weapon == WeaponSystem.TipoArma.Directa || weapon == WeaponSystem.TipoArma.Area;
    }
}
