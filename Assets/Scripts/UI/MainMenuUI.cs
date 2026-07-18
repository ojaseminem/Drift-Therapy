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
        public Button runButton, garageButton, homeButton, missionsButton, trialsButton, shopButton, leaderboardButton, settingsButton, plusButton;

        [Header("Popups")]
        public PopupHandler popups;
        public GameObject missionsPopup, shopPopup, trialsPopup, leaderboardPopup, settingsPopup, onboardingPopup;

        GameApp app;
        int lastCoins = -1, lastGems = -1;

        void Start()
        {
            app = GameApp.Instance;

            Bind(runButton, () => SceneFlow.GoToGame());
            // Garage is a full scene (with the vehicle carousel + 3D turntable),
            // not a popup — reached via the same fade-transition SceneLoader RUN uses.
            Bind(garageButton, () => SceneFlow.GoToGarage());
            Bind(missionsButton, () => { if (popups) popups.Open(missionsPopup); });
            Bind(trialsButton, () => { if (popups) popups.Open(trialsPopup); });
            Bind(shopButton, () => { if (popups) popups.Open(shopPopup); });
            Bind(plusButton, () => { if (popups) popups.Open(shopPopup); });
            Bind(leaderboardButton, () => { if (popups) popups.Open(leaderboardPopup); });
            Bind(settingsButton, () => { if (popups) popups.Open(settingsPopup); });
            Bind(homeButton, () => { if (popups) popups.Close(); });

            if (app != null) app.Changed += Refresh;
            Refresh();

            // First-run "how to play" — shown once, gated by onboardingSeen.
            if (app != null && !app.Data.onboardingSeen && popups != null && onboardingPopup != null)
            {
                popups.Open(onboardingPopup);
            }
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
            SetOrCount(coinsText, ref lastCoins, d.coins);
            SetOrCount(gemsText, ref lastGems, d.gems);
            Set(keysText, d.keys);
            Set(levelText, d.level);
            if (xpText) xpText.text = $"{d.xp}/{app.XpToLevel}";
            if (xpFill) UiJuice.FillTo(xpFill, app.XpToLevel > 0 ? (float)d.xp / app.XpToLevel : 0f, 0.25f);
            if (bestText) bestText.text = $"BEST {Mathf.FloorToInt(d.bestDistanceMeters)} m";
            var sel = app.Selected;
            if (sel != null)
            {
                if (vehicleNameText) vehicleNameText.text = sel.displayName;
                if (vehicleSwatch) vehicleSwatch.color = app.GetEquippedColor(sel.id);
            }
        }

        static void Set(TMP_Text t, int v) { if (t) t.text = v.ToString(); }

        /// <summary>Snaps on first refresh / on decrease (e.g. a spend); counts up on increase (e.g. a claim reward).</summary>
        static void SetOrCount(TMP_Text t, ref int last, int value)
        {
            if (t == null) { last = value; return; }
            if (last < 0 || value <= last) t.text = value.ToString();
            else UiJuice.CountTo(t, last, value, 0.4f);
            last = value;
        }
    }
}
