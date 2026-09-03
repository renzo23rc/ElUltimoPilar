/**
 * Artillery.cs
 * Artillero a distancia: Fijo o lento, dispara proyectiles en línea recta.
 * Pierde línea de visión cuando la arena rota o cambia.
 */
using UnityEngine;

public class Artillery : Enemy
{
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
        velocidadMovimiento = 1f;
        vidaMaxima = 40f;
        vidaActual = vidaMaxima;
        dañoAlPilar = 15f;
        dañoAlJugador = 10f;
        energiaDrop = 3;
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
            Vector3 dirPilar = (pilarObjetivo.transform.position - transform.position).normalized;
            MoverHacia(dirPilar);
            return;
        }
        
        // Determinar objetivo (más cercano: jugador o Pilar)
        Transform objetivo = SeleccionarObjetivo();
        if (objetivo == null) return;
        
        float distancia = Vector3.Distance(transform.position, objetivo.position);
        
        // Mirar al objetivo
        Vector3 dir = objetivo.position - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        
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

    void VerificarLineaVision()
    {
        // Raycast hacia el Pilar para verificar si hay obstáculos
        Vector3 dir = pilarObjetivo.transform.position - puntoDisparo.position;
        if (Physics.Raycast(puntoDisparo.position, dir.normalized, out RaycastHit hit, dir.magnitude))
        {
            tieneLineaVision = hit.collider.GetComponent<Pilar>() != null;
        }
        else
        {
            tieneLineaVision = true;
        }
    }

    Transform SeleccionarObjetivo()
    {
        // Priorizar jugador si está cerca y visible
        if (jugadorObjetivo != null)
        {
            float distJugador = Vector3.Distance(transform.position, jugadorObjetivo.transform.position);
            float distPilar = Vector3.Distance(transform.position, pilarObjetivo.transform.position);
            
            if (distJugador < distPilar && distJugador < rangoDisparo * 1.5f)
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
        
        GameObject proj = null;
        if (PoolManager.Instance != null)
        {
            // Intentar obtener del pool "Proyectil" (registrado en TestSceneSetup)
            proj = PoolManager.Instance.Get("Proyectil", puntoDisparo.position, Quaternion.LookRotation(objetivo.position - puntoDisparo.position));
            if (proj == null)
                proj = Instantiate(prefabProyectil, puntoDisparo.position, Quaternion.LookRotation(objetivo.position - puntoDisparo.position));
            else
            {
                proj.transform.SetPositionAndRotation(puntoDisparo.position, Quaternion.LookRotation(objetivo.position - puntoDisparo.position));
                proj.SetActive(true);
            }
        }
        else
        {
            proj = Instantiate(prefabProyectil, puntoDisparo.position, Quaternion.LookRotation(objetivo.position - puntoDisparo.position));
        }
        
        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = (objetivo.position - puntoDisparo.position).normalized * velocidadProyectil;
        }
        
        // Configurar daño del proyectil
        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
        {
            projComp.daño = dañoAlPilar;
            projComp.dañoJugador = dañoAlJugador;
            // Si está en pool, asegurar auto-release programado
            var pooled = proj.GetComponent<PooledObject>();
            if (pooled != null && PoolManager.Instance != null)
                pooled.ScheduleRelease(projComp.tiempoVida);
        }
    }
}
