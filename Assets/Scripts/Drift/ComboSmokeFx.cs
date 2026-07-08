using UnityEngine;

/// <summary>
/// Tints the shared asphalt smoke particle system by combo tier. Purely
/// additive — reads <see cref="FXController"/>'s already-public
/// <c>GetAspahaltParticles</c> property and adjusts the shared
/// <see cref="ParticleSystem"/>'s main-module color/size between emits. Does
/// NOT modify any vendored ACC_Lite file: <c>Wheel.cs</c> still owns
/// emission (one <c>Emit(1)</c> call per slipping wheel per frame) — this
/// only changes what those particles look like once they're born.
/// </summary>
[DisallowMultipleComponent]
public class ComboSmokeFx : MonoBehaviour
{
    [SerializeField] Color baseColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);
    [SerializeField] Color legendTintColor = new Color(1f, 0.55f, 0.15f, 0.7f);
    [SerializeField] float maxSizeMultiplier = 1.35f;
    [SerializeField] float lerpSpeed = 3f;

    float targetT;
    float currentT;

    void OnEnable() => GameSignals.MultiplierChanged += HandleMultiplierChanged;
    void OnDisable() => GameSignals.MultiplierChanged -= HandleMultiplierChanged;

    void HandleMultiplierChanged(float mult, int combo)
    {
        var tier = ScoreSystem.GetComboTier(combo);
        if (tier == ScoreSystem.ComboTier.Chain) targetT = 0.35f;
        else if (tier == ScoreSystem.ComboTier.Inferno) targetT = 0.7f;
        else if (tier == ScoreSystem.ComboTier.Legend) targetT = 1f;
        else targetT = 0f;
    }

    void Update()
    {
        if (Mathf.Approximately(currentT, targetT)) return;
        currentT = Mathf.MoveTowards(currentT, targetT, lerpSpeed * Time.deltaTime);

        var particles = FXController.Instance != null ? FXController.Instance.GetAspahaltParticles : null;
        if (particles == null) return;

        var main = particles.main;
        main.startColor = Color.Lerp(baseColor, legendTintColor, currentT);
        main.startSizeMultiplier = Mathf.Lerp(1f, maxSizeMultiplier, currentT);
    }
}
