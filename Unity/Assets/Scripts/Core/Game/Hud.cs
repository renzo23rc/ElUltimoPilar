/**
 * Hud.cs
 * HUD definitivo de Último Pilar.
 * Muestra: Vida del Pilar, estado de hasta 4 jugadores, Energía, Oleada,
 * Munición, variante temporal, menús de inicio/pausa y pantalla de resultado
 * con puntaje. Incluye crosshair central con flash de impacto.
 *
 * Colocar en un GameObject en la escena (o se genera automáticamente).
 */
using System.Collections.Generic;
using UltimoPilar.Core.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    private const float RevivePromptExtraRangeMeters = 0.5f;
    private const float DebugPilarDamage = 10f;

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
    private const float HudTopOffsetFilas = 130f;
    private const float HudTopOffsetMenu = 220f;
    private const float HudTopOffsetResultado = 300f;
    private const float HudTopOffsetPuntaje = 360f;
    private const float HudBarWidthPilar = 300f;
    private const float HudBarWidthJugador = 200f;
    private const float HudBarHeightPilar = 20f;
    private const float HudBarHeightJugador = 15f;
    private const float PlayerRowHeight = 28f;
    private static readonly Vector2 LeftLabelSize = new Vector2(440f, 44f);
    private static readonly Vector2 RightLabelSize = new Vector2(340f, 44f);
    private static readonly Vector2 CenterMessageSize = new Vector2(660f, 70f);
    private static readonly Vector2 CenterLineSize = new Vector2(660f, 60f);
    private static readonly Vector2 VariantLabelSize = new Vector2(460f, 44f);
    private static readonly Vector2 CrosshairSize = new Vector2(50f, 50f);
    private static readonly Vector2 PlayerRowsSize = new Vector2(260f, 200f);
    private static readonly Vector2 PlayerRowSize = new Vector2(260f, 26f);
    private static readonly Vector2 DefaultTextSize = new Vector2(400f, 40f);
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private const float MatchWidthOrHeight = 0.5f;
    private const int PilarFontSize = 28;
    private const int PlayerFontSize = 24;
    private const int WeaponFontSize = 22;
    private const int MessageFontSize = 40;
    private const int MenuFontSize = 34;
    private const int ResultFontSize = 50;
    private const int ScoreFontSize = 30;
    private const int VariantFontSize = 26;
    private const int PlayerRowFontSize = 19;
    private const int CrosshairFontSize = 34;
    private static readonly Color VariantColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color BarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private static readonly Vector2 OutlineDistance = new Vector2(1f, -1f);

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
    public Text textoVidaPilar;
    public Text textoVidaJugador;
    public Text textoEnergia;
    public Text textoOleada;
    public Text textoMunicion;
    public Text textoArma;
    public Text textoMensaje;

    [Header("Co-op y resultado")]
    public Text textoMenu;
    public Text textoResultado;
    public Text textoPuntaje;
    public Text textoVariante;
    public Text textoCrosshair;
    public Transform filasJugadores;

    [Header("Barras")]
    public Image barraVidaPilar;
    public Image barraVidaJugador;
    public Image barraEnergia;

    [Header("Colores por fase del Pilar")]
    public Color colorFase1 = Color.cyan;
    public Color colorFase2 = Color.yellow;
    public Color colorFase3 = new Color(1f, 0.5f, 0f);
    public Color colorFase4 = Color.red;

    private readonly HashSet<PlayerController> jugadoresSuscritos = new HashSet<PlayerController>();
    private readonly List<Text> textosFilaJugadores = new List<Text>();
    private GameManager managerSuscrito;
    private int conteoFilas;
    private float crosshairTimer;
    private bool promptReanimacionVisible;

    void Start()
    {
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        SeleccionarJugadorPrincipal();
        ConfigurarReferenciasJugador(false);

        if (textoVidaPilar == null)
            CrearUI();
        else
            AplicarLayout();

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

        if (managerSuscrito != null)
        {
            DesuscribirEventosGameManager();
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

        foreach (PlayerController jugadorRegistrado in gameManager.Players)
            SuscribirJugador(jugadorRegistrado);

        SeleccionarJugadorPrincipal();
        ConfigurarReferenciasJugador(false);
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
        foreach (PlayerController jugadorRegistrado in jugadoresSuscritos)
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

    void ManejarJugadorDerribado(PlayerController jugadorDerribado)
    {
        MostrarMensaje($"¡{jugadorDerribado.name} DERRIBADO! - Reanima con [E]", DownedMessageDurationSeconds);
    }

    void ManejarJugadorReanimado(PlayerController jugadorReanimado)
    {
        MostrarMensaje($"{jugadorReanimado.name} reanimado!", WaveMessageDurationSeconds);
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
        // Referencias generadas después de Start (TestSceneSetup compone en su propio Start).
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        SuscribirEventosGameManager();
        if (jugador == null)
        {
            SeleccionarJugadorPrincipal();
            ConfigurarReferenciasJugador(false);
        }

        ActualizarPilar();
        ActualizarJugador();
        ActualizarPromptReanimacion();
        ActualizarEnergia();
        ActualizarOleadaYArma();
        ActualizarOverlays();
        ActualizarFilasJugadores();
        ActualizarVariante();
        ActualizarCrosshair();
        AtajosMenu();
        ProcesarAtajoDebug();
    }

    void ActualizarPilar()
    {
        if (pilar == null) return;

        if (textoVidaPilar != null)
            textoVidaPilar.text = $"Pilar: {pilar.PorcentajeVida:F0}%";
        if (barraVidaPilar != null)
        {
            UiFill.Set(barraVidaPilar, pilar.PorcentajeVida / PercentageScale);
            barraVidaPilar.color = ObtenerColorFase(pilar.faseActual);
        }
    }

    void ActualizarJugador()
    {
        if (jugador == null) return;

        if (textoVidaJugador != null)
        {
            textoVidaJugador.text = jugador.estaDerribado
                ? $"Vida: DERRIBADO - [E] a {jugador.rangoReanimacion:F0}m para reanimar!"
                : $"Vida: {jugador.vidaActual:F0}/{jugador.vidaMaxima:F0}";
            textoVidaJugador.color = jugador.estaDerribado ? Color.red : Color.white;
        }

        if (barraVidaJugador != null)
        {
            UiFill.Set(barraVidaJugador, jugador.estaDerribado ? 0f : jugador.vidaActual / jugador.vidaMaxima);
            barraVidaJugador.color = jugador.estaDerribado ? Color.red : Color.green;
        }
    }

    void ActualizarPromptReanimacion()
    {
        if (textoMensaje == null) return;

        PlayerController aliado = BuscarAliadoParaReanimar();
        if (aliado != null && (promptReanimacionVisible || string.IsNullOrEmpty(textoMensaje.text)))
        {
            float distancia = Vector3.Distance(jugador.transform.position, aliado.transform.position);
            textoMensaje.text = $"Presiona [E] para reanimar a {aliado.name} ({distancia:F1}m)";
            textoMensaje.color = Color.yellow;
            promptReanimacionVisible = true;
            return;
        }

        // El prompt no tiene timer: se retira apenas deja de aplicar.
        if (aliado == null && promptReanimacionVisible)
        {
            textoMensaje.text = string.Empty;
            promptReanimacionVisible = false;
        }
    }

    PlayerController BuscarAliadoParaReanimar()
    {
        if (jugador == null || jugador.estaDerribado || gameManager == null) return null;

        return PlayerLocator.FindClosest(
            gameManager.Players,
            jugador.transform.position,
            EsAliadoDerribado,
            jugador.rangoReanimacion + RevivePromptExtraRangeMeters);
    }

    bool EsAliadoDerribado(PlayerController candidato)
    {
        return candidato != jugador && candidato.estaDerribado;
    }

    void ActualizarEnergia()
    {
        if (energia == null) return;

        if (textoEnergia != null)
            textoEnergia.text = $"Energía: {energia.energiaActual:F0}/{energia.energiaMaxima:F0}";
        if (barraEnergia != null)
            UiFill.Set(barraEnergia, energia.energiaActual / energia.energiaMaxima);
    }

    void ActualizarOleadaYArma()
    {
        if (gameManager != null && textoOleada != null)
            textoOleada.text = $"Oleada: {gameManager.oleadaActual}/{gameManager.totalOleadas}";

        WeaponSystem.Arma arma = armas != null ? armas.ObtenerArmaActual() : null;
        if (arma == null) return;

        if (textoMunicion != null)
            textoMunicion.text = arma.municionMaxima < 0 ? "Munición: ∞" : $"Munición: {arma.municionActual}/{arma.municionMaxima}";
        if (textoArma != null)
            textoArma.text = $"Arma: {arma.nombre}";
    }

    void ReconstruirFilasJugadores()
    {
        textosFilaJugadores.Clear();
        conteoFilas = 0;
        if (filasJugadores == null || gameManager == null) return;

        foreach (Transform hijo in filasJugadores)
            Destroy(hijo.gameObject);

        foreach (PlayerController p in gameManager.Players)
        {
            if (p == null) continue;
            Text fila = CrearTexto(filasJugadores, $"Fila{conteoFilas}");
            ConfigurarFila(fila, conteoFilas);
            textosFilaJugadores.Add(fila);
            conteoFilas++;
        }
    }

    void ActualizarFilasJugadores()
    {
        if (gameManager == null) return;
        if (gameManager.PlayerCount != conteoFilas)
            ReconstruirFilasJugadores();

        int indice = 0;
        foreach (PlayerController p in gameManager.Players)
        {
            if (p == null) continue;
            if (indice >= textosFilaJugadores.Count) break;

            Text texto = textosFilaJugadores[indice];
            if (texto != null)
            {
                texto.text = p.estaDerribado
                    ? $"P{indice + 1} {p.name}: DERRIBADO"
                    : $"P{indice + 1} {p.name}: {p.vidaActual:F0}/{p.vidaMaxima:F0}";
                texto.color = p.estaDerribado ? Color.red : Color.white;
            }

            indice++;
        }
    }

    void ActualizarOverlays()
    {
        if (gameManager == null) return;
        MatchState estado = gameManager.EstadoActual;
        bool terminal = estado == MatchState.Victory || estado == MatchState.Defeat;
        if (textoMenu != null)
        {
            if (estado == MatchState.WaitingToStart)
                textoMenu.text = "ÚLTIMO PILAR\nMovete o dispará para iniciar\nWASD + Mouse (P1) · Start en gamepad (P2-P4)";
            else if (estado == MatchState.Paused)
                textoMenu.text = "PAUSA\nEsc: continuar · Enter: reiniciar";
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

    void ActualizarVariante()
    {
        if (textoVariante == null) return;

        string displayName = armas == null
            ? string.Empty
            : GetVariantDisplayName(armas.VarianteActiva, armas.ActiveVariantDisplayName);
        textoVariante.text = armas == null
            ? string.Empty
            : FormatVariantLabel(displayName, armas.VariantMultipliesDamage, armas.multiplicadorVariante, armas.tiempoVarianteRestante);
        textoVariante.color = VariantColor;
    }

    void ActualizarCrosshair()
    {
        if (textoCrosshair == null || crosshairTimer <= 0f) return;

        crosshairTimer -= Time.unscaledDeltaTime;
        if (crosshairTimer <= 0f)
            textoCrosshair.color = Color.white;
    }

    void AtajosMenu()
    {
        if (gameManager == null || Keyboard.current == null) return;
        MatchState estado = gameManager.EstadoActual;
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
        promptReanimacionVisible = false;
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
        CombatFeedback.OnCombatHit -= FlashCrosshair;
        DesuscribirTodosLosJugadores();
    }

    // ===== CONSTRUCCIÓN Y LAYOUT =====

    void CrearUI()
    {
        Canvas canvas = BuscarCanvasPantalla();
        if (canvas == null)
        {
            var canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform raiz = canvas.transform;
        textoVidaPilar = CrearTexto(raiz, "VidaPilar");
        barraVidaPilar = CrearBarra(raiz, "BarraPilar");
        textoVidaJugador = CrearTexto(raiz, "VidaJugador");
        barraVidaJugador = CrearBarra(raiz, "BarraJugador");
        textoEnergia = CrearTexto(raiz, "Energia");
        barraEnergia = CrearBarra(raiz, "BarraEnergia");
        textoOleada = CrearTexto(raiz, "Oleada");
        textoMunicion = CrearTexto(raiz, "Municion");
        textoArma = CrearTexto(raiz, "Arma");
        textoMensaje = CrearTexto(raiz, "Mensaje");
        textoCrosshair = CrearTexto(raiz, "Crosshair");
        textoCrosshair.text = "+";
        textoMenu = CrearTexto(raiz, "Menu");
        textoResultado = CrearTexto(raiz, "Resultado");
        textoPuntaje = CrearTexto(raiz, "Puntaje");
        textoVariante = CrearTexto(raiz, "Variante");

        var filasObject = new GameObject("FilasJugadores");
        filasObject.transform.SetParent(raiz, false);
        filasObject.AddComponent<RectTransform>();
        filasJugadores = filasObject.transform;

        AplicarLayout();
        ReconstruirFilasJugadores();
    }

    // Único lugar que define anclas, tamaños y fuentes, tanto para el HUD generado como para uno existente.
    void AplicarLayout()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null) canvas = BuscarCanvasPantalla();
        if (canvas != null && canvas.TryGetComponent(out CanvasScaler scaler))
            ConfigurarCanvasScaler(scaler);

        ConfigurarTexto(textoVidaPilar, PilarFontSize, TextAnchor.UpperLeft, rect => AnclarTopLeft(rect, new Vector2(HudPadding, HudTopOffsetVidaPilar), LeftLabelSize));
        ConfigurarBarra(barraVidaPilar, new Vector2(HudPadding, HudTopOffsetBarraPilar), new Vector2(HudBarWidthPilar, HudBarHeightPilar));
        ConfigurarTexto(textoVidaJugador, PlayerFontSize, TextAnchor.UpperLeft, rect => AnclarTopLeft(rect, new Vector2(HudPadding, HudTopOffsetVidaJugador), LeftLabelSize));
        ConfigurarBarra(barraVidaJugador, new Vector2(HudPadding, HudTopOffsetBarraJugador), new Vector2(HudBarWidthJugador, HudBarHeightJugador));
        ConfigurarTexto(textoEnergia, PlayerFontSize, TextAnchor.UpperLeft, rect => AnclarTopLeft(rect, new Vector2(HudPadding, HudTopOffsetEnergia), LeftLabelSize));
        ConfigurarBarra(barraEnergia, new Vector2(HudPadding, HudTopOffsetBarraEnergia), new Vector2(HudBarWidthJugador, HudBarHeightJugador));
        ConfigurarTexto(textoOleada, PilarFontSize, TextAnchor.UpperRight, rect => AnclarTopRight(rect, new Vector2(HudPadding, HudTopOffsetOleada), RightLabelSize));
        ConfigurarTexto(textoMunicion, PlayerFontSize, TextAnchor.UpperRight, rect => AnclarTopRight(rect, new Vector2(HudPadding, HudTopOffsetMunicion), RightLabelSize));
        ConfigurarTexto(textoArma, WeaponFontSize, TextAnchor.UpperRight, rect => AnclarTopRight(rect, new Vector2(HudPadding, HudTopOffsetArma), RightLabelSize));
        ConfigurarTexto(textoMensaje, MessageFontSize, TextAnchor.MiddleCenter, rect => AnclarCentro(rect, CenterMessageSize));
        ConfigurarTexto(textoCrosshair, CrosshairFontSize, TextAnchor.MiddleCenter, rect => AnclarCentro(rect, CrosshairSize));
        ConfigurarTexto(textoMenu, MenuFontSize, TextAnchor.MiddleCenter, rect => AnclarTopCenter(rect, HudTopOffsetMenu, CenterLineSize));
        ConfigurarTexto(textoResultado, ResultFontSize, TextAnchor.MiddleCenter, rect => AnclarTopCenter(rect, HudTopOffsetResultado, CenterMessageSize));
        ConfigurarTexto(textoPuntaje, ScoreFontSize, TextAnchor.MiddleCenter, rect => AnclarTopCenter(rect, HudTopOffsetPuntaje, CenterLineSize));
        ConfigurarTexto(textoVariante, VariantFontSize, TextAnchor.MiddleCenter, rect => AnclarTopCenter(rect, HudTopOffsetVariante, VariantLabelSize));

        if (filasJugadores != null && filasJugadores.TryGetComponent(out RectTransform filasRect))
            AnclarTopRight(filasRect, new Vector2(HudPadding, HudTopOffsetFilas), PlayerRowsSize);

        for (int i = 0; i < textosFilaJugadores.Count; i++)
            ConfigurarFila(textosFilaJugadores[i], i);
    }

    // Las barras de vida de los enemigos también son Canvas: se busca uno de pantalla.
    static Canvas BuscarCanvasPantalla()
    {
        foreach (Canvas candidato in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (candidato.renderMode != RenderMode.WorldSpace)
                return candidato;
        }

        return null;
    }

    static void ConfigurarCanvasScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = MatchWidthOrHeight;
    }

    static void ConfigurarTexto(Text texto, int tamaño, TextAnchor alineacion, System.Action<RectTransform> anclar)
    {
        if (texto == null) return;
        texto.fontSize = tamaño;
        texto.alignment = alineacion;
        anclar(texto.rectTransform);
    }

    static void ConfigurarBarra(Image relleno, Vector2 offset, Vector2 tamaño)
    {
        if (relleno == null || relleno.transform.parent == null) return;
        if (relleno.transform.parent.TryGetComponent(out RectTransform fondo))
            AnclarTopLeft(fondo, offset, tamaño);
    }

    static void ConfigurarFila(Text fila, int indice)
    {
        ConfigurarTexto(fila, PlayerRowFontSize, TextAnchor.UpperRight, rect => AnclarTopRight(rect, new Vector2(0f, indice * PlayerRowHeight), PlayerRowSize));
    }

    static void AnclarTopLeft(RectTransform rect, Vector2 offsetDesdeBorde, Vector2 size)
    {
        Anclar(rect, new Vector2(0f, 1f), new Vector2(offsetDesdeBorde.x, -offsetDesdeBorde.y), size);
    }

    static void AnclarTopRight(RectTransform rect, Vector2 offsetDesdeBorde, Vector2 size)
    {
        Anclar(rect, new Vector2(1f, 1f), new Vector2(-offsetDesdeBorde.x, -offsetDesdeBorde.y), size);
    }

    static void AnclarTopCenter(RectTransform rect, float offsetDesdeTop, Vector2 size)
    {
        Anclar(rect, new Vector2(0.5f, 1f), new Vector2(0f, -offsetDesdeTop), size);
    }

    static void AnclarCentro(RectTransform rect, Vector2 size)
    {
        Anclar(rect, new Vector2(0.5f, 0.5f), Vector2.zero, size);
    }

    static void Anclar(RectTransform rect, Vector2 ancla, Vector2 posicion, Vector2 size)
    {
        rect.anchorMin = ancla;
        rect.anchorMax = ancla;
        rect.pivot = ancla;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = size;
    }

    static Text CrearTexto(Transform parent, string nombre)
    {
        var textoObject = new GameObject(nombre);
        textoObject.transform.SetParent(parent, false);
        Text text = textoObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.rectTransform.sizeDelta = DefaultTextSize;

        // Outline para legibilidad
        Outline outline = textoObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = OutlineDistance;
        return text;
    }

    static Image CrearBarra(Transform parent, string nombre)
    {
        var fondo = new GameObject(nombre + "_Fondo");
        fondo.transform.SetParent(parent, false);
        fondo.AddComponent<Image>().color = BarBackgroundColor;

        var relleno = new GameObject(nombre + "_Fill");
        relleno.transform.SetParent(fondo.transform, false);
        Image imgRelleno = relleno.AddComponent<Image>();
        imgRelleno.color = Color.green;

        RectTransform rect = imgRelleno.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return imgRelleno;
    }
}
