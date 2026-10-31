/// <summary>Optionally coordinates weapon selection and attack locking for a player presentation.</summary>
public interface IWeaponSwitchGate
{
    /// <summary>Gets whether the gate is configured and can handle selection requests.</summary>
    bool IsAvailable { get; }

    /// <summary>Gets whether attacks are blocked while a weapon transition is in progress.</summary>
    bool ActionBlocked { get; }

    /// <summary>Gets the latest selection, including one waiting to be committed.</summary>
    WeaponSystem.TipoArma RequestedWeapon { get; }

    /// <summary>Handles a requested selection without immediately committing it.</summary>
    void RequestWeapon(WeaponSystem.TipoArma weapon);

    /// <summary>Cancels pending selection and resets to the weapon currently committed by gameplay.</summary>
    void ResetTransition();
}
