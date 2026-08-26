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
        PlayerController jugador = FindFirstObjectByType<PlayerController>();
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
        if (player != null)
        {
            var energia = player.GetComponent<EnergySystem>();
            if (energia != null)
            {
                energia.RecolectarEnergia(cantidad);
            }
            
            Destroy(gameObject);
        }
    }
}
