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

    Vector3 cameraVelocity;

    void LateUpdate()
    {
        ResolveTargetIfNeeded();
        if (target == null)
        {
            return;
        }

        Vector3 velocityOffset = targetRigidbody != null ? targetRigidbody.linearVelocity * velocityLookAhead : Vector3.zero;

        Vector3 desiredPosition = target.TransformPoint(followOffset) + velocityOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref cameraVelocity, positionSmoothTime);

        Vector3 lookPoint = target.TransformPoint(lookOffset) + velocityOffset * 0.6f;
        Vector3 lookDirection = (lookPoint - transform.position).normalized;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
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
