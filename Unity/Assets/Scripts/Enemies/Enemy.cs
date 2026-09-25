/**
 * Enemy.cs
 * Clase base para todos los enemigos del Enjambre.
 * Gestiona vida, movimiento básico hacia el Pilar, y drops de energía.
 *
 * Heredar de esta clase para crear tipos específicos (Runner, Artillery, etc.)
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UltimoPilar.Core.Combat;
using UltimoPilar.Core.Shared;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{
    private const float TurretDiversionDistanceMeters = 8f;
    private const float TurretSearchDistanceMeters = 15f;
    private const float RotationSharpness = 10f;
    private const float DropHeightMeters = 0.5f;
    private const float DamageFlashDurationSeconds = 0.05f;
    private const float DestroyDelaySeconds = 0.1f;
    private const string EnergyPickupPoolKey = "EnergyPickup";

    private static readonly List<Enemy> ActiveEnemies = new List<Enemy>();
    private static readonly object UnattributedSlowdownSource = new object();

    [Header("Estadísticas Base")]
    public float vidaMaxima = 30f;
    public float vidaActual = 30f;
    public float velocidadMovimiento = 2.5f;
    public float dañoAlPilar = 10f;
    public float dañoAlJugador = 15f;
    public int energiaDrop = 2;

    [Header("Variante temporal")]
    public GameObject prefabVariante;
    [Range(0f, 1f)] public float chanceDropVariante = 0.08f;

    [Header("Comportamiento")]
    public bool atacaJugador = false; // Si es false, va directo al Pilar
    public float rangoAtaque = 2f;
    public float cooldownAtaque = 1f;

    [Header("Referencias")]
    public Transform modeloVisual;
    public GameObject prefabEnergia;

    // Eventos
    public event Action OnMuerte;
    public event Action<float> OnDañoRecibido;

    protected Pilar pilarObjetivo;
    protected PlayerController jugadorObjetivo;
    protected float timerAtaque = 0f;
    protected bool estaMuerto = false;
    protected Rigidbody rb;

    [Header("Ralentización")]
    public bool estaRalentizado = false;

    private readonly SlowdownTracker slowdowns = new SlowdownTracker();
    private float knockbackRemainingSeconds;
    private Renderer flashRenderer;
    private Coroutine flashCoroutine;

    /// <summary>Gets the enemies that are currently enabled in the scene.</summary>
    public static IReadOnlyList<Enemy> Active => ActiveEnemies;

    /// <summary>Gets whether the enemy has died.</summary>
    public bool EstaMuerto => estaMuerto;

    /// <summary>Gets the active speed multiplier applied by slowdowns.</summary>
    protected float SlowFactor => slowdowns.GetFactor(Time.time);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        ActiveEnemies.Clear();
    }

    protected virtual void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }
    }

    protected virtual void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    protected virtual void Start()
    {
        vidaActual = vidaMaxima;
        pilarObjetivo = FindFirstObjectByType<Pilar>();
        rb = GetComponent<Rigidbody>();

        // Jugador más cercano para cooperativo (se revalida si se pierde la referencia).
        ResolverJugadorCercano();

        // Auto-asignar modeloVisual si quedó sin asignar (evita UnassignedReference)
        if (modeloVisual == null)
        {
            modeloVisual = transform;
        }

        EnsureHealthBar();
    }

    private void EnsureHealthBar()
    {
        if (GetComponentInChildren<EnemyHealthBar>(true) != null)
        {
            return;
        }

        var barObject = new GameObject("HealthBar");
        barObject.transform.SetParent(transform, false);
        barObject.transform.localPosition = Vector3.zero;
        barObject.transform.localRotation = Quaternion.identity;
        barObject.AddComponent<EnemyHealthBar>();
    }

    protected virtual void Update()
    {
        if (estaMuerto)
        {
            return;
        }

        if (!IsMatchRunning())
        {
            return;
        }

        if (jugadorObjetivo == null)
        {
            ResolverJugadorCercano();
        }

        estaRalentizado = slowdowns.IsActive(Time.time);
        timerAtaque -= Time.deltaTime;

        // Durante el empuje la física controla el movimiento.
        if (knockbackRemainingSeconds > 0f)
        {
            knockbackRemainingSeconds -= Time.deltaTime;
            return;
        }

        if (DecoyBeacon.TryGetAttractingDecoy(transform.position, out Vector3 decoyPosition))
        {
            MoverHaciaSeñuelo(decoyPosition);
            return;
        }

        Comportamiento();
    }

    /// <summary>Gets whether gameplay should advance; without a manager the enemy runs standalone.</summary>
    protected static bool IsMatchRunning()
    {
        GameManager manager = GameManager.Instance;
        return manager == null || (manager.juegoActivo && !manager.juegoPausado);
    }

    /// <summary>
    /// Pushes the enemy with a physics velocity change and pauses its behavior while it recovers.
    /// </summary>
    /// <param name="velocityChange">The velocity change to apply.</param>
    /// <param name="recoverySeconds">The time during which behavior is paused.</param>
    public void ApplyKnockback(Vector3 velocityChange, float recoverySeconds)
    {
        if (estaMuerto)
        {
            return;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        // Enemigos estacionarios (kinematic) o sin física no se empujan.
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
        knockbackRemainingSeconds = Mathf.Max(knockbackRemainingSeconds, recoverySeconds);
    }

    private void MoverHaciaSeñuelo(Vector3 decoyPosition)
    {
        Vector3 direccion = decoyPosition - transform.position;
        direccion.y = 0;
        if (direccion.sqrMagnitude < Mathf.Epsilon)
        {
            return;
        }

        MoverHacia(direccion.normalized);
    }

    protected virtual void Comportamiento()
    {
        if (pilarObjetivo == null)
        {
            return;
        }

        // Priorizar torreta cercana si existe (hace que cueste mantenerlas)
        Torreta torretaCercana = BuscarTorretaCercana();
        if (torretaCercana != null)
        {
            Vector3 dirTorreta = torretaCercana.transform.position - transform.position;
            dirTorreta.y = 0;
            float distTorreta = dirTorreta.magnitude;
            if (distTorreta <= rangoAtaque)
            {
                AtacarTorreta(torretaCercana);
                return;
            }

            // Si torreta está a mitad de camino hacia pilar y cerca, desviarse
            if (distTorreta < TurretDiversionDistanceMeters
                && distTorreta < Vector3.Distance(transform.position, pilarObjetivo.transform.position))
            {
                MoverHacia(dirTorreta.normalized);
                return;
            }
        }

        // Comportamiento base: moverse hacia el Pilar
        Vector3 direccion = pilarObjetivo.transform.position - transform.position;
        direccion.y = 0;
        float distancia = direccion.magnitude;

        if (distancia > rangoAtaque)
        {
            MoverHacia(direccion.normalized);
        }
        else
        {
            AtacarPilar();
        }
    }

    Torreta BuscarTorretaCercana()
    {
        Torreta[] torretas = FindObjectsByType<Torreta>(FindObjectsSortMode.None);
        Torreta cercana = null;
        float minDist = TurretSearchDistanceMeters;
        foreach (Torreta torreta in torretas)
        {
            if (torreta == null)
            {
                continue;
            }

            float distancia = Vector3.Distance(transform.position, torreta.transform.position);
            if (distancia < minDist)
            {
                minDist = distancia;
                cercana = torreta;
            }
        }

        return cercana;
    }

    protected virtual void AtacarTorreta(Torreta torreta)
    {
        if (timerAtaque > 0f)
        {
            return;
        }

        torreta.RecibirDaño(dañoAlPilar);
        timerAtaque = cooldownAtaque;
    }

    /// <summary>Moves at the configured speed, already reduced by active slowdowns.</summary>
    protected virtual void MoverHacia(Vector3 direccion)
    {
        MoverHacia(direccion, velocidadMovimiento * SlowFactor);
    }

    /// <summary>Moves at an explicit speed and rotates toward the movement direction.</summary>
    protected void MoverHacia(Vector3 direccion, float velocidad)
    {
        Vector3 desplazamiento = direccion * velocidad * Time.deltaTime;
        if (rb != null)
        {
            rb.MovePosition(rb.position + desplazamiento);
        }
        else
        {
            transform.position += desplazamiento;
        }

        if (direccion != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direccion),
                Time.deltaTime * RotationSharpness);
        }
    }

    protected virtual void AtacarPilar()
    {
        if (timerAtaque > 0f)
        {
            return;
        }

        if (pilarObjetivo != null)
        {
            pilarObjetivo.RecibirDaño(dañoAlPilar);
        }

        timerAtaque = cooldownAtaque;
    }

    void IDamageable.ReceiveDamage(DamageRequest request)
    {
        RecibirDaño(request.Amount);
    }

    public virtual void RecibirDaño(float cantidad)
    {
        if (estaMuerto)
        {
            return;
        }

        var request = new DamageRequest(cantidad);
        float healthBeforeDamage = vidaActual;
        vidaActual -= request.Amount;
        bool isLethal = CombatFeedback.IsLethalDamage(request, healthBeforeDamage);
        NotificarDañoRecibido(request.Amount);
        CombatFeedback.NotifyHit(isLethal);
        IniciarFlashDaño();

        if (isLethal)
        {
            Morir();
        }
    }

    /// <summary>Kills the enemy ignoring resistances, keeping the normal death rewards.</summary>
    public void KillInstantly()
    {
        if (estaMuerto)
        {
            return;
        }

        vidaActual = 0f;
        Morir();
    }

    protected void NotificarDañoRecibido(float cantidad)
    {
        OnDañoRecibido?.Invoke(cantidad);
    }

    protected virtual void Morir()
    {
        estaMuerto = true;
        OnMuerte?.Invoke();

        DropearEnergia();
        DropearVariante();
        EnemySpawner.Instance?.EnemigoEliminado(this);

        Destroy(gameObject, DestroyDelaySeconds);
    }

    protected void ResolverJugadorCercano()
    {
        jugadorObjetivo = PlayerLocator.FindClosestRegistered(transform.position);
    }

    protected virtual void DropearEnergia()
    {
        if (prefabEnergia == null)
        {
            return;
        }

        Vector3 posicion = transform.position + Vector3.up * DropHeightMeters;
        GameObject drop = PoolManager.Instance != null
            ? PoolManager.Instance.Get(EnergyPickupPoolKey, posicion, Quaternion.identity)
            : null;
        if (drop == null)
        {
            drop = Instantiate(prefabEnergia, posicion, Quaternion.identity);
        }

        drop.SetActive(true);
        if (drop.TryGetComponent(out EnergyPickup pickup))
        {
            pickup.cantidad = energiaDrop;
        }
    }

    protected virtual void DropearVariante()
    {
        if (prefabVariante == null)
        {
            return;
        }

        if (UnityEngine.Random.value > chanceDropVariante)
        {
            return;
        }

        GameObject drop = Instantiate(prefabVariante, transform.position + Vector3.up * DropHeightMeters, Quaternion.identity);
        drop.SetActive(true);
    }

    /// <summary>Starts the damage flash, restarting it when a previous flash is still running.</summary>
    protected void IniciarFlashDaño()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        DetenerFlashDaño();
        flashCoroutine = StartCoroutine(FlashDaño());
    }

    /// <summary>Stops the damage flash and shows the material color again.</summary>
    protected void DetenerFlashDaño()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        RendererFlash.Clear(flashRenderer);
    }

    protected IEnumerator FlashDaño()
    {
        flashRenderer = ResolveFlashRenderer();
        if (flashRenderer == null)
        {
            yield break;
        }

        // El flash usa un property block: nunca pisa el color del material.
        RendererFlash.Apply(flashRenderer, Color.white);
        yield return new WaitForSeconds(DamageFlashDurationSeconds);
        RendererFlash.Clear(flashRenderer);
        flashCoroutine = null;
    }

    private Renderer ResolveFlashRenderer()
    {
        if (flashRenderer != null)
        {
            return flashRenderer;
        }

        Renderer resolved = modeloVisual != null ? modeloVisual.GetComponent<Renderer>() : null;
        if (resolved == null)
        {
            resolved = GetComponentInChildren<Renderer>();
        }

        return resolved;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null && timerAtaque <= 0f)
            {
                player.RecibirDaño(dañoAlJugador);
                timerAtaque = cooldownAtaque;
            }
        }

        Torreta torreta = collision.gameObject.GetComponentInParent<Torreta>();
        if (torreta != null && timerAtaque <= 0f)
        {
            torreta.RecibirDaño(dañoAlPilar);
            timerAtaque = cooldownAtaque;
        }
    }

    // ===== RALENTIZACIÓN (sin stack: gana la más fuerte, cada fuente se gestiona por separado) =====

    /// <summary>Applies an unattributed slowdown; reapplying refreshes it.</summary>
    public void AplicarRalentizacion(float factor, float duracion)
    {
        AplicarRalentizacion(UnattributedSlowdownSource, factor, duracion);
    }

    /// <summary>Applies or refreshes the slowdown owned by a source.</summary>
    /// <param name="fuente">The owner of the slowdown, such as a zone or an ability.</param>
    /// <param name="factor">The speed multiplier between 0 and 1.</param>
    /// <param name="duracion">The duration in seconds.</param>
    public void AplicarRalentizacion(object fuente, float factor, float duracion)
    {
        if (estaMuerto)
        {
            return;
        }

        slowdowns.Apply(fuente, factor, duracion, Time.time);
        estaRalentizado = slowdowns.IsActive(Time.time);
    }

    /// <summary>Removes every active slowdown.</summary>
    public void QuitarRalentizacion()
    {
        slowdowns.Clear();
        estaRalentizado = false;
    }

    /// <summary>Removes only the slowdown owned by a source, keeping the others.</summary>
    /// <param name="fuente">The owner of the slowdown.</param>
    public void QuitarRalentizacion(object fuente)
    {
        slowdowns.Remove(fuente);
        estaRalentizado = slowdowns.IsActive(Time.time);
    }

    protected virtual void OnDestroy()
    {
        ActiveEnemies.Remove(this);
    }
}
