/**
 * Hud.cs
 * HUD definitivo de Último Pilar.
 * Muestra: Vida del Pilar, estado de hasta 4 jugadores, Energía, Oleada,
 * Munición, variante temporal, menús de inicio/pausa y pantalla de resultado
 * con puntaje. Incluye crosshair central con flash de impacto.
 * 
 * Colocar en un GameObject en la escena (o se genera automáticamente).
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Displays match, Pilar, player, weapon, and result information.
/// </summary>
public class Hud : MonoBehaviour
{
    private const float PercentageScale = 100f;
    private const float VictoryMessageDurationSeconds = 5f;
    private const float WaveMessageDurationSeconds = 2f;
    private const float DownedMessageDurationSeconds = 3f;
    private const float KillCrosshairDurationSeconds = 0.35f;
    private const float HitCrosshairDurationSeconds = 0.15f;

    private const float HudPadding = 10f;
    private const float HudTopOffsetVidaPilar = 30f;
    private const float HudTopOffsetBarraPilar = 60f;
    private const float HudTopOffsetVidaJugador = 100f;
    private const float HudTopOffsetBarraJugador = 125f;
    private const float HudTopOffsetEnergia = 155f;
    private const float HudTopOffsetBarraEnergia = 180f;
    private const float HudTopOffsetOleada = 30f;
    private const float HudTopOffsetMunicion = 60f;
    private const float HudTopOffsetArma = 85f;
    private const float HudTopOffsetVariante = 70f;
    private const float HudTopOffsetMenu = 220f;
    private const float HudTopOffsetResultado = 300f;
    private const float HudTopOffsetPuntaje = 360f;
    private const float HudBarWidthPilar = 300f;
    private const float HudBarWidthJugador = 200f;
    private const float HudBarHeightPilar = 20f;
    private const float HudBarHeightJugador = 15f;

    [Header("Referencias")]
    /// <summary>Gets or sets the Pilar reference.</summary>
    public Pilar pilar;
    /// <summary>Gets or sets the primary player reference.</summary>
    public PlayerController jugador;
    /// <summary>Gets or sets the primary player's energy system.</summary>
    public EnergySystem energia;
    /// <summary>Gets or sets the primary player's weapon system.</summary>
    public WeaponSystem armas;
    /// <summary>Gets or sets the match manager reference.</summary>
    public GameManager gameManager;
    
    [Header("UI Elements")]
    /// <summary>Gets or sets the Pilar health label.</summary>
    public Text textoVidaPilar;
    /// <summary>Gets or sets the player health label.</summary>
    public Text textoVidaJugador;
    /// <summary>Gets or sets the energy label.</summary>
    public Text textoEnergia;
    /// <summary>Gets or sets the wave label.</summary>
    public Text textoOleada;
    /// <summary>Gets or sets the ammunition label.</summary>
    public Text textoMunicion;
    /// <summary>Gets or sets the weapon label.</summary>
    public Text textoArma;
    /// <summary>Gets or sets the message label.</summary>
    public Text textoMensaje;

    [Header("Co-op y resultado")]
    /// <summary>Gets or sets the menu label.</summary>
    public Text textoMenu;
    /// <summary>Gets or sets the result label.</summary>
    public Text textoResultado;
    /// <summary>Gets or sets the score label.</summary>
    public Text textoPuntaje;
    /// <summary>Gets or sets the weapon variant label.</summary>
    public Text textoVariante;
    /// <summary>Gets or sets the crosshair label.</summary>
    public Text textoCrosshair;
    /// <summary>Gets or sets the player rows container.</summary>
    public Transform filasJugadores;

    [Header("Barras")]
    /// <summary>Gets or sets the Pilar health bar.</summary>
    public Image barraVidaPilar;
    /// <summary>Gets or sets the player health bar.</summary>
    public Image barraVidaJugador;
    /// <summary>Gets or sets the energy bar.</summary>
    public Image barraEnergia;
    
    [Header("Colores por fase del Pilar")]
    public Color colorFase1 = Color.cyan;
    public Color colorFase2 = Color.yellow;
    public Color colorFase3 = new Color(1f, 0.5f, 0f);
    public Color colorFase4 = Color.red;

    private readonly HashSet<PlayerController> jugadoresSuscritos = new HashSet<PlayerController>();
    private GameManager managerSuscrito;
    private bool managerEventosSuscritos;
    private readonly List<Text> textosFilaJugadores = new List<Text>();
    private int conteoFilas;
    private float crosshairTimer;

    void Start()
    {
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        SeleccionarJugadorPrincipal();
        ConfigurarReferenciasJugador(false);
        
        // Crear UI si no existe
        if (textoVidaPilar == null)
            CrearUI();
        else
            SincronizarHudExistenteARelativo();

        ConfigurarCrosshairRelativo();
        SuscribirEventosGameManager();
        CombatFeedback.OnCombatHit += FlashCrosshair;
    }

    void SeleccionarJugadorPrincipal()
    {
        if (jugador != null || gameManager == null || gameManager.Players.Count == 0) return;
        jugador = gameManager.Players[0];
    }

    void ConfigurarReferenciasJugador(bool reemplazar)
    {
        if (jugador == null)
        {
            energia = null;
            armas = null;
            return;
        }

        if (reemplazar || energia == null) energia = jugador.GetComponent<EnergySystem>();
        if (reemplazar || armas == null) armas = jugador.GetComponent<WeaponSystem>();
    }

    void SuscribirEventosGameManager()
    {
        if (gameManager == null || managerSuscrito == gameManager) return;

        if (managerEventosSuscritos)
        {
            if (managerSuscrito != null)
            {
                managerSuscrito.OnVictoria -= ManejarVictoria;
                managerSuscrito.OnDerrota -= ManejarDerrota;
                managerSuscrito.OnOleadaIniciada -= ManejarOleadaIniciada;
                managerSuscrito.OnPlayerRegistered -= ManejarJugadorRegistrado;
                managerSuscrito.OnPlayerUnregistered -= ManejarJugadorDesregistrado;
                managerSuscrito.OnMatchResult -= ManejarResultado;
            }

            DesuscribirTodosLosJugadores();
            jugador = null;
        }

        managerSuscrito = gameManager;
        managerSuscrito.OnVictoria += ManejarVictoria;
        managerSuscrito.OnDerrota += ManejarDerrota;
        managerSuscrito.OnOleadaIniciada += ManejarOleadaIniciada;
        managerSuscrito.OnPlayerRegistered += ManejarJugadorRegistrado;
        managerSuscrito.OnPlayerUnregistered += ManejarJugadorDesregistrado;
        managerSuscrito.OnMatchResult += ManejarResultado;
        managerEventosSuscritos = true;
        SuscribirJugadores();
    }

    void SuscribirJugadores()
    {
        if (gameManager == null) return;

        foreach (var jugadorRegistrado in gameManager.Players)
            SuscribirJugador(jugadorRegistrado);

        SeleccionarJugadorPrincipal();
        ConfigurarReferenciasJugador(false);
    }

    void SuscribirJugador(PlayerController jugadorRegistrado)
    {
        if (jugadorRegistrado == null || !jugadoresSuscritos.Add(jugadorRegistrado)) return;

        jugadorRegistrado.OnDerribado += ManejarJugadorDerribado;
        jugadorRegistrado.OnReanimado += ManejarJugadorReanimado;
    }

    void DesuscribirJugador(PlayerController jugadorRegistrado)
    {
        if (jugadorRegistrado == null || !jugadoresSuscritos.Remove(jugadorRegistrado)) return;

        jugadorRegistrado.OnDerribado -= ManejarJugadorDerribado;
        jugadorRegistrado.OnReanimado -= ManejarJugadorReanimado;
    }

    void DesuscribirTodosLosJugadores()
    {
        foreach (var jugadorRegistrado in jugadoresSuscritos)
        {
            if (jugadorRegistrado == null) continue;

            jugadorRegistrado.OnDerribado -= ManejarJugadorDerribado;
            jugadorRegistrado.OnReanimado -= ManejarJugadorReanimado;
        }

        jugadoresSuscritos.Clear();
    }

    void ManejarJugadorRegistrado(PlayerController jugadorRegistrado)
    {
        SuscribirJugador(jugadorRegistrado);
        ReconstruirFilasJugadores();
        if (jugador != null) return;

        jugador = jugadorRegistrado;
        ConfigurarReferenciasJugador(true);
    }

    void ManejarJugadorDesregistrado(PlayerController jugadorDesregistrado)
    {
        DesuscribirJugador(jugadorDesregistrado);
        ReconstruirFilasJugadores();
        if (jugador != jugadorDesregistrado) return;

        jugador = null;
        SeleccionarJugadorPrincipal();
        ConfigurarReferenciasJugador(true);
    }

    void ManejarVictoria()
    {
        MostrarMensaje("¡VICTORIA!");
    }

    void ManejarDerrota()
    {
        MostrarMensaje("DERROTA");
    }

    void ManejarOleadaIniciada(int oleada)
    {
        MostrarMensaje($"Oleada {oleada}", WaveMessageDurationSeconds);
    }

    void ManejarJugadorDerribado(PlayerController jugador)
    {
        MostrarMensaje($"¡{jugador.name} DERRIBADO! - Reanima con [E]", DownedMessageDurationSeconds);
    }

    void ManejarJugadorReanimado(PlayerController jugador)
    {
        MostrarMensaje($"{jugador.name} reanimado!", WaveMessageDurationSeconds);
    }
    
    void ManejarResultado(MatchResult resultado)
    {
        if (resultado == null) return;
        string texto = resultado.Outcome == MatchState.Victory
            ? $"¡VICTORIA! Puntaje: {resultado.Score}"
            : $"DERROTA — Puntaje: {resultado.Score}";
        MostrarMensaje(texto, VictoryMessageDurationSeconds);
    }
    
    /// <summary>Flashes the crosshair for a hit or kill.</summary>
    /// <param name="mato">Whether the hit killed its target.</param>
    public void FlashCrosshair(bool mato)
    {
        crosshairTimer = mato ? KillCrosshairDurationSeconds : HitCrosshairDurationSeconds;
        if (textoCrosshair != null)
            textoCrosshair.color = mato ? Color.red : Color.yellow;
    }

    void Update()
    {
        // Auto-referencia si se generó después de Start (TestSceneSetup genera en Start)
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        SuscribirEventosGameManager();
        if (jugador == null)
        {
            SeleccionarJugadorPrincipal();
            ConfigurarReferenciasJugador(false);
        }

        // Vida del Pilar

        if (pilar != null)
        {
            float pct = pilar.PorcentajeVida / PercentageScale;
            if (textoVidaPilar != null)
                textoVidaPilar.text = $"Pilar: {pilar.VidaActual:F0}%";
            if (barraVidaPilar != null)
            {
                barraVidaPilar.fillAmount = pct;
                barraVidaPilar.color = ObtenerColorFase(pilar.faseActual);
            }
        }
        
        // Vida del Jugador + estado derribado (co-op)
        if (jugador != null)
        {
            float pctJ = jugador.vidaActual / jugador.vidaMaxima;
            if (textoVidaJugador != null)
            {
                if (jugador.estaDerribado)
                    textoVidaJugador.text = $"Vida: DERRIBADO - [E] a {jugador.rangoReanimacion:F0}m para reanimar!";
                else
                    textoVidaJugador.text = $"Vida: {jugador.vidaActual:F0}/{jugador.vidaMaxima:F0}";
                textoVidaJugador.color = jugador.estaDerribado ? Color.red : Color.white;
            }
            if (barraVidaJugador != null)
            {
                barraVidaJugador.fillAmount = jugador.estaDerribado ? 0f : pctJ;
                barraVidaJugador.color = jugador.estaDerribado ? Color.red : Color.green;
            }
            // Mostrar prompt si aliado cerca derribado
            if (!jugador.estaDerribado && gameManager != null)
            {
                foreach (var jugadorRegistrado in gameManager.Players)
                {
                    if (jugadorRegistrado == jugador || !jugadorRegistrado.estaDerribado) continue;
                    float d = Vector3.Distance(jugador.transform.position, jugadorRegistrado.transform.position);
                    if (d <= jugador.rangoReanimacion + 0.5f && textoMensaje != null && string.IsNullOrEmpty(textoMensaje.text))
                    {
                        textoMensaje.text = $"Presiona [E] para reanimar a {jugadorRegistrado.name} ({d:F1}m)";
                        textoMensaje.color = Color.yellow;
                        break;
                    }
                }
            }

        }
        
        // Energía
        if (energia != null)
        {
            float pctE = energia.energiaActual / energia.energiaMaxima;
            if (textoEnergia != null)
                textoEnergia.text = $"Energía: {energia.energiaActual:F0}/{energia.energiaMaxima:F0}";
            if (barraEnergia != null)
                barraEnergia.fillAmount = pctE;
        }
        
        // Oleada
        if (gameManager != null && textoOleada != null)
        {
            textoOleada.text = $"Oleada: {gameManager.oleadaActual}/{gameManager.totalOleadas}";
        }
        
        // Munición
        if (armas != null && textoMunicion != null)
        {
            var arma = armas.ObtenerArmaActual();
            if (arma != null)
            {
                if (arma.municionMaxima < 0)
                    textoMunicion.text = "Munición: ∞";
                else
                    textoMunicion.text = $"Munición: {arma.municionActual}/{arma.municionMaxima}";
            }
        }
        
        // Arma actual
        if (armas != null && textoArma != null)
        {
            textoArma.text = $"Arma: {armas.ObtenerArmaActual()?.nombre}";
        }
        
        ActualizarOverlays();
        ActualizarFilasJugadores();
        ActualizarVariante();
        ActualizarCrosshair();
        AtajosMenu();
        
        // R es exclusivamente un atajo de depuración; el gameplay usa PlayerCommand.
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
            if (pilar != null)
            {
                // R funciona siempre, incluso antes de WASD (fuerza inicio para que arena/torretas disparen)
                if (gameManager != null && !gameManager.juegoActivo)
                {
                    Debug.Log("[Hud] R: forzando inicio de juego para test de fases");
                    gameManager.IniciarJuego();
                }
                pilar.AplicarDañoPrueba(10f);
                Debug.Log($"[Hud] R: pilar dañado 10% -> {pilar.vidaActual}% fase {pilar.faseActual}");
            }
            else
            {
                Debug.LogWarning("[Hud] R: pilar aún null (¿generación no completada?)");
            }
        }
    }

    void ReconstruirFilasJugadores()
    {
        textosFilaJugadores.Clear();
        conteoFilas = 0;
        if (filasJugadores == null || gameManager == null) return;
        foreach (Transform hijo in filasJugadores)
            Destroy(hijo.gameObject);
        int i = 0;
        foreach (var p in gameManager.Players)
        {
            if (p == null) continue;
            Text fila = CrearTexto(filasJugadores, $"Fila{i}", Vector2.zero, 19);
            ConfigurarAnclaTopRight(fila.GetComponent<RectTransform>(), new Vector2(0f, i * 28f), new Vector2(260f, 26f));
            fila.alignment = TextAnchor.UpperRight;
            textosFilaJugadores.Add(fila);
            i++;
        }
        conteoFilas = i;
    }
    
    void ActualizarFilasJugadores()
    {
        if (gameManager == null) return;
        if (gameManager.PlayerCount != conteoFilas)
            ReconstruirFilasJugadores();
        int i = 0;
        foreach (var p in gameManager.Players)
        {
            if (p == null || i >= textosFilaJugadores.Count) continue;
            var texto = textosFilaJugadores[i];
            if (texto != null)
            {
                texto.text = p.estaDerribado
                    ? $"P{i + 1} {p.name}: DERRIBADO"
                    : $"P{i + 1} {p.name}: {p.vidaActual:F0}/{p.vidaMaxima:F0}";
                texto.color = p.estaDerribado ? Color.red : Color.white;
            }
            i++;
        }
    }
    
    void ActualizarOverlays()
    {
        if (gameManager == null) return;
        var estado = gameManager.EstadoActual;
        bool terminal = estado == MatchState.Victory || estado == MatchState.Defeat;
        if (textoMenu != null)
        {
            if (estado == MatchState.WaitingToStart)
                textoMenu.text = "ÚLTIMO PILAR\nMovete o dispará para iniciar\nWASD + Mouse (P1) · Start en gamepad (P2-P4)";
            else if (estado == MatchState.Paused)
                textoMenu.text = "PAUSA\nEsc: continuar · Enter: reiniciar";
            else
                textoMenu.text = "";
        }
        if (textoResultado != null && textoPuntaje != null)
        {
            if (terminal)
            {
                var resultado = gameManager.CurrentResult;
                int puntaje = resultado != null ? resultado.Score : 0;
                textoResultado.text = estado == MatchState.Victory ? "¡VICTORIA!" : "DERROTA";
                textoResultado.color = estado == MatchState.Victory ? Color.green : Color.red;
                textoPuntaje.text = $"Puntaje: {puntaje}\nEnter: reiniciar";
            }
            else
            {
                textoResultado.text = "";
                textoPuntaje.text = "";
            }
        }
    }
    
    public static string GetVariantDisplayName(bool variantIsActive, string semanticDisplayName)
    {
        if (!variantIsActive || string.IsNullOrEmpty(semanticDisplayName))
            return string.Empty;

        return semanticDisplayName;
    }

    void ActualizarVariante()
    {
        if (textoVariante == null) return;

        string displayName = armas == null
            ? string.Empty
            : GetVariantDisplayName(armas.VarianteActiva, armas.ActiveVariantDisplayName);
        if (string.IsNullOrEmpty(displayName))
        {
            textoVariante.text = "";
            return;
        }

        textoVariante.text = $"¡x{armas.multiplicadorVariante:F0} {displayName}! {armas.tiempoVarianteRestante:F0}s";
        textoVariante.color = new Color(1f, 0.55f, 0.1f);
    }

    void ConfigurarCrosshairRelativo()
    {
        if (textoCrosshair == null) return;
        RectTransform rect = textoCrosshair.GetComponent<RectTransform>();
        if (rect == null) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(50f, 50f);
        textoCrosshair.alignment = TextAnchor.MiddleCenter;
        textoCrosshair.fontSize = 34;
    }

    void SincronizarHudExistenteARelativo()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        if (textoVidaPilar != null)
        {
            textoVidaPilar.fontSize = 28;
            ConfigurarAnclaTopLeft(textoVidaPilar.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetVidaPilar), new Vector2(440f, 44f));
        }
        if (barraVidaPilar != null) ConfigurarAnclaTopLeft(barraVidaPilar.transform.parent.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetBarraPilar), new Vector2(HudBarWidthPilar, HudBarHeightPilar));
        if (textoVidaJugador != null)
        {
            textoVidaJugador.fontSize = 24;
            ConfigurarAnclaTopLeft(textoVidaJugador.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetVidaJugador), new Vector2(440f, 44f));
        }
        if (barraVidaJugador != null) ConfigurarAnclaTopLeft(barraVidaJugador.transform.parent.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetBarraJugador), new Vector2(HudBarWidthJugador, HudBarHeightJugador));
        if (textoEnergia != null)
        {
            textoEnergia.fontSize = 24;
            ConfigurarAnclaTopLeft(textoEnergia.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetEnergia), new Vector2(440f, 44f));
        }
        if (barraEnergia != null) ConfigurarAnclaTopLeft(barraEnergia.transform.parent.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetBarraEnergia), new Vector2(HudBarWidthJugador, HudBarHeightJugador));
        if (textoOleada != null)
        {
            textoOleada.fontSize = 28;
            ConfigurarAnclaTopRight(textoOleada.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetOleada), new Vector2(340f, 44f));
        }
        if (textoMunicion != null)
        {
            textoMunicion.fontSize = 24;
            ConfigurarAnclaTopRight(textoMunicion.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetMunicion), new Vector2(340f, 44f));
        }
        if (textoArma != null)
        {
            textoArma.fontSize = 22;
            ConfigurarAnclaTopRight(textoArma.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetArma), new Vector2(340f, 44f));
        }
        if (textoMensaje != null)
        {
            textoMensaje.fontSize = 40;
            ConfigurarAnclaCentro(textoMensaje.GetComponent<RectTransform>(), Vector2.zero, new Vector2(660f, 70f));
        }
        if (textoMenu != null)
        {
            textoMenu.fontSize = 34;
            ConfigurarAnclaTopCenter(textoMenu.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetMenu), new Vector2(660f, 60f));
        }
        if (textoResultado != null)
        {
            textoResultado.fontSize = 50;
            ConfigurarAnclaTopCenter(textoResultado.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetResultado), new Vector2(660f, 70f));
        }
        if (textoPuntaje != null)
        {
            textoPuntaje.fontSize = 30;
            ConfigurarAnclaTopCenter(textoPuntaje.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetPuntaje), new Vector2(660f, 60f));
        }
        if (textoVariante != null)
        {
            textoVariante.fontSize = 26;
            ConfigurarAnclaTopCenter(textoVariante.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetVariante), new Vector2(460f, 44f));
        }
        if (filasJugadores != null)
        {
            RectTransform filasRect = filasJugadores.GetComponent<RectTransform>();
            if (filasRect != null) ConfigurarAnclaTopRight(filasRect, new Vector2(HudPadding, 130f), new Vector2(260f, 200f));
            for (int i = 0; i < textosFilaJugadores.Count; i++)
            {
                Text t = textosFilaJugadores[i];
                if (t == null) continue;
                t.fontSize = 19;
                ConfigurarAnclaTopRight(t.GetComponent<RectTransform>(), new Vector2(0f, i * 28f), new Vector2(260f, 26f));
                t.alignment = TextAnchor.UpperRight;
            }
        }
    }

    void ActualizarCrosshair()
    {
        if (textoCrosshair == null) return;
        if (crosshairTimer > 0f)
        {
            crosshairTimer -= Time.unscaledDeltaTime;
            if (crosshairTimer <= 0f)
                textoCrosshair.color = Color.white;
        }
    }
    
    void AtajosMenu()
    {
        if (gameManager == null || Keyboard.current == null) return;
        var estado = gameManager.EstadoActual;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (estado == MatchState.Playing) gameManager.PausarJuego();
            else if (estado == MatchState.Paused) gameManager.ReanudarJuego();
        }
        if (Keyboard.current.enterKey.wasPressedThisFrame
            && (estado == MatchState.Victory || estado == MatchState.Defeat || estado == MatchState.Paused))
        {
            gameManager.ReiniciarJuego();
        }
    }
    
    Color ObtenerColorFase(int fase)
    {
        return fase switch
        {
            1 => colorFase1,
            2 => colorFase2,
            3 => colorFase3,
            4 => colorFase4,
            _ => Color.white
        };
    }

    /// <summary>Displays a message for an optional duration.</summary>
    /// <param name="mensaje">The message to display.</param>
    /// <param name="duracion">The display duration in seconds.</param>
    public void MostrarMensaje(string mensaje, float duracion = 0)
    {
        if (textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            textoMensaje.color = Color.white;
            if (duracion > 0)
                Invoke(nameof(LimpiarMensaje), duracion);
        }
        Debug.Log($"[Hud] {mensaje}");
    }

    /// <summary>Displays a colored warning for an optional duration.</summary>
    /// <param name="mensaje">The warning to display.</param>
    /// <param name="color">The warning color.</param>
    /// <param name="duracion">The display duration in seconds.</param>
    public void MostrarAdvertencia(string mensaje, Color color, float duracion)
    {
        if (textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            textoMensaje.color = color;
            textoMensaje.fontSize = 40;
            if (duracion > 0)
                Invoke(nameof(LimpiarMensaje), duracion);
        }
        Debug.Log($"[Hud] ADVERTENCIA: {mensaje}");
    }

    void LimpiarMensaje()
    {
        if (textoMensaje != null)
            textoMensaje.text = "";
    }

    void OnDestroy()
    {
        if (managerEventosSuscritos && managerSuscrito != null)
        {
            managerSuscrito.OnVictoria -= ManejarVictoria;
            managerSuscrito.OnDerrota -= ManejarDerrota;
            managerSuscrito.OnOleadaIniciada -= ManejarOleadaIniciada;
            managerSuscrito.OnPlayerRegistered -= ManejarJugadorRegistrado;
            managerSuscrito.OnPlayerUnregistered -= ManejarJugadorDesregistrado;
            managerSuscrito.OnMatchResult -= ManejarResultado;
        }
        
        CombatFeedback.OnCombatHit -= FlashCrosshair;

        DesuscribirTodosLosJugadores();
        managerEventosSuscritos = false;
    }

    void CrearUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        textoVidaPilar = CrearTexto(canvas.transform, "VidaPilar", Vector2.zero, 28);
        ConfigurarAnclaTopLeft(textoVidaPilar.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetVidaPilar), new Vector2(440f, 44f));
        textoVidaPilar.alignment = TextAnchor.UpperLeft;

        barraVidaPilar = CrearBarra(canvas.transform, "BarraPilar", Vector2.zero, new Vector2(HudBarWidthPilar, HudBarHeightPilar));
        ConfigurarAnclaTopLeft(barraVidaPilar.transform.parent.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetBarraPilar), new Vector2(HudBarWidthPilar, HudBarHeightPilar));

        textoVidaJugador = CrearTexto(canvas.transform, "VidaJugador", Vector2.zero, 24);
        ConfigurarAnclaTopLeft(textoVidaJugador.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetVidaJugador), new Vector2(440f, 44f));
        textoVidaJugador.alignment = TextAnchor.UpperLeft;

        barraVidaJugador = CrearBarra(canvas.transform, "BarraJugador", Vector2.zero, new Vector2(HudBarWidthJugador, HudBarHeightJugador));
        ConfigurarAnclaTopLeft(barraVidaJugador.transform.parent.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetBarraJugador), new Vector2(HudBarWidthJugador, HudBarHeightJugador));

        textoEnergia = CrearTexto(canvas.transform, "Energia", Vector2.zero, 24);
        ConfigurarAnclaTopLeft(textoEnergia.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetEnergia), new Vector2(440f, 44f));
        textoEnergia.alignment = TextAnchor.UpperLeft;

        barraEnergia = CrearBarra(canvas.transform, "BarraEnergia", Vector2.zero, new Vector2(HudBarWidthJugador, HudBarHeightJugador));
        ConfigurarAnclaTopLeft(barraEnergia.transform.parent.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetBarraEnergia), new Vector2(HudBarWidthJugador, HudBarHeightJugador));

        textoOleada = CrearTexto(canvas.transform, "Oleada", Vector2.zero, 28);
        ConfigurarAnclaTopRight(textoOleada.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetOleada), new Vector2(340f, 44f));
        textoOleada.alignment = TextAnchor.UpperRight;

        textoMunicion = CrearTexto(canvas.transform, "Municion", Vector2.zero, 24);
        ConfigurarAnclaTopRight(textoMunicion.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetMunicion), new Vector2(340f, 44f));
        textoMunicion.alignment = TextAnchor.UpperRight;

        textoArma = CrearTexto(canvas.transform, "Arma", Vector2.zero, 22);
        ConfigurarAnclaTopRight(textoArma.GetComponent<RectTransform>(), new Vector2(HudPadding, HudTopOffsetArma), new Vector2(340f, 44f));
        textoArma.alignment = TextAnchor.UpperRight;

        textoMensaje = CrearTexto(canvas.transform, "Mensaje", Vector2.zero, 40);
        ConfigurarAnclaCentro(textoMensaje.GetComponent<RectTransform>(), Vector2.zero, new Vector2(660f, 70f));
        textoMensaje.alignment = TextAnchor.MiddleCenter;

        textoCrosshair = CrearTexto(canvas.transform, "Crosshair", Vector2.zero, 34);
        ConfigurarAnclaCentro(textoCrosshair.GetComponent<RectTransform>(), Vector2.zero, new Vector2(50f, 50f));
        textoCrosshair.alignment = TextAnchor.MiddleCenter;
        textoCrosshair.text = "+";

        textoMenu = CrearTexto(canvas.transform, "Menu", Vector2.zero, 34);
        ConfigurarAnclaTopCenter(textoMenu.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetMenu), new Vector2(660f, 60f));
        textoMenu.alignment = TextAnchor.MiddleCenter;

        textoResultado = CrearTexto(canvas.transform, "Resultado", Vector2.zero, 50);
        ConfigurarAnclaTopCenter(textoResultado.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetResultado), new Vector2(660f, 70f));
        textoResultado.alignment = TextAnchor.MiddleCenter;

        textoPuntaje = CrearTexto(canvas.transform, "Puntaje", Vector2.zero, 30);
        ConfigurarAnclaTopCenter(textoPuntaje.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetPuntaje), new Vector2(660f, 60f));
        textoPuntaje.alignment = TextAnchor.MiddleCenter;

        textoVariante = CrearTexto(canvas.transform, "Variante", Vector2.zero, 26);
        ConfigurarAnclaTopCenter(textoVariante.GetComponent<RectTransform>(), new Vector2(0f, HudTopOffsetVariante), new Vector2(460f, 44f));
        textoVariante.alignment = TextAnchor.MiddleCenter;

        GameObject filasGO = new GameObject("FilasJugadores");
        filasGO.transform.SetParent(canvas.transform);
        RectTransform filasRect = filasGO.AddComponent<RectTransform>();
        ConfigurarAnclaTopRight(filasRect, new Vector2(HudPadding, 130f), new Vector2(220f, 200f));
        filasJugadores = filasGO.transform;
        ReconstruirFilasJugadores();
    }

    void ConfigurarAnclaTopLeft(RectTransform rect, Vector2 offsetDesdeBorde, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(offsetDesdeBorde.x, -offsetDesdeBorde.y);
        rect.sizeDelta = size;
    }

    void ConfigurarAnclaTopRight(RectTransform rect, Vector2 offsetDesdeBorde, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-offsetDesdeBorde.x, -offsetDesdeBorde.y);
        rect.sizeDelta = size;
    }

    void ConfigurarAnclaTopCenter(RectTransform rect, Vector2 offsetDesdeTop, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(offsetDesdeTop.x, -offsetDesdeTop.y);
        rect.sizeDelta = size;
    }

    void ConfigurarAnclaCentro(RectTransform rect, Vector2 offsetDesdeCentro, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = offsetDesdeCentro;
        rect.sizeDelta = size;
    }

    Text CrearTexto(Transform parent, string nombre, Vector2 pos, int tamaño)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(parent);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = tamaño;
        text.color = Color.white;
        
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 40);
        
        // Outline para legibilidad
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
        
        return text;
    }

    Image CrearBarra(Transform parent, string nombre, Vector2 pos, Vector2 tamaño)
    {
        GameObject fondo = new GameObject(nombre + "_Fondo");
        fondo.transform.SetParent(parent);
        var imgFondo = fondo.AddComponent<Image>();
        imgFondo.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        var rt = fondo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamaño;
        
        GameObject relleno = new GameObject(nombre + "_Fill");
        relleno.transform.SetParent(fondo.transform);
        var imgRelleno = relleno.AddComponent<Image>();
        imgRelleno.color = Color.green;
        
        var rt2 = relleno.GetComponent<RectTransform>();
        rt2.anchorMin = Vector2.zero;
        rt2.anchorMax = Vector2.one;
        rt2.pivot = Vector2.zero;
        rt2.anchoredPosition = Vector2.zero;
        rt2.sizeDelta = Vector2.zero;
        
        return imgRelleno;
    }
}
