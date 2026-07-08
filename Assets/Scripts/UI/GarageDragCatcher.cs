using UnityEngine;
using UnityEngine.EventSystems;

namespace DriftTherapy
{
    /// <summary>
    /// Sits over the 3D viewport region of the Garage screen (spatially above
    /// the card carousel, never overlapping it) and forwards horizontal drag
    /// deltas to <see cref="GarageVehicleDisplay.SetDragDelta"/> to rotate the
    /// currently-shown car.
    /// </summary>
    [DisallowMultipleComponent]
    public class GarageDragCatcher : MonoBehaviour, IDragHandler
    {
        public GarageVehicleDisplay display;

        public void OnDrag(PointerEventData eventData)
        {
            if (display != null) display.SetDragDelta(eventData.delta.x);
        }
    }
}
