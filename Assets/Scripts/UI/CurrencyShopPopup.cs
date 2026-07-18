using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// "Get Drift Coins & Gems" popup — one row per <see cref="CurrencyPackDef"/>
    /// in <see cref="GameApp.CurrencyPacks"/>, cloned from <see cref="rowTemplate"/>.
    /// Mirrors <see cref="MissionsPopup"/>'s Populate()/row-template pattern.
    /// Opened fresh each time via PopupHandler.Open (never reused), so Start()
    /// alone is enough to always show the latest real-store price.
    /// </summary>
    public class CurrencyShopPopup : Popup
    {
        public Transform content;
        public GameObject rowTemplate;

        static readonly Color CoinDot = new Color(1f, 0.80f, 0.20f, 1f);
        static readonly Color GemDot = new Color(0.40f, 0.75f, 1f, 1f);

        GameApp app;

        void Start()
        {
            app = GameApp.Instance;
            if (rowTemplate) rowTemplate.SetActive(false);
            Populate();
        }

        void Populate()
        {
            if (app == null || content == null || rowTemplate == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                if (c.gameObject != rowTemplate) Destroy(c.gameObject);
            }

            var packs = app.CurrencyPacks;
            if (packs == null) return;

            int shownIndex = 0;
            for (int i = 0; i < packs.Count; i++)
            {
                var def = packs[i];
                if (def == null) continue;
                var row = Instantiate(rowTemplate, content);
                row.SetActive(true);
                FillRow(row.transform, def);
                UiJuice.StaggerIn(row.GetComponent<CanvasGroup>(), shownIndex);
                shownIndex++;
            }
        }

        void FillRow(Transform row, CurrencyPackDef def)
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
                action.onClick.AddListener(() => PlatformServices.IAP.PurchaseCurrencyPack(def.productId));
            }
        }
    }
}
