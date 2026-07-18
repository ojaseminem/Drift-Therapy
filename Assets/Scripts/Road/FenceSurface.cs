using UnityEngine;

/// <summary>
/// Marker component identifying a collider as roadside fence, so
/// <see cref="FenceCollisionDetector"/> can tell fence contacts apart from
/// traffic/road/other geometry without relying on the Unity tag database.
/// </summary>
public class FenceSurface : MonoBehaviour
{
}
