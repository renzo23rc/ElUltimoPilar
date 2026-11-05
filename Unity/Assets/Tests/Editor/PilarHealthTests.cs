using NUnit.Framework;
using UnityEngine;

public sealed class PilarHealthTests
{
    private const float ExpectedMaximumHealth = 400f;
    private const float HalfHealth = 200f;
    private const float QuarterHealth = 100f;
    private const float PercentageTolerance = 0.001f;

    private GameObject pilarObject;
    private Pilar pilar;

    [SetUp]
    public void SetUp()
    {
        pilarObject = new GameObject("PilarHealthTest");
        pilar = pilarObject.AddComponent<Pilar>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(pilarObject);
    }

    [Test]
    public void NewPilar_StartsWithFullQuadrupledHealth()
    {
        Assert.AreEqual(ExpectedMaximumHealth, pilar.vidaMaxima, PercentageTolerance);
        Assert.AreEqual(ExpectedMaximumHealth, pilar.vidaActual, PercentageTolerance);
    }

    [Test]
    public void HealthPercentage_IsRelativeToMaximumHealth()
    {
        pilar.vidaActual = HalfHealth;
        Assert.AreEqual(50f, pilar.PorcentajeVida, PercentageTolerance);

        pilar.vidaActual = QuarterHealth;
        Assert.AreEqual(25f, pilar.PorcentajeVida, PercentageTolerance);
    }
}
