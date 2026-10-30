using System.Collections.Generic;
using System.IO;
using UltimoPilar.Arena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the Statue of Liberty gameplay scene using the same composition as the Obelisco scene.
/// </summary>
public static class CreateEstatuaDeLaLibertadScene
{
    private const string MenuItemPath = "Tools/Create Estatua De La Libertad Scene";
    private const string ObeliscoScenePath = "Assets/Scenes/Obelisco.unity";
    private const string NewScenePath = "Assets/Scenes/EstatuaDeLaLibertad.unity";
    private const string MapModelPath = "Assets/Models/EstatuaLib/EstatLib_fase1.fbx";
    private const string TurretPrefabPath = "Assets/Resources/Prefabs/Torreta.prefab";
    private const string ObeliscoObjectName = "Obelisco";
    private const string MapRootName = "EstatuaDeLaLibertad";
    private const string StatueChildName = "Statue";
    private const string PedestalChildName = "Base";
    private const string PlatformChildName = "Base.001";
    private const string IslandChildName = "isla";
    private const string PilarTintPropertyName = "tintByPhase";
    private const string LogPrefix = "[CreateEstatuaDeLaLibertadScene]";
    private const int TurretPointCount = 4;
    private const float TurretRadiusPaddingMeters = 1.25f;
    private const float PitRadiusMeters = 5f;
    private const float TurretClearanceOutsidePitMeters = 1.5f;
    private const float PilarMaximumHealth = 100f;
    private const float Phase2HealthThreshold = 75f;
    private const float Phase3HealthThreshold = 50f;
    private const float Phase4HealthThreshold = 25f;
    private const int InitialPhase = 1;
    private const float PilarLightRangeMeters = 30f;
    private const float PilarLightIntensity = 2f;
    private const float ExpectedArenaMeters = 87.5f;
    private const float OversizedMapThresholdMeters = 200f;
    private const float CircleRadians = Mathf.PI * 2f;
    private const float GameplaySurfaceHeightMeters = 2.1f;
    private const float PlayerSpawnZMeters = -8f;
    private const string ArenaSurfaceHeightProperty = "arenaSurfaceHeightMeters";
    private const string ArenaHazardHeightProperty = "arenaHazardHeightMeters";
    private const string HideGeneratedFloorProperty = "hideGeneratedFloor";
    private const string PlayerSpawnPositionProperty = "playerSpawnPosition";

    /// <summary>
    /// Creates the Statue of Liberty scene from the Unity Editor menu, replacing it if it already exists.
    /// </summary>
    [MenuItem(MenuItemPath)]
    public static void CreateFromMenu()
    {
        Create(overwrite: true);
    }

    [InitializeOnLoadMethod]
    private static void QueueCreateIfMissing()
    {
        EditorApplication.delayCall += CreateIfMissing;
    }

    private static void CreateIfMissing()
    {
        if (File.Exists(NewScenePath))
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning(
                $"{LogPrefix} Falta {NewScenePath}, pero hay cambios sin guardar. " +
                $"Usa {MenuItemPath} cuando puedas guardar.");
            return;
        }

        Create(overwrite: false);
    }

    /// <summary>
    /// Copies the Obelisco scene, swaps the Obelisco map for the Statue of Liberty, and wires the Pilar.
    /// </summary>
    public static void Create(bool overwrite)
    {
        if (!File.Exists(ObeliscoScenePath))
        {
            Debug.LogError($"{LogPrefix} No se encontró {ObeliscoScenePath}.");
            return;
        }

        GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapModelPath);
        if (mapPrefab == null)
        {
            Debug.LogError($"{LogPrefix} No se encontró el mapa {MapModelPath}.");
            return;
        }

        GameObject turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretPrefabPath);
        if (overwrite && File.Exists(NewScenePath))
        {
            AssetDatabase.DeleteAsset(NewScenePath);
        }

        if (!File.Exists(NewScenePath) && !AssetDatabase.CopyAsset(ObeliscoScenePath, NewScenePath))
        {
            Debug.LogError($"{LogPrefix} No se pudo copiar Obelisco a {NewScenePath}.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(NewScenePath, OpenSceneMode.Single);
        GameObject obelisco = GameObject.Find(ObeliscoObjectName);
        if (obelisco != null)
        {
            Object.DestroyImmediate(obelisco);
        }

        GameObject mapRoot = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, scene);
        mapRoot.name = MapRootName;
        if (mapRoot.GetComponent<EstatuaPilarAnchor>() == null)
        {
            mapRoot.AddComponent<EstatuaPilarAnchor>();
        }

        if (mapRoot.GetComponent<EstatuaPlayAreaLimiter>() == null)
        {
            mapRoot.AddComponent<EstatuaPlayAreaLimiter>();
        }

        ConfigureMapPlacement(mapRoot);

        Transform statue = FindChildByName(mapRoot.transform, StatueChildName);
        if (statue == null)
        {
            Debug.LogError($"{LogPrefix} El FBX no trae el hijo '{StatueChildName}'.");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        AddEnvironmentColliders(mapRoot.transform);
        Pilar pilar = ConfigurePilar(statue.gameObject, turretPrefab);
        EstatuaSurfaceHeights heights = MeasureSurfaceHeights(mapRoot.transform);
        PlaceTurretPoints(statue, pilar, heights.TurretY);
        PlacePilarLight(statue);
        ConfigureGameplayAlignment(mapRoot.transform, heights);
        RegisterSceneInBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"{LogPrefix} Escena creada en {NewScenePath}.");
    }

    private static void ConfigureMapPlacement(GameObject mapRoot)
    {
        mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        mapRoot.transform.localScale = Vector3.one;

        Transform island = FindChildByName(mapRoot.transform, IslandChildName);
        if (island != null)
        {
            Bounds islandBounds = EncapsulateRenderers(island);
            float horizontalExtent = Mathf.Max(islandBounds.size.x, islandBounds.size.z);
            if (horizontalExtent > OversizedMapThresholdMeters && horizontalExtent > 0f)
            {
                float scale = ExpectedArenaMeters / horizontalExtent;
                mapRoot.transform.localScale = Vector3.one * scale;
            }
        }

        Transform statue = FindChildByName(mapRoot.transform, StatueChildName);
        if (statue == null)
        {
            return;
        }

        Bounds monumentBounds = EncapsulateRenderers(statue);
        Transform pedestal = FindChildByName(mapRoot.transform, PedestalChildName);
        if (pedestal != null)
        {
            monumentBounds.Encapsulate(EncapsulateRenderers(pedestal));
        }

        Vector3 offset = new Vector3(-monumentBounds.center.x, 0f, -monumentBounds.center.z);
        if (island != null)
        {
            float islandMinY = EncapsulateRenderers(island).min.y;
            if (islandMinY < 0f)
            {
                offset.y = -islandMinY;
            }
        }

        mapRoot.transform.position += offset;
    }

    internal static Pilar ConfigurePilar(GameObject statue, GameObject turretPrefab)
    {
        if (!statue.TryGetComponent(out Pilar pilar))
        {
            pilar = statue.AddComponent<Pilar>();
        }

        pilar.vidaMaxima = PilarMaximumHealth;
        pilar.vidaActual = PilarMaximumHealth;
        pilar.umbralFase2 = Phase2HealthThreshold;
        pilar.umbralFase3 = Phase3HealthThreshold;
        pilar.umbralFase4 = Phase4HealthThreshold;
        pilar.faseActual = InitialPhase;
        pilar.colorFase1 = Color.cyan;
        pilar.colorFase2 = Color.yellow;
        pilar.colorFase3 = new Color(1f, 0.5f, 0f);
        pilar.colorFase4 = Color.red;
        pilar.torretasActivas = false;
        pilar.prefabTorreta = turretPrefab;

        SerializedObject serializedPilar = new SerializedObject(pilar);
        SerializedProperty tintProperty = serializedPilar.FindProperty(PilarTintPropertyName);
        if (tintProperty != null)
        {
            tintProperty.boolValue = false;
            serializedPilar.ApplyModifiedPropertiesWithoutUndo();
        }

        ConfigurePilarCollider(statue);
        PrefabUtility.RecordPrefabInstancePropertyModifications(pilar);
        return pilar;
    }

    private static void ConfigurePilarCollider(GameObject statue)
    {
        if (!statue.TryGetComponent(out BoxCollider boxCollider))
        {
            boxCollider = statue.AddComponent<BoxCollider>();
        }

        if (statue.TryGetComponent(out MeshRenderer meshRenderer))
        {
            boxCollider.center = meshRenderer.localBounds.center;
            boxCollider.size = meshRenderer.localBounds.size;
            return;
        }

        Bounds worldBounds = EncapsulateRenderers(statue.transform);
        boxCollider.center = statue.transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = statue.transform.InverseTransformVector(worldBounds.size);
        boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    internal static void PlaceTurretPoints(Transform statue, Pilar pilar, float turretHeightMeters)
    {
        ClearChildrenWithPrefix(statue, "PuntoTorreta_");
        Bounds monumentBounds = EncapsulateRenderers(statue);
        float radiusAroundMonument = Mathf.Max(monumentBounds.extents.x, monumentBounds.extents.z) * TurretRadiusPaddingMeters;
        float radius = Mathf.Max(radiusAroundMonument, PitRadiusMeters + TurretClearanceOutsidePitMeters);
        var points = new Transform[TurretPointCount];
        for (int i = 0; i < TurretPointCount; i++)
        {
            var point = new GameObject($"PuntoTorreta_{i}");
            float angle = (i / (float)TurretPointCount) * CircleRadians;
            Vector3 worldPosition = new Vector3(
                monumentBounds.center.x + (Mathf.Cos(angle) * radius),
                turretHeightMeters,
                monumentBounds.center.z + (Mathf.Sin(angle) * radius));
            point.transform.SetParent(statue, true);
            point.transform.position = worldPosition;
            point.transform.rotation = Quaternion.identity;
            points[i] = point.transform;
        }

        pilar.puntosTorretas = points;
        PrefabUtility.RecordPrefabInstancePropertyModifications(pilar);
    }

    internal static void PlacePilarLight(Transform statue)
    {
        ClearChildrenWithPrefix(statue, "LuzPilar");
        Bounds monumentBounds = EncapsulateRenderers(statue);
        var lightObject = new GameObject("LuzPilar");
        lightObject.transform.SetParent(statue, true);
        lightObject.transform.position = new Vector3(
            monumentBounds.center.x,
            monumentBounds.max.y,
            monumentBounds.center.z);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.cyan;
        light.range = PilarLightRangeMeters;
        light.intensity = PilarLightIntensity;
        lightObject.AddComponent<UniversalAdditionalLightData>();
    }

    private static EstatuaSurfaceHeights MeasureSurfaceHeights(Transform mapRoot)
    {
        Transform island = FindChildByName(mapRoot, IslandChildName);
        Transform star = FindChildByName(mapRoot, PlatformChildName);
        Transform pedestal = FindChildByName(mapRoot, PedestalChildName);
        float grassTopY = island != null ? EncapsulateRenderers(island).max.y : GameplaySurfaceHeightMeters;
        float starTopY = star != null ? EncapsulateRenderers(star).max.y : grassTopY;
        float pedestalBottomY = pedestal != null ? EncapsulateRenderers(pedestal).min.y : starTopY;
        float platformY = pedestalBottomY > starTopY ? pedestalBottomY : starTopY;
        return EstatuaSurfaceHeights.FromMeshTops(
            grassTopY,
            platformY,
            TestScenePlayerBuilder.ControllerHalfHeightMeters,
            EstatuaSurfaceHeights.DefaultTurretPivotAboveSurfaceMeters);
    }

    private static void ConfigureGameplayAlignment(Transform mapRoot, EstatuaSurfaceHeights heights)
    {
        TestSceneSetup setup = Object.FindFirstObjectByType<TestSceneSetup>();
        if (setup != null)
        {
            SerializedObject serializedSetup = new SerializedObject(setup);
            serializedSetup.FindProperty(ArenaSurfaceHeightProperty).floatValue = heights.WalkSurfaceY;
            serializedSetup.FindProperty(ArenaHazardHeightProperty).floatValue = heights.HazardY;
            serializedSetup.FindProperty(HideGeneratedFloorProperty).boolValue = true;
            serializedSetup.FindProperty(PlayerSpawnPositionProperty).vector3Value =
                new Vector3(0f, heights.PlayerSpawnY, PlayerSpawnZMeters);
            serializedSetup.ApplyModifiedPropertiesWithoutUndo();
        }

        SpawnZone[] zones = Object.FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            Vector3 zonePosition = zones[i].transform.position;
            zonePosition.y = heights.WalkSurfaceY;
            zones[i].transform.position = zonePosition;
        }

        EstatuaPilarAnchor anchor = mapRoot.GetComponent<EstatuaPilarAnchor>();
        if (anchor != null)
        {
            SerializedObject serializedAnchor = new SerializedObject(anchor);
            SerializedProperty turretHeight = serializedAnchor.FindProperty("turretHeightMeters");
            if (turretHeight != null)
            {
                turretHeight.floatValue = heights.TurretY;
                serializedAnchor.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void AddEnvironmentColliders(Transform mapRoot)
    {
        AddMeshColliderIfMissing(FindChildByName(mapRoot, IslandChildName));
        AddMeshColliderIfMissing(FindChildByName(mapRoot, PedestalChildName));
        AddMeshColliderIfMissing(FindChildByName(mapRoot, PlatformChildName));
    }

    private static void AddMeshColliderIfMissing(Transform target)
    {
        if (target == null || !target.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
        {
            return;
        }

        if (target.GetComponent<MeshCollider>() != null)
        {
            return;
        }

        MeshCollider collider = target.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = meshFilter.sharedMesh;
    }

    private static void RegisterSceneInBuildSettings()
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path == NewScenePath)
            {
                return;
            }
        }

        var scenes = new List<EditorBuildSettingsScene>(current)
        {
            new EditorBuildSettingsScene(NewScenePath, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ClearChildrenWithPrefix(Transform parent, string prefix)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix))
            {
                toDestroy.Add(child.gameObject);
            }
        }

        for (int i = 0; i < toDestroy.Count; i++)
        {
            Object.DestroyImmediate(toDestroy[i]);
        }
    }

    internal static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    internal static Bounds EncapsulateRenderers(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
