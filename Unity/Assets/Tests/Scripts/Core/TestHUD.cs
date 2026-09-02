/**
 * TestHUD.cs
 * HUD simple para el entorno de pruebas.
 * Muestra: Vida del Pilar, Vida del Jugador, Energía, Oleada, Munición.
 * 
 * Colocar en un GameObject en la escena (o se genera automáticamente).
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class TestHUD : MonoBehaviour
{
    [Header("Referencias")]
    public Pilar pilar;
    public PlayerController jugador;
    public EnergySystem energia;
    public WeaponSystem armas;
    public GameManager gameManager;
    
    [Header("UI Elements")]
    public Text textoVidaPilar;
    public Text textoVidaJugador;
    public Text textoEnergia;
    public Text textoOleada;
    public Text textoMunicion;
    public Text textoArma;
    public Text textoMensaje;
    
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
    private GameManager managerSuscrito;
    private bool managerEventosSuscritos;

    void Start()
    {
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        SeleccionarJugadorPrincipal();
        ConfigurarReferenciasJugador(false);
        
        // Crear UI si no existe
        if (textoVidaPilar == null)
            CrearUI();
        
        SuscribirEventosGameManager();
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
        if (jugador != null) return;

        jugador = jugadorRegistrado;
        ConfigurarReferenciasJugador(true);
    }

    void ManejarJugadorDesregistrado(PlayerController jugadorDesregistrado)
    {
        DesuscribirJugador(jugadorDesregistrado);
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
        MostrarMensaje($"Oleada {oleada}", 2f);
    }

    void ManejarJugadorDerribado(PlayerController jugador)
    {
        MostrarMensaje($"¡{jugador.name} DERRIBADO! - Reanima con [E]", 3f);
    }

    void ManejarJugadorReanimado(PlayerController jugador)
    {
        MostrarMensaje($"{jugador.name} reanimado!", 2f);
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
            float pct = pilar.PorcentajeVida / 100f;
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
        
        // Inputs de debug
        if (Keyboard.current != null)
        {
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                energia?.GastarEnCuracion();
            }
            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                energia?.ActivarHabilidad();
            }
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
                if (pilar != null)
                {
                    // R funciona siempre, incluso antes de WASD (fuerza inicio para que arena/torretas disparen)
                    if (gameManager != null && !gameManager.juegoActivo)
                    {
                        Debug.Log("[TestHUD] R: forzando inicio de juego para test de fases");
                        gameManager.IniciarJuego();
                    }
                    pilar.AplicarDañoPrueba(10f);
                    Debug.Log($"[TestHUD] R: pilar dañado 10% -> {pilar.vidaActual}% fase {pilar.faseActual}");
                }
                else
                {
                    Debug.LogWarning("[TestHUD] R: pilar aún null (¿generación no completada?)");
                }
            }
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

    public void MostrarMensaje(string mensaje, float duracion = 0)
    {
        if (textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            textoMensaje.color = Color.white;
            if (duracion > 0)
                Invoke(nameof(LimpiarMensaje), duracion);
        }
        Debug.Log($"[TestHUD] {mensaje}");
    }

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
        Debug.Log($"[TestHUD] ADVERTENCIA: {mensaje}");
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
        }

        DesuscribirTodosLosJugadores();
        managerEventosSuscritos = false;
    }

    void CrearUI()
    {
        // Buscar o crear canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Crear textos y barras
        textoVidaPilar = CrearTexto(canvas.transform, "VidaPilar", new Vector2(10, Screen.height - 30), 24);
        barraVidaPilar = CrearBarra(canvas.transform, "BarraPilar", new Vector2(10, Screen.height - 60), new Vector2(300, 20));
        
        textoVidaJugador = CrearTexto(canvas.transform, "VidaJugador", new Vector2(10, Screen.height - 100), 20);
        barraVidaJugador = CrearBarra(canvas.transform, "BarraJugador", new Vector2(10, Screen.height - 125), new Vector2(200, 15));
        
        textoEnergia = CrearTexto(canvas.transform, "Energia", new Vector2(10, Screen.height - 155), 20);
        barraEnergia = CrearBarra(canvas.transform, "BarraEnergia", new Vector2(10, Screen.height - 180), new Vector2(200, 15));
        
        textoOleada = CrearTexto(canvas.transform, "Oleada", new Vector2(Screen.width - 200, Screen.height - 30), 24);
        textoMunicion = CrearTexto(canvas.transform, "Municion", new Vector2(Screen.width - 200, Screen.height - 60), 20);
        textoArma = CrearTexto(canvas.transform, "Arma", new Vector2(Screen.width - 200, Screen.height - 85), 18);
        textoMensaje = CrearTexto(canvas.transform, "Mensaje", new Vector2(Screen.width / 2 - 200, Screen.height / 2), 36);
        textoMensaje.alignment = TextAnchor.MiddleCenter;
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
