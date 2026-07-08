using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Fits this RectTransform to <see cref="Screen.safeArea"/> so UI content
    /// avoids notches, cutouts, and gesture-bar insets. All screen content
    /// should live under a RectTransform carrying this component (see
    /// <c>UIBuilder.NewScreen</c>, which parents everything under one
    /// automatically — no other file needs to know this exists).
    ///
    /// Re-applies whenever the safe area actually changes (rotation, foldable
    /// hinge) rather than every frame — the per-frame check is a handful of
    /// float/int comparisons, no allocation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rt;
        Rect lastSafeArea;
        Vector2Int lastScreenSize;
        ScreenOrientation lastOrientation;

        void Awake()
        {
            rt = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != lastSafeArea ||
                Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y ||
                Screen.orientation != lastOrientation)
            {
                Apply();
            }
        }

        void Apply()
        {
            var safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // A valid safe-area anchor can never legitimately fall outside 0..1 —
            // clamp defensively. Found in practice: Unity's Device Simulator can
            // report Screen.safeArea in native device pixels while Screen.width/
            // height report the simulator's scaled preview resolution, producing
            // anchors like 2.9 instead of ~1.0. Clamping falls back to "no inset"
            // rather than a badly oversized layout when that happens, and is a
            // no-op on real devices where the two are always consistent.
            anchorMin.x = Mathf.Clamp01(anchorMin.x);
            anchorMin.y = Mathf.Clamp01(anchorMin.y);
            anchorMax.x = Mathf.Clamp01(anchorMax.x);
            anchorMax.y = Mathf.Clamp01(anchorMax.y);

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
