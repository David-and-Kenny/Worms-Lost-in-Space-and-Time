using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using Worms.Core;

namespace Worms.Monetization
{
    public sealed class AdMobInterstitialAds : IInterstitialAds
    {
        private readonly string adUnitId;
        private InterstitialAd ad;
        private bool initialized;
        private bool initializing;
        private bool loading;
        private bool disposed;
        private int generation;
        private double retryAt;
        private double retryDelay = 5;
        private double loadedAt;
        private double now;
        public bool ConsentAllowsAds { get; private set; }
        public bool PrivacyOptionsRequired => ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
        public bool IsReady => !disposed && ConsentAllowsAds && ad != null && ad.CanShowAd() && Time.realtimeSinceStartupAsDouble - loadedAt < 3500;
        public event Action Changed;

        public AdMobInterstitialAds(string adUnitId) { this.adUnitId = adUnitId; }

        public void Start()
        {
            // No age gate: conservatively apply child protections to every ad request.
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                AgeRestrictedTreatment = AgeRestrictedTreatment.Child,
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.True,
                TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.True,
                MaxAdContentRating = MaxAdContentRating.G,
                PublisherFirstPartyIdEnabled = false
            });
            ConsentInformation.Update(new ConsentRequestParameters { TagForUnderAgeOfConsent = true }, error => MainThread(() =>
            {
                if (disposed) return;
                if (error != null) { UpdateConsent(); return; }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => MainThread(UpdateConsent));
            }));
        }

        private static void MainThread(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);

        private void UpdateConsent()
        {
            if (disposed) return;
            ConsentAllowsAds = ConsentInformation.CanRequestAds();
            Changed?.Invoke();
            if (!ConsentAllowsAds) { ClearAd(); return; }
            if (initialized || initializing) return;
            initializing = true;
            MobileAds.Initialize(status => MainThread(() =>
            {
                initializing = false;
                if (disposed || status == null) return;
                initialized = true;
                Load();
            }));
        }

        public void Tick(double foregroundSeconds)
        {
            now = foregroundSeconds;
            if (ad != null && Time.realtimeSinceStartupAsDouble - loadedAt >= 3500) ClearAd();
            if (initialized && ConsentAllowsAds && ad == null && !loading && now >= retryAt) Load();
        }

        private void Load()
        {
            if (disposed || loading || !ConsentAllowsAds) return;
            loading = true;
            int requestGeneration = generation;
            InterstitialAd.Load(adUnitId, new AdRequest(), (loaded, error) => MainThread(() =>
            {
                if (disposed || requestGeneration != generation) { loaded?.Destroy(); return; }
                loading = false;
                if (error != null || loaded == null)
                {
                    loaded?.Destroy();
                    retryAt = now + retryDelay;
                    retryDelay = Math.Min(120, retryDelay * 2);
                    return;
                }
                ad = loaded;
                loadedAt = Time.realtimeSinceStartupAsDouble;
                retryDelay = 5;
            }));
        }

        public void Show(Action opened, Action finished)
        {
            if (!IsReady) { finished(); return; }
            var showing = ad;
            ad = null;
            bool ended = false;
            Action finish = () =>
            {
                if (ended) return;
                ended = true;
                showing.Destroy();
                retryAt = now + 1;
                finished();
            };
            showing.OnAdFullScreenContentOpened += () => MainThread(() => { if (!ended) opened(); });
            showing.OnAdFullScreenContentClosed += () => MainThread(finish);
            showing.OnAdFullScreenContentFailed += error => MainThread(finish);
            try { showing.Show(); }
            catch { finish(); }
        }

        public void ShowPrivacyOptions()
        {
            if (!PrivacyOptionsRequired) return;
            ConsentForm.ShowPrivacyOptionsForm(error => MainThread(UpdateConsent));
        }

        private void ClearAd()
        {
            generation++;
            loading = false;
            ad?.Destroy();
            ad = null;
        }

        public void Dispose() { disposed = true; ConsentAllowsAds = false; ClearAd(); }
    }
}
