using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Creates and exclusively pairs a player instance when a gamepad requests to join.
/// </summary>
public sealed class PlayerJoinCoordinator : MonoBehaviour
{
    private const string JoinActionMapName = "Join";
    private const string JoinActionName = "Join";
    private const string PlayerActionMapName = "Player";
    private const string GamepadControlSchemeName = "Gamepad";
    private const int MinimumPlayerCapacity = 1;
    private const int MaximumPlayerCapacity = 4;
    private const float SpawnHeightMeters = 1f;
    private const float SpawnRadiusMeters = 8f;

    [Header("References")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController playerTemplate;

    [Header("Configuration")]
    [SerializeField] private int maxPlayers = MaximumPlayerCapacity;

    private InputAction joinAction;
    private GameManager subscribedGameManager;
    private bool joinActionSubscribed;
    private readonly Dictionary<Gamepad, PlayerController> trackedAssignments =
        new Dictionary<Gamepad, PlayerController>();

    /// <summary>Configures the input asset, player template, and match manager.</summary>
    public void Configure(InputActionAsset actions, PlayerController template, GameManager manager)
    {
        inputActionAsset = actions;
        playerTemplate = template;
        gameManager = manager;
        ResolveJoinAction();
        SubscribeJoinAction();
        SubscribeGameManager();
    }

    private void OnEnable()
    {
        ResolveGameManager();
        SubscribeJoinAction();
        SubscribeGameManager();
    }

    private void Start()
    {
        ResolveGameManager();
        ResolveJoinAction();
        SubscribeJoinAction();
        SubscribeGameManager();
    }

    private void OnDisable()
    {
        if (joinActionSubscribed && joinAction != null)
            joinAction.performed -= HandleJoinPerformed;

        joinActionSubscribed = false;
        if (joinAction != null)
            joinAction.Disable();

        if (subscribedGameManager != null)
            subscribedGameManager.OnPlayerUnregistered -= HandlePlayerUnregistered;

        subscribedGameManager = null;
        trackedAssignments.Clear();
    }

    private void ResolveGameManager()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    private void ResolveJoinAction()
    {
        joinAction = null;
        if (inputActionAsset == null)
            return;

        var joinActionMap = inputActionAsset.FindActionMap(JoinActionMapName, false);
        joinAction = joinActionMap?.FindAction(JoinActionName, false);
    }

    private void SubscribeJoinAction()
    {
        if (joinAction == null)
            ResolveJoinAction();
        if (joinAction == null || joinActionSubscribed)
            return;

        joinAction.performed += HandleJoinPerformed;
        joinAction.Enable();
        joinActionSubscribed = true;
    }

    private void SubscribeGameManager()
    {
        if (gameManager == subscribedGameManager)
            return;

        if (subscribedGameManager != null)
            subscribedGameManager.OnPlayerUnregistered -= HandlePlayerUnregistered;

        subscribedGameManager = gameManager;
        trackedAssignments.Clear();
        if (subscribedGameManager != null)
            subscribedGameManager.OnPlayerUnregistered += HandlePlayerUnregistered;
    }

    private void HandleJoinPerformed(InputAction.CallbackContext context)
    {
        var gamepad = context.control?.device as Gamepad;
        if (gamepad == null)
            return;

        TryJoin(gamepad);
    }

    /// <summary>
    /// Attempts to add one gamepad-controlled player to the current roster.
    /// </summary>
    public bool TryJoin(Gamepad gamepad)
    {
        ResolveGameManager();
        if (gamepad == null || gameManager == null || playerTemplate == null || inputActionAsset == null)
            return false;
        if (playerTemplate.gameObject == null || playerTemplate.gameObject.activeSelf)
            return false;

        var configuredCapacity = Mathf.Min(maxPlayers, gameManager.maxPlayers);
        if (configuredCapacity < MinimumPlayerCapacity)
            return false;

        var capacity = Mathf.Min(configuredCapacity, MaximumPlayerCapacity);
        if (gameManager.PlayerCount >= capacity || IsGamepadAssigned(gamepad))
            return false;

        PlayerController joinedPlayer = null;
        bool joinCompleted = false;
        try
        {
            joinedPlayer = Instantiate(
                playerTemplate,
                GetSpawnPosition(gameManager.PlayerCount),
                Quaternion.identity);
            if (joinedPlayer == null)
                return false;

            joinedPlayer.gameObject.SetActive(false);
            var playerInput = joinedPlayer.GetComponent<PlayerInput>();
            if (playerInput == null)
                return false;

            playerInput.actions = inputActionAsset;
            playerInput.defaultActionMap = PlayerActionMapName;
            playerInput.defaultControlScheme = GamepadControlSchemeName;
            playerInput.neverAutoSwitchControlSchemes = true;
            playerInput.enabled = true;

            joinedPlayer.gameObject.SetActive(true);
            playerInput.SwitchCurrentControlScheme(GamepadControlSchemeName, gamepad);
            if (!HasExclusivePairing(playerInput, gamepad))
                return false;

            bool registered = gameManager.RegisterPlayer(joinedPlayer);
            if (!registered && !IsRegistered(joinedPlayer))
                return false;

            trackedAssignments[gamepad] = joinedPlayer;
            joinCompleted = true;
            return true;
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
        catch (System.ArgumentException)
        {
            return false;
        }
        finally
        {
            if (!joinCompleted && joinedPlayer != null)
                DestroyIncompletePlayer(joinedPlayer);
        }
    }

    /// <summary>
    /// Returns whether a gamepad is already tracked or paired to a registered player.
    /// </summary>
    public bool IsGamepadAssigned(Gamepad gamepad)
    {
        ResolveGameManager();
        if (gamepad == null)
            return false;

        if (trackedAssignments.TryGetValue(gamepad, out var trackedPlayer))
        {
            if (IsTrackedAssignmentValid(trackedPlayer, gamepad))
                return true;

            trackedAssignments.Remove(gamepad);
        }

        if (gameManager == null)
            return false;

        foreach (var player in gameManager.Players)
        {
            if (player == null)
                continue;

            var playerInput = player.GetComponent<PlayerInput>();
            if (playerInput == null)
                continue;

            for (var deviceIndex = 0; deviceIndex < playerInput.devices.Count; deviceIndex++)
            {
                if (playerInput.devices[deviceIndex] == gamepad)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the deterministic spawn position for a zero-based roster slot.
    /// </summary>
    public Vector3 GetSpawnPosition(int slot)
    {
        switch (slot)
        {
            case 0: return new Vector3(0f, SpawnHeightMeters, -SpawnRadiusMeters);
            case 1: return new Vector3(SpawnRadiusMeters, SpawnHeightMeters, 0f);
            case 2: return new Vector3(0f, SpawnHeightMeters, SpawnRadiusMeters);
            case 3: return new Vector3(-SpawnRadiusMeters, SpawnHeightMeters, 0f);
            default: return Vector3.zero;
        }
    }

    private bool IsTrackedAssignmentValid(PlayerController player, Gamepad gamepad)
    {
        if (!IsRegistered(player))
            return false;

        var playerInput = player.GetComponent<PlayerInput>();
        return HasDevice(playerInput, gamepad);
    }

    private bool HasDevice(PlayerInput playerInput, Gamepad gamepad)
    {
        if (playerInput == null || gamepad == null)
            return false;

        for (var deviceIndex = 0; deviceIndex < playerInput.devices.Count; deviceIndex++)
        {
            if (playerInput.devices[deviceIndex] == gamepad)
                return true;
        }

        return false;
    }

    private bool HasExclusivePairing(PlayerInput playerInput, Gamepad gamepad)
    {
        if (playerInput == null || playerInput.devices.Count != 1 || playerInput.devices[0] != gamepad)
            return false;

        if (gameManager == null)
            return false;

        foreach (var player in gameManager.Players)
        {
            if (player == null || player.GetComponent<PlayerInput>() == playerInput)
                continue;

            var otherInput = player.GetComponent<PlayerInput>();
            if (otherInput == null)
                continue;

            if (HasDevice(otherInput, gamepad))
                return false;
        }

        return true;
    }

    private bool IsRegistered(PlayerController player)
    {
        if (player == null || gameManager == null)
            return false;

        foreach (var registeredPlayer in gameManager.Players)
        {
            if (registeredPlayer == player)
                return true;
        }

        return false;
    }

    private void DestroyIncompletePlayer(PlayerController player)
    {
        if (player == null)
            return;

        if (IsRegistered(player))
            gameManager.UnregisterPlayer(player);

        RemoveTrackedAssignmentsForPlayer(player);
        Destroy(player.gameObject);
    }

    private void HandlePlayerUnregistered(PlayerController player)
    {
        RemoveTrackedAssignmentsForPlayer(player);
    }

    private void RemoveTrackedAssignmentsForPlayer(PlayerController player)
    {
        var assignmentsToRemove = new List<Gamepad>();
        foreach (var assignment in trackedAssignments)
        {
            if (assignment.Value == null || assignment.Value == player)
                assignmentsToRemove.Add(assignment.Key);
        }

        foreach (var gamepad in assignmentsToRemove)
            trackedAssignments.Remove(gamepad);
    }
}
