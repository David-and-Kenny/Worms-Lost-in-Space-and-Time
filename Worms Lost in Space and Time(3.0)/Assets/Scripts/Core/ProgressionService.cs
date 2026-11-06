using System;

namespace Worms.Core
{
    public sealed class ProgressionService
    {
        private readonly ILevelCatalog catalog;
        private readonly SaveService saves;
        public ProgressionService(ILevelCatalog catalog, SaveService saves)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.saves = saves ?? throw new ArgumentNullException(nameof(saves));
        }

        public bool IsUnlocked(string id)
        {
            int index = IndexOf(id);
            return index >= 0 && index <= HighestUnlockedIndex();
        }

        public bool HasStar(string id) => catalog.GetLevel(id) != null && GetProgress(id)?.StarEarned == true;
        public string GetContinueLevelId() => catalog.Levels.Count == 0 ? null : catalog.Levels[HighestUnlockedIndex()].Id;

        public CompletionResult RecordCompletion(string id, float elapsedSeconds)
        {
            LevelDefinition level = catalog.GetLevel(id);
            if (level == null) throw new ArgumentException("Unknown level ID.", nameof(id));
            if (!SaveService.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            float rounded = CompletionTiming.RoundSeconds(elapsedSeconds);
            bool earned = level.StarTimeSeconds > 0 && rounded < level.StarTimeSeconds;
            LevelProgress progress = GetProgress(id);
            if (progress == null)
            {
                progress = new LevelProgress { LevelId = id };
                saves.Data.Levels.Add(progress);
            }
            if (!progress.Completed || rounded < progress.BestTimeSeconds) progress.BestTimeSeconds = rounded;
            progress.Completed = true;
            progress.StarEarned |= earned;
            bool saved = saves.TrySave();
            return new CompletionResult(id, rounded, earned, catalog.GetNext(id)?.Id, saved);
        }

        private int HighestUnlockedIndex()
        {
            int highest = 0;
            for (int index = 0; index < catalog.Levels.Count; index++)
                if (GetProgress(catalog.Levels[index].Id)?.Completed == true)
                    highest = Math.Min(index + 1, catalog.Levels.Count - 1);
            return highest;
        }
        private int IndexOf(string id)
        {
            for (int index = 0; index < catalog.Levels.Count; index++)
                if (catalog.Levels[index].Id == id) return index;
            return -1;
        }
        private LevelProgress GetProgress(string id) => saves.Data.Levels.Find(item => item.LevelId == id);
    }
}
