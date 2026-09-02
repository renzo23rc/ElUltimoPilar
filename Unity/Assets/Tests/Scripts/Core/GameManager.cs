/**
 * GameManager.cs
 * Controla el flujo de la partida: oleadas, estado del juego,
 * victoria/derrota, y comunicación entre sistemas.
 *
 * Colocar en un GameObject vacío "GameManager" en la escena.
 */
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias")]
    public Pilar pilar;
    public EnemySpawner spawner;
    public PlayerController player;

    [Header("Configuración de Oleadas")]
    public int totalOleadas = 10; // B1: 10 oleadas escalables 12-20min
    public float tiempoEntreOleadas = 7f; // Balanceo: 5->7s para respiro táctico y decisión energía/munición

    [Header("Configuración de Jugadores")]
    [Range(PlayerRoster<PlayerController>.MinimumCapacity, PlayerRoster<PlayerController>.MaximumCapacity)]
    public int maxPlayers = PlayerRoster<PlayerController>.MaximumCapacity;

    [Header("Estado Actual")]
    public int oleadaActual => matchFlow?.CurrentWave ?? 0;
    public bool juegoActivo => matchFlow != null &&
        (matchFlow.State == MatchState.Playing || matchFlow.State == MatchState.Paused);
    public bool juegoPausado => matchFlow?.State == MatchState.Paused;
    public MatchState EstadoActual => matchFlow?.State ?? MatchState.WaitingToStart;
    public MatchResult CurrentResult { get; private set; }

    private MatchFlow matchFlow;
    private PlayerRoster<PlayerController> playerRoster;
    private readonly HashSet<PlayerController> jugadoresSuscritos = new HashSet<PlayerController>();

    public IReadOnlyList<PlayerController> Players =>
        playerRoster?.Players ?? Array.Empty<PlayerController>();
    public int PlayerCount => playerRoster?.Count ?? 0;

    // Eventos para que otros sistemas se suscriban
    public event Action<int> OnOleadaIniciada;
    public event Action<int> OnOleadaCompletada;
    public event Action OnVictoria;
    public event Action OnDerrota;
    public event Action<MatchResult> OnMatchResult;
    public event Action OnJuegoIniciado;
    public event Action<PlayerController> OnPlayerRegistered;
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
    public bool iniciarAlMover = true;
    private bool esperandoInputInicial = true;

    void Start()
    {
        if (pilar == null)
            pilar = FindFirstObjectByType<Pilar>();
        if (spawner == null)
            spawner = FindFirstObjectByType<EnemySpawner>();

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

        if (iniciarAlMover)
        {
            matchFlow.Reset();
            CurrentResult = null;
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
        if (!juegoActivo)
        {
            if (EstadoActual == MatchState.WaitingToStart && esperandoInputInicial && DetectarInputInicio())
            {
                esperandoInputInicial = false;
                IniciarJuego();
            }
            return;
        }
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

    public void IniciarJuego()
    {
        if (EstadoActual != MatchState.WaitingToStart)
            matchFlow.Reset();
        CurrentResult = null;
        if (!matchFlow.Start()) return;

        Time.timeScale = 1f;
        esperandoInputInicial = false;
        esperandoOleada = false;
        timerEntreOleadas = 0f;

        pilar?.RestaurarVida();
        // Restaurar jugadores si estaban derribados
        foreach (var jugadorRegistrado in Players)
        {
            if (jugadorRegistrado.estaDerribado) jugadorRegistrado.Reanimar();
            jugadorRegistrado.vidaActual = jugadorRegistrado.vidaMaxima;
        }
        OnJuegoIniciado?.Invoke();

        IniciarSiguienteOleada();
    }

    public void PausarJuego()
    {
        if (!matchFlow.Pause()) return;
        Time.timeScale = 0f;
    }

    public void ReanudarJuego()
    {
        if (!matchFlow.Resume()) return;
        Time.timeScale = 1f;
    }

    public void ReiniciarJuego()
    {
        Time.timeScale = 1f;
        matchFlow.Reset();
        CurrentResult = null;
        esperandoInputInicial = false;
        esperandoOleada = false;
        timerEntreOleadas = 0f;
        spawner?.LimpiarTodos();
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

    public void Victoria()
    {
        PublishTerminalResult(MatchState.Victory);
    }

    public void Derrota()
    {
        PublishTerminalResult(MatchState.Defeat);
    }

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
            Debug.Log("[GameManager] DERROTA - El Pilar ha caído");
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

    public void NotificarJugadorDerribado(PlayerController p)
    {
        VerificarDerrotaCoop();
    }

    public void NotificarJugadorReanimado(PlayerController p)
    {
        // No hace falta acción, solo log
    }

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
    }

    void DesuscribirEventosJugador(PlayerController jugador)
    {
        if (jugador == null || !jugadoresSuscritos.Remove(jugador)) return;

        jugador.OnDerribado -= OnJugadorDerribado;
        jugador.OnReanimado -= OnJugadorReanimado;
    }

    void OnJugadorDerribado(PlayerController jugador)
    {
        VerificarDerrotaCoop();
    }

    void OnJugadorReanimado(PlayerController jugador)
    {
        Debug.Log("[GameManager] Jugador reanimado - derrota evitada");
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

    bool DetectarInputInicio()
    {
        if (Keyboard.current == null) return false;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed ||
            Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.spaceKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            return true;
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.magnitude > 2f) return true;
            if (Mouse.current.leftButton.wasPressedThisFrame) return true;
        }
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
        return false;
    }

    void OnDestroy()
    {
        foreach (var jugador in jugadoresSuscritos)
        {
            if (jugador == null) continue;
            jugador.OnDerribado -= OnJugadorDerribado;
            jugador.OnReanimado -= OnJugadorReanimado;
        }
        jugadoresSuscritos.Clear();

        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}
