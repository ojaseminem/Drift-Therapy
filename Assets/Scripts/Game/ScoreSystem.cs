using System;

public class ScoreSystem
{
    readonly float comboStepDistance;
    readonly float multiplierStep;
    readonly int nearMissBonus;

    float comboDistance;

    public ScoreSystem(float comboStepDistance = 25f, float multiplierStep = 0.5f, int nearMissBonus = 250)
    {
        this.comboStepDistance = comboStepDistance > 0f ? comboStepDistance : 25f;
        this.multiplierStep = multiplierStep > 0f ? multiplierStep : 0.5f;
        this.nearMissBonus = nearMissBonus > 0 ? nearMissBonus : 250;
        DriftMultiplier = 1f;
    }

    public int DistanceScore { get; private set; }
    public int NearMissScore { get; private set; }
    public int FinalScore { get; private set; }
    public int BestScore { get; private set; }
    public int ComboCount { get; private set; }
    public float DriftMultiplier { get; private set; }
    public int CurrentScore => DistanceScore + NearMissScore;

    public event Action ScoreChanged;

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

    public void RegisterNearMiss()
    {
        NearMissScore += nearMissBonus;
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
        OnScoreChanged();
    }

    void OnScoreChanged()
    {
        ScoreChanged?.Invoke();
    }
}
