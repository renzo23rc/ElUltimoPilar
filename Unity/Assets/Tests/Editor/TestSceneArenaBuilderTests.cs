using NUnit.Framework;
using UnityEngine;

public class TestSceneArenaBuilderTests
{
    private const float Tolerance = 0.0001f;

    [Test]
    public void DefaultPitRadiusKeepsTheSampleSceneScale()
    {
        Vector3 scale = TestSceneArenaBuilder.ScaleForPitRadius(TestSceneArenaBuilder.DefaultPitRadiusMeters);

        Assert.That(scale.x, Is.EqualTo(10f).Within(Tolerance));
        Assert.That(scale.y, Is.EqualTo(0.5f).Within(Tolerance));
        Assert.That(scale.z, Is.EqualTo(10f).Within(Tolerance));
    }

    [Test]
    public void SmallerPitRadiusScalesOnlyHorizontally()
    {
        Vector3 scale = TestSceneArenaBuilder.ScaleForPitRadius(2.5f);

        Assert.That(scale.x, Is.EqualTo(5f).Within(Tolerance));
        Assert.That(scale.y, Is.EqualTo(0.5f).Within(Tolerance));
        Assert.That(scale.z, Is.EqualTo(5f).Within(Tolerance));
    }
}
