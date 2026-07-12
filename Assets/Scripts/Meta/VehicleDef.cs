using System;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// One purchasable colour variant of a <see cref="VehicleDef"/> — e.g. Factory,
    /// Stealth, Pearl, Rally. All vehicles share the same mesh, so a skin is just an
    /// alternate <see cref="color"/> applied via the same MaterialPropertyBlock path
    /// the base <see cref="VehicleDef.bodyColor"/> uses.
    /// </summary>
    [Serializable]
    public class VehicleSkinDef
    {
        [Tooltip("Stable id stored in the save file, unique within its vehicle. Keep unchanging.")]
        public string id = "factory";
        public string displayName = "Factory";
        public Color color = Color.white;

        [Tooltip("Coin price. 0 = not purchasable with coins.")]
        public int price = 0;

        [Tooltip("Gem price. 0 = not purchasable with gems.")]
        public int gemPrice = 0;

        [Tooltip("Owned without purchase (every vehicle should have exactly one default skin).")]
        public bool ownedByDefault = false;
    }

    /// <summary>
    /// A selectable vehicle in the garage. For this milestone a vehicle is a
    /// recolour of the player car body; the gameplay car applies the equipped
    /// skin's colour (falling back to <see cref="bodyColor"/>) on spawn.
    /// Extendable later to distinct meshes/stats.
    ///
    /// Create via: Assets → Create → Drift Therapy → Vehicle
    /// </summary>
    [CreateAssetMenu(fileName = "Vehicle", menuName = "Drift Therapy/Vehicle")]
    public class VehicleDef : ScriptableObject
    {
        [Tooltip("Stable id stored in the save file. Keep unique and unchanging.")]
        public string id = "default";
        public string displayName = "Drifter";

        [Tooltip("Coin price in the garage. 0 = not purchasable with coins.")]
        public int price = 0;

        [Tooltip("Gem price in the garage. 0 = not purchasable with gems.")]
        public int gemPrice = 0;

        [Tooltip("Owned without purchase (e.g. the starter car).")]
        public bool ownedByDefault = false;

        [Tooltip("Body colour applied to the car in-game when no skin system entry is equipped (fallback / default skin colour).")]
        public Color bodyColor = new Color(0.16f, 0.7f, 0.9f, 1f);

        [Tooltip("Full gameplay car prefab (physics + visuals), spawned by PlayerVehicleSpawner and shown on the Garage turntable. Create under Assets/Prefabs/Vehicles/.")]
        public GameObject prefab;

        [Header("Display-only specs (flavor for the Garage cards — not wired into physics; all vehicles stay cosmetic/stat-neutral by design)")]
        [Range(0, 100)] public int specSpeed = 50;
        [Range(0, 100)] public int specHandling = 50;
        [Range(0, 100)] public int specBoost = 50;

        [Header("Skins")]
        [Tooltip("Purchasable colour variants for this vehicle. Should include exactly one ownedByDefault entry.")]
        public VehicleSkinDef[] skins = Array.Empty<VehicleSkinDef>();
    }
}
