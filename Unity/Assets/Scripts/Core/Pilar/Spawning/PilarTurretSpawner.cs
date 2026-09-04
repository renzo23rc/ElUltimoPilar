using System.Collections.Generic;
using UnityEngine;
using UltimoPilar.Core.Shared;

namespace UltimoPilar.Core.Pilar
{
    /// <summary>Creates, configures, and cleans up Pilar emergency turrets.</summary>
    public sealed class PilarTurretSpawner
{
    private const float FallbackWidthMeters = 1.4f;
    private const float FallbackHeightMeters = 2.2f;
    private const float DefaultEmissionMultiplier = 1f;
    private const float FallbackColorRed = 1f;
    private const float FallbackColorGreen = 0.85f;
    private const float FallbackColorBlue = 0.1f;
    private const float FallbackLightRangeMeters = 6f;
    private const float FallbackLightIntensity = 2f;
    private const float SpawnPointForwardMeters = 0.8f;
    private const float SpawnPointHeightMeters = 0.6f;
    private const float FirePointHeightMeters = 1.3f;
    private const string FallbackShaderName = "Universal Render Pipeline/Lit";
    private const string LegacyShaderName = "Standard";
    private const string FinalShaderName = "Sprites/Default";

    private readonly List<GameObject> spawnedTurrets = new List<GameObject>();
    private readonly float spawnHeightMeters;
    private readonly float rangeMeters;
    private readonly float fireRatePerSecond;
    private readonly float damage;
    private readonly float health;
    private readonly int ammo;
    private readonly float reloadSeconds;
    private readonly float projectileSpeedMetersPerSecond;

    /// <summary>Creates a spawner with the runtime turret configuration.</summary>
    public PilarTurretSpawner(
        float spawnHeightMeters,
        float rangeMeters,
        float fireRatePerSecond,
        float damage,
        float health,
        int ammo,
        float reloadSeconds,
        float projectileSpeedMetersPerSecond)
    {
        this.spawnHeightMeters = spawnHeightMeters;
        this.rangeMeters = rangeMeters;
        this.fireRatePerSecond = fireRatePerSecond;
        this.damage = damage;
        this.health = health;
        this.ammo = ammo;
        this.reloadSeconds = reloadSeconds;
        this.projectileSpeedMetersPerSecond = projectileSpeedMetersPerSecond;
    }

    /// <summary>Spawns one configured turret at every valid point.</summary>
    public void Spawn(Transform[] points, GameObject prefab)
    {
        if (points == null)
        {
            return;
        }

        foreach (Transform point in points)
        {
            SpawnAtPoint(point, prefab);
        }
    }

    /// <summary>Disables and destroys all turrets created by this spawner.</summary>
    public void Clear()
    {
        foreach (GameObject turret in spawnedTurrets)
        {
            if (turret == null)
            {
                continue;
            }

            turret.SetActive(false);
            Object.Destroy(turret);
        }

        spawnedTurrets.Clear();
    }

    private void SpawnAtPoint(Transform point, GameObject prefab)
    {
        if (point == null)
        {
            return;
        }

        GameObject turret = prefab != null
            ? Object.Instantiate(prefab, point.position, point.rotation)
            : CreateFallback(point);
        ConfigureTurret(turret, point);
        spawnedTurrets.Add(turret);
        Debug.Log($"[Pilar] Torreta spawneada en {turret.transform.position} desde punto {point.name}");
    }

    private void ConfigureTurret(GameObject turret, Transform point)
    {
        turret.name = $"Torreta_{point.name}";
        turret.transform.position = new Vector3(point.position.x, spawnHeightMeters, point.position.z);

        if (!turret.TryGetComponent<Torreta>(out Torreta turretComponent))
        {
            return;
        }

        ConfigureTurretStats(turretComponent);
    }

    private void ConfigureTurretStats(Torreta turretComponent)
    {
        turretComponent.daño = damage;
        turretComponent.rango = rangeMeters;
        turretComponent.cadencia = fireRatePerSecond;
        turretComponent.velocidadProyectil = projectileSpeedMetersPerSecond;
        turretComponent.vidaMaxima = health;
        turretComponent.vidaActual = health;
        turretComponent.municionMaxima = ammo;
        turretComponent.municionActual = ammo;
        turretComponent.tiempoRecarga = reloadSeconds;
    }

    private GameObject CreateFallback(Transform point)
    {
        GameObject turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
        turret.transform.position = new Vector3(point.position.x, spawnHeightMeters, point.position.z);
        turret.transform.rotation = point.rotation;
        turret.transform.localScale = new Vector3(FallbackWidthMeters, FallbackHeightMeters, FallbackWidthMeters);
        ConfigureFallbackVisuals(turret);
        ConfigureFallbackComponent(turret);
        return turret;
    }

    private void ConfigureFallbackVisuals(GameObject turret)
    {
        Color color = new Color(FallbackColorRed, FallbackColorGreen, FallbackColorBlue);
        Renderer renderer = turret.GetComponent<Renderer>();
        Shader shader = Shader.Find(FallbackShaderName) ?? Shader.Find(LegacyShaderName) ?? Shader.Find(FinalShaderName);
        var material = new Material(shader);
        MaterialColorHelper.SetBaseAndEmissionColor(material, color, DefaultEmissionMultiplier);
        renderer.material = material;
        Object.Destroy(turret.GetComponent<BoxCollider>());

        var light = turret.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = FallbackLightRangeMeters;
        light.intensity = FallbackLightIntensity;
    }

    private void ConfigureFallbackComponent(GameObject turret)
    {
        var collider = turret.GetComponent<BoxCollider>() ?? turret.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = Vector3.one;

        Torreta turretComponent = turret.AddComponent<Torreta>();

        GameObject firePoint = new GameObject("PuntoDisparo");
        firePoint.transform.SetParent(turret.transform);
        firePoint.transform.localPosition = Vector3.forward * SpawnPointForwardMeters
            + Vector3.up * FirePointHeightMeters;
        firePoint.transform.localRotation = Quaternion.identity;
        firePoint.transform.localScale = Vector3.one;
        turretComponent.puntoDisparo = firePoint.transform;
    }
}
}
