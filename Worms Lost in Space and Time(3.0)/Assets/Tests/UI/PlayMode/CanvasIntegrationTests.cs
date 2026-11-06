using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Worms;
using Worms.Core;
using Worms.Navigation;
using Worms.UI;
using Worms.Gameplay;

public sealed class CanvasIntegrationTests
{
    private sealed class Catalog : ILevelCatalog
    {
        public Catalog(string firstSceneName = "Level1")
        {
            ((List<LevelDefinition>)Levels)[0].SceneName = firstSceneName;
        }
        public IReadOnlyList<LevelDefinition> Levels { get; } = new List<LevelDefinition>
        {
            new LevelDefinition { Id = "first", SceneName = "Level1", WorldId = "world-1", LegacyIndex = 1, StarTimeSeconds = 10 },
            new LevelDefinition { Id = "last", SceneName = "Level20", WorldId = "world-2", LegacyIndex = 20 }
        };
        public LevelDefinition GetLevel(string id) { foreach (var l in Levels) if (l.Id == id) return l; return null; }
        public LevelDefinition FindByScene(string scene) { foreach (var l in Levels) if (l.SceneName == scene) return l; return null; }
        public LevelDefinition GetNext(string id) => id == "first" ? Levels[1] : null;
    }
    private sealed class Storage : ISaveStorage
    {
        public bool Fail;
        public int Writes;
        public Exception LoadFailure;
        public SaveData LoadedData;
        public SaveData Load()
        {
            if (LoadFailure != null) throw LoadFailure;
            return LoadedData;
        }
        public void Save(SaveData data) { Writes++; if (Fail) throw new IOException("Test storage unavailable"); }
    }
    private sealed class Loader : ISceneLoader
    {
        public string CurrentSceneName => "Level20";
        public string Loaded;
        public Action Completed;
        public int Count;
        public bool QuitCalled;
        public bool CanLoad(string name) => true;
        public void Load(string name, Action completed) { Loaded = name; Completed = completed; Count++; }
        public void Quit() => QuitCalled = true;
    }
    private Scene fixture;
    private readonly List<Scene> addedScenes = new List<Scene>();
    private Scene original;
    private Storage storage;
    private Loader loader;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        original = SceneManager.GetActiveScene();
        fixture = SceneManager.CreateScene("UI isolated fixture");
        SceneManager.SetActiveScene(fixture);
        storage = new Storage();
        loader = new Loader();
        GameServices.ConfigureForTests(new Catalog(), storage);
        SceneNavigator.ConfigureForTests(new SceneNavigator(loader, GameServices.Catalog, GameServices.Progression));
        yield return null;
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var scene in addedScenes)
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        addedScenes.Clear();
        SceneManager.SetActiveScene(original);
        yield return SceneManager.UnloadSceneAsync(fixture);
        SceneNavigator.ConfigureForTests(null);
        GameServices.ResetForTests();
    }
    private static GameUiView View(string name)
    {
        var prefab = Resources.Load<GameObject>("UI/" + name);
        Assert.That(prefab, Is.Not.Null, "Run UiMigration.Apply to persist editable UI assets.");
        return UnityEngine.Object.Instantiate(prefab).GetComponent<GameUiView>();
    }
    [UnityTest]
    public IEnumerator ActualMenuButtonsRouteAndRapidPressesCoalesce()
    {
        var menu = View("MainMenu");
        yield return null;
        Assert.That(menu.WorldOne, Is.Not.Null);
        Assert.That(menu.WorldTwo, Is.Not.Null);
        Assert.That(menu.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(5), "No empty World3 destination.");
        menu.Primary.onClick.Invoke();
        menu.WorldTwo.onClick.Invoke();
        Assert.That(loader.Count, Is.EqualTo(1));
        Assert.That(loader.Loaded, Is.EqualTo("Level1"));
        loader.Completed();
        menu.WorldTwo.onClick.Invoke();
        Assert.That(loader.Loaded, Is.EqualTo("World2"));
        loader.Completed();
        menu.WorldOne.onClick.Invoke();
        Assert.That(loader.Loaded, Is.EqualTo("World1"));
        menu.Quit.onClick.Invoke();
        Assert.That(loader.QuitCalled, Is.True);
    }
    [UnityTest]
    public IEnumerator ActualResultBindsTimeStarFinalRouteAndSaveRetryWithoutRecompletion()
    {
        var completion = View("CompletionPanel");
        storage.Fail = true;
        var result = GameServices.Progression.RecordCompletion("first", 2.34f);
        completion.BindCompletion(result);
        Assert.That(completion.Body.text, Does.Contain("2.34"));
        Assert.That(completion.Body.text, Does.Contain("Star"));
        Assert.That(completion.SaveStatus.text, Does.Contain("Test storage unavailable"));
        Assert.That(completion.RetrySave.gameObject.activeSelf, Is.True);
        storage.Fail = false;
        completion.RetrySave.onClick.Invoke();
        Assert.That(storage.Writes, Is.EqualTo(2));
        Assert.That(completion.RetrySave.gameObject.activeSelf, Is.False);
        Assert.That(GameServices.Progression.HasStar("first"), Is.True);
        completion.Primary.onClick.Invoke();
        Assert.That(loader.Loaded, Is.EqualTo("Level20"));
        loader.Completed();
        completion.BindCompletion(GameServices.Progression.RecordCompletion("last", 4));
        Assert.That(completion.Title.text, Is.EqualTo("Journey complete!"));
        Assert.That(completion.Primary.gameObject.activeSelf, Is.False);
        completion.Back.onClick.Invoke();
        Assert.That(loader.Loaded, Is.EqualTo("World2"));
        loader.Completed();
        completion.Menu.onClick.Invoke();
        Assert.That(loader.Loaded, Is.EqualTo("StorySelect"));
        yield return null;
    }
    [UnityTest]
    public IEnumerator AuthoredWorldScenesKeepLevelEventsAndHideObsoleteHeaderAndBack()
    {
        foreach (string name in new[] { "World1", "World2" })
        {
            yield return SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            addedScenes.Add(scene);
            int boundLevels = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    var bindings = canvas.GetComponentsInChildren<LevelButtonBinding>(true);
                    if (bindings.Length == 0) continue;
                    boundLevels += bindings.Length;
                    foreach (var binding in bindings)
                    {
                        Assert.That(binding.Button, Is.Not.Null);
                        Assert.That(binding.Button.targetGraphic, Is.Not.Null, "Original level artwork remains bound.");
                        Assert.That(binding.Button.onClick.GetPersistentEventCount(), Is.GreaterThan(0));
                        Assert.That(binding.Button.onClick.GetPersistentMethodName(0), Is.EqualTo("Selectlevel"));
                        var authoredLevel = Resources.Load<Worms.Configuration.LevelCatalog>("Configuration/LevelCatalog").GetLevel(binding.LevelId);
                        Assert.That(binding.GetComponentInChildren<Text>().text, Is.EqualTo(authoredLevel.DisplayName));
                        if (authoredLevel.LegacyIndex != 1)
                            Assert.That(binding.Lock, Is.Not.Null, name + " retains a visible lock for every locked level.");
                        foreach (var graphic in binding.GetComponentsInChildren<Graphic>(true))
                            Assert.That(graphic.raycastTarget, Is.EqualTo(graphic == binding.Button.targetGraphic),
                                name + " level artwork alone receives pointer input: " + graphic.name);
                        foreach (var badge in binding.GetComponentsInChildren<Button>(true))
                            if (badge != binding.Button)
                                Assert.That(badge.navigation.mode, Is.EqualTo(Navigation.Mode.None));
                    }
                    foreach (var text in canvas.GetComponentsInChildren<Text>(true))
                        if (text.GetComponentInParent<LevelButtonBinding>() == null)
                            Assert.That(text.gameObject.activeInHierarchy, Is.False, name + " obsolete text: " + text.name);
                    foreach (var button in canvas.GetComponentsInChildren<Button>(true))
                        if (button.GetComponentInParent<LevelButtonBinding>() == null)
                            Assert.That(button.gameObject.activeInHierarchy, Is.False, name + " obsolete navigation: " + button.name);
                    foreach (var image in canvas.GetComponentsInChildren<Image>(true))
                        if (image.name == "ImageSelectLevel")
                            Assert.That(image.gameObject.activeInHierarchy, Is.False, name + " old rotated header");
                }
            }
            Assert.That(boundLevels, Is.EqualTo(10), name + " must retain every authored level button.");
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }

    private sealed class InputState : IPlayerInputState
    {
        public bool KeyboardJumpHeld { get; set; }
        public bool PointerHeld { get; set; }
        public bool TouchHeld { get; set; }
        public bool PointerOverControl { get; set; }
    }

    [UnityTest]
    public IEnumerator MenuTouchCannotLaunchPlayerAndReleaseWhileFallingEnablesNextTouch()
    {
        var state = new InputState { PointerHeld = true, TouchHeld = true };
        var gate = new PointerPressGate();
        gate.BlockUntilRelease();
        var body = new GameObject("Input fixture player").AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        var player = body.gameObject.AddComponent<Player>();
        player.InputSource = new PlayerInput(state, gate);
        var wheel = new GameObject("Input fixture wheel").AddComponent<Wheel>();
        wheel.speed = 0;
        wheel.holder = new GameObject("Input fixture holder").transform;
        wheel.holder.SetParent(wheel.transform);
        player.AttachToWheel(wheel, Vector2.right);
        yield return null;
        Assert.That(player.playerAttached, Is.True, "The menu's held finger must not launch the player.");
        player.detatch();
        state.PointerHeld = state.TouchHeld = false;
        yield return null;
        var nextWheel = new GameObject("Next fixture wheel").AddComponent<Wheel>();
        nextWheel.speed = 0;
        nextWheel.holder = new GameObject("Next fixture holder").transform;
        nextWheel.holder.SetParent(nextWheel.transform);
        state.PointerHeld = state.TouchHeld = true;
        player.AttachToWheel(nextWheel, Vector2.right);
        yield return null;
        Assert.That(player.playerAttached, Is.False, "The next gameplay touch must launch after an airborne release.");
        Assert.That(Vector2.Distance(body.linearVelocity, (Vector2)player.transform.up * 10), Is.LessThan(0.001f));
    }

    [UnityTest]
    public IEnumerator CurrentPositionRaycastExcludesActualMenuButtonsFromAnywhereRetry()
    {
        new GameObject("Fixture EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var death = View("DeathPanel");
        yield return null;
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)death.Menu.transform;
        Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        Assert.That(UiPointer.IsOverControl(center), Is.True, "A fresh button press must be excluded even before EventSystem.Update.");
        Assert.That(UiPointer.IsOverControl(new Vector2(2, 2)), Is.False, "Empty panel space remains an anywhere-retry target.");
    }
    [UnityTest]
    public IEnumerator SceneHostTerminalEventShowsOneResultPanel()
    {
        GameServices.ConfigureForTests(new Catalog(fixture.name), storage);
        var presenter = new GameObject("UI presenter").AddComponent<GameUiPresenter>();
        presenter.Hud = View("GameplayHud");
        presenter.Completion = View("CompletionPanel");
        presenter.Death = View("DeathPanel");
        var host = LevelSessionHost.GetOrCreate();
        yield return null;
        Assert.That(presenter.Hud.gameObject.activeSelf, Is.True);
        Assert.That(presenter.Completion.gameObject.activeSelf, Is.False);
        Assert.That(presenter.Death.gameObject.activeSelf, Is.False);
        Assert.That(host.Session.TryComplete(), Is.True);
        Assert.That(host.Session.TryComplete(), Is.False);
        Assert.That(presenter.Hud.gameObject.activeSelf, Is.False);
        Assert.That(presenter.Completion.gameObject.activeSelf, Is.True);
        Assert.That(presenter.Death.gameObject.activeSelf, Is.False);
        Assert.That(storage.Writes, Is.EqualTo(1));
    }
    [UnityTest]
    public IEnumerator CorruptFutureAndLegacyNoticesAreVisibleWithoutTouchingUserFiles()
    {
        storage.LoadFailure = new InvalidDataException("Malformed JSON fixture");
        GameServices.ConfigureForTests(new Catalog(), storage);
        var menu = View("MainMenu");
        Assert.That(menu.SaveStatus.text, Does.Contain("Malformed JSON fixture"));
        Assert.That(menu.RetrySave.gameObject.activeSelf, Is.True);
        storage.LoadFailure = null;
        storage.LoadedData = new SaveData { Version = 999 };
        GameServices.ConfigureForTests(new Catalog(), storage);
        menu.RefreshSaveStatus();
        Assert.That(menu.SaveStatus.text, Does.Contain("Unsupported save version"));
        menu.RetrySave.onClick.Invoke();
        Assert.That(storage.Writes, Is.Zero, "A future schema must not be overwritten by a retry.");
        storage.LoadedData = null;
        GameServices.ConfigureForTests(new Catalog(), storage, true);
        menu.RefreshSaveStatus();
        Assert.That(menu.SaveStatus.text, Does.Contain("not imported"));
        Assert.That(menu.SaveStatus.text, Does.Contain("fresh JSON progress"));
        Assert.That(menu.RetrySave.gameObject.activeSelf, Is.False);
        yield return null;
    }
    [UnityTest]
    public IEnumerator LevelBindingsDisplayLocksAndMonotonicStars()
    {
        var go = new GameObject("Bound level", typeof(RectTransform), typeof(Image), typeof(Button));
        var binding = go.AddComponent<LevelButtonBinding>();
        binding.LevelId = "last";
        binding.Button = go.GetComponent<Button>();
        binding.Lock = new GameObject("Lock");
        binding.Lock.transform.SetParent(go.transform);
        binding.Star = new GameObject("Star");
        binding.Star.transform.SetParent(go.transform);
        binding.Refresh();
        Assert.That(binding.Button.interactable, Is.False);
        Assert.That(binding.Lock.activeSelf, Is.True);
        GameServices.Progression.RecordCompletion("first", 1);
        binding.Refresh();
        Assert.That(binding.Button.interactable, Is.True);
        Assert.That(binding.Lock.activeSelf, Is.False);
        binding.LevelId = "first";
        binding.Refresh();
        Assert.That(binding.Star.activeSelf, Is.True);
        yield return null;
    }
    [Test]
    public void EveryEditablePrefabFitsSuppliedSafeRectsInPortraitLandscapeAndTablet()
    {
        var sizes = new[] { new Vector2(1280, 720), new Vector2(720, 1280), new Vector2(2340, 1080), new Vector2(1080, 2340), new Vector2(800, 600) };
        foreach (string name in new[] { "MainMenu", "WorldMenu", "StoryPanel", "GameplayHud", "CompletionPanel", "DeathPanel" })
        {
            var view = View(name);
            var canvas = view.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var root = (RectTransform)view.transform;
            foreach (Vector2 size in sizes)
            {
                root.sizeDelta = size;
                var safe = new Rect(30, 24, size.x - 60, size.y - 48);
                view.GetComponentInChildren<SafeAreaLayout>().Apply(safe, size);
                Canvas.ForceUpdateCanvases();
                foreach (var button in view.GetComponentsInChildren<Button>(true))
                {
                    var corners = new Vector3[4];
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector3 local = root.InverseTransformPoint(corner) + new Vector3(size.x / 2, size.y / 2);
                        Assert.That(local.x, Is.InRange(safe.xMin - 1, safe.xMax + 1), name + " horizontal clipping");
                        Assert.That(local.y, Is.InRange(safe.yMin - 1, safe.yMax + 1), name + " vertical clipping");
                    }
                    Assert.That(button.transform.localEulerAngles.z, Is.EqualTo(0).Within(0.01));
                }
            }
            UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
    }
}
