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
    // Balance de las torretas de emergencia (próximo paso: mover a un ScriptableObject TurretConfig).
    private const float TurretSpawnHeightMeters = 1.1f;
    private const float TurretRangeMeters = 22f;
    private const float TurretFireRatePerSecond = 0.9f;
    private const float TurretDamage = 6f;
    private const float TurretHealth = 120f;
    private const int TurretAmmo = 15;
    private const float TurretReloadSeconds = 10f;
    private const float TurretProjectileSpeedMetersPerSecond = 28f;
    // Medidas solo para dibujar los gizmos del editor; no afectan al gameplay.
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
    // Flag para spawnear una sola vez al entrar en fase 4.
    public bool torretasActivas = false;
    public Transform[] puntosTorretas;
    public GameObject prefabTorreta;

    // Eventos: la UI y el audio escuchan vida y daño; la Arena escucha los cambios de fase.
    public event Action<float> OnVidaCambiada;
    public event Action<int> OnFaseCambiada;
    public event Action<float> OnDañoRecibido;

    private Renderer rend;
    // El constructor del spawner es puro (solo guarda valores), por eso se crea
    // acá: existe desde la construcción, incluso si RestaurarVida() corre antes del Start().
    private PilarTurretSpawner turretSpawner = new PilarTurretSpawner(
        TurretSpawnHeightMeters,
        TurretRangeMeters,
        TurretFireRatePerSecond,
        TurretDamage,
        TurretHealth,
        TurretAmmo,
        TurretReloadSeconds,
        TurretProjectileSpeedMetersPerSecond);
    private PilarPhaseCoordinator phaseCoordinator;
    private PilarVisualPresenter visualPresenter;
    private float[] phaseThresholds;
    private Color[] phaseColors;

    // Prepara renderer y ayudantes. Si hay GameManager, él restaura la vida en su
    // propio reset, por eso acá se evita la doble restauración.
    private void Start()
    {
        CacheRenderer();
        EnsurePhaseCoordinator();
        EnsureVisualPresenter();

        if (GameManager.Instance != null)
        {
            return;
        }
    }

    // Por frame: aplica los pasos de fase pendientes según la vida y suaviza el color.
    private void Update()
    {
        IReadOnlyList<int> steps = EnsurePhaseCoordinator().StepToward(vidaActual);
        foreach (int phase in steps)
        {
            ChangePhase(phase);
        }

        EnsureVisualPresenter().Present(rend, faseActual, Time.deltaTime);
    }

    // Aplica un paso de fase, avisa a la Arena y activa las torretas una sola vez al llegar a fase 4.
    private void ChangePhase(int newPhase)
    {
        Debug.Log($"[Pilar] Fase cambiada: {faseActual} -> {newPhase} (Vida: {vidaActual}%)");
        faseActual = newPhase;
        OnFaseCambiada?.Invoke(faseActual);

        if (faseActual != EmergencyPhase || torretasActivas)
        {
            return;
        }

        ActivateTurrets();
    }

    // Unity lo llama al tocar el Inspector en el editor: empuja umbrales y colores a los ayudantes ya creados.
    private void OnValidate()
    {
        if (phaseCoordinator != null && phaseThresholds != null && phaseThresholds.Length == 3)
        {
            phaseThresholds[0] = umbralFase2;
            phaseThresholds[1] = umbralFase3;
            phaseThresholds[2] = umbralFase4;
            phaseCoordinator.UpdateThresholds(phaseThresholds);
        }

        if (visualPresenter != null && phaseColors != null && phaseColors.Length == 4)
        {
            phaseColors[0] = colorFase1;
            phaseColors[1] = colorFase2;
            phaseColors[2] = colorFase3;
            phaseColors[3] = colorFase4;
            visualPresenter.UpdateColors(phaseColors);
        }
    }

    // Crea el coordinador de fases una sola vez con los umbrales del Inspector.
    private PilarPhaseCoordinator EnsurePhaseCoordinator()
    {
        if (phaseCoordinator == null)
        {
            phaseThresholds = new[] { umbralFase2, umbralFase3, umbralFase4 };
            phaseCoordinator = new PilarPhaseCoordinator(phaseThresholds, faseActual);
        }

        return phaseCoordinator;
    }

    // Crea el presentador visual una sola vez con los colores del Inspector.
    private PilarVisualPresenter EnsureVisualPresenter()
    {
        if (visualPresenter == null)
        {
            phaseColors = new[] { colorFase1, colorFase2, colorFase3, colorFase4 };
            visualPresenter = new PilarVisualPresenter(phaseColors);
        }

        return visualPresenter;
    }

    /// <summary>Applies damage when the match is active.</summary>
    public void RecibirDaño(float cantidad)
    {
        // Fuera de partida el daño de gameplay se ignora.
        if (GameManager.Instance != null && !GameManager.Instance.juegoActivo)
        {
            return;
        }

        // Resta con piso en cero y avisa a la UI (vida) y al audio (daño).
        vidaActual = Mathf.Max(MinimumHealth, vidaActual - cantidad);
        OnVidaCambiada?.Invoke(vidaActual);
        OnDañoRecibido?.Invoke(cantidad);

        // Sin vida: derrota.
        if (vidaActual <= MinimumHealth)
        {
            GameManager.Instance?.Derrota();
        }
    }

    // Protocolo de emergencia (fase 4): marca el flag para spawnear una sola vez y delega al spawner.
    private void ActivateTurrets()
    {
        torretasActivas = true;
        Debug.Log("[Pilar] ¡Protocolo de emergencia! Torretas activadas. (4 torretas, proyectil físico, busca enemigo más cercano)");
        turretSpawner.Spawn(puntosTorretas, prefabTorreta);
    }

    // Guarda el Renderer una sola vez para no llamar a GetComponent en cada uso.
    private void CacheRenderer()
    {
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
    }


    /// <summary>Gets the current Pilar health.</summary>
    // Lectura para el GameManager (resultado de partida) y el Hud.
    public float VidaActual => vidaActual;

    /// <summary>Gets the current health as a percentage.</summary>
    // Porcentaje 0-100 para la barra del Hud; protege contra división por cero.
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
}
