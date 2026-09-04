using NUnit.Framework;

public class HudVariantDisplayTests
{
    [TestCase("Rifle de precisión")]
    [TestCase("Señuelo")]
    public void ActiveVariantUsesSemanticDisplayName(string semanticDisplayName)
    {
        string displayName = Hud.GetVariantDisplayName(true, semanticDisplayName);

        Assert.That(displayName, Is.EqualTo(semanticDisplayName));
    }

    [Test]
    public void InactiveVariantFallsBackToEmptyDisplayName()
    {
        string displayName = Hud.GetVariantDisplayName(false, "Stale variant");

        Assert.That(displayName, Is.Empty);
    }

    [TestCase("")]
    [TestCase(null)]
    public void MissingSemanticDisplayNameFallsBackToEmptyDisplayName(string semanticDisplayName)
    {
        string displayName = Hud.GetVariantDisplayName(true, semanticDisplayName);

        Assert.That(displayName, Is.Empty);
    }
}
