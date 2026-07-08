using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// "Buy this vehicle?" confirmation popup — reuses <see cref="Popup"/>/
    /// <see cref="PopupHandler"/> exactly as-is. <see cref="Show"/> populates
    /// name/price; Confirm actually spends currency via
    /// <see cref="GameApp.TryBuy(VehicleDef, bool)"/>.
    /// </summary>
    public class PurchaseConfirmPopup : Popup
    {
        public TMP_Text nameText, priceText;
        public Button confirmButton;

        VehicleDef pending;
        bool pendingUseGems;
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
        public void Show(VehicleDef v, bool useGems, Action onConfirmed)
        {
            pending = v;
            pendingUseGems = useGems;
            this.onConfirmed = onConfirmed;

            if (nameText) nameText.text = v.displayName;
            if (priceText) priceText.text = useGems ? (v.gemPrice + " GEMS") : (v.price + " COINS");
        }

        void OnConfirm()
        {
            if (pending == null || GameApp.Instance == null) return;

            // Guard against double-tap double-charging: disable immediately, the
            // popup closes on success either way.
            if (confirmButton != null) confirmButton.interactable = false;

            bool bought = GameApp.Instance.TryBuy(pending, pendingUseGems);
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
