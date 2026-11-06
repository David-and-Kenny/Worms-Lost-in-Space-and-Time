#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Worms.Configuration;

namespace Worms.LevelValidation
{
    /// <summary>Read-only scene inspection; never serializes changes to authored content.</summary>
    public static class SceneInspection
    {
        public static List<LevelManifest> Inspect(string selection)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Inspect scenes outside Play Mode.");
            if (EditorSceneManager.GetSceneManagerSetup().Any(item => EditorSceneManager.GetSceneByPath(item.path).isDirty))
                throw new InvalidOperationException("Save or discard dirty scenes before inspection.");
            var catalog = Resources.Load<LevelCatalog>("Configuration/LevelCatalog");
            var levels = catalog.Levels.Where(item => selection == "all" || item.Id == selection || item.SceneName == selection).ToList();
            bool customScene = selection != null && selection.StartsWith("Assets/", StringComparison.Ordinal) && File.Exists(selection) && selection.EndsWith(".unity", StringComparison.Ordinal);
            if (customScene)
            {
                var published = catalog.Levels.FirstOrDefault(item => EditorBuildSettings.scenes.Any(entry => entry.path == selection
                    && Path.GetFileNameWithoutExtension(entry.path) == item.SceneName));
                levels = new List<Core.LevelDefinition> { published ?? new Core.LevelDefinition {
                    Id = Path.GetFileNameWithoutExtension(selection), SceneName = Path.GetFileNameWithoutExtension(selection), WorldId = "world-1" } };
            }
            if (levels.Count == 0) throw new ArgumentException("No catalog level matches " + selection);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var results = new List<LevelManifest>();
            try
            {
                foreach (var level in levels)
                {
                    var entry = EditorBuildSettings.scenes.FirstOrDefault(item => item.enabled && Path.GetFileNameWithoutExtension(item.path) == level.SceneName);
                    var manifest = new LevelManifest { LevelId = level.Id, SceneName = level.SceneName };
                    results.Add(manifest);
                    if (entry == null && !customScene) { manifest.Errors.Add("Scene is not enabled in build settings."); continue; }
                    manifest.ScenePath = customScene ? selection : entry.path;
                    manifest.Fingerprint = Fingerprint(manifest.ScenePath);
                    var scene = EditorSceneManager.OpenScene(manifest.ScenePath, OpenSceneMode.Single);
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var player in root.GetComponentsInChildren<Player>(true)) Add(manifest, player, "spawn");
                        foreach (var surface in root.GetComponentsInChildren<WheelSurface>(true))
                        {
                            Add(manifest, surface, "wheel");
                            if (!surface.wheel || !surface.wheel.holder) manifest.Errors.Add("Wheel lacks rotation/holder reference: " + surface.name);
                        }
                        foreach (var surface in root.GetComponentsInChildren<Conveyor>(true)) Add(manifest, surface, "conveyor");
                        foreach (var finish in root.GetComponentsInChildren<Finish>(true)) Add(manifest, finish, "finish");
                        foreach (var finish in root.GetComponentsInChildren<SaveGame>(true)) Add(manifest, finish, "finish");
                        foreach (var hazard in root.GetComponentsInChildren<Collider2D>(true))
                            if (hazard.CompareTag("Deadly")) Add(manifest, hazard, "hazard");
                        foreach (var hazard in root.GetComponentsInChildren<MovingSpikes>(true))
                            if (!hazard.startPos || !hazard.endPos) manifest.Errors.Add("Moving hazard lacks path endpoints: " + hazard.name);
                        foreach (var component in root.GetComponentsInChildren<Component>(true))
                            if (component == null) manifest.Errors.Add("Missing script under " + root.name);
                    }
                    if (manifest.Objects.Count(item => item.Role == "spawn") != 1) manifest.Errors.Add("Expected exactly one active player spawn.");
                    if (!manifest.Objects.Any(item => item.Role == "finish")) manifest.Errors.Add("No active authored finish trigger.");
                    if (!manifest.Objects.Any(item => item.Role == "wheel" || item.Role == "conveyor"))
                        manifest.Warnings.Add("No attachable surfaces; only a direct route can succeed.");
                    if (!UnityEngine.Object.FindFirstObjectByType<Oxygen>()) manifest.Warnings.Add("No active oxygen component; oxygen readings use -1 (unavailable).");
                }
            }
            finally
            {
                if (setup.Length > 0 && setup.All(item => !string.IsNullOrEmpty(item.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            return results;
        }

        private static void Add(LevelManifest manifest, Component component, string role)
        {
            if (!component.gameObject.activeInHierarchy || (component is Behaviour behaviour && !behaviour.enabled)) return;
            var body = component.GetComponent<Rigidbody2D>();
            var colliders = component.GetComponents<Collider2D>();
            if (role == "spawn" && (!body || !colliders.Any(item => item.enabled))) manifest.Errors.Add("Player requires an enabled collider and Rigidbody2D.");
            if (role == "finish" && !colliders.Any(item => item.enabled && item.isTrigger)) manifest.Errors.Add("Finish requires an enabled trigger.");
            if (role == "wheel" || role == "conveyor")
                if (!colliders.Any(item => item.enabled && !item.isTrigger)) manifest.Errors.Add("Surface requires an enabled collision collider: " + component.name);
            string id = GlobalObjectId.GetGlobalObjectIdSlow(component.gameObject).ToString();
            if (manifest.Objects.Any(item => item.Id == id && item.Role == role)) return;
            manifest.Objects.Add(new SceneObjectIdentity { Id = id, Name = component.name, Role = role,
                Hierarchy = Hierarchy(component.transform), X = component.transform.position.x, Y = component.transform.position.y,
                AngularSpeed = component is WheelSurface surface && surface.wheel ? surface.wheel.speed * surface.wheel.direction : 0 });
        }

        public static string Hierarchy(Transform transform)
        {
            var indices = new List<int>();
            while (transform != null) { indices.Add(transform.GetSiblingIndex()); transform = transform.parent; }
            indices.Reverse();
            return string.Join("/", indices);
        }

        public static string Fingerprint(string scenePath)
        {
            var files = new SortedSet<string>(AssetDatabase.GetDependencies(scenePath, true), StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles("Assets/Scripts", "*", SearchOption.AllDirectories)) files.Add(file.Replace('\\', '/'));
            foreach (string file in Directory.GetFiles("Assets/Tests/LevelValidation", "*", SearchOption.AllDirectories)
                .Where(item => item.EndsWith(".cs", StringComparison.Ordinal) || item.EndsWith(".asmdef", StringComparison.Ordinal))) files.Add(file.Replace('\\', '/'));
            foreach (string file in new[] { "ProjectSettings/Physics2DSettings.asset", "ProjectSettings/TimeManager.asset",
                "Assets/Resources/Configuration/LevelCatalog.asset",
                "ProjectSettings/ProjectVersion.txt", "ProjectSettings/TagManager.asset", "ProjectSettings/EditorBuildSettings.asset",
                "Packages/manifest.json", "Packages/packages-lock.json" }) files.Add(file);
            foreach (string file in files.ToArray()) if (File.Exists(file + ".meta")) files.Add(file + ".meta");
            using (var hash = SHA256.Create())
            using (var stream = new MemoryStream())
            {
                foreach (string file in files.Where(File.Exists))
                {
                    byte[] name = Encoding.UTF8.GetBytes(file + "\0");
                    stream.Write(name, 0, name.Length);
                    bool text = new[] { ".cs", ".meta", ".asmdef", ".json", ".unity", ".prefab", ".asset", ".mat", ".anim", ".controller", ".lighting" }.Contains(Path.GetExtension(file));
                    byte[] bytes = text ? Encoding.UTF8.GetBytes(File.ReadAllText(file).Replace("\r\n", "\n")) : File.ReadAllBytes(file);
                    byte[] digest = hash.ComputeHash(bytes);
                    stream.Write(digest, 0, digest.Length);
                }
                return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
#endif
