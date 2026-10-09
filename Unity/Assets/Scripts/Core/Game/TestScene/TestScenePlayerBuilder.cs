using UltimoPilar.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private const float CameraNearClipMeters = 0.1f;
    private const float ControllerRadiusMeters = 0.5f;
    private const float ControllerHeightMeters = 2f;
    private const float ControllerSkinWidthMeters = 0.08f;
    private const float HalfFactor = 0.5f;
    private const float AnimationReferenceSpeedMetersPerSecond = 8f;
    private const string VisualObjectName = "JugadorVisual";
    private const string PrimaryModelResource = "Models/Personajes/pj1_anims";
    private const string LocomotionControllerResource = "Animation/PlayerLocomotion";
    private const string SecondaryModelResource = "Models/Personajes/pj2";
    private static readonly Vector3 PrimarySpawnPosition = new Vector3(0f, 1f, -8f);
    private static readonly Vector3 VisualScale = new Vector3(1f, 2f, 1f);

    /// <summary>
    /// Creates the inactive primary player; activate it only after the scene composition is complete.
    /// </summary>
    /// <param name="actions">The input actions asset assigned to the player.</param>
    /// <param name="material">The optional body material.</param>
    /// <param name="gameManager">The manager used to keep registered cameras tagged.</param>
    /// <returns>The primary player.</returns>
    public static PlayerController CreatePrimary(InputActionAsset actions, Material material, GameManager gameManager)
    {
        // GameObject vacío + hijo visual para evitar conflictos de colliders.
        var player = new GameObject("Jugador");
        player.SetActive(false);
        player.transform.position = PrimarySpawnPosition;

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
        return true;
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
        cameraObject.transform.localPosition = new Vector3(0f, CameraHeightMeters, 0f);
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
