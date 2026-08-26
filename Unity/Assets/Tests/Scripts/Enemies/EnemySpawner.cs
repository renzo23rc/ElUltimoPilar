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
        public float intervaloSpawn = 1.5f;
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
            timerSpawn = (configActualCache ?? ConfigActual())?.intervaloSpawn ?? 1.5f;
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
        enemigosActivos.Clear();
        
        ConfigOleada config = ConfigActual();
        if (config == null)
        {
            // Generar config automática si no está definida
            config = GenerarConfigAutomatica(numero);
        }
        configActualCache = config;
        
        enemigosPorSpawnear = config.cantidadTotal;
        OleadaEnProgreso = true;
        timerSpawn = 0f;
        
        Debug.Log($"[Spawner] Oleada {numero} iniciada. Enemigos: {enemigosPorSpawnear} (config cacheada: C{config.corredores} A{config.artilleros} E{config.explosivos})");
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
        return new Vector3(x, 1f, z);
    }

    ConfigOleada ConfigActual()
    {
        if (configuracionOleadas == null || configuracionOleadas.Count == 0) return null;
        return configuracionOleadas.Find(c => c.numeroOleada == oleadaActual);
    }

    ConfigOleada GenerarConfigAutomatica(int oleada)
    {
        var config = new ConfigOleada
        {
            numeroOleada = oleada,
            cantidadTotal = 5 + oleada * 3,
            corredores = 3 + oleada * 2,
            artilleros = Mathf.Max(0, oleada - 1),
            explosivos = Mathf.Max(0, oleada - 2),
            tejedores = Mathf.Max(0, oleada - 3),
            nidos = oleada >= 5 ? 1 : 0,
            colosos = oleada >= 7 ? 1 : 0,
            intervaloSpawn = Mathf.Max(0.5f, 2f - oleada * 0.1f)
        };
        return config;
    }

    void GenerarPuntosSpawnAutomaticos()
    {
        // Crear 8 puntos de spawn en círculo
        List<Transform> puntos = new List<Transform>();
        for (int i = 0; i < 8; i++)
        {
            GameObject go = new GameObject($"SpawnPoint_{i}");
            go.transform.SetParent(transform);
            float angulo = (i / 8f) * Mathf.PI * 2f;
            go.transform.position = new Vector3(Mathf.Cos(angulo) * radioSpawn, 1f, Mathf.Sin(angulo) * radioSpawn);
            puntos.Add(go.transform);
        }
        puntosSpawn = puntos.ToArray();
    }

    public void EnemigoEliminado(Enemy enemy)
    {
        enemigosActivos.Remove(enemy);
        EnemigosVivos = enemigosActivos.Count;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
