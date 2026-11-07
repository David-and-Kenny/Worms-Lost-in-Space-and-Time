using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using Worms.Core;

namespace Worms.Monetization
{
    public interface IRemoveAdsPurchases : IDisposable
    {
        string LocalizedPrice { get; }
        string Status { get; }
        bool CanBuy { get; }
        bool IsBusy { get; }
        event Action Changed;
        void Buy();
        void Restore();
    }

    public sealed class UnityRemoveAdsPurchases : IRemoveAdsPurchases
    {
        private readonly MonetizationSettings settings;
        private readonly RemoveAdsEntitlement entitlement;
        private StoreController store;
        private Product product;
        private bool disposed;
        public string LocalizedPrice => product?.metadata?.localizedPriceString ?? "";
        public string Status { get; private set; } = "Store is connecting...";
        public bool IsBusy { get; private set; }
        public bool CanBuy => !disposed && product != null && product.availableToPurchase &&
            entitlement.IsResolved && !entitlement.Owned && !IsBusy;
        public event Action Changed;

        public UnityRemoveAdsPurchases(MonetizationSettings settings, RemoveAdsEntitlement entitlement)
        {
            this.settings = settings;
            this.entitlement = entitlement;
        }

        public async void Start()
        {
            store = UnityIAPServices.StoreController();
            store.OnStoreConnected += Connected;
            store.OnStoreDisconnected += Disconnected;
            store.OnProductsFetched += ProductsFetched;
            store.OnProductsFetchFailed += ProductsFailed;
            store.OnPurchasesFetched += PurchasesFetched;
            store.OnPurchasesFetchFailed += PurchasesFailed;
            store.OnPurchasePending += Pending;
            store.OnPurchaseConfirmed += Confirmed;
            store.OnPurchaseFailed += Failed;
            store.OnPurchaseDeferred += Deferred;
            store.OnCheckEntitlement += Checked;
            store.OnAuthAccountChanged += AccountChanged;
            try { await store.Connect(); }
            catch { SetStatus("Store unavailable. You can keep playing and try Restore Purchases later."); }
        }

        private void SetStatus(string value)
        {
            if (disposed) return;
            Status = value;
            Changed?.Invoke();
        }

        private void Connected()
        {
            if (!disposed) store.FetchProducts(new List<ProductDefinition>
            {
                new ProductDefinition(settings.RemoveAdsProductId, ProductType.NonConsumable)
            });
        }

        private void Disconnected(StoreConnectionFailureDescription failure)
        {
            IsBusy = false;
            SetStatus("Store unavailable. Previously owned ad removal remains available offline.");
        }

        private void ProductsFetched(List<Product> products)
        {
            if (disposed) return;
            product = products.FirstOrDefault(p => p.definition.id == settings.RemoveAdsProductId);
            if (product == null) { SetStatus("Remove Ads is not available in this store yet."); return; }
            SetStatus("Checking purchases...");
            store.FetchPurchases();
        }

        private void ProductsFailed(ProductFetchFailed failure)
        {
            IsBusy = false;
            SetStatus("Store products unavailable. Try Restore Purchases later.");
        }

        private void PurchasesFetched(Orders orders)
        {
            if (!disposed && product != null) store.CheckEntitlement(product);
        }

        private void PurchasesFailed(PurchasesFetchFailureDescription failure)
        {
            IsBusy = false;
            SetStatus("Purchases could not be checked. Your offline ownership is retained.");
        }

        private bool IsOurOrder(Order order) => order?.CartOrdered?.Items()?.Any(
            item => item.Product.definition.id == settings.RemoveAdsProductId) == true;

        private bool Verify(Order order)
        {
            if (!IsOurOrder(order)) return false;
            if (Application.platform == RuntimePlatform.IPhonePlayer)
                return !string.IsNullOrEmpty(order.Info.Apple?.jwsRepresentation);
            if (Application.platform != RuntimePlatform.Android || string.IsNullOrEmpty(settings.GooglePlayLicenseKey))
                return false;
            try
            {
                var validator = new CrossPlatformValidator(Convert.FromBase64String(settings.GooglePlayLicenseKey), Application.identifier);
                return validator.Validate(order.Info.Receipt).Any(receipt => receipt.productID == settings.RemoveAdsProductId);
            }
            catch (Exception) { return false; }
        }

        private void Pending(PendingOrder order)
        {
            if (disposed || !IsOurOrder(order)) return;
            IsBusy = false;
            if (!Verify(order)) { SetStatus("Purchase could not be verified. Please try Restore Purchases."); return; }
            if (!entitlement.ApplyVerifiedOwnership(true))
            {
                SetStatus("Ads removed for this session. Purchase storage failed; restore to retry.");
                return;
            }
            SetStatus("Ads removed.");
            store.ConfirmPurchase(order);
        }

        private void Checked(Entitlement result)
        {
            if (disposed || result.Product?.definition.id != settings.RemoveAdsProductId) return;
            IsBusy = false;
            if (result.Status == EntitlementStatus.NotEntitled)
            {
                bool saved = entitlement.ApplyVerifiedOwnership(false);
                SetStatus(saved ? "One-time purchase. Removes all forced ads." : "Purchase storage unavailable. Please retry later.");
            }
            else if (result.Status == EntitlementStatus.EntitledButNotFinished && result.Order is PendingOrder pending)
                Pending(pending);
            else if (result.Status == EntitlementStatus.FullyEntitled && Verify(result.Order))
            {
                bool saved = entitlement.ApplyVerifiedOwnership(true);
                SetStatus(saved ? "Ads removed." : "Ads removed for this session. Restore to retry saving.");
            }
            else SetStatus("Ownership could not be verified. Your offline ownership is retained.");
        }

        private void Confirmed(Order order)
        {
            if (order is FailedOrder) SetStatus("Ads removed. The store will retry transaction confirmation.");
        }
        private void Failed(FailedOrder order) { IsBusy = false; SetStatus("Purchase cancelled or unsuccessful. You have not been granted ad removal."); }
        private void Deferred(DeferredOrder order) { IsBusy = false; SetStatus("Purchase is awaiting approval."); }
        private void AccountChanged()
        {
            product = null;
            entitlement.MarkUnresolved();
            SetStatus("Store account changed. Checking purchases...");
            Connected();
        }
        public void Buy()
        {
            if (!CanBuy) return;
            IsBusy = true;
            SetStatus("Waiting for the store...");
            try { store.PurchaseProduct(product); }
            catch { IsBusy = false; SetStatus("Store unavailable. Please try again."); }
        }
        public void Restore()
        {
            if (disposed || IsBusy || store == null) return;
            IsBusy = true;
            SetStatus("Restoring purchases...");
            if (product == null) { IsBusy = false; Connected(); return; }
            try
            {
                store.RestoreTransactions((success, error) =>
                {
                    if (disposed) return;
                    IsBusy = false;
                    if (success) store.FetchPurchases();
                    else SetStatus("Restore unavailable. Check your store account and connection.");
                });
            }
            catch { IsBusy = false; SetStatus("Restore unavailable. Please try again."); }
        }
        public void Dispose()
        {
            disposed = true;
            if (store == null) return;
            store.OnStoreConnected -= Connected;
            store.OnStoreDisconnected -= Disconnected;
            store.OnProductsFetched -= ProductsFetched;
            store.OnProductsFetchFailed -= ProductsFailed;
            store.OnPurchasesFetched -= PurchasesFetched;
            store.OnPurchasesFetchFailed -= PurchasesFailed;
            store.OnPurchasePending -= Pending;
            store.OnPurchaseConfirmed -= Confirmed;
            store.OnPurchaseFailed -= Failed;
            store.OnPurchaseDeferred -= Deferred;
            store.OnCheckEntitlement -= Checked;
            store.OnAuthAccountChanged -= AccountChanged;
        }
    }
}
