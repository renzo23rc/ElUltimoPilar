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
 *
 * La construcción de cada parte vive en TestSceneArenaBuilder, TestScenePlayerBuilder y
 * TestScenePrefabFactory; este componente solo compone la escena y conecta las referencias.
 */
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Builds the runtime test scene and its required gameplay composition.
/// </summary>
public class TestSceneSetup : MonoBehaviour
{
    private const int EnergyPoolInitialSize = 15;
    private const int EnergyPoolMaximumSize = 50;
    private const int ProjectilePoolInitialSize = 20;
    private const int ProjectilePoolMaximumSize = 80;
    private const float SpawnRadiusMeters = 25f;
    private const string RuntimeTemplatesRootName = "PlantillasRuntime";
    private const string ResourcesPrefabFolder = "Prefabs/";
    private const string EnergyPoolKey = "EnergyPickup";
    private const string ProjectilePoolKey = "Proyectil";
    private static readonly Vector3 SceneVariantPickupPosition = new Vector3(5f, 1f, -5f);
    private static readonly Color ColossusColor = new Color(0.5f, 0f, 0f);

    private Transform plantillasRuntime;

    [Header("Configuración Rápida")]
    /// <summary>Gets or sets whether generation runs on start.</summary>
    public bool generarAlIniciar = true;
    /// <summary>Gets or sets whether this setup object is destroyed after generation.</summary>
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

    /// <summary>Generates the complete runtime test scene.</summary>
    [ContextMenu("Generar Escena de Prueba")]
    public void GenerarEscenaDePrueba()
    {
        Debug.Log("[TestSceneSetup] Generando escena de prueba...");

        // Contenedor inactivo: las copias de prefabs que se configuran en runtime viven acá,
        // así nunca se modifican los assets de Resources.
        plantillasRuntime = new GameObject(RuntimeTemplatesRootName).transform;
        plantillasRuntime.gameObject.SetActive(false);

        GameObject gameManagerObject = new GameObject("GameManager");
        var gameManager = gameManagerObject.AddComponent<GameManager>();

        // Arena: Pilar con torretas de emergencia, suelo, pozo (fase 2) y zona de gravedad (fase 3).
        // Si la escena ya trae un Pilar (el Obelisco), se usa ese; si no, se arma el cilindro de respaldo.
        Pilar pilar = FindFirstObjectByType<Pilar>();
        if (pilar == null)
        {
            pilar = TestSceneArenaBuilder.CreatePilar(matPilar);
        }

        pilar.prefabTorreta = CargarPlantilla("Torreta") ?? TestScenePrefabFactory.CreateTurret();
        GameObject suelo = TestSceneArenaBuilder.CreateFloor(matSuelo);
        GameObject pozo = TestSceneArenaBuilder.CreatePit();
        GameObject zonaGravedad = TestSceneArenaBuilder.CreateGravityZone();

        // Jugador primario (inactivo hasta terminar la composición) y plantilla para gamepads.
        PlayerController jugador = TestScenePlayerBuilder.CreatePrimary(inputActionAsset, matJugador, gameManager);
        PlayerController plantillaJugador = TestScenePlayerBuilder.CreateJoinTemplate(jugador);

        GameObject spawnerObject = new GameObject("Spawner");
        var spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.radioSpawn = SpawnRadiusMeters;
        var poolManager = new GameObject("PoolManager").AddComponent<PoolManager>();

        var arena = new GameObject("ArenaManager").AddComponent<ArenaTransform>();
        arena.pilar = pilar;
        arena.sueloBase = suelo;
        arena.pozoCentral = pozo;
        arena.zonaGravedad = zonaGravedad;

        GameObject energia = PrepararEnergia(poolManager);
        GameObject proyectil = PrepararProyectil(poolManager);
        if (pilar.prefabTorreta.TryGetComponent(out Torreta torreta)) torreta.prefabProyectil = proyectil;
        PrepararEnemigos(spawner, energia, proyectil);

        gameManager.pilar = pilar;
        gameManager.spawner = spawner;
        gameManager.player = jugador;
        gameManagerObject.AddComponent<PlayerJoinCoordinator>().Configure(inputActionAsset, plantillaJugador, gameManager);
        gameManagerObject.AddComponent<SplitScreenCameraCoordinator>().Configure(gameManager);

        TestSceneArenaBuilder.CreateSunLight();
        CrearPresentacion(pilar, gameManager);
        PrepararVariantes(spawner);

        // Activar solo después de completar la composición y configurar las acciones del jugador.
        jugador.gameObject.SetActive(true);

        Debug.Log("[TestSceneSetup] ¡Escena de prueba generada! Apreta Play para testear.");

        if (destruirDespuésDeGenerar)
            Destroy(gameObject);
    }

    // Orbe de energía (prefab real o respaldo) con pool para los drops.
    GameObject PrepararEnergia(PoolManager poolManager)
    {
        GameObject energia = Resources.Load<GameObject>(ResourcesPrefabFolder + "EnergiaPickup");
        if (energia == null)
            energia = TestScenePrefabFactory.CreateEnergyPickup(matEnergia);
        else
            Debug.Log("[TestSceneSetup] Usando prefab real EnergiaPickup");

        poolManager.RegisterPool(EnergyPoolKey, energia, EnergyPoolInitialSize, EnergyPoolMaximumSize);
        return energia;
    }

    // Proyectil físico compartido por artilleros y torretas, siempre poolable e inactivo.
    GameObject PrepararProyectil(PoolManager poolManager)
    {
        GameObject proyectil = CargarPlantilla("ProyectilBase") ?? TestScenePrefabFactory.CreateProjectile();
        if (proyectil.GetComponent<PooledObject>() == null)
            proyectil.AddComponent<PooledObject>().poolKey = ProjectilePoolKey;
        if (proyectil.activeSelf) proyectil.SetActive(false);
        poolManager.RegisterPool(ProjectilePoolKey, proyectil, ProjectilePoolInitialSize, ProjectilePoolMaximumSize);
        return proyectil;
    }

    // Plantillas de enemigos: prefab real de Resources/Prefabs o cubo de respaldo.
    void PrepararEnemigos(EnemySpawner spawner, GameObject energia, GameObject proyectil)
    {
        spawner.prefabCorredor = CargarOcrearEnemigo("Corredor", Color.red, typeof(Runner), energia);
        spawner.prefabArtillero = CargarOcrearEnemigo("Artillero", Color.blue, typeof(Artillery), energia);
        spawner.prefabExplosivo = CargarOcrearEnemigo("Explosivo", Color.yellow, typeof(Explosive), energia);
        spawner.prefabTejedor = CargarOcrearEnemigo("Tejedor", Color.magenta, typeof(Weaver), energia);
        spawner.prefabNido = CargarOcrearEnemigo("Nido", Color.gray, typeof(Nest), energia);
        spawner.prefabColoso = CargarOcrearEnemigo("Coloso", ColossusColor, typeof(Colossus), energia);

        // Las plantillas son copias de escena: asignar el proyectil no modifica el asset.
        if (spawner.prefabArtillero.TryGetComponent(out Artillery artillero) && artillero.prefabProyectil == null)
            artillero.prefabProyectil = proyectil;
    }

    // HUD compartido, feedback de combate y audio procedural.
    void CrearPresentacion(Pilar pilar, GameManager gameManager)
    {
        GameObject canvas = new GameObject("Canvas");
        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        var hud = new GameObject("Hud").AddComponent<Hud>();
        hud.pilar = pilar;
        hud.gameManager = gameManager;
        new GameObject("CombatFeedback").AddComponent<CombatFeedback>();
        new GameObject("AudioAdapter").AddComponent<AudioAdapter>();
    }

    // Pickup de variante visible en la arena y como drop de todos los enemigos.
    void PrepararVariantes(EnemySpawner spawner)
    {
        GameObject variantePrefab = TestScenePrefabFactory.CreateVariantPickup();
        GameObject varianteEscena = Instantiate(variantePrefab, SceneVariantPickupPosition, Quaternion.identity);
        varianteEscena.name = "VariantePickup_Escena";
        varianteEscena.SetActive(true);

        GameObject[] prefabsEnemigos =
        {
            spawner.prefabCorredor,
            spawner.prefabArtillero,
            spawner.prefabExplosivo,
            spawner.prefabTejedor,
            spawner.prefabNido,
            spawner.prefabColoso
        };
        foreach (GameObject prefabEnemigo in prefabsEnemigos)
        {
            if (prefabEnemigo != null
                && prefabEnemigo.TryGetComponent(out Enemy enemigo)
                && enemigo.prefabVariante == null)
            {
                enemigo.prefabVariante = variantePrefab;
            }
        }
    }

    GameObject CargarOcrearEnemigo(string nombre, Color color, Type tipoScript, GameObject prefabEnergia)
    {
        GameObject plantilla = CargarPlantilla(nombre);
        if (plantilla == null)
            return TestScenePrefabFactory.CreateEnemy(nombre, color, tipoScript, prefabEnergia);

        if (plantilla.TryGetComponent(out Enemy enemy) && enemy.prefabEnergia == null)
            enemy.prefabEnergia = prefabEnergia;
        return plantilla;
    }

    /// <summary>
    /// Returns a scene copy of a Resources prefab parked under the inactive templates root,
    /// so runtime configuration never mutates the asset. Returns null when the prefab is missing.
    /// </summary>
    GameObject CargarPlantilla(string nombre)
    {
        GameObject asset = Resources.Load<GameObject>(ResourcesPrefabFolder + nombre);
        if (asset == null)
            return null;

        // Bajo un padre inactivo la copia no ejecuta Awake/OnEnable hasta que se instancie desde ella.
        GameObject plantilla = Instantiate(asset, plantillasRuntime);
        plantilla.name = asset.name;
        Debug.Log($"[TestSceneSetup] Usando prefab real {nombre} desde Resources/Prefabs");
        return plantilla;
    }
}
