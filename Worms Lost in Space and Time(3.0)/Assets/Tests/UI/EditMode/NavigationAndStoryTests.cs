using System;
using System.Collections.Generic;
using NUnit.Framework;
using Worms.Core;
using Worms.Navigation;
using Worms.UI;

public sealed class NavigationAndStoryTests
{
    private sealed class Catalog : ILevelCatalog
    {
        public IReadOnlyList<LevelDefinition> Levels { get; } = new List<LevelDefinition>
        {
            new LevelDefinition { Id = "first", SceneName = "Level1", WorldId = "world-1", LegacyIndex = 1 },
            new LevelDefinition { Id = "last", SceneName = "Level20", WorldId = "world-2", LegacyIndex = 20 }
        };
        public LevelDefinition GetLevel(string id) { foreach (var item in Levels) if (item.Id == id) return item; return null; }
        public LevelDefinition FindByScene(string name) { foreach (var item in Levels) if (item.SceneName == name) return item; return null; }
        public LevelDefinition GetNext(string id) => id == "first" ? Levels[1] : null;
    }
    private sealed class Storage : ISaveStorage
    {
        public SaveData Load() => null;
        public void Save(SaveData data) { }
    }
    private sealed class Loader : ISceneLoader
    {
        public string CurrentSceneName { get; set; } = "Level20";
        public string Loaded;
        public Action Completed;
        public int Loads;
        public bool QuitCalled;
        public bool CanLoad(string scene) => scene != "missing";
        public void Load(string scene, Action completed) { Loaded = scene; Completed = completed; Loads++; }
        public void Quit() => QuitCalled = true;
    }
    [Test]
    public void RejectsLockedUnknownAndEmptyWorldAndCoalescesPendingPresses()
    {
        var catalog = new Catalog();
        var progression = new ProgressionService(catalog, new SaveService(new Storage(), catalog));
        var loader = new Loader();
        var navigator = new SceneNavigator(loader, catalog, progression);
        Assert.That(navigator.OpenLevel("last"), Is.False);
        Assert.That(navigator.OpenLevel("unknown"), Is.False);
        Assert.That(navigator.OpenWorld("world-3"), Is.False);
        Assert.That(navigator.RouteLegacyScene("Level21"), Is.False);
        Assert.That(navigator.Continue(), Is.True);
        Assert.That(loader.Loaded, Is.EqualTo("Level1"));
        Assert.That(navigator.MainMenu(), Is.False);
        Assert.That(loader.Loads, Is.EqualTo(1));
        loader.Completed();
        Assert.That(navigator.BackToWorld(), Is.True);
        Assert.That(loader.Loaded, Is.EqualTo("World2"));
    }
    [Test]
    public void OnlyAcceptedNavigationConsumesTheCurrentPointerPress()
    {
        var catalog = new Catalog();
        var progression = new ProgressionService(catalog, new SaveService(new Storage(), catalog));
        var gate = new PointerPressGate();
        var navigator = new SceneNavigator(new Loader(), catalog, progression, gate);
        Assert.That(navigator.OpenLevel("unknown"), Is.False);
        Assert.That(gate.Sample(true, false), Is.True);
        Assert.That(navigator.Continue(), Is.True);
        Assert.That(gate.Sample(true, false), Is.False);
        gate.Sample(false, false);
        Assert.That(gate.Sample(true, false), Is.True);
    }
    [Test]
    public void DirectSceneEntryCanRestartEvenWhenMenuLevelIsLocked()
    {
        var catalog = new Catalog();
        var progression = new ProgressionService(catalog, new SaveService(new Storage(), catalog));
        var loader = new Loader();
        var navigator = new SceneNavigator(loader, catalog, progression);
        Assert.That(navigator.OpenLevel("last"), Is.False);
        Assert.That(navigator.Restart(), Is.True);
        Assert.That(loader.Loaded, Is.EqualTo("Level20"));
    }
    [Test]
    public void FinalResultHasNoNextRouteAndAllMenuRoutesRemainValid()
    {
        var catalog = new Catalog();
        var progression = new ProgressionService(catalog, new SaveService(new Storage(), catalog));
        progression.RecordCompletion("first", 1);
        var result = progression.RecordCompletion("last", 2);
        var loader = new Loader();
        var navigator = new SceneNavigator(loader, catalog, progression);
        Assert.That(navigator.OpenNext(result), Is.False);
        Assert.That(loader.Loads, Is.Zero);
        Assert.That(navigator.Restart(), Is.True);
        Assert.That(loader.Loaded, Is.EqualTo("Level20"));
        loader.Completed();
        Assert.That(navigator.RouteLegacyScene("World1"), Is.True);
        loader.Completed();
        Assert.That(navigator.MainMenu(), Is.True);
        Assert.That(loader.Loaded, Is.EqualTo("StorySelect"));
        navigator.Quit();
        Assert.That(loader.QuitCalled, Is.True);
    }
    [Test]
    public void StoryNeedsReleaseAndLaterPressAfterReveal()
    {
        var story = new StoryFlow("A story that takes time", 0.1f);
        Assert.That(story.Update(0, true), Is.False);
        Assert.That(story.VisibleText, Is.EqualTo("A story that takes time"));
        Assert.That(story.Update(5, true), Is.False, "Holding beyond the delay must not advance.");
        story.Update(0, false);
        Assert.That(story.Update(0, true), Is.True);
        story.Update(1, false);
        Assert.That(story.Update(1, true), Is.False, "Advance is emitted once.");
    }
    [Test]
    public void StoryShortSecondTapIsGuardedAndTypewriterCompletes()
    {
        var story = new StoryFlow("abc", 1);
        story.Update(0, true);
        story.Update(0.01f, false);
        Assert.That(story.Update(0.01f, true), Is.False);
        story.Update(0.2f, false);
        Assert.That(story.Update(0, true), Is.True);
        var automatic = new StoryFlow("abc", 0.1f);
        automatic.Update(1, false);
        Assert.That(automatic.VisibleText, Is.EqualTo("abc"));
        Assert.That(automatic.Update(0, true), Is.True);
    }
}
