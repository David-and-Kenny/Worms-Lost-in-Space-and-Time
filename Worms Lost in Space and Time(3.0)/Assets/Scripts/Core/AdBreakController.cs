using System;

namespace Worms.Core
{
    public interface IInterstitialAds : IDisposable
    {
        bool IsReady { get; }
        void Show(Action opened, Action finished);
    }

    /// <summary>Owns a single pending transition; SDK callbacks cannot navigate twice.</summary>
    public sealed class AdBreakController
    {
        private readonly AdBreakPolicy policy;
        private readonly IInterstitialAds ads;
        private readonly Func<double> clock;
        private Action pending;
        private bool opened;
        private int generation;
        public bool IsPresenting => pending != null;

        public AdBreakController(AdBreakPolicy policy, IInterstitialAds ads, Func<double> clock)
        {
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
            this.ads = ads ?? throw new ArgumentNullException(nameof(ads));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Navigate(bool eligibleTransition, bool adsAllowed, bool ownsRemoveAds, Action navigate)
        {
            if (pending != null) return;
            if (!eligibleTransition || !policy.CanShow(clock(), adsAllowed, ownsRemoveAds) || !ads.IsReady)
            {
                navigate();
                return;
            }
            pending = navigate;
            opened = false;
            int request = ++generation;
            try { ads.Show(() => Opened(request), () => Finish(request)); }
            catch { Finish(request); }
        }

        private void Opened(int request)
        {
            if (request != generation || pending == null || opened) return;
            opened = true;
            policy.RecordAdOpened(clock());
        }

        private void Finish(int request)
        {
            if (request != generation) return;
            var next = pending;
            pending = null;
            next?.Invoke();
        }

        public void Cancel() { generation++; pending = null; }
    }
}
