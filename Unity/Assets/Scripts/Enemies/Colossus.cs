/**
 * Colossus.cs
 * Coloso (mini-jefe de oleada tardía): Lento, mucha vida, resistente a disparos directos.
 * La única forma viable es empujarlo a pozos o zonas de gravedad.
 */
using UnityEngine;

public class Colossus : Enemy
{
private const float MovementSpeedMetersPerSecond = 0.9f;
private const float MaximumHealth = 300f;
private const float PilarDamage = 25f;
private const float PlayerDamage = 20f;
private const int EnergyDropAmount = 20;
    [Header("Coloso Específico")]
    public float resistenciaDisparos = 0.8f; // Reduce 80% del daño de disparos
    public float dañoEmpuje = 30f;
    public float radioAtaque = 4f;
    public GameObject prefabOndaImpacto;
    
    [HideInInspector] public bool enZonaPeligrosa = false; // Usado por PozoKill/ZonaGravedad para debug

    protected override void Start()
    {
        base.Start();
        atacaJugador = true;
        velocidadMovimiento = MovementSpeedMetersPerSecond;
        vidaMaxima = MaximumHealth;
        vidaActual = vidaMaxima;
        dañoAlPilar = PilarDamage;
        dañoAlJugador = PlayerDamage;
        energiaDrop = EnergyDropAmount;
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
        // Detectar pozo vía componente PozoKill (robusto) + fallback por nombre (compatibilidad)
        var pozo = other.GetComponent<PozoKill>();
        if (pozo == null) pozo = other.GetComponentInParent<PozoKill>();
        bool esPozo = pozo != null || other.gameObject.name.Contains("Pozo");
        bool esGravedad = other.gameObject.name.Contains("Gravedad");

        if (esPozo || esGravedad)
        {
            enZonaPeligrosa = true;
            if (esPozo)
            {
                Debug.Log("[Colossus] ¡El Coloso cayó en un pozo! Muerte instantánea.");
                // Bypass resistencia: vida 0 y morir directo con recompensa
                vidaActual = 0;
                // base.Morir() ya dropea energía y notifica spawner
                if (!estaMuerto) base.Morir();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radioAtaque);
    }
}
