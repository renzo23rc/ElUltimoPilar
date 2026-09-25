using UltimoPilar.Core.Shared;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the procedural shot feedback (tracers, impacts, explosions, muzzle flashes).
/// Every material it creates is released together with its short-lived object.
/// </summary>
public sealed class WeaponFeedbackVfx
{
    private const float TracerStartWidthMeters = 0.025f;
    private const float TracerEndWidthMeters = 0.015f;
    private const float TracerLifetimeSeconds = 0.12f;
    private const int TracerCapVertices = 4;
    private const int TracerCornerVertices = 2;
    private const float ImpactOffsetMeters = 0.05f;
    private const float ImpactHitDurationSeconds = 0.35f;
    private const float ImpactMissDurationSeconds = 0.2f;
    private const float ImpactEmissionMultiplier = 1.5f;
    private const float ImpactRingOffsetMeters = 0.02f;
    private const float ImpactRingScale = 1.8f;
    private const float ImpactRingThicknessMeters = 0.02f;
    private const float ImpactRingLifetimeSeconds = 0.25f;
    private const float ExplosionOffsetMeters = 0.05f;
    private const float ExplosionInitialScaleMeters = 0.2f;
    private const float ExplosionAlpha = 0.35f;
    private const float ExplosionDurationSeconds = 0.45f;
    private const float ShockwaveOffsetMeters = 0.02f;
    private const float ShockwaveInitialScaleMeters = 0.5f;
    private const float ShockwaveThicknessMeters = 0.02f;
    private const float MuzzleFlashOffsetMeters = 0.3f;
    private const float MuzzleFlashSizeMeters = 0.18f;
    private const float MuzzleFlashLifetimeSeconds = 0.06f;
    private const float MuzzleFlashEmissionMultiplier = 2f;

    /// <summary>Draws a short-lived line between two points.</summary>
    public void CreateTracer(Vector3 start, Vector3 end, Color color)
    {
        var tracer = new GameObject("Trazador");
        var line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = TracerStartWidthMeters;
        line.endWidth = TracerEndWidthMeters;
        line.numCapVertices = TracerCapVertices;
        line.numCornerVertices = TracerCornerVertices;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        OwnedMaterialCleanup.Assign(line, RuntimeMaterialFactory.CreateUnlit(color));
        Object.Destroy(tracer, TracerLifetimeSeconds);
    }

    /// <summary>Spawns an impact marker; hits also get a ring aligned with the surface.</summary>
    public void CreateImpact(Vector3 position, Vector3 normal, Color color, float sizeMeters, bool isHit)
    {
        GameObject marker = CreateVisualPrimitive(
            PrimitiveType.Sphere,
            isHit ? "Impacto_HIT" : "Impacto_MISS",
            RuntimeMaterialFactory.CreateLit(color, ImpactEmissionMultiplier));
        marker.transform.position = position + normal * ImpactOffsetMeters;
        marker.transform.localScale = Vector3.one * sizeMeters;
        marker.AddComponent<ImpactoAnim>().Init(isHit ? ImpactHitDurationSeconds : ImpactMissDurationSeconds, isHit);

        if (!isHit)
        {
            return;
        }

        GameObject ring = CreateVisualPrimitive(PrimitiveType.Cylinder, "AnilloImpacto", RuntimeMaterialFactory.CreateLit(Color.white));
        ring.transform.position = position + normal * ImpactRingOffsetMeters;
        ring.transform.localScale = new Vector3(sizeMeters * ImpactRingScale, ImpactRingThicknessMeters, sizeMeters * ImpactRingScale);
        ring.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
        Object.Destroy(ring, ImpactRingLifetimeSeconds);
    }

    /// <summary>Spawns the expanding explosion sphere and ground shockwave of the area weapon.</summary>
    public void CreateAreaExplosion(Vector3 position, float radiusMeters, Color color)
    {
        var sphereColor = new Color(color.r, color.g, color.b, ExplosionAlpha);
        GameObject sphere = CreateVisualPrimitive(PrimitiveType.Sphere, "ExplosionArea", RuntimeMaterialFactory.CreateLit(sphereColor));
        sphere.transform.position = position + Vector3.up * ExplosionOffsetMeters;
        sphere.transform.localScale = Vector3.one * ExplosionInitialScaleMeters;
        sphere.AddComponent<ExplosionAnim>().Init(radiusMeters, ExplosionDurationSeconds, color);

        GameObject shockwave = CreateVisualPrimitive(PrimitiveType.Cylinder, "OndaArea", RuntimeMaterialFactory.CreateLit(color));
        shockwave.transform.position = position + Vector3.up * ShockwaveOffsetMeters;
        shockwave.transform.localScale = new Vector3(ShockwaveInitialScaleMeters, ShockwaveThicknessMeters, ShockwaveInitialScaleMeters);
        shockwave.AddComponent<OndaAreaAnim>().Init(radiusMeters * 2f, ExplosionDurationSeconds);
    }

    /// <summary>Spawns a brief glow in front of the muzzle.</summary>
    public void CreateMuzzleFlash(Transform muzzle, Color color)
    {
        if (muzzle == null)
        {
            return;
        }

        GameObject flash = CreateVisualPrimitive(
            PrimitiveType.Sphere,
            "FlashBoca",
            RuntimeMaterialFactory.CreateLit(color, MuzzleFlashEmissionMultiplier));
        flash.transform.position = muzzle.position + muzzle.forward * MuzzleFlashOffsetMeters;
        flash.transform.localScale = Vector3.one * MuzzleFlashSizeMeters;
        Object.Destroy(flash, MuzzleFlashLifetimeSeconds);
    }

    private static GameObject CreateVisualPrimitive(PrimitiveType type, string objectName, Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(type);
        visual.name = objectName;
        // Solo visual: el collider no debe bloquear disparos ni movimiento.
        Object.Destroy(visual.GetComponent<Collider>());
        OwnedMaterialCleanup.Assign(visual.GetComponent<Renderer>(), material);
        return visual;
    }
}
