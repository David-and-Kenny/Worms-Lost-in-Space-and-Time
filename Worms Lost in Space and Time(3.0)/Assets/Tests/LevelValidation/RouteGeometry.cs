using System;
using System.Collections.Generic;
using System.Linq;
using Worms.Gameplay;

namespace Worms.LevelValidation
{
    /// <summary>Geometry orders trials; only the real game can establish a successful transition.</summary>
    public sealed class RouteGeometry
    {
        private readonly SceneObjectIdentity[] targets;
        private readonly SceneObjectIdentity[] hazards;
        private readonly Dictionary<string, double> remaining = new Dictionary<string, double>();
        private readonly double playerRadius;
        private readonly double minX, maxX, minY, maxY;
        public IReadOnlyList<SceneObjectIdentity> Targets => targets;
        public int FlightTicks { get; }

        public RouteGeometry(LevelManifest manifest)
        {
            targets = manifest.Objects.Where(item => item.Role == "wheel" || item.Role == "conveyor" || item.Role == "finish").ToArray();
            hazards = manifest.Objects.Where(item => (item.Role == "hazard" || item.Role == "obstacle") && !item.Moving).ToArray();
            if (manifest.Objects.Count > 0)
            {
                minX = manifest.Objects.Min(item => item.X - Math.Max(1, item.SizeX)) - 5;
                maxX = manifest.Objects.Max(item => item.X + Math.Max(1, item.SizeX)) + 5;
                minY = manifest.Objects.Min(item => item.Y - Math.Max(1, item.SizeY)) - 5;
                maxY = manifest.Objects.Max(item => item.Y + Math.Max(1, item.SizeY)) + 5;
            }
            var spawn = manifest.Objects.FirstOrDefault(item => item.Role == "spawn");
            playerRadius = spawn == null ? .25 : Math.Max(.1, Math.Max(spawn.SizeX, spawn.SizeY) * .5);
            double width = targets.Length == 0 ? 30 : targets.Max(item => item.X) - targets.Min(item => item.X);
            double height = targets.Length == 0 ? 30 : targets.Max(item => item.Y) - targets.Min(item => item.Y);
            // Search probes have a useful flight horizon; full certification retains the request's full limit.
            FlightTicks = (int)Math.Ceiling((Math.Sqrt(width * width + height * height) / 10 + 2) * GameSimulation.TicksPerSecond);
            BuildExitCosts();
        }

        private void BuildExitCosts()
        {
            foreach (var target in targets) remaining[target.Id] = target.Role == "finish" ? 0 : double.PositiveInfinity;
            var pending = new HashSet<SceneObjectIdentity>(targets);
            while (pending.Count > 0)
            {
                var next = pending.OrderBy(item => remaining[item.Id]).First();
                pending.Remove(next);
                if (double.IsPositiveInfinity(remaining[next.Id])) break;
                foreach (var source in pending)
                {
                    double distance = Distance(source.X, source.Y, next.X, next.Y);
                    double travel = distance + Obstructions(source.X, source.Y, next.X, next.Y) * 40;
                    remaining[source.Id] = Math.Min(remaining[source.Id], travel + remaining[next.Id]);
                }
            }
        }

        public double ExitCost(string surfaceId, double x, double y)
        {
            if (surfaceId != null && remaining.TryGetValue(surfaceId, out double cost) && !double.IsInfinity(cost)) return cost;
            var exits = targets.Where(item => item.Role == "finish").ToArray();
            return exits.Length == 0 ? 0 : exits.Min(item => Distance(x, y, item.X, item.Y));
        }

        public bool OutsideSearchEnvelope(TrialResult state)
        {
            if (targets.Length == 0) return false;
            return (state.X < minX && state.VelocityX <= 0) || (state.X > maxX && state.VelocityX >= 0)
                || (state.Y < minY && state.VelocityY <= 0) || (state.Y > maxY && state.VelocityY >= 0);
        }

        public double AimScore(TrialResult state, SceneObjectIdentity source, SceneObjectIdentity target, int delay)
        {
            double seconds = delay / (double)GameSimulation.TicksPerSecond;
            double turn = source.AngularSpeed * seconds * Math.PI / 180;
            double dx = state.X - source.X, dy = state.Y - source.Y;
            double x = Math.Abs(source.AngularSpeed) > .001
                ? source.X + dx * Math.Cos(turn) - dy * Math.Sin(turn) : state.X + state.VelocityX * seconds;
            double y = Math.Abs(source.AngularSpeed) > .001
                ? source.Y + dx * Math.Sin(turn) + dy * Math.Cos(turn) : state.Y + state.VelocityY * seconds;
            double centerX = target.SizeX > 0 ? target.CenterX : target.X;
            double centerY = target.SizeY > 0 ? target.CenterY : target.Y;
            double bearing = Math.Atan2(centerY - y, centerX - x);
            double heading = (state.Angle + 90) * Math.PI / 180 + turn;
            double error = Math.Abs(Math.Atan2(Math.Sin(bearing - heading), Math.Cos(bearing - heading)));
            double radius = Math.Max(.25, Math.Max(target.SizeX, target.SizeY) * .5) + playerRadius;
            double distance = Distance(x, y, centerX, centerY);
            double aperture = Math.Asin(Math.Min(1, radius / Math.Max(radius, distance)));
            double blocked = error < aperture ? Obstructions(x, y, centerX, centerY) * 50 : 0;
            return error * 180 / Math.PI + blocked + seconds * .05;
        }

        private int Obstructions(double x, double y, double targetX, double targetY)
        {
            int count = 0;
            double dx = targetX - x, dy = targetY - y;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared < .000001) return 0;
            foreach (var hazard in hazards)
            {
                double hx = hazard.SizeX > 0 ? hazard.CenterX : hazard.X;
                double hy = hazard.SizeY > 0 ? hazard.CenterY : hazard.Y;
                if (hazard.Circle)
                {
                    double along = Math.Max(0, Math.Min(1, ((hx - x) * dx + (hy - y) * dy) / lengthSquared));
                    double radius = Math.Max(.1, Math.Max(hazard.SizeX, hazard.SizeY) * .5) + playerRadius;
                    if (Distance(x + along * dx, y + along * dy, hx, hy) < radius) count++;
                }
                else
                {
                    double entry = 0, exit = 1;
                    double halfX = Math.Max(.1, hazard.SizeX * .5) + playerRadius;
                    double halfY = Math.Max(.1, hazard.SizeY * .5) + playerRadius;
                    if (IntersectAxis(x, dx, hx - halfX, hx + halfX, ref entry, ref exit)
                        && IntersectAxis(y, dy, hy - halfY, hy + halfY, ref entry, ref exit)) count++;
                }
            }
            return count;
        }

        private static bool IntersectAxis(double start, double direction, double minimum, double maximum, ref double entry, ref double exit)
        {
            if (Math.Abs(direction) < .000001) return start >= minimum && start <= maximum;
            double first = (minimum - start) / direction, last = (maximum - start) / direction;
            entry = Math.Max(entry, Math.Min(first, last));
            exit = Math.Min(exit, Math.Max(first, last));
            return entry <= exit;
        }

        private static double Distance(double x, double y, double otherX, double otherY)
        {
            double dx = otherX - x, dy = otherY - y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
