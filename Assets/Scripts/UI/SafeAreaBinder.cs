using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Applies the device <see cref="Screen.safeArea"/> (notch / home-indicator
/// insets) as padding on a UI Toolkit root element so HUD/overlay content
/// never sits under a notch or rounded corner.
///
/// UI Toolkit panels are laid out in "panel coordinates" which, with the
/// default <c>PanelSettings</c> scale mode, map 1:1 to screen pixels. We
/// convert the safe-area rect (in screen pixels, origin bottom-left) into
/// top/left/right/bottom insets and write them as inline padding. This is a
/// reasonable approximation; for a reference-resolution scaler the values are
/// proportional and remain visually correct.
/// </summary>
public static class SafeAreaBinder
{
    /// <summary>
    /// Applies the current safe-area insets as padding to <paramref name="root"/>.
    /// Safe to call repeatedly (e.g. on orientation change); it overwrites the
    /// inline padding each time.
    /// </summary>
    public static void Apply(VisualElement root)
    {
        if (root == null)
        {
            return;
        }

        Rect safe = Screen.safeArea;
        float screenW = Mathf.Max(1f, Screen.width);
        float screenH = Mathf.Max(1f, Screen.height);

        // Screen.safeArea has origin at bottom-left; convert to top/bottom insets.
        float left = safe.xMin;
        float right = screenW - safe.xMax;
        float bottom = safe.yMin;
        float top = screenH - safe.yMax;

        // Use the resolved panel size when available to map pixels → panel units;
        // fall back to 1:1 (matches default PanelSettings) before first layout.
        float panelW = root.resolvedStyle.width;
        float panelH = root.resolvedStyle.height;
        float scaleX = (panelW > 0f) ? panelW / screenW : 1f;
        float scaleY = (panelH > 0f) ? panelH / screenH : 1f;

        root.style.paddingLeft = left * scaleX;
        root.style.paddingRight = right * scaleX;
        root.style.paddingTop = top * scaleY;
        root.style.paddingBottom = bottom * scaleY;
    }
}
