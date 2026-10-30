using NUnit.Framework;
using UltimoPilar.Arena;

public class ArenaPhaseVisualSelectorTests
{
    private const int ThreeVariants = 3;

    [Test]
    public void PhaseOneKeepsTheOriginalMapPieces()
    {
        Assert.That(ArenaPhaseVisualSelector.DeterioratedVariantIndex(1, ThreeVariants), Is.EqualTo(-1));
    }

    [Test]
    public void PhaseTwoShowsTheFirstDeterioratedVariant()
    {
        Assert.That(ArenaPhaseVisualSelector.DeterioratedVariantIndex(2, ThreeVariants), Is.EqualTo(0));
    }

    [Test]
    public void PhaseThreeShowsTheSecondDeterioratedVariant()
    {
        Assert.That(ArenaPhaseVisualSelector.DeterioratedVariantIndex(3, ThreeVariants), Is.EqualTo(1));
    }

    [Test]
    public void PhaseFourShowsTheThirdDeterioratedVariant()
    {
        Assert.That(ArenaPhaseVisualSelector.DeterioratedVariantIndex(4, ThreeVariants), Is.EqualTo(2));
    }

    [Test]
    public void APhaseBeyondTheLastVariantKeepsTheLastVariant()
    {
        Assert.That(ArenaPhaseVisualSelector.DeterioratedVariantIndex(5, ThreeVariants), Is.EqualTo(2));
    }

    [Test]
    public void NoVariantsAlwaysKeepTheOriginalMapPieces()
    {
        Assert.That(ArenaPhaseVisualSelector.DeterioratedVariantIndex(4, 0), Is.EqualTo(-1));
    }
}
