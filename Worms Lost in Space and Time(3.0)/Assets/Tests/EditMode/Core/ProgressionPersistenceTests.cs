using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Worms;
using Worms.Configuration;
using Worms.Core;
using Worms.Persistence;

public sealed class ProgressionPersistenceTests
{
    private LevelCatalog catalog;
    private MemoryStorage memory;
    private string directory;

    [SetUp]
    public void SetUp()
    {
        catalog = ScriptableObject.CreateInstance<LevelCatalog>();
        catalog.SetLevels(new[] { Level("a", 1, 10), Level("b", 2, 15), Level("c", 3, 0) });
        memory = new MemoryStorage();
        directory = Path.Combine(Path.GetTempPath(), "worms-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
    }

    [TearDown]
    public void TearDown()
    {
        GameServices.ResetForTests();
        UnityEngine.Object.DestroyImmediate(catalog);
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public void MissingSaveStartsAtFirstLevelAndInjectionAvoidsRealStorage()
    {
        GameServices.ConfigureForTests(catalog, memory);
        Assert.That(GameServices.Progression.GetContinueLevelId(), Is.EqualTo("a"));
        Assert.That(GameServices.Progression.IsUnlocked("b"), Is.False);
        Assert.That(GameServices.Saves.LastError, Is.Null);
        Assert.That(GameServices.LegacySaveDetected, Is.False);
    }

    [Test]
    public void ReplayRetainsStarAndBestTimeAndNeverRelocks()
    {
        var saves = new SaveService(memory, catalog);
        var progression = new ProgressionService(catalog, saves);
        Assert.That(progression.RecordCompletion("a", 5).SaveSucceeded, Is.True);
        progression.RecordCompletion("b", 14);
        progression.RecordCompletion("a", 20);
        Assert.That(progression.HasStar("a"), Is.True);
        Assert.That(saves.Data.Levels.Single(item => item.LevelId == "a").BestTimeSeconds, Is.EqualTo(5));
        Assert.That(progression.IsUnlocked("c"), Is.True);
        Assert.That(progression.GetContinueLevelId(), Is.EqualTo("c"));
        Assert.That(progression.RecordCompletion("c", 0).NextLevelId, Is.Null);
        Assert.That(progression.HasStar("c"), Is.False);
        Assert.That(progression.GetContinueLevelId(), Is.EqualTo("c"));
    }

    [TestCase(9.99f, true)]
    [TestCase(10f, false)]
    [TestCase(10.01f, false)]
    [TestCase(9.999f, false)]
    [TestCase(0f, true)]
    public void StarUsesStrictComparisonAfterLegacyRounding(float elapsed, bool star)
    {
        var progression = new ProgressionService(catalog, new SaveService(memory, catalog));
        Assert.That(progression.RecordCompletion("a", elapsed).StarEarned, Is.EqualTo(star));
    }

    [TestCase(2.675f)]
    [TestCase(2.685f)]
    [TestCase(1.005f)]
    [TestCase(9.995f)]
    [TestCase(0f)]
    public void CompletionRoundingMatchesLegacyUnityFloatMath(float seconds)
    {
        float expected = Mathf.Round(seconds * 100f) / 100f;
        Assert.That(CompletionTiming.RoundSeconds(seconds), Is.EqualTo(expected));
        var progression = new ProgressionService(catalog, new SaveService(memory, catalog));
        Assert.That(progression.RecordCompletion("a", seconds).ElapsedSeconds, Is.EqualTo(expected));
    }
    [Test]
    public void UnknownLevelAndInvalidTimesCannotWriteProgress()
    {
        var saves = new SaveService(memory, catalog);
        var progression = new ProgressionService(catalog, saves);
        Assert.Throws<ArgumentException>(() => progression.RecordCompletion("missing", 1));
        foreach (float value in new[] { -1f, float.NaN, float.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => progression.RecordCompletion("a", value));
        Assert.That(saves.Data.Levels, Is.Empty);
        Assert.That(memory.SaveCount, Is.Zero);
        Assert.That(progression.IsUnlocked("missing"), Is.False);
    }

    [Test]
    public void WriteFailureKeepsCompletionAndRetryClearsError()
    {
        memory.FailSave = true;
        var saves = new SaveService(memory, catalog);
        var progression = new ProgressionService(catalog, saves);
        Assert.That(progression.RecordCompletion("a", 2).SaveSucceeded, Is.False);
        Assert.That(progression.IsUnlocked("b"), Is.True);
        Assert.That(saves.LastError, Is.Not.Empty);
        memory.FailSave = false;
        Assert.That(saves.TrySave(), Is.True);
        Assert.That(saves.LastError, Is.Null);
        Assert.That(saves.Data.Levels.Count, Is.EqualTo(1));
    }

    [TestCase("unknown")]
    [TestCase("duplicate")]
    [TestCase("nullEntry")]
    [TestCase("nullList")]
    [TestCase("nan")]
    [TestCase("infinity")]
    [TestCase("negative")]
    [TestCase("incompleteStar")]
    [TestCase("incompleteTime")]
    [TestCase("disabledStar")]
    [TestCase("future")]
    [TestCase("oldVersion")]
    public void InvalidDataIsReportedAndCannotOverwriteOriginal(string defect)
    {
        var item = new LevelProgress { LevelId = "a", Completed = true, BestTimeSeconds = 2 };
        var data = new SaveData { Levels = new List<LevelProgress> { item } };
        switch (defect)
        {
            case "unknown": item.LevelId = "unknown"; break;
            case "duplicate": data.Levels.Add(item); break;
            case "nullEntry": data.Levels[0] = null; break;
            case "nullList": data.Levels = null; break;
            case "nan": item.BestTimeSeconds = float.NaN; break;
            case "infinity": item.BestTimeSeconds = float.PositiveInfinity; break;
            case "negative": item.BestTimeSeconds = -1; break;
            case "incompleteStar": item.Completed = false; item.BestTimeSeconds = 0; item.StarEarned = true; break;
            case "incompleteTime": item.Completed = false; break;
            case "disabledStar": item.LevelId = "c"; item.StarEarned = true; break;
            case "future": data.Version = 2; break;
            case "oldVersion": data.Version = 0; break;
        }
        memory.Loaded = data;
        var saves = new SaveService(memory, catalog);
        Assert.That(saves.LastError, Is.Not.Empty);
        Assert.That(saves.TrySave(), Is.False);
        Assert.That(memory.SaveCount, Is.Zero);
        Assert.That(memory.Loaded, Is.SameAs(data));
    }

    [Test]
    public void LoadFailureCanRetryAfterExternalRepairWithoutLosingNewCompletion()
    {
        memory.FailLoad = true;
        var saves = new SaveService(memory, catalog);
        var progression = new ProgressionService(catalog, saves);
        Assert.That(progression.RecordCompletion("a", 3).SaveSucceeded, Is.False);
        memory.FailLoad = false;
        memory.Loaded = new SaveData { Levels = new List<LevelProgress> {
            new LevelProgress { LevelId = "b", Completed = true, BestTimeSeconds = 4, StarEarned = true } } };
        Assert.That(saves.TrySave(), Is.True);
        Assert.That(progression.HasStar("a"), Is.True);
        Assert.That(progression.HasStar("b"), Is.True);
    }

    [Test]
    public void StableIdsSurviveCatalogReordering()
    {
        var saves = new SaveService(memory, catalog);
        new ProgressionService(catalog, saves).RecordCompletion("a", 2);
        catalog.SetLevels(new[] { Level("c", 3, 0), Level("a", 1, 10), Level("b", 2, 15) });
        var reloaded = new ProgressionService(catalog, new SaveService(memory, catalog));
        Assert.That(reloaded.HasStar("a"), Is.True);
        Assert.That(reloaded.HasStar("c"), Is.False);
        Assert.That(reloaded.GetContinueLevelId(), Is.EqualTo("b"));
    }

    [Test]
    public void JsonRoundTripPreservesFieldsAndLegacyBytes()
    {
        byte[] legacy = { 0, 255, 10, 15, 7 };
        File.WriteAllBytes(Path.Combine(directory, "playerInfo.dat"), legacy);
        var storage = new JsonFileSaveStorage(directory);
        Assert.That(storage.Load(), Is.Null);
        var saves = new SaveService(storage, catalog);
        new ProgressionService(catalog, saves).RecordCompletion("a", 3.25f);
        SaveData loaded = storage.Load();
        Assert.That(loaded.Version, Is.EqualTo(1));
        Assert.That(loaded.Levels[0].LevelId, Is.EqualTo("a"));
        Assert.That(loaded.Levels[0].Completed, Is.True);
        Assert.That(loaded.Levels[0].StarEarned, Is.True);
        Assert.That(loaded.Levels[0].BestTimeSeconds, Is.EqualTo(3.25f));
        Assert.That(File.ReadAllBytes(Path.Combine(directory, "playerInfo.dat")), Is.EqualTo(legacy));
    }

    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    [TestCase("{bad}")]
    [TestCase("{\"Version\":1,\"Levels\":null}")]
    public void CorruptJsonRemainsByteForByteAndVisible(string json)
    {
        string path = Path.Combine(directory, "progress-v1.json");
        File.WriteAllText(path, json);
        var saves = new SaveService(new JsonFileSaveStorage(directory), catalog);
        Assert.That(saves.LastError, Is.Not.Empty);
        Assert.That(saves.TrySave(), Is.False);
        Assert.That(File.ReadAllText(path), Is.EqualTo(json));
    }

    [Test]
    public void OversizedJsonCannotLoadOrBeOverwritten()
    {
        string path = Path.Combine(directory, "progress-v1.json");
        string contents = new string(' ', JsonFileSaveStorage.MaximumFileBytes + 1);
        File.WriteAllText(path, contents);
        var saves = new SaveService(new JsonFileSaveStorage(directory), catalog);
        Assert.That(saves.TrySave(), Is.False);
        Assert.That(new FileInfo(path).Length, Is.EqualTo(contents.Length));
    }

    [TestCase("{\"Version\":2,\"Levels\":[]}")]
    [TestCase("{\"Version\":2}")]
    [TestCase("{\"Version\":999999999999999999999,\"Levels\":[]}")]
    public void FutureVersionWithBackupCannotBeOverwritten(string future)
    {
        var storage = new JsonFileSaveStorage(directory);
        storage.Save(new SaveData());
        string path = Path.Combine(directory, "progress-v1.json");
        File.Copy(path, path + ".bak");
        File.WriteAllText(path, future);
        var saves = new SaveService(storage, catalog);
        Assert.That(saves.TrySave(), Is.False);
        Assert.Throws<FutureSaveVersionException>(() => storage.Save(new SaveData()));
        Assert.That(File.ReadAllText(path), Is.EqualTo(future));
    }

    [Test]
    public void CorruptPrimaryRecoversBackupAndPreservesCorruptBytes()
    {
        var storage = new JsonFileSaveStorage(directory);
        var saves = new SaveService(storage, catalog);
        var progression = new ProgressionService(catalog, saves);
        progression.RecordCompletion("a", 2);
        progression.RecordCompletion("b", 3);
        string path = Path.Combine(directory, "progress-v1.json");
        File.WriteAllText(path, "broken interrupted primary");
        var recovered = new SaveService(storage, catalog);
        Assert.That(recovered.LastError, Does.Contain("backup"));
        Assert.That(recovered.Data.Levels.Count, Is.EqualTo(1));
        Assert.That(File.ReadAllText(Directory.GetFiles(directory, "*.corrupt-*").Single()), Is.EqualTo("broken interrupted primary"));
        Assert.That(recovered.TrySave(), Is.True);
    }

    [Test]
    public void TemporaryWriteFailureRetainsPrimaryAndRetrySucceeds()
    {
        var storage = new JsonFileSaveStorage(directory);
        var saves = new SaveService(storage, catalog);
        var progression = new ProgressionService(catalog, saves);
        progression.RecordCompletion("a", 2);
        string path = Path.Combine(directory, "progress-v1.json");
        string original = File.ReadAllText(path);
        Directory.CreateDirectory(path + ".tmp");
        Assert.That(progression.RecordCompletion("b", 3).SaveSucceeded, Is.False);
        Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        Directory.Delete(path + ".tmp");
        Assert.That(saves.TrySave(), Is.True);
        Assert.That(storage.Load().Levels.Count, Is.EqualTo(2));
    }

    [Test]
    public void FutureBackupCannotBeOverwrittenEvenWhenPrimaryIsMissing()
    {
        string path = Path.Combine(directory, "progress-v1.json");
        const string future = "{\"Version\":2,\"Levels\":[]}";
        File.WriteAllText(path + ".bak", future);
        var storage = new JsonFileSaveStorage(directory);
        Assert.Throws<FutureSaveVersionException>(() => storage.Save(new SaveData()));
        Assert.That(File.Exists(path), Is.False);
        Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(future));
        File.WriteAllText(path, "{\"Version\":1,\"Levels\":[]}");
        Assert.Throws<FutureSaveVersionException>(() => storage.Save(new SaveData()));
        Assert.That(File.ReadAllText(path + ".bak"), Is.EqualTo(future));
    }

    [Test]
    public void MissingPrimaryRecoversBackupWithVisibleStatus()
    {
        string path = Path.Combine(directory, "progress-v1.json");
        File.WriteAllText(path + ".bak", "{\"Version\":1,\"Levels\":[]}");
        var saves = new SaveService(new JsonFileSaveStorage(directory), catalog);
        Assert.That(saves.LastError, Does.Contain("backup"));
        Assert.That(File.Exists(path), Is.True);
        Assert.That(saves.TrySave(), Is.True);
        Assert.That(saves.LastError, Is.Null);
    }

    [Test]
    public void InterruptedTemporaryFileDoesNotOverrideAuthoritativeProgress()
    {
        var storage = new JsonFileSaveStorage(directory);
        var saves = new SaveService(storage, catalog);
        new ProgressionService(catalog, saves).RecordCompletion("a", 2);
        string path = Path.Combine(directory, "progress-v1.json");
        File.WriteAllText(path + ".tmp", "unfinished replacement");
        var recovered = new SaveService(storage, catalog);
        Assert.That(recovered.Data.Levels.Single().LevelId, Is.EqualTo("a"));
        Assert.That(recovered.TrySave(), Is.True);
        Assert.That(File.Exists(path + ".tmp"), Is.False);
    }

    [Test]
    public void FutureSaveAppearingAfterLoadRemainsProtected()
    {
        var storage = new JsonFileSaveStorage(directory);
        var saves = new SaveService(storage, catalog);
        string path = Path.Combine(directory, "progress-v1.json");
        const string future = "{\"Version\":2,\"Levels\":[]}";
        File.WriteAllText(path, future);
        var progression = new ProgressionService(catalog, saves);
        Assert.That(progression.RecordCompletion("a", 2).SaveSucceeded, Is.False);
        Assert.That(progression.IsUnlocked("b"), Is.True);
        Assert.That(File.ReadAllText(path), Is.EqualTo(future));
    }

    [Test]
    public void InvalidUtf8IsPreservedAndReported()
    {
        string path = Path.Combine(directory, "progress-v1.json");
        byte[] invalid = { 123, 255, 125 };
        File.WriteAllBytes(path, invalid);
        var saves = new SaveService(new JsonFileSaveStorage(directory), catalog);
        Assert.That(saves.LastError, Does.Contain("UTF-8"));
        Assert.That(saves.TrySave(), Is.False);
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(invalid));
    }
    private static LevelDefinition Level(string id, int index, float star) => new LevelDefinition
    {
        Id = id, WorldId = "world-1", SceneName = "Scene" + index, DisplayName = "Level " + index,
        LegacyIndex = index, StarTimeSeconds = star
    };

    private sealed class MemoryStorage : ISaveStorage
    {
        public SaveData Loaded;
        public bool FailLoad, FailSave;
        public int SaveCount;
        public SaveData Load()
        {
            if (FailLoad) throw new IOException("Simulated unreadable save.");
            return Loaded;
        }
        public void Save(SaveData data)
        {
            SaveCount++;
            if (FailSave) throw new IOException("Simulated write failure.");
            Loaded = data;
        }
    }
}
