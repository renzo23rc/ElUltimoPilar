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
using UnityEngine;

public class Torreta : MonoBehaviour
{
    private const float ZeroSeconds = 0f;
    private const float PointFiveSeconds = 0.5f;
    private const float RotationSmoothing = 8f;
    private const float GroundedVerticalSpeed = -2f;
    private const float ReloadLightBaseIntensity = 0.5f;
    private const float ReloadLightPulseRange = 0.5f;
    private const float FullPulseCycleSeconds = 1f;
    private const float SpawnPointForwardMeters = 0.8f;
    private const float SpawnPointHeightMeters = 0.5f;
    private const float ProjectileLifetimeSeconds = 4f;
    private const float ProjectileScale = 0.6f;
    private const float ProjectileColliderRadiusMeters = 0.5f;
    private const float ProjectileLightRangeMeters = 4f;
    private const float ProjectileLightIntensity = 2f;
    private const float DamageFlashSeconds = 0.07f;
    private const float DestroyDelaySeconds = 2f;
    private const string ProjectilePoolKey = "Proyectil";
    private const string SpawnPointName = "PuntoDisparo";
    private const string FallbackProjectileName = "ProyectilTorreta";

    [Header("Torreta - Configuración")]
    public float rango = 22f;
    public float cadencia = 0.9f; // disparos por segundo configurable (intervalo)
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
    private bool activa = true;
    private Renderer rendCache;
    private Light lightCache;
    private Color colorBase;
    private BoxCollider colCache;

    void Start()
    {
        if (puntoDisparo == null)
        {
            // Buscar hijo o crear punto al frente
            var existing = transform.Find(SpawnPointName);
            if (existing != null) puntoDisparo = existing;
            else
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

        // Vida y munición iniciales
        vidaActual = vidaMaxima;
        municionActual = municionMaxima;
        timerRecarga = ZeroSeconds;
        recargando = false;

        // Asegurar collider para recibir daño (antes se destruía)
        colCache = GetComponent<BoxCollider>();
        if (colCache == null)
        {
            colCache = gameObject.AddComponent<BoxCollider>();
            colCache.center = Vector3.zero;
            colCache.size = Vector3.one;
        }
        colCache.isTrigger = false;

        rendCache = GetComponentInChildren<Renderer>();
        if (rendCache != null) colorBase = rendCache.material.HasProperty("_BaseColor") ? rendCache.material.GetColor("_BaseColor") : rendCache.material.color;
        lightCache = GetComponentInChildren<Light>();

        // Suscribirse a fin de partida para desactivarse
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVictoria += Desactivar;
            GameManager.Instance.OnDerrota += Desactivar;
        }
        // Si no hay prefab proyectil, usará fallback esfera
        Debug.Log($"[Torreta] Iniciada en {transform.position} vida {vidaActual}/{vidaMaxima} daño {daño} munición {municionActual}/{municionMaxima} rango {rango} cadencia {cadencia}");
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnVictoria -= Desactivar;
            GameManager.Instance.OnDerrota -= Desactivar;
        }
    }

    void Update()
    {
        if (!activa || estaDestruida) return;

        // Gestión de recarga
        if (recargando)
        {
            timerRecarga -= Time.deltaTime;
            // Parpadeo visual durante recarga
            if (rendCache != null)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, FullPulseCycleSeconds);
                Color c = Color.Lerp(colorBase * 0.3f, colorBase, pulse);
                if (rendCache.material.HasProperty("_BaseColor")) rendCache.material.SetColor("_BaseColor", c);
                else rendCache.material.color = c;
            }
            if (lightCache != null)
            {
                lightCache.intensity = ReloadLightBaseIntensity + Mathf.PingPong(Time.time * 2f, ReloadLightPulseRange);
            }

            if (timerRecarga <= 0f)
            {
                recargando = false;
                municionActual = municionMaxima;
                // Restaurar visual
                if (rendCache != null)
                {
                    if (rendCache.material.HasProperty("_BaseColor")) rendCache.material.SetColor("_BaseColor", colorBase);
                    else rendCache.material.color = colorBase;
                }
                if (lightCache != null) lightCache.intensity = 2f;
                Debug.Log($"[Torreta] Recarga completa en {name} munición {municionActual}/{municionMaxima}");
            }
            else
            {
                // Durante recarga no disparar, pero seguir rotando
                var objetivoRecarga = BuscarEnemigoMasCercano();
                if (objetivoRecarga != null) RotarHacia(objetivoRecarga);
                return;
            }
        }

        timerDisparo -= Time.deltaTime;

        var objetivo = BuscarEnemigoMasCercano();
        if (objetivo == null) return;

        RotarHacia(objetivo);

        if (timerDisparo <= 0f)
        {
            // Verificar munición antes de disparar
            if (municionActual <= 0)
            {
                IniciarRecarga();
                return;
            }
            // Verificar línea de visión
            if (TieneLineaVision(objetivo))
            {
                Disparar(objetivo);
                timerDisparo = cadencia;
            }
        }
    }

    void RotarHacia(Enemy objetivo)
    {
        Vector3 dir = objetivo.transform.position - parteRotatoria.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            parteRotatoria.rotation = Quaternion.Slerp(parteRotatoria.rotation, targetRot, Time.deltaTime * 8f);
            Vector3 dirCompleta = objetivo.transform.position - puntoDisparo.position;
            if (dirCompleta != Vector3.zero)
                puntoDisparo.rotation = Quaternion.Slerp(puntoDisparo.rotation, Quaternion.LookRotation(dirCompleta), Time.deltaTime * 8f);
        }
    }

    Enemy BuscarEnemigoMasCercano()
    {
        var todos = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy masCercano = null;
        float minDist = float.MaxValue;
        foreach (var e in todos)
        {
            if (e == null) continue;
            if (e.vidaActual <= 0) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d > rango) continue;
            // Solo considerar visibles (pilar no bloquea)
            if (!TieneLineaVision(e)) continue;
            if (d < minDist)
            {
                minDist = d;
                masCercano = e;
            }
        }
        return masCercano;
    }

    bool TieneLineaVision(Enemy objetivo)
    {
        if (objetivo == null || puntoDisparo == null) return false;
        Vector3 dir = objetivo.transform.position - puntoDisparo.position;
        float dist = dir.magnitude;
        LayerMask mask = capaObstaculos.value == 0 ? Physics.DefaultRaycastLayers : capaObstaculos;
        if (Physics.Raycast(puntoDisparo.position, dir.normalized, out RaycastHit hit, dist, mask))
        {
            var hitEnemy = hit.collider.GetComponent<Enemy>();
            if (hitEnemy == null) hitEnemy = hit.collider.GetComponentInParent<Enemy>();
            if (hitEnemy == objetivo) return true;
            // Pilar no bloquea visión (torreta dispara por encima/alrededor)
            if (hit.collider.GetComponent<Pilar>() != null || hit.collider.GetComponentInParent<Pilar>() != null)
                return true;
            if (!hit.collider.isTrigger && hit.collider.GetComponent<Enemy>() == null)
                return false;
        }
        return true;
    }

    void IniciarRecarga()
    {
        if (recargando) return;
        recargando = true;
        timerRecarga = tiempoRecarga;
        Debug.Log($"[Torreta] Sin munición en {name} - recargando {tiempoRecarga}s...");
    }

    void Disparar(Enemy objetivo)
    {
        if (objetivo == null) return;
        if (recargando) return;

        // Consumir munición
        municionActual--;
        if (municionActual < 0) municionActual = 0;

        GameObject proj = null;
        if (prefabProyectil != null && PoolManager.Instance != null)
        {
            proj = PoolManager.Instance.Get(ProjectilePoolKey, puntoDisparo.position, puntoDisparo.rotation);
            if (proj != null)
            {
                proj.transform.SetPositionAndRotation(puntoDisparo.position, puntoDisparo.rotation);
                proj.SetActive(true);
            }
            else
            {
                proj = Instantiate(prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
            }
        }
        else if (prefabProyectil != null)
        {
            proj = Instantiate(prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
        }
        else
        {
            proj = CrearProyectilFallback();
        }

        var rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 dir = (objetivo.transform.position + Vector3.up * 0.5f - puntoDisparo.position).normalized;
            rb.linearVelocity = dir * velocidadProyectil;
            rb.useGravity = false;
        }

        var projComp = proj.GetComponent<Projectile>();
        if (projComp != null)
        {
            projComp.daño = daño;
            projComp.dañoJugador = 0; // torreta no daña jugador
            var pooled = proj.GetComponent<PooledObject>();
            if (pooled != null && PoolManager.Instance != null)
                pooled.ScheduleRelease(projComp.tiempoVida);
        }

        Debug.Log($"[Torreta] Disparo a {objetivo.name} daño {daño} munición {municionActual}/{municionMaxima}");
        if (municionActual <= 0)
        {
            IniciarRecarga();
        }
    }

    GameObject CrearProyectilFallback()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = FallbackProjectileName;
        go.transform.position = puntoDisparo.position;
        go.transform.rotation = puntoDisparo.rotation;
        go.transform.localScale = Vector3.one * 0.6f;
        // Limpiar collider default y añadir trigger
        Destroy(go.GetComponent<SphereCollider>());
        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = ProjectileColliderRadiusMeters;
        var rend = go.GetComponent<Renderer>();
        // Material emisivo naranja brillante con trail
        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(s);
        Color colMat = new Color(1f, 0.5f, 0f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colMat);
        else mat.color = colMat;
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", colMat);
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", colMat * 1.2f);
        mat.EnableKeyword("_EMISSION");
        rend.material = mat;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.4f;
        trail.startWidth = 0.25f;
        trail.endWidth = 0.05f;
        trail.material = mat;
        trail.startColor = colMat;
        trail.endColor = new Color(1, 0.5f, 0, 0.2f);

        var proj = go.AddComponent<Projectile>();
        proj.daño = daño;
        proj.tiempoVida = ProjectileLifetimeSeconds;
        proj.destruirAlImpactar = true;

        // Luz más grande para ver tiro
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = colMat;
        light.range = ProjectileLightRangeMeters;
        light.intensity = ProjectileLightIntensity;

        return go;
    }

    public void RecibirDaño(float cantidad)
    {
        if (!destruible || estaDestruida || !activa) return;
        vidaActual -= cantidad;
        Debug.Log($"[Torreta] {name} recibió {cantidad} daño vida {vidaActual:F0}/{vidaMaxima}");
        // Flash daño
        if (rendCache != null) StartCoroutine(FlashDaño());
        if (vidaActual <= 0)
        {
            Destruir();
        }
    }

    System.Collections.IEnumerator FlashDaño()
    {
        if (rendCache == null) yield break;
        Color orig = rendCache.material.HasProperty("_BaseColor") ? rendCache.material.GetColor("_BaseColor") : rendCache.material.color;
        Color flash = Color.white;
        if (rendCache.material.HasProperty("_BaseColor")) rendCache.material.SetColor("_BaseColor", flash);
        else rendCache.material.color = flash;
        yield return new WaitForSeconds(DamageFlashSeconds);
        if (rendCache != null)
        {
            if (rendCache.material.HasProperty("_BaseColor")) rendCache.material.SetColor("_BaseColor", orig);
            else rendCache.material.color = orig;
        }
    }

    void Destruir()
    {
        if (estaDestruida) return;
        estaDestruida = true;
        activa = false;
        Debug.Log($"[Torreta] ¡Destruida! {name} en {transform.position}");
        // Efecto visual: oscurecer y apagar luz
        if (rendCache != null)
        {
            Color gris = Color.gray * 0.6f;
            if (rendCache.material.HasProperty("_BaseColor")) rendCache.material.SetColor("_BaseColor", gris);
            else rendCache.material.color = gris;
            if (rendCache.material.HasProperty("_EmissionColor")) rendCache.material.SetColor("_EmissionColor", Color.black);
        }
        if (lightCache != null) lightCache.enabled = false;
        if (colCache != null) colCache.enabled = false;
        // Destruir tras delay para que se vea
        Destroy(gameObject, DestroyDelaySeconds);
    }

    public void Desactivar()
    {
        if (!activa) return;
        activa = false;
        Debug.Log($"[Torreta] Desactivada (fin de partida) en {name}");
        // Opcional: cambiar color a gris
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null) rend.material.color = Color.gray;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = recargando ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rango);
        if (puntoDisparo != null)
        {
            Gizmos.color = recargando ? Color.red : Color.yellow;
            Gizmos.DrawLine(puntoDisparo.position, puntoDisparo.position + puntoDisparo.forward * 2f);
        }
        // Vida
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, $"Vida {vidaActual:F0}/{vidaMaxima}\nMunición {municionActual}/{municionMaxima}" + (recargando ? $"\nRecargando {timerRecarga:F1}s" : ""));
        }
        #endif
    }
}
