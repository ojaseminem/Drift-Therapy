using NUnit.Framework;

public class ScoreSystemTests
{
    [Test]
    public void AddDistance_AddsWholeMetersToDistanceScore()
    {
        var score = new ScoreSystem();

        score.AddDistance(12.9f);

        Assert.That(score.DistanceScore, Is.EqualTo(12));
        Assert.That(score.CurrentScore, Is.EqualTo(12));
    }

    [Test]
    public void BuildDriftCombo_IncreasesMultiplierAndBoostsFutureDistance()
    {
        var score = new ScoreSystem(comboStepDistance: 20f, multiplierStep: 0.5f);

        score.BuildDriftCombo(45f);
        score.AddDistance(10f);

        Assert.That(score.ComboCount, Is.EqualTo(2));
        Assert.That(score.DriftMultiplier, Is.EqualTo(2f));
        Assert.That(score.DistanceScore, Is.EqualTo(20));
    }

    [Test]
    public void ResetCombo_RestoresBaseMultiplierForLaterScoring()
    {
        var score = new ScoreSystem(comboStepDistance: 10f, multiplierStep: 0.5f);

        score.BuildDriftCombo(25f);
        score.ResetCombo();
        score.AddDistance(10f);

        Assert.That(score.ComboCount, Is.EqualTo(0));
        Assert.That(score.DriftMultiplier, Is.EqualTo(1f));
        Assert.That(score.DistanceScore, Is.EqualTo(10));
    }

    [Test]
    public void RegisterNearMiss_AddsBonusAndRaisesScoreChanged()
    {
        var score = new ScoreSystem(nearMissBonus: 300);
        int changedCount = 0;

        score.ScoreChanged += () => changedCount++;

        score.RegisterNearMiss();

        Assert.That(score.NearMissScore, Is.EqualTo(300));
        Assert.That(score.CurrentScore, Is.EqualTo(300));
        Assert.That(changedCount, Is.EqualTo(1));
    }

    [Test]
    public void FinalizeRun_CapturesFinalScoreAndRetainsBestAcrossRuns()
    {
        var score = new ScoreSystem(nearMissBonus: 100);

        score.AddDistance(50f);
        score.RegisterNearMiss();
        score.FinalizeRun();

        Assert.That(score.FinalScore, Is.EqualTo(150));
        Assert.That(score.BestScore, Is.EqualTo(150));

        score.ResetRun();
        score.AddDistance(40f);
        score.FinalizeRun();

        Assert.That(score.FinalScore, Is.EqualTo(40));
        Assert.That(score.BestScore, Is.EqualTo(150));
    }
}
