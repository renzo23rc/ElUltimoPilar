/**
 * Pilar.cs
 * Gestiona la vida, estados visuales y transformaciones del Pilar.
 * Incluye detección de daño, umbrales de fase y eventos.
 * 
 * Colocar en el GameObject que representa al Pilar en el centro de la arena.
 */
using UnityEngine;
using System;

public class Pilar : MonoBehaviour
{
    [Header("Vida")]
    [Range(0, 100)]
    public float vidaMaxima = 100f;
    [Range(0, 100)]
    public float vidaActual = 100f;
    
    [Header("Umbrales de Transformación")]
    public float umbralFase2 = 75f; // Pozo central
    public float umbralFase3 = 50f; // Zona gravedad
    public float umbralFase4 = 25f; // Protocolo emergencia
    
    [Header("Estado Visual (Debug)")]
    public int faseActual = 1;
    public Color colorFase1 = Color.cyan;
    public Color colorFase2 = Color.yellow;
    public Color colorFase3 = new Color(1f, 0.5f, 0f); // Naranja
    public Color colorFase4 = Color.red;
    
    [Header("Torretas (Fase 4)")]
    public bool torretasActivas = false;
    public Transform[] puntosTorretas;
    public GameObject prefabTorreta;
    
    // Eventos
    public event Action<float> OnVidaCambiada;
    public event Action<int> OnFaseCambiada;
    public event Action<float> OnDañoRecibido;
    
    private Renderer rend;
    private int faseAnterior = 1;

    void Start()
    {
        rend = GetComponent<Renderer>();
        RestaurarVida();
    }

    void Update()
    {
        // Actualizar fase según vida
        int nuevaFase = CalcularFase();
        if (nuevaFase != faseActual)
        {
            CambiarFase(nuevaFase);
        }
        
        // Actualizar color visual para testing
        ActualizarColorVisual();
    }

    int CalcularFase()
    {
        if (vidaActual > umbralFase2) return 1;
        if (vidaActual > umbralFase3) return 2;
        if (vidaActual > umbralFase4) return 3;
        return 4;
    }

    void CambiarFase(int nuevaFase)
    {
        faseAnterior = faseActual;
        faseActual = nuevaFase;
        
        Debug.Log($"[Pilar] Fase cambiada: {faseAnterior} -> {faseActual} (Vida: {vidaActual}%)");
        OnFaseCambiada?.Invoke(faseActual);
        
        // Activar torretas en fase 4
        if (faseActual == 4 && !torretasActivas)
        {
            ActivarTorretas();
        }
    }

    void ActualizarColorVisual()
    {
        if (rend == null) return;
        
        Color targetColor = faseActual switch
        {
            1 => colorFase1,
            2 => colorFase2,
            3 => colorFase3,
            4 => colorFase4,
            _ => Color.white
        };
        
        rend.material.color = Color.Lerp(rend.material.color, targetColor, Time.deltaTime * 2f);
    }

    public void RecibirDaño(float cantidad)
    {
        if (!GameManager.Instance.juegoActivo) return;
        
        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        OnVidaCambiada?.Invoke(vidaActual);
        OnDañoRecibido?.Invoke(cantidad);
        
        if (vidaActual <= 0)
        {
            GameManager.Instance?.Derrota();
        }
    }

    public void RestaurarVida()
    {
        vidaActual = vidaMaxima;
        faseActual = 1;
        faseAnterior = 1;
        torretasActivas = false;
        OnVidaCambiada?.Invoke(vidaActual);
        
        if (rend != null)
            rend.material.color = colorFase1;
    }

    void ActivarTorretas()
    {
        torretasActivas = true;
        Debug.Log("[Pilar] ¡Protocolo de emergencia! Torretas activadas.");
        
        if (prefabTorreta == null || puntosTorretas == null) return;
        
        foreach (var punto in puntosTorretas)
        {
            if (punto != null)
                Instantiate(prefabTorreta, punto.position, punto.rotation, punto);
        }
    }

    // Getters
    public float VidaActual => vidaActual;
    public float PorcentajeVida => (vidaActual / vidaMaxima) * 100f;
    public bool EstaVivo => vidaActual > 0;
}
