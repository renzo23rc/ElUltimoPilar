using System.Collections.Generic;
using UnityEngine;

namespace UltimoPilar.Core.Pilar
{
    /// <summary>Creates, configures, and cleans up Pilar emergency turrets.</summary>
    public sealed class PilarTurretSpawner
{
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
        Vector3 position = new Vector3(point.position.x, spawnHeightMeters, point.position.z);
        return TurretFallbackFactory.Create(position, point.rotation);
    }
}
}
