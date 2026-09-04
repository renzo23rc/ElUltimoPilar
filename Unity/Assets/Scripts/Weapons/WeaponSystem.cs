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
private const float MuzzleForwardOffsetMeters = 0.9f;
private const float MuzzleHeightMeters = 0.8f;
private const string MuzzleObjectName = "PuntoDisparo";
private const float CrosshairViewportCenter = 0.5f;
private const float AimSelfIgnoreOffsetMeters = 0.05f;
private const int MaxAimSelfIgnoreIterations = 3;
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
private const float PushForceNewtons = 500f;

    public enum TipoArma { Directa, Area, CuerpoACuerpo }

    public enum WeaponVariant
    {
PrecisionRifle,
Decoy,
Slowdown,
PushStrike
    }

    public enum VariantEffect
    {
DamageMultiplier,
Decoy,
Slowdown,
Push
    }

    public readonly struct VariantDefinition
    {
public VariantDefinition(TipoArma weaponType, VariantEffect effect, string displayName)
{
WeaponType = weaponType;
Effect = effect;
DisplayName = displayName;
}

public TipoArma WeaponType { get; }
public VariantEffect Effect { get; }
public string DisplayName { get; }
    }

    public static VariantDefinition GetVariantDefinition(WeaponVariant variant)
    {
return variant switch
{
WeaponVariant.PrecisionRifle => new VariantDefinition(
TipoArma.Directa,
VariantEffect.DamageMultiplier,
"Rifle de precisión"),
WeaponVariant.Decoy => new VariantDefinition(
TipoArma.Area,
VariantEffect.Decoy,
"Señuelo"),
WeaponVariant.Slowdown => new VariantDefinition(
TipoArma.Area,
VariantEffect.Slowdown,
"Ralentización"),
WeaponVariant.PushStrike => new VariantDefinition(
TipoArma.CuerpoACuerpo,
VariantEffect.Push,
"Golpe de empuje"),
_ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported weapon variant.")
};
    }

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
        [HideInInspector] public WeaponVariant activeVariant = WeaponVariant.PrecisionRifle;
        [HideInInspector] public VariantEffect activeVariantEffect = VariantEffect.DamageMultiplier;
        public bool VarianteActiva => tiempoVarianteRestante > 0f;

    public WeaponVariant ActiveVariant => activeVariant;
    public VariantEffect ActiveVariantEffect => activeVariantEffect;
    public string ActiveVariantDisplayName => GetActiveVariantDisplayName();

    [Header("Referencias")]
    public Transform puntoDisparo;
    public Camera camara;
    public LayerMask capasImpacto;
    
    // Eventos
    public event Action<Arma> OnDisparo;
    public event Action OnSinMunicion;
    public event Action<TipoArma> OnCambioArma;
    public event Action OnVarianteExpirada;
    public event Action<Vector3> OnDecoyRequested;
    public event Action<Enemy, float> OnSlowdownRequested;

    private PlayerController player;
    private bool hasSemanticVariant;

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
        if (puntoDisparo == null || (camara != null && puntoDisparo == camara.transform))
        {
            puntoDisparo = EnsureMuzzleTransform();
        }
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

        if (puntoDisparo == null || (camara != null && puntoDisparo == camara.transform))
        {
            puntoDisparo = EnsureMuzzleTransform();
        }

        if (puntoDisparo != null && camara != null && puntoDisparo.parent == transform)
        {
            puntoDisparo.rotation = camara.transform.rotation;
        }
    }

    Transform EnsureMuzzleTransform()
    {
        if (camara == null)
        {
            return transform;
        }

        Transform existing = transform.Find(MuzzleObjectName);
        if (existing == null)
        {
            existing = camara.transform.Find(MuzzleObjectName);
        }

        if (existing != null)
        {
            return existing;
        }

        if (player != null && player.puntoDisparo != null && player.puntoDisparo != camara.transform)
        {
            return player.puntoDisparo;
        }

        GameObject muzzle = new GameObject(MuzzleObjectName);
        muzzle.transform.SetParent(transform);
        float height = camara.transform.localPosition.y;
        if (Mathf.Approximately(height, 0f))
        {
            height = MuzzleHeightMeters;
        }

        muzzle.transform.localPosition = new Vector3(0f, height, MuzzleForwardOffsetMeters);
        muzzle.transform.rotation = camara.transform.rotation;
        muzzle.transform.localScale = Vector3.one;
        return muzzle.transform;
    }

    Ray GetAimRay()
    {
        if (camara != null)
        {
            return camara.ViewportPointToRay(new Vector3(CrosshairViewportCenter, CrosshairViewportCenter, 0f));
        }

        if (puntoDisparo != null)
        {
            return new Ray(puntoDisparo.position, puntoDisparo.forward);
        }

        return new Ray(transform.position, transform.forward);
    }

    bool TryRaycastAim(Ray aimRay, float maxDistance, LayerMask mask, out RaycastHit hit)
    {
        hit = default;
        LayerMask effectiveMask = mask.value == 0 ? Physics.DefaultRaycastLayers : mask;
        Ray currentRay = aimRay;
        float remaining = maxDistance;

        for (int i = 0; i < MaxAimSelfIgnoreIterations; i++)
        {
            if (Physics.Raycast(currentRay, out RaycastHit candidate, remaining, effectiveMask))
            {
                PlayerController shooter = candidate.collider.GetComponentInParent<PlayerController>();
                if (shooter != null && shooter == player)
                {
                    float advance = candidate.distance + AimSelfIgnoreOffsetMeters;
                    if (advance >= remaining)
                    {
                        break;
                    }

                    currentRay = new Ray(candidate.point + currentRay.direction * AimSelfIgnoreOffsetMeters, currentRay.direction);
                    remaining -= advance;
                    continue;
                }

                if (puntoDisparo != null && candidate.collider.transform == puntoDisparo)
                {
                    float advance = candidate.distance + AimSelfIgnoreOffsetMeters;
                    currentRay = new Ray(candidate.point + currentRay.direction * AimSelfIgnoreOffsetMeters, currentRay.direction);
                    remaining -= advance;
                    continue;
                }

                hit = candidate;
                return true;
            }

            break;
        }

        return false;
    }

    Vector3 GetMuzzlePosition()
    {
        if (puntoDisparo != null)
        {
            return puntoDisparo.position;
        }

        if (camara != null)
        {
            return camara.transform.position + camara.transform.forward * MuzzleForwardOffsetMeters;
        }

        return transform.position;
    }

    Vector3 GetMuzzleForward()
    {
        if (puntoDisparo != null)
        {
            return puntoDisparo.forward;
        }

        if (camara != null)
        {
            return camara.transform.forward;
        }

        return transform.forward;
    }

    void Update()
    {
        cooldownDisparo -= Time.deltaTime;
        
        if (tiempoVarianteRestante > 0f)
        {
            tiempoVarianteRestante -= Time.deltaTime;
            if (tiempoVarianteRestante <= 0f)
            {
                ClearVariantState(true);
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
        if (puntoDisparo == null || (camara != null && puntoDisparo == camara.transform))
        {
            puntoDisparo = EnsureMuzzleTransform();
        }

        Vector3 muzzlePos = GetMuzzlePosition();
        Ray aimRay = GetAimRay();
        LayerMask mask = capasImpacto;
        bool hasHit = TryRaycastAim(aimRay, arma.alcance, mask, out RaycastHit aimHit);

        if (hasHit)
        {
            var enemy = aimHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                ApplyDamage(enemy, daño);
                Debug.Log($"[WeaponSystem] Impacto directo: {daño} daño a {enemy.name}");
                CrearImpactoVisual(aimHit.point, aimHit.normal, Color.red, 0.35f, true);
            }
            else
            {
                CrearImpactoVisual(aimHit.point, aimHit.normal, Color.white, 0.25f, false);
            }

            if (arma.prefabImpacto != null)
                Instantiate(arma.prefabImpacto, aimHit.point, Quaternion.LookRotation(aimHit.normal));

            CrearTrazador(muzzlePos, aimHit.point, Color.red);
            Debug.DrawRay(aimRay.origin, aimRay.direction * aimHit.distance, Color.red, 0.3f);
        }
        else
        {
            Vector3 fin = aimRay.origin + aimRay.direction * arma.alcance;
            Vector3 tracerEnd = muzzlePos + aimRay.direction * arma.alcance;
            CrearTrazador(muzzlePos, tracerEnd, new Color(1, 1, 1, 0.4f));
            CrearImpactoVisual(fin, -aimRay.direction, Color.gray, 0.15f, false);
            Debug.DrawRay(aimRay.origin, aimRay.direction * arma.alcance, Color.white, 0.3f);
        }

        if (arma.prefabProyectil != null)
        {
            Vector3 spawnPos = muzzlePos;
            Quaternion spawnRot = puntoDisparo != null ? puntoDisparo.rotation : Quaternion.LookRotation(aimRay.direction);
            GameObject proj = Instantiate(arma.prefabProyectil, spawnPos, spawnRot);
            Rigidbody rb = proj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 dir = hasHit ? (aimHit.point - spawnPos).normalized : aimRay.direction;
                rb.linearVelocity = dir * ProjectileSpeedMetersPerSecond;
            }
        }
        else
        {
            CrearFlashBoca(Color.red);
        }
    }

    void DispararArea(Arma arma)
    {
        float daño = DañoEfectivo(arma);
        if (puntoDisparo == null || (camara != null && puntoDisparo == camara.transform))
        {
            puntoDisparo = EnsureMuzzleTransform();
        }

        Vector3 muzzlePos = GetMuzzlePosition();
        Ray aimRay = GetAimRay();
        LayerMask mask = capasImpacto;
        Vector3 puntoImpacto;
        Vector3 normal = -aimRay.direction;

        if (TryRaycastAim(aimRay, arma.alcance, mask, out RaycastHit hit))
        {
            puntoImpacto = hit.point;
            normal = hit.normal;
        }
        else
        {
            puntoImpacto = aimRay.origin + aimRay.direction * arma.alcance;
        }

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

        ApplyAreaVariantEffect(afectados, puntoImpacto);
        Debug.Log($"[WeaponSystem] Explosión de área: {daño} daño a {contador} enemigos");

        CrearExplosionArea(puntoImpacto, normal, arma.radioArea, contador > 0 ? Color.yellow : new Color(1, 0.6f, 0, 0.8f));
        CrearTrazador(muzzlePos, puntoImpacto, Color.yellow);

        if (arma.prefabImpacto != null)
            Instantiate(arma.prefabImpacto, puntoImpacto, Quaternion.identity);

        CrearFlashBoca(Color.yellow);
        Debug.DrawRay(aimRay.origin, aimRay.direction * Vector3.Distance(aimRay.origin, puntoImpacto), Color.yellow, 0.5f);
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
                
                if (ShouldApplyPushVariant(arma))
                {
                    ApplyPush(col);
                }

                contador++;
            }
        }
        
        Debug.Log($"[WeaponSystem] Ataque melee: {daño} daño a {contador} enemigos");
    }

        private void ApplyAreaVariantEffect(Collider[] affected, Vector3 impactPoint)
        {
            if (!VarianteActiva || tipoVariante != TipoArma.Area)
                return;

            switch (activeVariantEffect)
            {
                case VariantEffect.Decoy:
                    OnDecoyRequested?.Invoke(impactPoint);
                    break;
                case VariantEffect.Slowdown:
                    foreach (var collider in affected)
                    {
                        var enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                            OnSlowdownRequested?.Invoke(enemy, tiempoVarianteRestante);
                    }
                    break;
            }
        }

        private bool ShouldApplyPushVariant(Arma arma)
        {
            return VarianteActiva
                && arma.tipo == tipoVariante
                && activeVariantEffect == VariantEffect.Push;
        }

        private void ApplyPush(Collider collider)
        {
            Rigidbody rigidbody = collider.GetComponent<Rigidbody>();
            if (rigidbody == null)
                return;

            Vector3 pushDirection = (collider.transform.position - transform.position).normalized;
            pushDirection.y = 0.5f;
            rigidbody.AddForce(pushDirection * PushForceNewtons);
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
            activeVariant = WeaponVariant.PrecisionRifle;
        hasSemanticVariant = false;
        activeVariantEffect = VariantEffect.DamageMultiplier;
        SetVariantState(tipo, multiplicador, duracion);
    }

    public void ApplyVariant(WeaponVariant variant, float multiplicador, float duracion)
    {
        VariantDefinition definition = GetVariantDefinition(variant);
        activeVariant = variant;
        activeVariantEffect = definition.Effect;
        hasSemanticVariant = true;
        SetVariantState(definition.WeaponType, multiplicador, duracion);
    }

    private void SetVariantState(TipoArma tipo, float multiplicador, float duracion)
    {
        tipoVariante = tipo;
        multiplicadorVariante = Mathf.Max(1f, multiplicador);
        tiempoVarianteRestante = Mathf.Max(0f, duracion);
        Debug.Log($"[WeaponSystem] ¡Variante temporal! x{multiplicadorVariante} {ActiveVariantDisplayName} por {tiempoVarianteRestante:F0}s");
    }

    private void ClearVariantState(bool notify)
    {
        tiempoVarianteRestante = 0f;
        multiplicadorVariante = 1f;
        tipoVariante = TipoArma.Directa;
        activeVariant = WeaponVariant.PrecisionRifle;
        activeVariantEffect = VariantEffect.DamageMultiplier;
        hasSemanticVariant = false;
    
        if (notify)
            OnVarianteExpirada?.Invoke();
    }

    private string GetActiveVariantDisplayName()
    {
        if (!VarianteActiva)
            return string.Empty;
            if (!hasSemanticVariant)
                return tipoVariante.ToString();
            return GetVariantDefinition(activeVariant).DisplayName;

    }
        
    public float DañoEfectivo(Arma arma)
    {
        if (arma == null) return 0f;
        if (VarianteActiva
            && arma.tipo == tipoVariante
            && activeVariantEffect == VariantEffect.DamageMultiplier)
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
        ClearVariantState(false);
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
