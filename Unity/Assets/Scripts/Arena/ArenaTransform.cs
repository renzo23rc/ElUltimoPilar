/**
 * ArenaTransform.cs
 * Gestiona las transformaciones acumulativas e irreversibles de la arena
 * según los umbrales de vida del Pilar.
 * 
 * Según el GDD:
 * - Fase 1 (100-75%): Arena base
 * - Fase 2 (75-50%): Pozo central se abre
 * - Fase 3 (50-25%): Zona de gravedad alterada
 * - Fase 4 (25-0%): Protocolo de emergencia + caos
 * 
 * Colocar en un GameObject vacío "ArenaManager" o en el suelo/arena.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltimoPilar.Arena;

public class ArenaTransform : MonoBehaviour
{
    private const int FirstArenaPhase = 1;
    private const float ArenaGizmoRadiusMeters = 20f;

    [Header("Referencias")]
    public Pilar pilar;
    public GameObject sueloBase;

    [Header("Elementos de Transformación")]
    public GameObject pozoCentral; // Se activa en fase 2
    public GameObject zonaGravedad; // Se activa en fase 3
    public GameObject escombrosFase4; // Se activan en fase 4
    public GameObject[] obstaculosAdicionales;

    [Header("Configuración")]
    public float tiempoAvisoPrevio = 3f;
    public Color colorAviso = Color.yellow;
    public AudioClip sonidoTransformacion;

    [Header("Estado")]
    public int faseActual = FirstArenaPhase;
    public bool transformacionEnProgreso = false;

    // Eventos
    public event Action<int> OnTransformacionIniciada;
    public event Action<int> OnTransformacionCompletada;

    private ArenaPhaseState phaseState;
    private IReadOnlyDictionary<int, IArenaPhaseHandler> phaseHandlers;
    private ArenaWarningPresenter warningPresenter;
    private ArenaPhaseEffects phaseEffects;
    private bool procesandoCola;

    private void Start()
    {
        if (pilar == null)
        {
            pilar = FindFirstObjectByType<Pilar>();
        }

        EnsureDependencies();
        if (pilar != null)
        {
            pilar.OnFaseCambiada += OnPilarFaseCambiada;
        }

        // GameManager owns the managed match reset. Preserve standalone
        // arena scenes without repeating the reset in a managed match.
        if (GameManager.Instance == null)
        {
            ResetState();
        }
    }

    private void OnDestroy()
    {
        if (pilar != null)
        {
            pilar.OnFaseCambiada -= OnPilarFaseCambiada;
        }
    }

    /// <summary>Resets arena progression and restores the captured presentation state.</summary>
    public void ResetState()
    {
        EnsureDependencies();
        StopAllCoroutines();
        procesandoCola = false;
        transformacionEnProgreso = false;
        faseActual = FirstArenaPhase;
        phaseState.Reset();
        phaseEffects.Reset();
        warningPresenter.Reset();
    }

    private void EnsureDependencies()
    {
        if (phaseState == null)
        {
            phaseState = new ArenaPhaseState();
        }

        if (warningPresenter == null)
        {
            Hud hud = FindFirstObjectByType<Hud>();
            Action<string, Color, float> hudWarningPort = null;
            if (hud != null)
            {
                hudWarningPort = hud.MostrarAdvertencia;
            }

            Action<AudioClip, Vector3, float> audioPort = AudioSource.PlayClipAtPoint;
            Func<Vector3> audioPositionPort = ResolveAudioPosition;
            warningPresenter = new ArenaWarningPresenter(
                GetFloorRenderer(),
                pozoCentral,
                colorAviso,
                sonidoTransformacion,
                audioPositionPort,
                hudWarningPort,
                audioPort);
        }

        if (phaseEffects == null)
        {
            phaseEffects = new ArenaPhaseEffects(
                pozoCentral,
                zonaGravedad,
                escombrosFase4,
                obstaculosAdicionales);
        }

        if (phaseHandlers == null)
        {
            phaseHandlers = ArenaPhaseHandlerCatalog.CreateDefault(warningPresenter, phaseEffects);
        }
    }

    private Renderer GetFloorRenderer()
    {
        if (sueloBase == null)
        {
            return null;
        }

        return sueloBase.GetComponent<Renderer>();
    }

    private Vector3 ResolveAudioPosition()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.player == null || gameManager.player.camaraJugador == null)
        {
            return transform.position;
        }

        return gameManager.player.camaraJugador.transform.position;
    }

    private void OnPilarFaseCambiada(int nuevaFase)
    {
        if (nuevaFase <= faseActual)
        {
            return;
        }

        EnsureDependencies();
        phaseState.EnqueueMissing(faseActual, nuevaFase);
        faseActual = nuevaFase;
        if (procesandoCola)
        {
            return;
        }

        StartCoroutine(ProcesarColaFases());
    }

    private IEnumerator ProcesarColaFases()
    {
        procesandoCola = true;
        while (phaseState.TryDequeue(out int phase))
        {
            yield return StartCoroutine(EjecutarTransformacion(phase));
        }

        procesandoCola = false;
    }

    private IEnumerator EjecutarTransformacion(int fase)
    {
        EnsureDependencies();
        transformacionEnProgreso = true;
        if (!phaseHandlers.TryGetValue(fase, out IArenaPhaseHandler handler) || handler == null)
        {
            transformacionEnProgreso = false;
            Debug.LogWarning($"[ArenaTransform] No handler configured for phase {fase}.");
            yield break;
        }

        Debug.Log($"[ArenaTransform] ¡AVISO! Transformación a Fase {fase} en {tiempoAvisoPrevio} segundos...");
        OnTransformacionIniciada?.Invoke(fase);
        yield return StartCoroutine(handler.Warn(tiempoAvisoPrevio));
        yield return StartCoroutine(handler.Activate());

        phaseState.MarkActivated(fase);
        transformacionEnProgreso = false;
        OnTransformacionCompletada?.Invoke(fase);
        Debug.Log($"[ArenaTransform] Transformación a Fase {fase} completada.");
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar radio de la arena
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ArenaGizmoRadiusMeters);

        // Dibujar zonas de transformación
        if (pozoCentral == null)
        {
            return;
        }

        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(pozoCentral.transform.position, pozoCentral.transform.localScale);
    }
}


