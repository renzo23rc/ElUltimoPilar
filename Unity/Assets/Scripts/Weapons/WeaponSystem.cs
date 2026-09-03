/**
 * WeaponSystem.cs
 * Gestiona las 3 armas base del jugador: Directa, Área, Cuerpo a cuerpo.
 * Incluye variantes temporales como drops.
 * 
 * Colocar en el mismo GameObject que PlayerController.
 */
using UnityEngine;
using System;

public class WeaponSystem : MonoBehaviour
{
private const int WeaponSlotCount = 3;
private const float ProjectileSpeedMetersPerSecond = 50f;
private const float TracerStartWidthMeters = 0.025f;
private const float TracerEndWidthMeters = 0.015f;
private const float TracerLifetimeSeconds = 0.12f;
private const float ImpactOffsetMeters = 0.05f;
private const float ImpactMissSizeMeters = 0.2f;
private const float ImpactHitSizeMeters = 0.35f;
private const float ImpactLifetimeSeconds = 0.25f;
private const float MuzzleFlashOffsetMeters = 0.3f;
private const float MuzzleFlashSizeMeters = 0.18f;
private const float MuzzleFlashLifetimeSeconds = 0.06f;
private const float MeleeForwardOffsetMeters = 1.5f;
    public enum TipoArma { Directa, Area, CuerpoACuerpo }
    
    [System.Serializable]
    public class Arma
    {
        public string nombre;
        public TipoArma tipo;
        public float daño;
        public float cadencia;
        public int municionMaxima;
        [HideInInspector] public int municionActual;
        public float alcance;
        public float radioArea; // Solo para arma de área
        public GameObject prefabProyectil;
        public GameObject prefabImpacto;
        
        [Header("Feedback")]
        public AudioClip sonidoDisparo;
        public float intensidadScreenShake = 0.1f;
    }
    
    [Header("Armas Base")]
    public Arma armaDirecta = new Arma
    {
        nombre = "Rifle Directo",
        tipo = TipoArma.Directa,
        daño = 16f, // Balanceo: 15->16 para TTK 2 hits corredor débil (10 vida)
        cadencia = 0.15f,
        municionMaxima = 80, // 60->80 para sostener oleada larga sin quedarse seco
        municionActual = 80,
        alcance = 100f
    };
    
    public Arma armaArea = new Arma
    {
        nombre = "Lanzador de Área",
        tipo = TipoArma.Area,
        daño = 42f, // 40->42 compensa resistencia coloso 0.8
        cadencia = 1.1f, // 1.2->1.1 un poco más ágil
        municionMaxima = 16, // 12->16 para decisiones recursos: área vs directa
        municionActual = 16,
        alcance = 32f,
        radioArea = 5.5f
    };
    
    public Arma armaMelee = new Arma
    {
        nombre = "Martillo de Choque",
        tipo = TipoArma.CuerpoACuerpo,
        daño = 50f,
        cadencia = 0.8f,
        municionMaxima = -1, // Sin munición
        municionActual = -1,
        alcance = 3f,
        radioArea = 2.5f
    };
    
    [Header("Estado")]
    public TipoArma armaEquipada = TipoArma.Directa;
    public float cooldownDisparo = 0f;
    
    [Header("Variante temporal")]
    public TipoArma tipoVariante = TipoArma.Directa;
    public float multiplicadorVariante = 1f;
    public float tiempoVarianteRestante = 0f;
    public bool VarianteActiva => tiempoVarianteRestante > 0f;
    
    [Header("Referencias")]
    public Transform puntoDisparo;
    public Camera camara;
    public LayerMask capasImpacto;
    
    // Eventos
    public event Action<Arma> OnDisparo;
    public event Action OnSinMunicion;
    public event Action<TipoArma> OnCambioArma;
    public event Action OnVarianteExpirada;
    
    private PlayerController player;

    private static void ApplyDamage(IDamageable target, float amount)
    {
        target.ReceiveDamage(new DamageRequest(amount));
    }

    void Start()
    {
        player = GetComponent<PlayerController>();
        // Preferir la camara del PlayerController y después una camara hija explícita.
        if (camara == null && player != null && player.camaraJugador != null)
        {
            camara = player.camaraJugador;
            puntoDisparo = player.puntoDisparo;
        }
        if (camara == null)
            camara = GetComponentInChildren<Camera>();
        if (puntoDisparo == null && camara != null)
            puntoDisparo = camara.transform;
        if (camara != null)
            Debug.Log($"[WeaponSystem] Camara asignada: {camara.name} en {camara.transform.position}, puntoDisparo: {puntoDisparo.name}");
    }

    void LateUpdate()
    {
        // Asegurar que puntoDisparo siga a la camara del jugador si cambió
        if (player != null && player.camaraJugador != null && camara != player.camaraJugador)
        {
            camara = player.camaraJugador;
            puntoDisparo = player.puntoDisparo;
        }
    }

    void Update()
    {
        cooldownDisparo -= Time.deltaTime;
        
        if (tiempoVarianteRestante > 0f)
        {
            tiempoVarianteRestante -= Time.deltaTime;
            if (tiempoVarianteRestante <= 0f)
            {
                tiempoVarianteRestante = 0f;
                multiplicadorVariante = 1f;
                OnVarianteExpirada?.Invoke();
                Debug.Log("[WeaponSystem] Variante temporal expirada");
            }
        }
    }

    public void ConsumeCommand(PlayerCommand command)
    {
        if (command.WeaponSlot.HasValue)
        {
            switch (command.WeaponSlot.Value)
            {
                case 1:
                    CambiarArma(TipoArma.Directa);
                    break;
                case 2:
                    CambiarArma(TipoArma.Area);
                    break;
                case 3:
                    CambiarArma(TipoArma.CuerpoACuerpo);
                    break;
            }
        }

        if (command.PreviousWeapon)
            CambiarArmaAnterior();
        if (command.NextWeapon)
            CambiarArmaSiguiente();
        if (command.Fire)
            DispararActual();

        // Mouse-wheel switching remains deferred to its dedicated input slice.
    }

    public void DispararActual()
    {
        if (cooldownDisparo > 0) return;
        
        Arma actual = ObtenerArmaActual();
        if (actual == null) return;
        
        // Verificar munición
        if (actual.municionActual == 0)
        {
            OnSinMunicion?.Invoke();
            Debug.Log("[WeaponSystem] ¡Sin munición!");
            return;
        }
        
        // Gastar munición (excepto melee)
        if (actual.tipo != TipoArma.CuerpoACuerpo)
        {
            actual.municionActual--;
        }
        
        cooldownDisparo = actual.cadencia;
        
        switch (actual.tipo)
        {
            case TipoArma.Directa:
                DispararDirecto(actual);
                break;
            case TipoArma.Area:
                DispararArea(actual);
                break;
            case TipoArma.CuerpoACuerpo:
                AtacarMelee(actual);
                break;
        }
        
        CombatFeedback.NotifyShot(actual.intensidadScreenShake);
        OnDisparo?.Invoke(actual);
        
        // Screen shake simple
        // ScreenShake.Instance?.Shake(actual.intensidadScreenShake);
    }

    void DispararDirecto(Arma arma)
    {
        float daño = DañoEfectivo(arma);
        if (puntoDisparo == null) puntoDisparo = camara != null ? camara.transform : transform;
        Ray ray = new Ray(puntoDisparo.position, puntoDisparo.forward);
        LayerMask mask = capasImpacto.value == 0 ? Physics.DefaultRaycastLayers : capasImpacto;
        if (Physics.Raycast(ray, out RaycastHit hit, arma.alcance, mask))
        {
            var enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamage(enemy, daño);
                Debug.Log($"[WeaponSystem] Impacto directo: {daño} daño a {enemy.name}");
                CrearImpactoVisual(hit.point, hit.normal, Color.red, 0.35f, true);
            }
            else
            {
                CrearImpactoVisual(hit.point, hit.normal, Color.white, 0.25f, false);
            }
            
            // Efecto de impacto prefab si existe
            if (arma.prefabImpacto != null)
                Instantiate(arma.prefabImpacto, hit.point, Quaternion.LookRotation(hit.normal));
            
            CrearTrazador(ray.origin, hit.point, Color.red);
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 0.3f);
        }
        else
        {
            Vector3 fin = ray.origin + ray.direction * arma.alcance;
            CrearTrazador(ray.origin, fin, new Color(1,1,1,0.4f));
            CrearImpactoVisual(fin, -ray.direction, Color.gray, 0.15f, false);
            Debug.DrawRay(ray.origin, ray.direction * arma.alcance, Color.white, 0.3f);
        }
        
        // Proyectil visual
        if (arma.prefabProyectil != null)
        {
            GameObject proj = Instantiate(arma.prefabProyectil, puntoDisparo.position, puntoDisparo.rotation);
            var rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = puntoDisparo.forward * ProjectileSpeedMetersPerSecond;
        }
        else
        {
            // Flash de boca procedural si no hay prefab
            CrearFlashBoca(Color.red);
        }
    }

    void DispararArea(Arma arma)
    {
        float daño = DañoEfectivo(arma);
        if (puntoDisparo == null) puntoDisparo = camara != null ? camara.transform : transform;
        Ray ray = new Ray(puntoDisparo.position, puntoDisparo.forward);
        LayerMask mask = capasImpacto.value == 0 ? Physics.DefaultRaycastLayers : capasImpacto;
        Vector3 puntoImpacto;
        Vector3 normal = -ray.direction;
        
        if (Physics.Raycast(ray, out RaycastHit hit, arma.alcance, mask))
        {
            puntoImpacto = hit.point;
            normal = hit.normal;
        }
        else
        {
            puntoImpacto = puntoDisparo.position + puntoDisparo.forward * arma.alcance;
        }
        
        // Daño en área
        Collider[] afectados = Physics.OverlapSphere(puntoImpacto, arma.radioArea);
        int contador = 0;
        foreach (var col in afectados)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamage(enemy, daño);
                contador++;
            }
        }
        
        Debug.Log($"[WeaponSystem] Explosión de área: {daño} daño a {contador} enemigos");
        
        // Efecto visual procedural SIEMPRE (aunque haya prefab)
        CrearExplosionArea(puntoImpacto, normal, arma.radioArea, contador > 0 ? Color.yellow : new Color(1,0.6f,0,0.8f));
        CrearTrazador(ray.origin, puntoImpacto, Color.yellow);
        
        if (arma.prefabImpacto != null)
            Instantiate(arma.prefabImpacto, puntoImpacto, Quaternion.identity);
        
        CrearFlashBoca(Color.yellow);
        Debug.DrawRay(ray.origin, ray.direction * (puntoImpacto - puntoDisparo.position).magnitude, Color.yellow, 0.5f);
    }

    void AtacarMelee(Arma arma)
    {
        float daño = DañoEfectivo(arma);
        // Ataque en arco frontal
        Collider[] afectados = Physics.OverlapSphere(transform.position + transform.forward * MeleeForwardOffsetMeters, arma.radioArea);
        int contador = 0;
        foreach (var col in afectados)
        {
            var enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamage(enemy, daño);
                
                // Empujar enemigo (física)
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dirEmpuje = (col.transform.position - transform.position).normalized;
                    dirEmpuje.y = 0.5f;
                    rb.AddForce(dirEmpuje * 500f);
                }
                
                contador++;
            }
        }
        
        Debug.Log($"[WeaponSystem] Ataque melee: {daño} daño a {contador} enemigos");
    }

    public void CambiarArma(TipoArma tipo)
    {
        if (armaEquipada == tipo) return;
        armaEquipada = tipo;
        OnCambioArma?.Invoke(tipo);
        Debug.Log($"[WeaponSystem] Arma cambiada a: {ObtenerArmaActual()?.nombre}");
    }

    void CambiarArmaSiguiente()
    {
        int actual = (int)armaEquipada;
        int siguiente = (actual + 1) % WeaponSlotCount;
        CambiarArma((TipoArma)siguiente);
    }

    void CambiarArmaAnterior()
    {
        int actual = (int)armaEquipada;
        int anterior = (actual - 1 + WeaponSlotCount) % WeaponSlotCount;
        CambiarArma((TipoArma)anterior);
    }

    public Arma ObtenerArmaActual()
    {
        return armaEquipada switch
        {
            TipoArma.Directa => armaDirecta,
            TipoArma.Area => armaArea,
            TipoArma.CuerpoACuerpo => armaMelee,
            _ => null
        };
    }

    public void ReponerMunicion()
    {
        if (armaDirecta != null) armaDirecta.municionActual = armaDirecta.municionMaxima;
        if (armaArea != null) armaArea.municionActual = armaArea.municionMaxima;
        SincronizarMunicionLegacy();
        Debug.Log("[WeaponSystem] Munición repuesta");
    }

    public void ApplyVariant(TipoArma tipo, float multiplicador, float duracion)
    {
        tipoVariante = tipo;
        multiplicadorVariante = Mathf.Max(1f, multiplicador);
        tiempoVarianteRestante = Mathf.Max(0f, duracion);
        Debug.Log($"[WeaponSystem] ¡Variante temporal! x{multiplicadorVariante} {tipoVariante} por {tiempoVarianteRestante:F0}s");
    }
    
    public float DañoEfectivo(Arma arma)
    {
        if (arma == null) return 0f;
        if (VarianteActiva && arma.tipo == tipoVariante)
            return arma.daño * multiplicadorVariante;
        return arma.daño;
    }
    
    public void ResetState()
    {
        if (player == null) player = GetComponent<PlayerController>();

        bool weaponChanged = armaEquipada != TipoArma.Directa;
        armaEquipada = TipoArma.Directa;

        if (armaDirecta != null) armaDirecta.municionActual = armaDirecta.municionMaxima;
        if (armaArea != null) armaArea.municionActual = armaArea.municionMaxima;
        if (armaMelee != null) armaMelee.municionActual = armaMelee.municionMaxima;
        cooldownDisparo = 0f;
        tiempoVarianteRestante = 0f;
        multiplicadorVariante = 1f;
        tipoVariante = TipoArma.Directa;
        SincronizarMunicionLegacy();

        if (weaponChanged) OnCambioArma?.Invoke(armaEquipada);
    }

    void SincronizarMunicionLegacy()
    {
        if (player == null) return;
        if (armaDirecta != null) player.municionDirecta = armaDirecta.municionActual;
        if (armaArea != null) player.municionArea = armaArea.municionActual;
    }

    // ===== VISUAL FEEDBACK PROCEDURAL (sin prefabs) =====
    void CrearTrazador(Vector3 inicio, Vector3 fin, Color color)
    {
        GameObject go = new GameObject("Trazador");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[]{ inicio, fin });
        lr.startWidth = TracerStartWidthMeters;
        lr.endWidth = TracerEndWidthMeters;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 2;
        // Material URP simple, fallback a Sprites/Default
        Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        lr.material = new Material(s);
        lr.material.color = color;
        // Sin sombras, billboard
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        Destroy(go, TracerLifetimeSeconds);
    }

    void CrearImpactoVisual(Vector3 pos, Vector3 normal, Color color, float size, bool esHit)
    {
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = esHit ? "Impacto_HIT" : "Impacto_MISS";
        esfera.transform.position = pos + normal * ImpactOffsetMeters;
        esfera.transform.localScale = Vector3.one * size;
        // Quitar collider para no bloquear
        Destroy(esfera.GetComponent<Collider>());
        var rend = esfera.GetComponent<Renderer>();
        // Material emision simple
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        // Emision para que brille
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 1.5f);
        if (m.HasProperty("_EmissiveColor")) m.SetColor("_EmissiveColor", color * 1.5f);
        rend.material = m;
        // Animar escala y desvanecer
        esfera.AddComponent<ImpactoAnim>().Init(esHit ? ImpactHitSizeMeters : ImpactMissSizeMeters, esHit);
        // Anillo extra si es hit
        if (esHit)
        {
            GameObject anillo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anillo.name = "AnilloImpacto";
            Destroy(anillo.GetComponent<Collider>());
            anillo.transform.position = pos + normal * 0.02f;
            anillo.transform.localScale = new Vector3(size*1.8f, 0.02f, size*1.8f);
            anillo.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            var r2 = anillo.GetComponent<Renderer>();
            Material m2 = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            m2.SetColor("_BaseColor", Color.white);
            m2.SetColor("_Color", Color.white);
            r2.material = m2;
            Destroy(anillo, ImpactLifetimeSeconds);
        }
    }

    void CrearExplosionArea(Vector3 pos, Vector3 normal, float radio, Color color)
    {
        // Esfera de explosion semitransparente
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "ExplosionArea";
        Destroy(esfera.GetComponent<Collider>());
        esfera.transform.position = pos + Vector3.up * 0.05f;
        esfera.transform.localScale = Vector3.one * 0.2f;
        var rend = esfera.GetComponent<Renderer>();
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        Color c = new Color(color.r, color.g, color.b, 0.35f);
        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c);
        // Transparencia
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
        rend.material = m;
        esfera.AddComponent<ExplosionAnim>().Init(radio, 0.45f, color);
        
        // Anillo de onda en suelo
        GameObject onda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        onda.name = "OndaArea";
        Destroy(onda.GetComponent<Collider>());
        onda.transform.position = pos + Vector3.up * 0.02f;
        onda.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
        var r2 = onda.GetComponent<Renderer>();
        Material m2 = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m2.SetColor("_BaseColor", color);
        m2.SetColor("_Color", color);
        r2.material = m2;
        onda.AddComponent<OndaAreaAnim>().Init(radio*2f, 0.45f);
    }

    void CrearFlashBoca(Color color)
    {
        if (puntoDisparo == null) return;
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "FlashBoca";
        Destroy(flash.GetComponent<Collider>());
        flash.transform.position = puntoDisparo.position + puntoDisparo.forward * MuzzleFlashOffsetMeters;
        flash.transform.localScale = Vector3.one * MuzzleFlashSizeMeters;
        var rend = flash.GetComponent<Renderer>();
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 2f);
        rend.material = m;
        Destroy(flash, MuzzleFlashLifetimeSeconds);
    }

    void OnDrawGizmosSelected()
    {
        if (armaEquipada == TipoArma.CuerpoACuerpo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * MeleeForwardOffsetMeters, armaMelee.radioArea);
        }
    }
}

// Helpers visuales (sin prefabs)
public class ImpactoAnim : MonoBehaviour
{
    float dur; bool hit; float t; Vector3 ini;
    public void Init(float d, bool h){ dur=d; hit=h; t=0; ini=transform.localScale; }
    void Update()
    {
        t+=Time.deltaTime;
        float p=t/dur;
        if(hit) transform.localScale = Vector3.Lerp(ini, ini*1.6f, p);
        else transform.localScale = Vector3.Lerp(ini, ini*0.6f, p);
        var r=GetComponent<Renderer>();
        if(r!=null){
            Color c=r.material.color;
            c.a=Mathf.Lerp(1,0,p);
            r.material.color=c;
        }
        if(t>=dur) Destroy(gameObject);
    }
}
public class ExplosionAnim : MonoBehaviour
{
    float radio, dur, t; Color col;
    public void Init(float r,float d,Color c){ radio=r; dur=d; col=c; t=0; }
    void Update()
    {
        t+=Time.deltaTime;
        float p=t/dur;
        float s=Mathf.Lerp(0.2f, radio*2f, p);
        transform.localScale=new Vector3(s,s*0.6f,s);
        var r=GetComponent<Renderer>();
        if(r!=null){
            Color c=col; c.a=Mathf.Lerp(0.5f,0,p);
            r.material.SetColor("_BaseColor",c);
            r.material.SetColor("_Color",c);
        }
        if(t>=dur) Destroy(gameObject);
    }
}
public class OndaAreaAnim : MonoBehaviour
{
    float radio,dur,t;
    public void Init(float r,float d){ radio=r; dur=d; t=0; }
    void Update()
    {
        t+=Time.deltaTime;
        float p=t/dur;
        float s=Mathf.Lerp(0.5f, radio, p);
        transform.localScale=new Vector3(s,0.02f,s);
        var r=GetComponent<Renderer>();
        if(r!=null){
            Color c=r.material.color; c.a=Mathf.Lerp(0.8f,0,p); r.material.color=c;
        }
        if(t>=dur) Destroy(gameObject);
    }
}
