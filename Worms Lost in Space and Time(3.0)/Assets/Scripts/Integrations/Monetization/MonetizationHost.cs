using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Worms.Core;
using Worms.Navigation;

namespace Worms.Monetization
{
    /// <summary>Application lifetime for platform SDKs. Domain objects never retain scene objects.</summary>
    public sealed class MonetizationHost : MonoBehaviour
    {
        private static MonetizationHost current;
        private MonetizationSettings settings;
        private RemoveAdsEntitlement entitlement;
        private UnityRemoveAdsPurchases purchases;
        private AdMobInterstitialAds ads;
        private AdBreakPolicy policy;
        private AdBreakController breaks;
        private LevelSession countedSession;
        private bool paused;
        private bool focused = true;
        private bool privacyOpen;
        private readonly ForegroundClock clock = new ForegroundClock();
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool presentationSuspended;
        public static bool BlocksInput => current != null && (current.privacyOpen || current.breaks?.IsPresenting == true);
        public bool Owned => entitlement?.Owned == true;
        public string Price => purchases?.LocalizedPrice ?? "";
        public string Status => purchases?.Status ?? (Owned ? "Ads removed." : "Purchases will be available when store setup is complete.");
        public bool CanBuy => purchases?.CanBuy == true;
        public bool CanRestore => purchases != null && !purchases.IsBusy;
        public bool PrivacyOptionsRequired => ads?.PrivacyOptionsRequired == true;
        public string PrivacyPolicyUrl => settings != null ? settings.PrivacyPolicyUrl : "";
        public event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { current = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { GetOrCreate(); }

        public static MonetizationHost GetOrCreate()
        {
            // Automated runners must never load a developer's purchase cache or call real SDKs.
            if (Application.isBatchMode) return null;
            if (current != null) return current;
            return new GameObject("Monetization").AddComponent<MonetizationHost>();
        }

        private void Awake()
        {
            if (current != null && current != this) { Destroy(gameObject); return; }
            current = this;
            DontDestroyOnLoad(gameObject);
            settings = Resources.Load<MonetizationSettings>("Configuration/MonetizationSettings");
            if (settings == null) { Debug.LogError("Missing MonetizationSettings; ads and purchases are disabled."); return; }
            policy = new AdBreakPolicy(settings.CompletionsPerAd, settings.FailuresPerAd,
                settings.OpeningGraceSeconds, settings.MinimumIntervalSeconds);
            entitlement = new RemoveAdsEntitlement(new EntitlementFileStorage(Application.persistentDataPath));
            if (settings.StorePurchasesEnabled && !Application.isEditor &&
                (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer))
            {
                purchases = new UnityRemoveAdsPurchases(settings, entitlement);
                purchases.Changed += PurchasesChanged;
                purchases.Start();
            }
            if (settings.AdsEnabled && !Owned) StartAds();
            SceneManager.activeSceneChanged += SceneChanged;
        }

        private void StartAds()
        {
            if (ads != null || settings == null || !settings.AdsEnabled || Owned) return;
            // Production waits for ownership resolution. Test preview can run without store products.
            if (!settings.DeveloperBuild && !entitlement.IsResolved) return;
            ads = new AdMobInterstitialAds(settings.InterstitialId);
            ads.Changed += Notify;
            breaks = new AdBreakController(policy, ads, () => clock.Seconds);
            ads.Start();
        }

        private void PurchasesChanged()
        {
            if (Owned && ads != null)
            {
                // Do not interrupt an already-open native ad; its close resumes the existing transition.
                ads.Dispose();
            }
            else
            {
                if (ads != null && !ads.ConsentAllowsAds && !breaks.IsPresenting)
                {
                    ads.Dispose();
                    ads = null;
                    breaks = null;
                }
                StartAds();
            }
            Notify();
        }

        private void Notify() { Changed?.Invoke(); }

        private void Update()
        {
            clock.Tick(Time.unscaledDeltaTime, !paused && focused && !BlocksInput);
            if (!paused && focused && !Owned) ads?.Tick(clock.Seconds);
        }
        private void OnApplicationPause(bool value) { paused = value; clock.Interrupt(); }
        private void OnApplicationFocus(bool value) { focused = value; clock.Interrupt(); }
        private void SceneChanged(Scene previous, Scene next)
        {
            breaks?.Cancel();
            RestorePresentation();
        }

        public void BeforeNavigate(string destination, bool restarting, Action navigate)
        {
            var session = LevelSessionHost.Current?.Session;
            if (settings == null || session == null || policy == null) { navigate(); return; }
            if (!ReferenceEquals(countedSession, session))
            {
                countedSession = session;
                policy.BeginRun();
            }
            bool completed = session.State == SessionState.Completed;
            bool failed = session.State == SessionState.Dead || restarting;
            if (completed || failed) policy.RecordOutcome(completed);
            bool eligible = completed || (failed && restarting);
            bool allowed = !paused && focused && settings.AdsEnabled && ads?.ConsentAllowsAds == true &&
                (settings.DeveloperBuild || entitlement.IsResolved);
            if (breaks == null) { navigate(); return; }
            bool mayPresent = eligible && allowed && !Owned && ads.IsReady && policy.CanShow(clock.Seconds, true, false);
            if (mayPresent) SuspendPresentation();
            breaks.Navigate(eligible, allowed, Owned, () =>
            {
                RestorePresentation();
                GameServices.PointerGate.BlockUntilRelease();
                navigate();
            });
        }

        private void SuspendPresentation()
        {
            if (presentationSuspended) return;
            presentationSuspended = true;
            clock.Interrupt();
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            Time.timeScale = 0;
            AudioListener.pause = true;
            GameServices.PointerGate.BlockUntilRelease();
        }
        private void RestorePresentation()
        {
            if (!presentationSuspended) return;
            presentationSuspended = false;
            clock.Interrupt();
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
        }
        public void Buy() => purchases?.Buy();
        public void RestorePurchases() => purchases?.Restore();
        public void ShowPrivacyOptions() => ads?.ShowPrivacyOptions();
        public void SetSettingsOpen(bool value)
        {
            privacyOpen = value;
            clock.Interrupt();
            GameServices.PointerGate.BlockUntilRelease();
        }
        private void OnDestroy()
        {
            if (current != this) return;
            SceneManager.activeSceneChanged -= SceneChanged;
            breaks?.Cancel();
            ads?.Dispose();
            purchases?.Dispose();
            RestorePresentation();
            current = null;
        }
    }

    public sealed class MonetizationTransitionGate : ISceneTransitionGate
    {
        public void BeforeNavigate(string destination, bool restarting, Action navigate)
        {
            var host = MonetizationHost.GetOrCreate();
            if (host == null) navigate();
            else host.BeforeNavigate(destination, restarting, navigate);
        }
    }
}
