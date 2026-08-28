/**
 * EnergySystem.cs
 * Gestiona la energía dropeada por enemigos.
 * Permite gastar energía en curación o habilidades.
 * Según el GDD: 20 puntos = 1% de vida del jugador.
 * 
 * Colocar en el mismo GameObject que PlayerController.
 */
using UnityEngine;
using System;

public class EnergySystem : MonoBehaviour
{
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
    
    private PlayerController player;

    void Start()
    {
        player = GetComponent<PlayerController>();
        energiaActual = 0f;
    }

    public void RecolectarEnergia(float cantidad)
    {
        energiaActual = Mathf.Min(energiaMaxima, energiaActual + cantidad);
        OnEnergiaCambiada?.Invoke(energiaActual);
        
        Debug.Log($"[EnergySystem] +{cantidad} energía. Total: {energiaActual}/{energiaMaxima}");
    }

    public bool GastarEnCuracion()
    {
        if (energiaActual >= costoCuracion && player != null)
        {
            energiaActual -= costoCuracion;
            player.Curar(vidaPorCuracion);
            OnEnergiaCambiada?.Invoke(energiaActual);
            
            Debug.Log($"[EnergySystem] Curación usada. Vida +{vidaPorCuracion}%. Energía restante: {energiaActual}");
            return true;
        }
        return false;
    }

    public bool ActivarHabilidad()
    {
        if (energiaActual >= costoHabilidad)
        {
            energiaActual -= costoHabilidad;
            OnEnergiaCambiada?.Invoke(energiaActual);
            
            if (habilidadPulsoDaño)
            {
                PulsoDeDaño();
            }
            else
            {
                RalentizacionArea();
            }
            
            OnHabilidadActivada?.Invoke();
            return true;
        }
        return false;
    }

    void PulsoDeDaño()
    {
        Collider[] enemigos = Physics.OverlapSphere(transform.position, radioPulso);
        int contador = 0;
        foreach (var col in enemigos)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.RecibirDaño(dañoPulso);
                contador++;
            }
        }
        
        Debug.Log($"[EnergySystem] ¡Pulso de daño! {contador} enemigos afectados.");
        
        // Efecto visual simple
        CrearOndaVisual(Color.yellow, radioPulso);
    }

    void RalentizacionArea()
    {
        // Ralentización temporal (stack prohibido) afecta a enemigos y jugadores en área
        Collider[] colisiones = Physics.OverlapSphere(transform.position, radioPulso * 1.5f);
        int countE = 0, countP = 0;
        foreach (var col in colisiones)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy == null) enemy = col.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.AplicarRalentizacion(factorRalentizacion, duracionRalentizacion);
                countE++;
            }
            var player = col.GetComponent<PlayerController>();
            if (player == null) player = col.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                // No ralentizar al propio lanzador si se desea? Sí afecta a todos por spec, incluso self, pero evitamos self para habilidad aliada
                // Por ahora sí afecta a todos excepto self para no penalizar al usarla
                if (player.gameObject != this.gameObject)
                {
                    player.AplicarRalentizacion(factorRalentizacion, duracionRalentizacion);
                    countP++;
                }
            }
        }
        // También afectar a todos los jugadores si rango incluye múltiples (co-op)
        var todosPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in todosPlayers)
        {
            if (p.gameObject == this.gameObject) continue;
            float d = Vector3.Distance(p.transform.position, transform.position);
            if (d <= radioPulso * 1.5f && !System.Array.Exists(colisiones, c => c.GetComponentInParent<PlayerController>() == p))
            {
                p.AplicarRalentizacion(factorRalentizacion, duracionRalentizacion);
                countP++;
            }
        }
        
        Debug.Log($"[EnergySystem] ¡Ralentización área! Enemigos {countE}, Jugadores {countP} x{factorRalentizacion} por {duracionRalentizacion}s");
        CrearOndaVisual(Color.cyan, radioPulso * 1.5f);
    }

    void CrearOndaVisual(Color color, float radio)
    {
        GameObject onda = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        onda.name = "OndaHabilidad";
        Destroy(onda.GetComponent<Collider>());
        onda.transform.position = transform.position;
        onda.transform.localScale = Vector3.one * 0.1f;
        
        Renderer rend = onda.GetComponent<Renderer>();
        rend.material.color = new Color(color.r, color.g, color.b, 0.3f);
        rend.material.SetFloat("_Mode", 3);
        
        // Animación simple de expansión
        onda.AddComponent<OndaExpansion>().Iniciar(radio, 0.5f);
    }
}

/**
 * OndaExpansion.cs
 * Helper para animar la onda visual de habilidades.
 */
public class OndaExpansion : MonoBehaviour
{
    private float radioObjetivo;
    private float duracion;
    private float timer;

    public void Iniciar(float radio, float tiempo)
    {
        radioObjetivo = radio;
        duracion = tiempo;
        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duracion;
        
        float escala = Mathf.Lerp(0.1f, radioObjetivo, t);
        transform.localScale = new Vector3(escala, escala * 0.2f, escala);
        
        if (timer >= duracion + 0.2f)
        {
            Destroy(gameObject);
        }
    }
}
