using NUnit.Framework;
using UnityEngine;

public class MatchResultTests
{
    private const float MaximumHealth = 100f;

    [Test]
    public void ValidVictorySnapshotExposesOutcomeAndHealth()
    {
        var snapshot = new PilarHealthSnapshot(75f, MaximumHealth);
        var result = new MatchResult(MatchState.Victory, snapshot);

        Assert.That(result.Outcome, Is.EqualTo(MatchState.Victory));
        Assert.That(result.PilarHealth.Remaining, Is.EqualTo(75f));
        Assert.That(result.PilarHealth.Maximum, Is.EqualTo(MaximumHealth));
        Assert.That(result.PilarHealth, Is.EqualTo(snapshot));
        Assert.That(result.Score, Is.EqualTo(75));
    }

    [Test]
    public void ValidDefeatSnapshotAllowsZeroRemainingHealth()
    {
        var snapshot = new PilarHealthSnapshot(0f, MaximumHealth);
        var result = new MatchResult(MatchState.Defeat, snapshot);

        Assert.That(result.Outcome, Is.EqualTo(MatchState.Defeat));
        Assert.That(result.PilarHealth.Remaining, Is.Zero);
        Assert.That(result.Score, Is.Zero);
    }

    [Test]
    public void SnapshotDerivesRemainingPercentageWithoutBeingAScore()
    {
        var snapshot = new PilarHealthSnapshot(25f, MaximumHealth);

        Assert.That(snapshot.RemainingPercentage, Is.EqualTo(25f).Within(0.0001f));
        Assert.That(snapshot.RemainingRatio, Is.EqualTo(0.25f).Within(0.0001f));
    }

    [Test]
    public void MatchResultPreservesTheComputedHealthScore()
    {
        var snapshot = new PilarHealthSnapshot(50.5f, MaximumHealth);
        var result = new MatchResult(MatchState.Victory, snapshot);

        Assert.That(result.Score, Is.EqualTo(51));
        Assert.That(typeof(MatchResult).GetProperty(nameof(MatchResult.Score)).CanWrite,
            Is.False);
    }

    [Test]
    public void MatchResultRejectsNonTerminalStates()
    {
        var snapshot = new PilarHealthSnapshot(50f, MaximumHealth);

        Assert.That(() => new MatchResult(MatchState.WaitingToStart, snapshot),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => new MatchResult(MatchState.Playing, snapshot),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => new MatchResult(MatchState.Paused, snapshot),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void SnapshotRejectsInvalidHealthBounds()
    {
        Assert.That(() => new PilarHealthSnapshot(-1f, MaximumHealth),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => new PilarHealthSnapshot(MaximumHealth + 1f, MaximumHealth),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => new PilarHealthSnapshot(1f, 0f),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void SnapshotRejectsNonFiniteHealthValues()
    {
        Assert.That(() => new PilarHealthSnapshot(float.NaN, MaximumHealth),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => new PilarHealthSnapshot(float.PositiveInfinity, MaximumHealth),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => new PilarHealthSnapshot(1f, float.NaN),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
        Assert.That(() => new PilarHealthSnapshot(1f, float.PositiveInfinity),
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void ResultKeepsAnImmutableSnapshotValue()
    {
        var snapshot = new PilarHealthSnapshot(40f, MaximumHealth);
        var result = new MatchResult(MatchState.Victory, snapshot);
        snapshot = new PilarHealthSnapshot(0f, MaximumHealth);

        Assert.That(result.PilarHealth.Remaining, Is.EqualTo(40f));
        Assert.That(typeof(PilarHealthSnapshot).GetProperty(nameof(PilarHealthSnapshot.Remaining)).CanWrite,
            Is.False);
        Assert.That(typeof(MatchResult).GetProperty(nameof(MatchResult.Outcome)).CanWrite,
            Is.False);
        Assert.That(typeof(MatchResult).GetProperty(nameof(MatchResult.PilarHealth)).CanWrite,
            Is.False);
    }

    [Test]
    public void MatchResultRejectsDefaultInvalidSnapshot()
    {
        Assert.That(() => new MatchResult(MatchState.Victory, default(PilarHealthSnapshot)),
            Throws.TypeOf<System.ArgumentException>());
    }

    [Test]
    public void TryCreateRejectsMalformedInputWithoutThrowing()
    {
        PilarHealthSnapshot snapshot;

        Assert.That(PilarHealthSnapshot.TryCreate(-1f, MaximumHealth, out snapshot), Is.False);
        Assert.That(snapshot.IsValid, Is.False);
        Assert.That(PilarHealthSnapshot.TryCreate(1f, float.PositiveInfinity, out snapshot), Is.False);
        Assert.That(snapshot.IsValid, Is.False);
        Assert.That(PilarHealthSnapshot.TryCreate(float.NaN, MaximumHealth, out snapshot), Is.False);
        Assert.That(snapshot.IsValid, Is.False);
    }

    [Test]
    public void GameManagerPublishesOneResultAndClearsItOnRestartOrNewStart()
    {
        CreateGameManager();
        resultPublicationCount = 0;
        victoryCount = 0;
        manager.OnMatchResult += CountResultPublication;
        manager.OnVictoria += CountVictory;

        manager.IniciarJuego();
        manager.Victoria();
        manager.Victoria();

        Assert.That(manager.CurrentResult, Is.Not.Null);
        Assert.That(resultPublicationCount, Is.EqualTo(1));
        Assert.That(victoryCount, Is.EqualTo(1));

        manager.IniciarJuego();
        Assert.That(manager.CurrentResult, Is.Null);

        manager.Victoria();
        manager.ReiniciarJuego();

        Assert.That(manager.CurrentResult, Is.Null);
        Assert.That(resultPublicationCount, Is.EqualTo(2));
        Assert.That(victoryCount, Is.EqualTo(2));
    }

    [Test]
    public void GameManagerPublishesValidDefeatResultThroughDirectDefeat()
    {
        CreateGameManager();
        resultPublicationCount = 0;
        manager.OnMatchResult += CountResultPublication;

        manager.IniciarJuego();
        manager.Derrota();

        Assert.That(manager.CurrentResult, Is.Not.Null);
        Assert.That(manager.CurrentResult.Outcome, Is.EqualTo(MatchState.Defeat));
        Assert.That(manager.CurrentResult.PilarHealth.Remaining, Is.EqualTo(MaximumHealth));
        Assert.That(manager.CurrentResult.Score, Is.EqualTo(100));
        Assert.That(resultPublicationCount, Is.EqualTo(1));
    }

    [Test]
    public void GameManagerKeepsLegacyDefeatEventWhenHealthSnapshotIsInvalid()
    {
        CreateGameManager();
        defeatCount = 0;
        manager.OnDerrota += CountDefeat;
        manager.IniciarJuego();
        manager.pilar.vidaMaxima = float.NaN;

        manager.DerrotaPorJugadores();

        Assert.That(manager.EstadoActual, Is.EqualTo(MatchState.Defeat));
        Assert.That(manager.CurrentResult, Is.Null);
        Assert.That(defeatCount, Is.EqualTo(1));
    }

    private GameObject gameManagerObject;
    private GameObject pilarObject;
    private GameManager manager;
    private int resultPublicationCount;
    private int victoryCount;
    private int defeatCount;

    private void CountResultPublication(MatchResult result)
    {
        resultPublicationCount++;
    }

    private void CountVictory()
    {
        victoryCount++;
    }

    private void CountDefeat()
    {
        defeatCount++;
    }

    private void CreateGameManager()
    {
        gameManagerObject = new GameObject("MatchResultTests.GameManager");
        manager = gameManagerObject.AddComponent<GameManager>();
        manager.SendMessage("Awake", SendMessageOptions.RequireReceiver);

        pilarObject = new GameObject("MatchResultTests.Pilar");
        var pilar = pilarObject.AddComponent<Pilar>();
        pilar.vidaMaxima = MaximumHealth;
        pilar.vidaActual = MaximumHealth;
        manager.pilar = pilar;
    }

    [TearDown]
    public void TearDown()
    {
        if (pilarObject != null)
            Object.DestroyImmediate(pilarObject);
        if (gameManagerObject != null)
            Object.DestroyImmediate(gameManagerObject);
    }
}
