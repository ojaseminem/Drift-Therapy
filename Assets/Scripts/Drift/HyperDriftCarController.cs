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

    [Header("Damage & Boost")]
    [SerializeField] float damageSpeedPenalty = 0.30f;   // top speed fraction lost when fully wrecked
    [SerializeField] float damageSteerWobble  = 0.16f;   // involuntary steer drift when damaged
    [SerializeField] float boostSpeedBonusKph = 30f;

    float damageT;     // 0 healthy .. 1 wrecked
    float boostTimer;  // seconds of boost remaining

    public bool IsBoosting => boostTimer > 0f;
    public void SetDamage01(float t) => damageT = Mathf.Clamp01(t);
    public void ActivateBoost(float seconds) => boostTimer = Mathf.Max(boostTimer, seconds);
    float EffectiveTargetSpeed => targetSpeedKph * (1f - damageT * damageSpeedPenalty)
                                  + (IsBoosting ? boostSpeedBonusKph : 0f);

    /// <summary>Damage-capped target speed (kph). Drops only with damage, not drift dips.</summary>
    public float TargetSpeedKph => EffectiveTargetSpeed;

    CarController car;
    float steering;
    bool driftActive;

    public bool DriftActive => driftActive;
    public bool TrailFxActive
    {
        get
        {
            if (car == null || car.Wheels == null)
            {
                return false;
            }

            var wheels = car.Wheels;
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i].TrailFxActive)
                {
                    return true;
                }
            }

            return false;
        }
    }
    public float SpeedKph => car != null ? car.SpeedInHour : 0f;

    /// <summary>
    /// When false the car ignores input and applies no throttle (used to hold the
    /// car still during the pre-run countdown). Set true on "GO!".
    /// </summary>
    public bool ControlsEnabled { get; set; } = true;

    public Rigidbody RB => car != null ? car.RB : null;

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
        if (!ControlsEnabled)
        {
            // Hold position: no input, no throttle, grip restored to base.
            steering = 0f;
            driftActive = false;
            car.UpdateControls(0f, 0f, false);
            UpdateWheelGrip(false);
            return;
        }

        float targetSteer = Mathf.Clamp(inputReader.Steering, -maxSteerInput, maxSteerInput);
        steering = Mathf.MoveTowards(steering, targetSteer, steerResponse * Time.deltaTime);

        if (boostTimer > 0f) boostTimer -= Time.deltaTime;
        if (damageT > 0.01f)
            steering = Mathf.Clamp(steering + Mathf.Sin(Time.time * 5.5f) * damageT * damageSteerWobble, -1f, 1f);

        float throttle = GetThrottleForTargetSpeed();
        bool driftIntent = inputReader.IsSteeringInputActive && Mathf.Abs(steering) >= driftSteerThreshold;
        driftActive = driftIntent && car.SpeedInHour >= minSpeedForDriftKph;

        car.UpdateControls(steering, throttle, driftActive);
        UpdateWheelGrip(driftActive);
    }

    float GetThrottleForTargetSpeed()
    {
        float speedError = EffectiveTargetSpeed - car.SpeedInHour;
        if (speedError < -coastAboveTargetKph)
        {
            return 0f;
        }

        float dynamicThrottle = minThrottle + speedError * throttleGain;
        return Mathf.Clamp(dynamicThrottle, minThrottle, maxThrottle);
    }

    /// <summary>
    /// Cuts player control and kicks the rigidbody so the crash plays out physically
    /// (existing wheel/suspension physics keeps simulating — this only supplies the
    /// impact force/spin, not a full replacement of the drive model).
    /// </summary>
    public void ApplyCrashImpulse(Vector3? impactWorldPos, float impulseForce, float upwardForce, float spinTorque)
    {
        ControlsEnabled = false;
        if (car == null || car.RB == null) return;

        Vector3 awayDir = impactWorldPos.HasValue
            ? transform.position - impactWorldPos.Value
            : -transform.forward;
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.01f) awayDir = -transform.forward;
        awayDir.Normalize();

        Rigidbody rb = car.RB;
        rb.linearVelocity *= 0.35f;
        rb.AddForce(awayDir * impulseForce + Vector3.up * upwardForce, ForceMode.VelocityChange);

        Vector3 spinAxis = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1f), Random.Range(-1f, 1f)).normalized;
        rb.AddTorque(spinAxis * spinTorque, ForceMode.VelocityChange);
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
