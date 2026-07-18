using System;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// A purchasable 3D cosmetic attachment (spoiler, underglow, exhaust tip, ...)
    /// that mounts onto a vehicle. <see cref="slotId"/> is a free-form string, not
    /// an enum, so adding a new attachment category is a pure data change — no
    /// code edit, no recompile. See <see cref="VehicleAttachmentController"/> for
    /// how a slot resolves to a mount point on the actual vehicle instance.
    ///
    /// Create via: Assets → Create → Drift Therapy → Cosmetic Attachment
    /// </summary>
    [CreateAssetMenu(fileName = "Attachment", menuName = "Drift Therapy/Cosmetic Attachment")]
    public class CosmeticAttachmentDef : ScriptableObject
    {
        [Tooltip("Stable id stored in the save file. Keep unique and unchanging.")]
        public string id = "attachment";
        public string displayName = "Attachment";

        [Tooltip("Which mount slot this occupies, e.g. \"Spoiler\", \"Underglow\", \"Exhaust\". " +
                 "Only one attachment can be equipped per slot per vehicle. New slot names just work — " +
                 "see VehicleAttachmentController's fallback mount-offset table.")]
        public string slotId = "Spoiler";

        [Tooltip("The 3D piece instantiated and parented onto the vehicle.")]
        public GameObject attachmentPrefab;

        [Tooltip("Coin price. 0 = not purchasable with coins.")]
        public int price = 0;

        [Tooltip("Gem price. 0 = not purchasable with gems.")]
        public int gemPrice = 0;

        [Tooltip("Owned without purchase.")]
        public bool ownedByDefault = false;
    }
}
