using System;
using System.Collections.Generic;
using System.Linq;
using Worms.Gameplay;

namespace Worms.LevelValidation
{
    public sealed class TrialPlan
    {
        public List<JumpCommand> Jumps = new List<JumpCommand>();
        public int ObserveAfterFrame = -1;
        public bool StopAtFrontier;
        public int MinimumAttachmentTicks;
        public bool RelativeTiming;
        public int FrameLimit = -1;
        public List<ButtonHold> ButtonHolds;
    }

    /// <summary>Best-first traversal with destination-diverse arrivals and bounded transition probes.</summary>
    public sealed class RouteSearch
    {
        private sealed class Frontier
        {
            public TrialPlan Prefix;
            public TrialResult State;
            public LaunchSchedule Schedule;
            public int Next;
            public double Priority;
            public string DiversityCell;
        }
        private readonly LevelValidationRequest options;
        private readonly LevelManifest manifest;
        private readonly RouteGeometry geometry;
        private readonly List<Frontier> frontiers = new List<Frontier>();
        private readonly HashSet<string> scheduled = new HashSet<string>();
        private readonly Queue<TrialPlan> extendedProbes = new Queue<TrialPlan>();
        private readonly Dictionary<string, int> surfaceAttempts = new Dictionary<string, int>();
        public int ActionsScheduled => scheduled.Count;
        public RouteSearch(LevelValidationRequest options, LevelManifest manifest)
        {
            this.options = options;
            this.manifest = manifest;
            geometry = new RouteGeometry(manifest);
        }

        public void Add(TrialPlan prefix, TrialResult state)
        {
            if (state.Outcome == TrialOutcome.TimedOut && prefix.FrameLimit > 0
                && state.LastFrame >= prefix.FrameLimit && prefix.FrameLimit < options.TrialSeconds * GameSimulation.TicksPerSecond)
            {
                var extended = Copy(prefix);
                extended.FrameLimit = -1;
                extendedProbes.Enqueue(extended);
            }
            if (state.Outcome != TrialOutcome.Frontier || prefix.Jumps.Count >= options.MaximumJumps) return;
            int horizon = (int)Math.Ceiling(options.TrialSeconds * GameSimulation.TicksPerSecond) - state.LastFrame;
            if (state.SafeWaitSeconds >= 0)
                horizon = Math.Min(horizon, (int)Math.Ceiling(state.SafeWaitSeconds * GameSimulation.TicksPerSecond) + 2);
            if (horizon < 0) return;
            var surface = manifest.Objects.FirstOrDefault(item => item.Id == state.SurfaceId);
            var schedule = new LaunchSchedule(state, surface, geometry, horizon);
            int visits = prefix.Jumps.Count(jump => jump.SurfaceId == state.SurfaceId);
            frontiers.Add(new Frontier { Prefix = Copy(prefix), State = state, Schedule = schedule,
                DiversityCell = LandingDiversity.Cell(state, surface),
                Priority = geometry.ExitCost(null, state.X, state.Y)
                    + geometry.ExitCost(state.SurfaceId, state.X, state.Y) * .1
                    + prefix.Jumps.Count * .5 + visits * 15 });

            // Retain alternative platforms instead of filling the beam with arrivals on one wheel.
            int perSurface = Math.Max(8, options.BeamWidth / 2);
            var crowded = frontiers.Where(item => item.State.SurfaceId == state.SurfaceId).ToArray();
            if (crowded.Length > perSurface) RemoveCrowdedArrival(crowded);
            if (frontiers.Count > options.BeamWidth)
                RemoveCrowdedArrival(frontiers);
        }

        private void RemoveCrowdedArrival(IEnumerable<Frontier> arrivals)
        {
            var group = arrivals.GroupBy(item => item.DiversityCell).OrderByDescending(item => item.Count()).First();
            var candidates = group.Count() > 1 ? group.AsEnumerable() : arrivals;
            frontiers.Remove(candidates.OrderByDescending(item => item.Priority + item.Next * .2).First());
        }

        public TrialPlan Next()
        {
            if (extendedProbes.Count > 0 && (scheduled.Count % 16 == 15 || frontiers.Count == 0))
                return extendedProbes.Dequeue();
            while (frontiers.Count > 0)
            {
                // Fresh arrivals must not reset a platform's search effort and starve later platforms.
                var source = frontiers.OrderBy(item => item.Priority + item.Next * .05
                    + AttemptsOn(item.State.SurfaceId) * .1).First();
                if (source.Next == source.Schedule.Delays.Count) { frontiers.Remove(source); continue; }
                int delay = source.Schedule.Delays[source.Next++];
                var plan = Copy(source.Prefix);
                plan.ObserveAfterFrame = source.State.LastFrame;
                plan.StopAtFrontier = true;
                var surface = manifest.Objects.FirstOrDefault(item => item.Id == source.State.SurfaceId);
                if (delay >= 0)
                {
                    int frame = Math.Max(source.State.AttachmentFrame + 1, source.State.LastFrame + delay);
                    plan.Jumps.Add(new JumpCommand { Frame = frame, SurfaceId = source.State.SurfaceId,
                        AttachmentOrdinal = source.State.AttachmentOrdinal,
                        DelaySeconds = (frame - source.State.AttachmentFrame) * GameSimulation.StepSeconds });
                    plan.FrameLimit = frame + geometry.FlightTicks;
                }
                else
                {
                    int passiveTicks = Math.Max(source.Schedule.RotationTicks * 2,
                        (int)Math.Ceiling((surface?.BreakSeconds ?? 0) * GameSimulation.TicksPerSecond) + geometry.FlightTicks);
                    plan.FrameLimit = source.State.LastFrame + passiveTicks;
                    if (delay == -2) plan.FrameLimit = -1;
                }
                string key = string.Join(",", plan.Jumps.Select(item => item.Frame)) + ":" + plan.ObserveAfterFrame + ":" + plan.FrameLimit;
                if (scheduled.Add(key))
                {
                    surfaceAttempts[source.State.SurfaceId] = AttemptsOn(source.State.SurfaceId) + 1;
                    return plan;
                }
            }
            return extendedProbes.Count > 0 ? extendedProbes.Dequeue() : null;
        }

        private int AttemptsOn(string surfaceId) => surfaceAttempts.TryGetValue(surfaceId, out int count) ? count : 0;

        public static TrialPlan Copy(TrialPlan plan) => new TrialPlan { ObserveAfterFrame = plan.ObserveAfterFrame,
            StopAtFrontier = plan.StopAtFrontier, MinimumAttachmentTicks = plan.MinimumAttachmentTicks,
            RelativeTiming = plan.RelativeTiming, FrameLimit = plan.FrameLimit,
            ButtonHolds = plan.ButtonHolds?.Select(item => new ButtonHold { FirstJump = item.FirstJump,
                LastJump = item.LastJump, StartFrame = item.StartFrame, EndFrame = item.EndFrame, Preheld = item.Preheld }).ToList(),
            Jumps = plan.Jumps.Select(item => new JumpCommand { Frame = item.Frame, SurfaceId = item.SurfaceId,
                AttachmentOrdinal = item.AttachmentOrdinal, DelaySeconds = item.DelaySeconds }).ToList() };
    }

    public static class ReplayComparison
    {
        public static bool Equivalent(TrialResult expected, TrialResult actual, out string reason)
        {
            reason = null;
            if (expected.Outcome != actual.Outcome || expected.Events.Count != actual.Events.Count)
                reason = "Terminal outcome or event count changed.";
            else
                for (int index = 0; index < expected.Events.Count; index++)
                {
                    var left = expected.Events[index]; var right = actual.Events[index];
                    float angle = Math.Abs(left.Angle - right.Angle) % 360f;
                    angle = Math.Min(angle, 360f - angle);
                    if (left.Frame != right.Frame || left.Kind != right.Kind || left.SurfaceId != right.SurfaceId || left.AttachmentOrdinal != right.AttachmentOrdinal)
                    { reason = "Traversal transition changed at event " + index + "."; break; }
                    // Collision callbacks expose a transient solver pose, retained for diagnostics.
                    // Stable attachment poses are checked separately after following has run.
                    if (left.Kind == "Attached") continue;
                    // Finish callbacks can expose contact correction within the pinned physics contact offset.
                    // Earlier decision poses, event timing and all velocities retain their tighter checks.
                    float positionTolerance = left.Kind == "Completed" ? .01f : .001f;
                    if (Math.Abs(left.X - right.X) > positionTolerance || Math.Abs(left.Y - right.Y) > positionTolerance
                        || Math.Abs(left.VelocityX - right.VelocityX) > .001f || Math.Abs(left.VelocityY - right.VelocityY) > .001f
                        || Math.Abs(left.Oxygen - right.Oxygen) > .01f || angle > .1f)
                    { reason = "Traversal diverged at event " + index + "."; break; }
                }
            return reason == null;
        }
    }
}
