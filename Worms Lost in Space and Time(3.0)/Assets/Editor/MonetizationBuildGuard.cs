using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Worms.Monetization;

public sealed class MonetizationBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        var settings = AssetDatabase.LoadAssetAtPath<MonetizationSettings>(MonetizationSetup.SettingsPath);
        ValidateSettings(settings, report.summary.platform, (report.summary.options & BuildOptions.Development) != 0);
    }

    public static void ValidateSettings(MonetizationSettings settings, BuildTarget platform, bool development)
    {
        if (settings == null) throw new BuildFailedException("Create Worms monetization settings before building.");
        bool android = platform == BuildTarget.Android;
        bool ios = platform == BuildTarget.iOS;
        if ((!android && !ios) || development || !settings.ProductionEnabled) return;
        if (!settings.AudienceAndPrivacyReviewed || !settings.StorePurchasesEnabled ||
            !Uri.TryCreate(settings.PrivacyPolicyUrl, UriKind.Absolute, out var policy) || policy.Scheme != "https")
            throw new BuildFailedException("Production ads require reviewed audience/privacy, a published HTTPS privacy policy and enabled Remove Ads purchasing.");
        string appId = android ? settings.AndroidAppId : settings.IosAppId;
        string adId = android ? settings.AndroidInterstitialId : settings.IosInterstitialId;
        if (!ValidId(appId, '~') || !ValidId(adId, '/') || string.IsNullOrWhiteSpace(settings.RemoveAdsProductId))
            throw new BuildFailedException("Production monetization requires real platform app/ad-unit IDs and a store product ID.");
        if (android)
        {
            try { if (Convert.FromBase64String(settings.GooglePlayLicenseKey).Length < 128) throw new FormatException(); }
            catch { throw new BuildFailedException("Configure the Google Play public license key for purchase verification."); }
        }
        var google = AssetDatabase.LoadMainAssetAtPath(MonetizationSetup.GoogleSettingsPath);
        if (google == null) throw new BuildFailedException("Missing Google Mobile Ads SDK settings.");
        var serialized = new SerializedObject(google);
        if (serialized.FindProperty(android ? "adMobAndroidAppId" : "adMobIOSAppId").stringValue != appId)
            throw new BuildFailedException("Google Mobile Ads app ID must match Worms monetization settings for this platform.");
        if (!string.IsNullOrEmpty(serialized.FindProperty("userTrackingUsageDescription").stringValue))
            throw new BuildFailedException("The no-tracking launch configuration must not include an IDFA/ATT explainer.");
    }

    private static bool ValidId(string value, char separator) =>
        !string.IsNullOrEmpty(value) && System.Text.RegularExpressions.Regex.IsMatch(value,
            "^ca-app-pub-[0-9]{16}" + System.Text.RegularExpressions.Regex.Escape(separator.ToString()) + "[0-9]{10}$") &&
        !value.Contains("3940256099942544");
}
