using System.Collections.Generic;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Spawns/pools/rotates the currently-selected carousel vehicle at a
    /// turntable point in the Garage scene. No RenderTexture — this is a real
    /// 3D scene, so the car renders directly behind the UI canvas.
    ///
    /// Pooled by <see cref="VehicleDef.id"/> — lazily instantiated on first
    /// <see cref="Show"/>, kept inactive (not destroyed) when swapped away.
    /// Fine at today's catalog size (a handful of vehicles); a much larger
    /// catalog would want LRU eviction, not attempted here.
    /// </summary>
    [DisallowMultipleComponent]
    public class GarageVehicleDisplay : MonoBehaviour
    {
        [SerializeField] Transform turntablePoint;
        [SerializeField] float idleSpinSpeed = 12f;

        readonly Dictionary<string, GameObject> pool = new Dictionary<string, GameObject>();
        GameObject currentInstance;
        VehicleDef currentDef;
        float manualRotationOverride;
        float lastDragTime = -999f;

        /// <summary>Shows (spawning or reusing a pooled instance of) the given vehicle at the turntable.</summary>
        public void Show(VehicleDef def)
        {
            if (def == null || def.prefab == null) return;
            if (currentDef == def) { RefreshTint(); return; }

            if (currentInstance != null) currentInstance.SetActive(false);

            GameObject instance;
            if (!pool.TryGetValue(def.id, out instance) || instance == null)
            {
                instance = Instantiate(def.prefab, turntablePoint != null ? turntablePoint.position : Vector3.zero, Quaternion.identity);
                instance.name = "GarageDisplay_" + def.id;
                DisableGameplayComponents(instance);
                pool[def.id] = instance;
            }

            instance.transform.position = turntablePoint != null ? turntablePoint.position : Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.SetActive(true);
            UiJuice.PopIn(instance.transform, 0.35f);

            currentInstance = instance;
            currentDef = def;
            manualRotationOverride = 0f;
            RefreshTint();
            RefreshAttachments();
        }

        /// <summary>Re-applies the currently-shown vehicle's equipped attachments. Call this
        /// after a Store purchase/equip so the turntable updates without needing to re-show.</summary>
        public void RefreshAttachments()
        {
            if (currentInstance == null || currentDef == null) return;
            var attachments = currentInstance.GetComponent<VehicleAttachmentController>();
            if (attachments == null) attachments = currentInstance.AddComponent<VehicleAttachmentController>();
            attachments.Bind(currentDef.id);
        }

        /// <summary>
        /// Re-applies the currently-shown vehicle's equipped skin colour (same
        /// MaterialPropertyBlock path GameController.ApplyVehicle uses). Call this
        /// after a skin purchase/equip so the turntable updates without needing to
        /// re-spawn the car.
        /// </summary>
        public void RefreshTint()
        {
            if (currentInstance == null || currentDef == null) return;
            Color color = GameApp.Instance != null ? GameApp.Instance.GetEquippedColor(currentDef.id) : currentDef.bodyColor;
            var mpb = new MaterialPropertyBlock();
            foreach (var r in currentInstance.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.transform.name != "SunLineGTE") continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", color);
                mpb.SetColor("_Color", color);
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>Rotates the currently-shown car around Y by a drag delta (screen pixels).</summary>
        public void SetDragDelta(float pixelsX)
        {
            manualRotationOverride += pixelsX * 0.3f;
            lastDragTime = Time.time;
        }

        void Update()
        {
            if (currentInstance == null) return;

            // Idle auto-spin resumes a couple of seconds after the last drag.
            float spin = (Time.time - lastDragTime > 2f) ? idleSpinSpeed * Time.deltaTime : 0f;
            currentInstance.transform.Rotate(0f, spin, 0f, Space.World);

            if (!Mathf.Approximately(manualRotationOverride, 0f))
            {
                currentInstance.transform.Rotate(0f, manualRotationOverride, 0f, Space.World);
                manualRotationOverride = 0f;
            }
        }

        static void DisableGameplayComponents(GameObject instance)
        {
            var hdc = instance.GetComponent<HyperDriftCarController>();
            if (hdc != null) hdc.enabled = false;
            var input = instance.GetComponent<DriftInputSystemReader>();
            if (input != null) input.enabled = false;
            var health = instance.GetComponent<VehicleHealth>();
            if (health != null) health.enabled = false;
            var carController = instance.GetComponent<CarController>();
            if (carController != null) carController.enabled = false;
            var rb = instance.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }
}
