/**
 * Colossus.cs
 * Coloso (mini-jefe de oleada tardía): Lento, mucha vida, resistente a disparos directos.
 * La única forma viable es empujarlo a pozos o zonas de gravedad.
 */
using UltimoPilar.Core.Shared;
using UnityEngine;

public class Colossus : Enemy
{
    private const float MovementSpeedMetersPerSecond = 0.9f;
    private const float MaximumHealth = 180f;
    private const float PilarDamage = 22f;
    private const float PlayerDamage = 18f;
    private const int EnergyDropAmount = 20;

    [Header("Coloso Específico")]
    [Range(0f, 0.9f)] public float resistenciaDisparos = 0.55f; // Reduce 55% del daño (antes 80% -> inmortal)
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
        if (pilarObjetivo == null)
        {
            return;
        }

        Vector3 direccion = pilarObjetivo.transform.position - transform.position;
        direccion.y = 0;
        float distancia = direccion.magnitude;

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
        if (timerAtaque > 0)
        {
            return;
        }

        // Daño en área al Pilar y jugadores cercanos, una vez por objetivo.
        foreach (Pilar pilar in OverlapQuery.FindUniqueInSphere<Pilar>(transform.position, radioAtaque))
        {
            pilar.RecibirDaño(dañoAlPilar);
        }

        foreach (PlayerController player in OverlapQuery.FindUniqueInSphere<PlayerController>(transform.position, radioAtaque))
        {
            player.RecibirDaño(dañoAlJugador);
        }

        if (prefabOndaImpacto != null)
        {
            Instantiate(prefabOndaImpacto, transform.position, Quaternion.identity);
        }

        timerAtaque = cooldownAtaque;
    }

    public override void RecibirDaño(float cantidad)
    {
        // Reducir daño de proyectiles/disparos
        float dañoFinal = cantidad * (1f - resistenciaDisparos);
        base.RecibirDaño(dañoFinal);
    }

    void OnTriggerEnter(Collider other)
    {
        bool esPozo = other.GetComponentInParent<PozoKill>() != null;
        bool esGravedad = other.GetComponentInParent<ZonaGravedadEffect>() != null;
        if (!esPozo && !esGravedad)
        {
            return;
        }

        enZonaPeligrosa = true;
        if (esPozo)
        {
            // Caer al pozo ignora la resistencia y conserva la recompensa.
            KillInstantly();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radioAtaque);
    }
}
