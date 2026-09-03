using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates the assigned PlayerInput action map into one player's command snapshots.
/// Device assignment and player creation remain outside this adapter.
/// </summary>
public sealed class PlayerInputAdapter : IInputAdapter
{
    private const string PlayerActionMapName = "Player";
    private const string JoinActionMapName = "Join";
    private const string JoinActionName = "Join";
    private const string MoveActionName = "Move";
    private const string LookActionName = "Look";
    private const string JumpActionName = "Jump";
    private const string FireActionName = "Attack";
    private const string InteractActionName = "Interact";
    private const string HealActionName = "Heal";
    private const string AbilityActionName = "Ability";
    private const string PreviousWeaponActionName = "Previous";
    private const string NextWeaponActionName = "Next";
    private const string WeaponSlot1ActionName = "WeaponSlot1";
    private const string WeaponSlot2ActionName = "WeaponSlot2";
    private const string WeaponSlot3ActionName = "WeaponSlot3";

    private readonly PlayerInput playerInput;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction fireAction;
    private InputAction interactAction;
    private InputAction healAction;
    private InputAction abilityAction;
    private InputAction previousWeaponAction;
    private InputAction nextWeaponAction;
    private InputAction weaponSlot1Action;
    private InputAction weaponSlot2Action;
    private InputAction weaponSlot3Action;
    private bool playerActionsConfigured;

    /// <summary>Creates an adapter for the supplied PlayerInput component.</summary>
    public PlayerInputAdapter(PlayerInput playerInput)
    {
        this.playerInput = playerInput;
        ResolveActions();
    }

    /// <summary>Gets the input component assigned to this adapter.</summary>
    public PlayerInput AssignedPlayerInput => playerInput;
    /// <summary>Gets the join action, when configured.</summary>
    public InputAction JoinAction { get; private set; }
    /// <summary>Gets whether the player action map is enabled.</summary>
    public bool IsEnabled { get; private set; }
    /// <summary>Gets the current command snapshot.</summary>
    public PlayerCommand CurrentCommand => ReadCurrentCommand();

    /// <summary>Enables the configured player action map.</summary>
    public void Enable()
    {
        ResolveActions();
        if (playerActionMap == null || !playerActionsConfigured)
        {
            IsEnabled = false;
            return;
        }

        playerActionMap.Enable();
        IsEnabled = true;
    }

    /// <summary>Disables the configured player action map.</summary>
    public void Disable()
    {
        if (playerActionMap != null)
            playerActionMap.Disable();

        IsEnabled = false;
    }

    private PlayerCommand ReadCurrentCommand()
    {
        if (!IsEnabled || !playerActionsConfigured || playerActionMap == null || !playerActionMap.enabled)
            return default(PlayerCommand);

        try
        {
            var movement = moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();
            var look = lookAction == null ? Vector2.zero : lookAction.ReadValue<Vector2>();
            return new PlayerCommand(
                movement.x,
                movement.y,
                look.x,
                look.y,
                WasPressed(jumpAction),
                WasPressed(fireAction),
                WasPressed(interactAction),
                WasPressed(healAction),
                WasPressed(abilityAction),
                WasPressed(previousWeaponAction),
                WasPressed(nextWeaponAction),
                ReadWeaponSlot());
        }
        catch (System.InvalidOperationException)
        {
            return default(PlayerCommand);
        }
    }

    private int? ReadWeaponSlot()
    {
        if (WasPressed(weaponSlot1Action)) return 1;
        if (WasPressed(weaponSlot2Action)) return 2;
        if (WasPressed(weaponSlot3Action)) return 3;
        return null;
    }

    private static bool WasPressed(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }

    private void ResolveActions()
    {
        playerActionMap = null;
        moveAction = null;
        lookAction = null;
        jumpAction = null;
        fireAction = null;
        interactAction = null;
        healAction = null;
        abilityAction = null;
        previousWeaponAction = null;
        nextWeaponAction = null;
        weaponSlot1Action = null;
        weaponSlot2Action = null;
        weaponSlot3Action = null;
        playerActionsConfigured = false;
        JoinAction = null;

        if (playerInput == null || playerInput.actions == null)
            return;

        var actions = playerInput.actions;
        playerActionMap = actions.FindActionMap(PlayerActionMapName, false);
        if (playerActionMap != null)
        {
            moveAction = playerActionMap.FindAction(MoveActionName, false);
            lookAction = playerActionMap.FindAction(LookActionName, false);
            jumpAction = playerActionMap.FindAction(JumpActionName, false);
            fireAction = playerActionMap.FindAction(FireActionName, false);
            interactAction = playerActionMap.FindAction(InteractActionName, false);
            healAction = playerActionMap.FindAction(HealActionName, false);
            abilityAction = playerActionMap.FindAction(AbilityActionName, false);
            previousWeaponAction = playerActionMap.FindAction(PreviousWeaponActionName, false);
            nextWeaponAction = playerActionMap.FindAction(NextWeaponActionName, false);
            weaponSlot1Action = playerActionMap.FindAction(WeaponSlot1ActionName, false);
            weaponSlot2Action = playerActionMap.FindAction(WeaponSlot2ActionName, false);
            weaponSlot3Action = playerActionMap.FindAction(WeaponSlot3ActionName, false);
            playerActionsConfigured = moveAction != null &&
                lookAction != null &&
                jumpAction != null &&
                fireAction != null &&
                interactAction != null &&
                healAction != null &&
                abilityAction != null &&
                previousWeaponAction != null &&
                nextWeaponAction != null &&
                weaponSlot1Action != null &&
                weaponSlot2Action != null &&
                weaponSlot3Action != null;
        }

        var joinActionMap = actions.FindActionMap(JoinActionMapName, false);
        if (joinActionMap != null)
            JoinAction = joinActionMap.FindAction(JoinActionName, false);
    }
}
