using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// First-run "how to play" popup — shown once (gated by
    /// <see cref="GameApp.PlayerData.onboardingSeen"/>), auto-opened by
    /// <see cref="MainMenuUI"/> on first launch. No wall of text: two lines
    /// (steer + drift), one dismiss button.
    /// </summary>
    public class OnboardingPopup : Popup
    {
        public Button gotItButton;

        void Start()
        {
            if (!gotItButton) return;
            gotItButton.onClick.RemoveAllListeners();
            gotItButton.onClick.AddListener(() =>
            {
                GameApp.Instance?.MarkOnboardingSeen();
                Close();
            });
        }
    }
}
