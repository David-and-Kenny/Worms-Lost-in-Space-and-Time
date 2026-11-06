using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Editor checks for migration and fresh-checkout validation.</summary>
public static class DevelopmentChecks
{
    [MenuItem("Worms/Validate build scenes")]
    public static void ValidateScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var previous = EditorSceneManager.GetSceneManagerSetup();
        int missing = 0;
        try
        {
            foreach (var entry in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            {
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                {
                    missing += SerializedBindingValidator.Validate(root, entry.path);
                }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                missing += SerializedBindingValidator.Validate(prefab, prefabPath);
            }
            if (missing > 0) throw new Exception("Invalid script or serialized callback bindings: " + missing);
            Debug.Log("All enabled build scenes and prefabs loaded with valid script and callback bindings.");
        }
        finally
        {
            if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }

    public static void BuildWindows()
    {
        ValidateScenes();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = "Builds/Windows/Worms.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows development build failed: " + report.summary.result);
    }

    public static void BuildAndroid()
    {
        ValidateScenes();
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = "Builds/Android/Worms.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Android development build failed: " + report.summary.result);
    }
}
