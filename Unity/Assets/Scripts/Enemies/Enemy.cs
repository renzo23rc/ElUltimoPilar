/**
 * Enemy.cs
 * Clase base para todos los enemigos del Enjambre.
 * Gestiona vida, movimiento básico hacia el Pilar, y drops de energía.
 * 
 * Heredar de esta clase para crear tipos específicos (Runner, Artillery, etc.)
 */
using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{
    private const float TurretDiversionDistanceMeters = 8f;
    private const float TurretSearchDistanceMeters = 15f;
    private const float RotationSharpness = 10f;
    private const float DropHeightMeters = 0.5f;
    private const float DamageFlashDurationSeconds = 0.05f;
    private const float DestroyDelaySeconds = 0.1f;
    private const float DefaultSlowFactor = 1f;
    private const float NoOriginalSpeed = -1f;

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

    [Header("Ralentización (Weaver)")]
    public bool estaRalentizado = false;
private float velocidadOriginal = NoOriginalSpeed;
    private Coroutine coRalentizacion;
    private float factorRalentActual = DefaultSlowFactor;

    protected virtual void Start()
    {
        vidaActual = vidaMaxima;
        pilarObjetivo = FindFirstObjectByType<Pilar>();
        rb = GetComponent<Rigidbody>();
        
        // Jugador más cercano para cooperativo (se revalida si se pierde la referencia).
        ResolverJugadorCercano();

        // Auto-asignar modeloVisual si quedó sin asignar (evita UnassignedReference)
        if (modeloVisual == null)
            modeloVisual = transform;
    }

    protected virtual void Update()
    {
        if (estaMuerto) return;
        if (!GameManager.Instance.juegoActivo) return;
        if (jugadorObjetivo == null)
            ResolverJugadorCercano();
        
        timerAtaque -= Time.deltaTime;
        
        Comportamiento();
    }

    protected virtual void Comportamiento()
    {
        // Priorizar torreta cercana si existe (hace que cueste mantenerlas)
        var torretaCercana = BuscarTorretaCercana();
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
            if (distTorreta < TurretDiversionDistanceMeters && distTorreta < Vector3.Distance(transform.position, pilarObjetivo.transform.position))
            {
                MoverHacia(dirTorreta.normalized);
                return;
            }
        }

        // Comportamiento base: moverse hacia el Pilar
        if (pilarObjetivo == null) return;
        
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
        var torretas = FindObjectsByType<Torreta>(FindObjectsSortMode.None);
        Torreta cercana = null;
        float minDist = float.MaxValue;
        foreach (var t in torretas)
        {
            if (t == null) continue;
            float d = Vector3.Distance(transform.position, t.transform.position);
            if (d < minDist && d < TurretSearchDistanceMeters) // solo considerar cercanas
            {
                minDist = d;
                cercana = t;
            }
        }
        return cercana;
    }

protected virtual void AtacarTorreta(Torreta torreta)
    {
        if (timerAtaque > 0f) return;
        torreta.RecibirDaño(dañoAlPilar);
        timerAtaque = cooldownAtaque;
        Debug.Log($"[{GetType().Name}] Atacó a Torreta {torreta.name} por {dañoAlPilar} daño");
    }

    protected virtual void MoverHacia(Vector3 direccion)
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + direccion * velocidadMovimiento * Time.deltaTime);
        }
        else
        {
            transform.position += direccion * velocidadMovimiento * Time.deltaTime;
        }
        
        // Rotar hacia la dirección
        if (direccion != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation(direccion), Time.deltaTime * RotationSharpness);
        }
    }

protected virtual void AtacarPilar()
    {
        if (timerAtaque > 0f) return;
        
        pilarObjetivo?.RecibirDaño(dañoAlPilar);
        timerAtaque = cooldownAtaque;
        
        Debug.Log($"[{GetType().Name}] Atacó al Pilar por {dañoAlPilar} de daño");
    }

    void IDamageable.ReceiveDamage(DamageRequest request)
    {
        RecibirDaño(request.Amount);
    }

    public virtual void RecibirDaño(float cantidad)
    {
        if (estaMuerto) return;
        
        vidaActual -= cantidad;
        NotificarDañoRecibido(cantidad);
        CombatFeedback.NotifyHit(vidaActual <= 0);
        
        // Feedback visual de daño
        StartCoroutine(FlashDaño());
        
        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    protected void NotificarDañoRecibido(float cantidad)
    {
        OnDañoRecibido?.Invoke(cantidad);
    }

    protected virtual void Morir()
    {
        estaMuerto = true;
        OnMuerte?.Invoke();
        
        // Dropear energía
        DropearEnergia();
        
        // Chance de variante temporal de arma
        DropearVariante();
        
        // Notificar al spawner
        EnemySpawner.Instance?.EnemigoEliminado(this);
        
        Destroy(gameObject, DestroyDelaySeconds);
    }

    protected void ResolverJugadorCercano()
    {
        var manager = GameManager.Instance;
        if (manager != null && manager.PlayerCount > 0)
        {
            PlayerController cercano = null;
            float minDist = float.MaxValue;
            foreach (var jugador in manager.Players)
            {
                if (jugador == null) continue;
                float d = Vector3.Distance(transform.position, jugador.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    cercano = jugador;
                }
            }
            if (cercano != null)
            {
                jugadorObjetivo = cercano;
                return;
            }
        }
        jugadorObjetivo = FindFirstObjectByType<PlayerController>();
    }
    
    protected virtual void DropearEnergia()
    {
        if (prefabEnergia != null)
        {
            GameObject go = null;
            // Intentar pooling si existe PoolManager y pool registrado
            if (PoolManager.Instance != null)
            {
                // Pool key basada en nombre de prefab
                string key = "EnergyPickup";
                // Si no registrado, registrar en este momento
                // Necesitamos asegurar que prefabEnergia esté registrado como key EnergyPickup
                // Si falta, fallback a Instantiate
                try
                {
                    go = PoolManager.Instance.Get(key, transform.position + Vector3.up * DropHeightMeters, Quaternion.identity);
                    if (go == null) throw new System.Exception("Pool Get null");
                    // Resetear pickup estado
// Reactivar collider/scale que pudo quedar desactivado
                    go.transform.localScale = Vector3.one * DropHeightMeters;
                    go.SetActive(true);
                }
catch
                {
                    go = Instantiate(prefabEnergia, transform.position + Vector3.up * DropHeightMeters, Quaternion.identity);
                }
            }
            else
            {
                go = Instantiate(prefabEnergia, transform.position + Vector3.up * DropHeightMeters, Quaternion.identity);
            }
            var pickup = go.GetComponent<EnergyPickup>();
            if (pickup != null)
                pickup.cantidad = energiaDrop;
        }
    }
    
    protected virtual void DropearVariante()
    {
        if (prefabVariante == null) return;
        if (UnityEngine.Random.value > chanceDropVariante) return;
        var drop = Instantiate(prefabVariante, transform.position + Vector3.up * DropHeightMeters, Quaternion.identity);
        drop.SetActive(true);
    }

    protected System.Collections.IEnumerator FlashDaño()
    {
        // Evitar UnassignedReference con ?. en Unity fake-null: chequeo explicito
        Renderer rend = null;
        if (modeloVisual != null)
            rend = modeloVisual.GetComponent<Renderer>();
        if (rend == null)
            rend = GetComponent<Renderer>();
        if (rend == null)
            rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            // Usar material instanciado sin leak grave para flash corto
            Color original = rend.material.color;
            rend.material.color = Color.white;
            yield return new WaitForSeconds(DamageFlashDurationSeconds);
            // Puede haberse destruido mientras esperaba
            if (rend != null)
                rend.material.color = original;
        }
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
        var torretaCol = collision.gameObject.GetComponent<Torreta>();
        if (torretaCol == null) torretaCol = collision.gameObject.GetComponentInParent<Torreta>();
        if (torretaCol != null && timerAtaque <= 0)
        {
            torretaCol.RecibirDaño(dañoAlPilar);
            timerAtaque = cooldownAtaque;
        }
    }

    // ===== SISTEMA DE RALENTIZACIÓN (stack prohibido, temporal) =====
    public void AplicarRalentizacion(float factor, float duracion)
    {
        if (estaMuerto) return;
        // Stack prohibido: si ya está ralentizado, no acumular multiplicadores
        if (estaRalentizado)
        {
            // Solo refrescar duración sin multiplicar de nuevo
            if (coRalentizacion != null) StopCoroutine(coRalentizacion);
            coRalentizacion = StartCoroutine(RutinaRalentizacion(factor, duracion));
            return;
        }
        velocidadOriginal = velocidadMovimiento;
        factorRalentActual = factor;
        velocidadMovimiento = velocidadOriginal * factor;
        estaRalentizado = true;
        Debug.Log($"[{GetType().Name}] Ralentizado x{factor} por {duracion}s (vel {velocidadOriginal:F1}->{velocidadMovimiento:F1})");
        coRalentizacion = StartCoroutine(RutinaRalentizacion(factor, duracion));
    }

    public void QuitarRalentizacion()
    {
        if (!estaRalentizado) return;
        if (coRalentizacion != null) StopCoroutine(coRalentizacion);
        coRalentizacion = null;
        velocidadMovimiento = velocidadOriginal;
        estaRalentizado = false;
        factorRalentActual = 1f;
        velocidadOriginal = -1f;
        Debug.Log($"[{GetType().Name}] Ralentización removida, vel restaurada a {velocidadMovimiento:F1}");
    }

    System.Collections.IEnumerator RutinaRalentizacion(float factor, float duracion)
    {
        yield return new WaitForSeconds(duracion);
        if (estaMuerto) yield break;
        // Solo restaurar si aún está ralentizado con ese factor
        if (estaRalentizado)
        {
            velocidadMovimiento = velocidadOriginal;
            estaRalentizado = false;
            factorRalentActual = DefaultSlowFactor;
            velocidadOriginal = NoOriginalSpeed;
            coRalentizacion = null;
            Debug.Log($"[{GetType().Name}] Ralentización expirada");
        }
    }

    protected virtual void OnDestroy()
    {
        if (coRalentizacion != null) StopCoroutine(coRalentizacion);
    }
}
