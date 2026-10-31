using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the primary keyboard-and-mouse player of the test scene and the inactive template
/// that <see cref="PlayerJoinCoordinator"/> clones for gamepad players.
/// </summary>
public static class TestScenePlayerBuilder
{
    private const string PrimaryPrefabResource = "Prefabs/JugadorP1";
    private const string SecondaryPrefabResource = "Prefabs/JugadorP2";
    private const string PrimaryModelResource = "Models/Personajes/p1";
    private const string SecondaryModelResource = "Models/Personajes/p2";
    private const string VisualObjectName = "JugadorVisual";
    private const string BazookaObjectName = "bazooka";
    private const string BodyMaterialNameFragment = "body";
    private const string PlayerActionMapName = "Player";
    private const string KeyboardMouseControlSchemeName = "Keyboard&Mouse";
    private const string UntaggedCameraTag = "Untagged";
    private const string MainCameraTag = "MainCamera";
    private const float HalfFactor = 0.5f;

    /// <summary>Height of the player CharacterController, in meters.</summary>
    public const float ControllerHeightMeters = 2f;
    /// <summary>Half the controller height, used to rest feet on a walk surface.</summary>
    public const float ControllerHalfHeightMeters = ControllerHeightMeters * HalfFactor;

    /// <summary>Creates an inactive primary player from its complete prefab.</summary>
    public static PlayerController CreatePrimary(
        InputActionAsset actions,
        Material material,
        GameManager gameManager,
        Vector3 spawnPosition)
    {
        PlayerController player = InstantiatePlayer(PrimaryPrefabResource);
        if (player == null)
        {
            return null;
        }

        player.gameObject.name = "Jugador";
        player.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        PlayerInput input = player.GetComponent<PlayerInput>();
        if (input != null)
        {
            input.actions = actions;
            input.defaultActionMap = PlayerActionMapName;
            input.defaultControlScheme = KeyboardMouseControlSchemeName;
            input.neverAutoSwitchControlSchemes = true;
        }

        ApplyMapSkin(player.transform, PrimaryModelResource);
        DisableOtherCameras(player.camaraJugador, gameManager);
        return player;
    }

    /// <summary>Creates an independent inactive P2 prefab template for gamepad joins.</summary>
    public static PlayerController CreateJoinTemplate(PlayerController primary)
    {
        PlayerController template = InstantiatePlayer(SecondaryPrefabResource);
        if (template == null)
        {
            return null;
        }

        template.gameObject.name = "PlayerTemplate";
        if (primary != null)
        {
            // The join coordinator uses this inactive template's height for every spawn.
            template.transform.position = primary.transform.position;
        }

        PlayerInput primaryInput = primary != null ? primary.GetComponent<PlayerInput>() : null;
        PlayerInput templateInput = template.GetComponent<PlayerInput>();
        if (primaryInput != null && templateInput != null)
        {
            templateInput.actions = primaryInput.actions;
        }

        ApplyMapSkin(template.transform, SecondaryModelResource);
        return template;
    }

    private static PlayerController InstantiatePlayer(string resourcePath)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null || prefab.GetComponent<PlayerController>() == null)
        {
            Debug.LogError($"[TestScenePlayerBuilder] Missing or invalid player prefab: {resourcePath}.");
            return null;
        }

        // Keep Awake and OnEnable from running until the spawn and input are configured.
        var inactiveParent = new GameObject("PlayerInstantiation");
        inactiveParent.SetActive(false);
        GameObject instance = Object.Instantiate(prefab, inactiveParent.transform);
        instance.SetActive(false);
        instance.transform.SetParent(null, false);
        if (Application.isPlaying)
        {
            Object.Destroy(inactiveParent);
        }
        else
        {
            Object.DestroyImmediate(inactiveParent);
        }

        return instance.GetComponent<PlayerController>();
    }

    private static void ApplyMapSkin(Transform player, string modelResource)
    {
        Transform visual = player.Find(VisualObjectName);
        if (visual == null)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        Material bodyMaterial = Resources.Load<Material>(
            PlayerMapSkinCatalog.BodyMaterialResource(modelResource, sceneName));
        Material equipmentMaterial = Resources.Load<Material>(
            PlayerMapSkinCatalog.EquipmentMaterialResource(modelResource, sceneName));
        if (bodyMaterial == null || equipmentMaterial == null)
        {
            return;
        }

        Transform areaWeapon = null;
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == BazookaObjectName)
            {
                areaWeapon = child;
                break;
            }
        }

        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (areaWeapon != null && renderer.transform.IsChildOf(areaWeapon))
            {
                continue;
            }

            Material[] slots = renderer.sharedMaterials;
            for (int index = 0; index < slots.Length; index++)
            {
                string slotName = slots[index] != null ? slots[index].name : string.Empty;
                slots[index] = slotName.ToLowerInvariant().Contains(BodyMaterialNameFragment)
                    ? bodyMaterial
                    : equipmentMaterial;
            }

            renderer.sharedMaterials = slots;
        }
    }

    private static void DisableOtherCameras(Camera keep, GameManager gameManager)
    {
        foreach (Camera other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (other == null || other == keep)
            {
                continue;
            }

            other.enabled = false;
            if (other.TryGetComponent(out AudioListener listener))
            {
                listener.enabled = false;
            }

            if (other.CompareTag(MainCameraTag) && !IsRegisteredPlayerCamera(gameManager, other))
            {
                other.tag = UntaggedCameraTag;
            }
        }
    }

    private static bool IsRegisteredPlayerCamera(GameManager manager, Camera camera)
    {
        if (manager == null || camera == null)
        {
            return false;
        }

        foreach (PlayerController player in manager.Players)
        {
            if (player != null && player.camaraJugador == camera)
            {
                return true;
            }
        }

        return false;
    }
}
