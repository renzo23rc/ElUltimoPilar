/**
 * Nest.cs
 * Nido/Incubadora: Objetivo estacionario que genera Corredores débiles.
 * Enemigo de presión y control.
 */
using UnityEngine;
using System.Collections.Generic;

public class Nest : Enemy
{
    [Header("Nido Específico")]
    public GameObject prefabCorredor;
    public float intervaloGeneracion = 6f;
    public int maxCorredoresSimultaneos = 3;
    public float radioSpawn = 3f;
    
    private float timerGeneracion = 0f;
    private int corredoresGenerados = 0;
    private readonly List<Enemy> corredoresVivos = new List<Enemy>();

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
        if (GameManager.Instance != null && !GameManager.Instance.juegoActivo) return;
        
        timerGeneracion -= Time.deltaTime;
        
        if (timerGeneracion <= 0 && prefabCorredor != null)
        {
            // Limpiar nulos antes de chequear límite
            corredoresVivos.RemoveAll(e => e == null);
            if (corredoresVivos.Count < maxCorredoresSimultaneos)
            {
                GenerarCorredor();
            }
            timerGeneracion = intervaloGeneracion;
        }
    }

    void GenerarCorredor()
    {
        Vector3 pos = transform.position + Random.insideUnitSphere * radioSpawn;
        pos.y = transform.position.y;
        
        GameObject corredor = Instantiate(prefabCorredor, pos, Quaternion.identity);
        corredor.SetActive(true);
        corredor.name = prefabCorredor.name + "(Clone_Nido)";

        // Configurar como corredor débil
        var runner = corredor.GetComponent<Runner>();
        if (runner != null)
        {
            runner.vidaMaxima = 10f;
            runner.vidaActual = 10f;
            runner.velocidadMovimiento = 2.5f;
        }

        var enemy = corredor.GetComponent<Enemy>();
        if (enemy != null)
        {
            corredoresVivos.Add(enemy);
            // Registrar en spawner para que la oleada no termine mientras sigan vivos
            EnemySpawner.Instance?.RegistrarEnemigoExterno(enemy);
            // Liberar cupo cuando muera
            enemy.OnMuerte += () => {
                corredoresVivos.Remove(enemy);
                // El spawner ya lo quita vía EnemigoEliminado, no hace falta duplicar
            };
        }
        
        corredoresGenerados++;
        Debug.Log($"[Nest] Corredor generado ({corredoresVivos.Count}/{maxCorredoresSimultaneos}) total {corredoresGenerados}");
    }

    protected override void Morir()
    {
        // Al morir el nido, los corredores ya generados siguen vivos (presión residual)
        // pero limpiamos referencia para GC
        base.Morir();
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
