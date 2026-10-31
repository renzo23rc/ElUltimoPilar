/**
 * RocketProjectile.cs
 * Proyectil físico del Lanzador de Área (bazooka).
 * Vuela con Rigidbody, detecta el primer impacto y detona:
 *   - Aplica daño en área a todos los enemigos dentro del radio.
 *   - Instancia el prefab de explosión (visual).
 *   - Se destruye a sí mismo.
 *
 * Colocar este componente en el prefab del cohete junto con un Rigidbody
 * (useGravity = false, Continuous Dynamic) y un Collider en modo Trigger.
 */
using System.Collections.Generic;
using UltimoPilar.Core.Shared;
using UnityEngine;

/// <summary>
/// Projectile fired by the player's area weapon. Travels in a straight line and
/// detonates on the first physics contact, dealing area damage to all enemies
/// inside <see cref="explosionRadiusMeters"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class RocketProjectile : MonoBehaviour
{
    private const float DefaultLifetimeSeconds = 8f;

    [Header("Configuración de detonación")]
    [SerializeField, Min(0f)] private float explosionRadiusMeters = 5.5f;
    [SerializeField, Min(0f)] private float damage = 42f;

    [Header("Visual")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField, Min(0f)] private float explosionPrefabLifetimeSeconds = 3f;

    private bool detonated;

    /// <summary>
    /// Configures damage and explosion radius from <see cref="WeaponSystem"/> before launch.
    /// </summary>
    public void Configure(float damageAmount, float radiusMeters, GameObject explosion)
    {
        damage = damageAmount;
        explosionRadiusMeters = radiusMeters;
        if (explosion != null)
        {
            explosionPrefab = explosion;
        }

        Destroy(gameObject, DefaultLifetimeSeconds);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (detonated)
        {
            return;
        }

        // Ignore trigger volumes (zones, pickups…) and self.
        if (other.isTrigger)
        {
            return;
        }

        // Ignore the player who fired this rocket.
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            return;
        }

        Detonate(transform.position);
    }

    private void Detonate(Vector3 position)
    {
        detonated = true;

        ApplyAreaDamage(position);
        SpawnExplosion(position);

        Destroy(gameObject);
    }

    private void ApplyAreaDamage(Vector3 position)
    {
        List<Enemy> enemies = OverlapQuery.FindUniqueInSphere<Enemy>(position, explosionRadiusMeters);
        foreach (Enemy enemy in enemies)
        {
            ((IDamageable)enemy).ReceiveDamage(new DamageRequest(damage));
        }
    }

    private void SpawnExplosion(Vector3 position)
    {
        if (explosionPrefab == null)
        {
            return;
        }

        GameObject vfx = Instantiate(explosionPrefab, position, Quaternion.identity);
        Destroy(vfx, explosionPrefabLifetimeSeconds);
    }
}
