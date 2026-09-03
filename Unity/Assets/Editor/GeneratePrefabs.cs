using UnityEngine;
using UnityEditor;
using System;

public class GeneratePrefabs
{
    private const float PickupColliderRadiusMeters = 0.5f;
    private const float PickupScale = 0.5f;
    private const float ProjectileScale = 0.6f;
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
    private const float TurretScaleX = 1.4f;
    private const float TurretScaleY = 2.2f;
    private const float TurretScaleZ = 1.4f;
    private const float TurretEmissionIntensity = 0.6f;
    private const float TurretRangeMeters = 22f;
    private const float TurretFireRateSeconds = 0.9f;
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
    private const float ArtilleryFireRateSeconds = 2f;
    private const float ExplosiveRadiusMeters = 5f;
    private const float WeaverFieldRadiusMeters = 6f;
    private const float NestSpawnIntervalSeconds = 6f;
    private const int NestMaximumConcurrentRunners = 3;
    private const float ColossusShotResistance = 0.8f;
    private static readonly Color ProjectileColor = new Color(1f, 0.5f, 0f);
    private static readonly Color TurretColor = new Color(1f, 0.85f, 0.1f);

    [MenuItem("Tools/Generate Real Prefabs (B1)")]
    public static void Generate()
    {
        Debug.Log("[GeneratePrefabs] Inicio generación prefabs reales...");
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder("Assets/Tests/Prefabs");

        // Crear y guardar prefabs base PRIMERO para que los assets existan y las referencias sean a GUID persistente
        var energiaTemp = CreateEnergiaPrefab();
        var proyectilTemp = CreateProyectilPrefab();
        SavePrefab(energiaTemp, "Assets/Resources/Prefabs/EnergiaPickup.prefab");
        SavePrefab(proyectilTemp, "Assets/Resources/Prefabs/ProyectilBase.prefab");
        SavePrefab(energiaTemp, "Assets/Tests/Prefabs/EnergiaPickup.prefab");
        SavePrefab(proyectilTemp, "Assets/Tests/Prefabs/ProyectilBase.prefab");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEngine.Object.DestroyImmediate(energiaTemp);
        UnityEngine.Object.DestroyImmediate(proyectilTemp);
        // Recargar como assets para que las referencias apunten a GUID y no a objeto temporal (fix prefabProyectil == null)
        var energiaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/EnergiaPickup.prefab");
        var proyectilPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/ProyectilBase.prefab");
        if (energiaPrefab == null || proyectilPrefab == null)
        {
            Debug.LogError("[GeneratePrefabs] No se pudo recargar Energia/Proyectil tras guardar base");
            return;
        }

        // Enemigos (ya con referencia a asset persistente)
        var corredor = CreateEnemigoPrefab("Corredor", Color.red, typeof(Runner), energiaPrefab, new Vector3(1,1,1));
        var artillero = CreateEnemigoPrefab("Artillero", Color.blue, typeof(Artillery), energiaPrefab, new Vector3(1,1,1));
        // Asignar proyectil al artillero (ahora sí GUID válido)
        var artComp = artillero.GetComponent<Artillery>();
        if (artComp != null) artComp.prefabProyectil = proyectilPrefab;

        var explosivo = CreateEnemigoPrefab("Explosivo", Color.yellow, typeof(Explosive), energiaPrefab, new Vector3(1,1,1));
        var tejedor = CreateEnemigoPrefab("Tejedor", Color.magenta, typeof(Weaver), energiaPrefab, new Vector3(1,1,1));
        var nido = CreateEnemigoPrefab("Nido", Color.gray, typeof(Nest), energiaPrefab, new Vector3(2f, 1f, 2f));
        var coloso = CreateEnemigoPrefab("Coloso", ColossusColor, typeof(Colossus), energiaPrefab, new Vector3(2.5f, 3f, 2.5f));

        // Nido necesita prefabCorredor - se asigna tras crear corredor (temporal, se corregirá reasignando asset tras guardar)
        var nidoComp = nido.GetComponent<Nest>();
        if (nidoComp != null) nidoComp.prefabCorredor = corredor;

        // Torreta con referencia a asset proyectil (fix fileID 0)
        var torreta = CreateTorretaPrefab(proyectilPrefab);

        // Guardar enemigos y torreta en dos ubicaciones para compatibilidad (Resources y Tests/Prefabs)
        SavePrefab(corredor, "Assets/Resources/Prefabs/Corredor.prefab");
        SavePrefab(artillero, "Assets/Resources/Prefabs/Artillero.prefab");
        SavePrefab(explosivo, "Assets/Resources/Prefabs/Explosivo.prefab");
        SavePrefab(tejedor, "Assets/Resources/Prefabs/Tejedor.prefab");
        SavePrefab(nido, "Assets/Resources/Prefabs/Nido.prefab");
        SavePrefab(coloso, "Assets/Resources/Prefabs/Coloso.prefab");
        SavePrefab(torreta, "Assets/Resources/Prefabs/Torreta.prefab");

        SavePrefab(corredor, "Assets/Tests/Prefabs/Corredor.prefab");
        SavePrefab(artillero, "Assets/Tests/Prefabs/Artillero.prefab");
        SavePrefab(explosivo, "Assets/Tests/Prefabs/Explosivo.prefab");
        SavePrefab(tejedor, "Assets/Tests/Prefabs/Tejedor.prefab");
        SavePrefab(nido, "Assets/Tests/Prefabs/Nido.prefab");
        SavePrefab(coloso, "Assets/Tests/Prefabs/Coloso.prefab");
        SavePrefab(torreta, "Assets/Tests/Prefabs/Torreta.prefab");

        // Corregir referencia nido->corredor para que apunte a GUID (reabrir asset y reasignar)
        {
            var nidoAssetPath = "Assets/Resources/Prefabs/Nido.prefab";
            var corredorAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Corredor.prefab");
            var nidoRoot = PrefabUtility.LoadPrefabContents(nidoAssetPath);
            var nc = nidoRoot.GetComponent<Nest>();
            if (nc != null && corredorAsset != null) nc.prefabCorredor = corredorAsset;
            PrefabUtility.SaveAsPrefabAsset(nidoRoot, nidoAssetPath);
            PrefabUtility.UnloadPrefabContents(nidoRoot);
            var nidoTestPath = "Assets/Tests/Prefabs/Nido.prefab";
            var nidoRoot2 = PrefabUtility.LoadPrefabContents(nidoTestPath);
            var nc2 = nidoRoot2.GetComponent<Nest>();
            if (nc2 != null) nc2.prefabCorredor = corredorAsset;
            PrefabUtility.SaveAsPrefabAsset(nidoRoot2, nidoTestPath);
            PrefabUtility.UnloadPrefabContents(nidoRoot2);
        }

        // Limpiar temporales
        UnityEngine.Object.DestroyImmediate(corredor);
        UnityEngine.Object.DestroyImmediate(artillero);
        UnityEngine.Object.DestroyImmediate(explosivo);
        UnityEngine.Object.DestroyImmediate(tejedor);
        UnityEngine.Object.DestroyImmediate(nido);
        UnityEngine.Object.DestroyImmediate(coloso);
        UnityEngine.Object.DestroyImmediate(torreta);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GeneratePrefabs] Prefabs reales generados en Assets/Resources/Prefabs y Assets/Tests/Prefabs (9 prefabs)");
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parent = System.IO.Path.GetDirectoryName(path);
            var name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static void SavePrefab(GameObject go, string path)
    {
        // Si ya existe, borrar
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log($"[GeneratePrefabs] Guardado {path}");
    }

    static GameObject CreateEnergiaPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "EnergiaPickup";
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        var sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = PickupColliderRadiusMeters;
        go.transform.localScale = Vector3.one * PickupScale;
        var rend = go.GetComponent<Renderer>();
        // Material cyan
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.cyan);
        else mat.color = Color.cyan;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.cyan);
        rend.material = mat;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<EnergyPickup>();
        var pooled = go.AddComponent<PooledObject>();
        pooled.poolKey = "EnergyPickup";
        return go;
    }

    static GameObject CreateProyectilPrefab()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ProyectilBase";
        go.transform.localScale = Vector3.one * ProjectileScale;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = PickupColliderRadiusMeters;
        var rend = go.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color color = ProjectileColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * ProjectileEmissionIntensity);
        mat.EnableKeyword("_EMISSION");
        rend.material = mat;
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = ProjectileTrailDurationSeconds;
        trail.startWidth = ProjectileTrailStartWidthMeters;
        trail.endWidth = ProjectileTrailEndWidthMeters;
        trail.material = mat;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, ProjectileTrailAlpha);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = ProjectileLightRangeMeters;
        light.intensity = ProjectileLightIntensity;
        var projectile = go.AddComponent<Projectile>();
        projectile.daño = ProjectileDamage;
        projectile.tiempoVida = ProjectileLifetimeSeconds;
        var pooled = go.AddComponent<PooledObject>();
        pooled.poolKey = "Proyectil";
        return go;
    }

    static GameObject CreateEnemigoPrefab(string nombre, Color color, Type tipoScript, GameObject prefabEnergia, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.localScale = scale;
        var rend = go.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        rend.material = mat;
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = false;
        go.AddComponent(tipoScript);
        var enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.prefabEnergia = prefabEnergia;
            if (enemy.modeloVisual == null) enemy.modeloVisual = go.transform;
        }
        // Config específica por tipo (igual que TestSceneSetup)
        if (tipoScript == typeof(Runner))
        {
            var r = go.GetComponent<Runner>();
            r.velocidadMovimiento = RunnerMovementSpeedMetersPerSecond;
        }
        else if (tipoScript == typeof(Artillery))
        {
            var a = go.GetComponent<Artillery>();
            a.rangoDisparo = ArtilleryRangeMeters;
            a.velocidadProyectil = ArtilleryProjectileSpeedMetersPerSecond;
            a.cadenciaDisparo = ArtilleryFireRateSeconds;
        }
        else if (tipoScript == typeof(Explosive))
        {
            var e = go.GetComponent<Explosive>();
            e.radioExplosion = ExplosiveRadiusMeters;
        }
        else if (tipoScript == typeof(Weaver))
        {
            var w = go.GetComponent<Weaver>();
            w.radioCampo = WeaverFieldRadiusMeters;
        }
        else if (tipoScript == typeof(Nest))
        {
            var n = go.GetComponent<Nest>();
            n.intervaloGeneracion = NestSpawnIntervalSeconds;
            n.maxCorredoresSimultaneos = NestMaximumConcurrentRunners;
        }
        else if (tipoScript == typeof(Colossus))
        {
            var c = go.GetComponent<Colossus>();
            c.resistenciaDisparos = ColossusShotResistance;
        }

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        // No desactivar para prefab asset
        return go;
    }

    static GameObject CreateTorretaPrefab(GameObject proyectilPrefab)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Torreta";
        go.transform.localScale = new Vector3(TurretScaleX, TurretScaleY, TurretScaleZ);
        var rend = go.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color color = TurretColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * TurretEmissionIntensity);
        mat.EnableKeyword("_EMISSION");
        rend.material = mat;
        // Mantener collider para que sea dañable (antes se destruía y no recibía daño)
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.center = Vector3.zero;
        box.size = Vector3.one;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = TurretLightRangeMeters;
        light.intensity = TurretLightIntensity;
        var torreta = go.AddComponent<Torreta>();
        torreta.rango = TurretRangeMeters;
        torreta.cadencia = TurretFireRateSeconds;
        torreta.daño = TurretDamage;
        torreta.velocidadProyectil = TurretProjectileSpeed;
        torreta.vidaMaxima = TurretMaximumHealth;
        torreta.vidaActual = TurretMaximumHealth;
        torreta.municionMaxima = TurretMaximumAmmo;
        torreta.municionActual = TurretMaximumAmmo;
        torreta.tiempoRecarga = TurretReloadSeconds;
        var pd = new GameObject("PuntoDisparo");
        pd.transform.SetParent(go.transform);
        pd.transform.localPosition = Vector3.forward * ProjectileSpawnForwardMeters + Vector3.up * ProjectileSpawnUpMeters;
        pd.transform.localRotation = Quaternion.identity;
        pd.transform.localScale = Vector3.one; // fix: antes 0.714/0.454 por herencia de escala padre
        torreta.puntoDisparo = pd.transform;
        torreta.prefabProyectil = proyectilPrefab;
        return go;
    }
}
