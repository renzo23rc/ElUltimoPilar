/**
 * Hud.cs
 * HUD de partida de Último Pilar.
 * Pantalla completa (compartido): vida del Pilar arriba al centro, oleada, mensajes,
 * menús de inicio/pausa y pantalla de resultado con puntaje.
 * Cada jugador registrado recibe además su propio PlayerHud dentro de su viewport
 * (identidad y rol, vida, energía, munición, arma, variante, crosshair y reanimación).
 *
 * Colocar en un GameObject en la escena (o se genera automáticamente).
 */
using System.Collections.Generic;
using UltimoPilar.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Displays the shared match information and owns one <see cref="PlayerHud"/> per registered player.
/// </summary>
public class Hud : MonoBehaviour
{
    private const float PercentageScale = 100f;
    private const float VictoryMessageDurationSeconds = 5f;
    private const float WaveMessageDurationSeconds = 2f;
    private const float DownedMessageDurationSeconds = 3f;
    private const float DebugPilarDamage = 10f;
    private const int OverlaySortingOrder = 20;

    private const float PilarLabelOffset = 10f;
    private const float PilarBarOffset = 46f;
    private const float WaveLabelOffset = 72f;
    private const float MessageOffset = 130f;
    private const float MenuOffset = 220f;
    private const float ResultOffset = 300f;
    private const float ScoreOffset = 360f;
    private const int PilarFontSize = 26;
    private const int WaveFontSize = 24;
    private const int MessageFontSize = 38;
    private const int MenuFontSize = 34;
    private const int ResultFontSize = 50;
    private const int ScoreFontSize = 30;
    private static readonly Vector2 PilarLabelSize = new Vector2(420f, 36f);
    private static readonly Vector2 PilarBarSize = new Vector2(420f, 22f);
    private static readonly Vector2 CenterLineSize = new Vector2(900f, 60f);
    private static readonly Vector2 CenterMessageSize = new Vector2(900f, 70f);

    [Header("Referencias")]
    /// <summary>Gets or sets the Pilar reference.</summary>
    public Pilar pilar;
    /// <summary>Gets or sets the match manager reference.</summary>
    public GameManager gameManager;

    [Header("UI compartida")]
    public Text textoVidaPilar;
    public Image barraVidaPilar;
    public Text textoOleada;
    public Text textoMensaje;
    public Text textoMenu;
    public Text textoResultado;
    public Text textoPuntaje;

    [Header("Colores por fase del Pilar")]
    public Color colorFase1 = Color.cyan;
    public Color colorFase2 = Color.yellow;
    public Color colorFase3 = new Color(1f, 0.5f, 0f);
    public Color colorFase4 = Color.red;

    private readonly Dictionary<PlayerController, PlayerHud> playerHuds = new Dictionary<PlayerController, PlayerHud>();
    private readonly HashSet<PlayerController> jugadoresSuscritos = new HashSet<PlayerController>();
    private readonly List<PlayerController> jugadoresARetirar = new List<PlayerController>();
    private GameManager managerSuscrito;

    void Start()
    {
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();

        if (textoVidaPilar == null)
            CrearUI();

        SuscribirEventosGameManager();
    }

    void Update()
    {
        // Referencias generadas después de Start (TestSceneSetup compone en su propio Start).
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        SuscribirEventosGameManager();
        SincronizarHudsDeJugadores();

        ActualizarPilar();
        ActualizarOleada();
        ActualizarOverlays();
        AtajosMenu();
        ProcesarAtajoDebug();
    }

    // ===== EVENTOS =====

    void SuscribirEventosGameManager()
    {
        if (gameManager == null || managerSuscrito == gameManager) return;

        DesuscribirEventosGameManager();
        managerSuscrito = gameManager;
        managerSuscrito.OnVictoria += ManejarVictoria;
        managerSuscrito.OnDerrota += ManejarDerrota;
        managerSuscrito.OnOleadaIniciada += ManejarOleadaIniciada;
        managerSuscrito.OnPlayerRegistered += ManejarJugadorRegistrado;
        managerSuscrito.OnPlayerUnregistered += ManejarJugadorDesregistrado;
        managerSuscrito.OnMatchResult += ManejarResultado;

        foreach (PlayerController jugador in gameManager.Players)
            ManejarJugadorRegistrado(jugador);
    }

    void DesuscribirEventosGameManager()
    {
        if (managerSuscrito == null) return;

        managerSuscrito.OnVictoria -= ManejarVictoria;
        managerSuscrito.OnDerrota -= ManejarDerrota;
        managerSuscrito.OnOleadaIniciada -= ManejarOleadaIniciada;
        managerSuscrito.OnPlayerRegistered -= ManejarJugadorRegistrado;
        managerSuscrito.OnPlayerUnregistered -= ManejarJugadorDesregistrado;
        managerSuscrito.OnMatchResult -= ManejarResultado;
        managerSuscrito = null;
    }

    void ManejarJugadorRegistrado(PlayerController jugador)
    {
        if (jugador == null) return;

        if (jugadoresSuscritos.Add(jugador))
        {
            jugador.OnDerribado += ManejarJugadorDerribado;
            jugador.OnReanimado += ManejarJugadorReanimado;
        }

        if (!playerHuds.ContainsKey(jugador))
            playerHuds[jugador] = PlayerHud.Create(jugador, transform);
    }

    void ManejarJugadorDesregistrado(PlayerController jugador)
    {
        RetirarJugador(jugador);
    }

    void RetirarJugador(PlayerController jugador)
    {
        if (jugador is object && jugadoresSuscritos.Remove(jugador) && jugador != null)
        {
            jugador.OnDerribado -= ManejarJugadorDerribado;
            jugador.OnReanimado -= ManejarJugadorReanimado;
        }

        if (jugador is object && playerHuds.TryGetValue(jugador, out PlayerHud hud))
        {
            if (hud != null) Destroy(hud.gameObject);
            playerHuds.Remove(jugador);
        }
    }

    // Red de seguridad: jugadores destruidos sin pasar por el desregistro no dejan HUDs huérfanos.
    void SincronizarHudsDeJugadores()
    {
        jugadoresARetirar.Clear();
        foreach (PlayerController jugador in playerHuds.Keys)
        {
            if (jugador == null) jugadoresARetirar.Add(jugador);
        }

        foreach (PlayerController jugador in jugadoresARetirar)
            RetirarJugador(jugador);
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
        MostrarMensaje($"¡{NombreDe(jugador)} DERRIBADO!", DownedMessageDurationSeconds);
    }

    void ManejarJugadorReanimado(PlayerController jugador)
    {
        MostrarMensaje($"{NombreDe(jugador)} reanimado", WaveMessageDurationSeconds);
    }

    void ManejarResultado(MatchResult resultado)
    {
        if (resultado == null) return;
        string texto = resultado.Outcome == MatchState.Victory
            ? $"¡VICTORIA! Puntaje: {resultado.Score}"
            : $"DERROTA — Puntaje: {resultado.Score}";
        MostrarMensaje(texto, VictoryMessageDurationSeconds);
    }

    static string NombreDe(PlayerController jugador)
    {
        return jugador.HasRole ? DefenderRoleCatalog.GetDisplayName(jugador.Role) : jugador.name;
    }

    // ===== ACTUALIZACIÓN =====

    void ActualizarPilar()
    {
        if (pilar == null) return;

        if (textoVidaPilar != null)
            textoVidaPilar.text = $"Pilar {pilar.PorcentajeVida:F0}%";
        if (barraVidaPilar != null)
        {
            UiFill.Set(barraVidaPilar, pilar.PorcentajeVida / PercentageScale);
            barraVidaPilar.color = ObtenerColorFase(pilar.faseActual);
        }
    }

    void ActualizarOleada()
    {
        if (gameManager != null && textoOleada != null)
            textoOleada.text = $"Oleada {gameManager.oleadaActual}/{gameManager.totalOleadas}";
    }

    void ActualizarOverlays()
    {
        if (gameManager == null) return;
        MatchState estado = gameManager.EstadoActual;
        bool terminal = estado == MatchState.Victory || estado == MatchState.Defeat;
        if (textoMenu != null)
        {
            if (estado == MatchState.WaitingToStart)
                textoMenu.text = "ÚLTIMO PILAR\nMovete o dispará para iniciar\nWASD + Mouse (P1) · Options o X en gamepad para unirse";
            else if (estado == MatchState.Paused)
                textoMenu.text = "PAUSA\nEsc / Options: continuar · Enter: reiniciar";
            else
                textoMenu.text = string.Empty;
        }

        if (textoResultado == null || textoPuntaje == null) return;

        if (terminal)
        {
            MatchResult resultado = gameManager.CurrentResult;
            int puntaje = resultado != null ? resultado.Score : 0;
            textoResultado.text = estado == MatchState.Victory ? "¡VICTORIA!" : "DERROTA";
            textoResultado.color = estado == MatchState.Victory ? Color.green : Color.red;
            textoPuntaje.text = $"Puntaje: {puntaje}\nEnter: reiniciar";
        }
        else
        {
            textoResultado.text = string.Empty;
            textoPuntaje.text = string.Empty;
        }
    }

    void AtajosMenu()
    {
        if (gameManager == null || Keyboard.current == null) return;

        // La pausa (Esc / Options) llega por la acción Pause del Input System y la resuelve GameManager.
        MatchState estado = gameManager.EstadoActual;
        if (Keyboard.current.enterKey.wasPressedThisFrame
            && (estado == MatchState.Victory || estado == MatchState.Defeat || estado == MatchState.Paused))
        {
            gameManager.ReiniciarJuego();
        }
    }

    // R daña el Pilar para probar las fases. Solo existe en el Editor y en Development builds.
    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    void ProcesarAtajoDebug()
    {
        if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame) return;
        if (pilar == null)
        {
            Debug.LogWarning("[Hud] R: Pilar no encontrado (¿generación no completada?)");
            return;
        }

        // Fuerza el inicio para que arena y torretas reaccionen al daño de prueba.
        if (gameManager != null && !gameManager.juegoActivo)
            gameManager.IniciarJuego();
        pilar.RecibirDaño(DebugPilarDamage);
        Debug.Log($"[Hud] R: Pilar dañado -> {pilar.PorcentajeVida:F0}% fase {pilar.faseActual}");
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

    // ===== API PÚBLICA =====

    public static string GetVariantDisplayName(bool variantIsActive, string semanticDisplayName)
    {
        if (!variantIsActive || string.IsNullOrEmpty(semanticDisplayName))
            return string.Empty;

        return semanticDisplayName;
    }

    /// <summary>Formats the variant label; only damage variants show their multiplier.</summary>
    /// <param name="displayName">The variant display name.</param>
    /// <param name="multipliesDamage">Whether the variant multiplies damage.</param>
    /// <param name="multiplier">The damage multiplier.</param>
    /// <param name="remainingSeconds">The remaining duration in seconds.</param>
    /// <returns>The label, or an empty string without a display name.</returns>
    public static string FormatVariantLabel(string displayName, bool multipliesDamage, float multiplier, float remainingSeconds)
    {
        if (string.IsNullOrEmpty(displayName))
            return string.Empty;

        string prefix = multipliesDamage ? $"x{multiplier:F0} " : string.Empty;
        return $"¡{prefix}{displayName}! {remainingSeconds:F0}s";
    }

    /// <summary>Flashes every player's crosshair for a hit or kill.</summary>
    /// <param name="mato">Whether the hit killed its target.</param>
    public void FlashCrosshair(bool mato)
    {
        foreach (PlayerHud hud in playerHuds.Values)
        {
            if (hud != null) hud.FlashCrosshair(mato);
        }
    }

    /// <summary>Displays a message for an optional duration.</summary>
    /// <param name="mensaje">The message to display.</param>
    /// <param name="duracion">The display duration in seconds; zero keeps it until replaced.</param>
    public void MostrarMensaje(string mensaje, float duracion = 0)
    {
        MostrarTextoCentral(mensaje, Color.white, duracion);
        Debug.Log($"[Hud] {mensaje}");
    }

    /// <summary>Displays a colored warning for an optional duration.</summary>
    /// <param name="mensaje">The warning to display.</param>
    /// <param name="color">The warning color.</param>
    /// <param name="duracion">The display duration in seconds.</param>
    public void MostrarAdvertencia(string mensaje, Color color, float duracion)
    {
        MostrarTextoCentral(mensaje, color, duracion);
        Debug.Log($"[Hud] ADVERTENCIA: {mensaje}");
    }

    void MostrarTextoCentral(string mensaje, Color color, float duracion)
    {
        if (textoMensaje == null) return;

        // Un mensaje nuevo cancela el borrado pendiente del anterior.
        CancelInvoke(nameof(LimpiarMensaje));
        textoMensaje.text = mensaje;
        textoMensaje.color = color;
        if (duracion > 0)
            Invoke(nameof(LimpiarMensaje), duracion);
    }

    void LimpiarMensaje()
    {
        if (textoMensaje != null)
            textoMensaje.text = string.Empty;
    }

    void OnDestroy()
    {
        DesuscribirEventosGameManager();
        foreach (PlayerController jugador in new List<PlayerController>(playerHuds.Keys))
            RetirarJugador(jugador);
    }

    // ===== CONSTRUCCIÓN =====

    // HUD compartido en pantalla completa, por encima de las pantallas divididas.
    void CrearUI()
    {
        Canvas canvas = BuscarCanvasPantalla();
        if (canvas == null)
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        canvas.sortingOrder = OverlaySortingOrder;
        if (canvas.TryGetComponent(out CanvasScaler scaler))
            HudUiFactory.ConfigureScaler(scaler);

        Transform raiz = canvas.transform;
        textoVidaPilar = HudUiFactory.CreateText(raiz, "VidaPilar", PilarFontSize, TextAnchor.MiddleCenter);
        HudUiFactory.AnchorTopCenter(textoVidaPilar.rectTransform, PilarLabelOffset, PilarLabelSize);
        barraVidaPilar = HudUiFactory.CreateBar(raiz, "BarraPilar");
        HudUiFactory.AnchorBar(barraVidaPilar, rect => HudUiFactory.AnchorTopCenter(rect, PilarBarOffset, PilarBarSize));
        textoOleada = HudUiFactory.CreateText(raiz, "Oleada", WaveFontSize, TextAnchor.MiddleCenter);
        HudUiFactory.AnchorTopCenter(textoOleada.rectTransform, WaveLabelOffset, PilarLabelSize);
        textoMensaje = HudUiFactory.CreateText(raiz, "Mensaje", MessageFontSize, TextAnchor.MiddleCenter);
        HudUiFactory.AnchorTopCenter(textoMensaje.rectTransform, MessageOffset, CenterMessageSize);
        textoMenu = HudUiFactory.CreateText(raiz, "Menu", MenuFontSize, TextAnchor.MiddleCenter);
        HudUiFactory.AnchorTopCenter(textoMenu.rectTransform, MenuOffset, CenterLineSize);
        textoResultado = HudUiFactory.CreateText(raiz, "Resultado", ResultFontSize, TextAnchor.MiddleCenter);
        HudUiFactory.AnchorTopCenter(textoResultado.rectTransform, ResultOffset, CenterMessageSize);
        textoPuntaje = HudUiFactory.CreateText(raiz, "Puntaje", ScoreFontSize, TextAnchor.MiddleCenter);
        HudUiFactory.AnchorTopCenter(textoPuntaje.rectTransform, ScoreOffset, CenterLineSize);
    }

    // Solo sirve un canvas Overlay: las barras de enemigos son WorldSpace y los PlayerHud usan la cámara.
    static Canvas BuscarCanvasPantalla()
    {
        foreach (Canvas candidato in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (candidato.renderMode == RenderMode.ScreenSpaceOverlay)
                return candidato;
        }

        return null;
    }
}
