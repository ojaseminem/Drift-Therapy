using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Binds the authored main-menu prefab to <see cref="GameApp"/> (currencies,
    /// level/XP, best, vehicle showcase) and routes the bottom-nav buttons to the
    /// <see cref="PopupHandler"/>, which instantiates the per-screen popup prefabs.
    /// Does not build UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Currencies / profile")]
        public TMP_Text coinsText, gemsText, keysText, levelText, xpText, bestText, vehicleNameText;
        public RectTransform xpFill;
        public Image vehicleSwatch;

        [Header("Buttons")]
        public Button runButton, garageButton, homeButton, missionsButton, trialsButton, shopButton;

        [Header("Popups")]
        public PopupHandler popups;
        public GameObject missionsPopup, vehiclesPopup, shopPopup, trialsPopup;

        GameApp app;

        void Start()
        {
            app = GameApp.Instance;

            Bind(runButton, () => SceneFlow.GoToGame());
            Bind(garageButton, () => { if (popups) popups.Open(vehiclesPopup); });
            Bind(missionsButton, () => { if (popups) popups.Open(missionsPopup); });
            Bind(trialsButton, () => { if (popups) popups.Open(trialsPopup); });
            Bind(shopButton, () => { if (popups) popups.Open(shopPopup); });
            Bind(homeButton, () => { if (popups) popups.Close(); });

            if (app != null) app.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() { if (app != null) app.Changed -= Refresh; }

        static void Bind(Button b, System.Action a)
        {
            if (!b) return;
            b.onClick.RemoveAllListeners();
            if (a != null) b.onClick.AddListener(() => a());
        }

        void Refresh()
        {
            if (app == null) return;
            var d = app.Data;
            Set(coinsText, d.coins); Set(gemsText, d.gems); Set(keysText, d.keys);
            Set(levelText, d.level);
            if (xpText) xpText.text = $"{d.xp}/{app.XpToLevel}";
            if (xpFill) { var an = xpFill.anchorMax; an.x = app.XpToLevel > 0 ? Mathf.Clamp01((float)d.xp / app.XpToLevel) : 0f; xpFill.anchorMax = an; }
            if (bestText) bestText.text = $"BEST {Mathf.FloorToInt(d.bestDistanceMeters)} m";
            var sel = app.Selected;
            if (sel != null)
            {
                if (vehicleNameText) vehicleNameText.text = sel.displayName;
                if (vehicleSwatch) vehicleSwatch.color = sel.bodyColor;
            }
        }

        static void Set(TMP_Text t, int v) { if (t) t.text = v.ToString(); }
    }
}
