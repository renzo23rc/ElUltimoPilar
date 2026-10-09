using UnityEngine;

/// <summary>Finds or creates the player muzzle point shared by the controller and the weapon system.</summary>
public static class MuzzleTransformResolver
{
    /// <summary>The name of the muzzle child object.</summary>
    public const string MuzzleObjectName = "PuntoDisparo";

    /// <summary>The muzzle height above the player center: the hands, not the head.</summary>
    public const float HandHeightMeters = 0.1f;

    /// <summary>The muzzle distance in front of the player center.</summary>
    public const float HandForwardMeters = 0.5f;

    /// <summary>Gets whether the candidate is a usable muzzle distinct from the camera.</summary>
    /// <param name="candidate">The candidate muzzle.</param>
    /// <param name="camera">The player camera.</param>
    /// <returns><see langword="true"/> when the candidate can be used as muzzle.</returns>
    public static bool IsUsable(Transform candidate, Camera camera)
    {
        return candidate != null && (camera == null || candidate != camera.transform);
    }

    /// <summary>Returns the existing muzzle under the owner or camera, creating one when missing.</summary>
    /// <param name="owner">The player root transform.</param>
    /// <param name="camera">The player camera; without it the owner itself is returned.</param>
    /// <returns>The muzzle transform.</returns>
    public static Transform Ensure(Transform owner, Camera camera)
    {
        if (camera == null)
        {
            return owner;
        }

        Transform existing = owner.Find(MuzzleObjectName);
        if (existing == null)
        {
            existing = camera.transform.Find(MuzzleObjectName);
        }

        if (existing != null)
        {
            return existing;
        }

        var muzzle = new GameObject(MuzzleObjectName);
        muzzle.transform.SetParent(owner);
        muzzle.transform.localPosition = new Vector3(0f, HandHeightMeters, HandForwardMeters);
        muzzle.transform.rotation = camera.transform.rotation;
        muzzle.transform.localScale = Vector3.one;
        return muzzle.transform;
    }
}
