using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worms.UI;

public static class MenuArtTools
{
    private const string Art = "Assets/Artwork/Menus/";
    private static readonly Color Cream = new Color(1f, .94f, .79f);
    private static readonly Color Gold = new Color(1f, .77f, .28f);
    private static Font displayFont, bodyFont;
    private static Sprite background, plate, levelCog, levelPipe, linkPipe, padlock, star, character;
    public static readonly string[] Scenes =
    {
        "Assets/Scenes/World1/Splash.unity", "Assets/Scenes/World1/StorySelect.unity",
        "Assets/Scenes/World1/World1.unity", "Assets/Scenes/World2/World2.unity"
    };

    [Serializable] public sealed class Inventory
    {
        public List<Entry> Entries = new List<Entry>();
    }
    [Serializable] public sealed class Entry
    {
        public string Scene, Hierarchy, Kind, Asset, Content;
        public bool Active;
    }

    public static void ExportInventory()
    {
        var result = new Inventory();
        foreach (string path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    result.Entries.Add(new Entry { Scene = scene.name, Hierarchy = Hierarchy(renderer.transform),
                        Kind = "Sprite", Asset = renderer.sprite ? AssetDatabase.GetAssetPath(renderer.sprite) : "",
                        Active = renderer.enabled && renderer.gameObject.activeInHierarchy });
                foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    result.Entries.Add(new Entry { Scene = scene.name, Hierarchy = Hierarchy(image.transform),
                        Kind = "Image", Asset = image.sprite ? AssetDatabase.GetAssetPath(image.sprite) : "",
                        Active = image.enabled && image.gameObject.activeInHierarchy });
                foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                    result.Entries.Add(new Entry { Scene = scene.name, Hierarchy = Hierarchy(text.transform),
                        Kind = "Text", Content = text.text, Asset = AssetDatabase.GetAssetPath(text.font),
                        Active = text.enabled && text.gameObject.activeInHierarchy });
            }
        }
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults/Art/Menus/inventory.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output, JsonUtility.ToJson(result, true));
    }

    [MenuItem("Worms/Art/Apply start and menu theme")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        background = ImportArt("observatory-background", false, 0);
        plate = ImportArt("menu-button", true, 1024);
        levelCog = ImportArt("level-cog", true, 512);
        levelPipe = ImportArt("level-pipe-button", true, 768);
        linkPipe = ImportArt("level-link-pipe", true, 512);
        padlock = ImportArt("lock-badge", true, 128);
        star = ImportArt("star-badge", true, 128);
        displayFont = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/Bungee-Regular.ttf");
        bodyFont = AssetDatabase.LoadAssetAtPath<Font>(Art + "Fonts/Nunito.ttf");
        if (!displayFont || !bodyFont) throw new InvalidOperationException("Menu fonts are missing.");
        character = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Artwork/sprite-sheets/astronaut.png");
        if (!character) throw new InvalidOperationException("Retain the original splash character sprite.");

        PreserveOriginalArt();
        StylePrefab("MainMenu");
        StylePrefab("WorldMenu");
        foreach (string path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var bindings = CaptureBindings(scene.GetRootGameObjects());
            foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = false;
                Record(renderer);
            }
            var backdropRoot = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Menu Art Backdrop");
            if (!backdropRoot)
            {
                backdropRoot = new GameObject("Menu Art Backdrop", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
                var canvas = backdropRoot.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = -100;
                ConfigureScaler(backdropRoot.GetComponent<UnityEngine.UI.CanvasScaler>());
            }
            var backdrop = Image(backdropRoot.transform, "Observatory", background, Vector2.zero, Vector2.one);
            var fitter = backdrop.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            if (!fitter) fitter = backdrop.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.rect.width / background.rect.height;

            if (scene.name == "Splash") StyleSplash(scene);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var view in root.GetComponentsInChildren<GameUiView>(true)) StyleMenuView(view, scene.name);
                StyleLevelButtons(root);
            }
            AssertBindings(bindings);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        ExportInventory();
    }

    private static Sprite ImportArt(string name, bool crop, int maximumWidth)
    {
        string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../../art/concepts/menus/" + name + ".png"));
        var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Texture2D result = null;
        try
        {
            if (!image.LoadImage(File.ReadAllBytes(source))) throw new IOException(source);
            int left = 0, bottom = 0, right = image.width - 1, top = image.height - 1;
            if (crop)
            {
                left = image.width; bottom = image.height; right = top = -1;
                var pixels = image.GetPixels32();
                for (int y = 0; y < image.height; y++)
                for (int x = 0; x < image.width; x++)
                {
                    if (pixels[y * image.width + x].a < 16) continue;
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    bottom = Math.Min(bottom, y); top = Math.Max(top, y);
                }
                if (right < left) throw new InvalidOperationException("Empty menu artwork: " + name);
            }
            int sourceWidth = right - left + 1, sourceHeight = top - bottom + 1;
            int width = maximumWidth > 0 ? Math.Min(maximumWidth, sourceWidth) : sourceWidth;
            int height = Mathf.RoundToInt((float)sourceHeight / sourceWidth * width);
            result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var colors = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                colors[y * width + x] = image.GetPixelBilinear((left + (x + .5f) * sourceWidth / width) / image.width,
                    (bottom + (y + .5f) * sourceHeight / height) / image.height);
            result.SetPixels(colors); result.Apply();
            File.WriteAllBytes(Art + name + ".png", result.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(image);
            if (result) UnityEngine.Object.DestroyImmediate(result);
        }
        string output = Art + name + ".png";
        AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(output);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = crop;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (name == "menu-button") importer.spriteBorder = new Vector4(110, 50, 110, 50);
        if (name == "level-link-pipe") importer.spriteBorder = new Vector4(80, 0, 80, 0);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(output);
    }

    private static void PreserveOriginalArt()
    {
        foreach (string path in new[] { "Assets/Artwork/Begin.jpg", "Assets/Artwork/shiny-space-art.jpg",
            "Assets/Artwork/external_1500.jpg", "Assets/Artwork/gameLock.png", "Assets/Artwork/magicalconch.png",
            "Assets/Artwork/sprite-sheets/cog1.png", "Assets/Artwork/sprite-sheets/cog4.png", "Assets/Artwork/sprite-sheets/cog7.png" })
        {
            string legacy = Path.ChangeExtension(path, ".legacy" + Path.GetExtension(path));
            if (!AssetDatabase.LoadMainAssetAtPath(path))
            {
                if (!AssetDatabase.LoadMainAssetAtPath(legacy)) throw new IOException("Missing source: " + path);
                continue;
            }
            if (AssetDatabase.LoadMainAssetAtPath(legacy)) throw new IOException("Legacy art already exists: " + legacy);
            string guid = AssetDatabase.AssetPathToGUID(path);
            byte[] bytes = File.ReadAllBytes(path);
            string error = AssetDatabase.MoveAsset(path, legacy);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            if (AssetDatabase.AssetPathToGUID(legacy) != guid || !bytes.SequenceEqual(File.ReadAllBytes(legacy)))
                throw new InvalidOperationException("Legacy preservation failed: " + path);
        }
    }

    private static void StylePrefab(string name)
    {
        string path = "Assets/Resources/UI/" + name + ".prefab";
        string legacy = "Assets/Resources/UI/" + name + ".legacy.prefab";
        // Layout backups are copies: the active prefab keeps its GUID and all scene bindings.
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(legacy) && !AssetDatabase.CopyAsset(path, legacy))
            throw new IOException("Cannot preserve original menu prefab: " + path);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var bindings = CaptureBindings(new[] { root });
            StyleMenuView(root.GetComponent<GameUiView>(), name == "MainMenu" ? "StorySelect" : "World1");
            AssertBindings(bindings);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void StyleSplash(UnityEngine.SceneManagement.Scene scene)
    {
        var root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Splash Art UI");
        if (!root)
        {
            root = new GameObject("Splash Art UI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 5;
            ConfigureScaler(root.GetComponent<UnityEngine.UI.CanvasScaler>());
        }
        var safe = Rect(root.transform, "Safe Area", Vector2.zero, Vector2.one);
        if (!safe.GetComponent<SafeAreaLayout>()) safe.gameObject.AddComponent<SafeAreaLayout>();
        Heading(safe, "Title", "WORMS", .80f, .94f, 100);
        Heading(safe, "Subtitle", "LOST IN SPACE AND TIME", .72f, .80f, 26, false);
        Hero(safe, .27f, .68f);
        Image(safe, "Begin Plate", plate, new Vector2(.17f, .13f), new Vector2(.83f, .23f), true);
        Heading(safe, "Begin Label", "TAP TO BEGIN", .13f, .23f, 32);
        Heading(safe, "Begin Hint", "Tap anywhere to start", .075f, .12f, 20, false);
    }

    private static void StyleMenuView(GameUiView view, string scene)
    {
        if (!view || (view.ScreenKind != UiScreen.MainMenu && view.ScreenKind != UiScreen.WorldMenu)) return;
        var safe = view.GetComponentInChildren<SafeAreaLayout>(true).transform;
        var oldBackground = safe.GetComponent<UnityEngine.UI.Image>();
        if (oldBackground) { oldBackground.enabled = false; Record(oldBackground); }
        foreach (var text in view.GetComponentsInChildren<UnityEngine.UI.Text>(true)) StyleText(text, false, 26);
        foreach (var button in view.GetComponentsInChildren<UnityEngine.UI.Button>(true)) StyleButton(button);
        if (view.Title)
        {
            SetRect(view.Title.rectTransform, new Vector2(.08f, .82f), new Vector2(.92f, .95f));
            view.Title.text = view.ScreenKind == UiScreen.MainMenu ? "WORMS" : scene == "World2" ? "WORLD 2" : "WORLD 1";
            StyleText(view.Title, true, view.ScreenKind == UiScreen.MainMenu ? 92 : 64);
            Record(view.Title); Record(view.Title.rectTransform);
        }
        Heading(safe, "Menu Subtitle", view.ScreenKind == UiScreen.MainMenu ? "LOST IN SPACE AND TIME" : "SELECT A LEVEL", .765f, .82f, 24, false);
        if (view.ScreenKind == UiScreen.MainMenu)
        {
            Hero(safe, .465f, .75f);
            PlaceButton(view.Primary, .12f, .35f, .88f, .445f);
            PlaceButton(view.WorldOne, .12f, .215f, .485f, .315f);
            PlaceButton(view.WorldTwo, .515f, .215f, .88f, .315f);
            PlaceButton(view.Quit, .12f, .105f, .485f, .17f);
        }
        else PlaceButton(view.Menu, .08f, .065f, .48f, .135f);
        if (view.SaveStatus)
        {
            SetRect(view.SaveStatus.rectTransform, new Vector2(.10f, .012f), new Vector2(.90f, .06f));
            StyleText(view.SaveStatus, false, 18);
            Record(view.SaveStatus.rectTransform);
        }
        if (view.RetrySave) PlaceButton(view.RetrySave, .30f, .14f, .70f, .195f);
        StyleSettings(view.GetComponent<MonetizationSettingsView>(), view.ScreenKind == UiScreen.MainMenu);
    }

    private static void StyleSettings(MonetizationSettingsView settings, bool main)
    {
        if (!settings) return;
        PlaceButton(settings.Open, main ? .515f : .52f, main ? .105f : .065f, main ? .88f : .92f, main ? .17f : .135f);
        var panel = settings.Panel.GetComponent<UnityEngine.UI.Image>();
        panel.enabled = true; panel.sprite = null; panel.type = UnityEngine.UI.Image.Type.Simple;
        panel.color = new Color(.025f, .07f, .10f); Record(panel);
        var backdrop = Image(settings.Panel.transform, "Settings Backdrop", background, Vector2.zero, Vector2.one);
        backdrop.transform.SetAsFirstSibling();
        var fitter = backdrop.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        if (!fitter) fitter = backdrop.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = background.rect.width / background.rect.height;
        foreach (var text in settings.Panel.GetComponentsInChildren<UnityEngine.UI.Text>(true)) StyleText(text, false, 25);
        var heading = settings.Panel.GetComponentsInChildren<UnityEngine.UI.Text>(true)
            .FirstOrDefault(text => !text.GetComponentInParent<UnityEngine.UI.Button>() && text != settings.Status);
        if (heading) StyleText(heading, true, 46);
        var controls = Rect(settings.Panel.transform, "Settings Controls", new Vector2(.10f, .34f), new Vector2(.90f, .79f));
        var layout = controls.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (!layout) layout = controls.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 22;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        foreach (var button in new[] { settings.Buy, settings.Restore, settings.Privacy, settings.Policy })
        {
            if (button.transform.parent != controls) button.transform.SetParent(controls, false);
            var element = button.GetComponent<UnityEngine.UI.LayoutElement>();
            if (!element) element = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = 48; element.preferredHeight = 100; element.flexibleHeight = 0;
            StyleButton(button);
        }
        PlaceButton(settings.Close, .18f, .065f, .82f, .145f);
        settings.Panel.SetActive(false);
        settings.Panel.transform.SetAsLastSibling();
        Record(settings.Panel.transform);
        Record(settings.Panel);
    }

    private static void StyleLevelButtons(GameObject root)
    {
        var levels = root.GetComponentsInChildren<LevelButtonBinding>(true).OrderBy(binding => binding.LevelId).ToArray();
        if (levels.Length == 0) return;
        Transform safe = levels[0].GetComponentInParent<SafeAreaLayout>().transform;
        var gridRect = Rect(safe, "Level Grid", new Vector2(.10f, .20f), new Vector2(.90f, .775f));
        var linksRect = Rect(safe, "Level Pipe Connections", gridRect.anchorMin, gridRect.anchorMax);
        linksRect.SetAsFirstSibling();
        var grid = gridRect.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (!grid) grid = gridRect.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        grid.spacing = new Vector2(48, 32);
        var responsive = gridRect.GetComponent<MenuLevelGrid>();
        if (!responsive) responsive = gridRect.gameObject.AddComponent<MenuLevelGrid>();
        var connections = new RectTransform[13];
        for (int i = 0; i < connections.Length; i++)
        {
            var pipe = Image(linksRect, "Pipe Link " + i, linkPipe, Vector2.zero, Vector2.one, true);
            pipe.pixelsPerUnitMultiplier = 6;
            connections[i] = pipe.rectTransform;
        }
        responsive.SetPipeLinks(connections);
        for (int i = 0; i < levels.Length; i++)
        {
            var binding = levels[i];
            if (binding.transform.parent != gridRect) binding.transform.SetParent(gridRect, false);
            Record(binding.transform);
            StyleButton(binding.Button);
            var oldFace = binding.GetComponent<UnityEngine.UI.Image>();
            oldFace.enabled = false; oldFace.raycastTarget = false; Record(oldFace);
            var oldCog = binding.transform.Find("Cog Face");
            if (oldCog) oldCog.name = "Pipe Face";
            var face = Image(binding.transform, "Pipe Face", levelPipe, Vector2.zero, Vector2.one);
            face.raycastTarget = true;
            // A solid inset keeps decorative connections behind the translucent source artwork.
            var backing = Rect(binding.transform, "Pipe Backing", Vector2.zero, Vector2.one);
            backing.SetAsFirstSibling();
            var backingFitter = backing.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            if (!backingFitter) backingFitter = backing.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            backingFitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            backingFitter.aspectRatio = levelPipe.rect.width / levelPipe.rect.height;
            var inset = Image(backing, "Opaque Inset", null, new Vector2(.07f, .18f), new Vector2(.93f, .82f));
            inset.color = new Color(.015f, .15f, .19f, 1);
            var fitter = face.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            if (!fitter) fitter = face.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = levelPipe.rect.width / levelPipe.rect.height;
            binding.Button.targetGraphic = face; Record(binding.Button);
            foreach (var text in binding.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if (text.transform.parent != face.transform) text.transform.SetParent(face.transform, false);
                SetRect(text.rectTransform, new Vector2(.16f, .25f), new Vector2(.84f, .75f));
                StyleText(text, true, 28); Record(text.rectTransform);
            }
            if (binding.Lock && binding.Lock.transform.parent != face.transform) binding.Lock.transform.SetParent(face.transform, false);
            if (binding.Star && binding.Star.transform.parent != face.transform) binding.Star.transform.SetParent(face.transform, false);
            Badge(binding.Lock, padlock, new Vector2(.86f, -.15f), new Vector2(1.02f, .40f));
            Badge(binding.Star, star, new Vector2(-.02f, -.15f), new Vector2(.14f, .40f));
        }
        Canvas.ForceUpdateCanvases();
        responsive.Refresh();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
    }

    private static void Badge(GameObject item, Sprite sprite, Vector2 min, Vector2 max)
    {
        if (!item) return;
        SetRect((RectTransform)item.transform, min, max);
        var image = item.GetComponent<UnityEngine.UI.Image>();
        if (image) { image.sprite = sprite; image.preserveAspect = true; image.color = Color.white; image.raycastTarget = false; Record(image); }
        var button = item.GetComponent<UnityEngine.UI.Button>();
        if (button) { button.enabled = false; Record(button); }
        Record(item.transform);
    }

    private static void Hero(Transform safe, float bottom, float top)
    {
        var halo = Image(safe, "Character Gear", levelCog, new Vector2(.22f, bottom), new Vector2(.78f, top));
        halo.preserveAspect = true;
        var image = Image(safe, "Original Character", character, new Vector2(.30f, bottom + .015f), new Vector2(.70f, top - .015f));
        image.preserveAspect = true;
    }

    private static void PlaceButton(UnityEngine.UI.Button button, float left, float bottom, float right, float top)
    {
        if (!button) return;
        SetRect((RectTransform)button.transform, new Vector2(left, bottom), new Vector2(right, top));
        StyleButton(button); Record(button.transform);
    }

    private static void StyleButton(UnityEngine.UI.Button button)
    {
        if (!button) return;
        if (button.targetGraphic is UnityEngine.UI.Image image)
        {
            image.sprite = plate; image.color = Color.white;
            image.type = UnityEngine.UI.Image.Type.Sliced; image.pixelsPerUnitMultiplier = 5;
            image.raycastTarget = true; Record(image);
        }
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .95f, .76f);
        colors.pressedColor = new Color(.65f, .85f, .87f);
        colors.selectedColor = new Color(1f, .95f, .76f);
        colors.disabledColor = new Color(.34f, .39f, .42f, .90f);
        button.colors = colors; Record(button);
        foreach (var text in button.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            StyleText(text, true, 27);
            SetRect(text.rectTransform, new Vector2(.10f, .12f), new Vector2(.90f, .88f));
            Record(text.rectTransform);
        }
    }

    private static void Heading(Transform parent, string name, string value, float bottom, float top, int size, bool display = true)
    {
        var rect = Rect(parent, name, new Vector2(.08f, bottom), new Vector2(.92f, top));
        var text = rect.GetComponent<UnityEngine.UI.Text>();
        if (!text) text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        text.text = value;
        StyleText(text, display, size);
    }

    private static void StyleText(UnityEngine.UI.Text text, bool display, int size)
    {
        text.font = display ? displayFont : bodyFont;
        text.fontSize = size;
        text.fontStyle = display ? FontStyle.Normal : FontStyle.Bold;
        text.color = display && size >= 46 ? Gold : Cream;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = display && size >= 46 ? 28 : 14;
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
        var outline = text.GetComponent<UnityEngine.UI.Outline>();
        if (!outline) outline = text.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(.015f, .025f, .04f, .9f);
        outline.effectDistance = display && size >= 46 ? new Vector2(3, -3) : new Vector2(1, -1);
        Record(text);
    }

    private static UnityEngine.UI.Image Image(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max, bool sliced = false)
    {
        var rect = Rect(parent, name, min, max);
        var image = rect.GetComponent<UnityEngine.UI.Image>();
        if (!image) image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite; image.color = Color.white; image.raycastTarget = false;
        image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 3;
        return image;
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var existing = parent.Find(name);
        var rect = existing ? existing.GetComponent<RectTransform>() : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        if (!existing) rect.SetParent(parent, false);
        SetRect(rect, min, max);
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity; rect.localScale = Vector3.one;
    }

    private static void ConfigureScaler(UnityEngine.UI.CanvasScaler scaler)
    {
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
    }

    private static Dictionary<UnityEngine.UI.Button, string> CaptureBindings(IEnumerable<GameObject> roots)
    {
        return roots.SelectMany(root => root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            .ToDictionary(button => button, BindingSignature);
    }

    private static string BindingSignature(UnityEngine.UI.Button button)
    {
        var serialized = new SerializedObject(button);
        var calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        string result = "";
        for (int i = 0; i < calls.arraySize; i++)
        {
            var call = calls.GetArrayElementAtIndex(i);
            result += button.onClick.GetPersistentTarget(i)?.GetEntityId() + ":" + button.onClick.GetPersistentMethodName(i)
                + ":" + call.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue + ";";
        }
        return result;
    }

    private static void AssertBindings(Dictionary<UnityEngine.UI.Button, string> before)
    {
        foreach (var entry in before)
            if (!entry.Key || BindingSignature(entry.Key) != entry.Value)
                throw new InvalidOperationException("Menu art must preserve button callbacks.");
    }

    private static void Record(UnityEngine.Object item)
    {
        EditorUtility.SetDirty(item);
        if (PrefabUtility.IsPartOfPrefabInstance(item)) PrefabUtility.RecordPrefabInstancePropertyModifications(item);
    }

    private static string Hierarchy(Transform node) => node.parent ? Hierarchy(node.parent) + "/" + node.name : node.name;
}
