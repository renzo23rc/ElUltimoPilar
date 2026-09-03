/**
 * GameManager.cs
 * Controla el flujo de la partida: oleadas, estado del juego,
 * victoria/derrota, y comunicación entre sistemas.
 *
 * Colocar en un GameObject vacío "GameManager" en la escena.
 */
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Coordinates match flow, wave progression, players, and terminal results.
/// </summary>
public class GameManager : MonoBehaviour
{
    private const float InitialTimeScale = 1f;
    private const float NoInputLookThreshold = 2f;

    /// <summary>
    /// Gets the active game manager instance.
    /// </summary>
    public static GameManager Instance { get; private set; }

    [Header("Referencias")]
    /// <summary>Gets or sets the Pilar controlled by the match.</summary>
    public Pilar pilar;
    /// <summary>Gets or sets the enemy wave spawner.</summary>
    public EnemySpawner spawner;
    /// <summary>Gets or sets the primary player.</summary>
    public PlayerController player;

    [Header("Configuración de Oleadas")]
    /// <summary>Gets or sets the total number of waves.</summary>
    public int totalOleadas = 10;
    /// <summary>Gets or sets the delay between waves in seconds.</summary>
    public float tiempoEntreOleadas = 7f;

    [Header("Configuración de Jugadores")]
    [Range(PlayerRoster<PlayerController>.MinimumCapacity, PlayerRoster<PlayerController>.MaximumCapacity)]
    /// <summary>Gets or sets the maximum number of players.</summary>
    public int maxPlayers = PlayerRoster<PlayerController>.MaximumCapacity;

    [Header("Estado Actual")]
    /// <summary>Gets the current wave number.</summary>
    public int oleadaActual => matchFlow?.CurrentWave ?? 0;
    /// <summary>Gets whether the match is active or paused.</summary>
    public bool juegoActivo => matchFlow != null &&
        (matchFlow.State == MatchState.Playing || matchFlow.State == MatchState.Paused);
    /// <summary>Gets whether the match is paused.</summary>
    public bool juegoPausado => matchFlow?.State == MatchState.Paused;
    /// <summary>Gets the current match state.</summary>
    public MatchState EstadoActual => matchFlow?.State ?? MatchState.WaitingToStart;
    /// <summary>Gets the current terminal result, or <see langword="null"/>.</summary>
    public MatchResult CurrentResult { get; private set; }

    private MatchFlow matchFlow;
    private PlayerRoster<PlayerController> playerRoster;
    private ArenaTransform arenaTransform;
    private readonly HashSet<PlayerController> jugadoresSuscritos = new HashSet<PlayerController>();

    /// <summary>Gets the registered players.</summary>
    public IReadOnlyList<PlayerController> Players =>
        playerRoster?.Players ?? Array.Empty<PlayerController>();
    /// <summary>Gets the number of registered players.</summary>
    public int PlayerCount => playerRoster?.Count ?? 0;

    /// <summary>Raised when a wave starts.</summary>
    public event Action<int> OnOleadaIniciada;
    /// <summary>Raised when a wave completes.</summary>
    public event Action<int> OnOleadaCompletada;
    /// <summary>Raised when the match is won.</summary>
    public event Action OnVictoria;
    /// <summary>Raised when the match is lost.</summary>
    public event Action OnDerrota;
    /// <summary>Raised when a valid terminal result is published.</summary>
    public event Action<MatchResult> OnMatchResult;
    /// <summary>Raised when the match starts.</summary>
    public event Action OnJuegoIniciado;
    /// <summary>Raised when a player is registered.</summary>
    public event Action<PlayerController> OnPlayerRegistered;
    /// <summary>Raised when a player is unregistered.</summary>
    public event Action<PlayerController> OnPlayerUnregistered;

    private float timerEntreOleadas = 0f;
    private bool esperandoOleada = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializePlayerRoster();
        matchFlow = new MatchFlow(totalOleadas);
    }

    void OnValidate()
    {
        maxPlayers = Mathf.Clamp(
            maxPlayers,
            PlayerRoster<PlayerController>.MinimumCapacity,
            PlayerRoster<PlayerController>.MaximumCapacity);
    }

    void InitializePlayerRoster()
    {
        maxPlayers = Mathf.Clamp(
            maxPlayers,
            PlayerRoster<PlayerController>.MinimumCapacity,
            PlayerRoster<PlayerController>.MaximumCapacity);
        playerRoster = new PlayerRoster<PlayerController>(maxPlayers);
    }

    [Header("Inicio")]
    /// <summary>Gets or sets whether player input starts the match.</summary>
    public bool iniciarAlMover = true;
    private bool esperandoInputInicial = true;

    void Start()
    {
        if (pilar == null)
            pilar = FindFirstObjectByType<Pilar>();
        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();
        if (arenaTransform == null)
            arenaTransform = FindFirstObjectByType<ArenaTransform>();

        if (pilar == null)
        {
            Debug.LogError("[GameManager] No se encontró el Pilar en la escena.");
            return;
        }
        if (spawner == null)
        {
            Debug.LogWarning("[GameManager] No se encontró el Spawner. Las oleadas no funcionarán.");
        }
        // Descubrimiento inicial temporal para registrar la escena existente.
        var jugadoresDescubiertos = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var jugadorDescubierto in jugadoresDescubiertos)
        {
            RegisterPlayer(jugadorDescubierto);
        }

        if (player == null)
        {
            Debug.LogWarning("[GameManager] No se encontró el Jugador.");
        }

        // Dejar la escena en un estado limpio una sola vez. Si el inicio se
        // difiere hasta recibir input, IniciarJuego reutiliza este estado.
        ResetOwnedState();

        if (iniciarAlMover)
        {
            esperandoInputInicial = true;
            Debug.Log("[GameManager] Presiona WASD o mueve el mouse para iniciar. (Para PC lenta)");
        }
        else
        {
            IniciarJuego();
        }
    }

    void Update()
    {
        if (!juegoActivo) return;
        if (juegoPausado) return;

        if (esperandoOleada)
        {
            timerEntreOleadas -= Time.deltaTime;
            if (timerEntreOleadas <= 0)
            {
                esperandoOleada = false;
                IniciarSiguienteOleada();
            }
        }
        else if (spawner != null && !spawner.OleadaEnProgreso && spawner.EnemigosVivos == 0)
        {
            CompletarOleadaActual();
        }
    }

    /// <summary>Starts or restarts the match.</summary>
    public void IniciarJuego()
    {
        // El arranque inicial ya fue preparado por Start. Los reinicios y
        // los nuevos partidos posteriores a un estado previo sí requieren
        // volver a limpiar el estado propio.
        if (EstadoActual != MatchState.WaitingToStart)
            ResetOwnedState();
        if (!matchFlow.Start()) return;

        esperandoInputInicial = false;
        OnJuegoIniciado?.Invoke();

        IniciarSiguienteOleada();
    }

    private void ResetOwnedState()
    {
        // Restaurar el reloj antes de detener cualquier estado dependiente del tiempo.
        Time.timeScale = 1f;
        matchFlow.Reset();
        CurrentResult = null;
        esperandoInputInicial = false;
        esperandoOleada = false;
        timerEntreOleadas = 0f;

        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();
        if (arenaTransform == null)
            arenaTransform = FindFirstObjectByType<ArenaTransform>();

        // Detener actores de la partida antes de restaurar sus fuentes de estado.
        spawner?.LimpiarTodos();
        arenaTransform?.ResetState();
        pilar?.RestaurarVida();

        // El roster y sus suscripciones sobreviven al reinicio; solo se reinicia su estado propio.
        foreach (var jugadorRegistrado in Players)
        {
            if (jugadorRegistrado == null) continue;

            jugadorRegistrado.ResetState();
            jugadorRegistrado.GetComponent<WeaponSystem>()?.ResetState();
            jugadorRegistrado.GetComponent<EnergySystem>()?.ResetState();
        }
    }

    /// <summary>Pauses the active match.</summary>
    public void PausarJuego()
    {
        if (!matchFlow.Pause()) return;
        Time.timeScale = 0f;
    }

    /// <summary>Resumes the paused match.</summary>
    public void ReanudarJuego()
    {
        if (!matchFlow.Resume()) return;
        Time.timeScale = 1f;
    }

    /// <summary>Restarts the match.</summary>
    public void ReiniciarJuego()
    {
        IniciarJuego();
    }

    void IniciarSiguienteOleada()
    {
        if (matchFlow.CurrentWave >= matchFlow.TotalWaves)
        {
            Victoria();
            return;
        }
        if (!matchFlow.TryStartNextWave()) return;

        int wave = matchFlow.CurrentWave;
        spawner?.IniciarOleada(wave);
        OnOleadaIniciada?.Invoke(wave);

        Debug.Log($"[GameManager] Oleada {wave}/{totalOleadas} iniciada");
    }

    void CompletarOleadaActual()
    {
        OnOleadaCompletada?.Invoke(oleadaActual);
        Debug.Log($"[GameManager] Oleada {oleadaActual} completada");

        // Dar munición al final de oleada (según GDD) a todos los jugadores registrados.
        playerRoster?.ReplenishWaveAmmo();

        if (oleadaActual >= totalOleadas)
        {
            Victoria();
        }
        else
        {
            esperandoOleada = true;
            timerEntreOleadas = tiempoEntreOleadas;
        }
    }

    /// <summary>Publishes a victory result.</summary>
    public void Victoria()
    {
        PublishTerminalResult(MatchState.Victory);
    }

    /// <summary>Publishes a defeat result.</summary>
    public void Derrota()
    {
        PublishTerminalResult(MatchState.Defeat);
    }

    /// <summary>Publishes defeat caused by all players being downed.</summary>
    public void DerrotaPorJugadores()
    {
        PublishTerminalResult(MatchState.Defeat);
    }

    private void PublishTerminalResult(MatchState outcome)
    {
        bool transitioned;
        switch (outcome)
        {
            case MatchState.Victory:
                transitioned = matchFlow.SetVictory();
                break;
            case MatchState.Defeat:
                transitioned = matchFlow.SetDefeat();
                break;
            default:
                return;
        }

        if (!transitioned) return;

        Time.timeScale = 1f;
        PilarHealthSnapshot snapshot;
        if (TryCreatePilarHealthSnapshot(out snapshot))
        {
            CurrentResult = new MatchResult(outcome, snapshot);
        }
        else
        {
            CurrentResult = null;
        }

        if (outcome == MatchState.Victory)
        {
            float vidaRestante = CurrentResult != null ? CurrentResult.PilarHealth.RemainingPercentage : 0f;
            Debug.Log($"[GameManager] ¡VICTORIA! Vida restante del Pilar: {vidaRestante}%");
            OnVictoria?.Invoke();
        }
        else
        {
            Debug.Log("[GameManager] DERROTA");
            OnDerrota?.Invoke();
        }

        if (CurrentResult != null)
            OnMatchResult?.Invoke(CurrentResult);
    }

    private bool TryCreatePilarHealthSnapshot(out PilarHealthSnapshot snapshot)
    {
        snapshot = default(PilarHealthSnapshot);
        if (pilar == null)
        {
            Debug.LogError("[GameManager] No se puede publicar el resultado: no se encontró el Pilar.");
            return false;
        }

        if (!PilarHealthSnapshot.TryCreate(pilar.VidaActual, pilar.vidaMaxima, out snapshot))
        {
            Debug.LogError($"[GameManager] No se puede publicar el resultado: salud inválida del Pilar (restante={pilar.VidaActual}, máxima={pilar.vidaMaxima}).");
            return false;
        }

        return true;
    }

    /// <summary>Notifies the manager that a player was downed.</summary>
    /// <param name="p">The downed player.</param>
    public void NotificarJugadorDerribado(PlayerController p)
    {
        VerificarDerrotaCoop();
    }

    /// <summary>Notifies the manager that a player was revived.</summary>
    /// <param name="p">The revived player.</param>
    public void NotificarJugadorReanimado(PlayerController p)
    {
        // No hace falta acción, solo log
    }

    /// <summary>Registers a player.</summary>
    /// <param name="jugador">The player to register.</param>
    /// <returns><see langword="true"/> when registration succeeds.</returns>
    public bool RegisterPlayer(PlayerController jugador)
    {
        if (jugador == null) return false;
        if (playerRoster == null) InitializePlayerRoster();
        if (!playerRoster.Register(jugador)) return false;

        if (player == null) player = jugador;
        SuscribirEventosJugador(jugador);
        OnPlayerRegistered?.Invoke(jugador);
        return true;
    }

    /// <summary>Unregisters a player.</summary>
    /// <param name="jugador">The player to unregister.</param>
    /// <returns><see langword="true"/> when unregistration succeeds.</returns>
    public bool UnregisterPlayer(PlayerController jugador)
    {
        if (jugador == null || playerRoster == null || !playerRoster.Unregister(jugador))
            return false;

        DesuscribirEventosJugador(jugador);
        if (player == jugador)
            player = Players.Count > 0 ? Players[0] : null;
        OnPlayerUnregistered?.Invoke(jugador);
        VerificarDerrotaCoop();
        return true;
    }

    void SuscribirEventosJugador(PlayerController jugador)
    {
        if (jugador == null || !jugadoresSuscritos.Add(jugador)) return;

        jugador.OnDerribado += OnJugadorDerribado;
        jugador.OnReanimado += OnJugadorReanimado;
        jugador.OnCommandIssued += OnJugadorCommandIssued;
    }

    void DesuscribirEventosJugador(PlayerController jugador)
    {
        if (jugador == null || !jugadoresSuscritos.Remove(jugador)) return;

        jugador.OnDerribado -= OnJugadorDerribado;
        jugador.OnReanimado -= OnJugadorReanimado;
        jugador.OnCommandIssued -= OnJugadorCommandIssued;
    }

    void OnJugadorDerribado(PlayerController jugador)
    {
        VerificarDerrotaCoop();
    }

    void OnJugadorReanimado(PlayerController jugador)
    {
        Debug.Log("[GameManager] Jugador reanimado - derrota evitada");
    }

    void OnJugadorCommandIssued(PlayerController jugador, PlayerCommand command)
    {
        if (jugador != player) return;
        if (EstadoActual != MatchState.WaitingToStart || !esperandoInputInicial) return;
        if (!DetectarInputInicio(command)) return;

        esperandoInputInicial = false;
        IniciarJuego();
    }

    void VerificarDerrotaCoop()
    {
        if (!juegoActivo || playerRoster == null || playerRoster.Count == 0) return;

        if (playerRoster.AreAllDowned)
        {
            Debug.Log($"[GameManager] Todos los {playerRoster.Count} jugadores derribados - Derrota co-op");
            DerrotaPorJugadores();
        }
        else
        {
            Debug.Log($"[GameManager] Jugador derribado, quedan {playerRoster.StandingCount}/{playerRoster.Count} en pie - Reanimación posible (E)");
        }
    }

    bool DetectarInputInicio(PlayerCommand command)
    {
        if (command.MoveX != 0f || command.MoveY != 0f)
            return true;
        if (new Vector2(command.LookX, command.LookY).magnitude > NoInputLookThreshold)
            return true;
        return command.Jump || command.Fire;
    }

    void OnDestroy()
    {
        foreach (var jugador in jugadoresSuscritos)
        {
            if (jugador == null) continue;
            jugador.OnDerribado -= OnJugadorDerribado;
            jugador.OnReanimado -= OnJugadorReanimado;
            jugador.OnCommandIssued -= OnJugadorCommandIssued;
        }
        jugadoresSuscritos.Clear();

        if (Instance == this)
        {
            Time.timeScale = InitialTimeScale;
            Instance = null;
        }
    }
}
