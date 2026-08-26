/**
 * GameManager.cs
 * Controla el flujo de la partida: oleadas, estado del juego,
 * victoria/derrota, y comunicación entre sistemas.
 * 
 * Colocar en un GameObject vacío "GameManager" en la escena.
 */
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Referencias")]
    public Pilar pilar;
    public EnemySpawner spawner;
    public PlayerController player;
    
    [Header("Configuración de Oleadas")]
    public int totalOleadas = 10;
    public float tiempoEntreOleadas = 5f;
    
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
            
        // Auto-iniciar para testing
        IniciarJuego();
    }

    void Update()
    {
        if (!juegoActivo || juegoPausado) return;
        
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
        
        // Dar munición al final de oleada (según GDD)
        player?.ReponerMunicion();
        
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
        juegoActivo = false;
        Debug.Log("[GameManager] DERROTA - El Pilar ha caído");
        OnDerrota?.Invoke();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
