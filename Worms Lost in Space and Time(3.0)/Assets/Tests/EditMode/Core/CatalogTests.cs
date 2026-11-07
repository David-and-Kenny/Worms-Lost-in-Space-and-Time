using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Worms.Configuration;
using Worms.Core;

public sealed class CatalogTests
{
    [Test]
    public void FinalPublishedLevelHasOneVisibleExitAndNoNextLevel()
    {
        var previous = UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/World2/Level20.unity");
            var finish = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Finish>()).Single();
            var trigger = finish.GetComponent<BoxCollider2D>();
            Assert.That(trigger.enabled && trigger.isTrigger && finish.gameObject.activeInHierarchy, Is.True);
            Assert.That(finish.GetComponentsInChildren<TextMesh>().Single().text, Is.EqualTo("EXIT"));
            float rightmostPlatform = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WheelSurface>())
                .Max(wheel => wheel.GetComponent<Collider2D>().bounds.max.x);
            Assert.That(trigger.bounds.min.x, Is.GreaterThan(rightmostPlatform));
            Assert.That(Resources.Load<LevelCatalog>("Configuration/LevelCatalog").GetNext("level-20"), Is.Null);
        }
        finally
        {
            if (previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path)))
                UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(previous);
            else UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        }
    }

    [Test]
    public void PublishedCatalogCoversExactlyTwentyEnabledGameplayScenes()
    {
        var catalog = Resources.Load<LevelCatalog>("Configuration/LevelCatalog");
        Assert.That(catalog, Is.Not.Null, "Run LevelCatalogMigration.Apply before acceptance tests.");
        Assert.That(catalog.Levels.Count, Is.EqualTo(20));
        var enabled = new HashSet<string>(EditorBuildSettings.scenes.Where(scene => scene.enabled)
            .Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path)));
        var ids = new HashSet<string>();
        var scenes = new HashSet<string>();
        var indices = new HashSet<int>();
        for (int index = 0; index < catalog.Levels.Count; index++)
        {
            LevelDefinition level = catalog.Levels[index];
            int number = index + 1;
            Assert.That(level.Id, Is.EqualTo("level-" + number.ToString("00")));
            Assert.That(level.SceneName, Is.EqualTo("Level" + number));
            Assert.That(level.WorldId, Is.EqualTo(number <= 10 ? "world-1" : "world-2"));
            Assert.That(level.LegacyIndex, Is.EqualTo(number));
            Assert.That(level.StarTimeSeconds, Is.EqualTo(number == 1 ? 10 : number == 2 ? 15 : 0));
            Assert.That(ids.Add(level.Id) && scenes.Add(level.SceneName) && indices.Add(level.LegacyIndex), Is.True);
            Assert.That(enabled.Contains(level.SceneName), Is.True);
            Assert.That(catalog.FindByScene(level.SceneName), Is.SameAs(level));
            Assert.That(catalog.GetNext(level.Id)?.Id, Is.EqualTo(index == 19 ? null : catalog.Levels[index + 1].Id));
        }
        Assert.That(catalog.GetLevel("unknown"), Is.Null);
        Assert.That(catalog.FindByScene("World3"), Is.Null);
        Assert.That(catalog.GetNext("unknown"), Is.Null);
    }

    [Test]
    public void InvalidCatalogDefinitionsAreRejected()
    {
        var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
        try
        {
            var valid = new LevelDefinition { Id = "a", WorldId = "world-1", SceneName = "A",
                DisplayName = "A", LegacyIndex = 1, StarTimeSeconds = 10 };
            Assert.Throws<System.ArgumentException>(() => catalog.SetLevels(new[] { valid, valid }));
            valid.WorldId = "world-3";
            Assert.Throws<System.ArgumentException>(() => catalog.SetLevels(new[] { valid }));
            valid.WorldId = "world-1";
            valid.StarTimeSeconds = float.NaN;
            Assert.Throws<System.ArgumentException>(() => catalog.SetLevels(new[] { valid }));
            valid.StarTimeSeconds = -1;
            Assert.Throws<System.ArgumentException>(() => catalog.SetLevels(new[] { valid }));
        }
        finally { Object.DestroyImmediate(catalog); }
    }
}
