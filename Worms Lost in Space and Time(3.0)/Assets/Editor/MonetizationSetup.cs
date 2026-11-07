using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Worms.Monetization;
using Worms.UI;

/// <summary>Authors editable settings and UI without resaving gameplay scenes or changing their physics.</summary>
public static class MonetizationSetup
{
    public const string SettingsPath = "Assets/Resources/Configuration/MonetizationSettings.asset";
    public const string GoogleSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

    [MenuItem("Worms/Monetization/Create settings and menu UI")]
    public static void Apply()
    {
        if (AssetDatabase.LoadAssetAtPath<MonetizationSettings>(SettingsPath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MonetizationSettings>(), SettingsPath);
        CreateGoogleSettings();
        AddMenu("MainMenu");
        AddMenu("WorldMenu");
        AssetDatabase.SaveAssets();
    }

    private static void CreateGoogleSettings()
    {
        if (AssetDatabase.LoadMainAssetAtPath(GoogleSettingsPath) != null) return;
        var type = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor", true);
        var settings = ScriptableObject.CreateInstance(type);
        Directory.CreateDirectory(Path.GetDirectoryName(GoogleSettingsPath));
        AssetDatabase.Refresh();
        AssetDatabase.CreateAsset(settings, GoogleSettingsPath);
        var serialized = new SerializedObject(settings);
        serialized.FindProperty("adMobAndroidAppId").stringValue = "ca-app-pub-3940256099942544~3347511713";
        serialized.FindProperty("adMobIOSAppId").stringValue = "ca-app-pub-3940256099942544~1458002511";
        serialized.FindProperty("userTrackingUsageDescription").stringValue = "";
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddMenu(string name)
    {
        string path = "Assets/Resources/UI/" + name + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponent<MonetizationSettingsView>() != null) return;
            var view = root.AddComponent<MonetizationSettingsView>();
            var safe = root.GetComponentInChildren<SafeAreaLayout>(true).transform;
            view.Open = Button(safe, "Settings", .02f, .94f, .28f, .995f);
            var panel = Rect(safe, "Settings Panel", 0, 0, 1, 1);
            panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.025f, .055f, .12f, 1);
            view.Panel = panel.gameObject;
            Label(panel, "Settings", .83f, .94f, 38);
            view.Buy = Button(panel, "Remove Ads — store unavailable", .10f, .68f, .90f, .77f);
            view.PurchaseLabel = view.Buy.GetComponentInChildren<UnityEngine.UI.Text>();
            view.Restore = Button(panel, "Restore Purchases", .10f, .56f, .90f, .65f);
            view.Privacy = Button(panel, "Privacy Choices", .10f, .44f, .90f, .53f);
            view.Policy = Button(panel, "Privacy Policy", .10f, .32f, .90f, .41f);
            view.Close = Button(panel, "Back", .10f, .06f, .90f, .15f);
            view.Status = Label(panel, "Purchases will be available when store setup is complete.", .17f, .29f, 24);
            view.Panel.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static RectTransform Rect(Transform parent, string name, float left, float bottom, float right, float top)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
    private static UnityEngine.UI.Text Label(Transform parent, string text, float bottom, float top, int size)
    {
        var label = Rect(parent, "Label", .05f, bottom, .95f, top).gameObject.AddComponent<UnityEngine.UI.Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = size;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 16;
        label.resizeTextMaxSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }
    private static UnityEngine.UI.Button Button(Transform parent, string text, float left, float bottom, float right, float top)
    {
        var rect = Rect(parent, text, left, bottom, right, top);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(.12f, .26f, .40f, 1);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        Label(rect, text, 0, 1, 28);
        return button;
    }
}
