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
    private const float MinimumAutomaticSpawnIntervalSeconds = 0.7f;
    private const float AutomaticSpawnIntervalSeconds = 1.8f;
    private const float AutomaticSpawnIntervalReductionSeconds = 0.08f;
    private const float SpawnHeightMeters = 1f;
    private const int AutomaticSpawnBonusThreshold = 5;
    private const int AutomaticSpawnLateThreshold = 8;
    private const int AutomaticSpawnPointCount = 8;
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
    
    [Header("Puntos de Spawn")]
    public Transform[] puntosSpawn;
    
    [Header("Radio de Spawn (si no hay puntos definidos)")]
    public float radioSpawn = 25f;
    
    [Header("Estado")]
    public bool OleadaEnProgreso { get; private set; }
    public int EnemigosVivos { get; private set; }
    public int OleadaActual => oleadaActual;
    
    private int oleadaActual = 0;
    private int enemigosSpawned = 0;
    private int enemigosPorSpawnear = 0;
    private float timerSpawn = 0f;
    private List<Enemy> enemigosActivos = new List<Enemy>();
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
        // Si no hay puntos de spawn definidos, generar alrededor del Pilar
        if (puntosSpawn == null || puntosSpawn.Length == 0)
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
        
        // Limpiar enemigos muertos de la lista
        enemigosActivos.RemoveAll(e => e == null);
        EnemigosVivos = enemigosActivos.Count;
        
        // Verificar si oleada terminó
        if (enemigosPorSpawnear <= 0 && EnemigosVivos == 0)
        {
            OleadaEnProgreso = false;
        }
    }

    public void IniciarOleada(int numero)
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
        
        ConfigOleada config = ConfigActual();
        if (config == null)
        {
            config = GenerarConfigAutomatica(numero);
        }
        // Clonar para no mutar la config original (evita bug de decrementar contadores)
        configActualCache = ClonarConfig(config);
        
        enemigosPorSpawnear = configActualCache.cantidadTotal;
        OleadaEnProgreso = true;
        timerSpawn = 0f;
        
        Debug.Log($"[Spawner] Oleada {numero} iniciada. Enemigos: {enemigosPorSpawnear} (config cacheada: C{configActualCache.corredores} A{configActualCache.artilleros} E{configActualCache.explosivos} T{configActualCache.tejedores} N{configActualCache.nidos} Col{configActualCache.colosos})");
    }

    ConfigOleada ClonarConfig(ConfigOleada src)
    {
        return new ConfigOleada
        {
            numeroOleada = src.numeroOleada,
            cantidadTotal = src.cantidadTotal,
            corredores = src.corredores,
            artilleros = src.artilleros,
            explosivos = src.explosivos,
            tejedores = src.tejedores,
            nidos = src.nidos,
            colosos = src.colosos,
            intervaloSpawn = src.intervaloSpawn
        };
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
        GameObject prefab = SeleccionarPrefab(config);
        if (prefab == null) return;
        
        Vector3 posicion = ObtenerPosicionSpawn();
        Debug.Log($"[Spawner] Spawneando {prefab.name} en {posicion} (restantes: {enemigosPorSpawnear})");
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

    GameObject SeleccionarPrefab(ConfigOleada config)
    {
        // Lógica simple: spawnear en orden de prioridad según contadores restantes
        int totalRestante = enemigosPorSpawnear;
        
        if (config.colosos > 0 && enemigosSpawned >= config.cantidadTotal - config.colosos && prefabColoso != null)
        {
            config.colosos--;
            return prefabColoso;
        }
        if (config.nidos > 0 && enemigosSpawned >= config.cantidadTotal / 2 && prefabNido != null)
        {
            config.nidos--;
            return prefabNido;
        }
        if (config.tejedores > 0 && enemigosSpawned >= config.cantidadTotal / 3 && prefabTejedor != null)
        {
            config.tejedores--;
            return prefabTejedor;
        }
        if (config.explosivos > 0 && prefabExplosivo != null)
        {
            config.explosivos--;
            return prefabExplosivo;
        }
        if (config.artilleros > 0 && prefabArtillero != null)
        {
            config.artilleros--;
            return prefabArtillero;
        }
        if (config.corredores > 0 && prefabCorredor != null)
        {
            config.corredores--;
            return prefabCorredor;
        }
        
        // Fallback: cualquier prefab disponible
        if (prefabCorredor != null) return prefabCorredor;
        if (prefabArtillero != null) return prefabArtillero;
        if (prefabExplosivo != null) return prefabExplosivo;
        
        return null;
    }

    Vector3 ObtenerPosicionSpawn()
    {
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

    ConfigOleada GenerarConfigAutomatica(int oleada)
    {
        // Balanceo B1: 10 oleadas escalables, 12-20 min totales (~70-120s por oleada)
        // Curva testeada: oleada 1 ~8 enemigos (~12s spawn), oleada 10 ~28 enemigos (~28s spawn) + combate
        var config = new ConfigOleada
        {
            numeroOleada = oleada,
            cantidadTotal = 6 + oleada * 2 + (oleada >= AutomaticSpawnBonusThreshold ? 2 : 0) + (oleada >= AutomaticSpawnLateThreshold ? 4 : 0), // 8,10,12..28 para 10
            corredores = 3 + oleada * 1 + (oleada >= 4 ? 1 : 0),
            artilleros = Mathf.Max(0, oleada - 1),
            explosivos = Mathf.Max(0, oleada >= 3 ? (oleada - 2) / 2 + 1 : 0), // menos spam explosivo
            tejedores = Mathf.Max(0, oleada >= 4 ? 1 : 0) + (oleada >= 7 ? 1 : 0),
            nidos = oleada >= 5 ? 1 : 0,
            colosos = oleada >= 7 ? 1 : 0,
            intervaloSpawn = Mathf.Max(MinimumAutomaticSpawnIntervalSeconds, AutomaticSpawnIntervalSeconds - oleada * AutomaticSpawnIntervalReductionSeconds) // 1.7s -> 1.0s, evita masacre instant
        };
        // Clamp para que suma de tipos no supere cantidadTotal (prioridad en SeleccionarPrefab maneja fallback)
        int suma = config.corredores + config.artilleros + config.explosivos + config.tejedores + config.nidos + config.colosos;
        if (suma > config.cantidadTotal)
        {
            // Reducir corredores si sobra
            config.corredores = Mathf.Max(1, config.cantidadTotal - (config.artilleros + config.explosivos + config.tejedores + config.nidos + config.colosos));
        }
        return config;
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
        // Asegurar que el spawner lo elimine cuando muera (si el enemigo no lo hace ya)
        enemy.OnMuerte += () => EnemigoEliminado(enemy);
        Debug.Log($"[Spawner] Enemigo externo registrado: {enemy.name} - Vivos ahora: {EnemigosVivos}");
    }

    /// <summary>
    /// Limpia todos los enemigos activos (usado al finalizar partida para evitar leaks).
    /// </summary>
    public void LimpiarTodos()
    {
        if (enemigosActivos == null)
            enemigosActivos = new List<Enemy>();

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
