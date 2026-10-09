/**
 * DecoyBeacon.cs
 * Punto de atracción temporal creado por la variante Lanzador de señuelo.
 * Atrae a los enemigos dentro de su radio sin dañarlos y se destruye al expirar.
 */
using System.Collections.Generic;
using UltimoPilar.Core.Shared;
using UnityEngine;

/// <summary>Temporary lure point that attracts nearby enemies without damaging them.</summary>
public class DecoyBeacon : MonoBehaviour
{
    private const string BeaconObjectName = "Señuelo";
    private const float VisualSizeMeters = 0.6f;
    private const float PulseFrequencyHertz = 4f;
    private const float PulseAmplitudeRatio = 0.25f;

    private static readonly List<DecoyBeacon> ActiveBeacons = new List<DecoyBeacon>();
    private static readonly Color VisualColor = new Color(1f, 0.3f, 0.9f);

    private float attractionRadiusMeters;
    private float remainingLifetimeSeconds;

    /// <summary>Gets the radius within which enemies are attracted.</summary>
    public float AttractionRadiusMeters => attractionRadiusMeters;

    /// <summary>Creates a decoy beacon at the requested position.</summary>
    /// <param name="position">The world position of the lure point.</param>
    /// <param name="attractionRadius">The attraction radius in meters.</param>
    /// <param name="lifetimeSeconds">The lifetime in seconds.</param>
    /// <returns>The created beacon.</returns>
    public static DecoyBeacon Spawn(Vector3 position, float attractionRadius, float lifetimeSeconds)
    {
        GameObject beaconObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beaconObject.name = BeaconObjectName;
        Destroy(beaconObject.GetComponent<Collider>());
        beaconObject.transform.position = position;
        beaconObject.transform.localScale = Vector3.one * VisualSizeMeters;
        MaterialColorHelper.SetBaseAndEmissionColor(beaconObject.GetComponent<Renderer>().material, VisualColor);

        var beacon = beaconObject.AddComponent<DecoyBeacon>();
        beacon.attractionRadiusMeters = Mathf.Max(0f, attractionRadius);
        beacon.remainingLifetimeSeconds = Mathf.Max(0f, lifetimeSeconds);
        return beacon;
    }

    /// <summary>Finds the closest active beacon whose radius contains the position.</summary>
    /// <param name="position">The position of the enemy being lured.</param>
    /// <param name="decoyPosition">The position of the closest beacon, when found.</param>
    /// <returns><see langword="true"/> when an active beacon attracts the position.</returns>
    public static bool TryGetAttractingDecoy(Vector3 position, out Vector3 decoyPosition)
    {
        decoyPosition = Vector3.zero;
        float closestDistance = float.MaxValue;
        bool found = false;
        foreach (DecoyBeacon beacon in ActiveBeacons)
        {
            if (beacon == null)
            {
                continue;
            }

            float distance = Vector3.Distance(position, beacon.transform.position);
            if (distance <= beacon.attractionRadiusMeters && distance < closestDistance)
            {
                closestDistance = distance;
                decoyPosition = beacon.transform.position;
                found = true;
            }
        }

        return found;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        ActiveBeacons.Clear();
    }

    private void OnEnable()
    {
        ActiveBeacons.Add(this);
    }

    private void OnDisable()
    {
        ActiveBeacons.Remove(this);
    }

    private void Update()
    {
        remainingLifetimeSeconds -= Time.deltaTime;
        if (remainingLifetimeSeconds <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + (Mathf.Sin(Time.time * PulseFrequencyHertz * 2f * Mathf.PI) * PulseAmplitudeRatio);
        transform.localScale = Vector3.one * (VisualSizeMeters * pulse);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = VisualColor;
        Gizmos.DrawWireSphere(transform.position, attractionRadiusMeters);
    }
}
