using UnityEngine;

/// <summary>
/// A single traffic vehicle. Pure data + transform follower — all decisions
/// (lane, speed, spacing) are made by the <see cref="TrafficDirector"/>. The
/// agent just advances along the road spline each frame and keeps itself inside
/// its lane. Kinematic, allocation-free.
/// </summary>
[DisallowMultipleComponent]
public class TrafficAgent : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId     = Shader.PropertyToID("_Color");

    Rigidbody body;
    Renderer[] renderers;
    MaterialPropertyBlock mpb;

    public TrafficVehicleType Type { get; private set; }
    public int   Direction   { get; private set; } = 1;   // +1 with road, -1 oncoming
    public float ArcDistance { get; private set; }
    public float LaneOffset  { get; private set; }
    public float Speed       { get; private set; }
    public float Length      => Type != null ? Type.length : 4.5f;
    public float HalfWidth   => Type != null ? Type.halfWidth : 0.9f;

    /// <summary>One-time setup when the instance is created by the pool.</summary>
    public void Init(TrafficVehicleType type)
    {
        Type = type;
        body = GetComponent<Rigidbody>();
        if (body)
        {
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }
        renderers = GetComponentsInChildren<Renderer>(true);
        mpb = new MaterialPropertyBlock();
    }

    /// <summary>Place the agent at the start of its life.</summary>
    public void Spawn(float arcDistance, float laneOffset, int direction, float speed)
    {
        ArcDistance = arcDistance;
        LaneOffset  = laneOffset;
        Direction   = direction >= 0 ? 1 : -1;
        Speed       = speed;
    }

    /// <summary>Tint the body via MaterialPropertyBlock (no material instances).</summary>
    public void ApplyColor(Color color)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, color);
            mpb.SetColor(ColorId, color);
            r.SetPropertyBlock(mpb);
        }
    }

    /// <summary>
    /// Advance along the road. <paramref name="speedCap"/>, when set, limits speed
    /// this frame (used for follow-gap behaviour). Returns false if the agent fell
    /// off the end of the built spline and should be recycled.
    /// </summary>
    public bool Advance(RoadSegmentPool road, float deltaTime, float? speedCap)
    {
        float v = speedCap.HasValue ? Mathf.Min(Speed, speedCap.Value) : Speed;
        ArcDistance += Direction * v * deltaTime;

        if (!road.TrySampleAtArcDistance(ArcDistance, out var sample))
            return false;

        float maxOffset = Mathf.Max(0f, sample.HalfWidth - HalfWidth - 0.1f);
        float offset = Mathf.Clamp(LaneOffset, -maxOffset, maxOffset);

        Quaternion rotation = Direction < 0
            ? sample.Rotation * Quaternion.Euler(0f, 180f, 0f)
            : sample.Rotation;

        transform.SetPositionAndRotation(sample.Position + sample.Right * offset, rotation);
        return true;
    }
}
