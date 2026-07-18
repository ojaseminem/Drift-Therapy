using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// A purchasable recolor of the drift boost/smoke trail VFX. Purely cosmetic —
    /// see <see cref="ComboSmokeFx"/>, which blends this color in as its base tint
    /// instead of the hardcoded default once one is equipped.
    ///
    /// Create via: Assets → Create → Drift Therapy → Booster Skin
    /// </summary>
    [CreateAssetMenu(fileName = "Booster", menuName = "Drift Therapy/Booster Skin")]
    public class BoosterSkinDef : ScriptableObject
    {
        [Tooltip("Stable id stored in the save file. Keep unique and unchanging.")]
        public string id = "booster";
        public string displayName = "Booster";

        [Tooltip("Tint applied to the drift smoke trail while this booster is equipped.")]
        public Color trailColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);

        [Tooltip("Coin price. 0 = not purchasable with coins.")]
        public int price = 0;

        [Tooltip("Gem price. 0 = not purchasable with gems.")]
        public int gemPrice = 0;

        [Tooltip("Owned without purchase (the default trail color).")]
        public bool ownedByDefault = false;
    }
}
