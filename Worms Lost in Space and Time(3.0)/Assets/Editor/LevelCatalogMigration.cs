using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Worms.Configuration;
using Worms.Core;

/// <summary>Reads instantiated legacy scene components without modifying scenes.</summary>
public static class LevelCatalogMigration
{
    private const string AssetPath = "Assets/Resources/Configuration/LevelCatalog.asset";

    [Serializable]
    public sealed class SceneAudit
    {
        public string ScenePath, Id, WorldId;
        public int LegacyIndex, FinishCount, SaveGameCount;
        public int[] EffectiveCurrentLevels;
        public float[] StarTimes;
        public string[] NextLevels;
        public string Finding;
    }

    [Serializable]
    private sealed class AuditDocument
    {
        public List<SceneAudit> Scenes = new List<SceneAudit>();
    }

    [MenuItem("Worms/Migration/Create Level Catalog")]
    public static void Apply()
    {
        SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
        var definitions = new List<LevelDefinition>();
        var audit = new AuditDocument();
        try
        {
            for (int index = 1; index <= 20; index++)
            {
                string name = "Level" + index;
                string path = "Assets/Scenes/World" + (index <= 10 ? "1/" : "2/") + name + ".unity";
                if (!File.Exists(path)) throw new FileNotFoundException("Missing gameplay scene.", path);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var finishes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Finish>(true)).ToArray();
                var saves = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SaveGame>(true)).ToArray();
                float[] thresholds = saves.Select(save => save.starTime).Distinct().ToArray();
                if (thresholds.Length > 1) throw new InvalidDataException("Ambiguous star thresholds in " + path);
                float threshold = thresholds.Length == 0 ? 0f : thresholds[0];
                if (float.IsNaN(threshold) || float.IsInfinity(threshold) || threshold < 0)
                    throw new InvalidDataException("Invalid star threshold in " + path);
                string id = "level-" + index.ToString("00");
                string world = index <= 10 ? "world-1" : "world-2";
                var entry = new SceneAudit
                {
                    ScenePath = path, Id = id, WorldId = world, LegacyIndex = index,
                    FinishCount = finishes.Length, SaveGameCount = saves.Length,
                    EffectiveCurrentLevels = saves.Select(save => save.currentLevel).ToArray(),
                    StarTimes = saves.Select(save => save.starTime).ToArray(),
                    NextLevels = finishes.Select(finish => finish.nextLevel).ToArray(),
                    Finding = finishes.Length == 0 ? "CONTENT GAP: no authored Finish; natural completion blocked. No geometry added."
                        : finishes.Length != 1 || saves.Length != 1 ? "REVIEW: duplicate or mismatched exit components."
                        : saves[0].currentLevel != index || finishes[0].nextLevel != (index == 20 ? null : "Level" + (index + 1))
                        ? "Catalog intentionally corrects inherited legacy identity/route." : "Authored exit metadata retained."
                };
                audit.Scenes.Add(entry);
                definitions.Add(new LevelDefinition { Id = id, WorldId = world, SceneName = name,
                    DisplayName = "Level " + index, LegacyIndex = index, StarTimeSeconds = threshold });
            }
        }
        finally
        {
            if (!Application.isBatchMode && original.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(original);
        }

        Directory.CreateDirectory("Assets/Resources/Configuration");
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }
        catalog.SetLevels(definitions);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string docs = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs"));
        Directory.CreateDirectory(docs);
        File.WriteAllText(Path.Combine(docs, "CATALOG_METADATA.json"), JsonUtility.ToJson(audit, true) + "\n");
        var csv = new StringBuilder("scenePath,id,world,legacyIndex,finishCount,saveGameCount,effectiveCurrentLevels,starTimes,nextLevels,finding\n");
        foreach (SceneAudit row in audit.Scenes)
        {
            string[] cells = { row.ScenePath, row.Id, row.WorldId, row.LegacyIndex.ToString(), row.FinishCount.ToString(),
                row.SaveGameCount.ToString(), string.Join(";", row.EffectiveCurrentLevels),
                string.Join(";", row.StarTimes.Select(time => time.ToString(CultureInfo.InvariantCulture))),
                string.Join(";", row.NextLevels), row.Finding };
            csv.AppendLine(string.Join(",", cells.Select(cell => "\"" + (cell ?? "").Replace("\"", "\"\"") + "\"")));
        }
        File.WriteAllText(Path.Combine(docs, "CATALOG_METADATA.csv"), csv.ToString());
        File.WriteAllText(Path.Combine(docs, "CATALOG_REVIEW.md"),
            "# Effective authored catalog metadata\n\nGenerated by LevelCatalogMigration.Apply from instantiated scene components; scenes are never saved.\n\n" +
            "The CSV and JSON preserve effective inherited values for review. Catalog order corrects legacy identities/routes, uses stable IDs, and disables timed stars at threshold zero.\n\n" +
            string.Join("\n", audit.Scenes.Select(row => "- " + row.Id + ": " + row.Finding)) + "\n");
        Debug.Log("Created catalog and effective scene audit at " + docs + ". Review content gaps before acceptance.");
    }
}
