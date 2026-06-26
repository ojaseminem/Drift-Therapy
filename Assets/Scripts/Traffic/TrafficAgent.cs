using UnityEngine;

[DisallowMultipleComponent]
public class TrafficAgent : MonoBehaviour
{
    [SerializeField] float speedMps = 18f;
    [SerializeField] float bodyLength = 4.5f;

    Rigidbody cachedRigidbody;

    public float SpeedMps => speedMps;
    public float BodyLength => bodyLength;
    public float ArcDistance { get; private set; }
    public float LateralOffset { get; private set; }

    void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void Configure(float arcDistance, float lateralOffset, float speed)
    {
        ArcDistance = arcDistance;
        LateralOffset = lateralOffset;
        speedMps = speed;
    }

    public bool Tick(RoadSegmentPool road, float deltaTime)
    {
        ArcDistance += speedMps * deltaTime;

        if (!road.TrySampleAtArcDistance(ArcDistance, out var sample))
            return false;

        transform.SetPositionAndRotation(sample.Position + sample.Right * LateralOffset, sample.Rotation);
        return true;
    }
}
