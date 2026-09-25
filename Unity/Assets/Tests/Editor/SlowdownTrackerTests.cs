using System;
using NUnit.Framework;
using UltimoPilar.Core.Combat;

public class SlowdownTrackerTests
{
    private const float Now = 10f;
    private const float Duration = 4f;
    private const float StrongFactor = 0.15f;
    private const float WeakFactor = 0.5f;
    private const float Tolerance = 0.0001f;

    [Test]
    public void WithoutSlowdownsFactorIsNeutral()
    {
        var tracker = new SlowdownTracker();

        Assert.That(tracker.GetFactor(Now), Is.EqualTo(SlowdownTracker.NoSlowdownFactor));
        Assert.That(tracker.IsActive(Now), Is.False);
    }

    [Test]
    public void AppliedSlowdownIsActiveUntilItExpires()
    {
        var tracker = new SlowdownTracker();
        tracker.Apply(new object(), WeakFactor, Duration, Now);

        Assert.That(tracker.GetFactor(Now + Duration - Tolerance), Is.EqualTo(WeakFactor));
        Assert.That(tracker.GetFactor(Now + Duration), Is.EqualTo(SlowdownTracker.NoSlowdownFactor));
        Assert.That(tracker.HasEntries, Is.False);
    }

    [Test]
    public void OverlappingSourcesDoNotStackAndStrongestWins()
    {
        var tracker = new SlowdownTracker();
        tracker.Apply(new object(), WeakFactor, Duration, Now);
        tracker.Apply(new object(), StrongFactor, Duration, Now);

        Assert.That(tracker.GetFactor(Now), Is.EqualTo(StrongFactor).Within(Tolerance));
    }

    [Test]
    public void RemovingOneSourceKeepsTheOthers()
    {
        var tracker = new SlowdownTracker();
        var zone = new object();
        var weaponVariant = new object();
        tracker.Apply(zone, WeakFactor, Duration, Now);
        tracker.Apply(weaponVariant, StrongFactor, Duration, Now);

        Assert.That(tracker.Remove(weaponVariant), Is.True);

        Assert.That(tracker.GetFactor(Now), Is.EqualTo(WeakFactor));
    }

    [Test]
    public void ReapplyingTheSameSourceRefreshesInsteadOfStacking()
    {
        var tracker = new SlowdownTracker();
        var source = new object();
        tracker.Apply(source, StrongFactor, Duration, Now);
        tracker.Apply(source, WeakFactor, Duration, Now + Duration - Tolerance);

        Assert.That(tracker.GetFactor(Now + Duration), Is.EqualTo(WeakFactor));
    }

    [TestCase(-1f, 0f)]
    [TestCase(2f, 1f)]
    public void FactorIsClampedBetweenZeroAndOne(float requestedFactor, float expectedFactor)
    {
        var tracker = new SlowdownTracker();
        tracker.Apply(new object(), requestedFactor, Duration, Now);

        Assert.That(tracker.GetFactor(Now), Is.EqualTo(expectedFactor));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    public void NonPositiveDurationIsIgnored(float duration)
    {
        var tracker = new SlowdownTracker();
        tracker.Apply(new object(), StrongFactor, duration, Now);

        Assert.That(tracker.HasEntries, Is.False);
    }

    [Test]
    public void ClearRemovesEverySource()
    {
        var tracker = new SlowdownTracker();
        tracker.Apply(new object(), WeakFactor, Duration, Now);
        tracker.Apply(new object(), StrongFactor, Duration, Now);

        tracker.Clear();

        Assert.That(tracker.IsActive(Now), Is.False);
    }

    [Test]
    public void NullSourceIsRejected()
    {
        var tracker = new SlowdownTracker();

        Assert.Throws<ArgumentNullException>(() => tracker.Apply(null, WeakFactor, Duration, Now));
        Assert.That(tracker.Remove(null), Is.False);
    }
}
