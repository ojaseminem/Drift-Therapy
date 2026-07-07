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

        score.RegisterNearMiss(0f);

        Assert.That(score.NearMissScore, Is.EqualTo(300));
        Assert.That(score.CurrentScore, Is.EqualTo(300));
        Assert.That(changedCount, Is.EqualTo(1));
    }

    [Test]
    public void FinalizeRun_CapturesFinalScoreAndRetainsBestAcrossRuns()
    {
        var score = new ScoreSystem(nearMissBonus: 100);

        score.AddDistance(50f);
        score.RegisterNearMiss(0f);
        score.FinalizeRun();

        Assert.That(score.FinalScore, Is.EqualTo(150));
        Assert.That(score.BestScore, Is.EqualTo(150));

        score.ResetRun();
        score.AddDistance(40f);
        score.FinalizeRun();

        Assert.That(score.FinalScore, Is.EqualTo(40));
        Assert.That(score.BestScore, Is.EqualTo(150));
    }

    [Test]
    public void RegisterNearMiss_WithinChainWindow_EscalatesBonusAndChainCount()
    {
        var score = new ScoreSystem(nearMissBonus: 100, nearMissChainStep: 50, nearMissChainWindow: 3f);

        score.RegisterNearMiss(0f);   // chain 1: +100
        score.RegisterNearMiss(1f);   // chain 2: +150 (within 3s window)
        score.RegisterNearMiss(2f);   // chain 3: +200

        Assert.That(score.NearMissChain, Is.EqualTo(3));
        Assert.That(score.NearMissScore, Is.EqualTo(100 + 150 + 200));
    }

    [Test]
    public void RegisterNearMiss_AfterChainWindowExpires_ResetsChainToOne()
    {
        var score = new ScoreSystem(nearMissBonus: 100, nearMissChainStep: 50, nearMissChainWindow: 3f);

        score.RegisterNearMiss(0f);    // chain 1
        score.RegisterNearMiss(10f);   // gap > window: chain resets to 1

        Assert.That(score.NearMissChain, Is.EqualTo(1));
        Assert.That(score.NearMissScore, Is.EqualTo(100 + 100));
    }

    [Test]
    public void ResetRun_ClearsNearMissChain()
    {
        var score = new ScoreSystem();
        score.RegisterNearMiss(0f);
        score.RegisterNearMiss(1f);

        score.ResetRun();

        Assert.That(score.NearMissChain, Is.EqualTo(0));
    }

    [TestCase(0, ScoreSystem.ComboTier.None)]
    [TestCase(2, ScoreSystem.ComboTier.Drift)]
    [TestCase(5, ScoreSystem.ComboTier.Chain)]
    [TestCase(9, ScoreSystem.ComboTier.Inferno)]
    [TestCase(10, ScoreSystem.ComboTier.Legend)]
    public void GetComboTier_MapsComboCountToExpectedTier(int comboCount, ScoreSystem.ComboTier expected)
    {
        Assert.That(ScoreSystem.GetComboTier(comboCount), Is.EqualTo(expected));
    }
}
