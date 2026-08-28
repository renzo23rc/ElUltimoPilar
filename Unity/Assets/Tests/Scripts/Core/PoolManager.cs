/**
 * PoolManager.cs
 * Object Pooling mínimo viable para hordas/proyectiles/pickups/VFX
 * Sustituye Instantiate/Destroy repetitivo en caminos frecuentes de combate.
 *
 * Usa UnityEngine.Pool.ObjectPool<GameObject> (Unity 6000).
 * Pools mínimos: Proyectiles, EnergyPickup, VFX Trazador/Impacto.
 * Enemigos quedan fuera de pooling en B1 mínimo (complejidad), pero infra lista para extender.
 *
 * Colocar en GameManager o crear automáticamente si no existe.
 */
using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private readonly Dictionary<string, ObjectPool<GameObject>> pools = new Dictionary<string, ObjectPool<GameObject>>();
    private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private Transform poolRoot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        poolRoot = new GameObject("PoolRoot").transform;
        poolRoot.SetParent(transform);
        DontDestroyOnLoad(gameObject); // opcional, si cambia escena
    }

    void Start()
    {
        // Auto-registrar pools si existen en escena (prefabs desactivados de TestSceneSetup)
        // Se crean bajo demanda con RegisterPool si no existen
    }

    public void RegisterPool(string key, GameObject prefab, int initial = 10, int max = 50)
    {
        if (pools.ContainsKey(key)) return;
        if (prefab == null)
        {
            Debug.LogWarning($"[PoolManager] RegisterPool {key} prefab null, se omitió registro");
            return;
        }
        prefabs[key] = prefab;
        pools[key] = new ObjectPool<GameObject>(
            createFunc: () => CreatePooled(key),
            actionOnGet: (go) => go.SetActive(true),
            actionOnRelease: (go) => go.SetActive(false),
            actionOnDestroy: (go) => Destroy(go),
            collectionCheck: false,
            defaultCapacity: initial,
            maxSize: max
        );
        // Pre-warm
        var warm = new List<GameObject>();
        for (int i = 0; i < initial; i++)
        {
            var go = pools[key].Get();
            warm.Add(go);
        }
        foreach (var go in warm) pools[key].Release(go);
        Debug.Log($"[PoolManager] Pool registrado: {key} ({initial}/{max})");
    }

    GameObject CreatePooled(string key)
    {
        if (!prefabs.TryGetValue(key, out var prefab)) return new GameObject($"Pooled_{key}");
        var go = Instantiate(prefab, poolRoot);
        go.name = prefab.name + "(Pooled)";
        // Añadir PooledObject para auto-release si se destruye
        var pooled = go.GetComponent<PooledObject>();
        if (pooled == null) pooled = go.AddComponent<PooledObject>();
        pooled.poolKey = key;
        return go;
    }

    public GameObject Get(string key, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogWarning($"[PoolManager] Get {key} sin pool registrado, Instantiate directo");
            if (prefabs.TryGetValue(key, out var pf))
                return Instantiate(pf, pos, rot, parent);
            return null;
        }
        var go = pools[key].Get();
        go.transform.SetPositionAndRotation(pos, rot);
        if (parent != null) go.transform.SetParent(parent, true);
        else go.transform.SetParent(poolRoot, true);
        // Si tiene Rigidbody, reset velocidad
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        return go;
    }

    public void Release(string key, GameObject go)
    {
        if (go == null) return;
        if (!pools.ContainsKey(key))
        {
            Destroy(go);
            return;
        }
        // Reset parent
        go.transform.SetParent(poolRoot);
        pools[key].Release(go);
    }

    // Helper para VFX con auto-release tras tiempo
    public GameObject GetVFX(string key, Vector3 pos, Quaternion rot, float autoRelease = 0.5f)
    {
        var go = Get(key, pos, rot, null);
        if (go != null && autoRelease > 0)
        {
            var pooled = go.GetComponent<PooledObject>();
            if (pooled != null) pooled.ScheduleRelease(autoRelease);
            else Destroy(go, autoRelease); // fallback
        }
        return go;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}


