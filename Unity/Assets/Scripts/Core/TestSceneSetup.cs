/**
 * TestSceneSetup.cs
 * Script de utilidad para armar rápidamente una escena de prueba.
 * 
 * INSTRUCCIONES:
 * 1. Crear una escena vacía (File > New Scene)
 * 2. Guardarla como Assets/Tests/Scenes/TestEnvironment.unity
 * 3. Crear un GameObject vacío llamado "Setup"
 * 4. Agregar este script al GameObject
 * 5. Apretar Play
 * 
 * Este script generará automáticamente:
 * - Pilar central (cilindro con material)
 * - Jugador (capsule con cámara)
 * - Suelo circular (plano)
 * - Spawner con puntos de spawn
 * - GameManager
 * - Y configurará todo para empezar a testear
 */
using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class TestSceneSetup : MonoBehaviour
{
    [Header("Configuración Rápida")]
    public bool generarAlIniciar = true;
    public bool destruirDespuésDeGenerar = true;
    
    [Header("Materiales de Prueba")]
    public Material matPilar;
    public Material matSuelo;
    public Material matEnemigo;
    public Material matJugador;
    public Material matEnergia;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActionAsset;

    void Start()
    {
        if (generarAlIniciar)
        {
            GenerarEscenaDePrueba();
        }
    }

    [ContextMenu("Generar Escena de Prueba")]
    public void GenerarEscenaDePrueba()
    {
        Debug.Log("[TestSceneSetup] Generando escena de prueba...");
        
        // 1. GameManager
        GameObject gm = new GameObject("GameManager");
        var gameManager = gm.AddComponent<GameManager>();
        
        // 2. Pilar
        GameObject pilarGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pilarGO.name = "Pilar";
        pilarGO.transform.position = new Vector3(0, 2f, 0);
        pilarGO.transform.localScale = new Vector3(4f, 2f, 4f);
        var pilar = pilarGO.AddComponent<Pilar>();
        if (matPilar != null) pilarGO.GetComponent<Renderer>().material = matPilar;
        
        // Puntos para torretas (fase 4) - FUERA del pozo (radio 5) => local 1.6 => mundo 6.4, y -0.45 => mundo 1.1 visible por encima del pozo
        Transform[] torretas = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject t = new GameObject($"PuntoTorreta_{i}");
            t.transform.SetParent(pilarGO.transform);
            float angulo = (i / 4f) * Mathf.PI * 2f;
            t.transform.localPosition = new Vector3(Mathf.Cos(angulo) * 1.6f, -0.45f, Mathf.Sin(angulo) * 1.6f);
            torretas[i] = t.transform;
        }
        pilar.puntosTorretas = torretas;
        // Prefab torreta (Fase 4) - intenta cargar prefab real, fallback runtime si no existe
        GameObject prefabTorreta = Resources.Load<GameObject>("Prefabs/Torreta");
        if (prefabTorreta == null)
        {
            prefabTorreta = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefabTorreta.name = "TorretaPrefab";
            prefabTorreta.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
            var rendTorreta = prefabTorreta.GetComponent<Renderer>();
            Shader sTorreta = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var matTorreta = new Material(sTorreta);
            Color colTorreta = new Color(1f, 0.85f, 0.1f);
            if (matTorreta.HasProperty("_BaseColor")) matTorreta.SetColor("_BaseColor", colTorreta);
            else matTorreta.color = colTorreta;
            if (matTorreta.HasProperty("_Color")) matTorreta.SetColor("_Color", colTorreta);
            if (matTorreta.HasProperty("_EmissionColor")) matTorreta.SetColor("_EmissionColor", colTorreta * 0.6f);
            matTorreta.EnableKeyword("_EMISSION");
            rendTorreta.material = matTorreta;
            var boxTorreta = prefabTorreta.GetComponent<BoxCollider>();
            if (boxTorreta == null) boxTorreta = prefabTorreta.AddComponent<BoxCollider>();
            boxTorreta.isTrigger = false;
            boxTorreta.center = Vector3.zero;
            boxTorreta.size = Vector3.one;
            var lightTorreta = prefabTorreta.AddComponent<Light>();
            lightTorreta.type = LightType.Point;
            lightTorreta.color = colTorreta;
            lightTorreta.range = 6f;
            lightTorreta.intensity = 2f;
            var torreta = prefabTorreta.AddComponent<Torreta>();
            torreta.rango = 22f;
            torreta.cadencia = 0.9f;
            torreta.daño = 6f;
            torreta.velocidadProyectil = 28f;
            torreta.vidaMaxima = 120f;
            torreta.vidaActual = 120f;
            torreta.municionMaxima = 15;
            torreta.municionActual = 15;
            torreta.tiempoRecarga = 10f;
            var pdTorreta = new GameObject("PuntoDisparo");
            pdTorreta.transform.SetParent(prefabTorreta.transform);
            pdTorreta.transform.localPosition = Vector3.forward * 0.8f + Vector3.up * 0.6f;
            pdTorreta.transform.localRotation = Quaternion.identity;
            pdTorreta.transform.localScale = Vector3.one;
            torreta.puntoDisparo = pdTorreta.transform;
            prefabTorreta.SetActive(false);
        }
        else
        {
            Debug.Log("[TestSceneSetup] Usando prefab real Torreta desde Resources/Prefabs");
        }
        pilar.prefabTorreta = prefabTorreta;
        
        // 3. Suelo / Arena
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "Arena";
        suelo.transform.position = Vector3.zero;
        suelo.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 unidades
        // Tag removido - no es necesario para el funcionamiento
        if (matSuelo != null) suelo.GetComponent<Renderer>().material = matSuelo;
        
        // 4. Pozo Central (inicialmente desactivado) - con PozoKill funcional - VISIBLE alrededor del Pilar
        GameObject pozo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pozo.name = "PozoCentral";
        // Reemplazar collider por trigger mortal (BoxCollider trigger robusto para caída) - grande para ser visible y tocable
        Destroy(pozo.GetComponent<Collider>());
        var triggerPozo = pozo.AddComponent<BoxCollider>();
        triggerPozo.isTrigger = true;
        triggerPozo.size = new Vector3(1f, 1f, 1f);
        triggerPozo.center = Vector3.zero;
        var pozoKill = pozo.AddComponent<PozoKill>();
        pozoKill.radioMortal = 5f; // Un poco más grande que pilar para ser tocable sin estar debajo
        pozoKill.alturaMortal = 1.5f; // Mata si y <= pozo.y + 1.5 (permite caminar sobre borde sin morir, solo al caer)
        var rbPozo = pozo.GetComponent<Rigidbody>();
        if (rbPozo == null) rbPozo = pozo.AddComponent<Rigidbody>();
        rbPozo.isKinematic = true;
        rbPozo.useGravity = false;
        // Posición visible: anillo alrededor del Pilar, ligeramente hundido pero con borde visible sobre el suelo - hitbox = visual
        pozo.transform.position = new Vector3(0, -0.2f, 0);
        pozo.transform.localScale = new Vector3(10f, 0.5f, 10f); // Radio 5 (0.5*10), coincide con radioMortal 5
        var rendPozo = pozo.GetComponent<Renderer>();
        rendPozo.material.color = Color.black;
        // Añadir borde emissive para visibilidad
        if (rendPozo.material.HasProperty("_EmissionColor")) rendPozo.material.SetColor("_EmissionColor", Color.red * 0.3f);
        pozo.SetActive(false);
        
        // 5. Zona de Gravedad (inicialmente desactivada) - TRIGGER EXAGERADO
        GameObject zonaGrav = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        zonaGrav.name = "ZonaGravedad";
        // Mantener collider como trigger gigante
        var colZona = zonaGrav.GetComponent<SphereCollider>();
        colZona.isTrigger = true;
        colZona.radius = 0.5f;
        zonaGrav.transform.position = new Vector3(8f, 0.5f, 0);
        zonaGrav.transform.localScale = new Vector3(10f, 4f, 10f); // mas grande y achatada (exagerado)
        Shader shaderZona = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var matZona = new Material(shaderZona);
        // Color violeta neón exagerado
        if (matZona.HasProperty("_Color")) matZona.color = new Color(0.6f, 0.1f, 1f, 0.35f);
        else if (matZona.HasProperty("_BaseColor")) matZona.SetColor("_BaseColor", new Color(0.6f, 0.1f, 1f, 0.35f));
        if (matZona.HasProperty("_Mode")) matZona.SetFloat("_Mode", 3);
        if (matZona.HasProperty("_SrcBlend")) matZona.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (matZona.HasProperty("_DstBlend")) matZona.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (matZona.HasProperty("_ZWrite")) matZona.SetInt("_ZWrite", 0);
        matZona.DisableKeyword("_ALPHATEST_ON");
        matZona.EnableKeyword("_ALPHABLEND_ON");
        matZona.renderQueue = 3000;
        var rendZona = zonaGrav.GetComponent<Renderer>();
        if (rendZona != null) rendZona.material = matZona;
        // Script de efecto exagerado
        var zonaScript = zonaGrav.AddComponent<ZonaGravedadEffect>();
        zonaScript.fuerzaAscenso = 18f;
        zonaScript.radioEfecto = 5f;
        // Particulas flotantes visuales
        for (int p=0;p<15;p++)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "ParticulaFlotante";
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(zonaGrav.transform);
            part.transform.localPosition = UnityEngine.Random.insideUnitSphere * 0.4f;
            part.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.08f,0.18f);
            var r = part.GetComponent<Renderer>();
            r.material.color = new Color(0.8f,0.4f,1f,0.9f);
            part.AddComponent<ParticulaFlotante>();
        }
        zonaGrav.SetActive(false);
        
        // 6. Jugador (GameObject vacío + hijo visual para evitar conflictos)
        GameObject jugador = new GameObject("Jugador");
        jugador.SetActive(false);
        jugador.transform.position = new Vector3(0, 1f, -8f);
        
        // Hijo visual (Cube)
        GameObject jugadorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jugadorVisual.name = "JugadorVisual";
        jugadorVisual.transform.SetParent(jugador.transform);
        jugadorVisual.transform.localPosition = Vector3.zero;
        jugadorVisual.transform.localScale = new Vector3(1f, 2f, 1f);
        if (matJugador != null) jugadorVisual.GetComponent<Renderer>().material = matJugador;
        Destroy(jugadorVisual.GetComponent<BoxCollider>()); // No necesitamos el collider del cube
        
        // Cámara hija del jugador
        GameObject cam = new GameObject("Camera");
        cam.transform.SetParent(jugador.transform);
        cam.transform.localPosition = new Vector3(0, 0.8f, 0);
        var camera = cam.AddComponent<Camera>();
        camera.nearClipPlane = 0.1f;
        camera.tag = "Untagged";
        cam.AddComponent<AudioListener>();
        // Desactivar la Main Camera vieja de SampleScene (quedaba en 0,1,-10 fija)
        var oldCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var oc in oldCams)
        {
            if (oc == null || oc.gameObject == cam)
                continue;

            oc.enabled = false;
            var al = oc.GetComponent<AudioListener>();
            if (al != null) al.enabled = false;
            if (oc.CompareTag("MainCamera") && !IsRegisteredPlayerCamera(gameManager, oc))
                oc.tag = "Untagged";
            Debug.Log($"[TestSceneSetup] Camara vieja desactivada: {oc.gameObject.name}");
        }

        var playerInput = jugador.AddComponent<PlayerInput>();
        playerInput.actions = inputActionAsset;
        playerInput.defaultActionMap = "Player";
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        playerInput.neverAutoSwitchControlSchemes = true;

        var playerController = jugador.AddComponent<PlayerController>();
        playerController.camaraJugador = camera;
        playerController.puntoDisparo = cam.transform;
        
        // Componentes
        jugador.AddComponent<EnergySystem>();
        var ws = jugador.AddComponent<WeaponSystem>();
        ws.camara = camera;
        ws.puntoDisparo = cam.transform;
        
        // CharacterController se agrega automáticamente por [RequireComponent] en PlayerController
        var cc = jugador.GetComponent<CharacterController>();
        cc.radius = 0.5f;
        cc.height = 2f;
        cc.center = new Vector3(0, 0, 0);

        // Mantener una plantilla inactiva para los jugadores que entren por gamepad.
        GameObject playerTemplateObject = Instantiate(jugador);
        playerTemplateObject.name = "PlayerTemplate";
        playerTemplateObject.SetActive(false);
        var playerTemplate = playerTemplateObject.GetComponent<PlayerController>();

        // 7. Spawner
        GameObject spawnerGO = new GameObject("Spawner");
        var spawner = spawnerGO.AddComponent<EnemySpawner>();
        spawner.radioSpawn = 25f;

        // 7b. PoolManager mínimo viable
        GameObject poolGO = new GameObject("PoolManager");
        var poolMgr = poolGO.AddComponent<PoolManager>();
        
        // 8. Arena Manager
        GameObject arenaGO = new GameObject("ArenaManager");
        var arena = arenaGO.AddComponent<ArenaTransform>();
        arena.pilar = pilar;
        arena.sueloBase = suelo;
        arena.pozoCentral = pozo;
        arena.zonaGravedad = zonaGrav;
        
        // 9. Prefab de energía (intenta cargar real, fallback runtime)
        GameObject energia = Resources.Load<GameObject>("Prefabs/EnergiaPickup");
        bool energiaIsReal = energia != null;
        if (energia == null)
        {
            energia = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            energia.name = "EnergiaPickup";
            Destroy(energia.GetComponent<Collider>());
            var sc = energia.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.5f;
            energia.transform.localScale = Vector3.one * 0.5f;
            if (matEnergia != null) energia.GetComponent<Renderer>().material = matEnergia;
            else energia.GetComponent<Renderer>().material.color = Color.cyan;
            var rbEnergia = energia.AddComponent<Rigidbody>();
            rbEnergia.isKinematic = true;
            rbEnergia.useGravity = false;
            energia.AddComponent<EnergyPickup>();
        }
        else Debug.Log("[TestSceneSetup] Usando prefab real EnergiaPickup");
        // Registrar pool para pickups (mínimo 15)
        poolMgr.RegisterPool("EnergyPickup", energia, 15, 50);

        // Prefab proyectil base (intenta cargar real)
        GameObject projPrefab = Resources.Load<GameObject>("Prefabs/ProyectilBase");
        bool projIsReal = projPrefab != null;
        if (projPrefab == null)
        {
            projPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projPrefab.name = "ProyectilBase";
            projPrefab.transform.localScale = Vector3.one * 0.6f;
            Destroy(projPrefab.GetComponent<SphereCollider>());
            var colProj = projPrefab.AddComponent<SphereCollider>();
            colProj.isTrigger = true;
            colProj.radius = 0.5f;
            var rendProj = projPrefab.GetComponent<Renderer>();
            Shader sProj = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            var matProj = new Material(sProj);
            Color colProjMat = new Color(1f, 0.5f, 0f);
            if (matProj.HasProperty("_BaseColor")) matProj.SetColor("_BaseColor", colProjMat);
            else matProj.color = colProjMat;
            if (matProj.HasProperty("_Color")) matProj.SetColor("_Color", colProjMat);
            if (matProj.HasProperty("_EmissionColor")) matProj.SetColor("_EmissionColor", colProjMat * 1.2f);
            matProj.EnableKeyword("_EMISSION");
            rendProj.material = matProj;
            var rbProj = projPrefab.AddComponent<Rigidbody>();
            rbProj.useGravity = false;
            rbProj.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var trail = projPrefab.AddComponent<TrailRenderer>();
            trail.time = 0.4f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.05f;
            trail.material = matProj;
            trail.startColor = colProjMat;
            trail.endColor = new Color(1, 0.5f, 0, 0.2f);
            var lightProj = projPrefab.AddComponent<Light>();
            lightProj.type = LightType.Point;
            lightProj.color = colProjMat;
            lightProj.range = 4f;
            lightProj.intensity = 2f;
            var projComp = projPrefab.AddComponent<Projectile>();
            projComp.daño = 10f;
            projComp.tiempoVida = 5f;
        }
        else Debug.Log("[TestSceneSetup] Usando prefab real ProyectilBase");
        // Asegurar PooledObject y estado inactivo para pool
        if (projPrefab.GetComponent<PooledObject>() == null) projPrefab.AddComponent<PooledObject>().poolKey = "Proyectil";
        if (projPrefab.activeSelf) projPrefab.SetActive(false);
        poolMgr.RegisterPool("Proyectil", projPrefab, 20, 80);
        // Asignar a torreta prefab
        var torretaComp = prefabTorreta.GetComponent<Torreta>();
        if (torretaComp != null) torretaComp.prefabProyectil = projPrefab;
        
        // 10. Crear prefabs de enemigos (intenta cargar reales desde Resources/Prefabs, fallback runtime)
        spawner.prefabCorredor = CargarOcrearEnemigo("Corredor", Color.red, typeof(Runner), energia);
        spawner.prefabArtillero = CargarOcrearEnemigo("Artillero", Color.blue, typeof(Artillery), energia);
        spawner.prefabExplosivo = CargarOcrearEnemigo("Explosivo", Color.yellow, typeof(Explosive), energia);
        spawner.prefabTejedor = CargarOcrearEnemigo("Tejedor", Color.magenta, typeof(Weaver), energia);
        spawner.prefabNido = CargarOcrearEnemigo("Nido", Color.gray, typeof(Nest), energia);
        spawner.prefabColoso = CargarOcrearEnemigo("Coloso", new Color(0.5f, 0f, 0f), typeof(Colossus), energia);
        // Escala ya viene en prefab real (2,1,2 y 2.5,3,2.5); no modificar asset
        // Asignar proyectil al Artillero (si es prefab real, no modificar asset directamente - se asigna en instancia)
        var artComp = spawner.prefabArtillero.GetComponent<Artillery>();
        if (artComp != null && artComp.prefabProyectil == null) artComp.prefabProyectil = projPrefab;
        
        // 11. Configurar GameManager
        gameManager.pilar = pilar;
        gameManager.spawner = spawner;
        gameManager.player = playerController;

        var joinCoordinator = gm.AddComponent<PlayerJoinCoordinator>();
        joinCoordinator.Configure(inputActionAsset, playerTemplate, gameManager);
        var splitScreenCoordinator = gm.AddComponent<SplitScreenCameraCoordinator>();
        splitScreenCoordinator.Configure(gameManager);

        // 12. Luz y ambiente básica
        GameObject luz = new GameObject("DirectionalLight");
        var light = luz.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        
        // Luz ambiental del Pilar
        GameObject luzPilar = new GameObject("LuzPilar");
        luzPilar.transform.SetParent(pilarGO.transform);
        luzPilar.transform.localPosition = Vector3.up * 3f;
        var lightPilar = luzPilar.AddComponent<Light>();
        lightPilar.type = LightType.Point;
        lightPilar.range = 30f;
        lightPilar.intensity = 2f;
        lightPilar.color = Color.cyan;
        
        // 13. UI básica (Canvas)
        GameObject canvas = new GameObject("Canvas");
        var cv = canvas.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 13b. Presentación y feedback final (HUD, combate, audio, variante).
        GameObject hudGO = new GameObject("Hud");
        var hud = hudGO.AddComponent<Hud>();
        hud.pilar = pilar;
        hud.gameManager = gameManager;
        
        GameObject feedbackGO = new GameObject("CombatFeedback");
        feedbackGO.AddComponent<CombatFeedback>();
        
        GameObject audioGO = new GameObject("AudioAdapter");
        audioGO.AddComponent<AudioAdapter>();
        
        // Pickup de variante temporal visible en la arena + prefab para drops de enemigos.
        GameObject variantePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        variantePrefab.name = "VariantePickup";
        Destroy(variantePrefab.GetComponent<SphereCollider>());
        var colVariante = variantePrefab.AddComponent<SphereCollider>();
        colVariante.isTrigger = true;
        colVariante.radius = 0.5f;
        variantePrefab.transform.localScale = Vector3.one * 0.6f;
        var rbVariante = variantePrefab.AddComponent<Rigidbody>();
        rbVariante.isKinematic = true;
        rbVariante.useGravity = false;
        var varianteComp = variantePrefab.AddComponent<WeaponVariantPickup>();
        varianteComp.tipoPotenciado = WeaponSystem.TipoArma.Directa;
        varianteComp.multiplicadorDaño = 2f;
        varianteComp.duracionSegundos = 12f;
        variantePrefab.SetActive(false);
        
        GameObject varianteEscena = Instantiate(variantePrefab, new Vector3(5f, 1f, -5f), Quaternion.identity);
        varianteEscena.name = "VariantePickup_Escena";
        varianteEscena.SetActive(true);
        
        foreach (var prefabEnemigo in new[] { spawner.prefabCorredor, spawner.prefabArtillero, spawner.prefabExplosivo, spawner.prefabTejedor, spawner.prefabNido, spawner.prefabColoso })
        {
            if (prefabEnemigo == null) continue;
            var enemigo = prefabEnemigo.GetComponent<Enemy>();
            if (enemigo != null && enemigo.prefabVariante == null)
                enemigo.prefabVariante = variantePrefab;
        }
        
        // Activar solo después de completar la composición y configurar las acciones del jugador.
        jugador.SetActive(true);
        
        Debug.Log("[TestSceneSetup] ¡Escena de prueba generada! Apreta Play para testear.");
        
        if (destruirDespuésDeGenerar)
            Destroy(gameObject);
    }

    private static bool IsRegisteredPlayerCamera(GameManager manager, Camera camera)
    {
        if (manager == null || camera == null)
            return false;

        foreach (var player in manager.Players)
        {
            if (player != null && player.camaraJugador == camera)
                return true;
        }

        return false;
    }

    GameObject CrearPrefabEnemigo(string nombre, Color color, Type tipoScript, GameObject prefabEnergia)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.GetComponent<Renderer>().material.color = color;
        
        // Collider
        var collider = go.GetComponent<BoxCollider>();
        collider.isTrigger = false;
        
        go.AddComponent(tipoScript);
        
        // Asignar prefab de energía y modelo visual (evita UnassignedReference)
        var enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.prefabEnergia = prefabEnergia;
            if (enemy.modeloVisual == null) enemy.modeloVisual = go.transform;
        }
        
        // Rigidbody para física
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Hacerlo prefab
        go.SetActive(false);
        return go;
    }

    GameObject CargarOcrearEnemigo(string nombre, Color color, Type tipoScript, GameObject prefabEnergia)
    {
        var loaded = Resources.Load<GameObject>("Prefabs/" + nombre);
        if (loaded != null)
        {
            Debug.Log($"[TestSceneSetup] Usando prefab real {nombre} desde Resources/Prefabs");
            var e = loaded.GetComponent<Enemy>();
            if (e != null && e.prefabEnergia == null) e.prefabEnergia = prefabEnergia;
            // Asegurar que el asset no se modifique en escena (instancia se creará via Instantiate)
            return loaded;
        }
        return CrearPrefabEnemigo(nombre, color, tipoScript, prefabEnergia);
    }
}
