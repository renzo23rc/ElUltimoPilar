using NUnit.Framework;

public class SpawnZoneSelectorTests
{
    [TestCase(0f, 0)]
    [TestCase(0.24f, 0)]
    [TestCase(0.26f, 1)]
    [TestCase(0.99f, 1)]
    [TestCase(1f, 1)]
    public void ZonesAreWeightedByArea(float roll, int expectedIndex)
    {
        float[] areas = { 100f, 300f };

        Assert.That(SpawnZoneSelector.PickIndex(areas, roll), Is.EqualTo(expectedIndex));
    }

    [Test]
    public void EmptyZonesArePickedNever()
    {
        float[] areas = { 0f, 50f, -10f };

        Assert.That(SpawnZoneSelector.PickIndex(areas, 0f), Is.EqualTo(1));
        Assert.That(SpawnZoneSelector.PickIndex(areas, 1f), Is.EqualTo(1));
    }

    [Test]
    public void NoUsableZoneReturnsMinusOne()
    {
        Assert.That(SpawnZoneSelector.PickIndex(new float[0], 0.5f), Is.EqualTo(-1));
        Assert.That(SpawnZoneSelector.PickIndex(new[] { 0f, 0f }, 0.5f), Is.EqualTo(-1));
    }

    [Test]
    public void NullAreasThrow()
    {
        Assert.Throws<System.ArgumentNullException>(() => SpawnZoneSelector.PickIndex(null, 0f));
    }
}
