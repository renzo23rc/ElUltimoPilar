/**
 * Runner.cs
 * Corredor: Rápido, poca vida, va directo al Pilar.
 * Prioridad de objetivo: IGNORA al jugador, solo ataca el Pilar.
 */
using UnityEngine;

public class Runner : Enemy
{
    private const float MovementSpeedMetersPerSecond = 3.5f;
    private const float MaximumHealth = 20f;
    private const float PilarDamage = 3f;
    private const int EnergyDropAmount = 2;

    [Header("Runner Específico")]
    public float velocidadSprint = 5f;
    public float distanciaSprint = 15f;

    private bool hasHatchlingStats;
    private float hatchlingHealth;
    private float hatchlingSpeedMetersPerSecond;
    private bool started;

    protected override void Start()
    {
        base.Start();
        atacaJugador = false;
        velocidadMovimiento = MovementSpeedMetersPerSecond;
        vidaMaxima = MaximumHealth;
        vidaActual = vidaMaxima;
        dañoAlPilar = PilarDamage;
        energiaDrop = EnergyDropAmount;
        started = true;
        ApplyHatchlingStats();
    }

    /// <summary>Configures this runner as a weak hatchling; the stats survive the type defaults applied in Start.</summary>
    /// <param name="health">The hatchling maximum health.</param>
    /// <param name="speedMetersPerSecond">The hatchling movement speed.</param>
    public void ConfigureAsHatchling(float health, float speedMetersPerSecond)
    {
        hasHatchlingStats = true;
        hatchlingHealth = health;
        hatchlingSpeedMetersPerSecond = speedMetersPerSecond;
        if (started)
        {
            ApplyHatchlingStats();
        }
    }

    private void ApplyHatchlingStats()
    {
        if (!hasHatchlingStats)
        {
            return;
        }

        vidaMaxima = hatchlingHealth;
        vidaActual = hatchlingHealth;
        velocidadMovimiento = hatchlingSpeedMetersPerSecond;
    }

    // Siempre camina o corre: nunca queda en Idle.
    protected override bool UsesIdlePose => false;

    protected override void Comportamiento()
    {
        if (pilarObjetivo == null)
        {
            return;
        }

        Vector3 direccion = OffsetToPilar();
        float distancia = direccion.magnitude;

        if (distancia > rangoAtaque)
        {
            // El Corredor de oleada corre todo el trayecto. La cría del Nido conserva su velocidad corta.
            bool sprinting = !hasHatchlingStats || distancia > distanciaSprint;
            float velocidadBase = sprinting ? velocidadSprint : velocidadMovimiento;
            AnimationDriver.SetRun(true);
            MoverHacia(direccion.normalized, velocidadBase * SlowFactor);
        }
        else
        {
            AnimationDriver.SetRun(false);
            AtacarPilar();
        }
    }
}
