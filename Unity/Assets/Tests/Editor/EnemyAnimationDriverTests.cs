using NUnit.Framework;
using UnityEngine;

public class EnemyAnimationDriverTests
{
    private const float FallbackSeconds = 0.1f;
    private const float DyingSeconds = 2.5f;
    private const float OtherSeconds = 1f;

    [Test]
    public void ClipNamedDyingSetsTheDeathDuration()
    {
        var walk = CreateClip("walking", OtherSeconds);
        var dying = CreateClip("Zombie_Dying", DyingSeconds);

        float duration = EnemyAnimationDriver.FindDeathClipLength(new[] { walk, dying }, FallbackSeconds);

        Assert.That(duration, Is.EqualTo(DyingSeconds).Within(0.01f));
    }

    [Test]
    public void MissingDeathClipUsesTheFallback()
    {
        var walk = CreateClip("walking", OtherSeconds);

        float duration = EnemyAnimationDriver.FindDeathClipLength(new[] { walk }, FallbackSeconds);

        Assert.That(duration, Is.EqualTo(FallbackSeconds));
    }

    [Test]
    public void DriverWithoutAnimatorIsSafeAndUsesTheFallback()
    {
        var driver = new EnemyAnimationDriver(null);

        Assert.DoesNotThrow(() =>
        {
            driver.TriggerDie();
            driver.SetIdle(true);
        });
        Assert.That(driver.GetDeathDurationSeconds(FallbackSeconds), Is.EqualTo(FallbackSeconds));
    }

    private static AnimationClip CreateClip(string clipName, float lengthSeconds)
    {
        var clip = new AnimationClip { name = clipName };
        clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, lengthSeconds, 1f));
        return clip;
    }
}
