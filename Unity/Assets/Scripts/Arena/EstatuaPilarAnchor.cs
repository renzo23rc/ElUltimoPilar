using System;
using UltimoPilar.Arena;
using UnityEngine;

/// <summary>
/// Moves the Statue of Liberty Pilar onto the statue mesh, centers the map on it,
/// and aligns the walk plane and turrets to the grass and star mesh tops.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class EstatuaPilarAnchor : MonoBehaviour
{
    private const string StatueObjectName = "Statue";
    private const string PedestalObjectName = "Base";
    private const string TorchObjectName = "Torch";
    private const string IslandObjectName = "isla";
    private const string StarObjectName = "Base.001";
    private const string TurretPointPrefix = "PuntoTorreta_";
    private const string PilarLightName = "LuzPilar";
    private const int TurretPointCount = 4;
    private const float TurretRadiusPadding = 1.25f;
    private const float CircleRadians = Mathf.PI * 2f;
    private const string LogPrefix = "[EstatuaPilarAnchor]";
    private const float DefaultPitRadiusPaddingMeters = 0.75f;

    [SerializeField] private string statueObjectName = StatueObjectName;
    [SerializeField] private float turretHeightMeters = 2.2f;
    [SerializeField] private float turretClearanceOutsidePitMeters = 1.5f;
    [Tooltip("Radio del pozo en esta escena, en metros. 0 lo ajusta al pedestal.")]
    [SerializeField] private float pitRadiusMeters;
    [SerializeField] private float pitRadiusPaddingMeters = DefaultPitRadiusPaddingMeters;
    [Tooltip("Si está activo, al Play se regeneran PuntoTorreta_0..3. Desactivalo para colocarlas a mano.")]
    [SerializeField] private bool rebuildTurretPointsOnPlay = true;

    private float resolvedPitRadiusMeters = TestSceneArenaBuilder.DefaultPitRadiusMeters;

    private void Awake()
    {
        Transform statue = FindChildByName(transform, statueObjectName);
        if (statue == null)
        {
            Debug.LogError($"{LogPrefix} No se encontró '{statueObjectName}' en el mapa.");
            return;
        }

        Vector3 mapShift = CenterMapOnMonument(statue);
        ShiftSpawnZones(mapShift);
        Pilar pilar = EnsurePilarOnStatue(statue);
        FitPilarCollider(statue);
        EstatuaSurfaceHeights heights = MeasureSurfaceHeights();
        turretHeightMeters = heights.TurretY;
        resolvedPitRadiusMeters = ResolvePitRadius();
        AlignGeneratedArena(heights);
        AlignSpawnZoneHeights(heights.WalkSurfaceY);
        EnsureStarCollider();
        if (rebuildTurretPointsOnPlay || !TryBindExistingTurretPoints(statue, pilar))
        {
            PlaceTurretPoints(statue, pilar);
        }

        PlacePilarLight(statue);
    }

    private EstatuaSurfaceHeights MeasureSurfaceHeights()
    {
        float grassTopY = MeshTopY(IslandObjectName, 0f);
        float starTopY = MeshTopY(StarObjectName, grassTopY);
        float pedestalBottomY = MeshBottomY(PedestalObjectName, starTopY);
        float platformY = pedestalBottomY > starTopY ? pedestalBottomY : starTopY;
        return EstatuaSurfaceHeights.FromMeshTops(
            grassTopY,
            platformY,
            TestScenePlayerBuilder.ControllerHalfHeightMeters,
            EstatuaSurfaceHeights.DefaultTurretPivotAboveSurfaceMeters);
    }

    private float MeshTopY(string objectName, float fallbackY)
    {
        return MeshBoundY(objectName, fallbackY, useMaximum: true);
    }

    private float MeshBottomY(string objectName, float fallbackY)
    {
        return MeshBoundY(objectName, fallbackY, useMaximum: false);
    }

    private float MeshBoundY(string objectName, float fallbackY, bool useMaximum)
    {
        Transform child = FindChildByName(transform, objectName);
        if (child == null)
        {
            return fallbackY;
        }

        Bounds bounds = EncapsulateRenderers(child);
        return useMaximum ? bounds.max.y : bounds.min.y;
    }

    private void AlignGeneratedArena(EstatuaSurfaceHeights heights)
    {
        TestSceneSetup setup = FindFirstObjectByType<TestSceneSetup>();
        if (setup == null)
        {
            return;
        }

        setup.AlignGeneratedSurface(heights.WalkSurfaceY, heights.PlayerSpawnY, heights.HazardY);
        setup.AlignPitRadius(resolvedPitRadiusMeters);
    }

    private float ResolvePitRadius()
    {
        Transform pedestal = FindChildByName(transform, PedestalObjectName);
        float pedestalHalfExtentMeters = 0f;
        if (pedestal != null)
        {
            Bounds pedestalBounds = EncapsulateRenderers(pedestal);
            pedestalHalfExtentMeters = Mathf.Max(pedestalBounds.extents.x, pedestalBounds.extents.z);
        }

        return EstatuaSurfaceHeights.ResolvePitRadius(
            pitRadiusMeters,
            pedestalHalfExtentMeters,
            pitRadiusPaddingMeters,
            TestSceneArenaBuilder.DefaultPitRadiusMeters);
    }

    private static void AlignSpawnZoneHeights(float walkSurfaceY)
    {
        SpawnZone[] zones = FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            Vector3 position = zones[i].transform.position;
            position.y = walkSurfaceY;
            zones[i].transform.position = position;
        }
    }

    private void EnsureStarCollider()
    {
        Transform star = FindChildByName(transform, StarObjectName);
        if (star == null || !star.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
        {
            return;
        }

        if (star.GetComponent<MeshCollider>() != null)
        {
            return;
        }

        MeshCollider collider = star.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = meshFilter.sharedMesh;
    }

    private Vector3 CenterMapOnMonument(Transform statue)
    {
        Bounds monument = EncapsulateNamedMeshes(transform, statue);
        Vector3 shift = new Vector3(-monument.center.x, 0f, -monument.center.z);
        transform.position += shift;
        return shift;
    }

    private static void ShiftSpawnZones(Vector3 shift)
    {
        if (shift == Vector3.zero)
        {
            return;
        }

        SpawnZone[] zones = FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            zones[i].transform.position += shift;
        }
    }

    private Pilar EnsurePilarOnStatue(Transform statue)
    {
        Pilar onStatue = statue.GetComponent<Pilar>();
        Pilar onRoot = GetComponent<Pilar>();
        if (onStatue != null)
        {
            if (onRoot != null && onRoot != onStatue)
            {
                CopyPilar(onRoot, onStatue);
                DestroyImmediate(onRoot);
            }

            return onStatue;
        }

        onStatue = statue.gameObject.AddComponent<Pilar>();
        if (onRoot != null)
        {
            CopyPilar(onRoot, onStatue);
            DestroyImmediate(onRoot);
        }

        if (TryGetComponent(out BoxCollider rootCollider))
        {
            DestroyImmediate(rootCollider);
        }

        return onStatue;
    }

    private static void CopyPilar(Pilar source, Pilar destination)
    {
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), destination);
        destination.puntosTorretas = source.puntosTorretas;
        destination.prefabTorreta = source.prefabTorreta;
    }

    private static void FitPilarCollider(Transform statue)
    {
        if (!statue.TryGetComponent(out BoxCollider boxCollider))
        {
            boxCollider = statue.gameObject.AddComponent<BoxCollider>();
        }

        if (statue.TryGetComponent(out MeshRenderer meshRenderer))
        {
            boxCollider.center = meshRenderer.localBounds.center;
            boxCollider.size = meshRenderer.localBounds.size;
            return;
        }

        Bounds worldBounds = EncapsulateRenderers(statue);
        boxCollider.center = statue.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = statue.InverseTransformVector(worldBounds.size);
        boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private void PlaceTurretPoints(Transform statue, Pilar pilar)
    {
        ClearChildrenWithPrefix(transform, TurretPointPrefix);
        ClearChildrenWithPrefix(statue, TurretPointPrefix);
        Bounds monument = EncapsulateNamedMeshes(transform, statue);
        float radiusAroundMonument = Mathf.Max(monument.extents.x, monument.extents.z) * TurretRadiusPadding;
        float radiusOutsidePit = resolvedPitRadiusMeters + turretClearanceOutsidePitMeters;
        float radius = Mathf.Max(radiusAroundMonument, radiusOutsidePit);
        var points = new Transform[TurretPointCount];
        for (int i = 0; i < TurretPointCount; i++)
        {
            var point = new GameObject($"{TurretPointPrefix}{i}");
            float angle = (i / (float)TurretPointCount) * CircleRadians;
            Vector3 worldPosition = new Vector3(
                monument.center.x + (Mathf.Cos(angle) * radius),
                turretHeightMeters,
                monument.center.z + (Mathf.Sin(angle) * radius));
            point.transform.SetParent(statue, true);
            point.transform.position = worldPosition;
            point.transform.rotation = Quaternion.identity;
            points[i] = point.transform;
        }

        pilar.puntosTorretas = points;
    }

    private bool TryBindExistingTurretPoints(Transform statue, Pilar pilar)
    {
        var points = new Transform[TurretPointCount];
        for (int i = 0; i < TurretPointCount; i++)
        {
            string pointName = $"{TurretPointPrefix}{i}";
            Transform point = FindChildByName(statue, pointName) ?? FindChildByName(transform, pointName);
            if (point == null)
            {
                return false;
            }

            points[i] = point;
            Vector3 worldPosition = point.position;
            worldPosition.y = turretHeightMeters;
            point.position = worldPosition;
        }

        pilar.puntosTorretas = points;
        return true;
    }

    private void PlacePilarLight(Transform statue)
    {
        Transform lightTransform = FindChildByName(transform, PilarLightName);
        if (lightTransform == null)
        {
            return;
        }

        Bounds monument = EncapsulateNamedMeshes(transform, statue);
        lightTransform.SetParent(statue, true);
        lightTransform.position = new Vector3(monument.center.x, monument.max.y, monument.center.z);
    }

    private static Bounds EncapsulateNamedMeshes(Transform root, Transform statue)
    {
        Bounds bounds = EncapsulateRenderers(statue);
        EncapsulateIfPresent(root, PedestalObjectName, ref bounds);
        EncapsulateIfPresent(root, TorchObjectName, ref bounds);
        return bounds;
    }

    private static void EncapsulateIfPresent(Transform root, string name, ref Bounds bounds)
    {
        Transform child = FindChildByName(root, name);
        if (child == null)
        {
            return;
        }

        Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
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

    private static void ClearChildrenWithPrefix(Transform parent, string prefix)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith(prefix, StringComparison.Ordinal))
            {
                DestroyImmediate(child.gameObject);
            }
        }
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
