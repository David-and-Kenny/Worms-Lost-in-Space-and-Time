using UnityEngine;

namespace Worms.Monetization
{
    [CreateAssetMenu(menuName = "Worms/Monetization settings")]
    public sealed class MonetizationSettings : ScriptableObject
    {
        [Tooltip("Disabled until account, audience, privacy, store products and device checks are complete.")]
        public bool ProductionEnabled;
        [Tooltip("Explicit preview only. Editor/development builds always use Google sample IDs.")]
        public bool PreviewTestAds;
        public bool StorePurchasesEnabled;
        public bool AudienceAndPrivacyReviewed;
        public string AndroidAppId = "";
        public string IosAppId = "";
        public string AndroidInterstitialId = "";
        public string IosInterstitialId = "";
        public string PrivacyPolicyUrl = "";
        public string RemoveAdsProductId = "remove_ads";
        [Tooltip("Google Play public licensing key, not an account credential.")]
        public string GooglePlayLicenseKey = "";
        public int CompletionsPerAd = 3;
        public int FailuresPerAd = 25;
        public float OpeningGraceSeconds = 300;
        public float MinimumIntervalSeconds = 180;

        public bool DeveloperBuild => Application.isEditor || Debug.isDebugBuild;
        public bool AdsEnabled => DeveloperBuild ? PreviewTestAds : ProductionEnabled && AudienceAndPrivacyReviewed;
        public string InterstitialId => DeveloperBuild
            ? Application.platform == RuntimePlatform.IPhonePlayer
                ? "ca-app-pub-3940256099942544/4411468910" : "ca-app-pub-3940256099942544/1033173712"
            : Application.platform == RuntimePlatform.IPhonePlayer ? IosInterstitialId : AndroidInterstitialId;
    }
}
