using System;
using NUnit.Framework;

public class WaveIntermissionTests
{
    private const float PauseSeconds = 7f;

    [Test]
    public void StartsIdle()
    {
        var intermission = new WaveIntermission();

        Assert.That(intermission.IsWaiting, Is.False);
        Assert.That(intermission.Tick(PauseSeconds), Is.False);
    }

    [Test]
    public void FinishesOnceWhenTheTimeRunsOut()
    {
        var intermission = new WaveIntermission();
        intermission.Begin(PauseSeconds);

        Assert.That(intermission.Tick(PauseSeconds - 1f), Is.False);
        Assert.That(intermission.IsWaiting, Is.True);
        Assert.That(intermission.Tick(1f), Is.True);
        Assert.That(intermission.IsWaiting, Is.False);
        Assert.That(intermission.Tick(1f), Is.False);
    }

    [Test]
    public void ZeroDurationFinishesOnTheFirstTick()
    {
        var intermission = new WaveIntermission();
        intermission.Begin(0f);

        Assert.That(intermission.Tick(0f), Is.True);
    }

    [Test]
    public void CancelStopsWithoutFinishing()
    {
        var intermission = new WaveIntermission();
        intermission.Begin(PauseSeconds);

        intermission.Cancel();

        Assert.That(intermission.IsWaiting, Is.False);
        Assert.That(intermission.RemainingSeconds, Is.EqualTo(0f));
        Assert.That(intermission.Tick(PauseSeconds), Is.False);
    }

    [Test]
    public void NaNDurationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WaveIntermission().Begin(float.NaN));
    }
}
