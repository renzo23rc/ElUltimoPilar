/**
 * Nest.cs
 * Nido/Incubadora: Objetivo estacionario que genera Corredores débiles.
 * Enemigo de presión y control.
 */
using UnityEngine;

public class Nest : Enemy
{
    [Header("Nido Específico")]
    public GameObject prefabCorredor;
    public float intervaloGeneracion = 6f;
    public int maxCorredoresSimultaneos = 3;
    public float radioSpawn = 3f;
    
    private float timerGeneracion = 0f;
    private int corredoresGenerados = 0;

    protected override void Start()
    {
        base.Start();
        atacaJugador = false;
        velocidadMovimiento = 0f; // Estacionario
        vidaMaxima = 80f;
        vidaActual = vidaMaxima;
        dañoAlPilar = 0f;
        energiaDrop = 10;
        
        // No se mueve
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    protected override void Update()
    {
        if (estaMuerto) return;
        if (!GameManager.Instance.juegoActivo) return;
        
        timerGeneracion -= Time.deltaTime;
        
        if (timerGeneracion <= 0 && prefabCorredor != null)
        {
            GenerarCorredor();
            timerGeneracion = intervaloGeneracion;
        }
    }

    void GenerarCorredor()
    {
        Vector3 pos = transform.position + Random.insideUnitSphere * radioSpawn;
        pos.y = transform.position.y;
        
        GameObject corredor = Instantiate(prefabCorredor, pos, Quaternion.identity);
        
        // Configurar como corredor débil
        var runner = corredor.GetComponent<Runner>();
        if (runner != null)
        {
            runner.vidaMaxima = 10f;
            runner.vidaActual = 10f;
            runner.velocidadMovimiento = 6f;
        }
        
        corredoresGenerados++;
        Debug.Log("[Nest] Corredor generado");
    }

    protected override void Comportamiento()
    {
        // No se mueve, solo genera
    }

    protected override void MoverHacia(Vector3 direccion)
    {
        // No se mueve
    }
}
