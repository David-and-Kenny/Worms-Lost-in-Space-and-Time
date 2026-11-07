using System;
using Worms.Gameplay;

namespace Worms.LevelValidation
{
    /// <summary>Beam allocation hint, never an identity test or proof that two worlds are equivalent.</summary>
    public static class LandingDiversity
    {
        public static string Cell(TrialResult state, SceneObjectIdentity surface)
        {
            if (surface == null) return state.SurfaceId ?? "unknown";
            double step = Math.Abs(surface.AngularSpeed) * GameSimulation.StepSeconds;
            double phase = step > .001 ? (state.Angle % step + step) % step : state.Angle;
            double dx = state.X - surface.X, dy = state.Y - surface.Y;
            int angle = (int)Math.Round(phase / .25);
            int radius = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy) / .05);
            int time = (state.LastFrame / 25) % 8;
            return state.SurfaceId + ":" + angle + ":" + radius + ":" + time;
        }
    }
}
