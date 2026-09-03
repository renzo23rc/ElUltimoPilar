using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps registered player cameras and their audio listeners aligned with roster order.
/// </summary>
public sealed class SplitScreenCameraCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    private GameManager subscribedGameManager;
        private readonly HashSet<Camera> managedCameras = new HashSet<Camera>();

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
        camera.tag = "Untagged";
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
        camera.tag = "Untagged";

        var audioListener = camera.GetComponent<AudioListener>();
        if (audioListener != null)
            audioListener.enabled = false;
    }

    private static Rect GetViewport(int playerCount, int playerIndex)
    {
        switch (playerCount)
        {
            case 1:
                return new Rect(0f, 0f, 1f, 1f);
            case 2:
                return new Rect(playerIndex * 0.5f, 0f, 0.5f, 1f);
            case 3:
                if (playerIndex == 2)
                    return new Rect(0f, 0f, 1f, 0.5f);
                return new Rect(playerIndex * 0.5f, 0.5f, 0.5f, 0.5f);
            case 4:
                return new Rect((playerIndex % 2) * 0.5f, (playerIndex / 2) * 0.5f, 0.5f, 0.5f);
            default:
                return new Rect(0f, 0f, 1f, 1f);
        }
    }
}


