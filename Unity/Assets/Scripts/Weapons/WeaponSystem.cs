/**
 * WeaponSystem.cs
 * Gestiona las 3 armas base del jugador: Directa, Área, Cuerpo a cuerpo.
 * Incluye variantes temporales como drops.
 *
 * Colocar en el mismo GameObject que PlayerController.
 */
using System;
using System.Collections.Generic;
using UltimoPilar.Core.Shared;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    private const int WeaponSlotCount = 3;
    private const int FirstWeaponSlot = 1;
    private const int SecondWeaponSlot = 2;
    private const int ThirdWeaponSlot = 3;
    private const float CrosshairViewportCenter = 0.5f;
    private const float AimSelfIgnoreOffsetMeters = 0.05f;
    private const int MaxAimSelfIgnoreIterations = 3;
    private const float ProjectileSpeedMetersPerSecond = 50f;
    private const float MeleeForwardOffsetMeters = 1.5f;
    private const float MinimumVariantMultiplier = 1f;
    private const float DirectHitImpactSizeMeters = 0.35f;
    private const float SurfaceImpactSizeMeters = 0.25f;
    private const float MissImpactSizeMeters = 0.15f;
    private static readonly Color MissTracerColor = new Color(1f, 1f, 1f, 0.4f);
    private static readonly Color EmptyAreaExplosionColor = new Color(1f, 0.6f, 0f, 0.8f);

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
            WeaponVariant.PrecisionRifle => new VariantDefinition(TipoArma.Directa, VariantEffect.DamageMultiplier, "Rifle de precisión"),
            WeaponVariant.Decoy => new VariantDefinition(TipoArma.Area, VariantEffect.Decoy, "Señuelo"),
            WeaponVariant.Slowdown => new VariantDefinition(TipoArma.Area, VariantEffect.Slowdown, "Ralentización"),
            WeaponVariant.PushStrike => new VariantDefinition(TipoArma.CuerpoACuerpo, VariantEffect.Push, "Golpe de empuje"),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported weapon variant.")
        };
    }

    [Serializable]
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

    [Header("Efectos de variante")]
    [SerializeField, Min(0f)] private float slowdownDurationSeconds = 4f;

    [Header("Empuje cuerpo a cuerpo")]
    [SerializeField, Min(0f)] private float meleeKnockbackSpeedMetersPerSecond = 4f;
    [SerializeField, Min(0f)] private float pushStrikeKnockbackSpeedMetersPerSecond = 12f;
    [SerializeField, Min(0f)] private float knockbackLiftRatio = 0.3f;
    [SerializeField, Min(0f)] private float knockbackRecoverySeconds = 0.6f;

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

    /// <summary>
    /// Raised once per attack of this weapon system that damages at least one enemy.
    /// The argument says whether any of the damaged enemies died.
    /// </summary>
    public event Action<bool> OnImpactoConfirmado;

    private readonly WeaponFeedbackVfx vfx = new WeaponFeedbackVfx();
    private PlayerController player;
    private bool hasSemanticVariant;

    public bool VarianteActiva => tiempoVarianteRestante > 0f;

    public WeaponVariant ActiveVariant => activeVariant;

    public VariantEffect ActiveVariantEffect => activeVariantEffect;

    public string ActiveVariantDisplayName => GetActiveVariantDisplayName();

    /// <summary>Gets whether the active variant multiplies damage, as opposed to a utility effect.</summary>
    public bool VariantMultipliesDamage => VarianteActiva && activeVariantEffect == VariantEffect.DamageMultiplier;

    // Devuelve si el golpe mató al enemigo (el Explosivo muere al detonar, pero su vida ya llega a 0).
    private static bool ApplyDamage(Enemy target, float amount)
    {
        bool wasAlive = !target.EstaMuerto && target.vidaActual > 0f;
        ((IDamageable)target).ReceiveDamage(new DamageRequest(amount));
        return wasAlive && (target.EstaMuerto || target.vidaActual <= 0f);
    }

    private void ReportImpact(bool anyHit, bool anyKilled)
    {
        if (anyHit)
        {
            OnImpactoConfirmado?.Invoke(anyKilled);
        }
    }

    void Start()
    {
        player = GetComponent<PlayerController>();
        EnsureVariantEffects();
        // Preferir la cámara del PlayerController y después una cámara hija explícita.
        if (camara == null && player != null && player.camaraJugador != null)
        {
            camara = player.camaraJugador;
        }

        if (camara == null)
        {
            camara = GetComponentInChildren<Camera>();
        }

        EnsureMuzzle();
    }

    private void EnsureVariantEffects()
    {
        if (GetComponent<WeaponVariantEffects>() == null)
        {
            gameObject.AddComponent<WeaponVariantEffects>();
        }
    }

    void LateUpdate()
    {
        // Seguir a la cámara del jugador si cambió.
        if (player != null && player.camaraJugador != null && camara != player.camaraJugador)
        {
            camara = player.camaraJugador;
            puntoDisparo = player.puntoDisparo;
        }

        EnsureMuzzle();
        if (puntoDisparo != null && camara != null && puntoDisparo.parent == transform)
        {
            puntoDisparo.rotation = camara.transform.rotation;
        }
    }

    void EnsureMuzzle()
    {
        if (MuzzleTransformResolver.IsUsable(puntoDisparo, camara))
        {
            return;
        }

        puntoDisparo = player != null && MuzzleTransformResolver.IsUsable(player.puntoDisparo, camara)
            ? player.puntoDisparo
            : MuzzleTransformResolver.Ensure(transform, camara);
    }

    Ray GetAimRay()
    {
        if (camara != null)
        {
            return camara.ViewportPointToRay(new Vector3(CrosshairViewportCenter, CrosshairViewportCenter, 0f));
        }

        Transform origin = puntoDisparo != null ? puntoDisparo : transform;
        return new Ray(origin.position, origin.forward);
    }

    bool TryRaycastAim(Ray aimRay, float maxDistance, out RaycastHit hit)
    {
        hit = default;
        LayerMask effectiveMask = capasImpacto.value == 0 ? Physics.DefaultRaycastLayers : capasImpacto;
        Ray currentRay = aimRay;
        float remaining = maxDistance;

        for (int i = 0; i < MaxAimSelfIgnoreIterations; i++)
        {
            if (!Physics.Raycast(currentRay, out RaycastHit candidate, remaining, effectiveMask))
            {
                return false;
            }

            // Ignorar el propio cuerpo y el punto de disparo: el rayo nace dentro del jugador.
            bool isSelf = candidate.collider.GetComponentInParent<PlayerController>() == player
                || (puntoDisparo != null && candidate.collider.transform == puntoDisparo);
            if (!isSelf)
            {
                hit = candidate;
                return true;
            }

            float advance = candidate.distance + AimSelfIgnoreOffsetMeters;
            if (advance >= remaining)
            {
                return false;
            }

            currentRay = new Ray(candidate.point + currentRay.direction * AimSelfIgnoreOffsetMeters, currentRay.direction);
            remaining -= advance;
        }

        return false;
    }

    Vector3 GetMuzzlePosition()
    {
        if (puntoDisparo != null)
        {
            return puntoDisparo.position;
        }

        return camara != null ? camara.transform.position : transform.position;
    }

    void Update()
    {
        cooldownDisparo = Mathf.Max(0f, cooldownDisparo - Time.deltaTime);

        if (tiempoVarianteRestante > 0f)
        {
            tiempoVarianteRestante -= Time.deltaTime;
            if (tiempoVarianteRestante <= 0f)
            {
                ClearVariantState(true);
            }
        }
    }

    public void ConsumeCommand(PlayerCommand command)
    {
        if (command.WeaponSlot.HasValue)
        {
            switch (command.WeaponSlot.Value)
            {
                case FirstWeaponSlot:
                    CambiarArma(TipoArma.Directa);
                    break;
                case SecondWeaponSlot:
                    CambiarArma(TipoArma.Area);
                    break;
                case ThirdWeaponSlot:
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

        // El cambio de arma con la rueda del mouse queda diferido a su slice de input.
    }

    public void DispararActual()
    {
        if (cooldownDisparo > 0f)
        {
            return;
        }

        Arma actual = ObtenerArmaActual();
        if (actual == null)
        {
            return;
        }

        if (actual.municionActual == 0)
        {
            OnSinMunicion?.Invoke();
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
    }

    void DispararDirecto(Arma arma)
    {
        EnsureMuzzle();
        Vector3 muzzlePos = GetMuzzlePosition();
        Ray aimRay = GetAimRay();
        bool hasHit = TryRaycastAim(aimRay, arma.alcance, out RaycastHit aimHit);

        if (hasHit)
        {
            Enemy enemy = aimHit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                ReportImpact(true, ApplyDamage(enemy, DañoEfectivo(arma)));
                vfx.CreateImpact(aimHit.point, aimHit.normal, Color.red, DirectHitImpactSizeMeters, true);
            }
            else
            {
                vfx.CreateImpact(aimHit.point, aimHit.normal, Color.white, SurfaceImpactSizeMeters, false);
            }

            if (arma.prefabImpacto != null)
            {
                Instantiate(arma.prefabImpacto, aimHit.point, Quaternion.LookRotation(aimHit.normal));
            }

            vfx.CreateTracer(muzzlePos, aimHit.point, Color.red);
        }
        else
        {
            Vector3 missPoint = aimRay.origin + aimRay.direction * arma.alcance;
            vfx.CreateTracer(muzzlePos, muzzlePos + aimRay.direction * arma.alcance, MissTracerColor);
            vfx.CreateImpact(missPoint, -aimRay.direction, Color.gray, MissImpactSizeMeters, false);
        }

        if (arma.prefabProyectil == null)
        {
            vfx.CreateMuzzleFlash(puntoDisparo, Color.red);
            return;
        }

        Quaternion spawnRot = puntoDisparo != null ? puntoDisparo.rotation : Quaternion.LookRotation(aimRay.direction);
        GameObject proj = Instantiate(arma.prefabProyectil, muzzlePos, spawnRot);
        if (proj.TryGetComponent(out Rigidbody rb))
        {
            Vector3 dir = hasHit ? (aimHit.point - muzzlePos).normalized : aimRay.direction;
            rb.linearVelocity = dir * ProjectileSpeedMetersPerSecond;
        }
    }

    void DispararArea(Arma arma)
    {
        EnsureMuzzle();
        Vector3 muzzlePos = GetMuzzlePosition();
        Ray aimRay = GetAimRay();
        Vector3 puntoImpacto = TryRaycastAim(aimRay, arma.alcance, out RaycastHit hit)
            ? hit.point
            : aimRay.origin + aimRay.direction * arma.alcance;

        List<Enemy> afectados = OverlapQuery.FindUniqueInSphere<Enemy>(puntoImpacto, arma.radioArea);
        // El señuelo atrae enemigos sin dañarlos.
        if (!IsAreaVariantActive(VariantEffect.Decoy))
        {
            float daño = DañoEfectivo(arma);
            bool anyKilled = false;
            foreach (Enemy enemy in afectados)
            {
                anyKilled |= ApplyDamage(enemy, daño);
            }

            ReportImpact(afectados.Count > 0, anyKilled);
        }

        ApplyAreaVariantEffect(afectados, puntoImpacto);

        vfx.CreateAreaExplosion(puntoImpacto, arma.radioArea, afectados.Count > 0 ? Color.yellow : EmptyAreaExplosionColor);
        vfx.CreateTracer(muzzlePos, puntoImpacto, Color.yellow);
        if (arma.prefabImpacto != null)
        {
            Instantiate(arma.prefabImpacto, puntoImpacto, Quaternion.identity);
        }

        vfx.CreateMuzzleFlash(puntoDisparo, Color.yellow);
    }

    void AtacarMelee(Arma arma)
    {
        float daño = DañoEfectivo(arma);
        Vector3 centro = transform.position + transform.forward * MeleeForwardOffsetMeters;
        List<Enemy> afectados = OverlapQuery.FindUniqueInSphere<Enemy>(centro, arma.radioArea);
        bool anyKilled = false;
        foreach (Enemy enemy in afectados)
        {
            anyKilled |= ApplyDamage(enemy, daño);
            ApplyMeleeKnockback(enemy, arma);
        }

        ReportImpact(afectados.Count > 0, anyKilled);
    }

    private bool IsAreaVariantActive(VariantEffect effect)
    {
        return VarianteActiva
            && tipoVariante == TipoArma.Area
            && activeVariantEffect == effect;
    }

    private void ApplyAreaVariantEffect(IReadOnlyList<Enemy> affected, Vector3 impactPoint)
    {
        if (!VarianteActiva || tipoVariante != TipoArma.Area)
        {
            return;
        }

        switch (activeVariantEffect)
        {
            case VariantEffect.Decoy:
                OnDecoyRequested?.Invoke(impactPoint);
                break;
            case VariantEffect.Slowdown:
                foreach (Enemy enemy in affected)
                {
                    OnSlowdownRequested?.Invoke(enemy, slowdownDurationSeconds);
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

    private void ApplyMeleeKnockback(Enemy enemy, Arma arma)
    {
        float speed = ShouldApplyPushVariant(arma)
            ? pushStrikeKnockbackSpeedMetersPerSecond
            : meleeKnockbackSpeedMetersPerSecond;
        Vector3 velocityChange = CalculateKnockbackVelocity(
            transform.position,
            transform.forward,
            enemy.transform.position,
            speed,
            knockbackLiftRatio);
        enemy.ApplyKnockback(velocityChange, knockbackRecoverySeconds);
    }

    /// <summary>
    /// Calculates the knockback velocity that pushes a target away from the attacker.
    /// </summary>
    /// <param name="attackerPosition">The attacker's world position.</param>
    /// <param name="attackerForward">The fallback direction when both positions overlap.</param>
    /// <param name="targetPosition">The target's world position.</param>
    /// <param name="speedMetersPerSecond">The horizontal knockback speed.</param>
    /// <param name="liftRatio">The vertical speed as a ratio of the horizontal speed.</param>
    /// <returns>The velocity change to apply to the target.</returns>
    public static Vector3 CalculateKnockbackVelocity(
        Vector3 attackerPosition,
        Vector3 attackerForward,
        Vector3 targetPosition,
        float speedMetersPerSecond,
        float liftRatio)
    {
        Vector3 direction = targetPosition - attackerPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            direction = attackerForward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            return Vector3.zero;
        }

        Vector3 horizontal = direction.normalized * speedMetersPerSecond;
        return horizontal + (Vector3.up * (speedMetersPerSecond * liftRatio));
    }

    public void CambiarArma(TipoArma tipo)
    {
        if (armaEquipada == tipo)
        {
            return;
        }

        armaEquipada = tipo;
        OnCambioArma?.Invoke(tipo);
    }

    void CambiarArmaSiguiente()
    {
        int siguiente = ((int)armaEquipada + 1) % WeaponSlotCount;
        CambiarArma((TipoArma)siguiente);
    }

    void CambiarArmaAnterior()
    {
        int anterior = ((int)armaEquipada - 1 + WeaponSlotCount) % WeaponSlotCount;
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
    }

    public void ApplyVariant(TipoArma tipo, float multiplicador, float duracion)
    {
        activeVariant = WeaponVariant.PrecisionRifle;
        activeVariantEffect = VariantEffect.DamageMultiplier;
        hasSemanticVariant = false;
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
        multiplicadorVariante = Mathf.Max(MinimumVariantMultiplier, multiplicador);
        tiempoVarianteRestante = Mathf.Max(0f, duracion);
    }

    private void ClearVariantState(bool notify)
    {
        tiempoVarianteRestante = 0f;
        multiplicadorVariante = MinimumVariantMultiplier;
        tipoVariante = TipoArma.Directa;
        activeVariant = WeaponVariant.PrecisionRifle;
        activeVariantEffect = VariantEffect.DamageMultiplier;
        hasSemanticVariant = false;

        if (notify)
        {
            OnVarianteExpirada?.Invoke();
        }
    }

    private string GetActiveVariantDisplayName()
    {
        if (!VarianteActiva)
        {
            return string.Empty;
        }

        return hasSemanticVariant ? GetVariantDefinition(activeVariant).DisplayName : tipoVariante.ToString();
    }

    public float DañoEfectivo(Arma arma)
    {
        if (arma == null)
        {
            return 0f;
        }

        return VariantMultipliesDamage && arma.tipo == tipoVariante
            ? arma.daño * multiplicadorVariante
            : arma.daño;
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

    void OnDrawGizmosSelected()
    {
        if (armaEquipada == TipoArma.CuerpoACuerpo && armaMelee != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * MeleeForwardOffsetMeters, armaMelee.radioArea);
        }
    }
}
