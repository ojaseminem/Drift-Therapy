using System;

public class ScoreSystem
{
    /// <summary>Cosmetic naming tier derived from <see cref="ComboCount"/> — presentation only, does not affect scoring math.</summary>
    public enum ComboTier { None, Drift, Chain, Inferno, Legend }

    readonly float comboStepDistance;
    readonly float multiplierStep;
    readonly int nearMissBonus;
    readonly int nearMissChainStep;
    readonly float nearMissChainWindow;

    float comboDistance;
    float lastNearMissTime = float.NegativeInfinity;

    public ScoreSystem(float comboStepDistance = 25f, float multiplierStep = 0.5f, int nearMissBonus = 250,
        int nearMissChainStep = 100, float nearMissChainWindow = 3f)
    {
        this.comboStepDistance = comboStepDistance > 0f ? comboStepDistance : 25f;
        this.multiplierStep = multiplierStep > 0f ? multiplierStep : 0.5f;
        this.nearMissBonus = nearMissBonus > 0 ? nearMissBonus : 250;
        this.nearMissChainStep = Math.Max(0, nearMissChainStep);
        this.nearMissChainWindow = nearMissChainWindow > 0f ? nearMissChainWindow : 3f;
        DriftMultiplier = 1f;
    }

    public int DistanceScore { get; private set; }
    public int NearMissScore { get; private set; }
    public int FinalScore { get; private set; }
    public int BestScore { get; private set; }
    public int ComboCount { get; private set; }
    public float DriftMultiplier { get; private set; }
    /// <summary>Consecutive near-misses registered within <see cref="nearMissChainWindow"/> seconds of each other.</summary>
    public int NearMissChain { get; private set; }
    public int CurrentScore => DistanceScore + NearMissScore;
    public ComboTier CurrentComboTier => GetComboTier(ComboCount);

    public event Action ScoreChanged;

    /// <summary>Maps a combo count to its cosmetic tier. Thresholds are presentation-only.</summary>
    public static ComboTier GetComboTier(int comboCount)
    {
        if (comboCount <= 0) return ComboTier.None;
        if (comboCount < 3) return ComboTier.Drift;
        if (comboCount < 6) return ComboTier.Chain;
        if (comboCount < 10) return ComboTier.Inferno;
        return ComboTier.Legend;
    }

    public void AddDistance(float meters)
    {
        int wholeMeters = (int)Math.Floor(meters);
        if (wholeMeters <= 0)
        {
            return;
        }

        int gainedScore = (int)Math.Round(wholeMeters * DriftMultiplier, MidpointRounding.AwayFromZero);
        DistanceScore += gainedScore;
        OnScoreChanged();
    }

    public void BuildDriftCombo(float driftDistance)
    {
        if (driftDistance <= 0f)
        {
            return;
        }

        comboDistance += driftDistance;

        int newComboCount = (int)Math.Floor(comboDistance / comboStepDistance);
        if (newComboCount == ComboCount)
        {
            return;
        }

        ComboCount = newComboCount;
        DriftMultiplier = 1f + ComboCount * multiplierStep;
        OnScoreChanged();
    }

    public void ResetCombo()
    {
        if (ComboCount == 0 && Math.Abs(DriftMultiplier - 1f) < 0.001f && comboDistance <= 0f)
        {
            return;
        }

        comboDistance = 0f;
        ComboCount = 0;
        DriftMultiplier = 1f;
        OnScoreChanged();
    }

    /// <summary>
    /// Registers a near miss at <paramref name="atTime"/> (pass Time.time). Consecutive
    /// near misses within <see cref="nearMissChainWindow"/> seconds of the previous one
    /// escalate the bonus by <see cref="nearMissChainStep"/> per chain link; a gap longer
    /// than the window resets the chain to 1.
    /// </summary>
    public void RegisterNearMiss(float atTime)
    {
        NearMissChain = (atTime - lastNearMissTime) <= nearMissChainWindow ? NearMissChain + 1 : 1;
        lastNearMissTime = atTime;

        NearMissScore += nearMissBonus + (NearMissChain - 1) * nearMissChainStep;
        OnScoreChanged();
    }

    public void FinalizeRun()
    {
        FinalScore = CurrentScore;
        if (FinalScore > BestScore)
        {
            BestScore = FinalScore;
        }

        OnScoreChanged();
    }

    public void ResetRun()
    {
        DistanceScore = 0;
        NearMissScore = 0;
        FinalScore = 0;
        comboDistance = 0f;
        ComboCount = 0;
        DriftMultiplier = 1f;
        NearMissChain = 0;
        lastNearMissTime = float.NegativeInfinity;
        OnScoreChanged();
    }

    void OnScoreChanged()
    {
        ScoreChanged?.Invoke();
    }
}
