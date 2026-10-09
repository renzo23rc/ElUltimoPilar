using UnityEngine;

/// <summary>
/// Resolves where a player's shots start and what they hit: the crosshair ray, the muzzle,
/// and a raycast that ignores the shooter's own colliders.
/// </summary>
public static class WeaponAim
{
    private const float CrosshairViewportCenter = 0.5f;
    private const float SelfIgnoreOffsetMeters = 0.05f;
    private const int MaxSelfIgnoreIterations = 3;

    /// <summary>Returns the ray through the screen center, or along the muzzle without a camera.</summary>
    public static Ray GetAimRay(Camera camera, Transform muzzle, Transform owner)
    {
        if (camera != null)
        {
            return camera.ViewportPointToRay(new Vector3(CrosshairViewportCenter, CrosshairViewportCenter, 0f));
        }

        Transform origin = muzzle != null ? muzzle : owner;
        return new Ray(origin.position, origin.forward);
    }

    /// <summary>Returns the world position where shots visually start.</summary>
    public static Vector3 GetMuzzlePosition(Camera camera, Transform muzzle, Transform owner)
    {
        if (muzzle != null)
        {
            return muzzle.position;
        }

        return camera != null ? camera.transform.position : owner.position;
    }

    /// <summary>Raycasts the aim ray, skipping the shooter's body and muzzle.</summary>
    /// <param name="aimRay">The aim ray.</param>
    /// <param name="maxDistance">The weapon range in meters.</param>
    /// <param name="mask">The layers to hit; an empty mask uses the default raycast layers.</param>
    /// <param name="shooter">The shooting player, whose colliders are ignored.</param>
    /// <param name="muzzle">The muzzle transform, also ignored.</param>
    /// <param name="hit">The first valid hit.</param>
    /// <returns><see langword="true"/> when something other than the shooter was hit.</returns>
    public static bool TryRaycast(
        Ray aimRay,
        float maxDistance,
        LayerMask mask,
        PlayerController shooter,
        Transform muzzle,
        out RaycastHit hit)
    {
        hit = default;
        LayerMask effectiveMask = mask.value == 0 ? Physics.DefaultRaycastLayers : mask;
        Ray currentRay = aimRay;
        float remaining = maxDistance;

        for (int i = 0; i < MaxSelfIgnoreIterations; i++)
        {
            if (!Physics.Raycast(currentRay, out RaycastHit candidate, remaining, effectiveMask))
            {
                return false;
            }

            // Ignorar el propio cuerpo y el punto de disparo: el rayo nace dentro del jugador.
            bool isSelf = candidate.collider.GetComponentInParent<PlayerController>() == shooter
                || (muzzle != null && candidate.collider.transform == muzzle);
            if (!isSelf)
            {
                hit = candidate;
                return true;
            }

            float advance = candidate.distance + SelfIgnoreOffsetMeters;
            if (advance >= remaining)
            {
                return false;
            }

            currentRay = new Ray(candidate.point + currentRay.direction * SelfIgnoreOffsetMeters, currentRay.direction);
            remaining -= advance;
        }

        return false;
    }
}
