using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Keeps a large ground plane centered under the player so the active biome's
    /// floor colour + fog read as an infinite environment. Horizontal-only follow;
    /// the plane's Y is fixed just below the road surface. No per-frame allocation.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundFollow : MonoBehaviour
    {
        [Tooltip("Transform to follow on the XZ plane (usually the player car).")]
        [SerializeField] Transform target;

        [Tooltip("World Y the ground sits at — keep slightly below the road so it never z-fights.")]
        [SerializeField] float groundY = -0.1f;

        void LateUpdate()
        {
            if (!target) return;
            Vector3 p = target.position;
            transform.position = new Vector3(p.x, groundY, p.z);
        }
    }
}
