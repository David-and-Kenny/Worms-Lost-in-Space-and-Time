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
        private readonly GameSimulation simulation;
        private readonly Player player;
        private readonly Rigidbody2D body;
        private readonly Oxygen oxygen;
        private readonly LevelSessionHost host;
        private int checkpointOrdinal;
        public TrialResult Result { get; } = new TrialResult();
        public int Frame => simulation.Tick;
        public int AttachmentFrame { get; private set; } = -1;
        public int AttachmentOrdinal { get; private set; }
        public string SurfaceId { get; private set; }
        public bool Attached => player && player.playerAttached && player.Attachment.HasSurface;
        public Player Player => player;
        public bool TerminalCaptured { get; private set; }
        public bool InFreeFlight => player && !Attached && body && body.gravityScale == 0;
        public TrialObservation(LevelManifest manifest, Player player)
        {
            this.manifest = manifest;
            this.player = player;
            body = player.GetComponent<Rigidbody2D>();
            oxygen = UnityEngine.Object.FindFirstObjectByType<Oxygen>();
            host = LevelSessionHost.GetOrCreate();
            simulation = player.Simulation;
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
            simulation.Stepped += OnStepped;
        }
        private void OnStepped(int tick) { if (!TerminalCaptured) Capture(true); }
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
                AttachmentOrdinal = AttachmentOrdinal, Time = simulation.ElapsedSeconds,
                X = body ? body.position.x : Result.X, Y = body ? body.position.y : Result.Y,
                Angle = body ? body.rotation : 0, VelocityX = body ? body.linearVelocity.x : 0,
                VelocityY = body ? body.linearVelocity.y : 0, Oxygen = oxygen ? oxygen.air : -1 };
        }
        public void Capture(bool simulationStep)
        {
            var sample = Sample("Position");
            Result.LastFrame = sample.Frame; Result.Elapsed = sample.Time;
            Result.X = sample.X; Result.Y = sample.Y; Result.Oxygen = sample.Oxygen;
            Result.Angle = sample.Angle;
            Result.VelocityX = sample.VelocityX; Result.VelocityY = sample.VelocityY;
            Result.SafeWaitSeconds = oxygen && oxygen.airBurnRate > 0 ? Math.Max(0, oxygen.air / oxygen.airBurnRate) : -1;
            var countdown = player && player.Attachment.Wheel ? player.Attachment.Wheel.GetComponent<CountdownExplode>() : null;
            if (countdown && countdown.isActiveAndEnabled && countdown.countdown)
            {
                float remaining = Math.Max(0, countdown.timer);
                Result.SafeWaitSeconds = Result.SafeWaitSeconds < 0 ? remaining : Math.Min(Result.SafeWaitSeconds, remaining);
            }
            Result.SurfaceId = SurfaceId; Result.AttachmentOrdinal = AttachmentOrdinal; Result.AttachmentFrame = AttachmentFrame;
            if (simulationStep && Attached && checkpointOrdinal != AttachmentOrdinal && Frame >= AttachmentFrame + 2)
            {
                Result.Events.Add(Sample("AttachmentCheckpoint"));
                checkpointOrdinal = AttachmentOrdinal;
            }
            if (simulationStep && Frame % 3 == 0) Result.Trace.Add(sample);
        }
        public void Dispose()
        {
            if (player) player.TraversalChanged -= OnTraversal;
            if (host) host.Changed -= OnTerminal;
            if (simulation) simulation.Stepped -= OnStepped;
        }
    }

    /// <summary>Supplies observed jump commands on fixed simulation ticks, before player movement.</summary>
    public sealed class ReplayInput : IPlayerTickCommands
    {
        private readonly TrialObservation observation;
        private readonly TrialPlan plan;
        private int next;
        private int sampledFrame = -1;
        private bool held;
        private readonly JumpInputBuffer buttonBuffer = new JumpInputBuffer();
        public string Divergence { get; private set; }
        public ReplayInput(TrialObservation observation, TrialPlan plan)
        { this.observation = observation; this.plan = plan; }
        public List<JumpCommand> Delivered { get; } = new List<JumpCommand>();
        public bool JumpRequested(int frame)
        {
            if (frame == sampledFrame) return held;
            sampledFrame = frame;
            held = false;
            if (plan.ButtonHolds != null)
            {
                buttonBuffer.Sample(plan.ButtonHolds.Any(item => frame >= item.StartFrame && frame <= item.EndFrame));
                bool requested = buttonBuffer.Consume(observation.Attached);
                if (!requested)
                {
                    if (next < plan.Jumps.Count && frame >= plan.Jumps[next].Frame)
                        Divergence = "Held input missed jump " + next + ".";
                    return false;
                }
                if (next >= plan.Jumps.Count || plan.Jumps[next].Frame != frame)
                { Divergence = "Held input requested an unrecorded jump."; return false; }
            }
            if (next >= plan.Jumps.Count || Divergence != null) return false;
            var jump = plan.Jumps[next];
            int target = plan.RelativeTiming
                ? observation.AttachmentFrame + Math.Max(1, (int)Math.Round(jump.DelaySeconds * GameSimulation.TicksPerSecond))
                : jump.Frame;
            if (plan.RelativeTiming && observation.AttachmentOrdinal < jump.AttachmentOrdinal) return false;
            if (frame < target) return false;
            if (frame > target || !observation.Attached || observation.SurfaceId != jump.SurfaceId
                || observation.AttachmentOrdinal != jump.AttachmentOrdinal)
            { Divergence = "Jump " + next + " did not reach its expected attachment/time."; return false; }
            held = true;
            Delivered.Add(new JumpCommand { Frame = frame, SurfaceId = jump.SurfaceId, AttachmentOrdinal = jump.AttachmentOrdinal,
                DelaySeconds = (frame - observation.AttachmentFrame) * GameSimulation.StepSeconds });
            next++;
            return true;
        }
        public bool AllDelivered => next == plan.Jumps.Count;
    }

    public sealed class UnityTrialRunner : ILevelTrialRunner
    {
        private readonly LevelValidationRequest request;
        private readonly Func<bool> cancelled;
        private readonly Func<LevelManifest, IEnumerator> load;
        public UnityTrialRunner(LevelValidationRequest request, Func<bool> cancelled = null, Func<LevelManifest, IEnumerator> load = null)
        { this.request = request; this.cancelled = cancelled ?? (() => false); this.load = load ?? LoadScene; }
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
            var geometry = new RouteGeometry(manifest);
            Time.captureDeltaTime = 1f / framesPerSecond;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
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
                    input = new ReplayInput(observation, plan);
                    players[0].TickCommands = input;
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
                    if (!observation.TerminalCaptured) observation.Capture(false);
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
                    if (result.LastFrame >= request.TrialSeconds * GameSimulation.TicksPerSecond)
                    { result.Outcome = TrialOutcome.TimedOut; result.Reason = "Trial simulation limit reached."; break; }
                    if (plan.StopAtFrontier && plan.FrameLimit > 0 && result.LastFrame >= plan.FrameLimit)
                    { result.Outcome = TrialOutcome.TimedOut; result.Reason = "Transition probe horizon reached; longer probes remain searchable."; break; }
                    if (plan.StopAtFrontier && observation.InFreeFlight && geometry.OutsideSearchEnvelope(result))
                    { result.Outcome = TrialOutcome.TimedOut; result.Reason = "Flight left the search geometry envelope."; break; }
                    if (plan.StopAtFrontier && input.AllDelivered && observation.Attached
                        && observation.AttachmentFrame > plan.ObserveAfterFrame
                        && observation.Frame >= observation.AttachmentFrame + plan.MinimumAttachmentTicks)
                    { result.Outcome = TrialOutcome.Frontier; result.Reason = "Reached an attachment through gameplay."; break; }
                    if (!observation.Player)
                    { result.Outcome = TrialOutcome.Diverged; result.Reason = "Player disappeared without a session terminal state."; break; }
                    yield return null;
                }
                observation.Result.SaveWrites = progress.Writes;
                observation.Result.DeliveredJumps = input.Delivered;
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
