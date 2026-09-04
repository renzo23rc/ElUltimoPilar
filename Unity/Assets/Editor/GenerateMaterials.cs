using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides the Unity Editor command that creates and assigns prefab materials.
/// </summary>
public class GenerateMaterials
{
    [MenuItem("Tools/Generate Materials For Prefabs")]
    public static void Generate()
    {
        Debug.Log("[GenerateMaterials] Creando materiales...");

        try
        {
            EnsureFolder(PrefabAssetConventions.MaterialsFolder);
            EnsureFolder(PrefabAssetConventions.ResourcesFolder);
            EnsureFolder(PrefabAssetConventions.ResourcesMaterialsFolder);
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError($"[GenerateMaterials] {exception.Message}");
            return;
        }

        List<(string prefabName, string materialName, Color color, bool emissive, float emissionIntensity)> definitions =
            new List<(string prefabName, string materialName, Color color, bool emissive, float emissionIntensity)>
            {
                (
                    PrefabAssetConventions.RunnerPrefabName,
                    PrefabAssetConventions.RunnerMaterialName,
                    PrefabAssetConventions.RunnerColor,
                    false,
                    PrefabAssetConventions.NoEmissionIntensity),
                (
                    PrefabAssetConventions.ArtilleryPrefabName,
                    PrefabAssetConventions.ArtilleryMaterialName,
                    PrefabAssetConventions.ArtilleryColor,
                    false,
                    PrefabAssetConventions.NoEmissionIntensity),
                (
                    PrefabAssetConventions.ExplosivePrefabName,
                    PrefabAssetConventions.ExplosiveMaterialName,
                    PrefabAssetConventions.ExplosiveColor,
                    true,
                    PrefabAssetConventions.ExplosiveEmissionIntensity),
                (
                    PrefabAssetConventions.WeaverPrefabName,
                    PrefabAssetConventions.WeaverMaterialName,
                    PrefabAssetConventions.WeaverColor,
                    true,
                    PrefabAssetConventions.WeaverEmissionIntensity),
                (
                    PrefabAssetConventions.NestPrefabName,
                    PrefabAssetConventions.NestMaterialName,
                    PrefabAssetConventions.NestColor,
                    false,
                    PrefabAssetConventions.NoEmissionIntensity),
                (
                    PrefabAssetConventions.ColossusPrefabName,
                    PrefabAssetConventions.ColossusMaterialName,
                    PrefabAssetConventions.ColossusColor,
                    true,
                    PrefabAssetConventions.ColossusEmissionIntensity),
                (
                    PrefabAssetConventions.TurretPrefabName,
                    PrefabAssetConventions.TurretMaterialName,
                    PrefabAssetConventions.TurretColor,
                    true,
                    PrefabAssetConventions.TurretEmissionIntensity),
                (
                    PrefabAssetConventions.ProjectilePrefabName,
                    PrefabAssetConventions.ProjectileMaterialName,
                    PrefabAssetConventions.ProjectileColor,
                    true,
                    PrefabAssetConventions.ProjectileEmissionIntensity),
                (
                    PrefabAssetConventions.EnergyPickupPrefabName,
                    PrefabAssetConventions.EnergyPickupMaterialName,
                    PrefabAssetConventions.EnergyPickupColor,
                    true,
                    PrefabAssetConventions.EnergyPickupEmissionIntensity)
            };

        bool generationSucceeded = true;
        foreach ((string prefabName, string materialName, Color color, bool emissive, float emissionIntensity) definition in definitions)
        {
            bool materialSucceeded = GenerateMaterial(definition);
            if (!materialSucceeded)
            {
                generationSucceeded = false;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (generationSucceeded)
        {
            Debug.Log(
                "[GenerateMaterials] Materiales creados y asignados en Assets/Materials "
                + "y prefabs actualizados.");
        }
        else
        {
            Debug.LogError("[GenerateMaterials] La generación terminó con errores.");
        }
    }

    private static bool GenerateMaterial(
        (string prefabName, string materialName, Color color, bool emissive, float emissionIntensity) definition)
    {
        string materialPath = PrefabAssetConventions.GetMaterialPath(definition.materialName);
        string resourceMaterialPath = PrefabAssetConventions.GetResourceMaterialPath(
            definition.materialName);
        string resourcesPrefabPath = PrefabAssetConventions.GetResourcePrefabPath(
            definition.prefabName);
        string testsPrefabPath = PrefabAssetConventions.GetTestsPrefabPath(definition.prefabName);
        Material material = null;
        Material resourceMaterial = null;

        try
        {
            material = PrefabMaterialUtility.CreateConfiguredMaterial(
                definition.color,
                definition.emissionIntensity,
                definition.emissive);
            if (!SaveMaterial(material, materialPath))
            {
                return false;
            }

            resourceMaterial = new Material(material);
            resourceMaterial.name = definition.materialName;
            if (!SaveMaterial(resourceMaterial, resourceMaterialPath))
            {
                return false;
            }

            bool resourcesAssigned = AssignMaterialToPrefab(resourcesPrefabPath, materialPath);
            bool testsAssigned = AssignMaterialToPrefab(testsPrefabPath, materialPath);
            return resourcesAssigned && testsAssigned;
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError(
                $"[GenerateMaterials] No se pudo generar {definition.materialName}: "
                + exception.Message);
            return false;
        }
        catch (ArgumentException exception)
        {
            Debug.LogError(
                $"[GenerateMaterials] Argumento inválido al generar {definition.materialName}: "
                + exception.Message);
            return false;
        }
        finally
        {
            PrefabMaterialUtility.DestroyTemporaryMaterial(material);
            PrefabMaterialUtility.DestroyTemporaryMaterial(resourceMaterial);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path);
        string name = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"La ruta de carpeta no es válida: {path}");
        }

        parent = parent.Replace('\\', '/');
        EnsureFolder(parent);
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string folderGuid = AssetDatabase.CreateFolder(parent, name);
        if (string.IsNullOrEmpty(folderGuid) && !AssetDatabase.IsValidFolder(path))
        {
            throw new InvalidOperationException($"No se pudo crear la carpeta: {path}");
        }
    }

    private static bool SaveMaterial(Material material, string path)
    {
        if (material == null)
        {
            Debug.LogError($"[GenerateMaterials] No se puede guardar material nulo en {path}.");
            return false;
        }

        UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (existingAsset != null && !AssetDatabase.DeleteAsset(path))
        {
            Debug.LogError($"[GenerateMaterials] No se pudo reemplazar el material: {path}");
            return false;
        }

        try
        {
            AssetDatabase.CreateAsset(material, path);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError(
                $"[GenerateMaterials] No se pudo crear el material {path}: {exception.Message}");
            return false;
        }

        Material savedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (savedMaterial == null)
        {
            Debug.LogError($"[GenerateMaterials] El material no quedó guardado en {path}");
            return false;
        }

        Debug.Log($"[GenerateMaterials] Material guardado {path}");
        return true;
    }

    private static bool AssignMaterialToPrefab(string prefabPath, string materialPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[GenerateMaterials] Falta el prefab requerido: {prefabPath}");
            return false;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Debug.LogError($"[GenerateMaterials] Falta el material requerido: {materialPath}");
            return false;
        }

        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError(
                    $"[GenerateMaterials] LoadPrefabContents devolvió null para {prefabPath}");
                return false;
            }

            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = root.GetComponentInChildren<Renderer>();
            }
            if (renderer == null)
            {
                Debug.LogError(
                    $"[GenerateMaterials] No se encontró Renderer en {prefabPath}");
                return false;
            }

            PrefabMaterialUtility.AssignSharedMaterial(renderer, material, prefabPath);
            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.sharedMaterial = material;
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            if (savedPrefab == null)
            {
                Debug.LogError(
                    $"[GenerateMaterials] SaveAsPrefabAsset devolvió null para {prefabPath}");
                return false;
            }

            Debug.Log($"[GenerateMaterials] Asignado {materialPath} a {prefabPath}");
            return true;
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError(
                $"[GenerateMaterials] No se pudo asignar {materialPath} a {prefabPath}: "
                + exception.Message);
            return false;
        }
        catch (ArgumentException exception)
        {
            Debug.LogError(
                $"[GenerateMaterials] Argumento inválido al asignar {materialPath} a {prefabPath}: "
                + exception.Message);
            return false;
        }
        finally
        {
            if (root != null)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
