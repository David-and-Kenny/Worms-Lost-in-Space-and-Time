using System;
using System.Collections.Generic;
using UnityEngine;
using Worms.Core;

namespace Worms.Configuration
{
    [CreateAssetMenu(menuName = "Worms/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject, ILevelCatalog
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();
        public IReadOnlyList<LevelDefinition> Levels => levels.AsReadOnly();

        public LevelDefinition GetLevel(string id) => levels.Find(level => level.Id == id);
        public LevelDefinition FindByScene(string sceneName) => levels.Find(level => level.SceneName == sceneName);
        public LevelDefinition GetNext(string id)
        {
            int index = levels.FindIndex(level => level.Id == id);
            return index >= 0 && index + 1 < levels.Count ? levels[index + 1] : null;
        }

        public void SetLevels(IList<LevelDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var scenes = new HashSet<string>(StringComparer.Ordinal);
            var indices = new HashSet<int>();
            foreach (LevelDefinition level in definitions)
            {
                if (level == null || string.IsNullOrWhiteSpace(level.Id) || string.IsNullOrWhiteSpace(level.SceneName)
                    || string.IsNullOrWhiteSpace(level.DisplayName) || (level.WorldId != "world-1" && level.WorldId != "world-2")
                    || level.LegacyIndex < 1 || float.IsNaN(level.StarTimeSeconds) || float.IsInfinity(level.StarTimeSeconds)
                    || level.StarTimeSeconds < 0 || !ids.Add(level.Id) || !scenes.Add(level.SceneName) || !indices.Add(level.LegacyIndex))
                    throw new ArgumentException("Catalog entries require unique identities, scenes, indices, valid worlds and finite nonnegative thresholds.");
            }
            levels = new List<LevelDefinition>(definitions);
        }

        private void OnEnable() => SetLevels(levels);

        public void SetLevelsForTests(IList<LevelDefinition> definitions) => SetLevels(definitions);
    }
}
