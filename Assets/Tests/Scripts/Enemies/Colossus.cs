/**
 * Colossus.cs
 * Coloso (mini-jefe de oleada tardía): Lento, mucha vida, resistente a disparos directos.
 * La única forma viable es empujarlo a pozos o zonas de gravedad.
 */
using UnityEngine;

public class Colossus : Enemy
{
    [Header("Coloso Específico")]
    public float resistenciaDisparos = 0.8f; // Reduce 80% del daño de disparos
    public float dañoEmpuje = 30f;
    public float radioAtaque = 4f;
    public GameObject prefabOndaImpacto;
    
    private bool enZonaPeligrosa = false;

    protected override void Start()
    {
        base.Start();
        atacaJugador = true;
        velocidadMovimiento = 1.5f;
        vidaMaxima = 300f;
        vidaActual = vidaMaxima;
        dañoAlPilar = 25f;
        dañoAlJugador = 20f;
        energiaDrop = 20;
        rangoAtaque = radioAtaque;
    }

    protected override void Comportamiento()
    {
        if (pilarObjetivo == null) return;
        
        Vector3 direccion = pilarObjetivo.transform.position - transform.position;
        direccion.y = 0;
        float distancia = direccion.magnitude;
        
        // Atacar área si está cerca
        if (distancia <= rangoAtaque)
        {
            AtacarArea();
        }
        else
        {
            MoverHacia(direccion.normalized);
        }
    }

    void AtacarArea()
    {
        if (timerAtaque > 0) return;
        
        // Daño en área al Pilar y jugadores cercanos
        Collider[] afectados = Physics.OverlapSphere(transform.position, radioAtaque);
        foreach (var col in afectados)
        {
            var pilar = col.GetComponent<Pilar>();
            if (pilar != null)
                pilar.RecibirDaño(dañoAlPilar);
            
            var player = col.GetComponent<PlayerController>();
            if (player != null)
                player.RecibirDaño(dañoAlJugador);
        }
        
        if (prefabOndaImpacto != null)
            Instantiate(prefabOndaImpacto, transform.position, Quaternion.identity);
        
        timerAtaque = cooldownAtaque;
        Debug.Log("[Colossus] Ataque de área!");
    }

    public override void RecibirDaño(float cantidad)
    {
        // Reducir daño de proyectiles/disparos
        float dañoFinal = cantidad * (1f - resistenciaDisparos);
        base.RecibirDaño(dañoFinal);
    }

    void OnTriggerEnter(Collider other)
    {
        // Detectar si entra en pozos o zonas de gravedad por nombre
        string nombre = other.gameObject.name;
        if (nombre.Contains("Pozo") || nombre.Contains("Gravedad"))
        {
            enZonaPeligrosa = true;
            // Muerte instantánea al caer en pozo
            if (nombre.Contains("Pozo"))
            {
                Debug.Log("[Colossus] ¡El Coloso cayó en un pozo! Muerte instantánea.");
                vidaActual = 0;
                base.Morir();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radioAtaque);
    }
}
