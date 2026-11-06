using System;
using System.Collections.Generic;
using System.IO;

namespace Worms.Core
{
    /// <summary>Validates persisted progress and keeps unsaved completions available for retry.</summary>
    public sealed class SaveService
    {
        private readonly ISaveStorage storage;
        private readonly ILevelCatalog catalog;
        private bool loadBlocked;
        public SaveData Data { get; private set; }
        public string LastError { get; private set; }

        public SaveService(ISaveStorage storage, ILevelCatalog catalog)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Data = new SaveData();
            try
            {
                Data = ValidateAndCopy(storage.Load());
                LastError = (storage as ISaveStorageDiagnostics)?.Notice;
            }
            catch (Exception error) when (IsExpected(error))
            {
                loadBlocked = true;
                LastError = "Could not load progress; existing data was preserved. " + error.Message;
            }
        }

        public bool TrySave()
        {
            try
            {
                if (loadBlocked)
                {
                    // Never replace unreadable data with fresh progress. External repair/removal permits retry.
                    SaveData recovered = ValidateAndCopy(storage.Load());
                    MergeRecovered(recovered);
                    loadBlocked = false;
                }
                SaveData snapshot = ValidateAndCopy(Data);
                storage.Save(snapshot);
                LastError = null;
                return true;
            }
            catch (Exception error) when (IsExpected(error))
            {
                LastError = "Could not save progress; retry is available. " + error.Message;
                return false;
            }
        }

        private SaveData ValidateAndCopy(SaveData source)
        {
            if (source == null) return new SaveData();
            if (source.Version != 1) throw new InvalidDataException("Unsupported save version " + source.Version + ".");
            if (source.Levels == null || source.Levels.Count > catalog.Levels.Count)
                throw new InvalidDataException("Invalid progress list.");
            var result = new SaveData();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (LevelProgress item in source.Levels)
            {
                LevelDefinition level = item == null ? null : catalog.GetLevel(item.LevelId);
                if (level == null || !seen.Add(item.LevelId) || !IsFinite(item.BestTimeSeconds) || item.BestTimeSeconds < 0
                    || (!item.Completed && (item.StarEarned || item.BestTimeSeconds != 0))
                    || (item.StarEarned && (level.StarTimeSeconds <= 0 || item.BestTimeSeconds >= level.StarTimeSeconds)))
                    throw new InvalidDataException("Invalid, duplicate or unknown level progress.");
                result.Levels.Add(new LevelProgress { LevelId = item.LevelId, Completed = item.Completed,
                    StarEarned = item.StarEarned, BestTimeSeconds = item.BestTimeSeconds });
            }
            return result;
        }

        private void MergeRecovered(SaveData recovered)
        {
            foreach (LevelProgress old in recovered.Levels)
            {
                LevelProgress current = Data.Levels.Find(item => item.LevelId == old.LevelId);
                if (current == null)
                {
                    Data.Levels.Add(old);
                    continue;
                }
                if (old.Completed && (!current.Completed || old.BestTimeSeconds < current.BestTimeSeconds))
                    current.BestTimeSeconds = old.BestTimeSeconds;
                current.Completed |= old.Completed;
                current.StarEarned |= old.StarEarned;
            }
        }

        private static bool IsExpected(Exception error) => error is IOException || error is InvalidDataException || error is UnauthorizedAccessException
            || error is ArgumentException || error is FormatException || error is NotSupportedException || error is System.Security.SecurityException;
        internal static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
