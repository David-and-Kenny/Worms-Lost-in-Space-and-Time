using System;
using System.Collections.Generic;
using System.Linq;

namespace Worms.LevelValidation
{
    public sealed class TrialPlan
    {
        public List<JumpCommand> Jumps = new List<JumpCommand>();
        public int ObserveAfterFrame = -1;
        public bool StopAtFrontier;
        public bool RelativeTiming;
    }

    /// <summary>Bounded best-first exploration. Each proposed trial starts at the real spawn.</summary>
    public sealed class RouteSearch
    {
        private sealed class Frontier
        {
            public TrialPlan Prefix;
            public TrialResult State;
            public List<int> Delays;
            public int Next;
            public float Priority;
        }
        private readonly LevelValidationRequest options;
        private readonly LevelManifest manifest;
        private readonly List<Frontier> frontiers = new List<Frontier>();
        private readonly HashSet<string> scheduled = new HashSet<string>();
        public RouteSearch(LevelValidationRequest options, LevelManifest manifest)
        {
            this.options = options;
            this.manifest = manifest;
        }

        public void Add(TrialPlan prefix, TrialResult state)
        {
            if (state.Outcome != TrialOutcome.Frontier || prefix.Jumps.Count >= options.MaximumJumps) return;
            int horizon = (int)Math.Ceiling(options.TrialSeconds * options.FramesPerSecond) - state.LastFrame;
            if (state.SafeWaitSeconds > 0)
                horizon = Math.Min(horizon, (int)Math.Ceiling(state.SafeWaitSeconds * options.FramesPerSecond) + 2);
            if (horizon < 0) return;
            // Coarse timing first, then every remaining frame. Narrow windows are not silently excluded.
            var delays = new List<int> { -1 };
            for (int frame = 0; frame <= horizon; frame += 3) delays.Add(frame);
            for (int frame = 0; frame <= horizon; frame++) if (frame % 3 != 0) delays.Add(frame);
            var rotating = manifest.Objects.FirstOrDefault(item => item.Id == state.SurfaceId && Math.Abs(item.AngularSpeed) > .001f);
            if (rotating != null) delays = PrioritizeAim(state, rotating, horizon);
            float distance = float.MaxValue;
            foreach (var goal in manifest.Objects.Where(item => item.Role == "finish"))
            {
                float dx = state.X - goal.X, dy = state.Y - goal.Y;
                distance = Math.Min(distance, (float)Math.Sqrt(dx * dx + dy * dy));
            }
            frontiers.Add(new Frontier { Prefix = Copy(prefix), State = state, Delays = delays,
                Priority = distance + prefix.Jumps.Count * .1f });
            if (frontiers.Count > options.BeamWidth)
                frontiers.Remove(frontiers.OrderByDescending(item => item.Priority).First());
        }

        public TrialPlan Next()
        {
            while (frontiers.Count > 0)
            {
                var source = frontiers.OrderBy(item => item.Priority + item.Next * .02f).First();
                if (source.Next == source.Delays.Count) { frontiers.Remove(source); continue; }
                int delay = source.Delays[source.Next++];
                var plan = Copy(source.Prefix);
                plan.ObserveAfterFrame = source.State.LastFrame;
                plan.StopAtFrontier = true;
                if (delay >= 0)
                    plan.Jumps.Add(new JumpCommand { Frame = source.State.LastFrame + delay,
                        SurfaceId = source.State.SurfaceId, AttachmentOrdinal = source.State.AttachmentOrdinal,
                        DelaySeconds = (source.State.LastFrame + delay - source.State.AttachmentFrame) / (float)options.FramesPerSecond });
                string key = string.Join(",", plan.Jumps.Select(item => item.Frame)) + ":" + plan.ObserveAfterFrame;
                if (scheduled.Add(key)) return plan;
            }
            return null;
        }

        private List<int> PrioritizeAim(TrialResult state, SceneObjectIdentity source, int horizon)
        {
            var targets = manifest.Objects.Where(item => item.Id != source.Id && (item.Role == "wheel" || item.Role == "conveyor" || item.Role == "finish")).ToArray();
            var coarse = Enumerable.Range(0, horizon / 3 + 1).Select(index => index * 3)
                .OrderBy(delay => AimScore(state, source, targets, delay)).ThenBy(delay => delay);
            var result = new List<int> { -1 };
            var used = new HashSet<int>();
            foreach (int delay in coarse)
                foreach (int candidate in new[] { delay, delay - 1, delay + 1, delay - 2, delay + 2 })
                    if (candidate >= 0 && candidate <= horizon && used.Add(candidate)) result.Add(candidate);
            var baseline = new List<int>();
            for (int delay = 0; delay <= horizon; delay += 3) baseline.Add(delay);
            for (int delay = 0; delay <= horizon; delay++) if (delay % 3 != 0) baseline.Add(delay);
            var mixed = new List<int> { -1 };
            used.Clear();
            for (int index = 0; index < baseline.Count; index++)
            {
                if (used.Add(baseline[index])) mixed.Add(baseline[index]);
                if (used.Add(result[index + 1])) mixed.Add(result[index + 1]);
            }
            return mixed;
        }

        private float AimScore(TrialResult state, SceneObjectIdentity source, SceneObjectIdentity[] targets, int delay)
        {
            double turn = source.AngularSpeed * delay / options.FramesPerSecond * Math.PI / 180;
            double dx = state.X - source.X, dy = state.Y - source.Y;
            double x = source.X + dx * Math.Cos(turn) - dy * Math.Sin(turn);
            double y = source.Y + dx * Math.Sin(turn) + dy * Math.Cos(turn);
            double heading = (state.Angle + 90) * Math.PI / 180 + turn;
            double best = 360;
            foreach (var target in targets)
            {
                double bearing = Math.Atan2(target.Y - y, target.X - x);
                double error = Math.Abs(Math.Atan2(Math.Sin(bearing - heading), Math.Cos(bearing - heading))) * 180 / Math.PI;
                best = Math.Min(best, error);
            }
            // This is a ranking hint, never a collision test or a reason to reject a candidate.
            return (float)(best + delay / (double)options.FramesPerSecond * .02);
        }

        public static TrialPlan Copy(TrialPlan plan) => new TrialPlan { ObserveAfterFrame = plan.ObserveAfterFrame,
            StopAtFrontier = plan.StopAtFrontier, RelativeTiming = plan.RelativeTiming,
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
                    if (Math.Abs(left.X - right.X) > .001f || Math.Abs(left.Y - right.Y) > .001f
                        || Math.Abs(left.VelocityX - right.VelocityX) > .001f || Math.Abs(left.VelocityY - right.VelocityY) > .001f
                        || Math.Abs(left.Oxygen - right.Oxygen) > .01f || angle > .1f)
                    { reason = "Traversal diverged at event " + index + "."; break; }
                }
            return reason == null;
        }
    }
}
