/**
 * WeaponVariantEffects.cs
 * Aplica en la escena los efectos que WeaponSystem solicita para las variantes
 * de área: Lanzador de señuelo y Carga de ralentización.
 *
 * WeaponSystem lo agrega automáticamente si falta en el jugador.
 */
using UnityEngine;

/// <summary>Resolves area weapon variant requests into scene effects.</summary>
[RequireComponent(typeof(WeaponSystem))]
public class WeaponVariantEffects : MonoBehaviour
{
    [Header("Lanzador de señuelo")]
    [SerializeField, Min(0f)] private float decoyAttractionRadiusMeters = 12f;
    [SerializeField, Min(0f)] private float decoyLifetimeSeconds = 5f;

    [Header("Carga de ralentización")]
    [SerializeField, Range(0f, 1f)] private float slowdownSpeedFactor = 0.15f;

    private WeaponSystem weaponSystem;

    private void OnEnable()
    {
        weaponSystem = GetComponent<WeaponSystem>();
        weaponSystem.OnDecoyRequested += HandleDecoyRequested;
        weaponSystem.OnSlowdownRequested += HandleSlowdownRequested;
    }

    private void OnDisable()
    {
        if (weaponSystem == null)
        {
            return;
        }

        weaponSystem.OnDecoyRequested -= HandleDecoyRequested;
        weaponSystem.OnSlowdownRequested -= HandleSlowdownRequested;
    }

    private void HandleDecoyRequested(Vector3 impactPoint)
    {
        DecoyBeacon.Spawn(impactPoint, decoyAttractionRadiusMeters, decoyLifetimeSeconds);
    }

    private void HandleSlowdownRequested(Enemy enemy, float durationSeconds)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.AplicarRalentizacion(this, slowdownSpeedFactor, durationSeconds);
    }
}
