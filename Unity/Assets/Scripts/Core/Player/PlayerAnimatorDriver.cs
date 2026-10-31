using UnityEngine;

/// <summary>
/// Feeds the locomotion Animator of a skinned player model with speed, ground contact and downed state.
/// Put it on the player root; the Animator lives on the visual child.
/// </summary>
public sealed class PlayerAnimatorDriver : MonoBehaviour
{
    private const string SpeedParameter = "Speed";
    private const string GroundedParameter = "Grounded";
    private const string DownedParameter = "Downed";
    private const string ShootParameter = "Shoot";
    private const string EquipParameter = "Equip";
    private const string KnifeParameter = "Knife1";
    private const string SwingParameter = "Swing";
    private const string FloatingParameter = "Floating";
    private const int FirstSwingIndex = 0;
    private const int SecondSwingIndex = 1;
    private const float SpeedDampSeconds = 0.1f;
    private const float GroundProbeMarginMeters = 0.3f;
    private const float HalfFactor = 0.5f;
    private const float MinimumFacingSpeedMetersPerSecond = 0.5f;
    private const float BodyTurnDegreesPerSecond = 720f;

    private static readonly int SpeedId = Animator.StringToHash(SpeedParameter);
    private static readonly int GroundedId = Animator.StringToHash(GroundedParameter);
    private static readonly int DownedId = Animator.StringToHash(DownedParameter);
    private static readonly int ShootId = Animator.StringToHash(ShootParameter);
    private static readonly int EquipId = Animator.StringToHash(EquipParameter);
    private static readonly int KnifeId = Animator.StringToHash(KnifeParameter);
    private static readonly int SwingId = Animator.StringToHash(SwingParameter);
    private static readonly int FloatingId = Animator.StringToHash(FloatingParameter);

    [SerializeField] private Animator animator;

    private PlayerController player;
    private WeaponSystem weapons;
    private GameManager subscribedManager;
    private CharacterController characterController;
    private Vector3 lastPosition;
    private Quaternion baseBodyRotation;
    private float bodyYawDegrees;
    private float movementYawDegrees;
    private int swingIndex = SecondSwingIndex;
    private bool equipRequested;

    /// <summary>Sets the Animator to drive.</summary>
    /// <param name="target">The Animator of the player model.</param>
    public void Configure(Animator target)
    {
        animator = target;
        if (animator != null)
        {
            animator.applyRootMotion = false;
            baseBodyRotation = animator.transform.localRotation;
        }

        bodyYawDegrees = 0f;
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        weapons = GetComponent<WeaponSystem>();
        if (animator != null)
        {
            baseBodyRotation = animator.transform.localRotation;
        }
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        if (animator != null)
        {
            animator.applyRootMotion = false;
            bodyYawDegrees = 0f;
        }

        if (weapons != null)
        {
            weapons.OnDisparo += HandleWeaponFired;
            weapons.OnMeleeResolved += HandleMeleeResolved;
            weapons.OnCambioArma += HandleWeaponChanged;
        }

        subscribedManager = GameManager.Instance;
        if (subscribedManager != null)
        {
            subscribedManager.OnJuegoIniciado += HandleMatchStarted;
            if (subscribedManager.juegoActivo && weapons != null
                && weapons.armaEquipada == WeaponSystem.TipoArma.Directa)
            {
                equipRequested = true;
            }
        }
    }

    private void OnDisable()
    {
        equipRequested = false;
        if (weapons != null)
        {
            weapons.OnDisparo -= HandleWeaponFired;
            weapons.OnMeleeResolved -= HandleMeleeResolved;
            weapons.OnCambioArma -= HandleWeaponChanged;
        }

        if (subscribedManager != null)
        {
            subscribedManager.OnJuegoIniciado -= HandleMatchStarted;
            subscribedManager = null;
        }
    }

    private void LateUpdate()
    {
        if (animator == null || animator.runtimeAnimatorController == null || Time.deltaTime <= 0f)
        {
            return;
        }

        Vector3 displacement = transform.position - lastPosition;
        lastPosition = transform.position;
        displacement.y = 0f;
        float speed = displacement.magnitude / Time.deltaTime;

        animator.SetFloat(SpeedId, speed, SpeedDampSeconds, Time.deltaTime);
        TurnBodyTowardMovement(displacement, animator.GetFloat(SpeedId));
        bool grounded = IsGrounded();
        bool downed = player != null && player.IsDowned;
        bool floating = player != null && player.enZonaGravedad;
        animator.SetBool(GroundedId, grounded);
        animator.SetBool(DownedId, downed);
        animator.SetBool(FloatingId, floating);
        if (!grounded || downed || floating)
        {
            // No attack transition can consume this trigger while airborne or downed.
            animator.ResetTrigger(KnifeId);
        }

        if (equipRequested)
        {
            equipRequested = false;
            animator.SetTrigger(EquipId);
        }
    }

    // CharacterController.isGrounded vale false mientras el motor no se mueve (antes de la partida): se sondea el piso.
    private bool IsGrounded()
    {
        if (characterController == null)
        {
            return true;
        }

        Vector3 origin = transform.TransformPoint(characterController.center);
        float distance = (characterController.height * HalfFactor) + GroundProbeMarginMeters;
        return Physics.Raycast(origin, Vector3.down, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    // Solo hay clips hacia adelante: el cuerpo gira hacia donde se mueve mientras la cámara sigue apuntando.
    private void TurnBodyTowardMovement(Vector3 worldDisplacement, float smoothedSpeed)
    {
        if (worldDisplacement.sqrMagnitude > Mathf.Epsilon)
        {
            Vector3 local = transform.InverseTransformDirection(worldDisplacement);
            movementYawDegrees = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        float targetYaw = smoothedSpeed >= MinimumFacingSpeedMetersPerSecond ? movementYawDegrees : 0f;
        bodyYawDegrees = Mathf.MoveTowardsAngle(bodyYawDegrees, targetYaw, BodyTurnDegreesPerSecond * Time.deltaTime);
        animator.transform.localRotation = baseBodyRotation * Quaternion.Euler(0f, bodyYawDegrees, 0f);
    }

    private void HandleWeaponFired(WeaponSystem.Arma weapon)
    {
        if (weapon != null && weapon.tipo == WeaponSystem.TipoArma.CuerpoACuerpo)
        {
            return;
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger(ShootId);
        }
    }

    // Cada tajo alterna entre los clips knife y knife2 mediante Swing, pegue o no.
    private void HandleMeleeResolved(bool hitAnyEnemy)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        swingIndex = swingIndex == FirstSwingIndex ? SecondSwingIndex : FirstSwingIndex;
        animator.SetInteger(SwingId, swingIndex);
        animator.SetTrigger(KnifeId);
    }

    private void HandleMatchStarted()
    {
        if (weapons == null || weapons.armaEquipada == WeaponSystem.TipoArma.Directa)
        {
            equipRequested = true;
        }
    }

    private void HandleWeaponChanged(WeaponSystem.TipoArma weapon)
    {
        equipRequested = weapon == WeaponSystem.TipoArma.Directa;
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (!equipRequested)
        {
            animator.ResetTrigger(EquipId);
        }

        if (weapon != WeaponSystem.TipoArma.CuerpoACuerpo)
        {
            animator.ResetTrigger(KnifeId);
        }
    }
}
