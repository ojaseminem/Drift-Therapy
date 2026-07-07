using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Independent <see cref="GameSignals"/> listener (same pattern as
    /// <see cref="GameHudUI"/>) that tracks this run's peak values and commits
    /// them to <see cref="GameApp"/> at run end. Zero coupling to
    /// <see cref="GameController"/> internals — place alongside it in the
    /// DriftEndless scene.
    ///
    /// Resets on <see cref="GameSignals.RunReady"/> (fired only by a fresh
    /// countdown, not by a revive) so a mid-run revive continues accumulating
    /// toward the same trial/mission progress instead of resetting it.
    /// </summary>
    [DisallowMultipleComponent]
    public class MissionTracker : MonoBehaviour
    {
        int nearMissCountThisRun;
        int peakComboThisRun;
        float peakDistanceThisRun;
        int coinsAtRunStart;
        int lastSeenCoinsTotal;

        void OnEnable()
        {
            GameSignals.RunReady += HandleRunReady;
            GameSignals.DistanceChanged += HandleDistanceChanged;
            GameSignals.NearMiss += HandleNearMiss;
            GameSignals.MultiplierChanged += HandleMultiplierChanged;
            GameSignals.DriftCoinsChanged += HandleDriftCoinsChanged;
            GameSignals.RunFailed += HandleRunFailed;
        }

        void OnDisable()
        {
            GameSignals.RunReady -= HandleRunReady;
            GameSignals.DistanceChanged -= HandleDistanceChanged;
            GameSignals.NearMiss -= HandleNearMiss;
            GameSignals.MultiplierChanged -= HandleMultiplierChanged;
            GameSignals.DriftCoinsChanged -= HandleDriftCoinsChanged;
            GameSignals.RunFailed -= HandleRunFailed;
        }

        void HandleRunReady()
        {
            nearMissCountThisRun = 0;
            peakComboThisRun = 0;
            peakDistanceThisRun = 0f;
            coinsAtRunStart = lastSeenCoinsTotal;
        }

        void HandleDistanceChanged(float metres) => peakDistanceThisRun = metres;
        void HandleNearMiss(int chain) => nearMissCountThisRun++;
        void HandleMultiplierChanged(float mult, int combo) => peakComboThisRun = Mathf.Max(peakComboThisRun, combo);

        // Only the committed `total` is meaningful here — `pending` changes every
        // Update() frame while drifting and must never be read as a discrete event.
        void HandleDriftCoinsChanged(int total, int pending) => lastSeenCoinsTotal = total;

        void HandleRunFailed(string reason)
        {
            if (GameApp.Instance == null) return;

            int peakDistance = Mathf.RoundToInt(peakDistanceThisRun);
            int coinsEarnedThisRun = Mathf.Max(0, lastSeenCoinsTotal - coinsAtRunStart);

            GameApp.Instance.ReportRunTrialPeak(MissionMetric.DistanceMeters, peakDistance);
            GameApp.Instance.ReportRunTrialPeak(MissionMetric.NearMisses, nearMissCountThisRun);
            GameApp.Instance.ReportRunTrialPeak(MissionMetric.DriftCombo, peakComboThisRun);

            GameApp.Instance.ReportMetric(MissionMetric.DistanceMeters, peakDistance);
            GameApp.Instance.ReportMetric(MissionMetric.CoinsEarned, coinsEarnedThisRun);
            GameApp.Instance.ReportMetric(MissionMetric.NearMisses, nearMissCountThisRun);
            GameApp.Instance.ReportMetric(MissionMetric.RunsCompleted, 1);
        }
    }
}
