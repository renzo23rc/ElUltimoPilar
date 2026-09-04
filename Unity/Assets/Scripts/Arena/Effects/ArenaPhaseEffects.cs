using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltimoPilar.Core.Shared;

namespace UltimoPilar.Arena
{

/// <summary>Owns arena object activation, animation, lighting, particles, and debris.</summary>
public sealed class ArenaPhaseEffects
{
    private const float GravityLightRangeMeters = 18f;
    private const float GravityLightIntensity = 4f;
    private const float DebrisSpawnRadiusMeters = 15f;
    private const float DebrisSpawnHeightMeters = 10f;
    private const int DebrisCount = 10;
    private const float DebrisMinimumScale = 0.3f;
    private const float DebrisMaximumScale = 1f;
    private const float DebrisMass = 0.1f;
    private const float DebrisLifetimeSeconds = 3f;
    private const float DebrisSpawnIntervalSeconds = 0.2f;
    private const float OpeningAnimationDurationSeconds = 1f;
    private const string GravityLightName = "LuzZona";
    private static readonly Color GravityZoneColor = new Color(0.6f, 0.2f, 1f);
    private static readonly Color DebrisColor = Color.gray;

    private readonly GameObject pitObject;
    private readonly GameObject gravityZoneObject;
    private readonly GameObject emergencyDebrisObject;
    private readonly GameObject[] additionalObstacles;
    private readonly List<GameObject> activeDebris = new();

    private Vector3 initialPitScale;
    private Vector3 initialGravityZoneScale;
    private Vector3 initialEmergencyDebrisScale;
    private bool initialPitScaleCaptured;
    private bool initialGravityZoneScaleCaptured;
    private bool initialEmergencyDebrisScaleCaptured;

    /// <summary>Initializes arena effects with the objects they control.</summary>
    /// <param name="pitObject">The central pit object.</param>
    /// <param name="gravityZoneObject">The altered gravity zone object.</param>
    /// <param name="emergencyDebrisObject">The emergency debris root object.</param>
    /// <param name="additionalObstacles">The obstacles enabled by the emergency phase.</param>
    public ArenaPhaseEffects(
        GameObject pitObject,
        GameObject gravityZoneObject,
        GameObject emergencyDebrisObject,
        GameObject[] additionalObstacles)
    {
        this.pitObject = pitObject;
        this.gravityZoneObject = gravityZoneObject;
        this.emergencyDebrisObject = emergencyDebrisObject;
        this.additionalObstacles = additionalObstacles;
    }

    /// <summary>Restores controlled objects and clears generated debris.</summary>
    public void Reset()
    {
        CaptureInitialScales();
        ClearGeneratedObjects();
        SetActive(pitObject, false);
        SetActive(gravityZoneObject, false);
        SetActive(emergencyDebrisObject, false);
        SetAdditionalObstaclesActive(false);
        RestoreInitialScales();
    }

    /// <summary>Activates the central pit and its opening animation.</summary>
    /// <returns>The activation coroutine.</returns>
    public IEnumerator ActivatePit()
    {
        if (pitObject == null)
        {
            yield break;
        }

        pitObject.SetActive(true);
        yield return AnimateOpening(pitObject);
    }

    /// <summary>Activates the gravity zone and bootstraps its visual components.</summary>
    /// <returns>The activation coroutine.</returns>
    public IEnumerator ActivateGravity()
    {
        if (gravityZoneObject == null)
        {
            yield break;
        }

        gravityZoneObject.SetActive(true);
        BootstrapGravityLight(gravityZoneObject);
        BootstrapGravityParticles(gravityZoneObject);
        yield return AnimateOpening(gravityZoneObject);
    }

    /// <summary>Activates emergency objects and starts the debris sequence.</summary>
    /// <returns>The activation coroutine.</returns>
    public IEnumerator ActivateEmergency()
    {
        SetActive(emergencyDebrisObject, true);
        SetAdditionalObstaclesActive(true);
        yield return SpawnDebris();
    }

    private void CaptureInitialScales()
    {
        if (!initialPitScaleCaptured && pitObject != null)
        {
            initialPitScale = pitObject.transform.localScale;
            initialPitScaleCaptured = true;
        }

        if (!initialGravityZoneScaleCaptured && gravityZoneObject != null)
        {
            initialGravityZoneScale = gravityZoneObject.transform.localScale;
            initialGravityZoneScaleCaptured = true;
        }

        if (!initialEmergencyDebrisScaleCaptured && emergencyDebrisObject != null)
        {
            initialEmergencyDebrisScale = emergencyDebrisObject.transform.localScale;
            initialEmergencyDebrisScaleCaptured = true;
        }
    }

    private void RestoreInitialScales()
    {
        if (initialPitScaleCaptured && pitObject != null)
        {
            pitObject.transform.localScale = initialPitScale;
        }

        if (initialGravityZoneScaleCaptured && gravityZoneObject != null)
        {
            gravityZoneObject.transform.localScale = initialGravityZoneScale;
        }

        if (initialEmergencyDebrisScaleCaptured && emergencyDebrisObject != null)
        {
            emergencyDebrisObject.transform.localScale = initialEmergencyDebrisScale;
        }
    }

    private void ClearGeneratedObjects()
    {
        foreach (GameObject debris in activeDebris)
        {
            if (debris == null)
            {
                continue;
            }

            debris.SetActive(false);
            UnityEngine.Object.Destroy(debris);
        }

        activeDebris.Clear();
    }

    private void BootstrapGravityLight(GameObject gravityZone)
    {
        Light gravityLight = gravityZone.GetComponentInChildren<Light>();
        if (gravityLight != null)
        {
            return;
        }

        GameObject lightObject = new GameObject(GravityLightName);
        lightObject.transform.SetParent(gravityZone.transform);
        lightObject.transform.localPosition = Vector3.zero;
        gravityLight = lightObject.AddComponent<Light>();
        gravityLight.type = LightType.Point;
        gravityLight.range = GravityLightRangeMeters;
        gravityLight.intensity = GravityLightIntensity;
        gravityLight.color = GravityZoneColor;
    }

    private void BootstrapGravityParticles(GameObject gravityZone)
    {
        ParticleSystem particles = gravityZone.GetComponentInChildren<ParticleSystem>();
        if (particles == null)
        {
            return;
        }

        particles.Play();
    }

    private IEnumerator AnimateOpening(GameObject target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 finalScale = target.transform.localScale;
        target.transform.localScale = Vector3.zero;
        float elapsedSeconds = 0f;
        while (elapsedSeconds < OpeningAnimationDurationSeconds)
        {
            elapsedSeconds += Time.deltaTime;
            float progress = elapsedSeconds / OpeningAnimationDurationSeconds;
            target.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, progress);
            yield return null;
        }

        target.transform.localScale = finalScale;
    }

    private IEnumerator SpawnDebris()
    {
        for (int index = 0; index < DebrisCount; index++)
        {
            Vector3 position = UnityEngine.Random.insideUnitSphere * DebrisSpawnRadiusMeters;
            position.y = DebrisSpawnHeightMeters;

            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (debris != null)
            {
                ConfigureDebris(debris, position);
            }

            yield return new WaitForSeconds(DebrisSpawnIntervalSeconds);
        }
    }

    private void ConfigureDebris(GameObject debris, Vector3 position)
    {
        activeDebris.Add(debris);
        debris.transform.position = position;
        debris.transform.localScale = Vector3.one
            * UnityEngine.Random.Range(DebrisMinimumScale, DebrisMaximumScale);

        Renderer renderer = debris.GetComponent<Renderer>();
        if (renderer != null)
        {
            MaterialColorHelper.SetBaseAndEmissionColor(renderer.material, DebrisColor);
        }

        Collider collider = debris.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        Rigidbody rigidbody = debris.AddComponent<Rigidbody>();
        rigidbody.mass = DebrisMass;
        UnityEngine.Object.Destroy(debris, DebrisLifetimeSeconds);
    }

    private void SetAdditionalObstaclesActive(bool active)
    {
        if (additionalObstacles == null)
        {
            return;
        }

        foreach (GameObject obstacle in additionalObstacles)
        {
            SetActive(obstacle, active);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(active);
    }
}
}
