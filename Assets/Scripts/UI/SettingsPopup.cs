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
        public Button musicButton, sfxButton, hapticsButton, removeAdsButton, restoreButton, resetDataButton;
        public TMP_Text versionText;

        GameApp app;

        [Tooltip("Seconds the Reset Progress button stays armed after a first tap, before it reverts to needing a fresh tap-to-arm.")]
        [SerializeField] float resetArmSeconds = 3f;
        bool resetArmed;
        float resetArmedUntil;

        void Start()
        {
            app = GameApp.Instance;
            if (versionText) versionText.text = "v" + Application.version;

            Bind(musicButton, () => app.SetMusicVolume(app.Data.musicVolume > 0f ? 0f : 1f));
            Bind(sfxButton, () => app.SetSfxVolume(app.Data.sfxVolume > 0f ? 0f : 1f));
            Bind(hapticsButton, () => app.SetHaptics(!app.Data.haptics));
            Bind(removeAdsButton, () => PlatformServices.IAP.PurchaseRemoveAds());
            Bind(restoreButton, () => PlatformServices.IAP.RestorePurchases());
            Bind(resetDataButton, OnResetDataTapped);

            if (app != null) app.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() { if (app != null) app.Changed -= Refresh; }

        void Update()
        {
            // Silently disarm (revert the label) once the confirm window lapses without a
            // second tap — no per-frame work otherwise.
            if (resetArmed && Time.unscaledTime >= resetArmedUntil)
            {
                resetArmed = false;
                RefreshResetLabel();
            }
        }

        static void Bind(Button b, System.Action a) { if (b) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => a()); } }

        /// <summary>Tap-to-arm confirmation: first tap arms a short confirm window (no popup
        /// needed for a single settings action); a second tap within that window actually wipes
        /// the save. Prevents an accidental one-tap wipe of the player's entire progress.</summary>
        void OnResetDataTapped()
        {
            if (resetArmed && Time.unscaledTime < resetArmedUntil)
            {
                resetArmed = false;
                GameApp.ClearSavedData();
                return;
            }

            resetArmed = true;
            resetArmedUntil = Time.unscaledTime + resetArmSeconds;
            RefreshResetLabel();
            if (resetDataButton) UiJuice.PunchScale((RectTransform)resetDataButton.transform, 0.15f);
        }

        void RefreshResetLabel()
        {
            if (!resetDataButton) return;
            var label = resetDataButton.transform.Find("Label")?.GetComponent<TMP_Text>();
            if (label) label.text = resetArmed ? "TAP AGAIN TO CONFIRM" : "RESET PROGRESS";
        }

        void Refresh()
        {
            if (app == null) return;
            SetToggleLabel(musicButton, "MUSIC", app.Data.musicVolume > 0f);
            SetToggleLabel(sfxButton, "SFX", app.Data.sfxVolume > 0f);
            SetToggleLabel(hapticsButton, "HAPTICS", app.Data.haptics);
            RefreshResetLabel();

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
