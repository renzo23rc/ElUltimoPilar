using System;
using System.Collections.Generic;
using UnityEngine;
using UltimoPilar.Core.Pilar;

/**
 * Pilar.cs
 * Gestiona la vida, estados visuales y transformaciones del Pilar.
 * Incluye detección de daño, umbrales de fase y eventos.
 *
 * Colocar en el GameObject que representa al Pilar en el centro de la arena.
 */
public class Pilar : MonoBehaviour
{
    private const float MinimumHealth = 0f;
    private const float PercentageScale = 100f;
    private const float MaximumHealth = 100f;
    private const int InitialPhase = 1;
    private const int EmergencyPhase = 4;
    private const float TurretSpawnHeightMeters = 1.1f;
    private const float TurretRangeMeters = 22f;
    private const float TurretFireRatePerSecond = 0.9f;
    private const float TurretDamage = 6f;
    private const float TurretHealth = 120f;
    private const int TurretAmmo = 15;
    private const float TurretReloadSeconds = 10f;
    private const float TurretProjectileSpeedMetersPerSecond = 28f;
    private const float TurretGizmoRadiusMeters = 25f;
    private const float TurretGizmoWellRadiusMeters = 5f;
    private const float TurretGizmoHeightMeters = 1.1f;
    private const float TurretGizmoWidthMeters = 1.4f;
    private const float TurretGizmoHeightSizeMeters = 2.2f;
    private const float TurretGizmoDepthMeters = 1.4f;

    [Header("Vida")]
    [Range(0, 100)]
    public float vidaMaxima = MaximumHealth;
    [Range(0, 100)]
    public float vidaActual = MaximumHealth;

    [Header("Umbrales de Transformación")]
    public float umbralFase2 = 75f;
    public float umbralFase3 = 50f;
    public float umbralFase4 = 25f;

    [Header("Estado Visual (Debug)")]
    public int faseActual = InitialPhase;
    public Color colorFase1 = Color.cyan;
    public Color colorFase2 = Color.yellow;
    public Color colorFase3 = new Color(1f, 0.5f, 0f);
    public Color colorFase4 = Color.red;

    [Header("Torretas (Fase 4)")]
    public bool torretasActivas = false;
    public Transform[] puntosTorretas;
    public GameObject prefabTorreta;

    public event Action<float> OnVidaCambiada;
    public event Action<int> OnFaseCambiada;
    public event Action<float> OnDañoRecibido;

    private Renderer rend;
    private int faseAnterior = InitialPhase;
    private PilarTurretSpawner turretSpawner;
    private PilarPhaseCoordinator phaseCoordinator;
    private PilarVisualPresenter visualPresenter;
    private float[] phaseThresholds;
    private Color[] phaseColors;

    private void Start()
    {
        CacheRenderer();
        EnsureTurretSpawner();
        EnsurePhaseCoordinator();
        EnsureVisualPresenter();

        if (GameManager.Instance != null)
        {
            return;
        }

        RestaurarVida();
    }

    private void Update()
    {
        IReadOnlyList<int> steps = EnsurePhaseCoordinator().StepToward(vidaActual);
        foreach (int phase in steps)
        {
            ChangePhase(phase);
        }

        EnsureVisualPresenter().Present(rend, faseActual, Time.deltaTime);
    }

    private void ChangePhase(int newPhase)
    {
        faseAnterior = faseActual;
        faseActual = newPhase;
        Debug.Log($"[Pilar] Fase cambiada: {faseAnterior} -> {faseActual} (Vida: {vidaActual}%)");
        OnFaseCambiada?.Invoke(faseActual);

        if (faseActual != EmergencyPhase || torretasActivas)
        {
            return;
        }

        ActivateTurrets();
    }

    private PilarPhaseCoordinator EnsurePhaseCoordinator()
    {
        if (phaseCoordinator == null)
        {
            phaseThresholds = new[] { umbralFase2, umbralFase3, umbralFase4 };
            phaseCoordinator = new PilarPhaseCoordinator(phaseThresholds, faseActual);
            return phaseCoordinator;
        }

        if (phaseThresholds[0] == umbralFase2
            && phaseThresholds[1] == umbralFase3
            && phaseThresholds[2] == umbralFase4)
        {
            return phaseCoordinator;
        }

        phaseThresholds[0] = umbralFase2;
        phaseThresholds[1] = umbralFase3;
        phaseThresholds[2] = umbralFase4;
        phaseCoordinator.UpdateThresholds(phaseThresholds);
        return phaseCoordinator;
    }

    private PilarVisualPresenter EnsureVisualPresenter()
    {
        if (visualPresenter == null)
        {
            phaseColors = new[] { colorFase1, colorFase2, colorFase3, colorFase4 };
            visualPresenter = new PilarVisualPresenter(phaseColors);
            return visualPresenter;
        }

        if (phaseColors[0] == colorFase1
            && phaseColors[1] == colorFase2
            && phaseColors[2] == colorFase3
            && phaseColors[3] == colorFase4)
        {
            return visualPresenter;
        }

        phaseColors[0] = colorFase1;
        phaseColors[1] = colorFase2;
        phaseColors[2] = colorFase3;
        phaseColors[3] = colorFase4;
        visualPresenter.UpdateColors(phaseColors);
        return visualPresenter;
    }

    /// <summary>Applies damage when the match is active.</summary>
    public void RecibirDaño(float cantidad)
    {
        ApplyDamage(cantidad, true);
    }

    /// <summary>Applies debug damage regardless of match activity.</summary>
    public void AplicarDañoPrueba(float cantidad)
    {
        ApplyDamage(cantidad, false);
        Debug.Log($"[Pilar] Daño prueba: {cantidad}. Vida: {vidaActual}%");
    }

    private void ApplyDamage(float amount, bool requireActiveMatch)
    {
        if (requireActiveMatch && GameManager.Instance != null && !GameManager.Instance.juegoActivo)
        {
            return;
        }

        vidaActual = Mathf.Max(MinimumHealth, vidaActual - amount);
        OnVidaCambiada?.Invoke(vidaActual);
        OnDañoRecibido?.Invoke(amount);

        if (vidaActual > MinimumHealth)
        {
            return;
        }

        if (!requireActiveMatch || GameManager.Instance == null || GameManager.Instance.juegoActivo)
        {
            GameManager.Instance?.Derrota();
        }
    }

    /// <summary>Restores health and visual state to the initial phase.</summary>
    public void RestaurarVida()
    {
        CacheRenderer();
        EnsureTurretSpawner();
        turretSpawner.Clear();
        vidaActual = vidaMaxima;
        faseActual = InitialPhase;
        faseAnterior = InitialPhase;
        EnsurePhaseCoordinator().ResetTo(InitialPhase);
        torretasActivas = false;
        OnVidaCambiada?.Invoke(vidaActual);

        if (rend != null)
        {
            rend.material.color = colorFase1;
        }
    }

    private void ActivateTurrets()
    {
        torretasActivas = true;
        EnsureTurretSpawner();
        Debug.Log("[Pilar] ¡Protocolo de emergencia! Torretas activadas. (4 torretas, proyectil físico, busca enemigo más cercano)");
        turretSpawner.Spawn(puntosTorretas, prefabTorreta);
    }

    private void CacheRenderer()
    {
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
    }

    private void EnsureTurretSpawner()
    {
        turretSpawner ??= new PilarTurretSpawner(
            TurretSpawnHeightMeters,
            TurretRangeMeters,
            TurretFireRatePerSecond,
            TurretDamage,
            TurretHealth,
            TurretAmmo,
            TurretReloadSeconds,
            TurretProjectileSpeedMetersPerSecond);
    }

    private void OnDrawGizmosSelected()
    {
        if (puntosTorretas == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        foreach (Transform point in puntosTorretas)
        {
            if (point == null)
            {
                continue;
            }

            Vector3 worldPosition = new Vector3(point.position.x, TurretGizmoHeightMeters, point.position.z);
            Gizmos.DrawWireCube(
                worldPosition,
                new Vector3(TurretGizmoWidthMeters, TurretGizmoHeightSizeMeters, TurretGizmoDepthMeters));
            Gizmos.DrawLine(point.position, worldPosition);
            Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, TurretGizmoWellRadiusMeters);
            Gizmos.color = Color.yellow;
        }

        if (faseActual != EmergencyPhase)
        {
            return;
        }

        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        foreach (Transform point in puntosTorretas)
        {
            if (point == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(
                new Vector3(point.position.x, TurretGizmoHeightMeters, point.position.z),
                TurretGizmoRadiusMeters);
        }
    }

    /// <summary>Gets the current Pilar health.</summary>
    public float VidaActual => vidaActual;

    /// <summary>Gets the current health as a percentage.</summary>
    public float PorcentajeVida
    {
        get
        {
            if (vidaMaxima <= MinimumHealth)
            {
                return MinimumHealth;
            }

            return (vidaActual / vidaMaxima) * PercentageScale;
        }
    }

    /// <summary>Gets whether the Pilar has positive health.</summary>
    public bool EstaVivo => vidaActual > MinimumHealth;
}
