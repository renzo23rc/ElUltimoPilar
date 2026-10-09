using UltimoPilar.Core.Shared;
using UnityEngine;

/// <summary>
/// Builds the procedural physics projectile used when no projectile prefab is available.
/// </summary>
public static class ProjectileFactory
{
    private const float ProjectileScale = 0.6f;
    private const float ColliderRadiusMeters = 0.5f;
    private const float EmissionMultiplier = 1.2f;
    private const float TrailSeconds = 0.4f;
    private const float TrailStartWidthMeters = 0.25f;
    private const float TrailEndWidthMeters = 0.05f;
    private const float LightRangeMeters = 4f;
    private const float LightIntensity = 2f;
    private static readonly Color ProjectileColor = new Color(1f, 0.5f, 0f);
    private static readonly Color TrailEndColor = new Color(1f, 0.5f, 0f, 0.2f);

    /// <summary>Creates an active glowing projectile with a trigger collider, trail, and light.</summary>
    /// <param name="objectName">The name of the created object.</param>
    /// <param name="position">The world position.</param>
    /// <param name="rotation">The world rotation.</param>
    /// <param name="damage">The damage dealt to non-player targets.</param>
    /// <param name="lifetimeSeconds">The time before the projectile expires.</param>
    /// <returns>The created projectile.</returns>
    public static Projectile CreateFallback(
        string objectName,
        Vector3 position,
        Quaternion rotation,
        float damage,
        float lifetimeSeconds)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = objectName;
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = Vector3.one * ProjectileScale;

        var collider = go.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = ColliderRadiusMeters;

        Material material = RuntimeMaterialFactory.CreateLit(ProjectileColor, EmissionMultiplier);
        OwnedMaterialCleanup.Assign(go.GetComponent<Renderer>(), material);

        var body = go.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = TrailSeconds;
        trail.startWidth = TrailStartWidthMeters;
        trail.endWidth = TrailEndWidthMeters;
        trail.sharedMaterial = material;
        trail.startColor = ProjectileColor;
        trail.endColor = TrailEndColor;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = ProjectileColor;
        light.range = LightRangeMeters;
        light.intensity = LightIntensity;

        var projectile = go.AddComponent<Projectile>();
        projectile.daño = damage;
        projectile.tiempoVida = lifetimeSeconds;
        projectile.destruirAlImpactar = true;
        return projectile;
    }
}
