using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Worms.UI;

public static class Level1ArtTools
{
    public const string ScenePath = "Assets/Scenes/World1/Level1.unity";
    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));

    [Serializable] public sealed class Inventory
    {
        public List<SpriteEntry> Sprites = new List<SpriteEntry>();
        public List<string> Dependencies = new List<string>();
    }

    [Serializable] public sealed class SpriteEntry
    {
        public string Hierarchy, Kind, Sprite, Texture;
        public float Width, Height, PixelsPerUnit, WorldWidth, WorldHeight;
        public bool Active;
    }

    public static void ExportInventory()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var report = new Inventory();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                Add(report, renderer.transform, renderer.sprite, "SpriteRenderer", renderer.enabled);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                Add(report, image.transform, image.sprite, "Image", image.enabled);
        }
        report.Dependencies.AddRange(AssetDatabase.GetDependencies(ScenePath, true)
            .Where(path => path.EndsWith(".png") || path.EndsWith(".jpg") ||
                path.EndsWith(".anim") || path.EndsWith(".controller") || path.EndsWith(".prefab")));
        string output = Path.Combine(RepositoryRoot, "TestResults/Art/level-01-inventory.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(output, JsonUtility.ToJson(report, true));
        Debug.Log("Level 1 art inventory: " + output);
    }

    [MenuItem("Worms/Art/Apply Level 1 first pass")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        const string folder = "Assets/Artwork/Level1/";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var replacements = new Dictionary<string, Sprite>();
        string[] originals = { "cog2", "cog3", "cog5", "pipe-large3" };
        string[] concepts = { "cog-spokes", "cog-double-ring", "cog-cutouts", "hazard-pipe" };
        for (int i = 0; i < originals.Length; i++)
        {
            string source = "Assets/Artwork/sprite-sheets/" + originals[i] + ".png";
            replacements[source] = ImportMatchedSprite(source,
                "art/concepts/level-01/" + concepts[i] + ".png", folder + concepts[i] + ".png", 3);
        }
        replacements["Assets/Artwork/bg_red.jpg"] = ImportMatchedSprite("Assets/Artwork/bg_red.jpg",
            "art/concepts/round-02/level-background-v04.png", folder + "background.png", 2);
        Sprite plate = ImportPlate(folder + "ui-plate.png");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int changed = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.sprite) continue;
                string source = AssetDatabase.GetAssetPath(renderer.sprite);
                if (!replacements.TryGetValue(source, out var replacement)) continue;
                if ((renderer.sprite.bounds.size - replacement.bounds.size).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Art import changed world bounds: " + source);
                renderer.sprite = replacement;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                changed++;
            }
            foreach (var view in root.GetComponentsInChildren<GameUiView>(true)) StyleView(view, plate);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Level 1 art applied; replaced renderers: " + changed + ". Physics and transforms unchanged.");
        ExportInventory();
    }

    private static Sprite ImportMatchedSprite(string originalPath, string conceptPath, string output, int scale)
    {
        var original = AssetDatabase.LoadAssetAtPath<Sprite>(originalPath);
        if (!original) throw new InvalidOperationException("Missing source sprite: " + originalPath);
        var oldPixels = ReadPng(Path.GetFullPath(Path.Combine(Application.dataPath, "..", originalPath)));
        var concept = ReadPng(Path.Combine(RepositoryRoot, conceptPath));
        Texture2D baked = null;
        try
        {
            // Match the old alpha envelope within its canvas, not just the new image's padding.
            RectInt envelope = AlphaBounds(oldPixels);
            var targetEnvelope = new RectInt(
                Mathf.RoundToInt(envelope.x * original.rect.width / oldPixels.width * scale),
                Mathf.RoundToInt(envelope.y * original.rect.height / oldPixels.height * scale),
                Mathf.RoundToInt(envelope.width * original.rect.width / oldPixels.width * scale),
                Mathf.RoundToInt(envelope.height * original.rect.height / oldPixels.height * scale));
            baked = Resample(concept, AlphaBounds(concept), (int)original.rect.width * scale,
                (int)original.rect.height * scale, targetEnvelope);
            File.WriteAllBytes(output, baked.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(oldPixels);
            UnityEngine.Object.DestroyImmediate(concept);
            if (baked) UnityEngine.Object.DestroyImmediate(baked);
        }
        ConfigureSprite(output, original.pixelsPerUnit * scale,
            new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height), Vector4.zero);
        return AssetDatabase.LoadAssetAtPath<Sprite>(output);
    }

    private static Sprite ImportPlate(string output)
    {
        var source = ReadPng(Path.Combine(RepositoryRoot, "art/concepts/level-01/ui-plate.png"));
        Texture2D baked = null;
        try
        {
            var bounds = AlphaBounds(source);
            baked = Resample(source, bounds, 1024, 288, new RectInt(0, 0, 1024, 288));
            File.WriteAllBytes(output, baked.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            if (baked) UnityEngine.Object.DestroyImmediate(baked);
        }
        ConfigureSprite(output, 100, new Vector2(.5f, .5f), new Vector4(110, 60, 110, 60));
        return AssetDatabase.LoadAssetAtPath<Sprite>(output);
    }

    private static Texture2D ReadPng(string path)
    {
        var result = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!result.LoadImage(File.ReadAllBytes(path))) throw new IOException("Cannot read image: " + path);
        return result;
    }

    private static RectInt AlphaBounds(Texture2D image)
    {
        int left = image.width, bottom = image.height, right = -1, top = -1;
        var pixels = image.GetPixels32();
        for (int y = 0; y < image.height; y++)
        for (int x = 0; x < image.width; x++)
        {
            if (pixels[y * image.width + x].a < 16) continue;
            left = Math.Min(left, x); bottom = Math.Min(bottom, y);
            right = Math.Max(right, x); top = Math.Max(top, y);
        }
        if (right < left) throw new InvalidOperationException("Empty reference image.");
        return new RectInt(left, bottom, right - left + 1, top - bottom + 1);
    }

    private static Texture2D Resample(Texture2D image, RectInt source, int width, int height, RectInt target)
    {
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (int y = 0; y < target.height; y++)
        for (int x = 0; x < target.width; x++)
        {
            float u = (source.x + (x + .5f) * source.width / target.width) / image.width;
            float v = (source.y + (y + .5f) * source.height / target.height) / image.height;
            pixels[(target.y + y) * width + target.x + x] = image.GetPixelBilinear(u, v);
        }
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    private static void ConfigureSprite(string path, float pixelsPerUnit, Vector2 pivot, Vector4 border)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spriteBorder = border;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void StyleView(GameUiView view, Sprite plate)
    {
        foreach (var image in view.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if (image == view.OxygenFill) continue;
            image.sprite = plate;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3f;
            image.color = Color.white;
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }
        foreach (var label in view.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            label.color = new Color(1f, .95f, .83f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        }
        if (view.OxygenFill)
        {
            view.OxygenFill.color = new Color(.22f, .75f, .72f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view.OxygenFill);
            var fill = view.OxygenFill.rectTransform;
            var existing = fill.parent.Find("Oxygen Frame");
            RectTransform frameRect;
            if (!existing)
            {
                var frame = new GameObject("Oxygen Frame", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                frameRect = frame.GetComponent<RectTransform>();
                frameRect.SetParent(fill.parent, false);
                frameRect.SetSiblingIndex(fill.GetSiblingIndex());
            }
            else frameRect = (RectTransform)existing;
            frameRect.anchorMin = fill.anchorMin; frameRect.anchorMax = fill.anchorMax;
            frameRect.anchoredPosition = fill.anchoredPosition;
            frameRect.sizeDelta = fill.sizeDelta + new Vector2(52, 28);
            var frameImage = frameRect.GetComponent<UnityEngine.UI.Image>();
            frameImage.sprite = plate;
            frameImage.type = UnityEngine.UI.Image.Type.Sliced;
            frameImage.pixelsPerUnitMultiplier = 6f;
            frameImage.raycastTarget = false;
        }
    }

    private static void Add(Inventory report, Transform node, Sprite sprite, string kind, bool enabled)
    {
        if (!sprite) return;
        report.Sprites.Add(new SpriteEntry
        {
            Hierarchy = Hierarchy(node), Kind = kind, Sprite = sprite.name,
            Texture = AssetDatabase.GetAssetPath(sprite),
            Width = sprite.rect.width, Height = sprite.rect.height,
            PixelsPerUnit = sprite.pixelsPerUnit,
            WorldWidth = sprite.bounds.size.x * Mathf.Abs(node.lossyScale.x),
            WorldHeight = sprite.bounds.size.y * Mathf.Abs(node.lossyScale.y),
            Active = enabled && node.gameObject.activeInHierarchy
        });
    }

    private static string Hierarchy(Transform node)
    {
        return node.parent ? Hierarchy(node.parent) + "/" + node.name : node.name;
    }
}
