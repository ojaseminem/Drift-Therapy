using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// A selectable vehicle (skin) in the garage. For this milestone a vehicle is a
    /// recolour of the player car body; the gameplay car applies <see cref="bodyColor"/>
    /// on spawn. Extendable later to distinct meshes/stats.
    ///
    /// Create via: Assets → Create → Drift Therapy → Vehicle
    /// </summary>
    [CreateAssetMenu(fileName = "Vehicle", menuName = "Drift Therapy/Vehicle")]
    public class VehicleDef : ScriptableObject
    {
        [Tooltip("Stable id stored in the save file. Keep unique and unchanging.")]
        public string id = "default";
        public string displayName = "Drifter";

        [Tooltip("Coin price in the garage. 0 = free.")]
        public int price = 0;

        [Tooltip("Owned without purchase (e.g. the starter car).")]
        public bool ownedByDefault = false;

        [Tooltip("Body colour applied to the car in-game.")]
        public Color bodyColor = new Color(0.16f, 0.7f, 0.9f, 1f);
    }
}
