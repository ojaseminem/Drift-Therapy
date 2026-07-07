using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Missions popup: a daily-challenge card (progress + streak + claim) at
    /// top, followed by cumulative mission rows cloned from
    /// <see cref="rowTemplate"/>. Mirrors <see cref="GaragePopup"/>'s pattern.
    /// </summary>
    public class MissionsPopup : Popup
    {
        [Header("Daily challenge")]
        public TMP_Text dailyTitleText, dailyProgressText, streakText;
        public RectTransform dailyProgressFill;
        public Button dailyClaimButton;

        [Header("Cumulative missions")]
        public Transform content;
        public GameObject rowTemplate;

        GameApp app;
        string lastClaimedId;

        void Start()
        {
            app = GameApp.Instance;
            if (rowTemplate) rowTemplate.SetActive(false);
            if (app != null) app.Changed += Populate;
            Populate();
        }

        void OnDestroy() { if (app != null) app.Changed -= Populate; }

        void Populate()
        {
            if (app == null) return;
            PopulateDaily();
            PopulateCumulative();
            lastClaimedId = null;
        }

        void PopulateDaily()
        {
            var def = app.GetMissionDef(app.Data.dailyChallengeId);
            if (def == null)
            {
                if (dailyTitleText) dailyTitleText.text = "No challenge today";
                if (dailyProgressText) dailyProgressText.text = "";
                if (dailyClaimButton) dailyClaimButton.gameObject.SetActive(false);
                if (streakText) streakText.text = "";
                return;
            }

            int progress = app.GetProgress(def.id);
            bool claimed = app.IsMissionClaimed(def.id);
            bool complete = progress >= def.targetValue;

            if (dailyTitleText) dailyTitleText.text = def.title;
            if (dailyProgressText) dailyProgressText.text = $"{Mathf.Min(progress, def.targetValue)}/{def.targetValue}";
            if (streakText) streakText.text = "STREAK " + app.Data.loginStreak;
            if (dailyProgressFill)
            {
                var a = dailyProgressFill.anchorMax;
                a.x = def.targetValue > 0 ? Mathf.Clamp01((float)progress / def.targetValue) : 0f;
                dailyProgressFill.anchorMax = a;
            }

            if (dailyClaimButton)
            {
                dailyClaimButton.gameObject.SetActive(true);
                dailyClaimButton.interactable = complete && !claimed;
                var label = dailyClaimButton.transform.Find("Label")?.GetComponent<TMP_Text>();
                if (label) label.text = claimed ? "CLAIMED" : complete ? "CLAIM" : "IN PROGRESS";
                dailyClaimButton.onClick.RemoveAllListeners();
                dailyClaimButton.onClick.AddListener(() => { lastClaimedId = def.id; app.ClaimMission(def.id); });
                if (claimed && def.id == lastClaimedId) UiJuice.PunchScale((RectTransform)dailyClaimButton.transform, 0.2f);
            }
        }

        void PopulateCumulative()
        {
            if (content == null || rowTemplate == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                if (c.gameObject != rowTemplate) Destroy(c.gameObject);
            }

            var defs = app.Missions;
            if (defs == null) return;

            int shownIndex = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                if (def == null || def.scope != MissionScope.Cumulative) continue;
                var row = Instantiate(rowTemplate, content);
                row.SetActive(true);
                FillRow(row.transform, def);
                UiJuice.StaggerIn(row.GetComponent<CanvasGroup>(), shownIndex);
                shownIndex++;
            }
        }

        void FillRow(Transform row, MissionDef def)
        {
            int progress = app.GetProgress(def.id);
            bool claimed = app.IsMissionClaimed(def.id);
            bool complete = progress >= def.targetValue;

            var title = row.Find("Title")?.GetComponent<TMP_Text>();
            if (title) title.text = def.title;

            var progressText = row.Find("ProgressText")?.GetComponent<TMP_Text>();
            if (progressText) progressText.text = $"{Mathf.Min(progress, def.targetValue)}/{def.targetValue}";

            var fill = row.Find("ProgressBar/Fill")?.GetComponent<RectTransform>();
            if (fill)
            {
                var a = fill.anchorMax;
                a.x = def.targetValue > 0 ? Mathf.Clamp01((float)progress / def.targetValue) : 0f;
                fill.anchorMax = a;
            }

            var actionT = row.Find("Action");
            var action = actionT ? actionT.GetComponent<Button>() : null;
            var label = actionT ? actionT.Find("Label")?.GetComponent<TMP_Text>() : null;
            if (label) label.text = claimed ? "CLAIMED" : complete ? "CLAIM" : "LOCKED";
            if (action)
            {
                action.interactable = complete && !claimed;
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(() => { lastClaimedId = def.id; app.ClaimMission(def.id); });
                if (claimed && def.id == lastClaimedId) UiJuice.PunchScale((RectTransform)action.transform, 0.2f);
            }
        }
    }
}
