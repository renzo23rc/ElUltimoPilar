using NUnit.Framework;
using UnityEngine;

public class KnockbackVelocityTests
{
    private const float SpeedMetersPerSecond = 10f;
    private const float LiftRatio = 0.3f;
    private const float NoLift = 0f;
    private const float Tolerance = 0.0001f;

    [Test]
    public void PushesTargetAwayFromAttackerOnHorizontalPlane()
    {
        Vector3 velocity = WeaponSystem.CalculateKnockbackVelocity(
            Vector3.zero,
            Vector3.forward,
            new Vector3(3f, 0f, 0f),
            SpeedMetersPerSecond,
            NoLift);

        Assert.That(velocity.x, Is.EqualTo(SpeedMetersPerSecond).Within(Tolerance));
        Assert.That(velocity.y, Is.EqualTo(0f).Within(Tolerance));
        Assert.That(velocity.z, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void IgnoresHeightDifferenceForHorizontalDirection()
    {
        Vector3 velocity = WeaponSystem.CalculateKnockbackVelocity(
            Vector3.zero,
            Vector3.forward,
            new Vector3(0f, 5f, 2f),
            SpeedMetersPerSecond,
            NoLift);

        Assert.That(velocity.z, Is.EqualTo(SpeedMetersPerSecond).Within(Tolerance));
        Assert.That(velocity.y, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void AddsLiftProportionalToSpeed()
    {
        Vector3 velocity = WeaponSystem.CalculateKnockbackVelocity(
            Vector3.zero,
            Vector3.forward,
            new Vector3(0f, 0f, 2f),
            SpeedMetersPerSecond,
            LiftRatio);

        Assert.That(velocity.y, Is.EqualTo(SpeedMetersPerSecond * LiftRatio).Within(Tolerance));
        Assert.That(velocity.z, Is.EqualTo(SpeedMetersPerSecond).Within(Tolerance));
    }

    [Test]
    public void FallsBackToAttackerForwardWhenPositionsOverlap()
    {
        Vector3 velocity = WeaponSystem.CalculateKnockbackVelocity(
            Vector3.one,
            Vector3.left,
            Vector3.one,
            SpeedMetersPerSecond,
            NoLift);

        Assert.That(velocity.x, Is.EqualTo(-SpeedMetersPerSecond).Within(Tolerance));
    }

    [Test]
    public void ReturnsZeroWhenNoHorizontalDirectionExists()
    {
        Vector3 velocity = WeaponSystem.CalculateKnockbackVelocity(
            Vector3.zero,
            Vector3.up,
            Vector3.zero,
            SpeedMetersPerSecond,
            LiftRatio);

        Assert.That(velocity, Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void ZeroSpeedProducesNoKnockback()
    {
        Vector3 velocity = WeaponSystem.CalculateKnockbackVelocity(
            Vector3.zero,
            Vector3.forward,
            new Vector3(1f, 0f, 1f),
            0f,
            LiftRatio);

        Assert.That(velocity.magnitude, Is.EqualTo(0f).Within(Tolerance));
    }
}
