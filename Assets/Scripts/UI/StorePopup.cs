using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Category-tabbed store: Cars, Boosters (trail color skins), Cosmetics (vehicle
    /// attachments), Currency (coin/gem packs), and No Ads. One popup, five panes —
    /// only one visible at a time, switched via the tab strip.
    ///
    /// Reuses the same "Content transform + rowTemplate clone" pattern as
    /// <see cref="MissionsPopup"/>/the old CurrencyShopPopup for the four list panes;
    /// No Ads is a single tile rather than a list (mirrors SettingsPopup's remove-ads
    /// button, just relocated here too since the Store is the natural one-stop shop).
    /// </summary>
    public class StorePopup : Popup
    {
        [Header("Tabs")]
        public Button carsTab, boostersTab, cosmeticsTab, currencyTab, noAdsTab;
        public GameObject carsPane, boostersPane, cosmeticsPane, currencyPane, noAdsPane;

        [Header("Cars")]
        public Transform carsContent;
        public GameObject carRowTemplate;

        [Header("Boosters")]
        public Transform boostersContent;
        public GameObject boosterRowTemplate;

        [Header("Cosmetics")]
        public Transform cosmeticsContent;
        public GameObject cosmeticRowTemplate;

        [Header("Currency")]
        public Transform currencyContent;
        public GameObject currencyRowTemplate;

        [Header("No Ads")]
        public Button noAdsButton;

        static readonly Color TabActive = new Color(0.18f, 0.85f, 0.78f, 1f);
        static readonly Color TabInactive = new Color(0.16f, 0.19f, 0.25f, 0.98f);
        static readonly Color CoinDot = new Color(1f, 0.80f, 0.20f, 1f);
        static readonly Color GemDot = new Color(0.40f, 0.75f, 1f, 1f);

        GameApp app;
        Button[] tabs;
        GameObject[] panes;
        int activeTab = -1;

        void Start()
        {
            app = GameApp.Instance;
            tabs = new[] { carsTab, boostersTab, cosmeticsTab, currencyTab, noAdsTab };
            panes = new[] { carsPane, boostersPane, cosmeticsPane, currencyPane, noAdsPane };

            if (carRowTemplate) carRowTemplate.SetActive(false);
            if (boosterRowTemplate) boosterRowTemplate.SetActive(false);
            if (cosmeticRowTemplate) cosmeticRowTemplate.SetActive(false);
            if (currencyRowTemplate) currencyRowTemplate.SetActive(false);

            BindTab(carsTab, 0);
            BindTab(boostersTab, 1);
            BindTab(cosmeticsTab, 2);
            BindTab(currencyTab, 3);
            BindTab(noAdsTab, 4);
            Bind(noAdsButton, () => PlatformServices.IAP.PurchaseRemoveAds());

            if (app != null) app.Changed += PopulateAll;
            PopulateAll();
            ShowTab(0);
        }

        void OnDestroy() { if (app != null) app.Changed -= PopulateAll; }

        static void Bind(Button b, System.Action a) { if (b) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => a()); } }
        void BindTab(Button b, int index) { Bind(b, () => ShowTab(index)); }

        void ShowTab(int index)
        {
            if (activeTab == index) return;
            activeTab = index;

            for (int i = 0; i < panes.Length; i++)
            {
                bool active = i == index;
                if (panes[i] != null) panes[i].SetActive(active);

                var img = tabs[i] != null ? tabs[i].GetComponent<Image>() : null;
                if (img != null) img.color = active ? TabActive : TabInactive;
                var label = tabs[i] != null ? tabs[i].transform.Find("Label")?.GetComponent<TMP_Text>() : null;
                if (label != null) label.color = active ? new Color(0.04f, 0.06f, 0.09f, 1f) : Color.white;
            }

            var activePaneGroup = panes[index] != null ? panes[index].GetComponent<CanvasGroup>() : null;
            if (activePaneGroup != null) UiJuice.FadeCanvasGroup(activePaneGroup, 1f, 0.18f);
            if (tabs[index] != null) UiJuice.PunchScale((RectTransform)tabs[index].transform, 0.1f, 0.16f);
        }

        void PopulateAll()
        {
            if (app == null) return;
            PopulateCars();
            PopulateBoosters();
            PopulateCosmetics();
            PopulateCurrency();
            RefreshNoAds();
        }

        // ── Cars ─────────────────────────────────────────────────────────────
        void PopulateCars()
        {
            if (carsContent == null || carRowTemplate == null) return;
            ClearExcept(carsContent, carRowTemplate);

            var defs = app.Vehicles;
            int shown = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var v = defs[i];
                if (v == null) continue;
                var row = Instantiate(carRowTemplate, carsContent);
                row.SetActive(true);
                FillCarRow(row.transform, v);
                UiJuice.StaggerIn(row.GetComponent<CanvasGroup>(), shown);
                shown++;
            }
        }

        void FillCarRow(Transform row, VehicleDef v)
        {
            var name = row.Find("Name")?.GetComponent<TMP_Text>();
            if (name) name.text = v.displayName;

            bool owned = app.Owns(v.id);
            bool equipped = app.Data.selectedVehicleId == v.id;

            var price = row.Find("Price")?.GetComponent<TMP_Text>();
            if (price)
            {
                bool useGems = v.price <= 0 && v.gemPrice > 0;
                price.gameObject.SetActive(!owned);
                if (!owned) price.text = (useGems ? v.gemPrice : v.price).ToString();
            }

            var action = row.Find("Action")?.GetComponent<Button>();
            var label = action ? action.transform.Find("Label")?.GetComponent<TMP_Text>() : null;
            if (label) label.text = equipped ? "EQUIPPED" : owned ? "EQUIP" : "BUY";
            if (action)
            {
                action.interactable = !equipped;
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(() =>
                {
                    UiJuice.PunchScale((RectTransform)action.transform, 0.12f, 0.18f);
                    if (owned) { app.Select(v.id); return; }
                    bool useGems = v.price <= 0 && v.gemPrice > 0;
                    app.TryBuy(v, useGems);
                });
            }
        }

        // ── Boosters ─────────────────────────────────────────────────────────
        void PopulateBoosters()
        {
            if (boostersContent == null || boosterRowTemplate == null) return;
            ClearExcept(boostersContent, boosterRowTemplate);

            var defs = app.Boosters;
            if (defs == null) return;
            int shown = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var b = defs[i];
                if (b == null) continue;
                var row = Instantiate(boosterRowTemplate, boostersContent);
                row.SetActive(true);
                FillBoosterRow(row.transform, b);
                UiJuice.StaggerIn(row.GetComponent<CanvasGroup>(), shown);
                shown++;
            }
        }

        void FillBoosterRow(Transform row, BoosterSkinDef b)
        {
            var dot = row.Find("Dot")?.GetComponent<Image>();
            if (dot) dot.color = b.trailColor;

            var name = row.Find("Name")?.GetComponent<TMP_Text>();
            if (name) name.text = b.displayName;

            bool owned = app.OwnsBooster(b.id);
            bool equipped = app.Data.equippedBoosterId == b.id;

            var price = row.Find("Price")?.GetComponent<TMP_Text>();
            if (price)
            {
                bool useGems = b.price <= 0 && b.gemPrice > 0;
                price.gameObject.SetActive(!owned);
                if (!owned) price.text = (useGems ? b.gemPrice : b.price).ToString();
            }

            var action = row.Find("Action")?.GetComponent<Button>();
            var label = action ? action.transform.Find("Label")?.GetComponent<TMP_Text>() : null;
            if (label) label.text = equipped ? "EQUIPPED" : owned ? "EQUIP" : "BUY";
            if (action)
            {
                action.interactable = !equipped;
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(() =>
                {
                    UiJuice.PunchScale((RectTransform)action.transform, 0.12f, 0.18f);
                    if (owned) { app.SelectBooster(b.id); return; }
                    bool useGems = b.price <= 0 && b.gemPrice > 0;
                    app.TryBuyBooster(b, useGems);
                });
            }
        }

        // ── Cosmetics (attachments) ──────────────────────────────────────────
        void PopulateCosmetics()
        {
            if (cosmeticsContent == null || cosmeticRowTemplate == null) return;
            ClearExcept(cosmeticsContent, cosmeticRowTemplate);

            var defs = app.Attachments;
            if (defs == null) return;
            int shown = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var a = defs[i];
                if (a == null) continue;
                var row = Instantiate(cosmeticRowTemplate, cosmeticsContent);
                row.SetActive(true);
                FillCosmeticRow(row.transform, a);
                UiJuice.StaggerIn(row.GetComponent<CanvasGroup>(), shown);
                shown++;
            }
        }

        void FillCosmeticRow(Transform row, CosmeticAttachmentDef a)
        {
            var name = row.Find("Name")?.GetComponent<TMP_Text>();
            if (name) name.text = a.displayName;

            var slotLabel = row.Find("Slot")?.GetComponent<TMP_Text>();
            if (slotLabel) slotLabel.text = a.slotId.ToUpperInvariant();

            bool owned = app.OwnsAttachment(a.id);
            // Attachments equip onto whichever vehicle is currently selected — switch
            // cars in the Garage to fit a different one to a different vehicle.
            string vehicleId = app.Data.selectedVehicleId;
            bool equipped = owned && app.GetEquippedAttachmentId(vehicleId, a.slotId) == a.id;

            var price = row.Find("Price")?.GetComponent<TMP_Text>();
            if (price)
            {
                bool useGems = a.price <= 0 && a.gemPrice > 0;
                price.gameObject.SetActive(!owned);
                if (!owned) price.text = (useGems ? a.gemPrice : a.price).ToString();
            }

            var action = row.Find("Action")?.GetComponent<Button>();
            var label = action ? action.transform.Find("Label")?.GetComponent<TMP_Text>() : null;
            if (label) label.text = equipped ? "UNEQUIP" : owned ? "EQUIP" : "BUY";
            if (action)
            {
                action.interactable = true;
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(() =>
                {
                    UiJuice.PunchScale((RectTransform)action.transform, 0.12f, 0.18f);
                    if (!owned)
                    {
                        bool useGems = a.price <= 0 && a.gemPrice > 0;
                        app.TryBuyAttachment(a, useGems);
                        return;
                    }
                    if (equipped) app.UnequipAttachmentSlot(vehicleId, a.slotId);
                    else app.EquipAttachment(vehicleId, a.slotId, a.id);
                });
            }
        }

        // ── Currency packs ───────────────────────────────────────────────────
        void PopulateCurrency()
        {
            if (currencyContent == null || currencyRowTemplate == null) return;
            ClearExcept(currencyContent, currencyRowTemplate);

            var packs = app.CurrencyPacks;
            if (packs == null) return;
            int shown = 0;
            for (int i = 0; i < packs.Count; i++)
            {
                var def = packs[i];
                if (def == null) continue;
                var row = Instantiate(currencyRowTemplate, currencyContent);
                row.SetActive(true);
                FillCurrencyRow(row.transform, def);
                UiJuice.StaggerIn(row.GetComponent<CanvasGroup>(), shown);
                shown++;
            }
        }

        void FillCurrencyRow(Transform row, CurrencyPackDef def)
        {
            bool gems = def.currencyType == CurrencyType.Gems;

            var dot = row.Find("Dot")?.GetComponent<Image>();
            if (dot) dot.color = gems ? GemDot : CoinDot;

            var amount = row.Find("Amount")?.GetComponent<TMP_Text>();
            if (amount) amount.text = def.amount.ToString("N0") + (gems ? " GEMS" : " DRIFT COINS");

            var bonus = row.Find("Bonus")?.GetComponent<TMP_Text>();
            if (bonus)
            {
                bonus.gameObject.SetActive(def.bonusPercent > 0);
                if (def.bonusPercent > 0) bonus.text = "+" + def.bonusPercent + "% BONUS";
            }

            var action = row.Find("Action")?.GetComponent<Button>();
            var label = action ? action.transform.Find("Label")?.GetComponent<TMP_Text>() : null;
            if (label)
            {
                string price = PlatformServices.IAP.GetLocalizedPrice(def.productId);
                label.text = string.IsNullOrEmpty(price) ? def.fallbackPriceText : price;
            }
            if (action)
            {
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(() =>
                {
                    UiJuice.PunchScale((RectTransform)action.transform, 0.12f, 0.18f);
                    PlatformServices.IAP.PurchaseCurrencyPack(def.productId);
                });
            }
        }

        // ── No Ads ───────────────────────────────────────────────────────────
        void RefreshNoAds()
        {
            bool owned = PlatformServices.IAP.RemoveAdsOwned;
            if (!noAdsButton) return;
            noAdsButton.interactable = !owned;
            var label = noAdsButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label) label.text = owned ? "ADS REMOVED" : "REMOVE ADS";
        }

        static void ClearExcept(Transform content, GameObject template)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                if (c.gameObject != template) Destroy(c.gameObject);
            }
        }
    }
}
