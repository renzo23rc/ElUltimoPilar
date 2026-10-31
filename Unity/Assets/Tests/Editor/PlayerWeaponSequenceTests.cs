using NUnit.Framework;

public sealed class PlayerWeaponSequenceTests
{
    private const float ReadySeconds = 3.25f;
    private const float ClipSeconds = 7.5f;
    private const float ToleranceSeconds = 0.0001f;

    [Test]
    public void FirearmStaysReadyUntilAnotherWeaponIsRequested()
    {
        var sequence = new PlayerWeaponSequence(WeaponSystem.TipoArma.Directa, ReadySeconds, ClipSeconds);
        Assert.That(sequence.Advance(20f, out _), Is.False);
        Assert.That(sequence.ClipTimeSeconds, Is.EqualTo(ReadySeconds));
        Assert.That(sequence.ActionBlocked, Is.False);
        Assert.That(sequence.CurrentWeapon, Is.EqualTo(WeaponSystem.TipoArma.Directa));
    }

    [Test]
    public void FirearmChangeWaitsForHolsterThenBlocksUntilNewFirearmIsDrawn()
    {
        var sequence = new PlayerWeaponSequence(WeaponSystem.TipoArma.Directa, ReadySeconds, ClipSeconds);
        Assert.That(sequence.Request(WeaponSystem.TipoArma.Area, out _), Is.False);
        Assert.That(sequence.ActionBlocked, Is.True);
        Assert.That(sequence.Advance(4f, out _), Is.False);
        Assert.That(sequence.CurrentWeapon, Is.EqualTo(WeaponSystem.TipoArma.Directa));
        Assert.That(sequence.Advance(0.3f, out WeaponSystem.TipoArma changed), Is.True);
        Assert.That(changed, Is.EqualTo(WeaponSystem.TipoArma.Area));
        Assert.That(sequence.CurrentWeapon, Is.EqualTo(WeaponSystem.TipoArma.Area));
        Assert.That(sequence.ClipTimeSeconds, Is.EqualTo(0.05f).Within(ToleranceSeconds));
        Assert.That(sequence.ActionBlocked, Is.True);
        Assert.That(sequence.Advance(3.2f, out _), Is.False);
        Assert.That(sequence.ActionBlocked, Is.False);
        Assert.That(sequence.ClipTimeSeconds, Is.EqualTo(ReadySeconds));
    }

    [Test]
    public void LastSelectionWinsAndSelectingCurrentWeaponReversesHolster()
    {
        var sequence = new PlayerWeaponSequence(WeaponSystem.TipoArma.Directa, ReadySeconds, ClipSeconds);
        sequence.Request(WeaponSystem.TipoArma.Area, out _);
        sequence.Advance(1f, out _);
        sequence.Request(WeaponSystem.TipoArma.CuerpoACuerpo, out _);
        Assert.That(sequence.RequestedWeapon, Is.EqualTo(WeaponSystem.TipoArma.CuerpoACuerpo));
        sequence.Request(WeaponSystem.TipoArma.Directa, out _);
        Assert.That(sequence.ActionBlocked, Is.True);
        Assert.That(sequence.Advance(1f, out _), Is.False);
        Assert.That(sequence.CurrentWeapon, Is.EqualTo(WeaponSystem.TipoArma.Directa));
        Assert.That(sequence.ClipTimeSeconds, Is.EqualTo(ReadySeconds));
        Assert.That(sequence.ActionBlocked, Is.False);
    }

    [Test]
    public void MeleeSwitchesImmediatelyAndNewFirearmDrawsBeforeAttacks()
    {
        var sequence = new PlayerWeaponSequence(WeaponSystem.TipoArma.CuerpoACuerpo, ReadySeconds, ClipSeconds);
        Assert.That(sequence.Request(WeaponSystem.TipoArma.Area, out WeaponSystem.TipoArma changed), Is.True);
        Assert.That(changed, Is.EqualTo(WeaponSystem.TipoArma.Area));
        Assert.That(sequence.ActionBlocked, Is.True);
        sequence.Advance(ReadySeconds, out _);
        Assert.That(sequence.ActionBlocked, Is.False);
    }

    [Test]
    public void RequestDuringDrawingIsProcessedAfterDrawAndHolster()
    {
        var sequence = new PlayerWeaponSequence(WeaponSystem.TipoArma.CuerpoACuerpo, ReadySeconds, ClipSeconds);
        sequence.Request(WeaponSystem.TipoArma.Directa, out _);
        sequence.Request(WeaponSystem.TipoArma.Area, out _);
        Assert.That(sequence.Advance(ReadySeconds, out _), Is.False);
        Assert.That(sequence.ActionBlocked, Is.True);
        Assert.That(sequence.Advance(ClipSeconds - ReadySeconds, out WeaponSystem.TipoArma changed), Is.True);
        Assert.That(changed, Is.EqualTo(WeaponSystem.TipoArma.Area));
        Assert.That(sequence.ActionBlocked, Is.True);
    }

    [Test]
    public void RifleAndBazookaUseTheirOwnDrawAndHolsterDurations()
    {
        var sequence = new PlayerWeaponSequence(
            WeaponSystem.TipoArma.Directa, ReadySeconds, ClipSeconds, 1.5f, 2f);
        sequence.Request(WeaponSystem.TipoArma.Area, out _);
        Assert.That(sequence.Advance(1.49f, out _), Is.False);
        Assert.That(sequence.Advance(0.01f, out WeaponSystem.TipoArma changed), Is.True);
        Assert.That(changed, Is.EqualTo(WeaponSystem.TipoArma.Area));
        Assert.That(sequence.ActionBlocked, Is.True);
        sequence.Advance(1.99f, out _);
        Assert.That(sequence.ActionBlocked, Is.True);
        sequence.Advance(0.01f, out _);
        Assert.That(sequence.ActionBlocked, Is.False);
        Assert.That(sequence.ClipTimeSeconds, Is.EqualTo(ReadySeconds));

        sequence.Request(WeaponSystem.TipoArma.CuerpoACuerpo, out _);
        Assert.That(sequence.Advance(1.99f, out _), Is.False);
        Assert.That(sequence.Advance(0.01f, out changed), Is.True);
        Assert.That(changed, Is.EqualTo(WeaponSystem.TipoArma.CuerpoACuerpo));
        sequence.Request(WeaponSystem.TipoArma.Directa, out _);
        sequence.Advance(1.49f, out _);
        Assert.That(sequence.ActionBlocked, Is.True);
        sequence.Advance(0.01f, out _);
        Assert.That(sequence.ActionBlocked, Is.False);
    }

    [Test]
    public void ResetCancelsPendingTransitionAndReleasesAttackLock()
    {
        var sequence = new PlayerWeaponSequence(WeaponSystem.TipoArma.Directa, ReadySeconds, ClipSeconds);
        sequence.Request(WeaponSystem.TipoArma.Area, out _);
        sequence.Advance(1f, out _);
        sequence.Reset(WeaponSystem.TipoArma.Directa);
        Assert.That(sequence.ActionBlocked, Is.False);
        Assert.That(sequence.RequestedWeapon, Is.EqualTo(WeaponSystem.TipoArma.Directa));
        Assert.That(sequence.ClipTimeSeconds, Is.EqualTo(ReadySeconds));
    }
}
