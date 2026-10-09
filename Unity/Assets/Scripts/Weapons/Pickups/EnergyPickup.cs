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
    private bool recolectado;

    void Start()
    {
        posicionInicial = transform.position;
        tiempo = PickupMotion.RandomPhase();
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        PickupMotion.SpinAndBob(transform, posicionInicial.y, tiempo, velocidadRotacion, velocidadLevitacion, alturaLevitacion);

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
            PoolManager.ReleaseOrDestroy(gameObject);
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
        if (GetComponent<Rigidbody>() == null)
            PickupMotion.EnsureKinematicBody(gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        // Fallback por si CharacterController no dispara OnTriggerEnter
        OnTriggerEnter(other);
    }
}
