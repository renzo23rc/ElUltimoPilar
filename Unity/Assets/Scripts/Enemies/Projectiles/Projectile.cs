/**
 * Projectile.cs
 * Proyectil básico usado por el Artillero y posiblemente armas del jugador.
 */
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    private const float ImpactVfxDurationSeconds = 1f;
    private const string ImpactPoolKey = "Impacto";

    [Header("Configuración")]
    public float daño = 10f;
    public float dañoJugador = 10f;
    public float tiempoVida = 5f;
    public bool destruirAlImpactar = true;
    public GameObject prefabImpacto;
    
    private bool impactado;

    void OnEnable()
    {
        // Las instancias del pool se reutilizan: cada activación puede impactar una vez.
        impactado = false;
    }

    void Start()
    {
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
        // Un proyectil impacta una sola vez aunque toque varios colliders en el mismo paso.
        if (impactado || other.GetComponentInParent<Projectile>() != null) return;

        var pilar = other.GetComponentInParent<Pilar>();
        if (pilar != null)
        {
            pilar.RecibirDaño(daño);
            Impacto();
            return;
        }
        
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.RecibirDaño(dañoJugador);
            Impacto();
            return;
        }
        
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.RecibirDaño(daño);
            Impacto();
            return;
        }

        var torreta = other.GetComponentInParent<Torreta>();
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
            GameObject vfx = PoolManager.Instance != null
                ? PoolManager.Instance.GetVFX(ImpactPoolKey, transform.position, Quaternion.identity, ImpactVfxDurationSeconds)
                : null;
            if (vfx == null)
                Destroy(Instantiate(prefabImpacto, transform.position, Quaternion.identity), ImpactVfxDurationSeconds);
        }

        if (destruirAlImpactar)
        {
            impactado = true;
            var pooled = GetComponent<PooledObject>();
            if (pooled != null && !string.IsNullOrEmpty(pooled.poolKey) && PoolManager.Instance != null)
                PoolManager.Instance.Release(pooled.poolKey, gameObject);
            else
                Destroy(gameObject);
        }
    }
}
