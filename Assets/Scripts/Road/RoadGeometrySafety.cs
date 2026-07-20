using UnityEngine;

/// <summary>
/// Shared curvature-safety helper for anything that offsets sideways from the road
/// centerline (fence, scattered props). On a tight turn — hairpins are a deliberate
/// gameplay feature of RoadCurveGenerator, not something to eliminate — a fixed
/// sideways offset can "fold" across onto the road's inside if that offset approaches
/// or exceeds the road's actual instantaneous turn radius there. This estimates that
/// radius from two nearby spline samples so callers can clamp how far past the road
/// edge they reach, without touching curve generation itself.
/// </summary>
public static class RoadGeometrySafety
{
    /// <summary>Instantaneous turn radius estimated from the rotation delta between two
    /// samples an arcSpan metres apart. float.MaxValue on a straight (no measurable turn).</summary>
    public static float EstimateRadius(Quaternion rotA, Quaternion rotB, float arcSpan)
    {
        float angleDeg = Quaternion.Angle(rotA, rotB);
        if (angleDeg < 0.05f || arcSpan <= 0f) return float.MaxValue;
        return arcSpan / (angleDeg * Mathf.Deg2Rad);
    }

    /// <summary>
    /// Clamps how far past the road edge (halfWidth) something can safely reach sideways
    /// without its geometry/placement folding across the road on a tight turn. safetyFactor
    /// leaves margin (e.g. 0.85 = use at most 85% of the spare radius beyond the road edge).
    /// </summary>
    public static float ClampReachBeyondEdge(float desiredExtra, float halfWidth, float localRadius, float safetyFactor = 0.85f)
    {
        float spareRadius = Mathf.Max(0.5f, localRadius - halfWidth);
        return Mathf.Min(desiredExtra, spareRadius * safetyFactor);
    }
}
