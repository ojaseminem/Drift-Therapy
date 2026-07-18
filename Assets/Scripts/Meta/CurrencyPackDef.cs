using UnityEngine;

namespace DriftTherapy
{
    /// <summary>Which wallet a <see cref="CurrencyPackDef"/> tops up.</summary>
    public enum CurrencyType { Coins, Gems }

    /// <summary>
    /// One purchasable currency pack shown in the shop (Drift Coins or Gems).
    /// <see cref="productId"/> must exactly match a consumable product
    /// configured in the store console (Google Play / App Store) — IAPService
    /// registers every entry in <see cref="GameApp.CurrencyPacks"/> as a
    /// consumable product at Init(), so adding a new pack here + in the store
    /// console is the whole integration; no code changes needed.
    ///
    /// Create via: Assets → Create → Drift Therapy → Currency Pack
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyPack", menuName = "Drift Therapy/Currency Pack")]
    public class CurrencyPackDef : ScriptableObject
    {
        [Tooltip("Stable id, unique across the catalog. Keep unchanging.")]
        public string id = "coins_small";

        [Tooltip("Must exactly match the consumable product ID configured in the Google Play Console / App Store Connect.")]
        public string productId = "coins_small";

        public string displayName = "Small Drift Coin Pack";
        public CurrencyType currencyType = CurrencyType.Coins;

        [Tooltip("How much of the currency this pack grants on purchase.")]
        public int amount = 1000;

        [Tooltip("Cosmetic-only bonus badge on the card (e.g. \"+10% BONUS\"). 0 = no badge.")]
        [Range(0, 100)] public int bonusPercent = 0;

        [Tooltip("Shown until the real store responds with a localized price (Editor Fake Store, slow network, or store not yet initialized). Keep roughly matching the real store listing.")]
        public string fallbackPriceText = "$0.99";
    }
}
