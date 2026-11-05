using NUnit.Framework;
using UnityEngine;

public sealed class PlayerInPlaceLocomotionTests
{
    private const float ToleranceMeters = 0.0001f;

    [Test]
    public void ForwardTravel_ReturnsForwardDifferenceFromRest()
    {
        Vector3 rest = new Vector3(0.1f, 0.5f, -0.1f);
        Vector3 current = new Vector3(0.1f, 0.5f, 1.7f);

        Vector3 travel = PlayerInPlaceLocomotion.ForwardTravel(current, rest);

        Assert.AreEqual(0f, travel.x, ToleranceMeters);
        Assert.AreEqual(0f, travel.y, ToleranceMeters);
        Assert.AreEqual(1.8f, travel.z, ToleranceMeters);
    }

    [Test]
    public void ForwardTravel_IgnoresSidewaysSwayAndVerticalBob()
    {
        Vector3 rest = new Vector3(0f, 0.5f, 0f);
        Vector3 current = new Vector3(-0.3f, 0.62f, 0f);

        Vector3 travel = PlayerInPlaceLocomotion.ForwardTravel(current, rest);

        Assert.AreEqual(0f, travel.sqrMagnitude, ToleranceMeters);
    }

    [Test]
    public void ForwardTravel_AtRest_IsZero()
    {
        Vector3 rest = new Vector3(0.3f, 0.2f, -0.4f);

        Assert.AreEqual(Vector3.zero, PlayerInPlaceLocomotion.ForwardTravel(rest, rest));
    }

    [Test]
    public void ForwardTravel_KeepsBackwardTravel()
    {
        Vector3 travel = PlayerInPlaceLocomotion.ForwardTravel(new Vector3(0f, 0f, -0.9f), Vector3.zero);

        Assert.AreEqual(-0.9f, travel.z, ToleranceMeters);
    }
}
