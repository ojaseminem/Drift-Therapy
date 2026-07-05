using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Vehicles popup: lists vehicles, buy/equip. Cloned rows from <see cref="rowTemplate"/>.
    /// </summary>
    public class GaragePopup : Popup
    {
        public Transform content;
        public GameObject rowTemplate;

        GameApp app;

        void Start()
        {
            app = GameApp.Instance;
            if (rowTemplate) rowTemplate.SetActive(false);
            Populate();
        }

        void Populate()
        {
            if (app == null || content == null || rowTemplate == null) return;

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var c = content.GetChild(i);
                if (c.gameObject != rowTemplate) Destroy(c.gameObject);
            }

            var vehicles = app.Vehicles;
            for (int i = 0; i < vehicles.Count; i++)
            {
                var v = vehicles[i];
                if (v == null) continue;
                var row = Instantiate(rowTemplate, content);
                row.SetActive(true);
                FillRow(row.transform, v);
            }
        }

        void FillRow(Transform row, VehicleDef v)
        {
            var swatch = row.Find("Swatch")?.GetComponent<Image>();
            if (swatch) swatch.color = v.bodyColor;
            var name = row.Find("Name")?.GetComponent<TMP_Text>();
            if (name) name.text = v.displayName;

            var actionT = row.Find("Action");
            var action = actionT ? actionT.GetComponent<Button>() : null;
            var label = actionT ? actionT.Find("Label")?.GetComponent<TMP_Text>() : null;

            bool owned = app.Owns(v.id);
            bool equipped = app.Data.selectedVehicleId == v.id;
            if (label) label.text = equipped ? "EQUIPPED" : owned ? "EQUIP" : v.price + " BUY";
            if (action)
            {
                action.interactable = !equipped;
                action.onClick.RemoveAllListeners();
                action.onClick.AddListener(() => OnRow(v));
            }
        }

        void OnRow(VehicleDef v)
        {
            if (!app.Owns(v.id)) { if (!app.TryBuy(v)) return; }
            app.Select(v.id);
            Populate();
        }
    }
}
