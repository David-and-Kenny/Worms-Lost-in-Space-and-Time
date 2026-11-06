using System;
using System.Collections.Generic;

namespace Worms.Core
{
    /// <summary>Authored identity and timing metadata for one playable level.</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public string Id;
        public string WorldId;
        public string SceneName;
        public string DisplayName;
        public int LegacyIndex;
        public float StarTimeSeconds;
    }

    /// <summary>Provides ordered, authored level metadata. Missing IDs and scenes return null.</summary>
    public interface ILevelCatalog
    {
        IReadOnlyList<LevelDefinition> Levels { get; }
        LevelDefinition GetLevel(string id);
        LevelDefinition FindByScene(string sceneName);
        LevelDefinition GetNext(string id);
    }

    /// <summary>Versioned, serializable progression data.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public List<LevelProgress> Levels = new List<LevelProgress>();
    }

    [Serializable]
    public sealed class LevelProgress
    {
        public string LevelId;
        public bool Completed;
        public bool StarEarned;
        public float BestTimeSeconds;
    }

    /// <summary>Persistence boundary used by SaveService.</summary>
    public interface ISaveStorage
    {
        SaveData Load();
        void Save(SaveData data);
    }

    public interface ISaveStorageDiagnostics
    {
        string Notice { get; }
    }

    /// <summary>Result of recording one completion. SaveSucceeded reports persistence separately from earned progress.</summary>
    public sealed class CompletionResult
    {
        public string LevelId { get; }
        public float ElapsedSeconds { get; }
        public bool StarEarned { get; }
        public string NextLevelId { get; }
        public bool SaveSucceeded { get; }

        public CompletionResult(string levelId, float elapsedSeconds, bool starEarned, string nextLevelId, bool saveSucceeded)
        {
            LevelId = levelId;
            ElapsedSeconds = elapsedSeconds;
            StarEarned = starEarned;
            NextLevelId = nextLevelId;
            SaveSucceeded = saveSucceeded;
        }
    }

    public enum SessionState
    {
        Playing,
        Completed,
        Dead
    }
}
