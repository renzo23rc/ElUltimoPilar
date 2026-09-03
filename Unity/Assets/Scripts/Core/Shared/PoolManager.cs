using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Manages reusable pools of Unity game objects.
/// </summary>
public class PoolManager : MonoBehaviour
{
    private const int DefaultInitialPoolSize = 10;
    private const int DefaultMaximumPoolSize = 50;
    private const string PoolRootName = "PoolRoot";
    private const string PooledObjectNameFormat = "Pooled_{0}";
    private const string PooledObjectSuffix = "(Pooled)";

    /// <summary>
    /// Gets the active pool manager instance.
    /// </summary>
    public static PoolManager Instance { get; private set; }

    private readonly Dictionary<string, ObjectPool<GameObject>> pools =
        new Dictionary<string, ObjectPool<GameObject>>();
    private readonly Dictionary<string, GameObject> prefabs =
        new Dictionary<string, GameObject>();
    private Transform poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        poolRoot = new GameObject(PoolRootName).transform;
        poolRoot.SetParent(transform);
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Registers a reusable pool for the specified prefab.
    /// </summary>
    /// <param name="key">The unique pool key.</param>
    /// <param name="prefab">The prefab to instantiate for the pool.</param>
    /// <param name="initial">The number of objects to pre-warm.</param>
    /// <param name="max">The maximum number of inactive objects retained.</param>
    public void RegisterPool(
        string key,
        GameObject prefab,
        int initial = DefaultInitialPoolSize,
        int max = DefaultMaximumPoolSize)
    {
        if (pools.ContainsKey(key))
        {
            return;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[PoolManager] RegisterPool {key} prefab null, se omitió registro");
            return;
        }

        prefabs[key] = prefab;
        pools[key] = new ObjectPool<GameObject>(
            createFunc: () => CreatePooled(key),
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: initial,
            maxSize: max);

        List<GameObject> warm = new List<GameObject>();
        for (int i = 0; i < initial; i++)
        {
            GameObject gameObject = pools[key].Get();
            warm.Add(gameObject);
        }

        foreach (GameObject gameObject in warm)
        {
            pools[key].Release(gameObject);
        }

        Debug.Log($"[PoolManager] Pool registrado: {key} ({initial}/{max})");
    }

    private GameObject CreatePooled(string key)
    {
        if (!prefabs.TryGetValue(key, out GameObject prefab))
        {
            return new GameObject(string.Format(PooledObjectNameFormat, key));
        }

        GameObject gameObject = Instantiate(prefab, poolRoot);
        gameObject.name = prefab.name + PooledObjectSuffix;
        PooledObject pooledObject = gameObject.GetComponent<PooledObject>();
        if (pooledObject == null)
        {
            pooledObject = gameObject.AddComponent<PooledObject>();
        }

        pooledObject.poolKey = key;
        return gameObject;
    }

    /// <summary>
    /// Gets an object from a registered pool at the specified transform.
    /// </summary>
    /// <param name="key">The pool key.</param>
    /// <param name="pos">The world position.</param>
    /// <param name="rot">The world rotation.</param>
    /// <param name="parent">The optional parent transform.</param>
    /// <returns>A pooled object, a direct instance for a known unregistered prefab, or null.</returns>
    public GameObject Get(string key, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogWarning($"[PoolManager] Get {key} sin pool registrado, Instantiate directo");
            if (prefabs.TryGetValue(key, out GameObject prefab))
            {
                return Instantiate(prefab, pos, rot, parent);
            }

            return null;
        }

        GameObject gameObject = pools[key].Get();
        gameObject.transform.SetPositionAndRotation(pos, rot);
        if (parent != null)
        {
            gameObject.transform.SetParent(parent, true);
        }
        else
        {
            gameObject.transform.SetParent(poolRoot, true);
        }

        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        return gameObject;
    }

    /// <summary>
    /// Releases an object to its registered pool.
    /// </summary>
    /// <param name="key">The pool key.</param>
    /// <param name="go">The object to release.</param>
    public void Release(string key, GameObject go)
    {
        if (go == null)
        {
            return;
        }

        if (!pools.ContainsKey(key))
        {
            Destroy(go);
            return;
        }

        go.transform.SetParent(poolRoot);
        pools[key].Release(go);
    }

    /// <summary>
    /// Gets a visual-effect object and optionally schedules its release.
    /// </summary>
    /// <param name="key">The pool key.</param>
    /// <param name="pos">The world position.</param>
    /// <param name="rot">The world rotation.</param>
    /// <param name="autoRelease">The release delay in seconds.</param>
    /// <returns>A pooled object or null when the pool and prefab are unavailable.</returns>
    public GameObject GetVFX(string key, Vector3 pos, Quaternion rot, float autoRelease = 0.5f)
    {
        GameObject gameObject = Get(key, pos, rot, null);
        if (gameObject != null && autoRelease > 0f)
        {
            PooledObject pooledObject = gameObject.GetComponent<PooledObject>();
            if (pooledObject != null)
            {
                pooledObject.ScheduleRelease(autoRelease);
            }
            else
            {
                Destroy(gameObject, autoRelease);
            }
        }

        return gameObject;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
