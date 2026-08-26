/**
 * Explosive.cs
 * Explosivo/Kamikaze: Explota al morir o al llegar al Pilar.
 * Se puede empujar a pozos o zonas de gravedad.
 */
using UnityEngine;

public class Explosive : Enemy
{
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
        velocidadMovimiento = 4f;
        vidaMaxima = 25f;
        vidaActual = vidaMaxima;
        dañoAlPilar = 30f; // Daño directo si llega
        energiaDrop = 5;
        rangoAtaque = 2f;
        
        rend = modeloVisual?.GetComponent<Renderer>() ?? GetComponent<Renderer>();
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
        if (detonando) return;
        detonando = true;
        
        // Parpadear en rojo como advertencia
        if (rend != null)
            rend.material.color = colorAdvertencia;
        
        Invoke(nameof(Explosion), tiempoDetonacion);
    }

    void Explosion()
    {
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
                player.RecibirDaño(dañoExplosion * 0.5f);
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
        
        Debug.Log("[Explosive] ¡BOOM!");
        
        // No dropear energía si explota (se destruye sin llamar a Morir)
        estaMuerto = true;
        EnemySpawner.Instance?.EnemigoEliminado(this);
        Destroy(gameObject);
    }

    public override void RecibirDaño(float cantidad)
    {
        base.RecibirDaño(cantidad);
        
        // Si muere por daño, también explota
        if (vidaActual <= 0 && !detonando)
        {
            vidaActual = 1; // Evitar que base.Morir() se ejecute
            IniciarDetonacion();
        }
    }

    protected override void Morir()
    {
        // Sobrescrito para manejar la explosión
        if (detonando) return;
        base.Morir();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioExplosion);
    }
}
