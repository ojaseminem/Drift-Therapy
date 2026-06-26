using PG_Physics.Wheel;
using UnityEngine;

/// <summary>
/// Endless hypercasual drift controller built on top of ACC_Lite CarController physics.
/// Keeps forward speed mostly constant and converts steer input into drift-style handling.
/// </summary>
[RequireComponent(typeof(CarController))]
[DisallowMultipleComponent]
public class HyperDriftCarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] DriftInputSystemReader inputReader;

    [Header("Speed")]
    [SerializeField] float targetSpeedKph = 95f;
    [SerializeField] float minThrottle = 0.22f;
    [SerializeField] float maxThrottle = 1f;
    [SerializeField] float throttleGain = 0.018f;
    [SerializeField] float coastAboveTargetKph = 8f;

    [Header("Steering")]
    [SerializeField] float steerResponse = 8f;
    [SerializeField] float maxSteerInput = 1f;

    [Header("Drift")]
    [SerializeField] float minSpeedForDriftKph = 22f;
    [SerializeField] float driftSteerThreshold = 0.12f;
    [SerializeField] float frontSideGripInDrift = 1.05f;
    [SerializeField] float rearSideGripInDrift = 0.62f;
    [SerializeField] float frontForwardGripInDrift = 0.95f;
    [SerializeField] float rearForwardGripInDrift = 0.78f;
    [SerializeField] float gripLerpSpeed = 7f;

    [Header("Compatibility")]
    [SerializeField] bool disableLegacyUserControl = true;

    CarController car;
    float steering;
    bool driftActive;

    public bool DriftActive => driftActive;
    public float SpeedKph => car != null ? car.SpeedInHour : 0f;

    PG_WheelCollider[] wheelAdapters;
    float[] baseForwardStiffness;
    float[] baseSideStiffness;
    float[] currentForwardStiffness;
    float[] currentSideStiffness;

    void Awake()
    {
        car = GetComponent<CarController>();
        if (inputReader == null)
        {
            inputReader = GetComponent<DriftInputSystemReader>();
        }

        if (inputReader == null)
        {
            inputReader = gameObject.AddComponent<DriftInputSystemReader>();
        }

        if (disableLegacyUserControl)
        {
            var oldControl = GetComponent<UserControl>();
            if (oldControl != null)
            {
                oldControl.enabled = false;
            }
        }

        CacheWheelStiffness();
    }

    void Update()
    {
        float targetSteer = Mathf.Clamp(inputReader.Steering, -maxSteerInput, maxSteerInput);
        steering = Mathf.MoveTowards(steering, targetSteer, steerResponse * Time.deltaTime);

        float throttle = GetThrottleForTargetSpeed();
        bool driftIntent = inputReader.IsSteeringInputActive && Mathf.Abs(steering) >= driftSteerThreshold;
        driftActive = driftIntent && car.SpeedInHour >= minSpeedForDriftKph;

        car.UpdateControls(steering, throttle, driftActive);
        UpdateWheelGrip(driftActive);
    }

    float GetThrottleForTargetSpeed()
    {
        float speedError = targetSpeedKph - car.SpeedInHour;
        if (speedError < -coastAboveTargetKph)
        {
            return 0f;
        }

        float dynamicThrottle = minThrottle + speedError * throttleGain;
        return Mathf.Clamp(dynamicThrottle, minThrottle, maxThrottle);
    }

    void CacheWheelStiffness()
    {
        var wheels = car.Wheels;
        if (wheels == null || wheels.Length == 0)
        {
            return;
        }

        int count = wheels.Length;
        wheelAdapters = new PG_WheelCollider[count];
        baseForwardStiffness = new float[count];
        baseSideStiffness = new float[count];
        currentForwardStiffness = new float[count];
        currentSideStiffness = new float[count];

        for (int i = 0; i < count; i++)
        {
            var adapter = wheels[i].PG_WheelCollider;
            var wc = wheels[i].WheelCollider;
            wheelAdapters[i] = adapter;
            baseForwardStiffness[i] = wc.forwardFriction.stiffness;
            baseSideStiffness[i] = wc.sidewaysFriction.stiffness;
            currentForwardStiffness[i] = baseForwardStiffness[i];
            currentSideStiffness[i] = baseSideStiffness[i];
        }
    }

    void UpdateWheelGrip(bool inDrift)
    {
        if (wheelAdapters == null || wheelAdapters.Length == 0)
        {
            return;
        }

        for (int i = 0; i < wheelAdapters.Length; i++)
        {
            bool isFront = i < 2;

            float forwardMult = 1f;
            float sideMult = 1f;
            if (inDrift)
            {
                forwardMult = isFront ? frontForwardGripInDrift : rearForwardGripInDrift;
                sideMult = isFront ? frontSideGripInDrift : rearSideGripInDrift;
            }

            float targetForward = baseForwardStiffness[i] * forwardMult;
            float targetSide = baseSideStiffness[i] * sideMult;

            currentForwardStiffness[i] = Mathf.MoveTowards(currentForwardStiffness[i], targetForward, gripLerpSpeed * Time.deltaTime);
            currentSideStiffness[i] = Mathf.MoveTowards(currentSideStiffness[i], targetSide, gripLerpSpeed * Time.deltaTime);

            wheelAdapters[i].UpdateStiffness(currentForwardStiffness[i], currentSideStiffness[i]);
        }
    }
}
