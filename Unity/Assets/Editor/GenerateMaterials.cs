using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GenerateMaterials
{
    private const float NoEmissionIntensity = 0f;
    private const float ExplosiveEmissionIntensity = 0.4f;
    private const float WeaverEmissionIntensity = 0.3f;
    private const float ColossusEmissionIntensity = 0.2f;
    private const float TurretEmissionIntensity = 0.6f;
    private const float ProjectileEmissionIntensity = 1.2f;
    private const float EnergyEmissionIntensity = 0.8f;

    private static readonly Color WeaverColor = new Color(1f, 0f, 1f);
    private static readonly Color ColossusColor = new Color(0.5f, 0f, 0f);
    private static readonly Color TurretColor = new Color(1f, 0.85f, 0.1f);
    private static readonly Color ProjectileColor = new Color(1f, 0.5f, 0f);

    [MenuItem("Tools/Generate Materials For Prefabs")]
    public static void Generate()
    {
        Debug.Log("[GenerateMaterials] Creando materiales...");

        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Materials");

        // Definir materiales por prefab
        var defs = new List<(string prefabPath, string matName, Color color, bool emissive, float emissionIntensity)>
        {
            ("Assets/Resources/Prefabs/Corredor.prefab", "Mat_Corredor", Color.red, false, 0f),
            ("Assets/Resources/Prefabs/Artillero.prefab", "Mat_Artillero", Color.blue, false, 0f),
            ("Assets/Resources/Prefabs/Explosivo.prefab", "Mat_Explosivo", Color.yellow, true, 0.4f),
                ("Assets/Resources/Prefabs/Tejedor.prefab", "Mat_Tejedor", WeaverColor, true, WeaverEmissionIntensity),
            ("Assets/Resources/Prefabs/Nido.prefab", "Mat_Nido", Color.gray, false, NoEmissionIntensity),
            ("Assets/Resources/Prefabs/Coloso.prefab", "Mat_Coloso", ColossusColor, true, ColossusEmissionIntensity),
            ("Assets/Resources/Prefabs/Torreta.prefab", "Mat_Torreta", TurretColor, true, TurretEmissionIntensity),
            ("Assets/Resources/Prefabs/ProyectilBase.prefab", "Mat_Proyectil", ProjectileColor, true, ProjectileEmissionIntensity),
            ("Assets/Resources/Prefabs/EnergiaPickup.prefab", "Mat_Energia", Color.cyan, true, EnergyEmissionIntensity),
        };

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        if (urpLit == null)
        {
            Debug.LogError("[GenerateMaterials] No se encontró shader URP/Lit ni Standard");
            return;
        }

        foreach (var def in defs)
        {
            string materialPath = $"Assets/Materials/{def.matName}.mat";
            string resourceMaterialPath = $"Assets/Resources/Materials/{def.matName}.mat";

            // Crear material asset
            var mat = new Material(urpLit);
            mat.name = def.matName;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", def.color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", def.color);
            mat.color = def.color;

            if (def.emissive && mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", def.color * def.emissionIntensity);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            // Para URP transparente en energía/proyectil si necesita alpha, no necesario para sólido
            // Guardar en ambas ubicaciones
            SaveMaterial(mat, materialPath);
            SaveMaterial(new Material(mat), resourceMaterialPath);

            // Asignar a prefab
            AssignMaterialToPrefab(def.prefabPath, materialPath);
            // También asignar a duplicate en Tests/Prefabs
            string testsPath = def.prefabPath.Replace("Assets/Resources/Prefabs/", "Assets/Tests/Prefabs/");
            AssignMaterialToPrefab(testsPath, materialPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GenerateMaterials] Materiales creados y asignados en Assets/Materials y prefabs actualizados");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path);
        var name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static void SaveMaterial(Material mat, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        Debug.Log($"[GenerateMaterials] Material guardado {path}");
    }

    static void AssignMaterialToPrefab(string prefabPath, string matPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (prefab == null || mat == null)
        {
            Debug.LogWarning($"[GenerateMaterials] No se pudo asignar {matPath} a {prefabPath}");
            return;
        }

        // Cargar contenido del prefab para editar
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[GenerateMaterials] LoadPrefabContents falló para {prefabPath}");
            return;
        }

        var renderer = root.GetComponent<Renderer>();
        if (renderer == null) renderer = root.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = mat;
            // Para TrailRenderer en Proyectil, también asignar mismo material
            var trail = root.GetComponent<TrailRenderer>();
            if (trail != null) trail.sharedMaterial = mat;
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"[GenerateMaterials] Asignado {matPath} a {prefabPath}");
    }
}
