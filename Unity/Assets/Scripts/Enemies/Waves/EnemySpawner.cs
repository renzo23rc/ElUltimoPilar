/**
 * EnemySpawner.cs
 * Gestiona la aparición de enemigos en oleadas según el GDD.
 * Soporta configuración por oleada y diferentes tipos de enemigo.
 * 
 * Colocar en un GameObject vacío "Spawner" con puntos de spawn como hijos.
 */
using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    private const float DefaultSpawnIntervalSeconds = 1.5f;
    private const float SpawnHeightMeters = 1f;
    private const int AutomaticSpawnPointCount = 8;
    private const int DefaultPlayerCount = 1;

    public static EnemySpawner Instance { get; private set; }

    [System.Serializable]
    public class ConfigOleada
    {
        public int numeroOleada;
        [Tooltip("Cantidad total de enemigos en esta oleada")]
        public int cantidadTotal;
        [Tooltip("Cantidad de Corredores")]
        public int corredores;
        [Tooltip("Cantidad de Artilleros")]
        public int artilleros;
        [Tooltip("Cantidad de Explosivos")]
        public int explosivos;
        [Tooltip("Cantidad de Tejedores (oleadas medias+)")]
        public int tejedores;
        [Tooltip("Cantidad de Nidos/Incubadoras (oleadas medias+)")]
        public int nidos;
        [Tooltip("Cantidad de Colosos (oleadas tardías)")]
        public int colosos;
        [Tooltip("Segundos entre spawns dentro de la oleada")]
        public float intervaloSpawn = DefaultSpawnIntervalSeconds;

        /// <summary>Returns a copy, so a running wave never mutates the Inspector configuration.</summary>
        public ConfigOleada Clone()
        {
            return (ConfigOleada)MemberwiseClone();
        }
    }
    
    [Header("Configuración de Oleadas")]
    public List<ConfigOleada> configuracionOleadas = new List<ConfigOleada>();
    
    [Header("Prefabs de Enemigos")]
    public GameObject prefabCorredor;
    public GameObject prefabArtillero;
    public GameObject prefabExplosivo;
    public GameObject prefabTejedor;
    public GameObject prefabNido;
    public GameObject prefabColoso;
    
    [Header("Zonas de Spawn")]
    [Tooltip("Zonas donde pueden aparecer enemigos. Si queda vacío se usan todas las SpawnZone de la escena. Tienen prioridad sobre los puntos y el radio.")]
    [SerializeField] private SpawnZone[] zonasSpawn;

    [Header("Puntos de Spawn (si no hay zonas)")]
    public Transform[] puntosSpawn;
    
    [Header("Radio de Spawn (si no hay puntos definidos)")]
    public float radioSpawn = 25f;

    [Header("Escalado cooperativo")]
    [Tooltip("Enemigos extra por cada jugador además del primero (0.35 = +35% por jugador). Nidos y Colosos no escalan.")]
    [SerializeField, Range(0f, 1f)] private float extraEnemiesPerPlayerRatio = 0.35f;
    
    [Header("Estado")]
    public bool OleadaEnProgreso { get; private set; }
    public int EnemigosVivos { get; private set; }
    public int OleadaActual => oleadaActual;
    
    private int oleadaActual = 0;
    private int enemigosSpawned = 0;
    private int enemigosPorSpawnear = 0;
    private float timerSpawn = 0f;
    private readonly List<Enemy> enemigosActivos = new List<Enemy>();
    private readonly List<float> zoneAreasBuffer = new List<float>();
    private ConfigOleada configActualCache = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (zonasSpawn == null || zonasSpawn.Length == 0)
        {
            zonasSpawn = FindObjectsByType<SpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        // Sin zonas ni puntos de spawn definidos, generar alrededor del Pilar
        if (zonasSpawn.Length == 0 && (puntosSpawn == null || puntosSpawn.Length == 0))
        {
            GenerarPuntosSpawnAutomaticos();
        }
    }

    void Update()
    {
        if (!OleadaEnProgreso) return;
        
        timerSpawn -= Time.deltaTime;
        
        if (timerSpawn <= 0 && enemigosPorSpawnear > 0)
        {
            SpawnearSiguienteEnemigo();
            timerSpawn = (configActualCache ?? ConfigActual())?.intervaloSpawn ?? DefaultSpawnIntervalSeconds;
        }
        
        // Limpiar enemigos muertos o destruidos de la lista
        enemigosActivos.RemoveAll(e => e == null || e.EstaMuerto);
        EnemigosVivos = enemigosActivos.Count;
        
        // Verificar si oleada terminó
        if (enemigosPorSpawnear <= 0 && EnemigosVivos == 0)
        {
            OleadaEnProgreso = false;
        }
    }

    public void IniciarOleada(int numero)
    {
        IniciarOleada(numero, DefaultPlayerCount);
    }

    /// <summary>Starts a wave scaled for the number of registered players.</summary>
    /// <param name="numero">The wave number.</param>
    /// <param name="cantidadJugadores">The number of registered players.</param>
    public void IniciarOleada(int numero, int cantidadJugadores)
    {
        oleadaActual = numero;
        enemigosSpawned = 0;
        // Limpiar solo nulos; los vivos de oleada anterior ya deben estar muertos (incluye crías de Nido)
        // pero por seguridad removemos nulos y mantenemos vivos si aún existen (evita perder track de crías)
        enemigosActivos.RemoveAll(e => e == null);
        if (enemigosActivos.Count > 0)
        {
            Debug.LogWarning($"[Spawner] IniciarOleada {numero} con {enemigosActivos.Count} enemigos residuales (crías de Nido u otros). Se mantienen en conteo.");
        }
        
        // Clonar para no mutar la config original (evita bug de decrementar contadores)
        configActualCache = (ConfigActual() ?? AutomaticWaveConfigFactory.Create(numero)).Clone();
        WaveDifficultyScaler.Apply(configActualCache, cantidadJugadores, extraEnemiesPerPlayerRatio);
        
        enemigosPorSpawnear = configActualCache.cantidadTotal;
        OleadaEnProgreso = true;
        timerSpawn = 0f;
        
        Debug.Log($"[Spawner] Oleada {numero} iniciada para {cantidadJugadores} jugador(es). Enemigos: {enemigosPorSpawnear} (config cacheada: C{configActualCache.corredores} A{configActualCache.artilleros} E{configActualCache.explosivos} T{configActualCache.tejedores} N{configActualCache.nidos} Col{configActualCache.colosos})");
    }

    void SpawnearSiguienteEnemigo()
    {
        ConfigOleada config = configActualCache ?? ConfigActual();
        if (config == null)
        {
            Debug.LogWarning($"[Spawner] SpawnearSiguienteEnemigo fallo: config null para oleada {oleadaActual}");
            return;
        }
        
        // Determinar qué tipo spawnear basado en la progresión
        GameObject prefab = WaveEnemySelector.TrySelect(config, enemigosSpawned, TienePrefab, out WaveEnemyType tipo)
            ? PrefabDe(tipo)
            : null;
        if (prefab == null)
        {
            // Sin prefabs asignados no hay nada que spawnear: se descuenta para que la oleada pueda terminar.
            Debug.LogWarning($"[Spawner] Oleada {oleadaActual}: no hay prefab de enemigo asignado.");
            enemigosPorSpawnear--;
            return;
        }

        Vector3 posicion = ObtenerPosicionSpawn();
        GameObject enemigo = Instantiate(prefab, posicion, Quaternion.identity);
        enemigo.SetActive(true); // prefab base esta desactivado en TestSceneSetup
        enemigo.name = prefab.name + "(Clone)";
        
        var enemyComp = enemigo.GetComponent<Enemy>();
        if (enemyComp != null)
        {
            enemigosActivos.Add(enemyComp);
        }
        
        enemigosSpawned++;
        enemigosPorSpawnear--;
    }

    bool TienePrefab(WaveEnemyType tipo)
    {
        return PrefabDe(tipo) != null;
    }

    GameObject PrefabDe(WaveEnemyType tipo)
    {
        return tipo switch
        {
            WaveEnemyType.Runner => prefabCorredor,
            WaveEnemyType.Artillery => prefabArtillero,
            WaveEnemyType.Explosive => prefabExplosivo,
            WaveEnemyType.Weaver => prefabTejedor,
            WaveEnemyType.Nest => prefabNido,
            WaveEnemyType.Colossus => prefabColoso,
            _ => null
        };
    }

    bool TryObtenerPosicionEnZona(out Vector3 posicion)
    {
        posicion = Vector3.zero;
        if (zonasSpawn == null || zonasSpawn.Length == 0) return false;

        zoneAreasBuffer.Clear();
        foreach (SpawnZone zona in zonasSpawn)
        {
            bool usable = zona != null && zona.isActiveAndEnabled;
            zoneAreasBuffer.Add(usable ? zona.Area : 0f);
        }

        int index = SpawnZoneSelector.PickIndex(zoneAreasBuffer, Random.value);
        if (index < 0) return false;

        posicion = zonasSpawn[index].SamplePoint();
        return true;
    }

    Vector3 ObtenerPosicionSpawn()
    {
        if (TryObtenerPosicionEnZona(out Vector3 posicionEnZona))
        {
            return posicionEnZona;
        }

        if (puntosSpawn != null && puntosSpawn.Length > 0)
        {
            int index = Random.Range(0, puntosSpawn.Length);
            return puntosSpawn[index].position;
        }
        
        // Spawn circular alrededor del origen
        float angulo = Random.Range(0f, Mathf.PI * 2f);
        float x = Mathf.Cos(angulo) * radioSpawn;
        float z = Mathf.Sin(angulo) * radioSpawn;
        return new Vector3(x, SpawnHeightMeters, z);
    }

    ConfigOleada ConfigActual()
    {
        if (configuracionOleadas == null || configuracionOleadas.Count == 0) return null;
        return configuracionOleadas.Find(c => c.numeroOleada == oleadaActual);
    }

    void GenerarPuntosSpawnAutomaticos()
    {
        // Crear 8 puntos de spawn en círculo
        List<Transform> puntos = new List<Transform>();
        for (int i = 0; i < AutomaticSpawnPointCount; i++)
        {
            GameObject go = new GameObject($"SpawnPoint_{i}");
            go.transform.SetParent(transform);
            float angulo = (i / (float)AutomaticSpawnPointCount) * Mathf.PI * 2f;
            go.transform.position = new Vector3(Mathf.Cos(angulo) * radioSpawn, SpawnHeightMeters, Mathf.Sin(angulo) * radioSpawn);
            puntos.Add(go.transform);
        }
        puntosSpawn = puntos.ToArray();
    }

    public void EnemigoEliminado(Enemy enemy)
    {
        enemigosActivos.Remove(enemy);
        EnemigosVivos = enemigosActivos.Count;
    }

    /// <summary>
    /// Registra enemigos generados externamente (ej. corredores de Nido) para que la oleada no termine mientras sigan vivos.
    /// </summary>
    public void RegistrarEnemigoExterno(Enemy enemy)
    {
        if (enemy == null) return;
        if (enemigosActivos.Contains(enemy)) return;
        enemigosActivos.Add(enemy);
        EnemigosVivos = enemigosActivos.Count;
        // Morir() notifica EnemigoEliminado; los destruidos por otra vía se purgan en Update.
    }

    /// <summary>
    /// Limpia todos los enemigos activos (usado al finalizar partida para evitar leaks).
    /// </summary>
    public void LimpiarTodos()
    {
        // Only enemies owned and tracked by this spawner are cleaned here.
        // Projectiles, pickups and WeaverZones have no coordinated owner yet.
        foreach (var e in enemigosActivos)
        {
            if (e != null) Destroy(e.gameObject);
        }
        enemigosActivos.Clear();
        EnemigosVivos = 0;
        OleadaEnProgreso = false;
        oleadaActual = 0;
        enemigosSpawned = 0;
        enemigosPorSpawnear = 0;
        timerSpawn = 0f;
        configActualCache = null;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
