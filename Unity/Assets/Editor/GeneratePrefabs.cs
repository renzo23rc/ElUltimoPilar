using UnityEngine;
using UnityEditor;
using System;

public class GeneratePrefabs
{
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
        var nido = CreateEnemigoPrefab("Nido", Color.gray, typeof(Nest), energiaPrefab, new Vector3(2,1,2));
        var coloso = CreateEnemigoPrefab("Coloso", new Color(0.5f,0,0), typeof(Colossus), energiaPrefab, new Vector3(2.5f,3f,2.5f));

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
        sc.radius = 0.5f;
        go.transform.localScale = Vector3.one * 0.5f;
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
        go.transform.localScale = Vector3.one * 0.6f;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f;
        var rend = go.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color c = new Color(1f, 0.5f, 0f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else mat.color = c;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * 1.2f);
        mat.EnableKeyword("_EMISSION");
        rend.material = mat;
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = 0.25f;
        trail.endWidth = 0.05f;
        trail.material = mat;
        trail.startColor = c;
        trail.endColor = new Color(1, 0.5f, 0, 0.2f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = c;
        light.range = 4f;
        light.intensity = 2f;
        var proj = go.AddComponent<Projectile>();
        proj.daño = 10f;
        proj.tiempoVida = 5f;
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
            r.velocidadMovimiento = 3.5f;
        }
        else if (tipoScript == typeof(Artillery))
        {
            var a = go.GetComponent<Artillery>();
            a.rangoDisparo = 20f;
            a.velocidadProyectil = 15f;
            a.cadenciaDisparo = 2f;
        }
        else if (tipoScript == typeof(Explosive))
        {
            var e = go.GetComponent<Explosive>();
            e.radioExplosion = 5f;
        }
        else if (tipoScript == typeof(Weaver))
        {
            var w = go.GetComponent<Weaver>();
            w.radioCampo = 6f;
        }
        else if (tipoScript == typeof(Nest))
        {
            var n = go.GetComponent<Nest>();
            n.intervaloGeneracion = 6f;
            n.maxCorredoresSimultaneos = 3;
        }
        else if (tipoScript == typeof(Colossus))
        {
            var c = go.GetComponent<Colossus>();
            c.resistenciaDisparos = 0.8f;
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
        go.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
        var rend = go.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color col = new Color(1f, 0.85f, 0.1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        else mat.color = col;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", col * 0.6f);
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
        light.color = col;
        light.range = 6f;
        light.intensity = 2f;
        var torreta = go.AddComponent<Torreta>();
        torreta.rango = 22f;
        torreta.cadencia = 0.9f;
        torreta.daño = 6f;
        torreta.velocidadProyectil = 28f;
        torreta.vidaMaxima = 120f;
        torreta.vidaActual = 120f;
        torreta.municionMaxima = 15;
        torreta.municionActual = 15;
        torreta.tiempoRecarga = 10f;
        var pd = new GameObject("PuntoDisparo");
        pd.transform.SetParent(go.transform);
        pd.transform.localPosition = Vector3.forward * 0.8f + Vector3.up * 0.6f;
        pd.transform.localRotation = Quaternion.identity;
        pd.transform.localScale = Vector3.one; // fix: antes 0.714/0.454 por herencia de escala padre
        torreta.puntoDisparo = pd.transform;
        torreta.prefabProyectil = proyectilPrefab;
        return go;
    }
}
