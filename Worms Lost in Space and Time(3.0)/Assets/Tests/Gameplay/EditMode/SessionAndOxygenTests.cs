using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Worms.Core;
using Worms.Gameplay;

public class SessionAndOxygenTests
{
    private sealed class Catalog : ILevelCatalog
    {
        public IReadOnlyList<LevelDefinition> Levels { get; } = new[]
        {
            new LevelDefinition { Id = "first", SceneName = "First", StarTimeSeconds = 3f },
            new LevelDefinition { Id = "last", SceneName = "Last" }
        };
        public LevelDefinition GetLevel(string id) { foreach (var level in Levels) if (level.Id == id) return level; return null; }
        public LevelDefinition FindByScene(string scene) { foreach (var level in Levels) if (level.SceneName == scene) return level; return null; }
        public LevelDefinition GetNext(string id) { return id == "first" ? Levels[1] : null; }
    }
    private sealed class Storage : ISaveStorage
    {
        public int Writes;
        public bool Fail;
        public SaveData Load() { return null; }
        public void Save(SaveData data) { Writes++; if (Fail) throw new IOException("Injected write failure"); }
    }
    private static LevelSession Session(Storage storage)
    {
        var catalog = new Catalog();
        return new LevelSession(catalog.Levels[0], new ProgressionService(catalog, new SaveService(storage, catalog)));
    }

    [Test]
    public void CompletionRoundsLegacyFloatAndIsSingleEvenForReentrantRequests()
    {
        var storage = new Storage();
        var session = Session(storage);
        int changed = 0;
        session.Changed += value => { changed++; Assert.IsFalse(value.TryDie()); Assert.IsFalse(value.TryComplete()); };
        session.Tick(2.675f);
        Assert.IsTrue(session.TryComplete());
        Assert.That(session.ElapsedSeconds, Is.EqualTo(2.68f));
        Assert.That(session.Completion.ElapsedSeconds, Is.EqualTo(2.68f));
        Assert.IsTrue(session.Completion.StarEarned);
        Assert.AreEqual("last", session.Completion.NextLevelId);
        session.Tick(12f);
        Assert.IsFalse(session.TryComplete());
        Assert.AreEqual(1, storage.Writes);
        Assert.AreEqual(1, changed);
        Assert.That(session.ElapsedSeconds, Is.EqualTo(2.68f));
    }

    [Test]
    public void DeathFirstNeverWritesAndFreezesClock()
    {
        var storage = new Storage();
        var session = Session(storage);
        int changed = 0;
        session.Changed += value => changed++;
        session.Tick(1f);
        Assert.IsTrue(session.TryDie());
        Assert.IsFalse(session.TryDie());
        Assert.IsFalse(session.TryComplete());
        session.Tick(9f);
        Assert.AreEqual(1f, session.ElapsedSeconds);
        Assert.AreEqual(0, storage.Writes);
        Assert.AreEqual(1, changed);
        Assert.IsNull(session.Completion);
    }

    [Test]
    public void FailedSaveStillCompletesOnceAndRetainsResult()
    {
        var storage = new Storage { Fail = true };
        var session = Session(storage);
        Assert.IsTrue(session.TryComplete());
        Assert.AreEqual(SessionState.Completed, session.State);
        Assert.IsFalse(session.Completion.SaveSucceeded);
        Assert.IsFalse(session.TryComplete());
        Assert.IsFalse(session.TryDie());
        Assert.AreEqual(1, storage.Writes);
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void InvalidTickCannotPoisonClock(float value)
    {
        var session = Session(new Storage());
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(value));
        Assert.AreEqual(0f, session.ElapsedSeconds);
    }

    [Test]
    public void ClockSaturatesWithoutBecomingInfinite()
    {
        var session = Session(new Storage());
        session.Tick(float.MaxValue);
        session.Tick(float.MaxValue);
        Assert.AreEqual(float.MaxValue, session.ElapsedSeconds);
        Assert.IsTrue(session.TryComplete());
        Assert.IsFalse(float.IsInfinity(session.Completion.ElapsedSeconds));
    }

    [Test]
    public void OxygenDepletesOnFollowingTickAndRefillsToLegacyTen()
    {
        var supply = new OxygenSupply(.5f, 20f, 2f);
        int depleted = 0;
        float fraction = -1f;
        supply.Depleted += () => depleted++;
        supply.Changed += value => fraction = value;
        supply.Tick(.5f);
        Assert.AreEqual(-.5f, supply.Current);
        Assert.AreEqual(0f, fraction);
        Assert.AreEqual(0, depleted);
        supply.Tick(0f);
        supply.Tick(1f);
        Assert.AreEqual(1, depleted);
        supply.Refill();
        Assert.AreEqual(10f, supply.Current);
        Assert.AreEqual(.5f, supply.Fraction);
        Assert.AreEqual(.5f, fraction);
    }
}
