// arenatransform.cs
// controla las transformaciones de la arena segun la vida del pilar
// fases: 1 base, 2 pozo, 3 gravedad, 4 emergencia
// va en un objeto vacio gestor de arena

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltimoPilar.Arena;

// componente que reacciona a cambios de fase del pilar y ejecuta avisos y activaciones en orden
public class ArenaTransform : MonoBehaviour
{
    private const int FirstArenaPhase = 1;
    private const float ArenaGizmoRadiusMeters = 20f;

    [Header("Referencias")]
    public Pilar pilar;
    public GameObject sueloBase;

    [Header("Elementos de Transformacion")]
    public GameObject pozoCentral;
    public GameObject zonaGravedad;
    public GameObject escombrosFase4;
    public GameObject[] obstaculosAdicionales;

    [Header("Configuracion")]
    public float tiempoAvisoPrevio = 3f;
    public Color colorAviso = Color.yellow;
    public AudioClip sonidoTransformacion;

    [Header("Estado")]
    public int faseActual = FirstArenaPhase;
    public bool transformacionEnProgreso = false;

    public event Action<int> OnTransformacionIniciada;
    public event Action<int> OnTransformacionCompletada;

    private ArenaPhaseState phaseState;
    private IReadOnlyDictionary<int, IArenaPhaseHandler> phaseHandlers;
    private ArenaWarningPresenter warningPresenter;
    private ArenaPhaseEffects phaseEffects;
    private bool procesandoCola;

    // inicializa, busca pilar si falta, asegura dependencias y se suscribe al cambio de fase
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

        if (GameManager.Instance == null)
        {
            ResetState();
        }
    }

    // se desuscribe del pilar al destruir el objeto
    private void OnDestroy()
    {
        if (pilar != null)
        {
            pilar.OnFaseCambiada -= OnPilarFaseCambiada;
        }
    }

    // resetea progresion, cola y efectos visuales a fase 1
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

    // crea los servicios internos si no existen (estado, presentador, efectos y manejadores)
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

    // obtiene el renderizador del suelo para el efecto de parpadeo
    private Renderer GetFloorRenderer()
    {
        if (sueloBase == null)
        {
            return null;
        }

        return sueloBase.GetComponent<Renderer>();
    }

    // resuelve donde suena el aviso, cerca de la camara del jugador o en la arena
    private Vector3 ResolveAudioPosition()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.player == null || gameManager.player.camaraJugador == null)
        {
            return transform.position;
        }

        return gameManager.player.camaraJugador.transform.position;
    }

    // recibe cambio de fase del pilar, encola fases faltantes y lanza la cola
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

    // vacia la cola de fases de forma secuencial, una transformacion por vez
    private IEnumerator ProcesarColaFases()
    {
        procesandoCola = true;
        while (phaseState.TryDequeue(out int phase))
        {
            yield return StartCoroutine(EjecutarTransformacion(phase));
        }

        procesandoCola = false;
    }

    // ejecuta aviso y activacion de una fase y la marca como completada
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

        Debug.Log($"[ArenaTransform] ¡AVISO! Transformacion a Fase {fase} en {tiempoAvisoPrevio} segundos...");
        OnTransformacionIniciada?.Invoke(fase);
        yield return StartCoroutine(handler.Warn(tiempoAvisoPrevio));
        yield return StartCoroutine(handler.Activate());

        phaseState.MarkActivated(fase);
        transformacionEnProgreso = false;
        OnTransformacionCompletada?.Invoke(fase);
        Debug.Log($"[ArenaTransform] Transformacion a Fase {fase} completada.");
    }

    // dibuja el radio de la arena y el pozo en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ArenaGizmoRadiusMeters);

        if (pozoCentral == null)
        {
            return;
        }

        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(pozoCentral.transform.position, pozoCentral.transform.localScale);
    }
}
