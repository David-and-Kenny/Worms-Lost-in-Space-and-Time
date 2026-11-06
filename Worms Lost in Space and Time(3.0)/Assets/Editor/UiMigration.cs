using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Worms.UI;

/// <summary>Explicit, idempotent authoring command. Never runs automatically on editor startup.</summary>
public static class UiMigration
{
    private const string Folder = "Assets/Resources/UI/";

    [MenuItem("Worms/Migration/Create Editable Canvas UI")]
    public static void Apply()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            ApplyToAssets();
        }
        finally
        {
            if (!Application.isBatchMode && setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static void ApplyToAssets()
    {
        Directory.CreateDirectory(Folder);
        CreatePrefab("MainMenu", UiScreen.MainMenu);
        CreatePrefab("WorldMenu", UiScreen.WorldMenu);
        CreatePrefab("StoryPanel", UiScreen.Story);
        CreatePrefab("GameplayHud", UiScreen.Hud);
        CreatePrefab("CompletionPanel", UiScreen.Completion);
        CreatePrefab("DeathPanel", UiScreen.Death);
        AssetDatabase.SaveAssets();
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled)) MigrateScene(entry.path);
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == path)) MigrateStoryFixture(path);
        }
        AssetDatabase.SaveAssets();
    }

    private static void CreatePrefab(string name, UiScreen screen)
    {
        string path = Folder + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameUiView));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = screen == UiScreen.Hud ? 10 : 20;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var safe = Rect("Safe Area", root.transform, new Vector2(0, 0), new Vector2(1, 1));
        safe.gameObject.AddComponent<SafeAreaLayout>();
        if (screen != UiScreen.Hud && screen != UiScreen.WorldMenu)
        {
            var background = safe.gameObject.AddComponent<Image>();
            background.color = new Color(0.025f, 0.055f, 0.12f, 0.94f);
            background.raycastTarget = false;
        }
        var view = root.GetComponent<GameUiView>();
        view.ScreenKind = screen;
        view.Title = Label(safe, "Title", screen == UiScreen.Death ? "Try again" : screen == UiScreen.MainMenu ? "Worms\nLost in Space and Time" : screen == UiScreen.WorldMenu ? "Choose a level" : "", 0.84f, 0.97f, 38);
        if (screen == UiScreen.Hud)
        {
            view.Body = Label(safe, "Oxygen Label", "Oxygen 100%", 0.92f, 0.98f, 28);
            var meter = Rect("Oxygen Meter", safe, new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.91f));
            var image = meter.gameObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.85f, 0.7f);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.raycastTarget = false;
            view.OxygenFill = image;
        }
        else if (screen == UiScreen.MainMenu)
        {
            view.Primary = Button(safe, "Continue", 0.65f);
            view.WorldOne = Button(safe, "World 1", 0.51f);
            view.WorldTwo = Button(safe, "World 2", 0.37f);
            view.Quit = Button(safe, "Quit", 0.23f);
        }
        else if (screen == UiScreen.WorldMenu)
        {
            view.Menu = Button(safe, "Main menu", 0.015f);
        }
        else if (screen == UiScreen.Story)
        {
            view.Body = Label(safe, "Story Text", "", 0.2f, 0.8f, 28);
            Label(safe, "Story Hint", "Tap to reveal • Release and tap again to continue", 0.05f, 0.15f, 22);
        }
        else
        {
            view.Body = Label(safe, "Result", screen == UiScreen.Death ? "Tap anywhere outside the controls to retry" : "", 0.70f, 0.83f, 28);
            view.Primary = Button(safe, screen == UiScreen.Death ? "Retry" : "Continue", 0.57f);
            view.Back = Button(safe, "Back to world", 0.45f);
            view.Menu = Button(safe, "Main menu", 0.33f);
            view.Quit = Button(safe, "Quit", 0.21f);
        }
        if (screen != UiScreen.Story && screen != UiScreen.Hud)
        {
            view.SaveStatus = Label(safe, "Save Status", "", screen == UiScreen.WorldMenu ? 0.12f : 0.10f, screen == UiScreen.WorldMenu ? 0.20f : 0.20f, 18);
            view.RetrySave = Button(safe, "Retry saving", screen == UiScreen.WorldMenu ? 0.21f : 0.005f);
            view.RetrySave.gameObject.SetActive(false);
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }
    private static Text Label(Transform parent, string name, string text, float bottom, float top, int fontSize)
    {
        var rect = Rect(name, parent, new Vector2(0.06f, bottom), new Vector2(0.94f, top));
        var label = rect.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = text;
        label.raycastTarget = false;
        return label;
    }
    private static Button Button(Transform parent, string text, float bottom)
    {
        var rect = Rect(text, parent, new Vector2(0.12f, bottom), new Vector2(0.88f, bottom + 0.09f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.26f, 0.4f, 0.98f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Label(rect, "Label", text, 0, 1, 30);
        return button;
    }
    private static GameUiView InstantiateView(string name)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + name + ".prefab"));
        return root.GetComponent<GameUiView>();
    }
    private static void EnsureEventSystem()
    {
        var systems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (systems.Length == 0) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        for (int i = 1; i < systems.Length; i++) Object.DestroyImmediate(systems[i].gameObject);
    }
    private static void MigrateScene(string path)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        if (scene.GetRootGameObjects().Any(o => o.name == "UI Migration v1"))
        {
            if ((scene.name == "World1" || scene.name == "World2") && CleanupLegacyWorldCanvas())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return;
        }
        // Editor migration reads the persisted catalog without initializing file storage.
        var catalog = AssetDatabase.LoadAssetAtPath<Worms.Configuration.LevelCatalog>("Assets/Resources/Configuration/LevelCatalog.asset");
        bool gameplay = catalog != null && catalog.FindByScene(scene.name) != null;
        if (gameplay)
        {
            var controller = new GameObject("UI Migration v1").AddComponent<GameUiPresenter>();
            controller.Hud = InstantiateView("GameplayHud");
            controller.Completion = InstantiateView("CompletionPanel");
            controller.Death = InstantiateView("DeathPanel");
            controller.Completion.gameObject.SetActive(false);
            controller.Death.gameObject.SetActive(false);
        }
        else if (scene.name == "StorySelect")
        {
            new GameObject("UI Migration v1");
            InstantiateView("MainMenu");
        }
        else if (scene.name == "World1" || scene.name == "World2")
        {
            new GameObject("UI Migration v1");
            MigrateWorld(scene.name == "World1" ? "world-1" : "world-2", catalog);
            InstantiateView("WorldMenu");
        }
        else return;
        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
    private static void MigrateWorld(string world, Worms.Configuration.LevelCatalog catalog)
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.transform.localRotation = Quaternion.identity;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight = 0.5f;
        var safe = Rect("Responsive levels", canvas.transform, Vector2.zero, Vector2.one);
        safe.gameObject.AddComponent<SafeAreaLayout>();
        var transforms = canvas.GetComponentsInChildren<Transform>(true);
        int position = 0;
        foreach (var level in catalog.Levels.Where(l => l.WorldId == world))
        {
            var button = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == level.SceneName);
            if (button == null) continue;
            var rect = button.GetComponent<RectTransform>();
            rect.SetParent(safe, false);
            float left = position % 2 == 0 ? 0.08f : 0.54f;
            float bottom = 0.74f - position / 2 * 0.10f;
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(left + 0.38f, bottom + 0.09f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            foreach (var child in rect.GetComponentsInChildren<RectTransform>(true)) child.localRotation = Quaternion.identity;
            foreach (var text in rect.GetComponentsInChildren<Text>(true))
            {
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
                text.rectTransform.localScale = Vector3.one;
                text.alignment = TextAnchor.MiddleCenter;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 16;
                text.resizeTextMaxSize = 36;
            }
            var binding = button.GetComponent<LevelButtonBinding>() ?? button.gameObject.AddComponent<LevelButtonBinding>();
            binding.LevelId = level.Id;
            binding.Button = button;
            var locked = transforms.FirstOrDefault(t => t.name == "LockedLevel" + level.LegacyIndex);
            var star = transforms.FirstOrDefault(t => t.name == "Conch" + (position + 1));
            binding.Lock = locked == null ? null : locked.gameObject;
            binding.Star = star == null ? null : star.gameObject;
            if (locked != null) PlaceBadge(locked, rect, new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f));
            if (star != null) PlaceBadge(star, rect, new Vector2(0.02f, 0.1f), new Vector2(0.23f, 0.9f));
            position++;
        }
        foreach (var image in canvas.GetComponentsInChildren<Graphic>(true))
            if (image.GetComponent<Button>() == null) image.raycastTarget = false;
        var usedBadges = canvas.GetComponentsInChildren<LevelButtonBinding>(true)
            .SelectMany(b => new[] { b.Lock, b.Star }).Where(b => b != null).ToArray();
        foreach (var item in transforms)
            if ((item.name.StartsWith("LockedLevel") || item.name.StartsWith("Conch")) && !usedBadges.Contains(item.gameObject))
                item.gameObject.SetActive(false);
        CleanupLegacyWorldCanvas();
    }
    private static bool CleanupLegacyWorldCanvas()
    {
        bool changed = false;
        var catalog = Resources.Load<Worms.Configuration.LevelCatalog>("Configuration/LevelCatalog");
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            var bindings = canvas.GetComponentsInChildren<LevelButtonBinding>(true);
            if (bindings.Length == 0) continue;
            foreach (var binding in bindings)
            {
                var level = catalog.GetLevel(binding.LevelId);
                if (binding.Lock == null && level.WorldId == "world-2")
                {
                    // The last four original World 2 badges retained their World 1 names.
                    var badge = canvas.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(item => item.name == "LockedLevel" + (level.LegacyIndex - 10));
                    if (badge != null)
                    {
                        binding.Lock = badge.gameObject;
                        PlaceBadge(badge, binding.transform, new Vector2(0.72f, 0.1f), new Vector2(0.98f, 0.9f));
                        badge.gameObject.SetActive(true);
                        changed = true;
                    }
                }
                foreach (var label in binding.GetComponentsInChildren<Text>(true))
                {
                    if (label.text != level.DisplayName) { label.text = level.DisplayName; changed = true; }
                    var rect = label.rectTransform;
                    var minimum = new Vector2(0.23f, 0f);
                    var maximum = new Vector2(0.72f, 1f);
                    if (rect.anchorMin == minimum && rect.anchorMax == maximum) continue;
                    rect.anchorMin = minimum;
                    rect.anchorMax = maximum;
                    changed = true;
                }
                // Legacy badges contain Buttons; only the level artwork should receive input.
                foreach (var graphic in binding.GetComponentsInChildren<Graphic>(true))
                {
                    bool receivesInput = graphic == binding.Button.targetGraphic;
                    if (graphic.raycastTarget == receivesInput) continue;
                    graphic.raycastTarget = receivesInput;
                    changed = true;
                }
                foreach (var badge in binding.GetComponentsInChildren<Button>(true))
                {
                    if (badge == binding.Button || badge.navigation.mode == Navigation.Mode.None) continue;
                    var navigation = badge.navigation;
                    navigation.mode = Navigation.Mode.None;
                    badge.navigation = navigation;
                    changed = true;
                }
            }
            foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponentInParent<LevelButtonBinding>() != null || !button.gameObject.activeSelf) continue;
                button.gameObject.SetActive(false);
                changed = true;
            }
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.GetComponentInParent<LevelButtonBinding>() != null) continue;
                bool obsolete = graphic is Text || graphic.GetComponentInParent<Button>() != null || graphic.name == "ImageSelectLevel";
                if (!obsolete || !graphic.gameObject.activeSelf) continue;
                graphic.gameObject.SetActive(false);
                changed = true;
            }
        }
        return changed;
    }
    private static void PlaceBadge(Transform badge, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = badge as RectTransform;
        if (rect == null) return;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }
    private static void MigrateStoryFixture(string path)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var story = Object.FindFirstObjectByType<StoryScript>();
        if (story == null || story.view != null) return;
        story.view = InstantiateView("StoryPanel");
        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
