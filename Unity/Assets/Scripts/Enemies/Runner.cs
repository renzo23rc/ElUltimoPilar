/**
 * Runner.cs
 * Corredor: Rápido, poca vida, va directo al Pilar.
 * Prioridad de objetivo: IGNORA al jugador, solo ataca el Pilar.
 */
using UnityEngine;

public class Runner : Enemy
{
    [Header("Runner Específico")]
    public float velocidadSprint = 5f;
    public float distanciaSprint = 15f;
    
    protected override void Start()
    {
        base.Start();
        atacaJugador = false;
        velocidadMovimiento = 3.5f;
        vidaMaxima = 20f;
        vidaActual = vidaMaxima;
        dañoAlPilar = 8f;
        energiaDrop = 2;
    }

    protected override void Comportamiento()
    {
        if (pilarObjetivo == null) return;
        
        Vector3 direccion = pilarObjetivo.transform.position - transform.position;
        direccion.y = 0;
        float distancia = direccion.magnitude;
        
        // Sprint cuando está lejos
        float velocidadActual = distancia > distanciaSprint ? velocidadSprint : velocidadMovimiento;
        
        if (distancia > rangoAtaque)
        {
            MoverHacia(direccion.normalized, velocidadActual);
        }
        else
        {
            AtacarPilar();
        }
    }

    void MoverHacia(Vector3 direccion, float velocidad)
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + direccion * velocidad * Time.deltaTime);
        }
        else
        {
            transform.position += direccion * velocidad * Time.deltaTime;
        }
        
        if (direccion != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(direccion), Time.deltaTime * 10f);
    }
}
