/**
 * Torreta.cs
 * Sistema de torreta del protocolo de emergencia (Fase 4 del Pilar).
 * Aparece una única vez en fase 4, busca enemigos cercanos, dispara con cadencia configurable
 * con proyectil físico y se desactiva al terminar la partida.
 *
 * Balance v2: menos daño (6), más vida (120, cuesta matar), munición limitada 15 cada 10s.
 * Colocar en el prefab de torreta instanciado por Pilar.ActivarTorretas().
 * Funciona sin prefab serializado: Pilar crea fallback procedural si prefabTorreta es null.
 */
using System.Collections;
using UltimoPilar.Core.Shared;
using UnityEngine;

public class Torreta : MonoBehaviour
{
    private const float RotationSmoothing = 8f;
    private const float MinimumAimDirectionSqr = 0.01f;
    private const float TargetAimHeightMeters = 0.5f;
    private const float RetargetIntervalSeconds = 0.2f;
    private const float ReloadDimmedColorRatio = 0.3f;
    private const float ReloadPulseSpeed = 3f;
    private const float ReloadLightPulseSpeed = 2f;
    private const float ReloadLightBaseIntensity = 0.5f;
    private const float ReloadLightPulseRange = 0.5f;
    private const float ActiveLightIntensity = 2f;
    private const float FullPulseCycleSeconds = 1f;
    private const float SpawnPointForwardMeters = 0.8f;
    private const float SpawnPointHeightMeters = 0.5f;
    private const float ProjectileLifetimeSeconds = 4f;
    private const float ProjectileScale = 0.6f;
    private const float ProjectileColliderRadiusMeters = 0.5f;
    private const float ProjectileEmissionMultiplier = 1.2f;
    private const float ProjectileTrailSeconds = 0.4f;
    private const float ProjectileTrailStartWidthMeters = 0.25f;
    private const float ProjectileTrailEndWidthMeters = 0.05f;
    private const float ProjectileLightRangeMeters = 4f;
    private const float ProjectileLightIntensity = 2f;
    private const float NoPlayerDamage = 0f;
    private const float DamageFlashSeconds = 0.07f;
    private const float DestroyedColorRatio = 0.6f;
    private const float DestroyDelaySeconds = 2f;
    private const float GizmoAimLineMeters = 2f;
    private const float GizmoLabelHeightMeters = 2.5f;
    private const string ProjectilePoolKey = "Proyectil";
    private const string SpawnPointName = "PuntoDisparo";
    private const string FallbackProjectileName = "ProyectilTorreta";
    private static readonly Color ProjectileColor = new Color(1f, 0.5f, 0f);
    private static readonly Color ProjectileTrailEndColor = new Color(1f, 0.5f, 0f, 0.2f);

    [Header("Torreta - Configuración")]
    public float rango = 22f;
    public float cadencia = 0.9f; // intervalo entre disparos en segundos
    public float daño = 6f; // rebalanceado: 15 -> 6
    public float velocidadProyectil = 28f;
    public Transform puntoDisparo;
    public GameObject prefabProyectil;
    public LayerMask capaObstaculos; // opcional para línea de visión

    [Header("Vida - Resistencia (cuesta matar)")]
    public float vidaMaxima = 120f;
    public float vidaActual = 120f;
    public bool destruible = true;
    private bool estaDestruida = false;

    [Header("Munición limitada (cada 10s)")]
    public int municionMaxima = 15;
    public int municionActual = 15;
    public float tiempoRecarga = 10f;
    private float timerRecarga = 0f;
    private bool recargando = false;

    [Header("Referencias")]
    public Transform parteRotatoria; // opcional, si no se asigna usa transform

    private float timerDisparo = 0f;
    private float timerBusqueda = 0f;
    private bool activa = true;
    private Enemy objetivoActual;
    private Renderer rendCache;
    private Light lightCache;
    private Color colorBase;
    private BoxCollider colCache;
    private Coroutine flashCoroutine;
    private GameManager managerSuscrito;

    void Start()
    {
        if (puntoDisparo == null)
        {
            puntoDisparo = transform.Find(SpawnPointName);
            if (puntoDisparo == null)
            {
                var go = new GameObject(SpawnPointName);
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.forward * SpawnPointForwardMeters + Vector3.up * SpawnPointHeightMeters;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                puntoDisparo = go.transform;
            }
        }

        if (parteRotatoria == null) parteRotatoria = transform;

        vidaActual = vidaMaxima;
        municionActual = municionMaxima;
        timerRecarga = 0f;
        recargando = false;

        // Asegurar collider para recibir daño.
        colCache = GetComponent<BoxCollider>();
        if (colCache == null)
        {
            colCache = gameObject.AddComponent<BoxCollider>();
            colCache.center = Vector3.zero;
            colCache.size = Vector3.one;
        }

        colCache.isTrigger = false;

        rendCache = GetComponentInChildren<Renderer>();
        if (rendCache != null) colorBase = GetColor(rendCache.material);
        lightCache = GetComponentInChildren<Light>();

        // Suscribirse a fin de partida para desactivarse.
        managerSuscrito = GameManager.Instance;
        if (managerSuscrito != null)
        {
            managerSuscrito.OnVictoria += Desactivar;
            managerSuscrito.OnDerrota += Desactivar;
        }
    }

    void OnDestroy()
    {
        if (managerSuscrito != null)
        {
            managerSuscrito.OnVictoria -= Desactivar;
            managerSuscrito.OnDerrota -= Desactivar;
        }
    }

    void Update()
    {
        if (!activa || estaDestruida) return;

        Enemy objetivo = ActualizarObjetivo();
        if (recargando)
        {
            ActualizarRecarga();
            if (recargando)
            {
                // Durante la recarga no dispara, pero sigue apuntando.
                if (objetivo != null) RotarHacia(objetivo);
                return;
            }
        }

        timerDisparo -= Time.deltaTime;
        if (objetivo == null) return;

        RotarHacia(objetivo);
        if (timerDisparo > 0f) return;

        if (municionActual <= 0)
        {
            IniciarRecarga();
            return;
        }

        if (TieneLineaVision(objetivo))
        {
            Disparar(objetivo);
            timerDisparo = cadencia;
        }
    }

    Enemy ActualizarObjetivo()
    {
        // Buscar objetivo a intervalos: el barrido con raycasts por enemigo es caro para hacerlo cada frame.
        timerBusqueda -= Time.deltaTime;
        bool objetivoInvalido = objetivoActual == null || objetivoActual.EstaMuerto;
        if (timerBusqueda <= 0f || objetivoInvalido)
        {
            timerBusqueda = RetargetIntervalSeconds;
            objetivoActual = BuscarEnemigoMasCercano();
        }

        return objetivoActual;
    }

    void ActualizarRecarga()
    {
        timerRecarga -= Time.deltaTime;
        if (timerRecarga > 0f)
        {
            // Parpadeo visual durante la recarga.
            if (rendCache != null)
            {
                float pulse = Mathf.PingPong(Time.time * ReloadPulseSpeed, FullPulseCycleSeconds);
                SetColor(rendCache.material, Color.Lerp(colorBase * ReloadDimmedColorRatio, colorBase, pulse));
            }

            if (lightCache != null)
                lightCache.intensity = ReloadLightBaseIntensity + Mathf.PingPong(Time.time * ReloadLightPulseSpeed, ReloadLightPulseRange);
            return;
        }

        recargando = false;
        municionActual = municionMaxima;
        if (rendCache != null) SetColor(rendCache.material, colorBase);
        if (lightCache != null) lightCache.intensity = ActiveLightIntensity;
    }

    void RotarHacia(Enemy objetivo)
    {
        Vector3 dir = objetivo.transform.position - parteRotatoria.position;
        dir.y = 0;
        if (dir.sqrMagnitude <= MinimumAimDirectionSqr) return;

        float t = Time.deltaTime * RotationSmoothing;
        parteRotatoria.rotation = Quaternion.Slerp(parteRotatoria.rotation, Quaternion.LookRotation(dir), t);
        Vector3 dirCompleta = objetivo.transform.position - puntoDisparo.position;
        if (dirCompleta != Vector3.zero)
            puntoDisparo.rotation = Quaternion.Slerp(puntoDisparo.rotation, Quaternion.LookRotation(dirCompleta), t);
    }

    Enemy BuscarEnemigoMasCercano()
    {
        Enemy masCercano = null;
        float minDist = rango;
        foreach (Enemy enemy in Enemy.Active)
        {
            if (enemy == null || enemy.EstaMuerto) continue;
            float d = Vector3.Distance(transform.position, enemy.transform.position);
            if (d > minDist) continue;
            // Solo considerar visibles (el Pilar no bloquea).
            if (!TieneLineaVision(enemy)) continue;
            minDist = d;
            masCercano = enemy;
        }

        return masCercano;
    }

    bool TieneLineaVision(Enemy objetivo)
    {
        if (objetivo == null || puntoDisparo == null) return false;
        Vector3 dir = objetivo.transform.position - puntoDisparo.position;
        LayerMask mask = capaObstaculos.value == 0 ? Physics.DefaultRaycastLayers : capaObstaculos;
        if (!Physics.Raycast(puntoDisparo.position, dir.normalized, out RaycastHit hit, dir.magnitude, mask))
            return true;

        // El objetivo, otros enemigos, triggers y el Pilar no bloquean: la torreta dispara por encima.
        if (hit.collider.isTrigger) return true;
        if (hit.collider.GetComponentInParent<Enemy>() != null) return true;
        return hit.collider.GetComponentInParent<Pilar>() != null;
    }

    void IniciarRecarga()
    {
        if (recargando) return;
        recargando = true;
        timerRecarga = tiempoRecarga;
    }

    void Disparar(Enemy objetivo)
    {
        if (objetivo == null || recargando) return;

        municionActual = Mathf.Max(0, municionActual - 1);

        GameObject proj = CrearProyectil();
        if (proj.TryGetComponent(out Rigidbody rb))
        {
            Vector3 dir = (objetivo.transform.position + Vector3.up * TargetAimHeightMeters - puntoDisparo.position).normalized;
            rb.linearVelocity = dir * velocidadProyectil;
            rb.useGravity = false;
        }

        if (proj.TryGetComponent(out Projectile projComp))
        {
            projComp.daño = daño;
            projComp.dañoJugador = NoPlayerDamage; // La torreta no daña jugadores.
            if (PoolManager.Instance != null && proj.TryGetComponent(out PooledObject pooled))
                pooled.ScheduleRelease(projComp.tiempoVida);
        }

        if (municionActual <= 0)
            IniciarRecarga();
    }

    GameObject CrearProyectil()
    {
        if (prefabProyectil == null)
            return CrearProyectilFallback();

        GameObject proj = PoolManager.Instance != null
            ? PoolManager.Instance.Get(ProjectilePoolKey, puntoDisparo.position, puntoDisparo.rotation)
            : null;
        if (proj == null)
            proj = Instantiate(prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);

        proj.SetActive(true);
        return proj;
    }

    GameObject CrearProyectilFallback()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = FallbackProjectileName;
        go.transform.SetPositionAndRotation(puntoDisparo.position, puntoDisparo.rotation);
        go.transform.localScale = Vector3.one * ProjectileScale;

        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = ProjectileColliderRadiusMeters;

        Material mat = RuntimeMaterialFactory.CreateLit(ProjectileColor, ProjectileEmissionMultiplier);
        OwnedMaterialCleanup.Assign(go.GetComponent<Renderer>(), mat);

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = ProjectileTrailSeconds;
        trail.startWidth = ProjectileTrailStartWidthMeters;
        trail.endWidth = ProjectileTrailEndWidthMeters;
        trail.sharedMaterial = mat;
        trail.startColor = ProjectileColor;
        trail.endColor = ProjectileTrailEndColor;

        var proj = go.AddComponent<Projectile>();
        proj.daño = daño;
        proj.tiempoVida = ProjectileLifetimeSeconds;
        proj.destruirAlImpactar = true;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = ProjectileColor;
        light.range = ProjectileLightRangeMeters;
        light.intensity = ProjectileLightIntensity;

        return go;
    }

    public void RecibirDaño(float cantidad)
    {
        if (!destruible || estaDestruida || !activa) return;
        vidaActual -= cantidad;

        if (rendCache != null && isActiveAndEnabled)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashDaño());
        }

        if (vidaActual <= 0)
            Destruir();
    }

    IEnumerator FlashDaño()
    {
        // Property block: el color del material (incluido el parpadeo de recarga) nunca se pisa.
        RendererFlash.Apply(rendCache, Color.white);
        yield return new WaitForSeconds(DamageFlashSeconds);
        RendererFlash.Clear(rendCache);
        flashCoroutine = null;
    }

    void Destruir()
    {
        if (estaDestruida) return;
        estaDestruida = true;
        activa = false;
        Debug.Log($"[Torreta] ¡Destruida! {name} en {transform.position}");

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (rendCache != null)
        {
            RendererFlash.Clear(rendCache);
            SetColor(rendCache.material, Color.gray * DestroyedColorRatio);
            if (rendCache.material.HasProperty("_EmissionColor")) rendCache.material.SetColor("_EmissionColor", Color.black);
        }

        if (lightCache != null) lightCache.enabled = false;
        if (colCache != null) colCache.enabled = false;
        Destroy(gameObject, DestroyDelaySeconds);
    }

    public void Desactivar()
    {
        if (!activa) return;
        activa = false;
        if (rendCache != null) SetColor(rendCache.material, Color.gray);
    }

    static Color GetColor(Material material)
    {
        return material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color;
    }

    static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        else material.color = color;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = recargando ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rango);
        if (puntoDisparo != null)
        {
            Gizmos.color = recargando ? Color.red : Color.yellow;
            Gizmos.DrawLine(puntoDisparo.position, puntoDisparo.position + puntoDisparo.forward * GizmoAimLineMeters);
        }

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            string recarga = recargando ? $"\nRecargando {timerRecarga:F1}s" : string.Empty;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * GizmoLabelHeightMeters,
                $"Vida {vidaActual:F0}/{vidaMaxima}\nMunición {municionActual}/{municionMaxima}{recarga}");
        }
#endif
    }
}
