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

    /// <summary>+1 = travels with the road (player overtakes), -1 = oncoming.</summary>
    public int Direction { get; private set; } = 1;

    void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void Configure(float arcDistance, float lateralOffset, float speed, int direction)
    {
        ArcDistance = arcDistance;
        LateralOffset = lateralOffset;
        speedMps = speed;
        Direction = direction >= 0 ? 1 : -1;
    }

    public bool Tick(RoadSegmentPool road, float deltaTime)
    {
        // Forward agents advance along the arc; oncoming agents move back toward the player.
        ArcDistance += Direction * speedMps * deltaTime;

        if (!road.TrySampleAtArcDistance(ArcDistance, out var sample))
            return false;

        // Keep the car on the road even as the road width changes underneath it.
        float maxOffset = Mathf.Max(0f, sample.HalfWidth - 0.6f);
        float offset = Mathf.Clamp(LateralOffset, -maxOffset, maxOffset);

        // Oncoming traffic faces the opposite way down the road.
        Quaternion rotation = Direction < 0
            ? sample.Rotation * Quaternion.Euler(0f, 180f, 0f)
            : sample.Rotation;

        transform.SetPositionAndRotation(sample.Position + sample.Right * offset, rotation);
        return true;
    }
}
