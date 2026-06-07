using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintains a Catmull-Rom spline of SplineNodes and provides continuous world-space sampling.
/// Pure data — no MonoBehaviour, no Unity rendering dependencies.
/// </summary>
public class SplineRoadBuilder
{
    public readonly struct SplineNode
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float Width;     // road half-width in metres
        public readonly float Banking;   // roll in degrees (positive = left side up)
        public readonly float Pitch;     // nose-up angle in degrees

        public SplineNode(Vector3 position, Quaternion rotation, float width = 5f, float banking = 0f, float pitch = 0f)
        {
            Position = position;
            Rotation = rotation;
            Width = width;
            Banking = banking;
            Pitch = pitch;
        }
    }

    readonly List<SplineNode> nodes = new List<SplineNode>();

    public int NodeCount => nodes.Count;

    public void AddNode(SplineNode node) => nodes.Add(node);

    public void RemoveFirstNode()
    {
        if (nodes.Count > 0) nodes.RemoveAt(0);
    }

    public SplineNode GetNode(int index) => nodes[index];

    /// <summary>
    /// Sample the spline at a normalised t across all segments (0 = first node, NodeCount-1 = last node).
    /// Returns false if fewer than 2 nodes exist.
    /// </summary>
    public bool SampleAt(float t, out Vector3 position, out Quaternion rotation, out float width, out float banking)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        width = 5f;
        banking = 0f;

        if (nodes.Count < 2) return false;

        t = Mathf.Clamp(t, 0f, nodes.Count - 1);
        int i = Mathf.Min((int)t, nodes.Count - 2);
        float localT = t - i;

        // Catmull-Rom control points
        int i0 = Mathf.Max(i - 1, 0);
        int i1 = i;
        int i2 = Mathf.Min(i + 1, nodes.Count - 1);
        int i3 = Mathf.Min(i + 2, nodes.Count - 1);

        position = CatmullRom(nodes[i0].Position, nodes[i1].Position, nodes[i2].Position, nodes[i3].Position, localT);

        // Interpolate rotation, width, banking
        Quaternion r1 = nodes[i1].Rotation;
        Quaternion r2 = nodes[i2].Rotation;
        float w1 = nodes[i1].Width;
        float w2 = nodes[i2].Width;
        float b1 = nodes[i1].Banking;
        float b2 = nodes[i2].Banking;

        rotation = Quaternion.Slerp(r1, r2, localT);
        width = Mathf.Lerp(w1, w2, localT);
        banking = Mathf.Lerp(b1, b2, localT);

        return true;
    }

    /// <summary>
    /// Estimate the total arc length of the spline using stepped sampling.
    /// </summary>
    public float EstimateArcLength(int stepsPerSegment = 10)
    {
        if (nodes.Count < 2) return 0f;

        float total = 0f;
        int totalSteps = (nodes.Count - 1) * stepsPerSegment;
        Vector3 prev = nodes[0].Position;

        for (int step = 1; step <= totalSteps; step++)
        {
            float t = (float)step / stepsPerSegment;
            SampleAt(t, out Vector3 pos, out _, out _, out _);
            total += Vector3.Distance(prev, pos);
            prev = pos;
        }
        return total;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}
