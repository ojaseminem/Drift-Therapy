using UnityEngine;
using UnityEngine.Purchasing;

namespace DriftTherapy
{
    /// <summary>Seam for real-money purchases: the remove-ads entitlement and consumable currency packs.</summary>
    public interface IIAPService
    {
        bool IsInitialized { get; }
        bool RemoveAdsOwned { get; }
        void Init();
        void PurchaseRemoveAds();
        void RestorePurchases();

        /// <summary>Buys the consumable currency pack with this product id (see <see cref="CurrencyPackDef.productId"/>).</summary>
        void PurchaseCurrencyPack(string productId);

        /// <summary>Real store-localized price string (e.g. "$4.99"), or null/empty if not yet available — callers should fall back to CurrencyPackDef.fallbackPriceText.</summary>
        string GetLocalizedPrice(string productId);
    }

    /// <summary>
    /// Real Unity IAP-backed implementation (com.unity.purchasing is already
    /// installed and configured for Google Play — see
    /// Assets/Resources/BillingMode.json). Registers the "remove_ads"
    /// non-consumable plus every <see cref="CurrencyPackDef"/> in
    /// <see cref="GameApp.CurrencyPacks"/> as a consumable product.
    ///
    /// Store resolution is automatic and needs no per-platform code here:
    /// Google Play on Android, the Apple App Store on iOS, and Unity IAP's
    /// built-in Fake Store (a simulated purchase dialog, no network/store
    /// account needed) in the Editor and any standalone/desktop build where
    /// no real store is configured. That Fake Store is what "desktop
    /// simulates it" means in practice — nothing else to wire up for it.
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

            var packs = GameApp.Instance != null ? GameApp.Instance.CurrencyPacks : null;
            if (packs != null)
                foreach (var pack in packs)
                    if (pack != null && !string.IsNullOrEmpty(pack.productId))
                        builder.AddProduct(pack.productId, ProductType.Consumable);

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
            InitiatePurchase(RemoveAdsProductId);
        }

        public void PurchaseCurrencyPack(string productId)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[IAP] Not initialized yet — call Init() first.");
                return;
            }

            InitiatePurchase(productId);
        }

        void InitiatePurchase(string productId)
        {
            var product = storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogWarning("[IAP] Product not available to purchase: " + productId);
            }
        }

        public string GetLocalizedPrice(string productId)
        {
            if (!IsInitialized) return null;
            var product = storeController.products.WithID(productId);
            return product != null ? product.metadata.localizedPriceString : null;
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
            string id = purchaseEvent.purchasedProduct.definition.id;

            if (id == RemoveAdsProductId)
            {
                GameApp.Instance?.SetRemoveAdsOwned(true);
                return PurchaseProcessingResult.Complete;
            }

            var pack = GameApp.Instance?.GetCurrencyPack(id);
            if (pack != null)
            {
                GameApp.Instance.GrantCurrencyPack(pack);
            }
            else
            {
                Debug.LogWarning("[IAP] Purchased unknown product id: " + id);
            }

            // Consumables are re-purchasable the instant this returns Complete —
            // that's what actually "consumes" it, no separate confirm call needed.
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogWarning("[IAP] Purchase failed: " + (product != null ? product.definition.id : "?") + " — " + reason);
        }
    }
}
