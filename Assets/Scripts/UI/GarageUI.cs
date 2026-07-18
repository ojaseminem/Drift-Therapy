using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Binds the authored Garage screen (carousel of vehicle cards + stats +
    /// purchase flow) to <see cref="GameApp"/> and the scene's
    /// <see cref="GarageVehicleDisplay"/>. Mirrors <see cref="MissionsPopup"/>'s
    /// Populate()/GameApp.Changed pattern. Does not build UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class GarageUI : MonoBehaviour
    {
        [Header("Stats")]
        public TMP_Text levelText, coinsText, gemsText;

        [Header("Carousel")]
        public SnapCarousel carousel;
        public Transform content;
        public GameObject cardTemplate;
        public Button leftArrow, rightArrow;

        [Header("3D display / input")]
        [Tooltip("Scene reference (the 3D turntable display) — resolved at runtime via FindFirstObjectByType since this is a UI-only prefab.")]
        public GarageDragCatcher dragCatcher;

        [Header("Navigation")]
        public Button homeButton, plusButton;

        [Header("Purchase confirmation")]
        public PopupHandler popups;
        public GameObject purchaseConfirmPopup;
        public GameObject shopPopup;

        GameApp app;
        GarageVehicleDisplay display;
        int lastCoins = -1, lastGems = -1;

        void Start()
        {
            app = GameApp.Instance;
            display = FindFirstObjectByType<GarageVehicleDisplay>();
            if (dragCatcher != null) dragCatcher.display = display;

            Bind(homeButton, () => SceneFlow.GoToMenu());
            Bind(leftArrow, () => { PunchButton(leftArrow); if (carousel != null) carousel.SnapTo(carousel.CurrentIndex - 1); });
            Bind(rightArrow, () => { PunchButton(rightArrow); if (carousel != null) carousel.SnapTo(carousel.CurrentIndex + 1); });
            Bind(plusButton, () => { PunchButton(plusButton); if (popups) popups.Open(shopPopup); });

            if (cardTemplate) cardTemplate.SetActive(false);
            if (carousel != null) carousel.IndexChanged += OnIndexChanged;

            Populate();

            if (app != null) app.Changed += Refresh;
        }

        void OnDestroy()
        {
            if (app != null) app.Changed -= Refresh;
            if (carousel != null) carousel.IndexChanged -= OnIndexChanged;
        }

        static void Bind(Button b, System.Action a) { if (b) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => a()); } }
        static void PunchButton(Button b) { if (b) UiJuice.PunchScale((RectTransform)b.transform, 0.12f, 0.18f); }

        void Populate()
        {
            if (app == null || content == null || cardTemplate == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                if (c.gameObject == cardTemplate) continue;
                // Destroy() is deferred to end-of-frame — detach immediately so a
                // second Populate() this same frame (e.g. after a purchase confirm)
                // doesn't still count these in content.childCount below.
                c.SetParent(null);
                Destroy(c.gameObject);
            }

            var defs = app.Vehicles;
            int count = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var v = defs[i];
                if (v == null) continue;
                var card = Instantiate(cardTemplate, content);
                card.SetActive(true);
                FillCard(card.transform, v);
                count++;
            }

            if (carousel != null)
            {
                carousel.SetCardCount(Mathf.Max(1, count));

                // Cards are positioned/sized directly here rather than via an
                // automatic layout group — script-driven LayoutElement width
                // changes didn't reliably propagate through Unity's layout pass
                // timing in practice. This guarantees exactly one card per
                // viewport width, index*cardWidth apart, matching what
                // SnapCarousel's snap math already assumes.
                float cardWidth = carousel.CardWidth;
                int cardIndex = 0;
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    if (c.gameObject == cardTemplate) continue;
                    var cardRt = c as RectTransform;
                    if (cardRt != null)
                    {
                        cardRt.sizeDelta = new Vector2(cardWidth, 0f);
                        cardRt.anchoredPosition = new Vector2(cardIndex * cardWidth, 0f);
                    }
                    cardIndex++;
                }

                // Content's own width must span every card, or ScrollRect concludes
                // there's nothing to scroll and snaps anchoredPosition back to
                // (0,0) every frame regardless of what SnapTo sets it to.
                var contentRt = content as RectTransform;
                if (contentRt != null) contentRt.sizeDelta = new Vector2(cardWidth * count, contentRt.sizeDelta.y);

                int startIndex = IndexOfSelected();
                carousel.SnapTo(startIndex, animate: false);
                OnIndexChanged(startIndex); // SnapTo only fires IndexChanged on an actual change; guarantee the initial 3D show.
            }

            RefreshStats();
        }

        /// <summary>Re-fills existing cards in place (affordability/ownership can change without needing a re-layout).</summary>
        void Refresh()
        {
            if (app == null || content == null) return;
            var defs = app.Vehicles;
            int di = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                var card = content.GetChild(i);
                if (card.gameObject == cardTemplate) continue;
                if (di >= defs.Count) break;
                FillCard(card, defs[di]);
                di++;
            }
            RefreshStats();
            if (display != null)
            {
                display.RefreshTint();
                display.RefreshAttachments();
            }
        }

        void RefreshStats()
        {
            if (app == null) return;
            if (levelText) levelText.text = "GARAGE LVL " + app.Data.level;
            SetOrCount(coinsText, ref lastCoins, app.Data.coins);
            SetOrCount(gemsText, ref lastGems, app.Data.gems);
        }

        /// <summary>Snaps on first refresh / on decrease (e.g. a spend); counts up on increase (e.g. a claim reward).</summary>
        static void SetOrCount(TMP_Text t, ref int last, int value)
        {
            if (t == null) { last = value; return; }
            if (last < 0 || value <= last) t.text = value.ToString();
            else UiJuice.CountTo(t, last, value, 0.4f);
            last = value;
        }

        int IndexOfSelected()
        {
            var defs = app.Vehicles;
            string selId = app.Data.selectedVehicleId;
            for (int i = 0; i < defs.Count; i++)
                if (defs[i] != null && defs[i].id == selId) return i;
            return 0;
        }

        void OnIndexChanged(int index)
        {
            var defs = app.Vehicles;
            if (leftArrow) leftArrow.interactable = index > 0;
            if (rightArrow) rightArrow.interactable = index < defs.Count - 1;

            if (index < 0 || index >= defs.Count || defs[index] == null) return;
            if (display != null) display.Show(defs[index]);
        }

        void FillCard(Transform card, VehicleDef v)
        {
            var nameText = card.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText) nameText.text = v.displayName;

            SetSpecBar(card, "SpeedBar", v.specSpeed);
            SetSpecBar(card, "HandlingBar", v.specHandling);
            SetSpecBar(card, "BoostBar", v.specBoost);

            bool owned = app.Owns(v.id);
            bool equipped = app.Data.selectedVehicleId == v.id;
            bool hasCoinsPrice = v.price > 0;
            bool hasGemsPrice = v.gemPrice > 0;

            var coinsPriceText = card.Find("CoinsPrice")?.GetComponent<TMP_Text>();
            if (coinsPriceText)
            {
                coinsPriceText.gameObject.SetActive(!owned && hasCoinsPrice);
                if (hasCoinsPrice) coinsPriceText.text = FormatPrice(v.price, app.Data.coins >= v.price);
            }

            var gemsPriceText = card.Find("GemsPrice")?.GetComponent<TMP_Text>();
            if (gemsPriceText)
            {
                gemsPriceText.gameObject.SetActive(!owned && hasGemsPrice);
                if (hasGemsPrice) gemsPriceText.text = FormatPrice(v.gemPrice, app.Data.gems >= v.gemPrice);
            }

            var actionT = card.Find("Action");
            var action = actionT ? actionT.GetComponent<Button>() : null;
            var label = actionT ? actionT.Find("Label")?.GetComponent<TMP_Text>() : null;
            if (label) label.text = equipped ? "EQUIPPED" : owned ? "EQUIP" : "BUY";
            if (action)
            {
                action.interactable = !equipped;
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(() => { PunchButton(action); OnCardAction(v); });
            }

            FillSkinSlots(card, v);
        }

        static readonly Color SkinBorderDefault = new Color(0.16f, 0.19f, 0.25f, 0.98f);
        static readonly Color SkinBorderEquipped = new Color(0.18f, 0.85f, 0.78f, 1f);

        void FillSkinSlots(Transform card, VehicleDef v)
        {
            var skins = v.skins;
            bool vehicleOwned = app.Owns(v.id);
            string equippedId = app.GetEquippedSkinId(v.id);
            for (int i = 0; i < 4; i++)
            {
                var slot = card.Find("Skin" + i);
                if (slot == null) continue;

                if (skins == null || i >= skins.Length)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                var skin = skins[i];
                slot.gameObject.SetActive(true);

                // Skins are locked entirely until the vehicle itself is owned —
                // no buying paint for a car you don't have yet.
                bool ownedSkin = vehicleOwned && app.OwnsSkin(v.id, skin.id);
                bool equippedSkin = ownedSkin && skin.id == equippedId;

                var border = slot.GetComponent<Image>();
                if (border) border.color = equippedSkin ? SkinBorderEquipped : SkinBorderDefault;

                var swatch = slot.Find("Swatch")?.GetComponent<Image>();
                if (swatch) swatch.color = skin.color;

                var lockOverlay = slot.Find("Lock");
                if (lockOverlay != null)
                {
                    lockOverlay.gameObject.SetActive(!ownedSkin);
                    var priceText = lockOverlay.Find("Price")?.GetComponent<TMP_Text>();
                    if (priceText != null)
                    {
                        if (!vehicleOwned)
                        {
                            priceText.text = "";
                        }
                        else
                        {
                            bool skinUseGems = skin.price <= 0 && skin.gemPrice > 0;
                            int amount = skinUseGems ? skin.gemPrice : skin.price;
                            priceText.text = amount.ToString();
                        }
                    }
                }

                var btn = slot.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = vehicleOwned;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { PunchButton(btn); OnSkinAction(v, skin); });
                }
            }
        }

        /// <summary>Strikethrough via TMP's native rich-text tag when unaffordable — no custom font/shader needed.</summary>
        static string FormatPrice(int amount, bool affordable) => affordable ? amount.ToString() : "<s>" + amount + "</s>";

        static void SetSpecBar(Transform card, string barName, int value0to100)
        {
            var fill = card.Find(barName + "/Fill")?.GetComponent<RectTransform>();
            if (fill == null) return;
            UiJuice.FillTo(fill, Mathf.Clamp01(value0to100 / 100f), 0.5f);
        }

        void OnCardAction(VehicleDef v)
        {
            if (app.Owns(v.id))
            {
                app.Select(v.id);
                return;
            }

            // Only a gem price set -> pay with gems; otherwise default to coins.
            bool useGems = v.price <= 0 && v.gemPrice > 0;
            int amount = useGems ? v.gemPrice : v.price;

            // No onConfirmed re-layout here: app.TryBuy already raises Changed,
            // which Refresh() (subscribed in Start) picks up in place — a full
            // Populate() would also re-snap the carousel back to the selected
            // (not currently-viewed) card.
            var confirm = OpenPurchasePopup();
            if (confirm != null) confirm.Show(v.displayName, amount, useGems, () => app.TryBuy(v, useGems), null);
        }

        void OnSkinAction(VehicleDef v, VehicleSkinDef skin)
        {
            // Skins are locked until the vehicle itself is owned.
            if (!app.Owns(v.id)) return;

            if (app.OwnsSkin(v.id, skin.id))
            {
                app.SelectSkin(v.id, skin.id);
                return;
            }

            bool useGems = skin.price <= 0 && skin.gemPrice > 0;
            int amount = useGems ? skin.gemPrice : skin.price;

            var confirm = OpenPurchasePopup();
            if (confirm != null)
            {
                confirm.Show(v.displayName + " – " + skin.displayName, amount, useGems, () =>
                {
                    // Buying a skin equips it immediately — unlike vehicles, there's
                    // no "which one to drive" choice, you were already looking at it.
                    bool bought = app.TryBuySkin(v, skin, useGems);
                    if (bought) app.SelectSkin(v.id, skin.id);
                    return bought;
                }, null);
            }
        }

        PurchaseConfirmPopup OpenPurchasePopup()
        {
            if (popups == null || purchaseConfirmPopup == null) return null;
            var popupGo = popups.Open(purchaseConfirmPopup);
            return popupGo != null ? popupGo.GetComponent<PurchaseConfirmPopup>() : null;
        }
    }
}
