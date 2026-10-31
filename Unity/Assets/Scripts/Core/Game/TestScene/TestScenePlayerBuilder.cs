using UltimoPilar.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the primary keyboard-and-mouse player of the test scene and the inactive template
/// that <see cref="PlayerJoinCoordinator"/> clones for gamepad players.
/// </summary>
public static class TestScenePlayerBuilder
{
    private const string PlayerActionMapName = "Player";
    private const string KeyboardMouseControlSchemeName = "Keyboard&Mouse";
    private const string UntaggedCameraTag = "Untagged";
    private const string MainCameraTag = "MainCamera";
    private const float CameraHeightMeters = 0.8f;
    // Adelantada para que la cámara no quede dentro de los lentes del modelo.
    private const float CameraForwardMeters = 0.3f;
    private const float CameraNearClipMeters = 0.1f;
    private const float ControllerRadiusMeters = 0.5f;
    /// <summary>Height of the generated CharacterController, in meters.</summary>
    public const float ControllerHeightMeters = 2f;
    private const float ControllerSkinWidthMeters = 0.08f;
    private const float HalfFactor = 0.5f;
    /// <summary>Half the generated CharacterController height, used to rest feet on a walk surface.</summary>
    public const float ControllerHalfHeightMeters = ControllerHeightMeters * HalfFactor;
    private const float AnimationReferenceSpeedMetersPerSecond = 8f;
    private const string VisualObjectName = "JugadorVisual";
    private const string BodyMaterialNameFragment = "body";
    private const string PrimaryModelResource = "Models/Personajes/p1";
    private const string LocomotionControllerResource = "Animation/PlayerLocomotion";
    private const string SecondaryModelResource = "Models/Personajes/p2";
    private const string BazookaResource = "Models/Armas/bazooka/bazooka";
    private const string BazookaObjectName = "bazooka";
    // Offset de la bazooka respecto a la mano: ajustar en Unity tras verla en Play.
    private static readonly Vector3 BazookaHandOffsetMeters = Vector3.zero;
    private static readonly Vector3 BazookaHandEulerAngles = Vector3.zero;
    private static readonly Vector3 VisualScale = new Vector3(1f, 2f, 1f);

    /// <summary>
    /// Creates the inactive primary player; activate it only after the scene composition is complete.
    /// </summary>
    /// <param name="actions">The input actions asset assigned to the player.</param>
    /// <param name="material">The optional body material.</param>
    /// <param name="gameManager">The manager used to keep registered cameras tagged.</param>
    /// <param name="spawnPosition">The world spawn position of the primary player.</param>
    /// <returns>The primary player.</returns>
    public static PlayerController CreatePrimary(
        InputActionAsset actions,
        Material material,
        GameManager gameManager,
        Vector3 spawnPosition)
    {
        // GameObject vacío + hijo visual para evitar conflictos de colliders.
        var player = new GameObject("Jugador");
        player.SetActive(false);
        player.transform.position = spawnPosition;

        CreateVisual(player.transform, PrimaryModelResource, material);
        AnimateVisual(player.transform);

        Camera camera = CreateCamera(player.transform);
        DisableOtherCameras(camera, gameManager);

        var playerInput = player.AddComponent<PlayerInput>();
        playerInput.actions = actions;
        playerInput.defaultActionMap = PlayerActionMapName;
        playerInput.defaultControlScheme = KeyboardMouseControlSchemeName;
        playerInput.neverAutoSwitchControlSchemes = true;

        var muzzle = new GameObject(MuzzleTransformResolver.MuzzleObjectName);
        muzzle.transform.SetParent(player.transform);
        muzzle.transform.localPosition = new Vector3(0f, MuzzleTransformResolver.HandHeightMeters, MuzzleTransformResolver.HandForwardMeters);
        muzzle.transform.localRotation = Quaternion.identity;
        muzzle.transform.localScale = Vector3.one;

        var controller = player.AddComponent<PlayerController>();
        controller.camaraJugador = camera;
        controller.puntoDisparo = muzzle.transform;

        player.AddComponent<EnergySystem>();
        var weapons = player.AddComponent<WeaponSystem>();
        weapons.camara = camera;
        weapons.puntoDisparo = muzzle.transform;

        // PlayerController agrega el CharacterController por [RequireComponent].
        var characterController = player.GetComponent<CharacterController>();
        characterController.radius = ControllerRadiusMeters;
        characterController.height = ControllerHeightMeters;
        characterController.center = Vector3.zero;
        characterController.skinWidth = ControllerSkinWidthMeters;
        return controller;
    }

    /// <summary>Clones the still-inactive primary player into the template used for gamepad joins.</summary>
    public static PlayerController CreateJoinTemplate(PlayerController primary)
    {
        GameObject templateObject = Object.Instantiate(primary.gameObject);
        templateObject.name = "PlayerTemplate";
        templateObject.SetActive(false);
        ReplaceVisual(templateObject.transform, SecondaryModelResource);
        AnimateVisual(templateObject.transform);
        return templateObject.GetComponent<PlayerController>();
    }

    // Con esqueleto y clips usa el Animator; sin ellos, el cuerpo respira y rebota por código.
    private static void AnimateVisual(Transform player)
    {
        Transform visual = player.Find(VisualObjectName);
        if (visual == null)
            return;

        if (TryUseSkeletalAnimation(player, visual))
            return;

        RemoveComponent<PlayerAnimatorDriver>(player);
        if (!player.TryGetComponent(out ProceduralLocomotionAnimator animator))
            animator = player.gameObject.AddComponent<ProceduralLocomotionAnimator>();
        animator.Configure(visual, AnimationReferenceSpeedMetersPerSecond);
    }

    private static bool TryUseSkeletalAnimation(Transform player, Transform visual)
    {
        Animator animator = visual.GetComponentInChildren<Animator>();
        var controller = Resources.Load<RuntimeAnimatorController>(LocomotionControllerResource);
        if (animator == null || controller == null || visual.GetComponentInChildren<SkinnedMeshRenderer>() == null)
            return false;

        animator.runtimeAnimatorController = controller;
        RemoveComponent<ProceduralLocomotionAnimator>(player);
        if (!player.TryGetComponent(out PlayerAnimatorDriver driver))
            driver = player.gameObject.AddComponent<PlayerAnimatorDriver>();
        driver.Configure(animator);
        return true;
    }

    private static void RemoveComponent<T>(Transform owner) where T : Component
    {
        if (owner.TryGetComponent(out T component))
            Object.Destroy(component);
    }

    // Modelo del personaje si existe; si no, el cubo de respaldo.
    private static void CreateVisual(Transform player, string modelResource, Material material)
    {
        if (TryCreateModelVisual(player, modelResource))
            return;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = VisualObjectName;
        visual.transform.SetParent(player);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = VisualScale;
        if (material != null) visual.GetComponent<Renderer>().material = material;
        // El CharacterController es el único collider del jugador.
        Object.Destroy(visual.GetComponent<BoxCollider>());
    }

    private static void ReplaceVisual(Transform player, string modelResource)
    {
        Transform current = player.Find(VisualObjectName);
        if (current == null || Resources.Load<GameObject>(modelResource) == null)
            return;

        // Destroy difiere al fin del frame: se renombra para que el reemplazo no choque de nombre.
        current.name = $"{VisualObjectName}_Old";
        current.gameObject.SetActive(false);
        Object.Destroy(current.gameObject);
        TryCreateModelVisual(player, modelResource);
    }

    private static bool TryCreateModelVisual(Transform player, string modelResource)
    {
        GameObject prefab = Resources.Load<GameObject>(modelResource);
        if (prefab == null)
            return false;

        GameObject model = Object.Instantiate(prefab, player);
        model.name = VisualObjectName;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        RemoveExportedExtras(model);
        FitToController(model.transform, player);
        ApplyMapSkin(model.transform, modelResource);
        EnsureBazooka(model.transform);
        return true;
    }

    // Aplica la textura del mapa activo: Obelisco usa Argentina, Cristo Redentor usa Brasil,
    // cualquier otra escena usa Estados Unidos. No toca la bazooka ni rompe si falta un material.
    private static void ApplyMapSkin(Transform visual, string modelResource)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string bodyResource = PlayerMapSkinCatalog.BodyMaterialResource(modelResource, sceneName);
        string equipmentResource = PlayerMapSkinCatalog.EquipmentMaterialResource(modelResource, sceneName);
        if (string.IsNullOrEmpty(bodyResource) || string.IsNullOrEmpty(equipmentResource))
        {
            return;
        }

        Material bodyMaterial = Resources.Load<Material>(bodyResource);
        Material equipmentMaterial = Resources.Load<Material>(equipmentResource);
        if (bodyMaterial == null || equipmentMaterial == null)
        {
            return;
        }

        ApplyMapSkinToRenderers(visual, bodyMaterial, equipmentMaterial);
    }

    // El slot "body" del FBX lleva el material de cuerpo; el resto (chaleco, cinturon,
    // pantalon, hombreras, etc.) lleva el de equipamiento. Usa sharedMaterials para no clonar.
    private static void ApplyMapSkinToRenderers(Transform visual, Material bodyMaterial, Material equipmentMaterial)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] slots = renderer.sharedMaterials;
            if (slots == null || slots.Length == 0)
            {
                continue;
            }

            Material[] resolved = new Material[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                string slotName = slots[i] != null ? slots[i].name : string.Empty;
                if (slotName.ToLowerInvariant().Contains(BodyMaterialNameFragment))
                {
                    resolved[i] = bodyMaterial;
                }
                else
                {
                    resolved[i] = equipmentMaterial;
                }
            }

            renderer.sharedMaterials = resolved;
        }
    }

    // La bazooka vive en Resources, no en el FBX del personaje: se cuelga del hueso
    // de la mano para que acompañe la animación, con nombre estable e idempotencia.
    private static void EnsureBazooka(Transform visual)
    {
        if (visual == null || FindChildRecursive(visual, BazookaObjectName) != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(BazookaResource);
        if (prefab == null)
            return;

        Transform hand = FindRightHand(visual);
        Transform parent = hand != null ? hand : visual;
        GameObject bazooka = Object.Instantiate(prefab, parent);
        bazooka.name = BazookaObjectName;
        bazooka.transform.localPosition = hand != null ? BazookaHandOffsetMeters : Vector3.zero;
        bazooka.transform.localRotation = hand != null ? Quaternion.Euler(BazookaHandEulerAngles) : Quaternion.identity;
        bazooka.transform.localScale = Vector3.one;
        RemoveExportedExtras(bazooka);
    }

    // Mano derecha vía avatar humanoide: funciona con cualquier rig compatible
    // (Mixamo incluido) sin hardcodear nombres de huesos.
    private static Transform FindRightHand(Transform visual)
    {
        Animator animator = visual.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman)
            return null;

        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        return hand != null && hand.IsChildOf(visual) ? hand : null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        return null;
    }

    // El FBX de Blender puede traer cámara, luz y colliders: el jugador usa los suyos.
    private static void RemoveExportedExtras(GameObject model)
    {
        foreach (Camera camera in model.GetComponentsInChildren<Camera>(true))
            Object.Destroy(camera.gameObject);
        foreach (Light light in model.GetComponentsInChildren<Light>(true))
            Object.Destroy(light.gameObject);
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            Object.Destroy(collider);
    }

    // Escala el modelo a la altura del CharacterController y apoya los pies en el piso:
    // el controller descansa a skinWidth sobre el suelo, así que los pies bajan esa distancia.
    private static void FitToController(Transform model, Transform player)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        if (bounds.size.y <= Mathf.Epsilon)
            return;

        model.localScale *= ControllerHeightMeters / bounds.size.y;
        bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
            bounds.Encapsulate(renderer.bounds);

        Vector3 feet = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 target = player.position + (Vector3.down * ((ControllerHeightMeters * HalfFactor) + ControllerSkinWidthMeters));
        model.position += target - feet;
    }

    private static Camera CreateCamera(Transform player)
    {
        var cameraObject = new GameObject("Camera");
        cameraObject.transform.SetParent(player);
        cameraObject.transform.localPosition = new Vector3(0f, CameraHeightMeters, CameraForwardMeters);
        var camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = CameraNearClipMeters;
        camera.tag = UntaggedCameraTag;
        cameraObject.AddComponent<AudioListener>();
        return camera;
    }

    // La Main Camera de la escena base quedaría fija en (0, 1, -10): se desactiva junto con su audio.
    private static void DisableOtherCameras(Camera keep, GameManager gameManager)
    {
        foreach (Camera other in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (other == null || other == keep)
                continue;

            other.enabled = false;
            if (other.TryGetComponent(out AudioListener listener)) listener.enabled = false;
            if (other.CompareTag(MainCameraTag) && !IsRegisteredPlayerCamera(gameManager, other))
                other.tag = UntaggedCameraTag;
            Debug.Log($"[TestSceneSetup] Camara vieja desactivada: {other.gameObject.name}");
        }
    }

    private static bool IsRegisteredPlayerCamera(GameManager manager, Camera camera)
    {
        if (manager == null || camera == null)
            return false;

        foreach (PlayerController player in manager.Players)
        {
            if (player != null && player.camaraJugador == camera)
                return true;
        }

        return false;
    }
}
