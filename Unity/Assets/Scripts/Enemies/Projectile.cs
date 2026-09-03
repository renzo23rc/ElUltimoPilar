/**
 * Projectile.cs
 * Proyectil básico usado por el Artillero y posiblemente armas del jugador.
 */
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
private const float ImpactVfxDurationSeconds = 1f;
    [Header("Configuración")]
    public float daño = 10f;
    public float dañoJugador = 10f;
    public float tiempoVida = 5f;
    public bool destruirAlImpactar = true;
    public GameObject prefabImpacto;
    
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Si está en pool, el PooledObject manejará auto-release; si no, Destroy tradicional
        if (TryGetComponent<PooledObject>(out var pooled) && !string.IsNullOrEmpty(pooled.poolKey))
        {
            pooled.ScheduleRelease(tiempoVida);
        }
        else
        {
            Destroy(gameObject, tiempoVida);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Ignorar otros proyectiles y enemigos (si es proyectil enemigo)
        if (other.GetComponent<Projectile>() != null) return;
        
        var pilar = other.GetComponent<Pilar>();
        if (pilar != null)
        {
            pilar.RecibirDaño(daño);
            Impacto();
            return;
        }
        
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.RecibirDaño(dañoJugador);
            Impacto();
            return;
        }
        
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.RecibirDaño(daño);
            Impacto();
            return;
        }

        var torreta = other.GetComponent<Torreta>();
        if (torreta == null) torreta = other.GetComponentInParent<Torreta>();
        if (torreta != null)
        {
            torreta.RecibirDaño(daño);
            Impacto();
            return;
        }
        
        // Impacto con cualquier otra cosa (pared, suelo, etc)
        if (!other.isTrigger)
        {
            Impacto();
        }
    }

    void Impacto()
    {
        if (prefabImpacto != null)
        {
            if (PoolManager.Instance != null)
                PoolManager.Instance.GetVFX("Impacto", transform.position, Quaternion.identity, ImpactVfxDurationSeconds);
            else
                Instantiate(prefabImpacto, transform.position, Quaternion.identity);
        }
        
        if (destruirAlImpactar)
        {
            var pooled = GetComponent<PooledObject>();
            if (pooled != null && !string.IsNullOrEmpty(pooled.poolKey) && PoolManager.Instance != null)
                PoolManager.Instance.Release(pooled.poolKey, gameObject);
            else
                Destroy(gameObject);
        }
    }
}
