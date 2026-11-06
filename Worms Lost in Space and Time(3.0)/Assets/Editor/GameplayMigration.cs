using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Assigns legacy child-name metadata once, while authoring. Runtime uses typed references.</summary>
public static class GameplayMigration
{
    [MenuItem("Worms/Migration/Gameplay References")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }).OrderBy(item => item))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (Configure(root)) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).OrderBy(item => item))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (var root in scene.GetRootGameObjects()) changed |= Configure(root);
                if (changed) EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (!Application.isBatchMode && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static bool Configure(GameObject root)
    {
        bool changed = false;
        foreach (var wheel in root.GetComponentsInChildren<Wheel>(true))
        {
            // Wheel also rotates hazards and props. Only the legacy attachable role migrates.
            if (!wheel.CompareTag("Wheel")) continue;
            if (!wheel.holder)
            {
                var holder = wheel.transform.Find("Holder");
                if (!holder) throw new InvalidOperationException("Attachable wheel has no authored Holder: " + wheel.name);
                wheel.holder = holder;
                EditorUtility.SetDirty(wheel);
                PrefabUtility.RecordPrefabInstancePropertyModifications(wheel);
                changed = true;
            }
            var surface = wheel.GetComponent<WheelSurface>();
            if (!surface) { surface = wheel.gameObject.AddComponent<WheelSurface>(); changed = true; }
            if (surface.wheel == wheel) continue;
            surface.wheel = wheel;
            EditorUtility.SetDirty(surface);
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            changed = true;
        }
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            if (node.name != "CameraChange") continue;
            var region = node.GetComponent<CameraRegion>();
            if (!region) { region = node.gameObject.AddComponent<CameraRegion>(); changed = true; }
            if (region.center) continue;
            region.center = node.Find("Center");
            if (!region.center) throw new InvalidOperationException("Camera region has no authored Center: " + node.name);
            EditorUtility.SetDirty(region);
            PrefabUtility.RecordPrefabInstancePropertyModifications(region);
            changed = true;
        }
        return changed;
    }
}
