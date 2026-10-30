using System;
using UnityEngine;

/// <summary>
/// Shrinks the Statue of Liberty play area to the star platform so players and spawns
/// stay off the bush-covered island.
/// </summary>
[DefaultExecutionOrder(50)]
public sealed class EstatuaPlayAreaLimiter : MonoBehaviour
{
    private const string StarObjectName = "Base.001";
    private const string PedestalObjectName = "Base";
    private const string StatueObjectName = "Statue";
    private const string LogPrefix = "[EstatuaPlayAreaLimiter]";
    private const float DefaultPaddingMeters = 1f;
    private const float DefaultSpawnInsetMeters = 4f;
    private const float MinimumHalfExtentMeters = 8f;

    [SerializeField] private string starObjectName = StarObjectName;
    [SerializeField] private float playAreaPaddingMeters = DefaultPaddingMeters;
    [SerializeField] private float spawnInsetMeters = DefaultSpawnInsetMeters;
    [Tooltip("Si está activo, al Play las SpawnZone se van a los bordes de la estrella. Desactivalo para dejarlas donde las pusiste.")]
    [SerializeField] private bool relocateSpawnZonesOnPlay = true;

    private void Start()
    {
        Bounds playArea = ComputePlayArea();
        ReplaceBoundaryWalls(playArea);
        if (relocateSpawnZonesOnPlay)
        {
            RelocateSpawnZones(playArea);
        }

        ClampEnemySpawnRadius(playArea);
    }

    private Bounds ComputePlayArea()
    {
        Transform star = FindChildByName(transform, starObjectName);
        Bounds bounds = star != null
            ? EncapsulateRenderers(star)
            : EncapsulateNamedMeshes(StatueObjectName, PedestalObjectName);
        bounds.Expand(new Vector3(playAreaPaddingMeters * 2f, 0f, playAreaPaddingMeters * 2f));
        float halfX = Mathf.Max(bounds.extents.x, MinimumHalfExtentMeters);
        float halfZ = Mathf.Max(bounds.extents.z, MinimumHalfExtentMeters);
        return new Bounds(
            new Vector3(bounds.center.x, bounds.center.y, bounds.center.z),
            new Vector3(halfX * 2f, bounds.size.y, halfZ * 2f));
    }

    private static void ReplaceBoundaryWalls(Bounds playArea)
    {
        GameObject previous = GameObject.Find(TestSceneArenaBuilder.BoundaryRootName);
        if (previous != null)
        {
            Destroy(previous);
        }

        TestSceneArenaBuilder.CreateBoundaryWalls(
            new Vector3(playArea.center.x, 0f, playArea.center.z),
            playArea.extents.x,
            playArea.extents.z);
    }

    private void RelocateSpawnZones(Bounds playArea)
    {
        SpawnZone[] zones = FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
        if (zones.Length == 0)
        {
            return;
        }

        float y = zones[0].transform.position.y;
        for (int i = 0; i < zones.Length; i++)
        {
            SpawnZone zone = zones[i];
            float insetX = Mathf.Max(spawnInsetMeters, zone.SizeMeters.x * 0.5f);
            float insetZ = Mathf.Max(spawnInsetMeters, zone.SizeMeters.y * 0.5f);
            float minX = playArea.min.x + insetX;
            float maxX = playArea.max.x - insetX;
            float minZ = playArea.min.z + insetZ;
            float maxZ = playArea.max.z - insetZ;
            if (minX > maxX)
            {
                minX = playArea.center.x;
                maxX = playArea.center.x;
            }

            if (minZ > maxZ)
            {
                minZ = playArea.center.z;
                maxZ = playArea.center.z;
            }

            zone.transform.position = SpawnPositionForZone(zone.name, y, minX, maxX, minZ, maxZ, playArea.center);
        }
    }

    private static Vector3 SpawnPositionForZone(
        string zoneName,
        float y,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        Vector3 center)
    {
        bool north = zoneName.IndexOf("Norte", StringComparison.Ordinal) >= 0;
        bool south = zoneName.IndexOf("Sur", StringComparison.Ordinal) >= 0;
        bool east = zoneName.IndexOf("Este", StringComparison.Ordinal) >= 0;
        bool west = zoneName.IndexOf("Oeste", StringComparison.Ordinal) >= 0;
        bool left = zoneName.IndexOf("Izq", StringComparison.Ordinal) >= 0;
        bool right = zoneName.IndexOf("Der", StringComparison.Ordinal) >= 0;

        float x = center.x;
        float z = center.z;
        if (north)
        {
            z = maxZ;
        }
        else if (south)
        {
            z = minZ;
        }

        if (west)
        {
            x = minX;
        }
        else if (east)
        {
            x = maxX;
        }
        else if (left)
        {
            x = minX;
        }
        else if (right)
        {
            x = maxX;
        }

        return new Vector3(x, y, z);
    }

    private void ClampEnemySpawnRadius(Bounds playArea)
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            return;
        }

        float half = Mathf.Min(playArea.extents.x, playArea.extents.z);
        spawner.radioSpawn = Mathf.Max(MinimumHalfExtentMeters, half - spawnInsetMeters);
    }

    private Bounds EncapsulateNamedMeshes(params string[] names)
    {
        Bounds? bounds = null;
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = FindChildByName(transform, names[i]);
            if (child == null)
            {
                continue;
            }

            Bounds childBounds = EncapsulateRenderers(child);
            if (bounds == null)
            {
                bounds = childBounds;
            }
            else
            {
                Bounds current = bounds.Value;
                current.Encapsulate(childBounds);
                bounds = current;
            }
        }

        return bounds ?? new Bounds(transform.position, Vector3.one * (MinimumHalfExtentMeters * 2f));
    }

    private static Bounds EncapsulateRenderers(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
