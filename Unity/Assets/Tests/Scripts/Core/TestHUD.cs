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

    void Start()
    {
        if (pilar == null) pilar = FindFirstObjectByType<Pilar>();
        if (jugador == null) jugador = FindFirstObjectByType<PlayerController>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (jugador != null && energia == null) energia = jugador.GetComponent<EnergySystem>();
        if (jugador != null && armas == null) armas = jugador.GetComponent<WeaponSystem>();
        
        // Crear UI si no existe
        if (textoVidaPilar == null)
            CrearUI();
        
        // Suscribirse a eventos
        if (gameManager != null)
        {
            gameManager.OnVictoria += () => MostrarMensaje("¡VICTORIA!");
            gameManager.OnDerrota += () => MostrarMensaje("DERROTA - El Pilar cayó");
            gameManager.OnOleadaIniciada += (o) => MostrarMensaje($"Oleada {o}", 2f);
        }
    }

    void Update()
    {
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
        
        // Vida del Jugador
        if (jugador != null)
        {
            float pctJ = jugador.vidaActual / jugador.vidaMaxima;
            if (textoVidaJugador != null)
                textoVidaJugador.text = $"Vida: {jugador.vidaActual:F0}/{jugador.vidaMaxima:F0}";
            if (barraVidaJugador != null)
                barraVidaJugador.fillAmount = pctJ;
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
                pilar?.RecibirDaño(10f);
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

    void MostrarMensaje(string mensaje, float duracion = 0)
    {
        if (textoMensaje != null)
        {
            textoMensaje.text = mensaje;
            if (duracion > 0)
                Invoke(nameof(LimpiarMensaje), duracion);
        }
        Debug.Log($"[TestHUD] {mensaje}");
    }

    void LimpiarMensaje()
    {
        if (textoMensaje != null)
            textoMensaje.text = "";
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
