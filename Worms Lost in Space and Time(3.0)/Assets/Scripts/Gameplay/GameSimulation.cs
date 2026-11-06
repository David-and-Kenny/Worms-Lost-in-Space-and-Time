using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Worms.Gameplay
{
    /// <summary>Orders gameplay before Unity's physics step. One instance belongs to each scene.</summary>
    public sealed class GameSimulation : MonoBehaviour
    {
        public const int TicksPerSecond = 50;
        public const float StepSeconds = 1f / TicksPerSecond;

        private readonly List<Wheel> wheels = new List<Wheel>();
        private readonly List<MovingSpikes> hazards = new List<MovingSpikes>();
        private readonly List<Player> players = new List<Player>();
        private readonly List<CountdownExplode> countdowns = new List<CountdownExplode>();
        private readonly List<Oxygen> oxygen = new List<Oxygen>();
        private readonly List<AlienC> emitters = new List<AlienC>();
        public int Tick { get; private set; }
        public float ElapsedSeconds => Tick * StepSeconds;
        public bool AutomaticSteps { get; set; } = true;
        public event System.Action<int> Stepped;

        private void Awake()
        {
            if (Mathf.Abs(Time.fixedDeltaTime - StepSeconds) > .000001f)
                throw new System.NotSupportedException("Game simulation requires the authored 0.02-second fixed timestep.");
        }

        public static GameSimulation For(Component owner)
        {
            foreach (var simulation in FindObjectsByType<GameSimulation>(FindObjectsSortMode.None))
                if (simulation.gameObject.scene == owner.gameObject.scene) return simulation;

            var root = new GameObject("Game simulation");
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            return root.AddComponent<GameSimulation>();
        }

        public void Register(Wheel item) => wheels.Add(item);
        public void Register(MovingSpikes item) => hazards.Add(item);
        public void Register(Player item) => players.Add(item);
        public void Register(CountdownExplode item) => countdowns.Add(item);
        public void Register(Oxygen item) => oxygen.Add(item);
        public void Register(AlienC item) => emitters.Add(item);
        public void Unregister(Wheel item) => wheels.Remove(item);
        public void Unregister(MovingSpikes item) => hazards.Remove(item);
        public void Unregister(Player item) => players.Remove(item);
        public void Unregister(CountdownExplode item) => countdowns.Remove(item);
        public void Unregister(Oxygen item) => oxygen.Remove(item);
        public void Unregister(AlienC item) => emitters.Remove(item);

        private void FixedUpdate()
        {
            if (AutomaticSteps) Step();
        }

        public void Step()
        {
            var host = LevelSessionHost.Current;
            if (host && host.gameObject.scene != gameObject.scene) host = null;
            if (host && (host.IsPaused || host.Session.State != Core.SessionState.Playing)) return;

            Tick++;
            if (host) host.Advance(StepSeconds);
            // Reverse iteration permits a component to disable/unregister itself during its step.
            for (int index = wheels.Count - 1; index >= 0; index--) wheels[index].Advance(StepSeconds);
            for (int index = hazards.Count - 1; index >= 0; index--) hazards[index].Advance(ElapsedSeconds);
            for (int index = players.Count - 1; index >= 0; index--) players[index].Advance(Tick);
            for (int index = countdowns.Count - 1; index >= 0; index--) countdowns[index].Advance(StepSeconds);
            for (int index = oxygen.Count - 1; index >= 0; index--) oxygen[index].Advance(StepSeconds);
            for (int index = emitters.Count - 1; index >= 0; index--) emitters[index].Advance(StepSeconds);
            Stepped?.Invoke(Tick);
        }
    }
}
