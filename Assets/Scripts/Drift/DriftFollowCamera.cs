using UnityEngine;

/// <summary>
/// Smooth follow camera tuned for hypercasual top-down/over-shoulder drifting.
/// </summary>
[DisallowMultipleComponent]
public class DriftFollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Rigidbody targetRigidbody;

    [Header("Framing")]
    [SerializeField] Vector3 followOffset = new Vector3(0f, 9.5f, -12f);
    [SerializeField] Vector3 lookOffset = new Vector3(0f, 1.5f, 14f);
    [SerializeField] float velocityLookAhead = 0.09f;

    [Header("Smoothing")]
    [SerializeField] float positionSmoothTime = 0.18f;
    [SerializeField] float rotationLerpSpeed = 8f;

    [Header("Combo Feel (Phase 1 feedback layer)")]
    [Tooltip("Optional — the Camera to apply a combo-tier FOV boost to. Auto-resolved from this GameObject if left empty.")]
    [SerializeField] Camera cam;
    [Tooltip("Added to the camera's base FOV at max combo tier (Legend).")]
    [SerializeField] float maxFovBoost = 6f;
    [Tooltip("Camera roll (dutch angle) at max combo tier, direction follows the car's turn.")]
    [SerializeField] float maxTiltDegrees = 4f;
    [SerializeField] float comboFeelLerpSpeed = 4f;

    Vector3 cameraVelocity;
    float baseFov;
    float targetComboT;
    float currentComboT;

    void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
        if (cam) baseFov = cam.fieldOfView;
    }

    void OnEnable() => GameSignals.MultiplierChanged += HandleMultiplierChanged;
    void OnDisable() => GameSignals.MultiplierChanged -= HandleMultiplierChanged;

    /// <summary>
    /// Maps combo tier to a 0..1 camera-feel intensity — cosmetic only, mirrors
    /// GameHudUI's tier presentation but doesn't share code with it since this
    /// is gameplay-scene-only and GameHudUI is UI-prefab-only.
    /// </summary>
    void HandleMultiplierChanged(float mult, int combo)
    {
        var tier = ScoreSystem.GetComboTier(combo);
        if (tier == ScoreSystem.ComboTier.Chain) targetComboT = 0.4f;
        else if (tier == ScoreSystem.ComboTier.Inferno) targetComboT = 0.7f;
        else if (tier == ScoreSystem.ComboTier.Legend) targetComboT = 1f;
        else targetComboT = 0f;
    }

    void LateUpdate()
    {
        ResolveTargetIfNeeded();
        if (target == null)
        {
            return;
        }

        currentComboT = Mathf.MoveTowards(currentComboT, targetComboT, comboFeelLerpSpeed * Time.deltaTime);

        Vector3 velocityOffset = targetRigidbody != null ? targetRigidbody.linearVelocity * velocityLookAhead : Vector3.zero;

        Vector3 desiredPosition = target.TransformPoint(followOffset) + velocityOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref cameraVelocity, positionSmoothTime);

        Vector3 lookPoint = target.TransformPoint(lookOffset) + velocityOffset * 0.6f;
        Vector3 lookDirection = (lookPoint - transform.position).normalized;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float tiltSign = targetRigidbody != null ? -Mathf.Sign(targetRigidbody.angularVelocity.y) : 0f;
        float tiltDeg = tiltSign * maxTiltDegrees * currentComboT;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up) * Quaternion.Euler(0f, 0f, tiltDeg);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);

        if (cam)
        {
            float targetFov = baseFov + maxFovBoost * currentComboT;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, rotationLerpSpeed * Time.deltaTime);
        }
    }

    void ResolveTargetIfNeeded()
    {
        if (target != null)
        {
            if (targetRigidbody == null)
            {
                targetRigidbody = target.GetComponentInParent<Rigidbody>();
            }
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        target = player.transform;
        targetRigidbody = player.GetComponent<Rigidbody>();
    }
}
