using System;

namespace Worms.Core
{
    /// <summary>Excludes inactive intervals and the first frame across a lifecycle boundary.</summary>
    public sealed class ForegroundClock
    {
        private bool previousFrameActive;
        public double Seconds { get; private set; }

        public void Tick(double deltaSeconds, bool active)
        {
            if (double.IsNaN(deltaSeconds) || double.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (active && previousFrameActive) Seconds += deltaSeconds;
            previousFrameActive = active;
        }

        public void Interrupt() => previousFrameActive = false;
    }
}
