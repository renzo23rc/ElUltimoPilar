/**
 * Pilar.cs
 * Gestiona la vida, estados visuales y transformaciones del Pilar.
 * Incluye detección de daño, umbrales de fase y eventos.
 * 
 * Colocar en el GameObject que representa al Pilar en el centro de la arena.
 */
using UnityEngine;
using System;
using System.Collections.Generic;

public class Pilar : MonoBehaviour
{
    private const float MinimumHealth = 0f;
    private const float PercentageScale = 100f;
    private const float MaximumHealth = 100f;
    private const int InitialPhase = 1;
    private const int EmergencyPhase = 4;
    private const float TurretSpawnHeightMeters = 1.1f;
    private const float TurretRangeMeters = 22f;
    private const float TurretFireRatePerSecond = 0.9f;
    private const float TurretDamage = 6f;
    private const float TurretHealth = 120f;
    private const int TurretAmmo = 15;
    private const float TurretReloadSeconds = 10f;
    private const float TurretLightRangeMeters = 6f;
    private const float TurretLightIntensity = 2f;

    [Header("Vida")]
    [Range(0, 100)]
    public float vidaMaxima = MaximumHealth;
    [Range(0, 100)]
    public float vidaActual = MaximumHealth;
    
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
    private int faseAnterior = InitialPhase;
    private readonly List<GameObject> spawnedTurrets = new List<GameObject>();

    void Start()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        // GameManager owns the match reset. Keep standalone Pilar scenes
        // usable without introducing a second reset in a managed match.
        if (GameManager.Instance == null)
        RestaurarVida();
    }

    void Update()
    {
        // Actualizar fase según vida - garantizar transformaciones acumulativas
        // Si vida cae de golpe cruzando varias fases, ejecutar cada fase intermedia en orden
        int destino = CalcularFase();
        if (destino > faseActual)
        {
            // Avanzar fase por fase para que ArenaTransform reciba cada evento con aviso previo
            for (int f = faseActual + 1; f <= destino; f++)
            {
                CambiarFase(f);
            }
        }
        else if (destino != faseActual)
        {
            // Por si vida sube (curación debug) - también notificar retroceso visual, pero Arena es irreversible
            CambiarFase(destino);
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
        if (faseActual == EmergencyPhase && !torretasActivas)
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

    /// <summary>Applies damage when the match is active.</summary>
    public void RecibirDaño(float cantidad)
    {
        if (GameManager.Instance != null && !GameManager.Instance.juegoActivo) return;
        
        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        OnVidaCambiada?.Invoke(vidaActual);
        OnDañoRecibido?.Invoke(cantidad);
        
        if (vidaActual <= 0)
        {
            GameManager.Instance?.Derrota();
        }
    }

    /// <summary>
    /// Daño de prueba que ignora juegoActivo (para tecla R debug y advertencias)
    /// </summary>
    public void AplicarDañoPrueba(float cantidad)
    {
        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        OnVidaCambiada?.Invoke(vidaActual);
        OnDañoRecibido?.Invoke(cantidad);
        Debug.Log($"[Pilar] Daño prueba: {cantidad}. Vida: {vidaActual}%");
        if (vidaActual <= 0)
        {
            if (GameManager.Instance != null && GameManager.Instance.juegoActivo)
                GameManager.Instance?.Derrota();
        }
    }

    /// <summary>Restores health and visual state to the initial phase.</summary>
    public void RestaurarVida()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        ClearSpawnedTurrets();
        vidaActual = vidaMaxima;
        faseActual = InitialPhase;
        faseAnterior = InitialPhase;
        torretasActivas = false;
        OnVidaCambiada?.Invoke(vidaActual);
            
        if (rend != null)
            rend.material.color = colorFase1;
    }

    void ClearSpawnedTurrets()
    {
        foreach (var turret in spawnedTurrets)
        {
            if (turret == null) continue;
            turret.SetActive(false);
            Destroy(turret);
        }
        spawnedTurrets.Clear();
    }

    void ActivarTorretas()
    {
        torretasActivas = true;
        Debug.Log("[Pilar] ¡Protocolo de emergencia! Torretas activadas. (4 torretas, proyectil físico, busca enemigo más cercano)");

        if (puntosTorretas == null) return;

        foreach (var punto in puntosTorretas)
        {
            if (punto == null) continue;
            GameObject torretaGO;
            if (prefabTorreta != null)
            {
                // Sin parent escalado para evitar que herede scale 4,2,4 del pilar y se oculte dentro
                torretaGO = Instantiate(prefabTorreta, punto.position, punto.rotation);
            }
            else
            {
                torretaGO = CrearTorretaFallback(punto);
            }
            spawnedTurrets.Add(torretaGO);
            torretaGO.name = $"Torreta_{punto.name}";
            // Asegurar que quede a ras de suelo y visible (no dentro del pilar ni del pozo) - y 1.1 = base por encima de pozo top 0.3
            torretaGO.transform.position = new Vector3(punto.position.x, TurretSpawnHeightMeters, punto.position.z);
            // Rebalanceo aplicado en runtime para prefabs viejos serializados con daño 15
            var tComp = torretaGO.GetComponent<Torreta>();
            if (tComp != null)
            {
                tComp.daño = TurretDamage;
                tComp.rango = TurretRangeMeters;
                tComp.cadencia = TurretFireRatePerSecond;
                tComp.vidaMaxima = TurretHealth;
                tComp.vidaActual = TurretHealth;
                tComp.municionMaxima = TurretAmmo;
                tComp.municionActual = TurretAmmo;
                tComp.tiempoRecarga = TurretReloadSeconds;
            }
            Debug.Log($"[Pilar] Torreta spawneada en {torretaGO.transform.position} desde punto {punto.name}");
        }
    }

    GameObject CrearTorretaFallback(Transform punto)
    {
        // Fallback procedural si no hay prefab asignado - CUBO VISIBLE GRANDE
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position = new Vector3(punto.position.x, TurretSpawnHeightMeters, punto.position.z);
        go.transform.rotation = punto.rotation;
        go.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
        var rend = go.GetComponent<Renderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color col = new Color(1f, 0.85f, 0.1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        else mat.color = col;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", col * 0.6f);
        mat.EnableKeyword("_EMISSION");
        rend.material = mat;
        Destroy(go.GetComponent<BoxCollider>());
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = col;
        light.range = TurretLightRangeMeters;
        light.intensity = TurretLightIntensity;
        // Collider para que sea dañable
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.center = Vector3.zero;
        box.size = Vector3.one;
        var t = go.AddComponent<Torreta>();
        t.rango = TurretRangeMeters;
        t.cadencia = TurretFireRatePerSecond;
        t.daño = TurretDamage;
        t.velocidadProyectil = 28f;
        t.vidaMaxima = TurretHealth;
        t.vidaActual = TurretHealth;
        t.municionMaxima = TurretAmmo;
        t.municionActual = TurretAmmo;
        t.tiempoRecarga = TurretReloadSeconds;
        var pd = new GameObject("PuntoDisparo");
        pd.transform.SetParent(go.transform);
        pd.transform.localPosition = Vector3.forward * 0.8f + Vector3.up * 0.6f;
        pd.transform.localRotation = Quaternion.identity;
        pd.transform.localScale = Vector3.one;
        t.puntoDisparo = pd.transform;
        return go;
    }

    /// <summary>Gets the current Pilar health.</summary>
    public float VidaActual => vidaActual;
    /// <summary>Gets the current health as a percentage.</summary>
    public float PorcentajeVida => (vidaActual / vidaMaxima) * PercentageScale;
    /// <summary>Gets whether the Pilar has positive health.</summary>
    public bool EstaVivo => vidaActual > MinimumHealth;

    void OnDrawGizmosSelected()
    {
        // Visualizar puntos de torretas en editor (antes de Play son invisibles)
        if (puntosTorretas != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var p in puntosTorretas)
            {
                if (p == null) continue;
                // Esfera en punto real donde spawneará (y 1.1)
                Vector3 worldPos = new Vector3(p.position.x, 1.1f, p.position.z);
                Gizmos.DrawWireCube(worldPos, new Vector3(1.4f, 2.2f, 1.4f));
                Gizmos.DrawLine(p.position, worldPos);
                // indicar radio pozo para referencia
                Gizmos.color = new Color(1,0,0,0.15f);
                Gizmos.DrawWireSphere(transform.position, 5f);
                Gizmos.color = Color.yellow;
            }
        }
        // Dibujar rango torreta si hay una instanciada
        if (faseActual == 4)
        {
            Gizmos.color = new Color(0,1,1,0.2f);
            foreach (var p in puntosTorretas)
            {
                if (p==null) continue;
                Gizmos.DrawWireSphere(new Vector3(p.position.x, 1.1f, p.position.z), 25f);
            }
        }
    }
}
