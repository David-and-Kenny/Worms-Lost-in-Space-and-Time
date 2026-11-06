using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Worms.Configuration;
using Worms.Core;
using Worms.Gameplay;

namespace Worms.LevelValidation
{
    public interface ILevelTrialRunner
    {
        IEnumerator Run(LevelManifest manifest, TrialPlan plan, int framesPerSecond, Action<TrialResult> completed);
    }

    public sealed class DisposableProgress : ISaveStorage
    {
        public int Writes { get; private set; }
        public SaveData Load() => null;
        public void Save(SaveData data) { Writes++; }
    }

    public sealed class TrialCatalog : ILevelCatalog
    {
        private readonly LevelDefinition definition;
        private readonly ILevelCatalog published;
        public TrialCatalog(LevelManifest manifest)
        {
            published = Resources.Load<LevelCatalog>("Configuration/LevelCatalog");
            definition = published.GetLevel(manifest.LevelId);
            if (definition != null) return;
            published = null;
            definition = new LevelDefinition { Id = manifest.LevelId, SceneName = manifest.SceneName, WorldId = "world-1", DisplayName = manifest.LevelId };
        }
        public IReadOnlyList<LevelDefinition> Levels => published?.Levels ?? new[] { definition };
        public LevelDefinition GetLevel(string id) => published != null ? published.GetLevel(id) : id == definition.Id ? definition : null;
        public LevelDefinition FindByScene(string scene) => published != null ? published.FindByScene(scene) : scene == definition.SceneName ? definition : null;
        public LevelDefinition GetNext(string id) => published?.GetNext(id);
    }

    /// <summary>Restores every global modified by a validation session, even on failure.</summary>
    public sealed class ValidationEnvironment : IDisposable
    {
        private readonly float capture = Time.captureDeltaTime;
        private readonly float scale = Time.timeScale;
        private readonly int target = Application.targetFrameRate;
        private readonly UnityEngine.Random.State random = UnityEngine.Random.state;
        private readonly SimulationMode2D simulation = Physics2D.simulationMode;
        private readonly bool background = Application.runInBackground;
        public ValidationEnvironment(bool visual = false)
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Validation environments run only in Play Mode.");
            if (Math.Abs(Time.fixedDeltaTime - .02f) > .000001f) throw new NotSupportedException("This validation profile requires the authored 0.02-second fixed timestep.");
            Time.timeScale = 1;
            Application.targetFrameRate = visual ? 60 : -1;
            Application.runInBackground = true;
        }
        public void Dispose()
        {
            Time.captureDeltaTime = capture;
            Time.timeScale = scale;
            Application.targetFrameRate = target;
            UnityEngine.Random.state = random;
            Physics2D.simulationMode = simulation;
            Application.runInBackground = background;
            GameServices.ResetForTests();
            Worms.Navigation.SceneNavigator.ConfigureForTests(null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void IsolateCommandStartup()
        {
            bool requested = Environment.GetCommandLineArgs().Contains("-worms-validation-request");
#if UNITY_EDITOR
            requested |= !string.IsNullOrEmpty(UnityEditor.SessionState.GetString("Worms.ValidationRequest", null));
#endif
            if (!requested) return;
            GameServices.ConfigureForTests(Resources.Load<LevelCatalog>("Configuration/LevelCatalog"), new DisposableProgress());
        }
    }

    public sealed class TrialObservation
    {
        private readonly LevelManifest manifest;
        private readonly Dictionary<GameObject, string> identities = new Dictionary<GameObject, string>();
        private readonly int startFrame;
        private readonly Player player;
        private readonly Rigidbody2D body;
        private readonly Oxygen oxygen;
        private readonly LevelSessionHost host;
        private int checkpointOrdinal;
        public TrialResult Result { get; } = new TrialResult();
        public ValidationPhysicsClock Clock { get; set; }
        public int Frame => Clock ? Clock.Frame : Time.frameCount - startFrame;
        public int AttachmentFrame { get; private set; } = -1;
        public int AttachmentOrdinal { get; private set; }
        public string SurfaceId { get; private set; }
        public bool Attached => player && player.playerAttached && player.Attachment.HasSurface;
        public Player Player => player;
        public bool TerminalCaptured { get; private set; }
        public TrialObservation(LevelManifest manifest, Player player)
        {
            this.manifest = manifest;
            this.player = player;
            body = player.GetComponent<Rigidbody2D>();
            oxygen = UnityEngine.Object.FindFirstObjectByType<Oxygen>();
            host = LevelSessionHost.GetOrCreate();
            startFrame = Time.frameCount;
            var roots = player.gameObject.scene.GetRootGameObjects();
            foreach (var identity in manifest.Objects)
            {
                string[] path = identity.Hierarchy.Split('/');
                int rootIndex = int.Parse(path[0]);
                if (rootIndex >= roots.Length) throw new InvalidOperationException("Scene identity no longer resolves: " + identity.Id);
                Transform node = roots[rootIndex].transform;
                for (int index = 1; index < path.Length; index++) node = node.GetChild(int.Parse(path[index]));
                if (node.name != identity.Name) throw new InvalidOperationException("Scene hierarchy differs from its manifest: " + identity.Id);
                identities[node.gameObject] = identity.Id;
            }
            player.TraversalChanged += OnTraversal;
            if (host) host.Changed += OnTerminal;
        }
        private void OnTerminal(LevelSession session)
        {
            if (session.State == SessionState.Playing) return;
            Capture(false);
            Result.Elapsed = session.ElapsedSeconds;
            Result.Outcome = session.State == SessionState.Completed ? TrialOutcome.Completed : TrialOutcome.Dead;
            Result.Reason = session.State.ToString();
            Result.Events.Add(Sample(Result.Reason));
            TerminalCaptured = true;
        }
        private void OnTraversal(Player source, PlayerTraversalKind kind, Component surface)
        {
            if (TerminalCaptured) return;
            string id = null;
            if (surface && !identities.TryGetValue(surface.gameObject, out id))
                throw new InvalidOperationException("Encountered an uninspected traversal surface: " + surface.name);
            if (kind == PlayerTraversalKind.Attached)
            {
                SurfaceId = id;
                AttachmentOrdinal++;
                AttachmentFrame = Frame;
            }
            Result.Events.Add(Sample(kind.ToString(), id));
        }
        public TraversalSample Sample(string kind, string surface = null)
        {
            return new TraversalSample { Frame = Frame, Kind = kind, SurfaceId = surface ?? SurfaceId,
                AttachmentOrdinal = AttachmentOrdinal, Time = Time.timeSinceLevelLoad,
                X = player ? player.transform.position.x : Result.X, Y = player ? player.transform.position.y : Result.Y,
                Angle = player ? player.transform.eulerAngles.z : 0, VelocityX = body ? body.linearVelocity.x : 0,
                VelocityY = body ? body.linearVelocity.y : 0, Oxygen = oxygen ? oxygen.air : -1 };
        }
        public void Capture(bool postUpdate = true)
        {
            var sample = Sample("Position");
            Result.LastFrame = sample.Frame; Result.Elapsed = sample.Time;
            Result.X = sample.X; Result.Y = sample.Y; Result.Oxygen = sample.Oxygen;
            Result.Angle = sample.Angle;
            Result.SafeWaitSeconds = oxygen && oxygen.airBurnRate > 0 ? Math.Max(0, oxygen.air / oxygen.airBurnRate) : -1;
            Result.SurfaceId = SurfaceId; Result.AttachmentOrdinal = AttachmentOrdinal; Result.AttachmentFrame = AttachmentFrame;
            if (postUpdate && Attached && checkpointOrdinal != AttachmentOrdinal && Frame >= AttachmentFrame + 2)
            {
                Result.Events.Add(Sample("AttachmentCheckpoint"));
                checkpointOrdinal = AttachmentOrdinal;
            }
            if (Frame % 3 == 0) Result.Trace.Add(sample);
        }
        public void Dispose()
        {
            if (player) player.TraversalChanged -= OnTraversal;
            if (host) host.Changed -= OnTerminal;
        }
    }

    /// <summary>One input sample per frame, through the exact boundary used by human input.</summary>
    public sealed class ReplayInput : IPlayerInput
    {
        private readonly TrialObservation observation;
        private readonly TrialPlan plan;
        private readonly int rate;
        private int next;
        private int sampledFrame = -1;
        private bool held;
        public string Divergence { get; private set; }
        public ReplayInput(TrialObservation observation, TrialPlan plan, int rate)
        { this.observation = observation; this.plan = plan; this.rate = rate; }
        public bool JumpHeld
        {
            get
            {
                int frame = observation.Frame;
                if (frame == sampledFrame) return held;
                sampledFrame = frame;
                held = false;
                if (next >= plan.Jumps.Count || Divergence != null) return false;
                var jump = plan.Jumps[next];
                int target = plan.RelativeTiming
                    ? observation.AttachmentFrame + (int)Math.Round(jump.DelaySeconds * rate)
                    : jump.Frame;
                if (plan.RelativeTiming && observation.AttachmentOrdinal < jump.AttachmentOrdinal) return false;
                if (frame < target) return false;
                if (frame > target || !observation.Attached || observation.SurfaceId != jump.SurfaceId
                    || observation.AttachmentOrdinal != jump.AttachmentOrdinal)
                { Divergence = "Jump " + next + " did not reach its expected attachment/time."; return false; }
                held = true;
                next++;
                return true;
            }
        }
        public bool AllDelivered => next == plan.Jumps.Count;
    }

    public sealed class UnityTrialRunner : ILevelTrialRunner
    {
        private readonly LevelValidationRequest request;
        private readonly Func<bool> cancelled;
        private readonly Func<LevelManifest, IEnumerator> load;
        private readonly bool controlledPhysics;
        public UnityTrialRunner(LevelValidationRequest request, Func<bool> cancelled = null, Func<LevelManifest, IEnumerator> load = null, bool controlledPhysics = true)
        { this.request = request; this.cancelled = cancelled ?? (() => false); this.load = load ?? LoadScene; this.controlledPhysics = controlledPhysics; }
        private static IEnumerator LoadScene(LevelManifest manifest)
        {
#if UNITY_EDITOR
            var operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(manifest.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            var operation = SceneManager.LoadSceneAsync(manifest.ScenePath, LoadSceneMode.Single);
#endif
            if (operation == null) throw new InvalidOperationException("Cannot load " + manifest.ScenePath);
            while (!operation.isDone) yield return null;
        }
        public IEnumerator Run(LevelManifest manifest, TrialPlan plan, int framesPerSecond, Action<TrialResult> completed)
        {
            TrialObservation observation = null;
            ReplayInput input = null;
            string loadError = null;
            Time.captureDeltaTime = 1f / framesPerSecond;
            Physics2D.simulationMode = controlledPhysics ? SimulationMode2D.Script : SimulationMode2D.FixedUpdate;
            // Empty warmup removes the previous world's timers and normalizes the render/physics cadence.
            var progress = new DisposableProgress();
            GameServices.ConfigureForTests(new TrialCatalog(manifest), progress);
            yield return LoadScene(new LevelManifest { ScenePath = "Assets/Tests/LevelValidation/Fixtures/EmptyReset.unity" });
            for (int frame = 0; frame < framesPerSecond / 10; frame++) yield return null;
            UnityEngine.Random.InitState(request.Seed);
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) =>
            {
                if (scene.name != manifest.SceneName) return;
                try
                {
                    var players = UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
                    if (players.Length != 1) throw new InvalidOperationException("Expected exactly one active Player.");
                    observation = new TrialObservation(manifest, players[0]);
                    input = new ReplayInput(observation, plan, framesPerSecond);
                    players[0].InputSource = input;
                    if (controlledPhysics)
                    {
                        observation.Clock = new GameObject("Validation physics clock").AddComponent<ValidationPhysicsClock>();
                        observation.Clock.FramesPerSecond = framesPerSecond;
                        foreach (var hazard in UnityEngine.Object.FindObjectsByType<MovingSpikes>(FindObjectsSortMode.None))
                            hazard.ElapsedTimeSource = () => observation.Frame / (float)framesPerSecond;
                    }
                    if (request.Visual) players[0].gameObject.AddComponent<TraversalPreview>();
                }
                catch (Exception error) { loadError = error.ToString(); }
            };
            SceneManager.sceneLoaded += loaded;
            string runtimeError = null;
            Application.LogCallback onLog = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) runtimeError = message + "\n" + stack;
            };
            Application.logMessageReceived += onLog;
            try
            {
                yield return load(manifest);
                if (loadError != null || observation == null)
                    throw new InvalidOperationException(loadError ?? "Scene did not initialize the trial observer.");
                var host = LevelSessionHost.GetOrCreate();
                while (true)
                {
                    if (!observation.TerminalCaptured) observation.Capture();
                    var result = observation.Result;
                    if (runtimeError != null)
                    { result.Outcome = TrialOutcome.ExecutionError; result.Reason = runtimeError; break; }
                    if (host && host.Session.State != SessionState.Playing)
                    {
                        break;
                    }
                    if (input.Divergence != null)
                    { result.Outcome = TrialOutcome.Diverged; result.Reason = input.Divergence; break; }
                    if (cancelled())
                    { result.Outcome = TrialOutcome.Cancelled; result.Reason = "Wall-clock budget exhausted or cancelled."; break; }
                    if (result.LastFrame >= request.TrialSeconds * framesPerSecond)
                    { result.Outcome = TrialOutcome.TimedOut; result.Reason = "Trial simulation limit reached."; break; }
                    if (plan.StopAtFrontier && input.AllDelivered && observation.Attached
                        && observation.AttachmentFrame > plan.ObserveAfterFrame
                        && observation.Frame >= observation.AttachmentFrame)
                    { result.Outcome = TrialOutcome.Frontier; result.Reason = "Reached an attachment through gameplay."; break; }
                    if (!observation.Player)
                    { result.Outcome = TrialOutcome.Diverged; result.Reason = "Player disappeared without a session terminal state."; break; }
                    yield return null;
                }
                observation.Result.SaveWrites = progress.Writes;
                completed(observation.Result);
            }
            finally
            {
                SceneManager.sceneLoaded -= loaded;
                Application.logMessageReceived -= onLog;
                observation?.Dispose();
            }
        }
    }
}
