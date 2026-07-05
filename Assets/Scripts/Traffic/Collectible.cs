using UnityEngine;

/// <summary>A road-bound pickup (mechanic part) that rides the spline like traffic.</summary>
[DisallowMultipleComponent]
public class Collectible : MonoBehaviour
{
    public float ArcDistance { get; private set; }
    public float LateralOffset { get; private set; }

    public void Spawn(float arc, float offset)
    {
        ArcDistance = arc;
        LateralOffset = offset;
    }

    /// <summary>Reposition on the spline; false if past the built spline.</summary>
    public bool Advance(RoadSegmentPool road)
    {
        if (!road.TrySampleAtArcDistance(ArcDistance, out var s)) return false;
        float maxOff = Mathf.Max(0f, s.HalfWidth - 0.8f);
        Vector3 pos = s.Position + s.Right * Mathf.Clamp(LateralOffset, -maxOff, maxOff) + Vector3.up * 0.7f;
        transform.SetPositionAndRotation(pos, s.Rotation);
        return true;
    }
}
