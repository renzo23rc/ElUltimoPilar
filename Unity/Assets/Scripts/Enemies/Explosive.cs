/**
 * Explosive.cs
 * Explosivo/Kamikaze: Explota al morir o al llegar al Pilar.
 * Se puede empujar a pozos o zonas de gravedad.
 */
using UnityEngine;

public class Explosive : Enemy
{
private const float MovementSpeedMetersPerSecond = 2f;
private const float MaximumHealth = 25f;
private const float PilarDamage = 30f;
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
        dañoAlPilar = PilarDamage; // Daño directo si llega
        energiaDrop = 5;
        rangoAtaque = AttackRangeMeters; // Aumentado de 2 -> 3.5 para que detone al tocar pilar (radio pilar 2 + half 0.5)
        
        // Fix UnassignedReference: chequeo explicito (?. no funciona con fake-null de Unity)
        if (modeloVisual != null) rend = modeloVisual.GetComponent<Renderer>();
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
    }

    protected override void Comportamiento()
    {
        if (detonando) return;
        
        // Moverse hacia el Pilar
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
            IniciarDetonacion();
        }
    }

    void IniciarDetonacion()
    {
        if (detonando || estaMuerto) return;
        detonando = true;
        
        // Parpadear en rojo como advertencia
        if (rend != null)
            rend.material.color = colorAdvertencia;
        
        Invoke(nameof(Explosion), tiempoDetonacion);
    }

    void Explosion()
    {
        if (estaMuerto) return; // Evitar doble ejecución
        // Daño en área
        Collider[] afectados = Physics.OverlapSphere(transform.position, radioExplosion);
        foreach (var col in afectados)
        {
            var pilar = col.GetComponent<Pilar>();
            if (pilar != null)
            {
                pilar.RecibirDaño(dañoExplosion);
            }
            
            var player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                player.RecibirDaño(dañoExplosion * PlayerDamageMultiplier);
            }
            
            var enemy = col.GetComponent<Enemy>();
            if (enemy != null && enemy != this)
            {
                enemy.RecibirDaño(dañoExplosion);
            }
        }
        
        // Efecto visual
        if (prefabExplosion != null)
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);
        
        CombatFeedback.NotifyHit(true);
        Debug.Log("[Explosive] ¡BOOM!");
        
        // No dropear energía si explota (se destruye sin llamar a Morir) - evita duplicar drop
        estaMuerto = true;
        EnemySpawner.Instance?.EnemigoEliminado(this);
        Destroy(gameObject);
    }

    public override void RecibirDaño(float cantidad)
    {
        if (estaMuerto || detonando) return;

        vidaActual -= cantidad;
        NotificarDañoRecibido(cantidad);
        CombatFeedback.NotifyHit(vidaActual <= 0);
        StartCoroutine(FlashDaño());

        if (vidaActual <= 0)
        {
            // Iniciar detonación sin pasar por base.Morir() (que dropearía energía)
            IniciarDetonacion();
        }
    }

    protected override void Morir()
    {
        // Si ya está detonando, ignorar Morir base (evita doble Destroy/Drop)
        if (detonando) return;
        base.Morir();
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        // Detonación por contacto físico con Pilar (fallback si distancia no alcanza por collider grande)
        if (collision.collider.GetComponent<Pilar>() != null || collision.collider.GetComponentInParent<Pilar>() != null)
        {
            IniciarDetonacion();
            return;
        }
        // Mantener daño a jugador base
        base.OnCollisionEnter(collision);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}
