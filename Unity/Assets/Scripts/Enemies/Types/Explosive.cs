/**
 * Explosive.cs
 * Explosivo/Kamikaze: Explota al morir o al llegar al Pilar.
 * Se puede empujar a pozos o zonas de gravedad.
 */
using UltimoPilar.Core.Shared;
using UnityEngine;

public class Explosive : Enemy
{
    private const float MovementSpeedMetersPerSecond = 2f;
    private const float MaximumHealth = 25f;
    private const float PilarDamage = 30f;
    private const int EnergyDropAmount = 5;
    // Detona al tocar el Pilar: radio del Pilar (2) + medio cuerpo (0.5) + margen.
    private const float AttackRangeMeters = 3.5f;
    private const float PlayerDamageMultiplier = 0.5f;

    [Header("Explosivo Específico")]
    public float radioExplosion = 5f;
    public float dañoExplosion = 40f;
    public float tiempoDetonacion = 0.5f;
    public GameObject prefabExplosion;
    public Color colorAdvertencia = Color.red;

    private bool detonando = false;
    private Renderer rend;

    protected override void Start()
    {
        base.Start();
        atacaJugador = false;
        velocidadMovimiento = MovementSpeedMetersPerSecond;
        vidaMaxima = MaximumHealth;
        vidaActual = vidaMaxima;
        dañoAlPilar = PilarDamage;
        energiaDrop = EnergyDropAmount;
        rangoAtaque = AttackRangeMeters;

        // Chequeo explícito: ?. no respeta el fake-null de Unity.
        if (modeloVisual != null)
        {
            rend = modeloVisual.GetComponent<Renderer>();
        }

        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
        }
    }

    protected override void Comportamiento()
    {
        if (detonando || pilarObjetivo == null)
        {
            return;
        }

        Vector3 direccion = pilarObjetivo.transform.position - transform.position;
        direccion.y = 0;
        float distancia = direccion.magnitude;

        if (distancia > rangoAtaque)
        {
            MoverHacia(direccion.normalized);
        }
        else
        {
            IniciarDetonacion();
        }
    }

    void IniciarDetonacion()
    {
        if (detonando || estaMuerto)
        {
            return;
        }

        detonando = true;

        // El color de advertencia vive en el material; se corta el flash para que se vea.
        DetenerFlashDaño();
        if (rend != null)
        {
            rend.material.color = colorAdvertencia;
        }

        Invoke(nameof(Explosion), tiempoDetonacion);
    }

    void Explosion()
    {
        if (estaMuerto)
        {
            return;
        }

        Vector3 centro = transform.position;
        foreach (Pilar pilar in OverlapQuery.FindUniqueInSphere<Pilar>(centro, radioExplosion))
        {
            pilar.RecibirDaño(dañoExplosion);
        }

        foreach (PlayerController player in OverlapQuery.FindUniqueInSphere<PlayerController>(centro, radioExplosion))
        {
            player.RecibirDaño(dañoExplosion * PlayerDamageMultiplier);
        }

        foreach (Enemy enemy in OverlapQuery.FindUniqueInSphere<Enemy>(centro, radioExplosion))
        {
            if (enemy != this)
            {
                enemy.RecibirDaño(dañoExplosion);
            }
        }

        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, centro, Quaternion.identity);
        }

        CombatFeedback.NotifyHit(true);

        // Al explotar no dropea energía (no pasa por Morir), para no duplicar el drop.
        estaMuerto = true;
        EnemySpawner.Instance?.EnemigoEliminado(this);
        Destroy(gameObject);
    }

    public override void RecibirDaño(float cantidad)
    {
        if (estaMuerto || detonando)
        {
            return;
        }

        vidaActual -= cantidad;
        NotificarDañoRecibido(cantidad);
        CombatFeedback.NotifyHit(vidaActual <= 0);
        IniciarFlashDaño();

        if (vidaActual <= 0)
        {
            // Detona sin pasar por base.Morir(), que dropearía energía.
            IniciarDetonacion();
        }
    }

    protected override void Morir()
    {
        // Si ya está detonando, se ignora Morir para evitar doble Destroy/Drop.
        if (detonando)
        {
            return;
        }

        base.Morir();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CancelInvoke();
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        // Detonación por contacto físico con el Pilar (si la distancia no alcanza por collider grande).
        if (collision.collider.GetComponentInParent<Pilar>() != null)
        {
            IniciarDetonacion();
            return;
        }

        base.OnCollisionEnter(collision);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}
