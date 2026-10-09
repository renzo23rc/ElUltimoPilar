using NUnit.Framework;
using UnityEngine;

public class PitRespawnPointTests
{
    private const float LethalRadius = 4.5f;
    private const float Margin = 1f;
    private const float Tolerance = 0.001f;

    [Test]
    public void RimLiesOutsideThePitOnTheSideWhereThePlayerFell()
    {
        var center = new Vector3(2f, 0f, 3f);
        var fall = new Vector3(5f, -1f, 3f);

        Vector3 rim = PitRespawnPoint.FindRimPosition(center, LethalRadius, fall, Vector3.back, Margin);

        Assert.That(rim.x, Is.EqualTo(center.x + LethalRadius + Margin).Within(Tolerance));
        Assert.That(rim.z, Is.EqualTo(center.z).Within(Tolerance));
    }

    [Test]
    public void RimIsAlwaysTheSameDistanceFromTheCenter()
    {
        Vector3 center = Vector3.zero;

        Vector3 rim = PitRespawnPoint.FindRimPosition(center, LethalRadius, new Vector3(1f, 0f, 2f), Vector3.forward, Margin);

        Assert.That(Vector3.Distance(center, new Vector3(rim.x, 0f, rim.z)), Is.EqualTo(LethalRadius + Margin).Within(Tolerance));
    }

    [Test]
    public void RimKeepsTheHeightOfTheFall()
    {
        Vector3 rim = PitRespawnPoint.FindRimPosition(Vector3.zero, LethalRadius, new Vector3(2f, 7.5f, 0f), Vector3.forward, Margin);

        Assert.That(rim.y, Is.EqualTo(7.5f).Within(Tolerance));
    }

    [Test]
    public void HeightDifferenceDoesNotChangeTheDirection()
    {
        Vector3 shallow = PitRespawnPoint.FindRimPosition(Vector3.zero, LethalRadius, new Vector3(0f, 0f, 3f), Vector3.right, Margin);
        Vector3 deep = PitRespawnPoint.FindRimPosition(Vector3.zero, LethalRadius, new Vector3(0f, -50f, 3f), Vector3.right, Margin);

        Assert.That(deep.x, Is.EqualTo(shallow.x).Within(Tolerance));
        Assert.That(deep.z, Is.EqualTo(shallow.z).Within(Tolerance));
    }

    [Test]
    public void FallingExactlyAtTheCenterUsesTheFallbackDirection()
    {
        Vector3 rim = PitRespawnPoint.FindRimPosition(Vector3.zero, LethalRadius, new Vector3(0f, -2f, 0f), Vector3.left, Margin);

        Assert.That(rim.x, Is.EqualTo(-(LethalRadius + Margin)).Within(Tolerance));
        Assert.That(rim.z, Is.EqualTo(0f).Within(Tolerance));
    }

    [Test]
    public void WithoutAnyDirectionItStillReturnsAPointOnTheRim()
    {
        Vector3 rim = PitRespawnPoint.FindRimPosition(Vector3.zero, LethalRadius, Vector3.zero, Vector3.zero, Margin);

        Assert.That(new Vector2(rim.x, rim.z).magnitude, Is.EqualTo(LethalRadius + Margin).Within(Tolerance));
    }
}
