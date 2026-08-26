/**
 * ArenaTransform.cs
 * Gestiona las transformaciones acumulativas e irreversibles de la arena
 * según los umbrales de vida del Pilar.
 * 
 * Según el GDD:
 * - Fase 1 (100-75%): Arena base
 * - Fase 2 (75-50%): Pozo central se abre
 * - Fase 3 (50-25%): Zona de gravedad alterada
 * - Fase 4 (25-0%): Protocolo de emergencia + caos
 * 
 * Colocar en un GameObject vacío "ArenaManager" o en el suelo/arena.
 */
using UnityEngine;
using System;
using System.Collections;

public class ArenaTransform : MonoBehaviour
{
    [Header("Referencias")]
    public Pilar pilar;
    public GameObject sueloBase;
    
    [Header("Elementos de Transformación")]
    public GameObject pozoCentral; // Se activa en fase 2
    public GameObject zonaGravedad; // Se activa en fase 3
    public GameObject escombrosFase4; // Se activan en fase 4
    public GameObject[] obstaculosAdicionales;
    
    [Header("Configuración")]
    public float tiempoAvisoPrevio = 3f;
    public Color colorAviso = Color.yellow;
    public AudioClip sonidoTransformacion;
    
    [Header("Estado")]
    public int faseActual = 1;
    public bool transformacionEnProgreso = false;
    
    // Eventos
    public event Action<int> OnTransformacionIniciada;
    public event Action<int> OnTransformacionCompletada;
    
    private bool[] fasesActivadas = new bool[5]; // Índice 0 no usado, 1-4

    void Start()
    {
        if (pilar == null)
            pilar = FindFirstObjectByType<Pilar>();
        
        // Suscribirse a cambios de fase del Pilar
        if (pilar != null)
        {
            pilar.OnFaseCambiada += OnPilarFaseCambiada;
        }
        
        // Estado inicial: todo desactivado excepto fase 1
        InicializarEstado();
    }

    void OnDestroy()
    {
        if (pilar != null)
            pilar.OnFaseCambiada -= OnPilarFaseCambiada;
    }

    void InicializarEstado()
    {
        if (pozoCentral != null) pozoCentral.SetActive(false);
        if (zonaGravedad != null) zonaGravedad.SetActive(false);
        if (escombrosFase4 != null) escombrosFase4.SetActive(false);
        
        if (obstaculosAdicionales != null)
        {
            foreach (var obs in obstaculosAdicionales)
            {
                if (obs != null) obs.SetActive(false);
            }
        }
    }

    void OnPilarFaseCambiada(int nuevaFase)
    {
        if (nuevaFase <= faseActual) return; // Solo avanza, nunca retrocede
        if (fasesActivadas[nuevaFase]) return; // Ya se activó esta fase
        
        faseActual = nuevaFase;
        StartCoroutine(EjecutarTransformacion(nuevaFase));
    }

    IEnumerator EjecutarTransformacion(int fase)
    {
        transformacionEnProgreso = true;
        
        // 1. Aviso previo
        Debug.Log($"[ArenaTransform] ¡AVISO! Transformación a Fase {fase} en {tiempoAvisoPrevio} segundos...");
        OnTransformacionIniciada?.Invoke(fase);
        
        // Sonido de aviso (stinger)
        if (sonidoTransformacion != null)
            AudioSource.PlayClipAtPoint(sonidoTransformacion, Camera.main.transform.position, 0.7f);
        
        // Efecto visual de aviso (parpadear el suelo)
        yield return StartCoroutine(AvisoVisual(tiempoAvisoPrevio));
        
        // 2. Ejecutar transformación
        switch (fase)
        {
            case 2:
                ActivarPozoCentral();
                break;
            case 3:
                ActivarZonaGravedad();
                break;
            case 4:
                ActivarProtocoloEmergencia();
                break;
        }
        
        fasesActivadas[fase] = true;
        transformacionEnProgreso = false;
        OnTransformacionCompletada?.Invoke(fase);
        
        Debug.Log($"[ArenaTransform] Transformación a Fase {fase} completada.");
    }

    IEnumerator AvisoVisual(float duracion)
    {
        float timer = 0f;
        Renderer rend = sueloBase?.GetComponent<Renderer>();
        Color colorOriginal = rend != null ? rend.material.color : Color.gray;
        
        while (timer < duracion)
        {
            timer += Time.deltaTime;
            float t = timer / duracion;
            
            // Parpadeo que acelera
            float parpadeo = Mathf.PingPong(Time.time * (1f + t * 5f), 1f);
            
            if (rend != null)
            {
                rend.material.color = Color.Lerp(colorOriginal, colorAviso, parpadeo * 0.5f);
            }
            
            yield return null;
        }
        
        if (rend != null)
            rend.material.color = colorOriginal;
    }

    void ActivarPozoCentral()
    {
        if (pozoCentral != null)
        {
            pozoCentral.SetActive(true);
            
            // Animación de apertura
            StartCoroutine(AnimarApertura(pozoCentral));
        }
        
        Debug.Log("[ArenaTransform] Pozo central abierto. ¡Cuidado con los bordes!");
    }

    void ActivarZonaGravedad()
    {
        if (zonaGravedad != null)
        {
            zonaGravedad.SetActive(true);
            
            // Efecto visual de distorsión
            var particulas = zonaGravedad.GetComponentInChildren<ParticleSystem>();
            if (particulas != null) particulas.Play();
        }
        
        Debug.Log("[ArenaTransform] Zona de gravedad alterada activada. ¡Saltar para moverte!");
    }

    void ActivarProtocoloEmergencia()
    {
        if (escombrosFase4 != null)
        {
            escombrosFase4.SetActive(true);
        }
        
        // Activar obstáculos adicionales
        if (obstaculosAdicionales != null)
        {
            foreach (var obs in obstaculosAdicionales)
            {
                if (obs != null) obs.SetActive(true);
            }
        }
        
        // Caída de escombros visual
        StartCoroutine(CaidaEscombros());
        
        Debug.Log("[ArenaTransform] PROTOCOLO DE EMERGENCIA. La arena está severamente dañada.");
    }

    IEnumerator AnimarApertura(GameObject objeto)
    {
        Vector3 escalaFinal = objeto.transform.localScale;
        objeto.transform.localScale = Vector3.zero;
        
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            objeto.transform.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, timer);
            yield return null;
        }
        
        objeto.transform.localScale = escalaFinal;
    }

    IEnumerator CaidaEscombros()
    {
        // Simular caída de escombros pequeños
        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = UnityEngine.Random.insideUnitSphere * 15f;
            pos.y = 10f;
            
            GameObject escombro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            escombro.transform.position = pos;
            escombro.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.3f, 1f);
            escombro.GetComponent<Renderer>().material.color = Color.gray;
            
            Destroy(escombro.GetComponent<Collider>());
            
            Rigidbody rb = escombro.AddComponent<Rigidbody>();
            rb.mass = 0.1f;
            
            Destroy(escombro, 3f);
            
            yield return new WaitForSeconds(0.2f);
        }
    }

    // Zona de gravedad alterada
    void OnTriggerStay(Collider other)
    {
        if (zonaGravedad != null && zonaGravedad.activeInHierarchy && zonaGravedad.GetComponent<Collider>() == other)
        {
            // Aplicar efecto de gravedad alterada
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * 5f, ForceMode.Acceleration);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar radio de la arena
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 20f);
        
        // Dibujar zonas de transformación
        if (pozoCentral != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(pozoCentral.transform.position, pozoCentral.transform.localScale);
        }
    }
}
