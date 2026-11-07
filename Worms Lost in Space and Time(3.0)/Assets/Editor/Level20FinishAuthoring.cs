using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit, repeatable authoring of the final level's missing exit.</summary>
public static class Level20FinishAuthoring
{
    public const string ScenePath = "Assets/Scenes/World2/Level20.unity";
    public static readonly Vector3 ExitPosition = new Vector3(7.5f, -1f, 0);

    public static void CaptureOverview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var host = new GameObject("Temporary overview camera");
        var camera = host.AddComponent<Camera>();
        camera.transform.position = new Vector3(-5, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = 9;
        var target = new RenderTexture(1400, 700, 24);
        var pixels = new Texture2D(1400, 700, TextureFormat.RGB24, false);
        try
        {
            foreach (var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                text.font.RequestCharactersInTexture(text.text, text.fontSize, FontStyle.Normal);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1400, 700), 0, 0);
            pixels.Apply();
            string file = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../TestResults/Level20-Overview.png"));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
            System.IO.File.WriteAllBytes(file, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = null;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(pixels);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    [MenuItem("Worms/Authoring/Add Level 20 finish")]
    public static void Create()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save or discard the current scene before authoring the exit.");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var existing = UnityEngine.Object.FindObjectsByType<Finish>(FindObjectsSortMode.None);
        if (existing.Length > 0)
        {
            if (existing.Length != 1 || Vector3.Distance(existing[0].transform.position, ExitPosition) > .001f)
                throw new InvalidOperationException("Review the existing exit instead of replacing authored content.");
            return;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Finish.prefab");
        if (!prefab) throw new InvalidOperationException("The shared finish prefab is missing.");
        var exit = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        exit.name = "Finish";
        exit.transform.position = ExitPosition;
        exit.GetComponent<BoxCollider2D>().size = new Vector2(.5f, 5.5f);
        exit.GetComponent<SaveGame>().currentLevel = 20;
        exit.GetComponent<SaveGame>().starTime = 0;
        exit.GetComponent<Finish>().nextLevel = "";

        var frame = new GameObject("Exit frame");
        frame.transform.SetParent(exit.transform, false);
        var renderer = frame.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Artwork/Menus/menu-button.png").OfType<Sprite>().Single();
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(1.8f, 6f);
        renderer.color = new Color(.65f, 1f, .8f);
        renderer.sortingOrder = -1;

        var sign = new GameObject("Exit sign");
        sign.transform.SetParent(exit.transform, false);
        sign.transform.localPosition = new Vector3(0, 2.1f, -.1f);
        var text = sign.AddComponent<TextMesh>();
        text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Artwork/Menus/Fonts/Bungee-Regular.ttf");
        text.text = "EXIT";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 64;
        text.characterSize = .18f;
        text.color = Color.white;
        sign.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        sign.GetComponent<MeshRenderer>().sortingOrder = 5;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Level 20 exit authored beyond the final existing wheel; movement and existing obstacles unchanged.");
    }
}
