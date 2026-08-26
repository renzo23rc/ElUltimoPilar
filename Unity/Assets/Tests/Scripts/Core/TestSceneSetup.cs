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
        
        // Puntos para torretas (fase 4)
        Transform[] torretas = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject t = new GameObject($"PuntoTorreta_{i}");
            t.transform.SetParent(pilarGO.transform);
            float angulo = (i / 4f) * Mathf.PI * 2f;
            t.transform.localPosition = new Vector3(Mathf.Cos(angulo) * 3f, 0, Mathf.Sin(angulo) * 3f);
            torretas[i] = t.transform;
        }
        pilar.puntosTorretas = torretas;
        
        // 3. Suelo / Arena
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "Arena";
        suelo.transform.position = Vector3.zero;
        suelo.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 unidades
        // Tag removido - no es necesario para el funcionamiento
        if (matSuelo != null) suelo.GetComponent<Renderer>().material = matSuelo;
        
        // 4. Pozo Central (inicialmente desactivado)
        GameObject pozo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pozo.name = "PozoCentral";
        Destroy(pozo.GetComponent<Collider>());
        pozo.transform.position = new Vector3(0, -3f, 0);
        pozo.transform.localScale = new Vector3(3f, 3f, 3f);
        pozo.GetComponent<Renderer>().material.color = Color.black;
        pozo.SetActive(false);
        
        // 5. Zona de Gravedad (inicialmente desactivada)
        GameObject zonaGrav = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        zonaGrav.name = "ZonaGravedad";
        Destroy(zonaGrav.GetComponent<Collider>());
        zonaGrav.transform.position = new Vector3(8f, 1f, 0);
        zonaGrav.transform.localScale = new Vector3(6f, 6f, 6f);
        var matZona = new Material(Shader.Find("Standard"));
        matZona.color = new Color(0.3f, 0f, 0.6f, 0.2f);
        matZona.SetFloat("_Mode", 3);
        zonaGrav.GetComponent<Renderer>().material = matZona;
        zonaGrav.SetActive(false);
        
        // 6. Jugador (GameObject vacío + hijo visual para evitar conflictos)
        GameObject jugador = new GameObject("Jugador");
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
        
        var playerController = jugador.AddComponent<PlayerController>();
        playerController.camaraJugador = camera;
        playerController.puntoDisparo = cam.transform;
        
        // Componentes
        jugador.AddComponent<EnergySystem>();
        jugador.AddComponent<WeaponSystem>();
        
        // CharacterController se agrega automáticamente por [RequireComponent] en PlayerController
        var cc = jugador.GetComponent<CharacterController>();
        cc.radius = 0.5f;
        cc.height = 2f;
        cc.center = new Vector3(0, 0, 0);
        
        // 7. Spawner
        GameObject spawnerGO = new GameObject("Spawner");
        var spawner = spawnerGO.AddComponent<EnemySpawner>();
        spawner.radioSpawn = 25f;
        
        // 8. Arena Manager
        GameObject arenaGO = new GameObject("ArenaManager");
        var arena = arenaGO.AddComponent<ArenaTransform>();
        arena.pilar = pilar;
        arena.sueloBase = suelo;
        arena.pozoCentral = pozo;
        arena.zonaGravedad = zonaGrav;
        
        // 9. Prefab de energía (crear primero para asignar a enemigos)
        GameObject energia = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        energia.name = "EnergiaPickup";
        Destroy(energia.GetComponent<Collider>());
        var sc = energia.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.5f;
        energia.transform.localScale = Vector3.one * 0.5f;
        if (matEnergia != null) energia.GetComponent<Renderer>().material = matEnergia;
        else energia.GetComponent<Renderer>().material.color = Color.cyan;
        energia.AddComponent<EnergyPickup>();
        
        // 10. Crear prefabs de enemigos para el spawner
        spawner.prefabCorredor = CrearPrefabEnemigo("Corredor", Color.red, typeof(Runner), energia);
        spawner.prefabArtillero = CrearPrefabEnemigo("Artillero", Color.blue, typeof(Artillery), energia);
        spawner.prefabExplosivo = CrearPrefabEnemigo("Explosivo", Color.yellow, typeof(Explosive), energia);
        spawner.prefabTejedor = CrearPrefabEnemigo("Tejedor", Color.magenta, typeof(Weaver), energia);
        spawner.prefabNido = CrearPrefabEnemigo("Nido", Color.gray, typeof(Nest), energia);
        spawner.prefabColoso = CrearPrefabEnemigo("Coloso", new Color(0.5f, 0f, 0f), typeof(Colossus), energia);
        
        spawner.prefabNido.transform.localScale = new Vector3(2f, 1f, 2f);
        spawner.prefabColoso.transform.localScale = new Vector3(2.5f, 3f, 2.5f);
        
        // 11. Configurar GameManager
        gameManager.pilar = pilar;
        gameManager.spawner = spawner;
        gameManager.player = playerController;
        
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
        
        Debug.Log("[TestSceneSetup] ¡Escena de prueba generada! Apreta Play para testear.");
        
        if (destruirDespuésDeGenerar)
            Destroy(gameObject);
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
        
        // Asignar prefab de energía
        var enemy = go.GetComponent<Enemy>();
        if (enemy != null) enemy.prefabEnergia = prefabEnergia;
        
        // Rigidbody para física
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Hacerlo prefab
        go.SetActive(false);
        return go;
    }
}
