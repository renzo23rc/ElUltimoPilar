using System;
using NUnit.Framework;
using UltimoPilar.Core.Shared;

public class LocomotionPoseModelTests
{
    private const float ReferenceSpeed = 5f;
    private const float FrameSeconds = 1f / 60f;
    private const int SimulatedFrames = 240;
    private const float Tolerance = 0.0001f;

    [Test]
    public void IntensityIsClampedBetweenZeroAndOne()
    {
        var model = new LocomotionPoseModel(new LocomotionPoseSettings(), ReferenceSpeed);

        Assert.That(model.GetIntensity(-1f), Is.EqualTo(0f));
        Assert.That(model.GetIntensity(ReferenceSpeed * 0.5f), Is.EqualTo(0.5f).Within(Tolerance));
        Assert.That(model.GetIntensity(ReferenceSpeed * 3f), Is.EqualTo(1f));
    }

    [Test]
    public void StandingStillNeverBobsOrSways()
    {
        var model = new LocomotionPoseModel(new LocomotionPoseSettings(), ReferenceSpeed);

        for (int frame = 0; frame < SimulatedFrames; frame++)
        {
            LocomotionPose pose = model.Advance(0f, FrameSeconds);
            Assert.That(pose.BobMeters, Is.EqualTo(0f));
            Assert.That(pose.RollDegrees, Is.EqualTo(0f));
        }
    }

    [Test]
    public void StandingStillBreathesAroundTheRestingHeight()
    {
        var settings = new LocomotionPoseSettings();
        var model = new LocomotionPoseModel(settings, ReferenceSpeed);
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int frame = 0; frame < SimulatedFrames; frame++)
        {
            float scale = model.Advance(0f, FrameSeconds).VerticalScale;
            min = Math.Min(min, scale);
            max = Math.Max(max, scale);
        }

        Assert.That(min, Is.LessThan(1f));
        Assert.That(max, Is.GreaterThan(1f));
        Assert.That(max - min, Is.LessThanOrEqualTo(settings.IdleBreathAmplitude * 2f + Tolerance));
    }

    [Test]
    public void MovingBouncesSwaysBothWaysAndNeverExceedsTheConfiguredAmplitude()
    {
        var settings = new LocomotionPoseSettings();
        var model = new LocomotionPoseModel(settings, ReferenceSpeed);
        float minRoll = float.MaxValue;
        float maxRoll = float.MinValue;
        float maxBob = 0f;

        for (int frame = 0; frame < SimulatedFrames; frame++)
        {
            LocomotionPose pose = model.Advance(ReferenceSpeed, FrameSeconds);
            minRoll = Math.Min(minRoll, pose.RollDegrees);
            maxRoll = Math.Max(maxRoll, pose.RollDegrees);
            maxBob = Math.Max(maxBob, pose.BobMeters);
            Assert.That(pose.BobMeters, Is.GreaterThanOrEqualTo(0f));
        }

        Assert.That(minRoll, Is.LessThan(0f));
        Assert.That(maxRoll, Is.GreaterThan(0f));
        Assert.That(maxBob, Is.GreaterThan(0f).And.LessThanOrEqualTo(settings.BobMeters + Tolerance));
    }

    [Test]
    public void WidthCompensatesHeightToKeepVolume()
    {
        var model = new LocomotionPoseModel(new LocomotionPoseSettings(), ReferenceSpeed);

        for (int frame = 0; frame < SimulatedFrames; frame++)
        {
            LocomotionPose pose = model.Advance(ReferenceSpeed, FrameSeconds);
            float volume = pose.VerticalScale * pose.HorizontalScale * pose.HorizontalScale;
            Assert.That(volume, Is.EqualTo(1f).Within(Tolerance));
        }
    }

    [Test]
    public void NonPositiveReferenceSpeedIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LocomotionPoseModel(new LocomotionPoseSettings(), 0f));
    }
}
