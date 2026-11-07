using System;

namespace Worms.Core
{
    /// <summary>Counts runs, not button presses. Time is supplied by the foreground clock.</summary>
    public sealed class AdBreakPolicy
    {
        private readonly int completionLimit;
        private readonly int failureLimit;
        private readonly double graceSeconds;
        private readonly double intervalSeconds;
        private bool outcomeCounted;
        private double lastAdSeconds = double.NegativeInfinity;
        public int CompletedRuns { get; private set; }
        public int FailedRuns { get; private set; }

        public AdBreakPolicy(int completionLimit = 3, int failureLimit = 25,
            double graceSeconds = 300, double intervalSeconds = 180)
        {
            if (completionLimit < 1 || failureLimit < 1 || double.IsNaN(graceSeconds) ||
                double.IsInfinity(graceSeconds) || graceSeconds < 0 || double.IsNaN(intervalSeconds) ||
                double.IsInfinity(intervalSeconds) || intervalSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(completionLimit));
            this.completionLimit = completionLimit;
            this.failureLimit = failureLimit;
            this.graceSeconds = graceSeconds;
            this.intervalSeconds = intervalSeconds;
        }

        public void BeginRun() => outcomeCounted = false;

        public void RecordOutcome(bool completed)
        {
            if (outcomeCounted) return;
            outcomeCounted = true;
            if (completed) CompletedRuns = Math.Min(completionLimit, CompletedRuns + 1);
            else FailedRuns = Math.Min(failureLimit, FailedRuns + 1);
        }

        public bool CanShow(double foregroundSeconds, bool adsAllowed, bool ownsRemoveAds)
        {
            return adsAllowed && !ownsRemoveAds && !double.IsNaN(foregroundSeconds) &&
                !double.IsInfinity(foregroundSeconds) && foregroundSeconds >= graceSeconds &&
                foregroundSeconds - lastAdSeconds >= intervalSeconds &&
                (CompletedRuns >= completionLimit || FailedRuns >= failureLimit);
        }

        public void RecordAdOpened(double foregroundSeconds)
        {
            lastAdSeconds = foregroundSeconds;
            CompletedRuns = 0;
            FailedRuns = 0;
        }
    }
}
