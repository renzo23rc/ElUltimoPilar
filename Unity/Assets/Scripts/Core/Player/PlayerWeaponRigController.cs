using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>Aligns P1's held firearm with the camera and the supporting hand with its grip.</summary>
[DefaultExecutionOrder(30)]
public sealed class PlayerWeaponRigController : MonoBehaviour
{
    private const string GroundedParameter = "Grounded";
    private const float DefaultMaximumPitchDegrees = 45f;
    private const float DefaultAimDistanceMeters = 10f;
    private const float DefaultBlendSeconds = 0.15f;
    private const float MinimumDirectionSquared = 0.0001f;

    private static readonly int GroundedId = Animator.StringToHash(GroundedParameter);

    [SerializeField] private Animator animator;
    [SerializeField] private RigBuilder rigBuilder;
    [SerializeField] private MultiAimConstraint torsoAim;
    [SerializeField] private TwoBoneIKConstraint primaryHand;
    [SerializeField] private TwoBoneIKConstraint rifleSupport;
    [SerializeField] private TwoBoneIKConstraint areaSupport;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform upperSpine;
    [SerializeField] private Vector3 neutralSpineDirection = Vector3.forward;
    [SerializeField, Range(0f, 80f)] private float maximumPitchDegrees = DefaultMaximumPitchDegrees;
    [SerializeField, Min(0.1f)] private float aimDistanceMeters = DefaultAimDistanceMeters;
    [SerializeField, Min(0.01f)] private float blendSeconds = DefaultBlendSeconds;

    private PlayerController player;
    private WeaponSystem weapons;
    private PlayerWeaponSwitchCoordinator coordinator;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        weapons = GetComponent<WeaponSystem>();
        coordinator = GetComponent<PlayerWeaponSwitchCoordinator>();
    }

    private void OnDisable()
    {
        SetWeights(0f, 0f, 0f, 0f);
    }

    private void Update()
    {
        if (animator == null || rigBuilder == null || torsoAim == null || primaryHand == null || rifleSupport == null
            || areaSupport == null || aimTarget == null || upperSpine == null
            || player == null || weapons == null || coordinator == null)
        {
            return;
        }

        UpdateAimTarget();
        bool grounded = animator.GetBool(GroundedId);
        bool active = coordinator.FirearmReady && grounded && !player.enZonaGravedad;
        float step = Time.deltaTime / blendSeconds;
        float aimWeight = Mathf.MoveTowards(torsoAim.weight, active ? 1f : 0f, step);
        float primaryWeight = Mathf.MoveTowards(primaryHand.weight, active ? 1f : 0f, step);
        float rifleWeight = Mathf.MoveTowards(rifleSupport.weight,
            active && weapons.armaEquipada == WeaponSystem.TipoArma.Directa ? 1f : 0f, step);
        float areaWeight = Mathf.MoveTowards(areaSupport.weight,
            active && weapons.armaEquipada == WeaponSystem.TipoArma.Area ? 1f : 0f, step);
        SetWeights(aimWeight, primaryWeight, rifleWeight, areaWeight);
        rigBuilder.SyncLayers();
    }

    private void UpdateAimTarget()
    {
        Camera camera = player.camaraJugador;
        if (camera == null)
        {
            return;
        }

        Vector3 cameraDirection = transform.InverseTransformDirection(camera.transform.forward);
        Vector3 planarDirection = new Vector3(cameraDirection.x, 0f, cameraDirection.z);
        if (planarDirection.sqrMagnitude < MinimumDirectionSquared)
        {
            planarDirection = Vector3.forward;
        }

        float yawDegrees = Mathf.Atan2(planarDirection.x, planarDirection.z) * Mathf.Rad2Deg;
        float pitchDegrees = Mathf.Atan2(-cameraDirection.y, planarDirection.magnitude) * Mathf.Rad2Deg;
        pitchDegrees = Mathf.Clamp(pitchDegrees, -maximumPitchDegrees, maximumPitchDegrees);
        Quaternion aimedRotation = transform.rotation * Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        Vector3 direction = aimedRotation * neutralSpineDirection.normalized;
        aimTarget.position = upperSpine.position + (direction * aimDistanceMeters);
    }

    private void SetWeights(float aim, float primary, float rifle, float area)
    {
        if (torsoAim != null)
        {
            torsoAim.weight = aim;
        }

        if (primaryHand != null)
        {
            primaryHand.weight = primary;
        }

        if (rifleSupport != null)
        {
            rifleSupport.weight = rifle;
        }

        if (areaSupport != null)
        {
            areaSupport.weight = area;
        }
    }
}
