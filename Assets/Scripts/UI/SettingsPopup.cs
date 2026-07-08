using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Settings popup: music/SFX/haptics toggles, remove-ads purchase/restore,
    /// version line. Volume is a simple on/off toggle (not a continuous
    /// slider) — matches the existing button-driven UI pattern everywhere
    /// else in this codebase rather than introducing an untested custom
    /// Slider control for one popup.
    /// </summary>
    public class SettingsPopup : Popup
    {
        public Button musicButton, sfxButton, hapticsButton, removeAdsButton, restoreButton;
        public TMP_Text versionText;

        GameApp app;

        void Start()
        {
            app = GameApp.Instance;
            if (versionText) versionText.text = "v" + Application.version;

            Bind(musicButton, () => app.SetMusicVolume(app.Data.musicVolume > 0f ? 0f : 1f));
            Bind(sfxButton, () => app.SetSfxVolume(app.Data.sfxVolume > 0f ? 0f : 1f));
            Bind(hapticsButton, () => app.SetHaptics(!app.Data.haptics));
            Bind(removeAdsButton, () => PlatformServices.IAP.PurchaseRemoveAds());
            Bind(restoreButton, () => PlatformServices.IAP.RestorePurchases());

            if (app != null) app.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() { if (app != null) app.Changed -= Refresh; }

        static void Bind(Button b, System.Action a) { if (b) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => a()); } }

        void Refresh()
        {
            if (app == null) return;
            SetToggleLabel(musicButton, "MUSIC", app.Data.musicVolume > 0f);
            SetToggleLabel(sfxButton, "SFX", app.Data.sfxVolume > 0f);
            SetToggleLabel(hapticsButton, "HAPTICS", app.Data.haptics);

            bool owned = PlatformServices.IAP.RemoveAdsOwned;
            if (removeAdsButton)
            {
                removeAdsButton.interactable = !owned;
                var label = removeAdsButton.transform.Find("Label")?.GetComponent<TMP_Text>();
                if (label) label.text = owned ? "ADS REMOVED" : "REMOVE ADS";
            }
        }

        static void SetToggleLabel(Button b, string title, bool on)
        {
            if (!b) return;
            var label = b.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label) label.text = title + ": " + (on ? "ON" : "OFF");
        }
    }
}
