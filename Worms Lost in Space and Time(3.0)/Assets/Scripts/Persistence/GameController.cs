using UnityEngine;
using Worms;
using Worms.Core;

/// <summary>Compatibility facade. Catalog progression owns all persistence and completion rules.</summary>
public class GameController
{
    public int levelcount;
    public bool[] tempStarArray;
    public GameController()
    {
        GameServices.EnsureInitialized();
        levelcount = Load();
        tempStarArray = LoadArray();
    }

    /// <summary>Legacy callers have no elapsed-time argument. Measure scene time; catalog rules determine the star.</summary>
    public void Save(int level, bool success)
    {
        foreach (LevelDefinition definition in GameServices.Catalog.Levels)
        {
            if (definition.LegacyIndex != level) continue;
            GameServices.Progression.RecordCompletion(definition.Id, Time.timeSinceLevelLoad);
            levelcount = Load();
            tempStarArray = LoadArray();
            return;
        }
    }

    public int Load()
    {
        GameServices.EnsureInitialized();
        var level = GameServices.Catalog.GetLevel(GameServices.Progression.GetContinueLevelId());
        return level == null ? 1 : level.LegacyIndex;
    }

    public bool[] LoadArray()
    {
        GameServices.EnsureInitialized();
        var stars = new bool[31];
        foreach (LevelDefinition level in GameServices.Catalog.Levels)
            if (level.LegacyIndex >= 0 && level.LegacyIndex < stars.Length)
                stars[level.LegacyIndex] = GameServices.Progression.HasStar(level.Id);
        return stars;
    }
}
