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

public sealed class Level1ArtCaptureTests
{
    [UnityTest]
    public IEnumerator CaptureAdvancingLevelOne()
    {
        bool capture = Environment.GetCommandLineArgs().Contains("-worms-art-capture");
        if (capture)
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType,
                "Screenshots require a rendering device; do not use -nographics.");
        var catalog = Resources.Load<LevelCatalog>("Configuration/LevelCatalog");
        GameServices.ConfigureForTests(catalog, new MemoryStorage());
        SceneNavigator.ConfigureForTests(null);
        yield return SceneManager.LoadSceneAsync("Level1");
        yield return null;
        var player = UnityEngine.Object.FindFirstObjectByType<Player>();
        var wheel = UnityEngine.Object.FindFirstObjectByType<Wheel>();
        Assert.IsNotNull(player);
        Assert.IsNotNull(wheel);
        Assert.AreEqual("player-player", player.GetComponent<SpriteRenderer>().sprite.name,
            "The character is excluded from the art replacement.");
        var renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        int themedSurfaces = 0;
        foreach (var renderer in renderers)
        {
            if (!renderer.sprite) continue;
            Vector2 expected;
            switch (renderer.sprite.name)
            {
                case "cog-spokes": expected = new Vector2(4.33f, 4.32f); break;
                case "cog-double-ring": expected = new Vector2(4.32f, 4.32f); break;
                case "cog-cutouts": expected = new Vector2(4.44f, 4.44f); break;
                case "hazard-pipe": expected = new Vector2(6.16f, .68f); break;
                default: continue;
            }
            Assert.Less(Vector2.Distance(renderer.sprite.bounds.size, expected), .0001f,
                "New artwork must retain its original sprite bounds.");
            themedSurfaces++;
        }
        Assert.AreEqual(22, themedSurfaces, "Five cogs and seventeen pipe hazards should use the new art.");
        Vector3 initialPosition = player.transform.position;
        Quaternion initialRotation = wheel.transform.rotation;
        int initialFrame = Time.frameCount;
        float initialTime = Time.time;
        yield return new WaitForSeconds(.65f);
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults/Art"));
        Directory.CreateDirectory(output);
        var camera = Camera.main;
        Assert.IsNotNull(camera);
        if (capture)
        {
            Capture(camera, 1280, 720, Path.Combine(output, "level-01-landscape.png"));
            Capture(camera, 720, 1280, Path.Combine(output, "level-01-portrait.png"));
        }
        yield return new WaitForSeconds(.7f);
        Assert.IsTrue(player, "Player should remain alive for the initial art review.");
        Assert.Greater(Time.frameCount - initialFrame, 2);
        Assert.Greater(Time.time - initialTime, 1f);
        Assert.Greater(Quaternion.Angle(initialRotation, wheel.transform.rotation), .1f,
            "A render of a frozen scene is not a simulation capture.");
        if (capture) File.WriteAllText(Path.Combine(output, "simulation.txt"),
            "Scene: Level1\nFrames advanced: " + (Time.frameCount - initialFrame) +
            "\nGame time advanced: " + (Time.time - initialTime) +
            "\nPlayer from: " + initialPosition + "\nPlayer to: " + player.transform.position +
            "\nWheel angle advanced: " + Quaternion.Angle(initialRotation, wheel.transform.rotation) +
            "\nStorage: in-memory; no developer saves accessed." +
            "\nCapture: actual runtime camera rendered to textures, including existing Canvas UI." +
            "\nNo inputs, teleportation, time-step changes, or completion calls used.\n");
        Debug.Log("Level 1 art simulation advanced and captured: " + output);
    }

    internal static void Capture(Camera camera, int width, int height, string path)
    {
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        float previousAspect = camera.aspect;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.aspect = (float)width / height;
            // Render the existing UI with this actual camera; do not reconstruct the HUD in an image editor.
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 1;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
        }
        finally
        {
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(pixels);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    [UnityTearDown]
    public IEnumerator RestoreServices()
    {
        if (GameServices.Saves == null) yield break;
        yield return SceneManager.LoadSceneAsync("Splash");
        GameServices.ResetForTests();
        SceneNavigator.ConfigureForTests(null);
    }

    private sealed class MemoryStorage : ISaveStorage
    {
        public SaveData Load() => null;
        public void Save(SaveData data) { }
    }
}
