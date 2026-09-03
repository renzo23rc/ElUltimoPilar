/// <summary>
/// Immutable input snapshot for one local player.
/// </summary>
public readonly struct PlayerCommand
{
    /// <summary>
    /// Creates an immutable command snapshot.
    /// </summary>
    public PlayerCommand(
        float moveX,
        float moveY,
        float lookX,
        float lookY,
        bool jump = false,
        bool fire = false,
        bool interact = false,
        bool heal = false,
        bool ability = false,
        bool previousWeapon = false,
        bool nextWeapon = false,
        int? weaponSlot = null)
    {
        MoveX = moveX;
        MoveY = moveY;
        LookX = lookX;
        LookY = lookY;
        Jump = jump;
        Fire = fire;
        Interact = interact;
        Heal = heal;
        Ability = ability;
        PreviousWeapon = previousWeapon;
        NextWeapon = nextWeapon;
        WeaponSlot = weaponSlot;
    }

    /// <summary>Horizontal movement input.</summary>
    public float MoveX { get; }
    /// <summary>Vertical movement input.</summary>
    public float MoveY { get; }
    /// <summary>Horizontal look input.</summary>
    public float LookX { get; }
    /// <summary>Vertical look input.</summary>
    public float LookY { get; }
    /// <summary>Whether jump was pressed this frame.</summary>
    public bool Jump { get; }
    /// <summary>Whether fire was pressed this frame.</summary>
    public bool Fire { get; }
    /// <summary>Whether interaction was pressed this frame.</summary>
    public bool Interact { get; }
    /// <summary>Whether healing was pressed this frame.</summary>
    public bool Heal { get; }
    /// <summary>Whether the ability was pressed this frame.</summary>
    public bool Ability { get; }
    /// <summary>Whether previous weapon was pressed this frame.</summary>
    public bool PreviousWeapon { get; }
    /// <summary>Whether next weapon was pressed this frame.</summary>
    public bool NextWeapon { get; }
    /// <summary>The selected weapon slot, when one was pressed.</summary>
    public int? WeaponSlot { get; }
}
