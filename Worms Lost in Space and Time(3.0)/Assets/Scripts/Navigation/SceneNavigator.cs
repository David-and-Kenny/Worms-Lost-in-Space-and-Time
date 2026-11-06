using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Worms.Core;

namespace Worms.Navigation
{
    public interface ISceneLoader
    {
        string CurrentSceneName { get; }
        bool CanLoad(string sceneName);
        void Load(string sceneName, Action completed);
        void Quit();
    }

    /// <summary>Validates every destination before handing one request to the platform loader.</summary>
    public sealed class SceneNavigator
    {
        private static SceneNavigator instance;
        private readonly ISceneLoader loader;
        private readonly ILevelCatalog catalog;
        private readonly ProgressionService progression;
        private readonly PointerPressGate pointerGate;
        public bool IsLoading { get; private set; }

        public static SceneNavigator Instance
        {
            get
            {
                if (instance == null)
                {
                    GameServices.EnsureInitialized();
                    instance = new SceneNavigator(new UnitySceneLoader(), GameServices.Catalog, GameServices.Progression, GameServices.PointerGate);
                }
                return instance;
            }
        }

        public SceneNavigator(ISceneLoader loader, ILevelCatalog catalog, ProgressionService progression, PointerPressGate pointerGate = null)
        {
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.progression = progression ?? throw new ArgumentNullException(nameof(progression));
            this.pointerGate = pointerGate ?? new PointerPressGate();
        }

        public bool Continue() => OpenLevel(progression.GetContinueLevelId());
        public bool OpenWorld(string worldId)
        {
            string scene = worldId == "world-1" ? "World1" : worldId == "world-2" ? "World2" : null;
            bool hasContent = false;
            foreach (var level in catalog.Levels) if (level.WorldId == worldId) hasContent = true;
            return hasContent && Load(scene);
        }
        public bool OpenLevel(string id)
        {
            var level = catalog.GetLevel(id);
            return level != null && progression.IsUnlocked(id) && Load(level.SceneName);
        }
        public bool Restart()
        {
            var level = catalog.FindByScene(loader.CurrentSceneName);
            return level != null && Load(level.SceneName);
        }
        public bool BackToWorld()
        {
            var level = catalog.FindByScene(loader.CurrentSceneName);
            return level != null ? OpenWorld(level.WorldId) : MainMenu();
        }
        public bool MainMenu() => Load("StorySelect");
        public void Quit() => loader.Quit();
        public bool RouteLegacyScene(string scene)
        {
            if (scene == "StorySelect") return MainMenu();
            if (scene == "World1") return OpenWorld("world-1");
            if (scene == "World2") return OpenWorld("world-2");
            var level = catalog.FindByScene(scene);
            return level != null && OpenLevel(level.Id);
        }
        public bool OpenNext(CompletionResult result)
        {
            return result != null && result.NextLevelId != null && OpenLevel(result.NextLevelId);
        }
        private bool Load(string scene)
        {
            if (IsLoading || string.IsNullOrEmpty(scene) || !loader.CanLoad(scene)) return false;
            IsLoading = true;
            pointerGate.BlockUntilRelease();
            try { loader.Load(scene, () => IsLoading = false); }
            catch { IsLoading = false; throw; }
            return true;
        }
        public static void ConfigureForTests(SceneNavigator navigator) => instance = navigator;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => instance = null;

        private sealed class UnitySceneLoader : ISceneLoader
        {
            public string CurrentSceneName => SceneManager.GetActiveScene().name;
            public bool CanLoad(string sceneName) => Application.CanStreamedLevelBeLoaded(sceneName);
            public void Load(string sceneName, Action completed)
            {
                var operation = SceneManager.LoadSceneAsync(sceneName);
                if (operation == null) throw new InvalidOperationException("Unable to load " + sceneName);
                operation.completed += _ => completed();
            }
            public void Quit()
            {
#if !UNITY_EDITOR
                Application.Quit();
#endif
            }
        }
    }
}
