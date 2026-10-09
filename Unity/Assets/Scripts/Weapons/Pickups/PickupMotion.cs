using UnityEngine;

/// <summary>
/// Idle presentation shared by collectible pickups: spinning around the vertical axis and bobbing.
/// </summary>
public static class PickupMotion
{
    private static readonly float FullCircleRadians = Mathf.PI * 2f;

    /// <summary>Returns a random bob phase so pickups dropped together do not move in sync.</summary>
    public static float RandomPhase()
    {
        return Random.Range(0f, FullCircleRadians);
    }

    /// <summary>Spins the pickup and places it on its bob curve around the base height.</summary>
    /// <param name="pickup">The pickup transform.</param>
    /// <param name="baseHeight">The world height the pickup bobs around.</param>
    /// <param name="phase">The accumulated bob phase in seconds.</param>
    /// <param name="spinDegreesPerSecond">The spin speed.</param>
    /// <param name="bobFrequency">The bob angular speed.</param>
    /// <param name="bobHeightMeters">The bob amplitude.</param>
    public static void SpinAndBob(
        Transform pickup,
        float baseHeight,
        float phase,
        float spinDegreesPerSecond,
        float bobFrequency,
        float bobHeightMeters)
    {
        pickup.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime);
        float y = baseHeight + Mathf.Sin(phase * bobFrequency) * bobHeightMeters;
        pickup.position = new Vector3(pickup.position.x, y, pickup.position.z);
    }

    /// <summary>Adds a kinematic, gravity-free rigidbody so triggers fire against CharacterControllers.</summary>
    public static void EnsureKinematicBody(GameObject pickup)
    {
        if (!pickup.TryGetComponent(out Rigidbody body))
        {
            body = pickup.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;
    }
}
