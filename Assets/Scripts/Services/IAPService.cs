using UnityEngine;
using UnityEngine.Purchasing;

namespace DriftTherapy
{
    /// <summary>Seam for the remove-ads entitlement purchase.</summary>
    public interface IIAPService
    {
        bool IsInitialized { get; }
        bool RemoveAdsOwned { get; }
        void Init();
        void PurchaseRemoveAds();
        void RestorePurchases();
    }

    /// <summary>
    /// Real Unity IAP-backed implementation (com.unity.purchasing is already
    /// installed and configured for Google Play — see
    /// Assets/Resources/BillingMode.json). Single non-consumable product,
    /// "remove_ads", defined in code — no separate IAP Catalog asset needed.
    ///
    /// Entitlement persists via GameApp.Data.removeAdsOwned. The legacy
    /// SaveService.dt_remove_ads key is not written by this class — see
    /// GameApp's save-system note (new state goes in GameApp.Data only).
    /// </summary>
    public class IAPService : IStoreListener, IIAPService
    {
        const string RemoveAdsProductId = "remove_ads";

        IStoreController storeController;
        IExtensionProvider extensionProvider;

        public bool IsInitialized => storeController != null && extensionProvider != null;
        public bool RemoveAdsOwned => GameApp.Instance != null && GameApp.Instance.Data.removeAdsOwned;

        public void Init()
        {
            if (IsInitialized) return;

            var module = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);
            builder.AddProduct(RemoveAdsProductId, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void PurchaseRemoveAds()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[IAP] Not initialized yet — call Init() first.");
                return;
            }

            if (RemoveAdsOwned) return;

            var product = storeController.products.WithID(RemoveAdsProductId);
            if (product != null && product.availableToPurchase)
            {
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogWarning("[IAP] remove_ads product not available to purchase.");
            }
        }

        /// <summary>
        /// Android (Google Play) restores non-consumable ownership automatically
        /// via ProcessPurchase during Init — nothing further is needed there.
        /// This only matters for iOS, where restore is a distinct user action.
        /// </summary>
        public void RestorePurchases()
        {
            if (!IsInitialized) return;

            var apple = extensionProvider.GetExtension<IAppleExtensions>();
            apple?.RestoreTransactions((success, message) => Debug.Log("[IAP] Restore result: " + success + " " + message));
        }

        // ── IStoreListener ───────────────────────────────────────────────────
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            extensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning("[IAP] Initialize failed: " + error);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning("[IAP] Initialize failed: " + error + " — " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            if (purchaseEvent.purchasedProduct.definition.id == RemoveAdsProductId)
            {
                GameApp.Instance?.SetRemoveAdsOwned(true);
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogWarning("[IAP] Purchase failed: " + (product != null ? product.definition.id : "?") + " — " + reason);
        }
    }
}
