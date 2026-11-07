using System;

namespace Worms.Core
{
    public interface IEntitlementStorage
    {
        bool Load();
        void Save(bool owned);
    }

    public sealed class RemoveAdsEntitlement
    {
        private readonly IEntitlementStorage storage;
        public bool Owned { get; private set; }
        public bool IsResolved { get; private set; }
        public string Error { get; private set; }

        public RemoveAdsEntitlement(IEntitlementStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            try { Owned = storage.Load(); }
            catch (Exception error) { Error = error.Message; }
        }

        public bool ApplyVerifiedOwnership(bool owned)
        {
            // Suppress ads immediately, even if the durable write needs to be retried.
            Owned = owned;
            try
            {
                storage.Save(owned);
                IsResolved = true;
                Error = null;
                return true;
            }
            catch (Exception error)
            {
                IsResolved = false;
                Error = error.Message;
                return false;
            }
        }

        public void MarkUnresolved() => IsResolved = false;
    }
}
