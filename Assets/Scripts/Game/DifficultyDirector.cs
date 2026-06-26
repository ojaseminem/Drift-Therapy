using UnityEngine;

/// <summary>
/// Drives the run's normalised difficulty (0..1) from distance travelled and
/// pushes it into the road system. Other systems may read the exposed
/// multipliers to scale their own behaviour (e.g. traffic density, car speed).
///
/// Driven by <c>GameController</c> via <see cref="Tick"/> on the gameplay loop;
/// it does not run its own Update so the controller stays the single source of
/// truth for run lifecycle.
/// </summary>
[DisallowMultipleComponent]
public class DifficultyDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RoadSegmentPool road;

    [Header("Tuning")]
    [Tooltip("Distance in metres at which difficulty reaches its maximum (t = 1).")]
    [SerializeField] float fullDifficultyDistance = 2500f;

    [Tooltip("Optional remap of the linear 0..1 ramp. Leave as default linear if unset.")]
    [SerializeField] AnimationCurve difficultyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Multipliers")]
    [SerializeField] float maxSpeedMultiplier = 1.35f;
    [SerializeField] float maxTrafficDensityMultiplier = 2f;

    const float ChangeEpsilon = 0.001f;

    float currentT;
    bool hasPushed;

    /// <summary>Current normalised difficulty in the 0..1 range.</summary>
    public float CurrentT => currentT;

    /// <summary>Suggested speed scale (lerps 1 → <c>maxSpeedMultiplier</c> by difficulty).</summary>
    public float SpeedMultiplier => Mathf.Lerp(1f, maxSpeedMultiplier, currentT);

    /// <summary>Suggested traffic density scale (lerps 1 → <c>maxTrafficDensityMultiplier</c>).</summary>
    public float TrafficDensityMultiplier => Mathf.Lerp(1f, maxTrafficDensityMultiplier, currentT);

    /// <summary>
    /// Recomputes difficulty from the current distance. When the value changes
    /// meaningfully it is written to the road and broadcast via GameSignals.
    /// Allocation-free.
    /// </summary>
    public void Tick(float distanceMetres)
    {
        float denom = fullDifficultyDistance > 0f ? fullDifficultyDistance : 1f;
        float linear = Mathf.Clamp01(distanceMetres / denom);
        float t = Mathf.Clamp01(difficultyCurve != null ? difficultyCurve.Evaluate(linear) : linear);

        if (hasPushed && Mathf.Abs(t - currentT) < ChangeEpsilon)
        {
            return;
        }

        currentT = t;
        hasPushed = true;

        if (road != null)
        {
            road.DifficultyT = t;
        }

        GameSignals.RaiseDifficultyChanged(t);
    }

    /// <summary>Resets difficulty back to zero (used on restart).</summary>
    public void ResetDifficulty()
    {
        currentT = 0f;
        hasPushed = false;

        if (road != null)
        {
            road.DifficultyT = 0f;
        }

        GameSignals.RaiseDifficultyChanged(0f);
    }
}
