/**
 * Nest.cs
 * Nido/Incubadora: Objetivo estacionario que genera Corredores débiles.
 * Enemigo de presión y control.
 */
using UnityEngine;
using System.Collections.Generic;

public class Nest : Enemy
{
private const float StationaryMovementSpeedMetersPerSecond = 0f;
private const float MaximumHealth = 80f;
private const float NoPilarDamage = 0f;
private const int EnergyDropAmount = 10;
private const float RunnerHealth = 10f;
private const float RunnerSpeedMetersPerSecond = 2.5f;
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
        velocidadMovimiento = StationaryMovementSpeedMetersPerSecond; // Estacionario
        vidaMaxima = MaximumHealth;
        vidaActual = vidaMaxima;
        dañoAlPilar = NoPilarDamage;
        energiaDrop = EnergyDropAmount;
        
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
runner.vidaMaxima = RunnerHealth;
            runner.vidaActual = RunnerHealth;
            runner.velocidadMovimiento = RunnerSpeedMetersPerSecond;
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
