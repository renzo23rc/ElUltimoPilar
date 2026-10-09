/**
 * Artillery.cs
 * Artillero a distancia: Fijo o lento, dispara proyectiles en línea recta.
 * Pierde línea de visión cuando la arena rota o cambia.
 */
using UnityEngine;

public class Artillery : Enemy
{
    private const float DefaultMovementSpeedMetersPerSecond = 1f;
    private const float DefaultHealth = 40f;
    private const float PilarDamage = 6f;
    private const float PlayerDamage = 4f;
    private const int EnergyDropAmount = 3;
    private const float PlayerTargetRangeMultiplier = 1.5f;
    private const float RotationSharpness = 5f;
    private const string ProjectilePoolKey = "Proyectil";

    [Header("Artillero Específico")]
    public GameObject prefabProyectil;
    public Transform puntoDisparo;
    public float rangoDisparo = 20f;
    public float velocidadProyectil = 15f;
    public float cadenciaDisparo = 2f;
    
    private float timerDisparo = 0f;
    private bool tieneLineaVision = true;

    protected override void Start()
    {
        base.Start();
        atacaJugador = true; // Puede atacar al jugador o al Pilar
        velocidadMovimiento = DefaultMovementSpeedMetersPerSecond;
        vidaMaxima = DefaultHealth;
        vidaActual = vidaMaxima;
        dañoAlPilar = PilarDamage;
        dañoAlJugador = PlayerDamage;
        energiaDrop = EnergyDropAmount;
        rangoAtaque = rangoDisparo;
        
        if (puntoDisparo == null)
            puntoDisparo = transform;
    }

    protected override void Comportamiento()
    {
        if (pilarObjetivo == null) return;
        
        // Verificar línea de visión
        VerificarLineaVision();
        
        if (!tieneLineaVision)
        {
            // Moverse para reposicionarse si no tiene línea de visión
            MoverHacia(OffsetToPilar().normalized);
            return;
        }
        
        // Determinar objetivo (más cercano: jugador o Pilar)
        Transform objetivo = SeleccionarObjetivo();
        if (objetivo == null) return;
        
        float distancia = Vector3.Distance(transform.position, AimPositionOf(objetivo));
        
        // Mirar al objetivo
        Vector3 dir = AimPositionOf(objetivo) - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(dir), Time.deltaTime * RotationSharpness);
        
        if (distancia <= rangoDisparo)
        {
            // Disparar
            timerDisparo -= Time.deltaTime;
            if (timerDisparo <= 0)
            {
                Disparar(objetivo);
                timerDisparo = cadenciaDisparo;
            }
        }
        else
        {
            // Acercarse
            MoverHacia(dir.normalized);
        }
    }

    // El artillero solo gira sobre Y: cualquier inclinación (física, empujes) se descarta.
    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void VerificarLineaVision()
    {
        // Raycast hacia el Pilar para verificar si hay obstáculos
        Vector3 dir = pilarObjetivo.AimPoint - puntoDisparo.position;
        if (Physics.Raycast(puntoDisparo.position, dir.normalized, out RaycastHit hit, dir.magnitude))
        {
            tieneLineaVision = hit.collider.GetComponentInParent<Pilar>() != null;
        }
        else
        {
            tieneLineaVision = true;
        }
    }

    // El Pilar se apunta en su eje, no en el pivote del modelo (que puede quedar bajo el suelo).
    Vector3 AimPositionOf(Transform objetivo)
    {
        return objetivo.GetComponent<Pilar>() != null ? pilarObjetivo.AimPoint : objetivo.position;
    }

    Transform SeleccionarObjetivo()
    {
        // Priorizar jugador si está cerca y visible
        if (jugadorObjetivo != null)
        {
            float distJugador = Vector3.Distance(transform.position, jugadorObjetivo.transform.position);
            float distPilar = Vector3.Distance(transform.position, pilarObjetivo.AimPoint);
            
            if (distJugador < distPilar && distJugador < rangoDisparo * PlayerTargetRangeMultiplier)
                return jugadorObjetivo.transform;
        }
        return pilarObjetivo.transform;
    }

    void Disparar(Transform objetivo)
    {
        if (prefabProyectil == null)
        {
            // Daño directo si no hay prefab
            if (objetivo.GetComponent<Pilar>() != null)
                pilarObjetivo.RecibirDaño(dañoAlPilar);
            else if (objetivo.GetComponent<PlayerController>() != null)
                jugadorObjetivo.RecibirDaño(dañoAlJugador);
            return;
        }
        
        Vector3 origen = puntoDisparo.position;
        Vector3 destino = AimPositionOf(objetivo);
        Quaternion rotacion = Quaternion.LookRotation(destino - origen);
        GameObject proj = PoolManager.Spawn(ProjectilePoolKey, prefabProyectil, origen, rotacion);

        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = (destino - puntoDisparo.position).normalized * velocidadProyectil;
        }

        // El proyectil daña al Pilar y a los jugadores con los valores de este artillero.
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
        {
            projComp.ConfigurarDaño(dañoAlPilar, dañoAlJugador);
        }
    }
}
