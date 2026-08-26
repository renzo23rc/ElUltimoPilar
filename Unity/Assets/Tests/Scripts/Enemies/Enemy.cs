/**
 * Enemy.cs
 * Clase base para todos los enemigos del Enjambre.
 * Gestiona vida, movimiento básico hacia el Pilar, y drops de energía.
 * 
 * Heredar de esta clase para crear tipos específicos (Runner, Artillery, etc.)
 */
using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Header("Estadísticas Base")]
    public float vidaMaxima = 30f;
    public float vidaActual = 30f;
    public float velocidadMovimiento = 5f;
    public float dañoAlPilar = 10f;
    public float dañoAlJugador = 15f;
    public int energiaDrop = 2;
    
    [Header("Comportamiento")]
    public bool atacaJugador = false; // Si es false, va directo al Pilar
    public float rangoAtaque = 2f;
    public float cooldownAtaque = 1f;
    
    [Header("Referencias")]
    public Transform modeloVisual;
    public GameObject prefabEnergia;
    
    // Eventos
    public event Action OnMuerte;
    public event Action<float> OnDañoRecibido;
    
    protected Pilar pilarObjetivo;
    protected PlayerController jugadorObjetivo;
    protected float timerAtaque = 0f;
    protected bool estaMuerto = false;
    protected Rigidbody rb;

    protected virtual void Start()
    {
        vidaActual = vidaMaxima;
        pilarObjetivo = FindFirstObjectByType<Pilar>();
        rb = GetComponent<Rigidbody>();
        
        // Encontrar jugador más cercano (para cooperativo, se podría mejorar)
        jugadorObjetivo = FindFirstObjectByType<PlayerController>();
    }

    protected virtual void Update()
    {
        if (estaMuerto) return;
        if (!GameManager.Instance.juegoActivo) return;
        
        timerAtaque -= Time.deltaTime;
        
        Comportamiento();
    }

    protected virtual void Comportamiento()
    {
        // Comportamiento base: moverse hacia el Pilar
        if (pilarObjetivo == null) return;
        
        Vector3 direccion = pilarObjetivo.transform.position - transform.position;
        direccion.y = 0;
        float distancia = direccion.magnitude;
        
        if (distancia > rangoAtaque)
        {
            MoverHacia(direccion.normalized);
        }
        else
        {
            AtacarPilar();
        }
    }

    protected virtual void MoverHacia(Vector3 direccion)
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + direccion * velocidadMovimiento * Time.deltaTime);
        }
        else
        {
            transform.position += direccion * velocidadMovimiento * Time.deltaTime;
        }
        
        // Rotar hacia la dirección
        if (direccion != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(direccion), Time.deltaTime * 10f);
        }
    }

    protected virtual void AtacarPilar()
    {
        if (timerAtaque > 0) return;
        
        pilarObjetivo?.RecibirDaño(dañoAlPilar);
        timerAtaque = cooldownAtaque;
        
        Debug.Log($"[{GetType().Name}] Atacó al Pilar por {dañoAlPilar} de daño");
    }

    public virtual void RecibirDaño(float cantidad)
    {
        if (estaMuerto) return;
        
        vidaActual -= cantidad;
        OnDañoRecibido?.Invoke(cantidad);
        
        // Feedback visual de daño
        StartCoroutine(FlashDaño());
        
        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    protected virtual void Morir()
    {
        estaMuerto = true;
        OnMuerte?.Invoke();
        
        // Dropear energía
        DropearEnergia();
        
        // Notificar al spawner
        EnemySpawner.Instance?.EnemigoEliminado(this);
        
        Destroy(gameObject, 0.1f);
    }

    protected virtual void DropearEnergia()
    {
        if (prefabEnergia != null)
        {
            var go = Instantiate(prefabEnergia, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            var pickup = go.GetComponent<EnergyPickup>();
            if (pickup != null)
                pickup.cantidad = energiaDrop;
        }
    }

    System.Collections.IEnumerator FlashDaño()
    {
        Renderer rend = modeloVisual?.GetComponent<Renderer>() ?? GetComponent<Renderer>();
        if (rend != null)
        {
            Color original = rend.material.color;
            rend.material.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            rend.material.color = original;
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null && timerAtaque <= 0)
            {
                player.RecibirDaño(dañoAlJugador);
                timerAtaque = cooldownAtaque;
            }
        }
    }
}
