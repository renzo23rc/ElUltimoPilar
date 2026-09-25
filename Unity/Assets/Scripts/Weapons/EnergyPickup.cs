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
    private static readonly float FullCircleRadians = Mathf.PI * 2f;

    [Header("Configuración")]
    public float cantidad = 2f;
    public float velocidadRotacion = 100f;
    public float velocidadLevitacion = 2f;
    public float alturaLevitacion = 0.5f;
    public float rangoAtraccion = 5f;
    public float velocidadAtraccion = 8f;
    
    private Vector3 posicionInicial;
    private float tiempo;
    private bool recolectado;

    void Start()
    {
        posicionInicial = transform.position;
        tiempo = Random.Range(0f, FullCircleRadians);
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
        PlayerController jugador = PlayerLocator.FindClosestRegistered(transform.position);
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
        // OnTriggerStay reenvía acá: sin esta guarda un mismo orbe podía sumarse más de una vez.
        if (recolectado) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            recolectado = true;
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
        recolectado = false;
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
}
