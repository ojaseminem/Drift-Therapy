using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// "TOP RUNS" popup — local top-10 list from <see cref="LeaderboardProvider"/>.
    /// Named "TOP RUNS" rather than "Leaderboard"/"Global" because it's a
    /// single-device list until Play Games Services lands (Phase 3); renaming
    /// it then is a one-line title change, not a data-model change.
    /// Mirrors <see cref="GaragePopup"/>'s row-clone pattern.
    /// </summary>
    public class LeaderboardPopup : Popup
    {
        public Transform content;
        public GameObject rowTemplate;

        [Header("Online (Play Games) — hidden until available")]
        public Button viewGlobalButton;

        GameApp app;

        void Start()
        {
            app = GameApp.Instance;
            if (rowTemplate) rowTemplate.SetActive(false);

            if (viewGlobalButton)
            {
                viewGlobalButton.gameObject.SetActive(PlatformServices.PlayGames.IsAvailable);
                viewGlobalButton.onClick.RemoveAllListeners();
                viewGlobalButton.onClick.AddListener(() => PlatformServices.PlayGames.ShowLeaderboardUI("top_runs"));
            }

            Populate();
        }

        void Populate()
        {
            if (content == null || rowTemplate == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                if (c.gameObject != rowTemplate) Destroy(c.gameObject);
            }

            var top = LeaderboardProvider.Current.GetTop(10);
            float latestDistance = app != null ? app.LastRun.distanceMeters : -1f;
            bool highlightedOnce = false;

            for (int i = 0; i < top.Count; i++)
            {
                var entry = top[i];
                var row = Instantiate(rowTemplate, content);
                row.SetActive(true);

                bool highlight = !highlightedOnce && latestDistance >= 0f &&
                    Mathf.Approximately(entry.distanceMeters, latestDistance);
                if (highlight) highlightedOnce = true;

                FillRow(row.transform, i + 1, entry, highlight);
                UiJuice.StaggerIn(row.GetComponent<CanvasGroup>(), i);
            }
        }

        void FillRow(Transform row, int rank, LeaderboardEntry entry, bool highlight)
        {
            var rankText = row.Find("Rank")?.GetComponent<TMP_Text>();
            if (rankText) rankText.text = "#" + rank;

            var swatch = row.Find("Swatch")?.GetComponent<Image>();
            if (swatch)
            {
                var vehicle = app != null ? app.GetVehicle(entry.vehicleId) : null;
                swatch.color = vehicle != null ? app.GetEquippedColor(vehicle.id) : Color.white;
            }

            var distanceText = row.Find("Distance")?.GetComponent<TMP_Text>();
            if (distanceText) distanceText.text = Mathf.FloorToInt(entry.distanceMeters) + " m";

            var dateText = row.Find("Date")?.GetComponent<TMP_Text>();
            if (dateText) dateText.text = entry.dateUtc;

            // Highlight the row matching the most recent run, if it made the top list.
            var bg = row.GetComponent<Image>();
            if (bg && highlight) bg.color = HighlightColor;
        }

        static readonly Color HighlightColor = new Color(1f, 0.80f, 0.20f, 0.35f);
    }
}
