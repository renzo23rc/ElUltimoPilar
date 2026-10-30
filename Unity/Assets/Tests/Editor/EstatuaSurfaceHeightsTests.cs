using NUnit.Framework;
using UltimoPilar.Arena;

public class EstatuaSurfaceHeightsTests
{
    private const float Tolerance = 0.0001f;
    private const float ControllerHalfHeightMeters = 1f;
    private const float TurretPivotAboveSurfaceMeters = 1.1f;

    [Test]
    public void PlayerSpawnSitsOneHalfControllerAboveTheGrass()
    {
        EstatuaSurfaceHeights heights = EstatuaSurfaceHeights.FromMeshTops(
            1.4f,
            2.6f,
            ControllerHalfHeightMeters,
            TurretPivotAboveSurfaceMeters);

        Assert.That(heights.WalkSurfaceY, Is.EqualTo(1.4f).Within(Tolerance));
        Assert.That(heights.PlayerSpawnY, Is.EqualTo(2.4f).Within(Tolerance));
    }

    [Test]
    public void PitAndTurretsSitOnTheStarWhenItIsHigherThanTheGrass()
    {
        EstatuaSurfaceHeights heights = EstatuaSurfaceHeights.FromMeshTops(
            1.4f,
            2.6f,
            ControllerHalfHeightMeters,
            TurretPivotAboveSurfaceMeters);

        Assert.That(heights.HazardY, Is.EqualTo(2.6f).Within(Tolerance));
        Assert.That(heights.TurretY, Is.EqualTo(3.7f).Within(Tolerance));
    }

    [Test]
    public void HazardsNeverSitBelowTheWalkPlaneWhenTheStarIsLower()
    {
        EstatuaSurfaceHeights heights = EstatuaSurfaceHeights.FromMeshTops(
            2.1f,
            1.5f,
            ControllerHalfHeightMeters,
            TurretPivotAboveSurfaceMeters);

        Assert.That(heights.HazardY, Is.EqualTo(2.1f).Within(Tolerance));
        Assert.That(heights.TurretY, Is.EqualTo(3.2f).Within(Tolerance));
    }

    [Test]
    public void PitRadiusOverrideWinsOverThePedestal()
    {
        float radius = EstatuaSurfaceHeights.ResolvePitRadius(3.25f, 2f, 0.75f, 5f);

        Assert.That(radius, Is.EqualTo(3.25f).Within(Tolerance));
    }

    [Test]
    public void PitRadiusFitsThePedestalWhenOverrideIsZero()
    {
        float radius = EstatuaSurfaceHeights.ResolvePitRadius(0f, 2f, 0.75f, 5f);

        Assert.That(radius, Is.EqualTo(2.75f).Within(Tolerance));
    }

    [Test]
    public void PitRadiusFallsBackWhenThePedestalIsMissing()
    {
        float radius = EstatuaSurfaceHeights.ResolvePitRadius(0f, 0f, 0f, 5f);

        Assert.That(radius, Is.EqualTo(5f).Within(Tolerance));
    }
}
