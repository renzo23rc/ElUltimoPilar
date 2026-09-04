using UnityEditor;
using UnityEngine;

/// <summary>
/// Validates the generated prefab assets and their serialized references.
/// </summary>
internal static class GeneratedPrefabValidator
{
    private const string MenuItemPath = "Tools/Validate Generated Prefabs";

    /// <summary>
    /// Runs prefab validation from the Unity Editor menu.
    /// </summary>
    [MenuItem(MenuItemPath)]
    public static void ValidateGeneratedPrefabsMenu()
    {
        ValidateGeneratedPrefabs();
    }

    /// <summary>
    /// Validates the nine generated prefabs in Resources and Tests.
    /// </summary>
    /// <returns><c>true</c> when every generated prefab is valid.</returns>
    public static bool ValidateGeneratedPrefabs()
    {
        bool isValid = true;
        GameObject[] resourcesPrefabs = LoadPrefabSet(
            PrefabAssetConventions.ResourcesPrefabFolder,
            ref isValid);
        GameObject[] testsPrefabs = LoadPrefabSet(
            PrefabAssetConventions.TestsPrefabFolder,
            ref isValid);

        ValidatePrefabSet(
            PrefabAssetConventions.ResourcesPrefabFolder,
            resourcesPrefabs,
            resourcesPrefabs,
            ref isValid);
        ValidatePrefabSet(
            PrefabAssetConventions.TestsPrefabFolder,
            testsPrefabs,
            resourcesPrefabs,
            ref isValid);

        if (!isValid)
        {
            Debug.LogError("[GeneratedPrefabValidator] La validación de prefabs generados falló.");
            return false;
        }

        Debug.Log("[GeneratedPrefabValidator] Prefabs generados válidos en Resources y Tests.");
        return true;
    }

    private static GameObject[] LoadPrefabSet(string folder, ref bool isValid)
    {
        GameObject[] prefabs = new GameObject[PrefabAssetConventions.GeneratedPrefabNames.Length];
        for (int index = 0; index < PrefabAssetConventions.GeneratedPrefabNames.Length; index++)
        {
            string prefabName = PrefabAssetConventions.GeneratedPrefabNames[index];
            string path = PrefabAssetConventions.GetPrefabPath(folder, prefabName);
            prefabs[index] = LoadPrefabAsset(path, ref isValid);
        }

        return prefabs;
    }

    private static GameObject LoadPrefabAsset(string path, ref bool isValid)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid))
        {
            ReportError($"No se encontró GUID/meta resoluble para {path}.", ref isValid);
            return null;
        }

        string resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
        if (resolvedPath != path)
        {
            ReportError(
                $"El GUID {guid} de {path} resuelve a {resolvedPath}.",
                ref isValid);
            return null;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            ReportError($"No se pudo cargar el asset de prefab {path}.", ref isValid);
            return null;
        }

        return prefab;
    }

    private static void ValidatePrefabSet(
        string folder,
        GameObject[] prefabs,
        GameObject[] expectedPrefabs,
        ref bool isValid)
    {
        ValidateEnergyPrefab(
            prefabs[0],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.EnergyPickupPrefabName),
            ref isValid);
        ValidateProjectilePrefab(
            prefabs[1],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.ProjectilePrefabName),
            ref isValid);
        ValidateRunnerPrefab(
            prefabs[2],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.RunnerPrefabName),
            expectedPrefabs[0],
            ref isValid);
        ValidateArtilleryPrefab(
            prefabs[3],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.ArtilleryPrefabName),
            expectedPrefabs[0],
            expectedPrefabs[1],
            ref isValid);
        ValidateExplosivePrefab(
            prefabs[4],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.ExplosivePrefabName),
            expectedPrefabs[0],
            ref isValid);
        ValidateWeaverPrefab(
            prefabs[5],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.WeaverPrefabName),
            expectedPrefabs[0],
            ref isValid);
        ValidateNestPrefab(
            prefabs[6],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.NestPrefabName),
            expectedPrefabs[0],
            expectedPrefabs[2],
            ref isValid);
        ValidateColossusPrefab(
            prefabs[7],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.ColossusPrefabName),
            expectedPrefabs[0],
            ref isValid);
        ValidateTurretPrefab(
            prefabs[8],
            PrefabAssetConventions.GetPrefabPath(
                folder,
                PrefabAssetConventions.TurretPrefabName),
            expectedPrefabs[1],
            ref isValid);
    }

    private static void ValidateEnergyPrefab(
        GameObject prefab,
        string path,
        ref bool isValid)
    {
        if (prefab == null)
        {
            return;
        }

        ValidateRequiredComponent<EnergyPickup>(prefab, path, ref isValid);
        ValidateRequiredComponent<PooledObject>(prefab, path, ref isValid);
        SphereCollider collider = ValidateRequiredComponent<SphereCollider>(
            prefab,
            path,
            ref isValid);
        if (collider != null && !collider.isTrigger)
        {
            ReportError($"El collider de {path} debe ser trigger.", ref isValid);
        }

        Rigidbody rigidbody = ValidateRequiredComponent<Rigidbody>(prefab, path, ref isValid);
        if (rigidbody != null && (!rigidbody.isKinematic || rigidbody.useGravity))
        {
            ReportError(
                $"El Rigidbody de {path} debe ser cinemático y sin gravedad.",
                ref isValid);
        }

        PooledObject pooledObject = prefab.GetComponent<PooledObject>();
        if (pooledObject != null
            && pooledObject.poolKey != PrefabAssetConventions.EnergyPickupPoolKey)
        {
            ReportError(
                $"El poolKey de {path} debe ser {PrefabAssetConventions.EnergyPickupPoolKey}.",
                ref isValid);
        }

        ValidateRenderer(prefab, path, ref isValid);
    }

    private static void ValidateProjectilePrefab(
        GameObject prefab,
        string path,
        ref bool isValid)
    {
        if (prefab == null)
        {
            return;
        }

        ValidateRequiredComponent<Projectile>(prefab, path, ref isValid);
        ValidateRequiredComponent<PooledObject>(prefab, path, ref isValid);
        SphereCollider collider = ValidateRequiredComponent<SphereCollider>(
            prefab,
            path,
            ref isValid);
        if (collider != null && !collider.isTrigger)
        {
            ReportError($"El collider de {path} debe ser trigger.", ref isValid);
        }

        Rigidbody rigidbody = ValidateRequiredComponent<Rigidbody>(prefab, path, ref isValid);
        if (rigidbody != null && rigidbody.useGravity)
        {
            ReportError($"El Rigidbody de {path} no debe usar gravedad.", ref isValid);
        }

        ValidateRequiredComponent<TrailRenderer>(prefab, path, ref isValid);
        ValidateRequiredComponent<Light>(prefab, path, ref isValid);
        PooledObject pooledObject = prefab.GetComponent<PooledObject>();
        if (pooledObject != null
            && pooledObject.poolKey != PrefabAssetConventions.ProjectilePoolKey)
        {
            ReportError(
                $"El poolKey de {path} debe ser {PrefabAssetConventions.ProjectilePoolKey}.",
                ref isValid);
        }

        ValidateRenderer(prefab, path, ref isValid);
        TrailRenderer trail = prefab.GetComponent<TrailRenderer>();
        if (trail != null && trail.sharedMaterial == null)
        {
            ReportError($"El TrailRenderer de {path} no tiene material compartido.", ref isValid);
        }
    }

    private static void ValidateRunnerPrefab(
        GameObject prefab,
        string path,
        GameObject expectedEnergyPrefab,
        ref bool isValid)
    {
        Runner runner = ValidateEnemyPrefab<Runner>(
            prefab,
            path,
            expectedEnergyPrefab,
            ref isValid);
        if (runner == null)
        {
            return;
        }
    }

    private static void ValidateArtilleryPrefab(
        GameObject prefab,
        string path,
        GameObject expectedEnergyPrefab,
        GameObject expectedProjectilePrefab,
        ref bool isValid)
    {
        Artillery artillery = ValidateEnemyPrefab<Artillery>(
            prefab,
            path,
            expectedEnergyPrefab,
            ref isValid);
        if (artillery == null)
        {
            return;
        }

        ValidateReference(
            artillery.prefabProyectil,
            expectedProjectilePrefab,
            path,
            "prefabProyectil",
            ref isValid);
        // Artillery puntoDisparo is optional: Artillery falls back to its own
        // transform at runtime, and the committed prefabs keep it null.
    }

    private static void ValidateExplosivePrefab(
        GameObject prefab,
        string path,
        GameObject expectedEnergyPrefab,
        ref bool isValid)
    {
        ValidateEnemyPrefab<Explosive>(prefab, path, expectedEnergyPrefab, ref isValid);
    }

    private static void ValidateWeaverPrefab(
        GameObject prefab,
        string path,
        GameObject expectedEnergyPrefab,
        ref bool isValid)
    {
        ValidateEnemyPrefab<Weaver>(prefab, path, expectedEnergyPrefab, ref isValid);
    }

    private static void ValidateNestPrefab(
        GameObject prefab,
        string path,
        GameObject expectedEnergyPrefab,
        GameObject expectedRunnerPrefab,
        ref bool isValid)
    {
        Nest nest = ValidateEnemyPrefab<Nest>(
            prefab,
            path,
            expectedEnergyPrefab,
            ref isValid);
        if (nest == null)
        {
            return;
        }

        ValidateReference(
            nest.prefabCorredor,
            expectedRunnerPrefab,
            path,
            "prefabCorredor",
            ref isValid);
    }

    private static void ValidateColossusPrefab(
        GameObject prefab,
        string path,
        GameObject expectedEnergyPrefab,
        ref bool isValid)
    {
        ValidateEnemyPrefab<Colossus>(prefab, path, expectedEnergyPrefab, ref isValid);
    }

    private static void ValidateTurretPrefab(
        GameObject prefab,
        string path,
        GameObject expectedProjectilePrefab,
        ref bool isValid)
    {
        if (prefab == null)
        {
            return;
        }

        Torreta turret = ValidateRequiredComponent<Torreta>(prefab, path, ref isValid);
        ValidateRequiredComponent<BoxCollider>(prefab, path, ref isValid);
        ValidateRequiredComponent<Light>(prefab, path, ref isValid);
        ValidateRenderer(prefab, path, ref isValid);
        if (turret == null)
        {
            return;
        }

        ValidateReference(
            turret.prefabProyectil,
            expectedProjectilePrefab,
            path,
            "prefabProyectil",
            ref isValid);
        ValidateTransformReference(turret.puntoDisparo, path, "puntoDisparo", ref isValid);
        if (turret.puntoDisparo != null
            && turret.puntoDisparo.name != PrefabAssetConventions.TurretMuzzleName)
        {
            ReportError(
                $"La referencia puntoDisparo de {path} debe apuntar a {PrefabAssetConventions.TurretMuzzleName}.",
                ref isValid);
        }
    }

    private static TEnemy ValidateEnemyPrefab<TEnemy>(
        GameObject prefab,
        string path,
        GameObject expectedEnergyPrefab,
        ref bool isValid)
        where TEnemy : Enemy
    {
        if (prefab == null)
        {
            return null;
        }

        TEnemy enemy = ValidateRequiredComponent<TEnemy>(prefab, path, ref isValid);
        ValidateRequiredComponent<Collider>(prefab, path, ref isValid);
        ValidateRequiredComponent<Rigidbody>(prefab, path, ref isValid);
        ValidateRenderer(prefab, path, ref isValid);
        if (enemy == null)
        {
            return null;
        }

        ValidateReference(
            enemy.prefabEnergia,
            expectedEnergyPrefab,
            path,
            "prefabEnergia",
            ref isValid);
        if (enemy.modeloVisual == null)
        {
            ReportError($"La referencia modeloVisual de {path} es nula.", ref isValid);
        }

        return enemy;
    }

    private static TComponent ValidateRequiredComponent<TComponent>(
        GameObject prefab,
        string path,
        ref bool isValid)
        where TComponent : Component
    {
        TComponent component = prefab.GetComponent<TComponent>();
        if (component == null)
        {
            ReportError(
                $"El prefab {path} no tiene el componente {typeof(TComponent).Name}.",
                ref isValid);
        }

        return component;
    }

    private static void ValidateRenderer(
        GameObject prefab,
        string path,
        ref bool isValid)
    {
        Renderer renderer = prefab.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            ReportError($"El prefab {path} no tiene Renderer.", ref isValid);
            return;
        }
        if (renderer.sharedMaterial == null)
        {
            ReportError($"El Renderer de {path} no tiene material compartido.", ref isValid);
        }
    }

    private static void ValidateTransformReference(
        Transform reference,
        string path,
        string fieldName,
        ref bool isValid)
    {
        if (reference == null)
        {
            ReportError($"La referencia {fieldName} de {path} es nula.", ref isValid);
        }
    }

    private static void ValidateReference(
        GameObject reference,
        GameObject expected,
        string path,
        string fieldName,
        ref bool isValid)
    {
        if (reference == null)
        {
            ReportError($"La referencia {fieldName} de {path} es nula.", ref isValid);
            return;
        }
        if (expected == null)
        {
            ReportError(
                $"No se pudo resolver el prefab esperado para {fieldName} de {path}.",
                ref isValid);
            return;
        }
        if (reference == expected)
        {
            return;
        }

        string actualPath = AssetDatabase.GetAssetPath(reference);
        string expectedPath = AssetDatabase.GetAssetPath(expected);
        ReportError(
            $"La referencia {fieldName} de {path} apunta a {actualPath} y se esperaba {expectedPath}.",
            ref isValid);
    }

    private static void ReportError(string message, ref bool isValid)
    {
        isValid = false;
        Debug.LogError($"[GeneratedPrefabValidator] {message}");
    }
}
