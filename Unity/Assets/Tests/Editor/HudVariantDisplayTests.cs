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

    [Test]
    public void DamageVariantLabelShowsMultiplier()
    {
        string label = Hud.FormatVariantLabel("Rifle de precisión", true, 2f, 12f);

        Assert.That(label, Is.EqualTo("¡x2 Rifle de precisión! 12s"));
    }

    [TestCase("Señuelo")]
    [TestCase("Ralentización")]
    public void UtilityVariantLabelOmitsMultiplier(string displayName)
    {
        string label = Hud.FormatVariantLabel(displayName, false, 2f, 5f);

        Assert.That(label, Is.EqualTo($"¡{displayName}! 5s"));
    }

    [TestCase("")]
    [TestCase(null)]
    public void MissingDisplayNameProducesEmptyLabel(string displayName)
    {
        Assert.That(Hud.FormatVariantLabel(displayName, true, 2f, 5f), Is.Empty);
    }
}
