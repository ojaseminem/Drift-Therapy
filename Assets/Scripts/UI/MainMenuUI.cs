using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Builds and drives the main-menu home screen (code-driven uGUI). Subway-Surfers
    /// style layout adapted for vehicles: currency bar, profile + level/XP, a vehicle
    /// showcase, a big RUN button, and bottom navigation (Missions / Vehicles / Home /
    /// Trials / Shop). Garage is fully functional (buy + equip); the others are stubs
    /// with working navigation, ready to flesh out.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuUI : MonoBehaviour
    {
        GameApp app;

        // Live elements refreshed from GameApp.
        TMP_Text coinsText, gemsText, keysText, levelText, xpText, highScoreText, vehicleNameText;
        RectTransform xpFill;
        Image vehicleSwatch;
        RectTransform overlayHost;

        void Start()
        {
            app = GameApp.Instance;
            Build();
            if (app != null) app.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (app != null) app.Changed -= Refresh;
        }

        // ── Build ────────────────────────────────────────────────────────────
        void Build()
        {
            var canvas = UIFactory.CreateCanvas("MenuCanvas", 0);
            var root = UIFactory.Panel(canvas.transform, "Root", UIFactory.Bg);
            UIFactory.Stretch((RectTransform)root.transform);

            BuildTopBar(root.transform);
            BuildProfile(root.transform);
            BuildShowcase(root.transform);
            BuildRunButton(root.transform);
            BuildBottomNav(root.transform);

            overlayHost = UIFactory.Rect(root.transform, "OverlayHost");
            UIFactory.Stretch(overlayHost);
            overlayHost.gameObject.SetActive(false);
        }

        void BuildTopBar(Transform parent)
        {
            var bar = UIFactory.Panel(parent, "TopBar", UIFactory.PanelDark);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 130); rt.anchoredPosition = new Vector2(0, -40);

            keysText  = Chip(bar.transform, "Keys",  UIFactory.Key,  new Vector2(0.0f, 0.5f), 24f);
            gemsText  = Chip(bar.transform, "Gems",  UIFactory.Gem,  new Vector2(0.34f, 0.5f), 24f);
            coinsText = Chip(bar.transform, "Coins", UIFactory.Coin, new Vector2(0.68f, 0.5f), 24f);

            UIFactory.Button(bar.transform, "Settings", "⚙", UIFactory.PanelCol, UIFactory.TextCol,
                () => OpenStub("Settings"), 44f);
            var srt = (RectTransform)bar.transform.Find("Settings");
            UIFactory.Box(srt, new Vector2(1, 0.5f), new Vector2(90, 90), new Vector2(-20, 0));
        }

        TMP_Text Chip(Transform parent, string name, Color dotColor, Vector2 anchor, float pad)
        {
            var chip = UIFactory.Panel(parent, name, UIFactory.PanelCol);
            UIFactory.Box((RectTransform)chip.transform, anchor, new Vector2(300, 80), new Vector2(20 + anchor.x * 30, 0));
            var dot = UIFactory.Panel(chip.transform, "Dot", dotColor);
            UIFactory.Box((RectTransform)dot.transform, new Vector2(0, 0.5f), new Vector2(48, 48), new Vector2(28, 0));
            var t = UIFactory.Text(chip.transform, "Value", "0", 34f, UIFactory.TextCol, TextAlignmentOptions.MidlineLeft);
            UIFactory.Box((RectTransform)t.transform, new Vector2(0, 0.5f), new Vector2(180, 60), new Vector2(70, 0));
            return t;
        }

        void BuildProfile(Transform parent)
        {
            var card = UIFactory.Panel(parent, "Profile", UIFactory.PanelCol);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-40, 150); rt.anchoredPosition = new Vector2(0, -190);

            var badge = UIFactory.Panel(card.transform, "LevelBadge", UIFactory.Accent2);
            UIFactory.Box((RectTransform)badge.transform, new Vector2(0, 0.5f), new Vector2(110, 110), new Vector2(30, 0));
            levelText = UIFactory.Text(badge.transform, "Lvl", "1", 48f, UIFactory.Ink);
            UIFactory.Stretch((RectTransform)levelText.transform);

            xpText = UIFactory.Text(card.transform, "XpLabel", "XP", 24f, UIFactory.TextDim, TextAlignmentOptions.MidlineLeft);
            UIFactory.Box((RectTransform)xpText.transform, new Vector2(0, 0.5f), new Vector2(420, 40), new Vector2(170, 24));

            var bar = UIFactory.ProgressBar(card.transform, "XpBar", UIFactory.PanelDark, UIFactory.Accent, 0f, out xpFill);
            UIFactory.Box((RectTransform)bar.transform, new Vector2(0, 0.5f), new Vector2(440, 26), new Vector2(170, -22));

            highScoreText = UIFactory.Text(card.transform, "HighScore", "BEST 0", 34f, UIFactory.Coin, TextAlignmentOptions.MidlineRight);
            UIFactory.Box((RectTransform)highScoreText.transform, new Vector2(1, 0.5f), new Vector2(360, 60), new Vector2(-30, 0));
        }

        void BuildShowcase(Transform parent)
        {
            var stage = UIFactory.Panel(parent, "Showcase", new Color(0, 0, 0, 0));
            var rt = (RectTransform)stage.transform;
            UIFactory.Stretch(rt, 40, 40, 380, 520);

            vehicleSwatch = UIFactory.Panel(stage.transform, "CarSwatch", UIFactory.Accent);
            UIFactory.Box((RectTransform)vehicleSwatch.transform, new Vector2(0.5f, 0.5f), new Vector2(460, 260), new Vector2(0, 30));

            vehicleNameText = UIFactory.Text(stage.transform, "CarName", "Drifter", 56f, UIFactory.TextCol);
            UIFactory.Box((RectTransform)vehicleNameText.transform, new Vector2(0.5f, 0), new Vector2(700, 80), new Vector2(0, 20));
        }

        void BuildRunButton(Transform parent)
        {
            var btn = UIFactory.Button(parent, "RunButton", "▶  RUN", UIFactory.Coin, UIFactory.Ink,
                () => SceneFlow.GoToGame(), 64f);
            UIFactory.Box((RectTransform)btn.transform, new Vector2(0.5f, 0), new Vector2(700, 150), new Vector2(0, 230));
        }

        void BuildBottomNav(Transform parent)
        {
            var bar = UIFactory.Panel(parent, "BottomNav", UIFactory.PanelDark);
            var rt = (RectTransform)bar.transform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, 180); rt.anchoredPosition = Vector2.zero;

            string[] labels = { "Missions", "Vehicles", "Home", "Trials", "Shop" };
            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                bool home = label == "Home";
                var b = UIFactory.Button(bar.transform, label, label,
                    home ? UIFactory.Accent : UIFactory.PanelCol, home ? UIFactory.Ink : UIFactory.TextCol,
                    () => OnNav(label), 28f);
                var brt = (RectTransform)b.transform;
                brt.anchorMin = new Vector2(i / 5f, 0); brt.anchorMax = new Vector2((i + 1) / 5f, 1);
                brt.pivot = new Vector2(0.5f, 0.5f); brt.offsetMin = new Vector2(8, 16); brt.offsetMax = new Vector2(-8, -16);
            }
        }

        // ── Navigation ───────────────────────────────────────────────────────
        void OnNav(string label)
        {
            switch (label)
            {
                case "Home":     CloseOverlay(); break;
                case "Vehicles": OpenGarage();   break;
                default:         OpenStub(label); break;
            }
        }

        void CloseOverlay()
        {
            foreach (Transform c in overlayHost) Destroy(c.gameObject);
            overlayHost.gameObject.SetActive(false);
        }

        RectTransform OpenOverlay(string title)
        {
            CloseOverlay();
            overlayHost.gameObject.SetActive(true);
            var scrim = UIFactory.Panel(overlayHost, "Scrim", new Color(0, 0, 0, 0.6f));
            UIFactory.Stretch((RectTransform)scrim.transform);

            var panel = UIFactory.Panel(overlayHost, "Panel", UIFactory.PanelCol);
            UIFactory.Stretch((RectTransform)panel.transform, 40, 40, 220, 220);

            var titleText = UIFactory.Text(panel.transform, "Title", title, 56f, UIFactory.Accent);
            UIFactory.Box(titleText.rectTransform, new Vector2(0.5f, 1), new Vector2(600, 80), new Vector2(0, -40));

            UIFactory.Button(panel.transform, "Close", "✕", UIFactory.Accent2, UIFactory.Ink,
                CloseOverlay, 40f);
            var crt = (RectTransform)panel.transform.Find("Close");
            UIFactory.Box(crt, new Vector2(1, 1), new Vector2(80, 80), new Vector2(-24, -24));
            return (RectTransform)panel.transform;
        }

        void OpenStub(string title)
        {
            var panel = OpenOverlay(title);
            UIFactory.Text(panel, "Soon", title + "\ncoming soon", 40f, UIFactory.TextDim)
                .alignment = TextAlignmentOptions.Center;
            var srt = (RectTransform)panel.Find("Soon");
            UIFactory.Stretch(srt, 40, 40, 160, 160);
        }

        // ── Garage ───────────────────────────────────────────────────────────
        void OpenGarage()
        {
            var panel = OpenOverlay("Garage");
            if (app == null) return;

            var list = UIFactory.Rect(panel, "List");
            UIFactory.Stretch(list, 30, 30, 150, 40);

            var vehicles = app.Vehicles;
            float y = -10;
            for (int i = 0; i < vehicles.Count; i++)
            {
                var v = vehicles[i];
                if (v == null) continue;
                BuildGarageRow(list, v, ref y);
            }
        }

        void BuildGarageRow(RectTransform list, VehicleDef v, ref float y)
        {
            var row = UIFactory.Panel(list, "Row_" + v.id, UIFactory.PanelDark);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 150); rt.anchoredPosition = new Vector2(0, y);
            y -= 165;

            var sw = UIFactory.Panel(row.transform, "Swatch", v.bodyColor);
            UIFactory.Box((RectTransform)sw.transform, new Vector2(0, 0.5f), new Vector2(180, 110), new Vector2(20, 0));

            UIFactory.Text(row.transform, "Name", v.displayName, 36f, UIFactory.TextCol, TextAlignmentOptions.MidlineLeft)
                .rectTransform.sizeDelta = new Vector2(360, 60);
            var nrt = (RectTransform)row.transform.Find("Name");
            UIFactory.Box(nrt, new Vector2(0, 0.5f), new Vector2(360, 60), new Vector2(230, 0));

            bool owned = app.Owns(v.id);
            bool equipped = app.Data.selectedVehicleId == v.id;

            string label = equipped ? "EQUIPPED" : owned ? "EQUIP" : v.price + "  BUY";
            Color bg = equipped ? UIFactory.PanelCol : owned ? UIFactory.Accent : UIFactory.Coin;
            Color fg = equipped ? UIFactory.TextDim : UIFactory.Ink;

            var btn = UIFactory.Button(row.transform, "Action", label, bg, fg, () => OnGarageAction(v), 30f);
            var art = (RectTransform)btn.transform;
            UIFactory.Box(art, new Vector2(1, 0.5f), new Vector2(260, 90), new Vector2(-20, 0));
            btn.interactable = !equipped;
        }

        void OnGarageAction(VehicleDef v)
        {
            if (!app.Owns(v.id)) { if (!app.TryBuy(v)) return; }
            app.Select(v.id);
            OpenGarage();   // rebuild rows
            Refresh();
        }

        // ── Refresh ──────────────────────────────────────────────────────────
        void Refresh()
        {
            if (app == null) return;
            var d = app.Data;
            if (coinsText) coinsText.text = d.coins.ToString();
            if (gemsText)  gemsText.text  = d.gems.ToString();
            if (keysText)  keysText.text  = d.keys.ToString();
            if (levelText) levelText.text = d.level.ToString();
            if (xpText)    xpText.text    = $"XP {d.xp} / {app.XpToLevel}";
            if (xpFill)    UIFactory.SetFill(xpFill, app.XpToLevel > 0 ? (float)d.xp / app.XpToLevel : 0f);
            if (highScoreText) highScoreText.text = "BEST " + d.highScore;

            var sel = app.Selected;
            if (sel != null)
            {
                if (vehicleNameText) vehicleNameText.text = sel.displayName;
                if (vehicleSwatch)   vehicleSwatch.color  = sel.bodyColor;
            }
        }
    }
}
