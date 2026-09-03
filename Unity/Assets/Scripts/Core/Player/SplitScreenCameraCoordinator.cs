using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps registered player cameras and their audio listeners aligned with roster order.
/// </summary>
public sealed class SplitScreenCameraCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    private const int SinglePlayerCount = 1;
    private const int TwoPlayerCount = 2;
    private const int ThreePlayerCount = 3;
    private const int FourPlayerCount = 4;
    private const float FullViewport = 1f;
    private const float HalfViewport = 0.5f;
    private const string UntaggedCameraTag = "Untagged";

    private GameManager subscribedGameManager;
    private readonly HashSet<Camera> managedCameras = new HashSet<Camera>();

    /// <summary>Configures the manager used to obtain registered players.</summary>
    public void Configure(GameManager manager)
    {
        gameManager = manager;
        SubscribeToGameManager();
    }

        private void OnEnable()
    {
        ResolveGameManager();
        SubscribeToGameManager();
    }

    private void Start()
    {
        ResolveGameManager();
        SubscribeToGameManager();
        ApplyViewports();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    private void ResolveGameManager()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    private void SubscribeToGameManager()
    {
        if (gameManager == subscribedGameManager)
            return;

        UnsubscribeFromGameManager();
        subscribedGameManager = gameManager;
        if (subscribedGameManager == null)
            return;

        subscribedGameManager.OnPlayerRegistered += HandlePlayerRegistered;
        subscribedGameManager.OnPlayerUnregistered += HandlePlayerUnregistered;
    }

    private void UnsubscribeFromGameManager()
    {
        if (subscribedGameManager == null)
            return;

        subscribedGameManager.OnPlayerRegistered -= HandlePlayerRegistered;
        subscribedGameManager.OnPlayerUnregistered -= HandlePlayerUnregistered;
        subscribedGameManager = null;
    }

    private void HandlePlayerRegistered(PlayerController player)
    {
        ApplyViewports();
    }

    private void HandlePlayerUnregistered(PlayerController player)
    {
        DisableCamera(player == null ? null : player.camaraJugador);
        ApplyViewports();
    }

    /// <summary>
    /// Applies the layout for the current roster and disables managed cameras
    /// that do not belong to a currently registered player.
    /// </summary>
    public void ApplyViewports()
    {
        ResolveGameManager();

        var registeredPlayers = new List<PlayerController>();
        var registeredPlayerCameras = new HashSet<Camera>();
        if (gameManager != null)
        {
            foreach (var player in gameManager.Players)
            {
                if (player == null)
                    continue;

                registeredPlayers.Add(player);
                if (player.camaraJugador != null)
                    registeredPlayerCameras.Add(player.camaraJugador);
            }
        }

        foreach (var camera in managedCameras)
        {
            if (!registeredPlayerCameras.Contains(camera))
                DisableCamera(camera);
        }

        int playerCount = registeredPlayers.Count;
        for (int playerIndex = 0; playerIndex < playerCount; playerIndex++)
        {
            var player = registeredPlayers[playerIndex];
            var camera = player.camaraJugador;
            if (camera == null)
                continue;

            ConfigureCamera(
                camera,
                GetViewport(playerCount, playerIndex),
                playerIndex == 0);
        }
    }

    /// <summary>
    /// Configures one explicitly supplied player camera and its listener.
    /// </summary>
    public void ConfigureCamera(Camera camera, Rect viewport, bool enableAudio)
    {
        if (camera == null)
            return;

        managedCameras.Add(camera);
        camera.rect = viewport;
        camera.tag = UntaggedCameraTag;
        camera.enabled = true;

        var audioListener = camera.GetComponent<AudioListener>();
        if (audioListener != null)
            audioListener.enabled = enableAudio;
    }

    private void DisableCamera(Camera camera)
    {
        if (camera == null)
            return;

        camera.enabled = false;
        camera.tag = UntaggedCameraTag;

        var audioListener = camera.GetComponent<AudioListener>();
        if (audioListener != null)
            audioListener.enabled = false;
    }

    private static Rect GetViewport(int playerCount, int playerIndex)
    {
        switch (playerCount)
        {
            case SinglePlayerCount:
                return new Rect(0f, 0f, FullViewport, FullViewport);
            case TwoPlayerCount:
                return new Rect(playerIndex * HalfViewport, 0f, HalfViewport, FullViewport);
            case ThreePlayerCount:
                if (playerIndex == TwoPlayerCount)
                    return new Rect(0f, 0f, FullViewport, HalfViewport);
                return new Rect(playerIndex * HalfViewport, HalfViewport, HalfViewport, HalfViewport);
            case FourPlayerCount:
                return new Rect(
                    (playerIndex % TwoPlayerCount) * HalfViewport,
                    (playerIndex / TwoPlayerCount) * HalfViewport,
                    HalfViewport,
                    HalfViewport);
            default:
                return new Rect(0f, 0f, FullViewport, FullViewport);
        }
    }
}


