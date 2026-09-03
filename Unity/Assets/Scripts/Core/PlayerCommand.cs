/// <summary>
/// Immutable input snapshot for one local player.
/// </summary>
public readonly struct PlayerCommand
{
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

    public float MoveX { get; }
    public float MoveY { get; }
    public float LookX { get; }
    public float LookY { get; }
    public bool Jump { get; }
    public bool Fire { get; }
    public bool Interact { get; }
    public bool Heal { get; }
    public bool Ability { get; }
    public bool PreviousWeapon { get; }
    public bool NextWeapon { get; }
    public int? WeaponSlot { get; }
}
