using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the Animator of an enemy's visual model through the shared parameter set.
/// It is safe to use when the model has no Animator or lacks a parameter: the call is skipped.
/// </summary>
public sealed class EnemyAnimationDriver
{
    private const string DeathClipNameFragment = "dying";
    private const string DeathClipShortNameFragment = "die";

    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int PunchHash = Animator.StringToHash("Punch");
    private static readonly int ScreamHash = Animator.StringToHash("Scream");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int RunHash = Animator.StringToHash("Run");

    private readonly Animator animator;
    private readonly HashSet<int> availableParameters = new();

    /// <summary>Creates a driver for an Animator; a null Animator makes every call a no-op.</summary>
    /// <param name="animator">The Animator of the visual model, or null when it has none.</param>
    public EnemyAnimationDriver(Animator animator)
    {
        bool isDrivable = animator != null && animator.runtimeAnimatorController != null;
        this.animator = isDrivable ? animator : null;
        if (!isDrivable)
        {
            return;
        }

        // El movimiento lo hace el Rigidbody del script: el clip no debe desplazar al enemigo.
        animator.applyRootMotion = false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            availableParameters.Add(parameter.nameHash);
        }
    }

    /// <summary>Gets whether there is an Animator to drive.</summary>
    public bool HasAnimator => animator != null;

    /// <summary>Plays the main attack.</summary>
    public void TriggerAttack() => Trigger(Attack1Hash);

    /// <summary>Plays the area punch.</summary>
    public void TriggerPunch() => Trigger(PunchHash);

    /// <summary>Plays the scream used for fields and detonation warnings.</summary>
    public void TriggerScream() => Trigger(ScreamHash);

    /// <summary>Plays the death clip.</summary>
    public void TriggerDie() => Trigger(DieHash);

    /// <summary>Sets the standing (true) or walking (false) pose.</summary>
    public void SetIdle(bool isIdle) => SetBool(IdleHash, isIdle);

    /// <summary>Sets the running pose.</summary>
    public void SetRun(bool isRunning) => SetBool(RunHash, isRunning);

    /// <summary>Gets how long the death clip lasts, or the fallback when the model has none.</summary>
    /// <param name="fallbackSeconds">The duration to use when no death clip is found.</param>
    public float GetDeathDurationSeconds(float fallbackSeconds)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return fallbackSeconds;
        }

        return FindDeathClipLength(animator.runtimeAnimatorController.animationClips, fallbackSeconds);
    }

    /// <summary>Finds the death clip by name and returns its length.</summary>
    /// <param name="clips">The clips of the controller.</param>
    /// <param name="fallbackSeconds">The value returned when no clip matches.</param>
    public static float FindDeathClipLength(IReadOnlyList<AnimationClip> clips, float fallbackSeconds)
    {
        AnimationClip shortNameMatch = null;
        for (int i = 0; i < clips.Count; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string clipName = clip.name.ToLowerInvariant();
            if (clipName.Contains(DeathClipNameFragment))
            {
                return clip.length;
            }

            if (shortNameMatch == null && clipName.Contains(DeathClipShortNameFragment))
            {
                shortNameMatch = clip;
            }
        }

        return shortNameMatch != null ? shortNameMatch.length : fallbackSeconds;
    }

    private void Trigger(int hash)
    {
        if (availableParameters.Contains(hash))
        {
            animator.SetTrigger(hash);
        }
    }

    private void SetBool(int hash, bool value)
    {
        if (availableParameters.Contains(hash))
        {
            animator.SetBool(hash, value);
        }
    }
}
