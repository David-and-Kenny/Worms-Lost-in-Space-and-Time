using System;
using System.Collections.Generic;
using System.Linq;
using Worms.Gameplay;

namespace Worms.LevelValidation
{
    /// <summary>Shares early trials among possible destinations, then covers the complete tick grid.</summary>
    public sealed class LaunchSchedule
    {
        public List<int> Delays { get; } = new List<int> { -1 };
        public int RotationTicks { get; }

        public LaunchSchedule(TrialResult state, SceneObjectIdentity source, RouteGeometry geometry, int horizon)
        {
            RotationTicks = source != null && Math.Abs(source.AngularSpeed) > .001
                ? (int)Math.Ceiling(360 / Math.Abs(source.AngularSpeed) * GameSimulation.TicksPerSecond)
                : Math.Min(horizon, 5 * GameSimulation.TicksPerSecond);
            int firstCycle = Math.Min(horizon, Math.Max(1, RotationTicks));
            var preferred = new List<int>();
            if (source != null)
            {
                var choices = geometry.Targets.Where(target => target.Id != source.Id)
                    .OrderBy(target => geometry.ExitCost(target.Id, target.X, target.Y))
                    .Select(target => Enumerable.Range(0, firstCycle + 1)
                        .OrderBy(delay => geometry.AimScore(state, source, target, delay)).Take(16).ToArray()).ToArray();
                for (int rank = 0; rank < 16; rank++)
                    foreach (var target in choices)
                        if (rank < target.Length) preferred.Add(target[rank]);
            }

            var fallback = new List<int>();
            for (int start = 0; start <= horizon; start += Math.Max(1, firstCycle + 1))
            {
                int end = Math.Min(horizon, start + firstCycle);
                for (int delay = start; delay <= end; delay += 3) fallback.Add(delay);
                for (int delay = start; delay <= end; delay++) if ((delay - start) % 3 != 0) fallback.Add(delay);
            }
            var used = new HashSet<int>();
            int hint = 0, baseline = 0;
            while (hint < preferred.Count || baseline < fallback.Count)
            {
                for (int count = 0; count < 8 && hint < preferred.Count; count++)
                {
                    int delay = preferred[hint++];
                    if (used.Add(delay)) Delays.Add(delay);
                }
                if (baseline < fallback.Count)
                {
                    int delay = fallback[baseline++];
                    if (used.Add(delay)) Delays.Add(delay);
                }
            }
            // Keep long passive/forced-detach routes available without paying their full cost first.
            Delays.Insert(Math.Min(33, Delays.Count), -2);
        }
    }
}
