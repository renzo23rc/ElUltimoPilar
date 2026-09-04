using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides the Unity Editor command that creates project prefabs.
/// </summary>
public class GeneratePrefabs
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string TestsFolder = "Assets/Tests";
    private const string ResourcesPrefabFolder = ResourcesFolder + "/Prefabs";
    private const string TestsPrefabFolder = TestsFolder + "/Prefabs";
    private const string PrefabExtension = ".prefab";
    private const string MenuItemPath = "Tools/Generate Real Prefabs (B1)";
    private const string LitShaderName = "Universal Render Pipeline/Lit";
    private const string StandardShaderName = "Standard";
    private const string SpriteShaderName = "Sprites/Default";
    private const string BaseColorPropertyName = "_BaseColor";
    private const string ColorPropertyName = "_Color";
    private const string EmissionColorPropertyName = "_EmissionColor";
    private const string EmissionKeyword = "_EMISSION";
    private const string EnergyPickupPoolKey = "EnergyPickup";
    private const string ProjectilePoolKey = "Proyectil";
    private const string EnergyPickupPrefabName = "EnergiaPickup";
    private const string ProjectilePrefabName = "ProyectilBase";
    private const string RunnerPrefabName = "Corredor";
    private const string ArtilleryPrefabName = "Artillero";
    private const string ExplosivePrefabName = "Explosivo";
    private const string WeaverPrefabName = "Tejedor";
    private const string NestPrefabName = "Nido";
    private const string ColossusPrefabName = "Coloso";
    private const string TurretPrefabName = "Torreta";
    private const string TurretMuzzleName = "PuntoDisparo";

    private const float NoEmissionIntensity = 0f;
    private const float PickupColliderRadiusMeters = 0.5f;
    private const float EnergyPickupScale = 0.5f;
    private const float ProjectileScale = 0.6f;
    private const float ProjectileColliderRadiusMeters = 0.5f;
    private const float ProjectileTrailDurationSeconds = 0.4f;
    private const float ProjectileTrailStartWidthMeters = 0.25f;
    private const float ProjectileTrailEndWidthMeters = 0.05f;
    private const float ProjectileTrailAlpha = 0.2f;
    private const float ProjectileLightRangeMeters = 4f;
    private const float ProjectileLightIntensity = 2f;
    private const float ProjectileEmissionIntensity = 1.2f;
    private const float ProjectileDamage = 10f;
    private const float ProjectileLifetimeSeconds = 5f;
    private const float TurretLightRangeMeters = 6f;
    private const float TurretLightIntensity = 2f;
    private const float TurretEmissionIntensity = 0.6f;
    private const float TurretRangeMeters = 22f;
    private const float TurretFireIntervalSeconds = 0.9f;
    private const float TurretDamage = 6f;
    private const float TurretProjectileSpeed = 28f;
    private const float TurretMaximumHealth = 120f;
    private const int TurretMaximumAmmo = 15;
    private const float TurretReloadSeconds = 10f;
    private const float ProjectileSpawnForwardMeters = 0.8f;
    private const float ProjectileSpawnUpMeters = 0.6f;
    private const float RunnerMovementSpeedMetersPerSecond = 3.5f;
    private const float ArtilleryRangeMeters = 20f;
    private const float ArtilleryProjectileSpeedMetersPerSecond = 15f;
    private const float ArtilleryFireIntervalSeconds = 2f;
    private const float ExplosiveRadiusMeters = 5f;
    private const float WeaverFieldRadiusMeters = 6f;
    private const float NestSpawnIntervalSeconds = 6f;
    private const int NestMaximumConcurrentRunners = 3;
    private const float ColossusShotResistance = 0.8f;

    private static readonly Color ProjectileColor = new Color(1f, 0.5f, 0f);
    private static readonly Color TurretColor = new Color(1f, 0.85f, 0.1f);
    private static readonly Color ColossusColor = new Color(0.5f, 0f, 0f);
    private static readonly Vector3 NestScale = new Vector3(2f, 1f, 2f);
    private static readonly Vector3 ColossusScale = new Vector3(2.5f, 3f, 2.5f);
    private static readonly Vector3 TurretScale = new Vector3(1.4f, 2.2f, 1.4f);

    /// <summary>
    /// Generates the project prefabs and persists their Unity asset references.
    /// </summary>
    [MenuItem(MenuItemPath)]
    public static void Generate()
    {
        List<GameObject> temporaryObjects = new List<GameObject>();

        try
        {
            Debug.Log("[GeneratePrefabs] Inicio generación prefabs reales...");
            EnsureFolders();

            GameObject energyTemporary = CreateEnergyPrefab(temporaryObjects);
            GameObject projectileTemporary = CreateProjectilePrefab(temporaryObjects);

            SaveBasePrefabs(energyTemporary, projectileTemporary);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject energyPrefab = LoadPrefabAsset(
                GetPrefabPath(ResourcesPrefabFolder, EnergyPickupPrefabName));
            GameObject projectilePrefab = LoadPrefabAsset(
                GetPrefabPath(ResourcesPrefabFolder, ProjectilePrefabName));

            CreateAndSaveEnemyPrefabs(energyPrefab, projectilePrefab, temporaryObjects);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[GeneratePrefabs] Prefabs reales generados en {ResourcesPrefabFolder} y "
                + $"{TestsPrefabFolder}.");
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError($"[GeneratePrefabs] {exception.Message}");
        }
        finally
        {
            DestroyTemporaryObjects(temporaryObjects);
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(ResourcesPrefabFolder);
        EnsureFolder(TestsFolder);
        EnsureFolder(TestsPrefabFolder);
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

    private static void SaveBasePrefabs(
        GameObject energyPrefab,
        GameObject projectilePrefab)
    {
        SavePrefabToBothLocations(energyPrefab, EnergyPickupPrefabName);
        SavePrefabToBothLocations(projectilePrefab, ProjectilePrefabName);
    }

    private static void CreateAndSaveEnemyPrefabs(
        GameObject energyPrefab,
        GameObject projectilePrefab,
        List<GameObject> temporaryObjects)
    {
        GameObject runner = CreateEnemyPrefab<Runner>(
            temporaryObjects,
            RunnerPrefabName,
            Color.red,
            energyPrefab,
            Vector3.one,
            ConfigureRunner);

        GameObject artillery = CreateEnemyPrefab<Artillery>(
            temporaryObjects,
            ArtilleryPrefabName,
            Color.blue,
            energyPrefab,
            Vector3.one,
            ConfigureArtillery);
        GetRequiredComponent<Artillery>(artillery).prefabProyectil = projectilePrefab;

        GameObject explosive = CreateEnemyPrefab<Explosive>(
            temporaryObjects,
            ExplosivePrefabName,
            Color.yellow,
            energyPrefab,
            Vector3.one,
            ConfigureExplosive);

        GameObject weaver = CreateEnemyPrefab<Weaver>(
            temporaryObjects,
            WeaverPrefabName,
            Color.magenta,
            energyPrefab,
            Vector3.one,
            ConfigureWeaver);

        GameObject nest = CreateEnemyPrefab<Nest>(
            temporaryObjects,
            NestPrefabName,
            Color.gray,
            energyPrefab,
            NestScale,
            ConfigureNest);
        GetRequiredComponent<Nest>(nest).prefabCorredor = runner;

        GameObject colossus = CreateEnemyPrefab<Colossus>(
            temporaryObjects,
            ColossusPrefabName,
            ColossusColor,
            energyPrefab,
            ColossusScale,
            ConfigureColossus);

        GameObject turret = CreateTurretPrefab(temporaryObjects, projectilePrefab);

        SavePrefabToBothLocations(runner, RunnerPrefabName);
        SavePrefabToBothLocations(artillery, ArtilleryPrefabName);
        SavePrefabToBothLocations(explosive, ExplosivePrefabName);
        SavePrefabToBothLocations(weaver, WeaverPrefabName);
        SavePrefabToBothLocations(nest, NestPrefabName);
        SavePrefabToBothLocations(colossus, ColossusPrefabName);
        SavePrefabToBothLocations(turret, TurretPrefabName);
        RepairNestReferences();
    }

    private static GameObject LoadPrefabAsset(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            throw new InvalidOperationException($"No se pudo cargar el prefab: {path}");
        }

        return prefab;
    }

    private static void SavePrefabToBothLocations(GameObject prefab, string prefabName)
    {
        SavePrefab(prefab, GetPrefabPath(ResourcesPrefabFolder, prefabName));
        SavePrefab(prefab, GetPrefabPath(TestsPrefabFolder, prefabName));
    }

    private static string GetPrefabPath(string folder, string prefabName)
    {
        return folder + "/" + prefabName + PrefabExtension;
    }

    private static void SavePrefab(GameObject gameObject, string path)
    {
        if (gameObject == null)
        {
            throw new ArgumentNullException(nameof(gameObject));
        }
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("La ruta del prefab no puede estar vacía.", nameof(path));
        }

        UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (existingAsset != null && !AssetDatabase.DeleteAsset(path))
        {
            throw new InvalidOperationException($"No se pudo reemplazar el prefab: {path}");
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
        if (savedPrefab == null)
        {
            throw new InvalidOperationException($"No se pudo guardar el prefab: {path}");
        }

        Debug.Log($"[GeneratePrefabs] Guardado {path}");
    }

    private static GameObject CreateTemporaryPrimitive(
        List<GameObject> temporaryObjects,
        PrimitiveType primitiveType,
        string name)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
        if (gameObject == null)
        {
            throw new InvalidOperationException($"No se pudo crear el objeto temporal: {name}");
        }

        gameObject.name = name;
        temporaryObjects.Add(gameObject);
        return gameObject;
    }

    private static GameObject CreateEnergyPrefab(List<GameObject> temporaryObjects)
    {
        GameObject gameObject = CreateTemporaryPrimitive(
            temporaryObjects,
            PrimitiveType.Sphere,
            EnergyPickupPrefabName);

        SphereCollider collider = GetRequiredComponent<SphereCollider>(gameObject);
        ConfigureTriggerCollider(collider, PickupColliderRadiusMeters);
        gameObject.transform.localScale = Vector3.one * EnergyPickupScale;
        ApplyMaterial(
            GetRequiredComponent<Renderer>(gameObject),
            Color.cyan,
            NoEmissionIntensity,
            false);
        ConfigureKinematicPhysics(gameObject);
        gameObject.AddComponent<EnergyPickup>();
        PooledObject pooledObject = gameObject.AddComponent<PooledObject>();
        pooledObject.poolKey = EnergyPickupPoolKey;
        return gameObject;
    }

    private static GameObject CreateProjectilePrefab(List<GameObject> temporaryObjects)
    {
        GameObject gameObject = CreateTemporaryPrimitive(
            temporaryObjects,
            PrimitiveType.Sphere,
            ProjectilePrefabName);

        gameObject.transform.localScale = Vector3.one * ProjectileScale;
        SphereCollider collider = GetRequiredComponent<SphereCollider>(gameObject);
        ConfigureTriggerCollider(collider, ProjectileColliderRadiusMeters);
        Material material = ApplyMaterial(
            GetRequiredComponent<Renderer>(gameObject),
            ProjectileColor,
            ProjectileEmissionIntensity,
            true);
        Rigidbody rigidbody = ConfigureProjectilePhysics(gameObject);
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = ProjectileTrailDurationSeconds;
        trail.startWidth = ProjectileTrailStartWidthMeters;
        trail.endWidth = ProjectileTrailEndWidthMeters;
        trail.sharedMaterial = material;
        trail.startColor = ProjectileColor;
        trail.endColor = new Color(
            ProjectileColor.r,
            ProjectileColor.g,
            ProjectileColor.b,
            ProjectileTrailAlpha);
        Light light = gameObject.AddComponent<Light>();
        ConfigureLight(light, ProjectileColor, ProjectileLightRangeMeters, ProjectileLightIntensity);
        Projectile projectile = gameObject.AddComponent<Projectile>();
        projectile.daño = ProjectileDamage;
        projectile.tiempoVida = ProjectileLifetimeSeconds;
        PooledObject pooledObject = gameObject.AddComponent<PooledObject>();
        pooledObject.poolKey = ProjectilePoolKey;
        return gameObject;
    }

    private static GameObject CreateEnemyPrefab<TEnemy>(
        List<GameObject> temporaryObjects,
        string name,
        Color color,
        GameObject energyPrefab,
        Vector3 scale,
        Action<TEnemy> configureEnemy)
        where TEnemy : Enemy
    {
        if (energyPrefab == null)
        {
            throw new ArgumentNullException(nameof(energyPrefab));
        }
        if (configureEnemy == null)
        {
            throw new ArgumentNullException(nameof(configureEnemy));
        }

        GameObject gameObject = CreateTemporaryPrimitive(
            temporaryObjects,
            PrimitiveType.Cube,
            name);
        gameObject.transform.localScale = scale;
        ApplyMaterial(
            GetRequiredComponent<Renderer>(gameObject),
            color,
            NoEmissionIntensity,
            false);
        ConfigureCollider(GetRequiredComponent<BoxCollider>(gameObject), false);
        TEnemy enemyComponent = gameObject.AddComponent<TEnemy>();
        enemyComponent.prefabEnergia = energyPrefab;
        if (enemyComponent.modeloVisual == null)
        {
            enemyComponent.modeloVisual = gameObject.transform;
        }

        configureEnemy(enemyComponent);
        ConfigureEnemyPhysics(gameObject);
        return gameObject;
    }

    private static void ConfigureRunner(Runner runner)
    {
        runner.velocidadMovimiento = RunnerMovementSpeedMetersPerSecond;
    }

    private static void ConfigureArtillery(Artillery artillery)
    {
        artillery.rangoDisparo = ArtilleryRangeMeters;
        artillery.velocidadProyectil = ArtilleryProjectileSpeedMetersPerSecond;
        artillery.cadenciaDisparo = ArtilleryFireIntervalSeconds;
    }

    private static void ConfigureExplosive(Explosive explosive)
    {
        explosive.radioExplosion = ExplosiveRadiusMeters;
    }

    private static void ConfigureWeaver(Weaver weaver)
    {
        weaver.radioCampo = WeaverFieldRadiusMeters;
    }

    private static void ConfigureNest(Nest nest)
    {
        nest.intervaloGeneracion = NestSpawnIntervalSeconds;
        nest.maxCorredoresSimultaneos = NestMaximumConcurrentRunners;
    }

    private static void ConfigureColossus(Colossus colossus)
    {
        colossus.resistenciaDisparos = ColossusShotResistance;
    }

    private static GameObject CreateTurretPrefab(
        List<GameObject> temporaryObjects,
        GameObject projectilePrefab)
    {
        if (projectilePrefab == null)
        {
            throw new ArgumentNullException(nameof(projectilePrefab));
        }

        GameObject gameObject = CreateTemporaryPrimitive(
            temporaryObjects,
            PrimitiveType.Cube,
            TurretPrefabName);
        gameObject.transform.localScale = TurretScale;
        ApplyMaterial(
            GetRequiredComponent<Renderer>(gameObject),
            TurretColor,
            TurretEmissionIntensity,
            true);
        BoxCollider collider = GetRequiredComponent<BoxCollider>(gameObject);
        ConfigureCollider(collider, false);
        collider.center = Vector3.zero;
        collider.size = Vector3.one;
        Light light = gameObject.AddComponent<Light>();
        ConfigureLight(light, TurretColor, TurretLightRangeMeters, TurretLightIntensity);
        Torreta turret = gameObject.AddComponent<Torreta>();
        turret.rango = TurretRangeMeters;
        turret.cadencia = TurretFireIntervalSeconds;
        turret.daño = TurretDamage;
        turret.velocidadProyectil = TurretProjectileSpeed;
        turret.vidaMaxima = TurretMaximumHealth;
        turret.vidaActual = TurretMaximumHealth;
        turret.municionMaxima = TurretMaximumAmmo;
        turret.municionActual = TurretMaximumAmmo;
        turret.tiempoRecarga = TurretReloadSeconds;
        GameObject muzzle = new GameObject(TurretMuzzleName);
        muzzle.transform.SetParent(gameObject.transform);
        muzzle.transform.localPosition = Vector3.forward * ProjectileSpawnForwardMeters
            + Vector3.up * ProjectileSpawnUpMeters;
        muzzle.transform.localRotation = Quaternion.identity;
        muzzle.transform.localScale = Vector3.one;
        turret.puntoDisparo = muzzle.transform;
        turret.prefabProyectil = projectilePrefab;
        return gameObject;
    }

    private static Material ApplyMaterial(
        Renderer renderer,
        Color color,
        float emissionIntensity,
        bool enableEmission)
    {
        if (renderer == null)
        {
            throw new ArgumentNullException(nameof(renderer));
        }

        Shader shader = FindCompatibleShader();
        Material material = new Material(shader);
        bool colorConfigured = false;
        if (material.HasProperty(BaseColorPropertyName))
        {
            material.SetColor(BaseColorPropertyName, color);
            colorConfigured = true;
        }
        if (material.HasProperty(ColorPropertyName))
        {
            material.SetColor(ColorPropertyName, color);
            colorConfigured = true;
        }
        if (!colorConfigured)
        {
            UnityEngine.Object.DestroyImmediate(material);
            throw new InvalidOperationException("El shader no expone una propiedad de color compatible.");
        }
        if (enableEmission && material.HasProperty(EmissionColorPropertyName))
        {
            material.SetColor(EmissionColorPropertyName, color * emissionIntensity);
            material.EnableKeyword(EmissionKeyword);
        }

        renderer.sharedMaterial = material;
        return material;
    }

    private static Shader FindCompatibleShader()
    {
        Shader shader = Shader.Find(LitShaderName);
        if (shader == null)
        {
            shader = Shader.Find(StandardShaderName);
        }
        if (shader == null)
        {
            shader = Shader.Find(SpriteShaderName);
        }
        if (shader == null)
        {
            throw new InvalidOperationException("No se encontró un shader compatible para los prefabs.");
        }

        return shader;
    }

    private static void ConfigureCollider(Collider collider, bool isTrigger)
    {
        if (collider == null)
        {
            throw new InvalidOperationException("El prefab generado no tiene collider.");
        }

        collider.isTrigger = isTrigger;
    }

    private static void ConfigureTriggerCollider(SphereCollider collider, float radius)
    {
        ConfigureCollider(collider, true);
        collider.radius = radius;
    }

    private static Rigidbody ConfigureEnemyPhysics(GameObject gameObject)
    {
        Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.useGravity = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;
        return rigidbody;
    }

    private static Rigidbody ConfigureProjectilePhysics(GameObject gameObject)
    {
        Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        return rigidbody;
    }

    private static void ConfigureKinematicPhysics(GameObject gameObject)
    {
        Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    private static void ConfigureLight(Light light, Color color, float range, float intensity)
    {
        if (light == null)
        {
            throw new ArgumentNullException(nameof(light));
        }

        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
    }

    private static void RepairNestReferences()
    {
        GameObject runnerAsset = LoadPrefabAsset(
            GetPrefabPath(ResourcesPrefabFolder, RunnerPrefabName));
        RepairNestReference(
            GetPrefabPath(ResourcesPrefabFolder, NestPrefabName),
            runnerAsset);
        RepairNestReference(
            GetPrefabPath(TestsPrefabFolder, NestPrefabName),
            runnerAsset);
    }

    private static void RepairNestReference(string nestPath, GameObject runnerAsset)
    {
        GameObject nestRoot = null;
        try
        {
            nestRoot = PrefabUtility.LoadPrefabContents(nestPath);
            if (nestRoot == null)
            {
                throw new InvalidOperationException($"No se pudo cargar el prefab: {nestPath}");
            }

            Nest nest = GetRequiredComponent<Nest>(nestRoot);
            if (runnerAsset == null)
            {
                throw new InvalidOperationException(
                    $"No se encontró el prefab de corredor para: {nestPath}");
            }

            nest.prefabCorredor = runnerAsset;
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(nestRoot, nestPath);
            if (savedPrefab == null)
            {
                throw new InvalidOperationException(
                    $"No se pudo guardar el prefab reparado: {nestPath}");
            }
        }
        finally
        {
            if (nestRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(nestRoot);
            }
        }
    }

    private static TComponent GetRequiredComponent<TComponent>(GameObject gameObject)
        where TComponent : Component
    {
        if (gameObject == null)
        {
            throw new ArgumentNullException(nameof(gameObject));
        }

        TComponent component = gameObject.GetComponent<TComponent>();
        if (component == null)
        {
            throw new InvalidOperationException(
                $"El objeto {gameObject.name} no tiene {typeof(TComponent).Name}.");
        }

        return component;
    }

    private static void DestroyTemporaryObjects(List<GameObject> temporaryObjects)
    {
        HashSet<Material> materials = new HashSet<Material>();
        foreach (GameObject temporaryObject in temporaryObjects)
        {
            if (temporaryObject == null)
            {
                continue;
            }

            Renderer[] renderers = temporaryObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterial != null)
                {
                    materials.Add(renderer.sharedMaterial);
                }
            }
        }

        foreach (GameObject temporaryObject in temporaryObjects)
        {
            if (temporaryObject != null)
            {
                UnityEngine.Object.DestroyImmediate(temporaryObject);
            }
        }

        foreach (Material material in materials)
        {
            if (material != null && !AssetDatabase.Contains(material))
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
