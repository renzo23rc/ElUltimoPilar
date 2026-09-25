/**
 * EnergySystem.cs
 * Gestiona la energía dropeada por enemigos.
 * Permite gastar energía en curación o habilidades.
 * Según el GDD: 20 puntos = 1% de vida del jugador.
 * 
 * Colocar en el mismo GameObject que PlayerController.
 */
using System;
using System.Collections.Generic;
using UltimoPilar.Core.Shared;
using UnityEngine;

public class EnergySystem : MonoBehaviour
{
    private const float AreaRadiusMultiplier = 1.5f;
    private const float WaveExpansionDurationSeconds = 0.5f;
    private const float WaveAlpha = 0.3f;

    [Header("Configuración")]
    public float energiaMaxima = 100f;
    public float energiaActual = 0f;
    public float costoCuracion = 15f; // Balanceo: 20->15 para que curar no sea castigo extremo (decisión real)
    public float vidaPorCuracion = 8f; // 1->8% vida: ahora curar es relevante tácticamente (2 curas = 16% vida)
    public float costoHabilidad = 28f; // 30->28 un poco más accesible para habilidad de control

    [Header("Habilidades")]
    public bool habilidadPulsoDaño = true; // true = pulso de daño, false = ralentización
    public float radioPulso = 8f;
    public float dañoPulso = 25f;
    public float duracionRalentizacion = 5f;
    public float factorRalentizacion = 0.5f;

    // Eventos
    public event Action<float> OnEnergiaCambiada;
    public event Action OnHabilidadActivada;
    public event Action OnCuracionUsada;

    private PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Start()
    {
        if (player == null) player = GetComponent<PlayerController>();
        // GameManager es dueño del reinicio; sin manager se conserva la inicialización autónoma.
        if (GameManager.Instance == null)
            energiaActual = 0f;
    }

    public void ResetState()
    {
        energiaActual = 0f;
        OnEnergiaCambiada?.Invoke(energiaActual);
    }

    public void RecolectarEnergia(float cantidad)
    {
        energiaActual = Mathf.Min(energiaMaxima, energiaActual + cantidad);
        OnEnergiaCambiada?.Invoke(energiaActual);
    }

    public bool GastarEnCuracion()
    {
        if (energiaActual < costoCuracion || player == null)
            return false;

        energiaActual -= costoCuracion;
        player.Curar(vidaPorCuracion);
        OnEnergiaCambiada?.Invoke(energiaActual);
        OnCuracionUsada?.Invoke();
        return true;
    }

    public bool ActivarHabilidad()
    {
        if (energiaActual < costoHabilidad)
            return false;

        energiaActual -= costoHabilidad;
        OnEnergiaCambiada?.Invoke(energiaActual);

        if (habilidadPulsoDaño)
            PulsoDeDaño();
        else
            RalentizacionArea();

        OnHabilidadActivada?.Invoke();
        return true;
    }

    void PulsoDeDaño()
    {
        foreach (Enemy enemy in OverlapQuery.FindUniqueInSphere<Enemy>(transform.position, radioPulso))
            enemy.RecibirDaño(dañoPulso);

        CrearOndaVisual(Color.yellow, radioPulso);
    }

    void RalentizacionArea()
    {
        // Ralentización temporal sin stack: afecta a enemigos y aliados en el área, no al lanzador.
        float radio = radioPulso * AreaRadiusMultiplier;
        foreach (Enemy enemy in OverlapQuery.FindUniqueInSphere<Enemy>(transform.position, radio))
            enemy.AplicarRalentizacion(this, factorRalentizacion, duracionRalentizacion);

        foreach (PlayerController aliado in AliadosEnRadio(radio))
            aliado.AplicarRalentizacion(this, factorRalentizacion, duracionRalentizacion);

        CrearOndaVisual(Color.cyan, radio);
    }

    List<PlayerController> AliadosEnRadio(float radio)
    {
        var aliados = new List<PlayerController>();
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return aliados;

        foreach (PlayerController candidato in manager.Players)
        {
            if (candidato == null || candidato == player)
                continue;
            if (Vector3.Distance(candidato.transform.position, transform.position) <= radio)
                aliados.Add(candidato);
        }

        return aliados;
    }

    void CrearOndaVisual(Color color, float radio)
    {
        GameObject onda = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        onda.name = "OndaHabilidad";
        Destroy(onda.GetComponent<Collider>());
        onda.transform.position = transform.position;
        OwnedMaterialCleanup.Assign(
            onda.GetComponent<Renderer>(),
            RuntimeMaterialFactory.CreateLit(new Color(color.r, color.g, color.b, WaveAlpha)));
        onda.AddComponent<OndaExpansion>().Iniciar(radio, WaveExpansionDurationSeconds);
    }
}
