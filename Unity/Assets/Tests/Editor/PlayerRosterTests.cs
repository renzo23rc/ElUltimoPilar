using NUnit.Framework;

public class PlayerRosterTests
{
    [Test]
    public void MaximumCapacityIsFourPlayers()
    {
        Assert.That(PlayerRoster<FakePlayer>.MaximumCapacity, Is.EqualTo(4));
    }

    [TestCase(PlayerRoster<FakePlayer>.MinimumCapacity - 1)]
    [TestCase(PlayerRoster<FakePlayer>.MaximumCapacity + 1)]
    public void ConstructorRejectsCapacityOutsideSupportedBounds(int capacity)
    {
        Assert.That(() => new PlayerRoster<FakePlayer>(capacity),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [TestCase(PlayerRoster<FakePlayer>.MinimumCapacity)]
    [TestCase(PlayerRoster<FakePlayer>.MaximumCapacity)]
    public void ConstructorAcceptsSupportedCapacityBounds(int capacity)
    {
        var roster = new PlayerRoster<FakePlayer>(capacity);

        Assert.That(roster.Capacity, Is.EqualTo(capacity));
        Assert.That(roster.Count, Is.Zero);
    }

    [Test]
    public void RegisterRejectsNullDuplicateAndFifthPlayer()
    {
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);
        var players = new FakePlayer[PlayerRoster<FakePlayer>.MaximumCapacity];

        Assert.That(roster.Register(null), Is.False);

        for (var index = 0; index < players.Length; index++)
        {
            players[index] = new FakePlayer();
            Assert.That(roster.Register(players[index]), Is.True);
        }

        Assert.That(roster.Register(players[0]), Is.False);
        Assert.That(roster.Register(new FakePlayer()), Is.False);
        Assert.That(roster.Count, Is.EqualTo(PlayerRoster<FakePlayer>.MaximumCapacity));
    }

    [Test]
    public void UnregisterFreesSlotForAnotherPlayer()
    {
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MinimumCapacity);
        var first = new FakePlayer();
        var replacement = new FakePlayer();

        Assert.That(roster.Register(first), Is.True);
        Assert.That(roster.Unregister(first), Is.True);
        Assert.That(roster.Unregister(first), Is.False);
        Assert.That(roster.Register(replacement), Is.True);
        Assert.That(roster.Players[0], Is.SameAs(replacement));
    }

    [Test]
    public void RegisterAllowsDistinctPlayerInstancesThatCompareEqual()
    {
        var roster = new PlayerRoster<EqualPlayer>(PlayerRoster<EqualPlayer>.MinimumCapacity + 1);
        var first = new EqualPlayer();
        var second = new EqualPlayer();

        Assert.That(roster.Register(first), Is.True);
        Assert.That(roster.Register(second), Is.True);
        Assert.That(roster.Count, Is.EqualTo(roster.Capacity));
        Assert.That(roster.Players[0], Is.SameAs(first));
        Assert.That(roster.Players[1], Is.SameAs(second));
    }

    [Test]
    public void UnregisterDoesNotRemoveDistinctPlayerInstanceThatComparesEqual()
    {
        var roster = new PlayerRoster<EqualPlayer>(PlayerRoster<EqualPlayer>.MinimumCapacity + 1);
        var registeredPlayer = new EqualPlayer();
        var unregisteredPlayer = new EqualPlayer();

        Assert.That(roster.Register(registeredPlayer), Is.True);
        Assert.That(roster.Unregister(unregisteredPlayer), Is.False);
        Assert.That(roster.Count, Is.EqualTo(1));
        Assert.That(roster.Players[0], Is.SameAs(registeredPlayer));
    }

    [Test]
    public void PlayersPreserveInsertionOrder()
    {
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);
        var first = new FakePlayer();
        var second = new FakePlayer();
        var third = new FakePlayer();

        roster.Register(first);
        roster.Register(second);
        roster.Register(third);

        Assert.That(roster.Players, Is.EqualTo(new[] { first, second, third }));
    }

    [Test]
    public void EmptyRosterIsNotAllDowned()
    {
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);

        Assert.That(roster.Count, Is.Zero);
        Assert.That(roster.DownedCount, Is.Zero);
        Assert.That(roster.StandingCount, Is.Zero);
        Assert.That(roster.AreAllDowned, Is.False);
    }

    [Test]
    public void AllStandingRosterReportsStandingPlayers()
    {
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);
        roster.Register(new FakePlayer());
        roster.Register(new FakePlayer());

        Assert.That(roster.DownedCount, Is.Zero);
        Assert.That(roster.StandingCount, Is.EqualTo(2));
        Assert.That(roster.AreAllDowned, Is.False);
    }

    [Test]
    public void MixedRosterReportsDownedAndStandingPlayers()
    {
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);
        roster.Register(new FakePlayer { IsDowned = true });
        roster.Register(new FakePlayer());

        Assert.That(roster.DownedCount, Is.EqualTo(1));
        Assert.That(roster.StandingCount, Is.EqualTo(1));
        Assert.That(roster.AreAllDowned, Is.False);
    }

    [Test]
    public void AllDownedRosterReportsDefeatQuery()
    {
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);
        roster.Register(new FakePlayer { IsDowned = true });
        roster.Register(new FakePlayer { IsDowned = true });

        Assert.That(roster.DownedCount, Is.EqualTo(2));
        Assert.That(roster.StandingCount, Is.Zero);
        Assert.That(roster.AreAllDowned, Is.True);
    }

    [Test]
    public void DownedPlayerBecomingStandingUpdatesQueries()
    {
        var player = new FakePlayer { IsDowned = true };
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);
        roster.Register(player);

        player.IsDowned = false;

        Assert.That(roster.DownedCount, Is.Zero);
        Assert.That(roster.StandingCount, Is.EqualTo(1));
        Assert.That(roster.AreAllDowned, Is.False);
    }

    [Test]
    public void ReplenishWaveAmmoCallsEveryRegisteredMemberExactlyOnceIncludingDowned()
    {
        var standing = new FakePlayer();
        var downed = new FakePlayer { IsDowned = true };
        var roster = new PlayerRoster<FakePlayer>(PlayerRoster<FakePlayer>.MaximumCapacity);
        roster.Register(standing);
        roster.Register(downed);

        roster.ReplenishWaveAmmo();

        Assert.That(standing.ReplenishCalls, Is.EqualTo(1));
        Assert.That(downed.ReplenishCalls, Is.EqualTo(1));
    }

    private sealed class FakePlayer : IPlayerRosterMember
    {
        public bool IsDowned { get; set; }
        public int ReplenishCalls { get; private set; }

        public void ReplenishWaveAmmo()
        {
            ReplenishCalls++;
        }
    }

    private sealed class EqualPlayer : IPlayerRosterMember
    {
        public bool IsDowned { get; set; }

        public void ReplenishWaveAmmo()
        {
        }

        public override bool Equals(object obj)
        {
            return obj is EqualPlayer;
        }

        public override int GetHashCode()
        {
            return typeof(EqualPlayer).GetHashCode();
        }
    }
}
