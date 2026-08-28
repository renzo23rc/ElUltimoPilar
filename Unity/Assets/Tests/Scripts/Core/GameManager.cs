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
    
    [Header("Estado Actual")]
    public int oleadaActual = 0;
    public bool juegoActivo = false;
    public bool juegoPausado = false;
    
    // Eventos para que otros sistemas se suscriban
    public event Action<int> OnOleadaIniciada;
    public event Action<int> OnOleadaCompletada;
    public event Action OnVictoria;
    public event Action OnDerrota;
    public event Action OnJuegoIniciado;
    
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
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        
        if (pilar == null)
        {
            Debug.LogError("[GameManager] No se encontró el Pilar en la escena.");
            return;
        }
        if (spawner == null)
        {
            Debug.LogWarning("[GameManager] No se encontró el Spawner. Las oleadas no funcionarán.");
        }
        if (player == null)
        {
            Debug.LogWarning("[GameManager] No se encontró el Jugador.");
        }

        // Suscribirse a eventos de derribado para todos los jugadores (co-op, derrota si todos derribados)
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in jugadores)
        {
            p.OnDerribado += _ => VerificarDerrotaCoop();
            p.OnReanimado += _ => Debug.Log($"[GameManager] Jugador reanimado - derrota evitada");
        }
            
        if (iniciarAlMover)
        {
            juegoActivo = false;
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
            if (esperandoInputInicial && DetectarInputInicio())
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
        juegoActivo = true;
        juegoPausado = false;
        oleadaActual = 0;
        
        pilar?.RestaurarVida();
        // Restaurar jugadores si estaban derribados
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var j in jugadores)
        {
            if (j.estaDerribado) j.Reanimar();
            j.vidaActual = j.vidaMaxima;
            // Re-suscribir si son nuevos
            j.OnDerribado -= _ => VerificarDerrotaCoop();
            j.OnDerribado += _ => VerificarDerrotaCoop();
        }
        OnJuegoIniciado?.Invoke();
        
        IniciarSiguienteOleada();
    }

    void IniciarSiguienteOleada()
    {
        if (oleadaActual >= totalOleadas)
        {
            Victoria();
            return;
        }
        
        oleadaActual++;
        spawner?.IniciarOleada(oleadaActual);
        OnOleadaIniciada?.Invoke(oleadaActual);
        
        Debug.Log($"[GameManager] Oleada {oleadaActual}/{totalOleadas} iniciada");
    }

    void CompletarOleadaActual()
    {
        OnOleadaCompletada?.Invoke(oleadaActual);
        Debug.Log($"[GameManager] Oleada {oleadaActual} completada");
        
        // Dar munición al final de oleada (según GDD) - a todos los jugadores si co-op
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (jugadores.Length > 0) foreach (var j in jugadores) j.ReponerMunicion();
        else player?.ReponerMunicion();
        
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
        juegoActivo = false;
        float vidaRestante = pilar != null ? pilar.VidaActual : 0;
        Debug.Log($"[GameManager] ¡VICTORIA! Vida restante del Pilar: {vidaRestante}%");
        OnVictoria?.Invoke();
    }

    public void Derrota()
    {
        if (!juegoActivo) return; // Evitar doble derrota
        juegoActivo = false;
        Debug.Log("[GameManager] DERROTA - El Pilar ha caído");
        OnDerrota?.Invoke();
    }

    public void DerrotaPorJugadores()
    {
        if (!juegoActivo) return;
        juegoActivo = false;
        Debug.Log("[GameManager] DERROTA - Todos los jugadores derribados");
        OnDerrota?.Invoke();
    }

    public void NotificarJugadorDerribado(PlayerController p)
    {
        VerificarDerrotaCoop();
    }

    public void NotificarJugadorReanimado(PlayerController p)
    {
        // No hace falta acción, solo log
    }

    void VerificarDerrotaCoop()
    {
        if (!juegoActivo) return;
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (jugadores.Length == 0) return;
        bool todosDerribados = true;
        foreach (var j in jugadores)
        {
            if (!j.estaDerribado)
            {
                todosDerribados = false;
                break;
            }
        }
        if (todosDerribados)
        {
            Debug.Log($"[GameManager] Todos los {jugadores.Length} jugadores derribados - Derrota co-op");
            DerrotaPorJugadores();
        }
        else
        {
            int vivos = 0; foreach (var j in jugadores) if (!j.estaDerribado) vivos++;
            Debug.Log($"[GameManager] Jugador derribado, quedan {vivos}/{jugadores.Length} en pie - Reanimación posible (E)");
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
        if (Instance == this) Instance = null;
    }
}
