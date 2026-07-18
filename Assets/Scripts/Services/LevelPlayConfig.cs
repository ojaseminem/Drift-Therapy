using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// LevelPlay (Unity Ads Mediation / ironSource) configuration — the App Key and ad
    /// unit ids from your LevelPlay dashboard. Loaded via Resources.Load, so this asset
    /// must live at exactly <c>Assets/Resources/LevelPlayConfig.asset</c>.
    ///
    /// All fields start empty on purpose: <see cref="LevelPlayAdsService"/> checks for
    /// an empty App Key and simply stays uninitialized (logs a warning, never crashes)
    /// until you paste in real values from your account — nobody but you can generate
    /// these. Create via: Assets → Create → Drift Therapy → LevelPlay Config.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelPlayConfig", menuName = "Drift Therapy/LevelPlay Config")]
    public class LevelPlayConfig : ScriptableObject
    {
        [Tooltip("Android App Key from the LevelPlay dashboard (Setup > App Settings).")]
        public string androidAppKey = "";

        [Tooltip("Ad unit id for interstitials (LevelPlay dashboard > Ad Units).")]
        public string interstitialAdUnitId = "";

        [Tooltip("Ad unit id for rewarded video (LevelPlay dashboard > Ad Units).")]
        public string rewardedAdUnitId = "";
    }
}
