using System.Collections.Generic;
using System.IO;
using UltimoPilar.Arena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the Cristo Redentor gameplay scene from the Obelisco scene: swaps the Obelisco map for the statue,
/// wires the Pilar on the statue, aligns the walk plane to the lookout platform, and adds the phase visuals.
/// </summary>
public static class CreateCristoRedentorScene
{
    private const string MenuItemPath = "Tools/Create Cristo Redentor Scene";
    private const string ObeliscoScenePath = "Assets/Scenes/Obelisco.unity";
    private const string NewScenePath = "Assets/Scenes/CristoRedentor.unity";
    private const string MapModelPath = "Assets/Models/CristoRedentor/cristo_redentor_lowpoly_fase1.fbx";
    private const string TurretPrefabPath = "Assets/Resources/Prefabs/Torreta.prefab";
    private const string ObeliscoObjectName = "Obelisco";
    private const string ObeliscoWallPrefix = "MuroAutos_";
    private const string MapRootName = "CristoRedentor";
    private const string StatueChildName = "Cristo_Redentor_Low";
    private const string PlatformChildName = "Mirador_Plataforma";
    private const string ImportedLightName = "Sol";
    private const string ImportedCameraName = "Cámara";
    private const string StarObjectNameProperty = "starObjectName";
    private const string PlayAreaPaddingProperty = "playAreaPaddingMeters";
    private const string ArenaSurfaceHeightProperty = "arenaSurfaceHeightMeters";
    private const string ArenaHazardHeightProperty = "arenaHazardHeightMeters";
    private const string HideGeneratedFloorProperty = "hideGeneratedFloor";
    private const string PlayerSpawnPositionProperty = "playerSpawnPosition";
    private const string PitRadiusProperty = "pitRadiusMeters";
    private const string LogPrefix = "[CreateCristoRedentorScene]";
    private const float PlayerSpawnZMeters = -8f;
    private const float PitRadiusPaddingMeters = 0.75f;
    private const float DefaultPitRadiusMeters = 5f;

    // The handrail sits one meter inside the platform edge, so the play area shrinks by that much.
    private const float PlayAreaPaddingMeters = -1f;

    /// <summary>Creates the Cristo Redentor scene from the Unity Editor menu, replacing it if it already exists.</summary>
    [MenuItem(MenuItemPath)]
    public static void CreateFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning($"{LogPrefix} Sal de Play antes de crear la escena.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Create();
    }

    private static void Create()
    {
        if (!File.Exists(ObeliscoScenePath))
        {
            Debug.LogError($"{LogPrefix} No se encontro {ObeliscoScenePath}.");
            return;
        }

        var mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapModelPath);
        if (mapPrefab == null)
        {
            Debug.LogError($"{LogPrefix} No se encontro el mapa {MapModelPath}.");
            return;
        }

        var turretPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurretPrefabPath);
        if (File.Exists(NewScenePath))
        {
            AssetDatabase.DeleteAsset(NewScenePath);
        }

        if (!AssetDatabase.CopyAsset(ObeliscoScenePath, NewScenePath))
        {
            Debug.LogError($"{LogPrefix} No se pudo copiar Obelisco a {NewScenePath}.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(NewScenePath, OpenSceneMode.Single);
        RemoveObeliscoObjects();

        var mapRoot = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, scene);
        mapRoot.name = MapRootName;
        mapRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        mapRoot.transform.localScale = Vector3.one;
        DisableImportedLightAndCamera(mapRoot.transform);

        Transform statue = CreateEstatuaDeLaLibertadScene.FindChildByName(mapRoot.transform, StatueChildName);
        Transform platform = CreateEstatuaDeLaLibertadScene.FindChildByName(mapRoot.transform, PlatformChildName);
        if (statue == null || platform == null)
        {
            Debug.LogError($"{LogPrefix} El FBX no trae '{StatueChildName}' o '{PlatformChildName}'.");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        Pilar pilar = CreateEstatuaDeLaLibertadScene.ConfigurePilar(statue.gameObject, turretPrefab);
        EstatuaSurfaceHeights heights = MeasureSurfaceHeights(platform);
        CreateEstatuaDeLaLibertadScene.PlaceTurretPoints(statue, pilar, heights.TurretY);
        CreateEstatuaDeLaLibertadScene.PlacePilarLight(statue);
        ConfigureGameplayAlignment(statue, heights);
        ConfigurePlayAreaLimiter(mapRoot);
        BuildPilarPhaseVisuals.BuildForCristoRoot();
        RegisterSceneInBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"{LogPrefix} Escena creada en {NewScenePath}.");
    }

    private static void RemoveObeliscoObjects()
    {
        GameObject obelisco = GameObject.Find(ObeliscoObjectName);
        if (obelisco != null)
        {
            Object.DestroyImmediate(obelisco);
        }

        var walls = new List<GameObject>();
        foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (candidate.name.StartsWith(ObeliscoWallPrefix, System.StringComparison.Ordinal))
            {
                walls.Add(candidate);
            }
        }

        for (int i = 0; i < walls.Count; i++)
        {
            Object.DestroyImmediate(walls[i]);
        }
    }

    private static void DisableImportedLightAndCamera(Transform mapRoot)
    {
        SetChildActive(mapRoot, ImportedLightName, false);
        SetChildActive(mapRoot, ImportedCameraName, false);
    }

    private static void SetChildActive(Transform root, string childName, bool active)
    {
        Transform child = CreateEstatuaDeLaLibertadScene.FindChildByName(root, childName);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static EstatuaSurfaceHeights MeasureSurfaceHeights(Transform platform)
    {
        float platformTopY = CreateEstatuaDeLaLibertadScene.EncapsulateRenderers(platform).max.y;
        return EstatuaSurfaceHeights.FromMeshTops(
            platformTopY,
            platformTopY,
            TestScenePlayerBuilder.ControllerHalfHeightMeters,
            EstatuaSurfaceHeights.DefaultTurretPivotAboveSurfaceMeters);
    }

    private static void ConfigureGameplayAlignment(Transform statue, EstatuaSurfaceHeights heights)
    {
        var setup = Object.FindFirstObjectByType<TestSceneSetup>();
        if (setup != null)
        {
            Bounds statueBounds = CreateEstatuaDeLaLibertadScene.EncapsulateRenderers(statue);
            float pitRadius = EstatuaSurfaceHeights.ResolvePitRadius(
                0f,
                Mathf.Max(statueBounds.extents.x, statueBounds.extents.z),
                PitRadiusPaddingMeters,
                DefaultPitRadiusMeters);

            var serializedSetup = new SerializedObject(setup);
            serializedSetup.FindProperty(ArenaSurfaceHeightProperty).floatValue = heights.WalkSurfaceY;
            serializedSetup.FindProperty(ArenaHazardHeightProperty).floatValue = heights.HazardY;
            serializedSetup.FindProperty(HideGeneratedFloorProperty).boolValue = true;
            serializedSetup.FindProperty(PlayerSpawnPositionProperty).vector3Value =
                new Vector3(0f, heights.PlayerSpawnY, PlayerSpawnZMeters);
            serializedSetup.FindProperty(PitRadiusProperty).floatValue = pitRadius;
            serializedSetup.ApplyModifiedPropertiesWithoutUndo();
        }

        SpawnZone[] zones = Object.FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            Vector3 zonePosition = zones[i].transform.position;
            zonePosition.y = heights.WalkSurfaceY;
            zones[i].transform.position = zonePosition;
        }
    }

    private static void ConfigurePlayAreaLimiter(GameObject mapRoot)
    {
        var limiter = mapRoot.AddComponent<EstatuaPlayAreaLimiter>();
        var serializedLimiter = new SerializedObject(limiter);
        serializedLimiter.FindProperty(StarObjectNameProperty).stringValue = PlatformChildName;
        serializedLimiter.FindProperty(PlayAreaPaddingProperty).floatValue = PlayAreaPaddingMeters;
        serializedLimiter.ApplyModifiedPropertiesWithoutUndo();
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
}
