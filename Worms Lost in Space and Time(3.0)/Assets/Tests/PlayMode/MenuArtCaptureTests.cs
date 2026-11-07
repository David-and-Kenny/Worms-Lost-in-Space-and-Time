using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worms;
using Worms.Configuration;
using Worms.Core;
using Worms.Navigation;
using Worms.UI;

public sealed class MenuArtCaptureTests
{
    [UnityTest]
    public IEnumerator ThemedMenusUseReadableAssetsAndRetainControls()
    {
        bool capture = Environment.GetCommandLineArgs().Contains("-worms-menu-capture");
        if (capture) Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType);
        var catalog = Resources.Load<LevelCatalog>("Configuration/LevelCatalog");
        GameServices.ConfigureForTests(catalog, new PreviewStorage());
        SceneNavigator.ConfigureForTests(null);
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults/Art/Menus"));
        Directory.CreateDirectory(output);
        foreach (string sceneName in new[] { "Splash", "StorySelect", "World1", "World2" })
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var images = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None);
            Assert.IsTrue(images.Any(image => image.sprite && image.sprite.name == "observatory-background"),
                sceneName + " needs the new opaque menu background.");
            var texts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                Assert.IsNotNull(text.font, sceneName + ": " + text.name);
                Assert.IsTrue(text.font.name.StartsWith("Bungee") || text.font.name.StartsWith("Nunito"),
                    sceneName + " retains unthemed text: " + text.name + " / " + text.font.name);
                Assert.IsFalse(text.raycastTarget, "Labels must not intercept the button's input.");
            }
            if (sceneName == "Splash" || sceneName == "StorySelect")
                Assert.IsTrue(images.Any(image => image.sprite && image.sprite.name == "astronaut"),
                    "Reuse the original character artwork without a new generation.");
            foreach (var binding in UnityEngine.Object.FindObjectsByType<LevelButtonBinding>(FindObjectsSortMode.None))
            {
                Assert.AreEqual("level-pipe-button", ((UnityEngine.UI.Image)binding.Button.targetGraphic).sprite.name);
                if (binding.Lock) Assert.AreEqual("lock-badge", binding.Lock.GetComponent<UnityEngine.UI.Image>().sprite.name);
                if (binding.Star) Assert.AreEqual("star-badge", binding.Star.GetComponent<UnityEngine.UI.Image>().sprite.name);
            }
            if (sceneName == "World1" || sceneName == "World2")
            {
                var pipes = images.Where(image => image.sprite && image.sprite.name == "level-link-pipe").ToArray();
                Assert.AreEqual(13, pipes.Length, "Every adjacent level is joined by a smaller pipe.");
                Assert.IsTrue(pipes.All(pipe => !pipe.raycastTarget), "Decorative pipes must not intercept level input.");
            }
            if (capture) Capture(sceneName, output);
            var settings = UnityEngine.Object.FindFirstObjectByType<MonetizationSettingsView>();
            if (settings)
            {
                settings.Open.onClick.Invoke();
                Assert.IsTrue(settings.Panel.activeInHierarchy);
                yield return null;
                if (capture) Capture(sceneName + "-settings", output);
                settings.Close.onClick.Invoke();
                Assert.IsFalse(settings.Panel.activeSelf);
            }
        }
        Debug.Log("Menu theme checked; capture requested: " + capture + ". Output: " + output);
    }

    private static void Capture(string scene, string output)
    {
        var camera = Camera.main ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
        Assert.IsNotNull(camera);
        Level1ArtCaptureTests.Capture(camera, 720, 1280, Path.Combine(output, scene + "-portrait.png"));
        Level1ArtCaptureTests.Capture(camera, 1280, 720, Path.Combine(output, scene + "-landscape.png"));
    }

    [UnityTearDown]
    public IEnumerator RestoreServices()
    {
        yield return SceneManager.LoadSceneAsync("Splash");
        SceneNavigator.ConfigureForTests(null);
        GameServices.ResetForTests();
    }

    private sealed class PreviewStorage : ISaveStorage
    {
        public SaveData Load()
        {
            var data = new SaveData();
            data.Levels.Add(new LevelProgress { LevelId = "level-01", Completed = true, StarEarned = true, BestTimeSeconds = 5 });
            return data;
        }
        public void Save(SaveData data) { }
    }
}
