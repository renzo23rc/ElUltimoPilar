using NUnit.Framework;

public class MatchStartInputPolicyTests
{
    private const float LookThreshold = 2f;

    [Test]
    public void IdleCommandDoesNotStartTheMatch()
    {
        Assert.That(MatchStartInputPolicy.IsStartInput(default(PlayerCommand), LookThreshold), Is.False);
    }

    [TestCase(1f, 0f)]
    [TestCase(0f, -0.2f)]
    public void AnyMovementStartsTheMatch(float moveX, float moveY)
    {
        Assert.That(MatchStartInputPolicy.IsStartInput(new PlayerCommand(moveX, moveY, 0f, 0f), LookThreshold), Is.True);
    }

    [TestCase(1.5f, 1f, false)]
    [TestCase(2f, 0f, false)]
    [TestCase(2f, 1f, true)]
    public void OnlyAStrongLookStartsTheMatch(float lookX, float lookY, bool expected)
    {
        Assert.That(MatchStartInputPolicy.IsStartInput(new PlayerCommand(0f, 0f, lookX, lookY), LookThreshold), Is.EqualTo(expected));
    }

    [Test]
    public void JumpOrFireStartTheMatch()
    {
        Assert.That(MatchStartInputPolicy.IsStartInput(new PlayerCommand(0f, 0f, 0f, 0f, jump: true), LookThreshold), Is.True);
        Assert.That(MatchStartInputPolicy.IsStartInput(new PlayerCommand(0f, 0f, 0f, 0f, fire: true), LookThreshold), Is.True);
    }

    [Test]
    public void PauseAloneDoesNotStartTheMatch()
    {
        Assert.That(MatchStartInputPolicy.IsStartInput(new PlayerCommand(0f, 0f, 0f, 0f, pause: true), LookThreshold), Is.False);
    }
}
