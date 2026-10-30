using NUnit.Framework;

public class VictorySelectionTests
{
    private const int OptionCount = 2;

    [Test]
    public void MoveSelectionWrapsBetweenRestartAndMenu()
    {
        Assert.That(VictoryScreenController.MoveSelection(0, 1, OptionCount), Is.EqualTo(1));
        Assert.That(VictoryScreenController.MoveSelection(1, 1, OptionCount), Is.EqualTo(0));
        Assert.That(VictoryScreenController.MoveSelection(0, -1, OptionCount), Is.EqualTo(1));
        Assert.That(VictoryScreenController.MoveSelection(1, -1, OptionCount), Is.EqualTo(0));
    }
}
