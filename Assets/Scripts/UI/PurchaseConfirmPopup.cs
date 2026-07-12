using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// "Buy this?" confirmation popup — reuses <see cref="Popup"/>/
    /// <see cref="PopupHandler"/> exactly as-is. Generic over what's being bought
    /// (a vehicle or a skin) via a <see cref="Func{bool}"/> spend callback, so
    /// <see cref="GarageUI"/> can drive both a vehicle purchase
    /// (<see cref="GameApp.TryBuy(VehicleDef, bool)"/>) and a skin purchase
    /// (<see cref="GameApp.TryBuySkin"/>) through the one popup.
    /// </summary>
    public class PurchaseConfirmPopup : Popup
    {
        public TMP_Text nameText, priceText;
        public Button confirmButton;

        Func<bool> pendingBuy;
        Action onConfirmed;

        protected override void Awake()
        {
            base.Awake();
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirm);
            }
        }

        /// <summary>Populates and shows the popup for a pending purchase. Call PopupHandler.Open on this popup's prefab first.</summary>
        public void Show(string itemName, int amount, bool useGems, Func<bool> buy, Action onConfirmed)
        {
            pendingBuy = buy;
            this.onConfirmed = onConfirmed;

            if (nameText) nameText.text = itemName;
            if (priceText) priceText.text = useGems ? (amount + " GEMS") : (amount + " COINS");
            if (confirmButton) confirmButton.interactable = true;
        }

        void OnConfirm()
        {
            if (pendingBuy == null) return;

            // Guard against double-tap double-charging: disable immediately, the
            // popup closes on success either way.
            if (confirmButton != null) confirmButton.interactable = false;

            bool bought = pendingBuy();
            if (bought)
            {
                onConfirmed?.Invoke();
                Close();
            }
            else if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
        }
    }
}
