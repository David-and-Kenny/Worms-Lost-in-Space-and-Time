using System;
using UnityEngine;
using UnityEngine.UI;
using Worms.Monetization;

namespace Worms.UI
{
    public sealed class MonetizationSettingsView : MonoBehaviour
    {
        public GameObject Panel;
        public Button Open;
        public Button Close;
        public Button Buy;
        public Button Restore;
        public Button Privacy;
        public Button Policy;
        public Text PurchaseLabel;
        public Text Status;
        private MonetizationHost host;

        private void Awake()
        {
            Open.onClick.AddListener(Show);
            Close.onClick.AddListener(Hide);
            Buy.onClick.AddListener(() => host?.Buy());
            Restore.onClick.AddListener(() => host?.RestorePurchases());
            Privacy.onClick.AddListener(() => host?.ShowPrivacyOptions());
            Policy.onClick.AddListener(OpenPolicy);
        }
        private void Start()
        {
            host = MonetizationHost.GetOrCreate();
            if (host != null) host.Changed += Refresh;
            Refresh();
        }
        private void Show() { Panel.SetActive(true); host?.SetSettingsOpen(true); Refresh(); }
        private void Hide() { Panel.SetActive(false); host?.SetSettingsOpen(false); }
        private void Refresh()
        {
            PurchaseLabel.text = host?.Owned == true ? "Ads removed" : string.IsNullOrEmpty(host?.Price)
                ? "Remove Ads — store unavailable" : "Remove Ads — " + host.Price;
            Buy.interactable = host?.CanBuy == true;
            Restore.interactable = host?.CanRestore == true;
            Privacy.gameObject.SetActive(host?.PrivacyOptionsRequired == true);
            Policy.interactable = ValidPolicy(host?.PrivacyPolicyUrl);
            Status.text = host?.Status ?? "Monetization is disabled in automated tests.";
        }
        private static bool ValidPolicy(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https";
        private void OpenPolicy()
        {
            if (ValidPolicy(host?.PrivacyPolicyUrl)) Application.OpenURL(host.PrivacyPolicyUrl);
        }
        private void OnDisable() { if (Panel != null) Panel.SetActive(false); host?.SetSettingsOpen(false); }
        private void OnDestroy() { if (host != null) host.Changed -= Refresh; }
    }
}
