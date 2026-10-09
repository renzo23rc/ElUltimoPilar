using System;
using UltimoPilar.Core.Pilar;
using UnityEngine;

/// <summary>
/// Creates the inactive runtime templates the test scene uses when a Resources prefab is missing.
/// Every template is returned inactive, so it never runs gameplay until it is instantiated.
/// </summary>
public static class TestScenePrefabFactory
{
    private const float EnergyColliderRadiusMeters = 0.5f;
    private const float EnergyScale = 0.5f;
    private const float ProjectileDamage = 10f;
    private const float ProjectileLifetimeSeconds = 5f;
    private const float VariantColliderRadiusMeters = 0.5f;
    private const float VariantScale = 0.6f;
    private const float VariantDamageMultiplier = 2f;
    private const float VariantDurationSeconds = 12f;

    /// <summary>Creates the energy orb template.</summary>
    public static GameObject CreateEnergyPickup(Material material)
    {
        GameObject energy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        energy.name = "EnergiaPickup";
        UnityEngine.Object.Destroy(energy.GetComponent<Collider>());
        var collider = energy.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = EnergyColliderRadiusMeters;
        energy.transform.localScale = Vector3.one * EnergyScale;
        if (material != null) energy.GetComponent<Renderer>().material = material;
        else energy.GetComponent<Renderer>().material.color = Color.cyan;
        PickupMotion.EnsureKinematicBody(energy);
        energy.AddComponent<EnergyPickup>();
        return energy;
    }

    /// <summary>Creates the physics projectile template shared by artillery and turrets.</summary>
    public static GameObject CreateProjectile()
    {
        return ProjectileFactory.CreateFallback(
            "ProyectilBase",
            Vector3.zero,
            Quaternion.identity,
            ProjectileDamage,
            ProjectileLifetimeSeconds).gameObject;
    }

    /// <summary>Creates the inactive emergency turret template.</summary>
    public static GameObject CreateTurret()
    {
        GameObject turret = TurretFallbackFactory.Create(Vector3.zero, Quaternion.identity);
        turret.SetActive(false);
        return turret;
    }

    /// <summary>Creates an inactive colored cube enemy of the given <see cref="Enemy"/> subtype.</summary>
    public static GameObject CreateEnemy(string enemyName, Color color, Type enemyType, GameObject energyPrefab)
    {
        GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyObject.name = enemyName;
        enemyObject.GetComponent<Renderer>().material.color = color;
        enemyObject.GetComponent<BoxCollider>().isTrigger = false;
        enemyObject.AddComponent(enemyType);

        // Asignar prefab de energía y modelo visual (evita UnassignedReference).
        if (enemyObject.TryGetComponent(out Enemy enemy))
        {
            enemy.prefabEnergia = energyPrefab;
            if (enemy.modeloVisual == null) enemy.modeloVisual = enemyObject.transform;
        }

        var body = enemyObject.AddComponent<Rigidbody>();
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        enemyObject.SetActive(false);
        return enemyObject;
    }

    /// <summary>Creates the inactive temporary weapon variant pickup template.</summary>
    public static GameObject CreateVariantPickup()
    {
        GameObject variant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        variant.name = "VariantePickup";
        UnityEngine.Object.Destroy(variant.GetComponent<SphereCollider>());
        var collider = variant.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = VariantColliderRadiusMeters;
        variant.transform.localScale = Vector3.one * VariantScale;
        PickupMotion.EnsureKinematicBody(variant);
        var pickup = variant.AddComponent<WeaponVariantPickup>();
        pickup.tipoPotenciado = WeaponSystem.TipoArma.Directa;
        pickup.multiplicadorDaño = VariantDamageMultiplier;
        pickup.duracionSegundos = VariantDurationSeconds;
        variant.SetActive(false);
        return variant;
    }
}
