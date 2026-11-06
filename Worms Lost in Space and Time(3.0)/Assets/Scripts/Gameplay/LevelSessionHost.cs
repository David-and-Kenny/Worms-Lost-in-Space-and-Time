using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Worms.Core;

namespace Worms
{
    /// <summary>Scene lifetime and Unity clock for a plain gameplay session.</summary>
    public sealed class LevelSessionHost : MonoBehaviour
    {
        private static LevelSessionHost current;
        private bool paused;
        public bool IsPaused => paused;
        public static LevelSessionHost Current => current && current.gameObject.scene == SceneManager.GetActiveScene() ? current : null;
        public LevelSession Session { get; private set; }
        public event Action<LevelSession> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GetOrCreate();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { GetOrCreate(); }

        public static LevelSessionHost GetOrCreate()
        {
            if (Current) return current;
            GameServices.EnsureInitialized();
            if (GameServices.Catalog.FindByScene(SceneManager.GetActiveScene().name) == null) return null;
            // With scene reload disabled, Awake does not run again when statics reset.
            foreach (var existing in FindObjectsByType<LevelSessionHost>())
            {
                if (existing.gameObject.scene != SceneManager.GetActiveScene()) continue;
                existing.Initialize();
                return existing;
            }
            return new GameObject("LevelSession").AddComponent<LevelSessionHost>();
        }

        private void Awake()
        {
            if (Current && current != this) { Destroy(gameObject); return; }
            GameServices.EnsureInitialized();
            var level = GameServices.Catalog.FindByScene(gameObject.scene.name);
            if (level == null) { Destroy(this); return; }
            Initialize();
        }

        private void Initialize()
        {
            var level = GameServices.Catalog.FindByScene(gameObject.scene.name);
            if (Session != null) Session.Changed -= ForwardChanged;
            current = this;
            paused = false;
            Session = new LevelSession(level, GameServices.Progression);
            Session.Changed += ForwardChanged;
            Changed?.Invoke(Session);
        }

        private void ForwardChanged(LevelSession session) { Changed?.Invoke(session); }
        private void Update() { if (!paused && Session != null) Session.Tick(Time.deltaTime); }
        private void OnApplicationPause(bool value) { paused = value; }
        private void OnDestroy()
        {
            if (Session != null) Session.Changed -= ForwardChanged;
            if (current == this) current = null;
        }
    }

    public static class FinishRun
    {
        public static bool TryComplete(GameObject player)
        {
            var host = LevelSessionHost.GetOrCreate();
            if (host == null || !host.Session.TryComplete()) return false;
            UnityEngine.Object.Destroy(player);
            return true;
        }
    }
}
