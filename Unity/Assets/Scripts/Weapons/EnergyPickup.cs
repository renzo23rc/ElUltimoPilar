/**
 * EnergyPickup.cs
 * Orbe de energía dropeado por enemigos al morir.
 * El jugador lo recolecta al acercarse.
 * 
 * Colocar como prefab con un Collider (trigger) y este script.
 */
using UnityEngine;

public class EnergyPickup : MonoBehaviour
{
    [Header("Configuración")]
    public float cantidad = 2f;
    public float velocidadRotacion = 100f;
    public float velocidadLevitacion = 2f;
    public float alturaLevitacion = 0.5f;
    public float rangoAtraccion = 5f;
    public float velocidadAtraccion = 8f;
    
    private Vector3 posicionInicial;
    private float tiempo;

    void Start()
    {
        posicionInicial = transform.position;
        tiempo = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        
        // Rotación
        transform.Rotate(Vector3.up, velocidadRotacion * Time.deltaTime);
        
        // Levitación
        float y = posicionInicial.y + Mathf.Sin(tiempo * velocidadLevitacion) * alturaLevitacion;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        
        // Atracción hacia jugador cercano
        AtraccionJugador();
    }

    void AtraccionJugador()
    {
        PlayerController jugador = ResolverJugadorCercano();
        if (jugador == null) return;
        
        float distancia = Vector3.Distance(transform.position, jugador.transform.position);
        
        if (distancia <= rangoAtraccion)
        {
            Vector3 direccion = (jugador.transform.position - transform.position).normalized;
            transform.position += direccion * velocidadAtraccion * Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            var energia = player.GetComponent<EnergySystem>();
            if (energia != null)
            {
                energia.RecolectarEnergia(cantidad);
            }
            
            // Si está en pool, liberar en vez de Destroy
            var pooled = GetComponent<PooledObject>();
            if (pooled != null && !string.IsNullOrEmpty(pooled.poolKey) && PoolManager.Instance != null)
                PoolManager.Instance.Release(pooled.poolKey, gameObject);
            else
                Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // Reset levitación base al respawn desde pool y asegurar trigger no bloquea enemigos
        posicionInicial = transform.position;
        var col = GetComponent<SphereCollider>();
        if (col != null) col.isTrigger = true;
        // Asegurar que trigger funcione con CharacterController (necesita Rigidbody en trigger)
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Fallback por si CharacterController no dispara OnTriggerEnter
        OnTriggerEnter(other);
    }
    
    PlayerController ResolverJugadorCercano()
    {
        var manager = GameManager.Instance;
        if (manager != null && manager.PlayerCount > 0)
        {
            PlayerController cercano = null;
            float minDist = float.MaxValue;
            foreach (var jugador in manager.Players)
            {
                if (jugador == null) continue;
                float d = Vector3.Distance(transform.position, jugador.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    cercano = jugador;
                }
            }
            if (cercano != null) return cercano;
        }
        return FindFirstObjectByType<PlayerController>();
    }
}
