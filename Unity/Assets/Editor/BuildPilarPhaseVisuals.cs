using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the PilarVisuales container (phase 2-4 monument variants plus <see cref="ArenaPhaseVisuals"/>)
/// under a map root already placed in the open scene. The scene is left dirty, never saved.
/// </summary>
public static class BuildPilarPhaseVisuals
{
    private const string ObeliscoMenuItemPath = "Tools/Pilar Visuals/Build for Obelisco (open scene)";
    private const string CristoMenuItemPath = "Tools/Pilar Visuals/Build for Cristo Redentor (open scene)";
    private const string ContainerName = "PilarVisuales";
    private const string OriginalPieceNamesProperty = "originalPieceNames";
    private const string LogPrefix = "[BuildPilarPhaseVisuals]";
    private const string BakedMaterialPrefix = "Baked_";
    private const string MaterialExtension = ".mat";

    // Nodes that carry the per-phase damage: obelisk, the Evita mural ("1"), buildings, trees, ground, fallen pieces, and ruins.
    private static readonly string[] ObeliscoDamagePrefixes =
    {
        "Obelisco_",
        "1",
        "Edificios_Merged",
        "Arboles_Merged",
        "Suelo_Merged",
        "Mural_Pedazo_",
        "9dejulio",
        "Ruinas_Merged",
        "RockCut_"
    };

    private static readonly MapProfile ObeliscoProfile = new(
        "Obelisco",
        new[] { "Obelisco" },
        new[] { "Obelisco_LowPoly", "Obelisco_Windows", "1", "Edificios_Merged", "Arboles_Merged", "Suelo_Merged" },
        new[]
        {
            new PhaseVariant("Assets/Models/ObeliscoV2/obelisco_fase2.fbx", ObeliscoDamagePrefixes),
            new PhaseVariant("Assets/Models/ObeliscoV2/obelisco_fase3.fbx", ObeliscoDamagePrefixes),
            new PhaseVariant("Assets/Models/ObeliscoV2/obelisco_fase4.fbx", ObeliscoDamagePrefixes)
        });

    private static readonly MapProfile CristoProfile = new(
        "Cristo Redentor",
        new[] { "CristoRedentor", "cristo_redentor_lowpoly_fase1" },
        new[] { "Cristo_Redentor_Low" },
        new[]
        {
            new PhaseVariant(
                "Assets/Models/CristoRedentor/cristo_redentor_lowpoly_fase2.fbx",
                new[] { "Cristo_Redentor_F2", "Escaleras" }),
            new PhaseVariant(
                "Assets/Models/CristoRedentor/cristo_redentor_lowpoly_fase3.fbx",
                new[] { "Cristo_F3_", "Escombro", "Escaleras" }),
            new PhaseVariant(
                "Assets/Models/CristoRedentor/cristo_redentor_lowpoly_fase4.fbx",
                new[] { "Cristo_F3_", "Cristo_F4_", "Escombro" })
        },
        "Assets/Models/CristoRedentor/Materials");

    [MenuItem(ObeliscoMenuItemPath)]
    private static void BuildForObelisco()
    {
        Build(ObeliscoProfile);
    }

    [MenuItem(CristoMenuItemPath)]
    private static void BuildForCristo()
    {
        Build(CristoProfile);
    }

    private static void Build(MapProfile profile)
    {
        GameObject mapRoot = FindMapRoot(profile);
        if (mapRoot == null)
        {
            Debug.LogError(
                $"{LogPrefix} No hay un objeto {string.Join(" / ", profile.RootNames)} en la escena abierta. " +
                $"Arrastra el FBX de fase 1 de {profile.DisplayName} a la escena y reintenta.");
            return;
        }

        GameObject[] variantPrefabs = LoadVariantPrefabs(profile);
        if (variantPrefabs == null)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        RemoveExistingContainer(mapRoot.transform);

        var container = new GameObject(ContainerName);
        Undo.RegisterCreatedObjectUndo(container, $"Build {ContainerName}");
        container.transform.SetParent(mapRoot.transform, false);

        for (int i = 0; i < variantPrefabs.Length; i++)
        {
            AddVariant(container.transform, variantPrefabs[i], profile.Variants[i], profile.BakedMaterialFolder);
        }

        ConfigureComponent(container, profile);
        EditorUtility.SetDirty(mapRoot);
        EditorSceneManager.MarkSceneDirty(mapRoot.scene);
        Selection.activeGameObject = container;
        Debug.Log($"{LogPrefix} {ContainerName} creado bajo '{mapRoot.name}'. Guarda la escena para conservarlo.");
    }

    private static GameObject FindMapRoot(MapProfile profile)
    {
        for (int i = 0; i < profile.RootNames.Length; i++)
        {
            GameObject found = GameObject.Find(profile.RootNames[i]);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject[] LoadVariantPrefabs(MapProfile profile)
    {
        var prefabs = new GameObject[profile.Variants.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(profile.Variants[i].ModelPath);
            if (prefabs[i] == null)
            {
                Debug.LogError($"{LogPrefix} No se encontro el modelo {profile.Variants[i].ModelPath}.");
                return null;
            }
        }

        return prefabs;
    }

    private static void RemoveExistingContainer(Transform mapRoot)
    {
        Transform existing = mapRoot.Find(ContainerName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }

    private static void AddVariant(Transform container, GameObject prefab, PhaseVariant variant, string bakedMaterialFolder)
    {
        var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container);
        Undo.RegisterCreatedObjectUndo(copy, $"Add {copy.name}");
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.identity;
        copy.transform.localScale = Vector3.one;

        foreach (Transform child in copy.transform)
        {
            child.gameObject.SetActive(variant.Keeps(child.name));
            AssignBakedMaterial(child, bakedMaterialFolder);
        }

        copy.SetActive(false);
    }

    private static void AssignBakedMaterial(Transform piece, string bakedMaterialFolder)
    {
        if (bakedMaterialFolder == null || !piece.TryGetComponent(out Renderer pieceRenderer))
        {
            return;
        }

        string path = $"{bakedMaterialFolder}/{BakedMaterialPrefix}{piece.name}{MaterialExtension}";
        var bakedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (bakedMaterial == null)
        {
            return;
        }

        var slots = new Material[pieceRenderer.sharedMaterials.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = bakedMaterial;
        }

        pieceRenderer.sharedMaterials = slots;
    }

    private static void ConfigureComponent(GameObject container, MapProfile profile)
    {
        ArenaPhaseVisuals visuals = Undo.AddComponent<ArenaPhaseVisuals>(container);
        var serializedVisuals = new SerializedObject(visuals);
        SerializedProperty pieceNames = serializedVisuals.FindProperty(OriginalPieceNamesProperty);
        pieceNames.arraySize = profile.OriginalPieceNames.Length;
        for (int i = 0; i < profile.OriginalPieceNames.Length; i++)
        {
            pieceNames.GetArrayElementAtIndex(i).stringValue = profile.OriginalPieceNames[i];
        }

        serializedVisuals.ApplyModifiedPropertiesWithoutUndo();
    }

    private sealed class MapProfile
    {
        public MapProfile(
            string displayName,
            string[] rootNames,
            string[] originalPieceNames,
            PhaseVariant[] variants,
            string bakedMaterialFolder = null)
        {
            BakedMaterialFolder = bakedMaterialFolder;
            DisplayName = displayName;
            RootNames = rootNames;
            OriginalPieceNames = originalPieceNames;
            Variants = variants;
        }

        public string DisplayName { get; }

        /// <summary>Folder with per-piece <c>Baked_*</c> materials, or null when the map has none.</summary>
        public string BakedMaterialFolder { get; }

        public string[] RootNames { get; }

        public string[] OriginalPieceNames { get; }

        public PhaseVariant[] Variants { get; }
    }

    private sealed class PhaseVariant
    {
        private readonly string[] keepPrefixes;

        public PhaseVariant(string modelPath, string[] keepPrefixes)
        {
            ModelPath = modelPath;
            this.keepPrefixes = keepPrefixes;
        }

        public string ModelPath { get; }

        /// <summary>True when a child of the variant is monument content rather than extra environment.</summary>
        public bool Keeps(string childName)
        {
            for (int i = 0; i < keepPrefixes.Length; i++)
            {
                if (childName.StartsWith(keepPrefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
