using System.Collections.Generic;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Mounts owned/equipped <see cref="CosmeticAttachmentDef"/> prefabs onto a vehicle
    /// instance. Attach this to any spawned vehicle (gameplay car or garage turntable
    /// car), call <see cref="Bind"/> once after spawn, and call <see cref="Refresh"/>
    /// again whenever equipped attachments might have changed (e.g. on GameApp.Changed).
    ///
    /// Mount resolution, in order:
    ///   1. A child transform named "Attach_&lt;slotId&gt;" on the vehicle instance, if
    ///      present (exact placement — add one to a vehicle prefab for a tailored fit).
    ///   2. A built-in fallback local offset for a few well-known slot ids (Spoiler,
    ///      Underglow, Exhaust), computed from the vehicle's render bounds.
    ///   3. A generic center-of-bounds fallback for any other slot id — so a brand new
    ///      slot type just works on every vehicle the instant its CosmeticAttachmentDef
    ///      exists, with no code change required. This is what makes the slot system
    ///      "extend by adding data, not code."
    /// </summary>
    [DisallowMultipleComponent]
    public class VehicleAttachmentController : MonoBehaviour
    {
        public string VehicleId { get; private set; }

        readonly Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();

        public void Bind(string vehicleId)
        {
            VehicleId = vehicleId;
            Refresh();
        }

        /// <summary>Destroys and re-spawns every equipped attachment for this vehicle from scratch.
        /// Only called on spawn / on GameApp.Changed (buy or equip) — never per-frame — so a full
        /// rebuild is simplest and plenty cheap.</summary>
        public void Refresh()
        {
            var app = GameApp.Instance;

            foreach (var go in spawned.Values) if (go != null) Destroy(go);
            spawned.Clear();

            if (app == null || string.IsNullOrEmpty(VehicleId)) return;

            foreach (var e in app.Data.equippedAttachments)
            {
                if (e.vehicleId != VehicleId) continue;
                var def = app.GetAttachmentDef(e.attachmentId);
                if (def == null || def.attachmentPrefab == null) continue;
                SpawnAttachment(e.slotId, def);
            }
        }

        void SpawnAttachment(string slotId, CosmeticAttachmentDef def)
        {
            var instance = Instantiate(def.attachmentPrefab);
            var mount = transform.Find("Attach_" + slotId);

            if (mount != null)
            {
                instance.transform.SetParent(mount, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                instance.transform.SetParent(transform, false);
                instance.transform.localPosition = FallbackLocalPosition(slotId);
                instance.transform.localRotation = Quaternion.identity;
            }

            spawned[slotId] = instance;
        }

        Vector3 FallbackLocalPosition(string slotId)
        {
            Bounds b = ComputeLocalBounds();
            switch (slotId)
            {
                case "Spoiler":   return new Vector3(0f, b.max.y, b.min.z + b.size.z * 0.08f);
                case "Underglow": return new Vector3(0f, b.min.y + 0.02f, b.center.z);
                case "Exhaust":   return new Vector3(0f, b.min.y + b.size.y * 0.15f, b.min.z);
                default:          return b.center; // unknown slot id — safe generic fallback
            }
        }

        /// <summary>
        /// Render bounds of the vehicle in its own local space. Assumes the vehicle root
        /// is at world-identity rotation at mount time (true for both PlayerVehicleSpawner
        /// and GarageVehicleDisplay, which both explicitly set Quaternion.identity on spawn).
        /// </summary>
        Bounds ComputeLocalBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

            Bounds world = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) world.Encapsulate(renderers[i].bounds);

            Vector3 localCenter = transform.InverseTransformPoint(world.center);
            Vector3 localSize = transform.InverseTransformVector(world.size);
            return new Bounds(localCenter, new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z)));
        }
    }
}
