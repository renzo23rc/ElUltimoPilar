using UnityEngine;

/// <summary>Coordinates P1's weapon selection with the unmodified draw-and-holster clip.</summary>
[DefaultExecutionOrder(20)]
[RequireComponent(typeof(WeaponSystem))]
public sealed class PlayerWeaponSwitchCoordinator : MonoBehaviour, IWeaponSwitchGate
{
    private const string FirearmLayerName = "P1 Firearm Upper Body";
    private const string ClipTimeParameter = "P1GrabTime";
    private const float DefaultReadyPoseSeconds = 3.25f;
    private const float DefaultRifleTransitionSeconds = 1.5f;
    private const float DefaultBazookaTransitionSeconds = 2f;
    private const float DefaultLayerBlendSeconds = 0.12f;

    private static readonly int ClipTimeId = Animator.StringToHash(ClipTimeParameter);

    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip drawAndHolsterClip;
    [SerializeField, Min(0.01f)] private float readyPoseSeconds = DefaultReadyPoseSeconds;
    [SerializeField, Min(0.01f), Tooltip("Seconds to draw or holster the rifle.")]
    private float rifleTransitionSeconds = DefaultRifleTransitionSeconds;
    [SerializeField, Min(0.01f), Tooltip("Seconds to draw or holster the bazooka.")]
    private float bazookaTransitionSeconds = DefaultBazookaTransitionSeconds;
    [SerializeField, Min(0.01f)] private float layerBlendSeconds = DefaultLayerBlendSeconds;

    private WeaponSystem weapons;
    private PlayerController player;
    private GameManager subscribedManager;
    private PlayerWeaponSequence sequence;
    private int firearmLayer = -1;
    private bool wasDowned;

    /// <summary>Gets whether the coordinator can safely handle selection requests.</summary>
    public bool IsAvailable => isActiveAndEnabled && sequence != null && firearmLayer >= 0;

    /// <summary>Gets whether P1 must wait before firing or attacking.</summary>
    public bool ActionBlocked => IsAvailable && (sequence.ActionBlocked || IsDowned);

    /// <summary>Gets the latest weapon selection, including a queued choice.</summary>
    public WeaponSystem.TipoArma RequestedWeapon => sequence != null ? sequence.RequestedWeapon : weapons.armaEquipada;

    /// <summary>Gets whether a firearm is selected and its draw or holster has finished.</summary>
    public bool FirearmReady => IsAvailable && !ActionBlocked && sequence.HasFirearm;

    private bool IsDowned => player != null && player.IsDowned;

    private void Awake()
    {
        weapons = GetComponent<WeaponSystem>();
        player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        if (!InitializeSequence())
        {
            return;
        }

        subscribedManager = GameManager.Instance;
        if (subscribedManager != null)
        {
            subscribedManager.OnJuegoIniciado += HandleMatchStarted;
            if (subscribedManager.juegoActivo)
            {
                sequence.BeginDraw();
            }
        }

        ApplyAnimatorPose(true);
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnJuegoIniciado -= HandleMatchStarted;
            subscribedManager = null;
        }

        if (animator != null && firearmLayer >= 0 && animator.isActiveAndEnabled)
        {
            animator.SetLayerWeight(firearmLayer, 0f);
        }

        sequence = null;
        wasDowned = false;
    }

    private void Update()
    {
        if (!IsAvailable)
        {
            return;
        }

        if (IsDowned)
        {
            if (!wasDowned)
            {
                sequence.Reset(weapons.armaEquipada);
                wasDowned = true;
            }

            ApplyAnimatorPose(false);
            return;
        }

        if (wasDowned)
        {
            sequence.Reset(weapons.armaEquipada);
            wasDowned = false;
        }

        if (sequence.Advance(Time.deltaTime, out WeaponSystem.TipoArma committed))
        {
            weapons.CommitWeaponChange(committed);
        }

        ApplyAnimatorPose(false);
    }

    /// <summary>Queues the latest choice or commits it immediately when no firearm needs holstering.</summary>
    public void RequestWeapon(WeaponSystem.TipoArma weapon)
    {
        if (!IsAvailable || IsDowned)
        {
            return;
        }

        if (sequence.Request(weapon, out WeaponSystem.TipoArma committed))
        {
            weapons.CommitWeaponChange(committed);
        }

        ApplyAnimatorPose(false);
    }

    /// <summary>Cancels pending changes and synchronizes with the gameplay weapon.</summary>
    public void ResetTransition()
    {
        if (sequence == null || weapons == null)
        {
            return;
        }

        sequence.Reset(weapons.armaEquipada);
        wasDowned = false;
        ApplyAnimatorPose(true);
    }

    private bool InitializeSequence()
    {
        if (weapons == null)
        {
            weapons = GetComponent<WeaponSystem>();
        }

        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }

        if (animator == null || animator.runtimeAnimatorController == null || drawAndHolsterClip == null)
        {
            Debug.LogError("[PlayerWeaponSwitchCoordinator] Missing P1 Animator or draw clip.", this);
            return false;
        }

        firearmLayer = animator.GetLayerIndex(FirearmLayerName);
        if (firearmLayer < 0 || readyPoseSeconds <= 0f || readyPoseSeconds >= drawAndHolsterClip.length
            || rifleTransitionSeconds <= 0f || bazookaTransitionSeconds <= 0f)
        {
            Debug.LogError("[PlayerWeaponSwitchCoordinator] Missing P1 firearm layer or invalid pose or transition duration.", this);
            return false;
        }

        sequence = new PlayerWeaponSequence(
            weapons.armaEquipada,
            readyPoseSeconds,
            drawAndHolsterClip.length,
            rifleTransitionSeconds,
            bazookaTransitionSeconds);
        return true;
    }

    private void HandleMatchStarted()
    {
        if (!IsAvailable)
        {
            return;
        }

        sequence.Reset(weapons.armaEquipada);
        sequence.BeginDraw();
        ApplyAnimatorPose(true);
    }

    private void ApplyAnimatorPose(bool immediate)
    {
        if (animator == null || firearmLayer < 0 || sequence == null)
        {
            return;
        }

        animator.SetFloat(ClipTimeId, sequence.ClipTimeSeconds / drawAndHolsterClip.length);
        float targetWeight = sequence.HasFirearm && !IsDowned ? 1f : 0f;
        float weight = immediate
            ? targetWeight
            : Mathf.MoveTowards(animator.GetLayerWeight(firearmLayer), targetWeight, Time.deltaTime / layerBlendSeconds);
        animator.SetLayerWeight(firearmLayer, weight);
    }
}
