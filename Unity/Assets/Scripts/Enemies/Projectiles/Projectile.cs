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

    /// <summary>
    /// Sets the damage this shot deals and, when it comes from a pool, schedules its return.
    /// </summary>
    /// <param name="dañoObjetivos">The damage dealt to the Pilar, enemies, and turrets.</param>
    /// <param name="dañoAJugadores">The damage dealt to players.</param>
    public void ConfigurarDaño(float dañoObjetivos, float dañoAJugadores)
    {
        daño = dañoObjetivos;
        dañoJugador = dañoAJugadores;
        if (PoolManager.Instance != null && TryGetComponent(out PooledObject pooled))
            pooled.ScheduleRelease(tiempoVida);
    }

    void OnTriggerEnter(Collider other)
    {
        // Un proyectil impacta una sola vez aunque toque varios colliders en el mismo paso.
        if (impactado || other.GetComponentInParent<Projectile>() != null) return;

        // Pilar, jugadores, enemigos y torretas reciben el daño por la misma frontera.
        IDamageable objetivo = other.GetComponentInParent<IDamageable>();
        if (objetivo != null)
        {
            float cantidad = objetivo is PlayerController ? dañoJugador : daño;
            objetivo.ReceiveDamage(new DamageRequest(cantidad));
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
            PoolManager.ReleaseOrDestroy(gameObject);
        }
    }
}
