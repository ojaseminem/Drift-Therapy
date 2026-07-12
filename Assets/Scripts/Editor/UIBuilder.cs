#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DriftTherapy.EditorTools
{
    /// <summary>
    /// Authors the game's UI as real, editable Canvas prefabs (no runtime building).
    /// Run via menu: Drift Therapy ▶ Build UI Prefabs. Re-runnable.
    /// Produces Assets/Prefabs/UI/MenuUI.prefab and GameUI.prefab, each with its own
    /// Canvas + new-Input-System EventSystem, LuckiestGuy font, and a bound presenter.
    /// </summary>
    public static class UIBuilder
    {
        const string Dir = "Assets/Prefabs/UI";
        const string FontPath = "Assets/Fonts/LUCKIESTGUY_TMP.asset";
        static TMP_FontAsset font;

        // palette
        static readonly Color Bg = new Color(0.09f, 0.11f, 0.15f, 1f);
        static readonly Color Panel = new Color(0.16f, 0.19f, 0.25f, 0.98f);
        static readonly Color Dark = new Color(0.11f, 0.13f, 0.18f, 0.98f);
        static readonly Color Accent = new Color(0.18f, 0.85f, 0.78f, 1f);
        static readonly Color Accent2 = new Color(1f, 0.30f, 0.62f, 1f);
        static readonly Color Coin = new Color(1f, 0.80f, 0.20f, 1f);
        static readonly Color Gem = new Color(0.40f, 0.75f, 1f, 1f);
        static readonly Color Key = new Color(1f, 0.45f, 0.45f, 1f);
        static readonly Color Health = new Color(0.35f, 0.85f, 0.45f, 1f);
        static readonly Color Ink = new Color(0.04f, 0.06f, 0.09f, 1f);
        static readonly Color White = Color.white;
        static readonly Color Dim = new Color(1f, 1f, 1f, 0.6f);

        [MenuItem("Drift Therapy/Build UI Prefabs")]
        public static void Build()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            if (!Directory.Exists(Dir + "/Popups")) Directory.CreateDirectory(Dir + "/Popups");
            BuildMenu();
            BuildGame();
            BuildGarage();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIBuilder] Built MenuUI.prefab, GameUI.prefab, and GarageUI.prefab");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        static RectTransform RT(Transform p, string n)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.transform.SetParent(p, false);
            return go.GetComponent<RectTransform>();
        }
        static Image Img(Transform p, string n, Color c) { var rt = RT(p, n); var i = rt.gameObject.AddComponent<Image>(); i.color = c; return i; }
        static TMP_Text Txt(Transform p, string n, string s, float size, Color c, TextAlignmentOptions a = TextAlignmentOptions.Center)
        {
            var rt = RT(p, n); var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = s; t.font = font; t.fontSize = size; t.color = c; t.alignment = a;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; return t;
        }
        static Button Btn(Transform p, string n, string label, Color bg, Color fg, float size)
        {
            var i = Img(p, n, bg); var b = i.gameObject.AddComponent<Button>(); b.targetGraphic = i;
            var l = Txt(i.transform, "Label", label, size, fg); Stretch(l.rectTransform); return b;
        }
        static RectTransform Bar(Transform p, string n, Color track, Color fill)
        {
            var bg = Img(p, n, track); bg.raycastTarget = false;
            var f = Img(bg.transform, "Fill", fill); f.raycastTarget = false;
            var fr = (RectTransform)f.transform;
            fr.anchorMin = new Vector2(0, 0); fr.anchorMax = new Vector2(1, 1); fr.pivot = new Vector2(0, 0.5f);
            fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
            return fr; // presenter drives via anchorMax.x
        }
        static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }
        static RectTransform Box(RectTransform rt, Vector2 a, Vector2 size, Vector2 off)
        { rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a; rt.sizeDelta = size; rt.anchoredPosition = off; return rt; }

        /// <summary>
        /// Builds a screen's Canvas + safe-area content root + EventSystem.
        /// Returns <c>canvas</c> (the safe-area child — actual readable/interactive
        /// content parents here, exactly as before) AND <c>rawCanvas</c> (the raw,
        /// full-bleed Canvas — ONLY full-screen backgrounds/scrims should parent
        /// here, so they cover every edge regardless of notch/cutout insets; see
        /// PopupHandler wiring in BuildMenu()/BuildGarage() and the pause/end
        /// Scrim() calls in BuildGame()).
        /// </summary>
        static (GameObject root, RectTransform canvas, RectTransform rawCanvas) NewScreen(string name)
        {
            var root = new GameObject(name);
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(root.transform, false);
            var c = cgo.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = cgo.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080, 1920);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; s.matchWidthOrHeight = 0.5f;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(root.transform, false);

            // Actual content parents under this safe-area child — every existing
            // Img(canvas,...)/Txt(canvas,...) call site automatically inherits
            // notch/cutout/gesture-bar insets with zero changes elsewhere.
            var safeAreaGo = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaGo.transform.SetParent(cgo.transform, false);
            var safeAreaRt = (RectTransform)safeAreaGo.transform;
            Stretch(safeAreaRt);
            safeAreaGo.AddComponent<SafeAreaFitter>();

            return (root, safeAreaRt, (RectTransform)cgo.transform);
        }

        static RectTransform Scrim(Transform p, out RectTransform card, float pad, float topPad)
        {
            var scrim = Img(p, "Scrim", new Color(0, 0, 0, 0.72f)); Stretch((RectTransform)scrim.transform);
            var c = Img(scrim.transform, "Card", Panel); Stretch((RectTransform)c.transform, pad, pad, topPad, topPad);
            card = (RectTransform)c.transform;
            return (RectTransform)scrim.transform;
        }

        static void Save(GameObject root, string path) { PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); }

        // ── Menu ─────────────────────────────────────────────────────────────
        static void BuildMenu()
        {
            var (root, canvas, rawCanvas) = NewScreen("MenuUI");
            var ui = root.AddComponent<MainMenuUI>();
            // Full-bleed screen background — parents under the raw canvas (not the
            // safe-area child) so it covers every corner regardless of notch insets.
            // Must render behind SafeArea (which NewScreen() already parented as the
            // first child of rawCanvas) or it draws on top and hides all content.
            var bg = Img(rawCanvas, "Bg", Bg); Stretch((RectTransform)bg.transform);
            bg.transform.SetAsFirstSibling();

            // top bar
            var bar = Img(canvas, "TopBar", Dark);
            var brt = (RectTransform)bar.transform; brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1); brt.pivot = new Vector2(0.5f, 1);
            brt.sizeDelta = new Vector2(0, 140); brt.anchoredPosition = new Vector2(0, -30);
            ui.keysText  = Chip(bar.transform, "Keys", Key, new Vector2(0, 0.5f), new Vector2(40, 0));
            ui.gemsText  = Chip(bar.transform, "Gems", Gem, new Vector2(0.34f, 0.5f), Vector2.zero);
            ui.coinsText = Chip(bar.transform, "Coins", Coin, new Vector2(0.66f, 0.5f), Vector2.zero);
            ui.settingsButton = Btn(bar.transform, "Settings", "SET", Panel, White, 24);
            Box((RectTransform)ui.settingsButton.transform, new Vector2(1, 0.5f), new Vector2(90, 90), new Vector2(-40, 0));

            // profile
            var card = Img(canvas, "Profile", Panel);
            var crt = (RectTransform)card.transform; crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(-40, 160); crt.anchoredPosition = new Vector2(0, -190);
            var badge = Img(card.transform, "Badge", Accent2); Box((RectTransform)badge.transform, new Vector2(0, 0.5f), new Vector2(120, 120), new Vector2(30, 0));
            ui.levelText = Txt(badge.transform, "Lvl", "1", 56, Ink); Stretch(ui.levelText.rectTransform);
            ui.xpText = Txt(card.transform, "XpText", "0/100", 26, Dim, TextAlignmentOptions.Left); Box(ui.xpText.rectTransform, new Vector2(0, 0.5f), new Vector2(420, 40), new Vector2(180, 28));
            ui.xpFill = Bar(card.transform, "XpBar", Dark, Accent); Box((RectTransform)ui.xpFill.parent, new Vector2(0, 0.5f), new Vector2(440, 26), new Vector2(180, -24));
            ui.bestText = Txt(card.transform, "Best", "BEST 0", 34, Coin, TextAlignmentOptions.Right); Box(ui.bestText.rectTransform, new Vector2(1, 0.5f), new Vector2(360, 60), new Vector2(-30, 0));

            // showcase
            ui.vehicleSwatch = Img(canvas, "CarSwatch", Accent); Box((RectTransform)ui.vehicleSwatch.transform, new Vector2(0.5f, 0.5f), new Vector2(520, 300), new Vector2(0, 120));
            ui.vehicleNameText = Txt(canvas, "CarName", "Drifter", 64, White); Box(ui.vehicleNameText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(700, 90), new Vector2(0, -80));

            // run
            ui.runButton = Btn(canvas, "Run", "RUN", Coin, Ink, 70); Box((RectTransform)ui.runButton.transform, new Vector2(0.5f, 0), new Vector2(720, 160), new Vector2(0, 240));

            // bottom nav
            var nav = Img(canvas, "Nav", Dark);
            var nrt = (RectTransform)nav.transform; nrt.anchorMin = new Vector2(0, 0); nrt.anchorMax = new Vector2(1, 0); nrt.pivot = new Vector2(0.5f, 0);
            nrt.sizeDelta = new Vector2(0, 180);
            const int navSlots = 6;
            ui.missionsButton    = NavBtn(nav.transform, "Missions", 0, navSlots);
            ui.garageButton      = NavBtn(nav.transform, "Garage", 1, navSlots);
            ui.homeButton        = NavBtn(nav.transform, "Home", 2, navSlots, true);
            ui.trialsButton      = NavBtn(nav.transform, "Trials", 3, navSlots);
            ui.shopButton        = NavBtn(nav.transform, "Shop", 4, navSlots);
            ui.leaderboardButton = NavBtn(nav.transform, "Ranks", 5, navSlots);

            // popups: one handler under the RAW canvas (not the safe-area child) so
            // each popup's full-screen scrim actually reaches every edge — see
            // NewScreen()'s doc comment.
            var ph = RT(rawCanvas, "PopupHandler"); Stretch(ph);
            ui.popups = ph.gameObject.AddComponent<PopupHandler>();
            ui.missionsPopup    = BuildMissionsPopup();
            ui.trialsPopup      = BuildTrialsPopup();
            ui.shopPopup        = BuildStubPopup("SHOP");
            ui.leaderboardPopup = BuildLeaderboardPopup();
            ui.settingsPopup    = BuildSettingsPopup();
            ui.onboardingPopup  = BuildOnboardingPopup();

            Save(root, Dir + "/MenuUI.prefab");
        }

        static GameObject SavePopup(GameObject root, string path)
        { var a = PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root); return a; }

        static (RectTransform root, RectTransform card, Popup popup) PopupRoot(string name, float pad, float topPad)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.72f);
            var card = Img(go.transform, "Card", Panel); Stretch((RectTransform)card.transform, pad, pad, topPad, topPad);
            return (rt, (RectTransform)card.transform, null);
        }

        static GameObject BuildStubPopup(string title)
        {
            var (rt, card, _) = PopupRoot(title + "Popup", 70, 420);
            var p = rt.gameObject.AddComponent<Popup>();
            var t = Txt(card, "Title", title, 62, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(600, 90), new Vector2(0, -40));
            var body = Txt(card, "Body", "Coming soon", 42, Dim, TextAlignmentOptions.Center); Stretch(body.rectTransform, 40, 40, 170, 200);
            p.closeButton = Btn(card, "Close", "CLOSE", Accent, Ink, 44); Box((RectTransform)p.closeButton.transform, new Vector2(0.5f, 0), new Vector2(420, 120), new Vector2(0, 60));
            return SavePopup(rt.gameObject, Dir + "/Popups/" + title + "Popup.prefab");
        }

        static GameObject BuildSettingsPopup()
        {
            var (rt, card, _) = PopupRoot("SettingsPopup", 70, 300);
            var sp = rt.gameObject.AddComponent<SettingsPopup>();
            var t = Txt(card, "Title", "SETTINGS", 62, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(600, 90), new Vector2(0, -40));
            sp.closeButton = Btn(card, "Close", "X", Accent2, Ink, 44); Box((RectTransform)sp.closeButton.transform, new Vector2(1, 1), new Vector2(90, 90), new Vector2(-24, -24));

            sp.musicButton    = StackBtn(card, "Music", "MUSIC: ON", Panel, White, 0);
            sp.sfxButton      = StackBtn(card, "Sfx", "SFX: ON", Panel, White, 1);
            sp.hapticsButton  = StackBtn(card, "Haptics", "HAPTICS: ON", Panel, White, 2);
            sp.removeAdsButton = StackBtn(card, "RemoveAds", "REMOVE ADS", Coin, Ink, 3);
            sp.restoreButton  = StackBtn(card, "Restore", "RESTORE PURCHASES", Panel, White, 4);

            sp.versionText = Txt(card, "Version", "v1.0", 24, Dim);
            Box(sp.versionText.rectTransform, new Vector2(0.5f, 0), new Vector2(400, 40), new Vector2(0, 40));

            return SavePopup(rt.gameObject, Dir + "/Popups/SettingsPopup.prefab");
        }

        static GameObject BuildOnboardingPopup()
        {
            var (rt, card, _) = PopupRoot("OnboardingPopup", 70, 420);
            var op = rt.gameObject.AddComponent<OnboardingPopup>();
            var t = Txt(card, "Title", "HOW TO PLAY", 58, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(640, 90), new Vector2(0, -40));

            var steerRow = Img(card, "SteerRow", Dark); Box((RectTransform)steerRow.transform, new Vector2(0.5f, 1), new Vector2(700, 130), new Vector2(0, -160));
            var steerDot = Img(steerRow.transform, "Dot", Accent); Box((RectTransform)steerDot.transform, new Vector2(0, 0.5f), new Vector2(64, 64), new Vector2(36, 0));
            var steerLabel = Txt(steerRow.transform, "Label", "SWIPE TO STEER", 32, White, TextAlignmentOptions.Left);
            Box(steerLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(520, 70), new Vector2(96, 0));

            var driftRow = Img(card, "DriftRow", Dark); Box((RectTransform)driftRow.transform, new Vector2(0.5f, 1), new Vector2(700, 130), new Vector2(0, -300));
            var driftDot = Img(driftRow.transform, "Dot", Accent2); Box((RectTransform)driftDot.transform, new Vector2(0, 0.5f), new Vector2(64, 64), new Vector2(36, 0));
            var driftLabel = Txt(driftRow.transform, "Label", "HOLD TO DRIFT", 32, White, TextAlignmentOptions.Left);
            Box(driftLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(520, 70), new Vector2(96, 0));

            op.gotItButton = Btn(card, "GotIt", "GOT IT", Coin, Ink, 48);
            Box((RectTransform)op.gotItButton.transform, new Vector2(0.5f, 0), new Vector2(480, 120), new Vector2(0, 60));

            return SavePopup(rt.gameObject, Dir + "/Popups/OnboardingPopup.prefab");
        }

        static GameObject BuildMissionsPopup()
        {
            var (rt, card, _) = PopupRoot("MISSIONSPopup", 50, 240);
            var mp = rt.gameObject.AddComponent<MissionsPopup>();
            var t = Txt(card, "Title", "MISSIONS", 64, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(520, 90), new Vector2(0, -40));
            mp.closeButton = Btn(card, "Close", "X", Accent2, Ink, 44); Box((RectTransform)mp.closeButton.transform, new Vector2(1, 1), new Vector2(90, 90), new Vector2(-24, -24));

            // daily challenge card
            var daily = Img(card, "DailyCard", Dark); Box((RectTransform)daily.transform, new Vector2(0.5f, 1), new Vector2(760, 220), new Vector2(0, -150));
            mp.dailyTitleText = Txt(daily.transform, "Title", "Daily Challenge", 32, White, TextAlignmentOptions.Left);
            Box(mp.dailyTitleText.rectTransform, new Vector2(0, 1), new Vector2(480, 50), new Vector2(30, -20));
            mp.streakText = Txt(daily.transform, "Streak", "STREAK 0", 26, Coin, TextAlignmentOptions.Right);
            Box(mp.streakText.rectTransform, new Vector2(1, 1), new Vector2(260, 50), new Vector2(-30, -20));
            mp.dailyProgressFill = Bar(daily.transform, "ProgressBar", Panel, Accent);
            Box((RectTransform)mp.dailyProgressFill.parent, new Vector2(0.5f, 0.5f), new Vector2(560, 30), new Vector2(-90, -10));
            mp.dailyProgressText = Txt(daily.transform, "ProgressText", "0/0", 24, Dim, TextAlignmentOptions.Left);
            Box(mp.dailyProgressText.rectTransform, new Vector2(0, 0.5f), new Vector2(260, 40), new Vector2(30, -10));
            mp.dailyClaimButton = Btn(daily.transform, "Claim", "CLAIM", Coin, Ink, 28);
            Box((RectTransform)mp.dailyClaimButton.transform, new Vector2(1, 0.5f), new Vector2(200, 80), new Vector2(-30, -10));

            var content = RT(card, "Content"); Stretch(content, 30, 30, 400, 40);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>(); vlg.spacing = 14; vlg.childForceExpandHeight = false; vlg.childControlHeight = false; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            mp.content = content;
            mp.rowTemplate = BuildMissionRow(content);
            return SavePopup(rt.gameObject, Dir + "/Popups/MISSIONSPopup.prefab");
        }

        static GameObject BuildTrialsPopup()
        {
            var (rt, card, _) = PopupRoot("TRIALSPopup", 50, 240);
            var tp = rt.gameObject.AddComponent<TrialsPopup>();
            var t = Txt(card, "Title", "TRIALS", 64, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(500, 90), new Vector2(0, -40));
            tp.closeButton = Btn(card, "Close", "X", Accent2, Ink, 44); Box((RectTransform)tp.closeButton.transform, new Vector2(1, 1), new Vector2(90, 90), new Vector2(-24, -24));

            var content = RT(card, "Content"); Stretch(content, 30, 30, 150, 40);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>(); vlg.spacing = 14; vlg.childForceExpandHeight = false; vlg.childControlHeight = false; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            tp.content = content;
            tp.rowTemplate = BuildMissionRow(content);
            return SavePopup(rt.gameObject, Dir + "/Popups/TRIALSPopup.prefab");
        }

        static GameObject BuildLeaderboardPopup()
        {
            var (rt, card, _) = PopupRoot("LeaderboardPopup", 50, 240);
            var lp = rt.gameObject.AddComponent<LeaderboardPopup>();
            var t = Txt(card, "Title", "TOP RUNS", 64, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(560, 90), new Vector2(0, -40));
            lp.closeButton = Btn(card, "Close", "X", Accent2, Ink, 44); Box((RectTransform)lp.closeButton.transform, new Vector2(1, 1), new Vector2(90, 90), new Vector2(-24, -24));

            // Hidden by default; LeaderboardPopup enables it once PlatformServices.PlayGames.IsAvailable.
            lp.viewGlobalButton = Btn(card, "ViewGlobal", "VIEW GLOBAL", Panel, White, 26);
            Box((RectTransform)lp.viewGlobalButton.transform, new Vector2(0.5f, 1), new Vector2(420, 70), new Vector2(0, -140));
            lp.viewGlobalButton.gameObject.SetActive(false);

            var content = RT(card, "Content"); Stretch(content, 30, 30, 220, 40);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>(); vlg.spacing = 12; vlg.childForceExpandHeight = false; vlg.childControlHeight = false; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            lp.content = content;
            lp.rowTemplate = BuildLeaderboardRow(content);
            return SavePopup(rt.gameObject, Dir + "/Popups/LeaderboardPopup.prefab");
        }

        static TMP_Text Chip(Transform p, string name, Color dot, Vector2 anchor, Vector2 off)
        {
            var chip = Img(p, name, Panel); Box((RectTransform)chip.transform, anchor, new Vector2(300, 80), off + new Vector2(anchor.x > 0 ? 0 : 0, 0));
            var d = Img(chip.transform, "Dot", dot); Box((RectTransform)d.transform, new Vector2(0, 0.5f), new Vector2(48, 48), new Vector2(26, 0));
            var t = Txt(chip.transform, "Value", "0", 36, White, TextAlignmentOptions.Left); Box(t.rectTransform, new Vector2(0, 0.5f), new Vector2(190, 60), new Vector2(70, 0));
            return t;
        }

        static Button NavBtn(Transform p, string label, int i, int totalSlots, bool active = false)
        {
            var b = Btn(p, label, label, active ? Accent : Panel, active ? Ink : White, 24);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(i / (float)totalSlots, 0); rt.anchorMax = new Vector2((i + 1) / (float)totalSlots, 1);
            rt.pivot = new Vector2(0.5f, 0.5f); rt.offsetMin = new Vector2(8, 16); rt.offsetMax = new Vector2(-8, -16);
            return b;
        }

        static GameObject BuildMissionRow(Transform content)
        {
            var row = Img(content, "RowTemplate", Dark);
            row.gameObject.AddComponent<CanvasGroup>();
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 170; le.preferredHeight = 170;
            var title = Txt(row.transform, "Title", "Mission", 32, White, TextAlignmentOptions.Left);
            Box(title.rectTransform, new Vector2(0, 1), new Vector2(520, 50), new Vector2(24, -14));
            var progressText = Txt(row.transform, "ProgressText", "0/0", 24, Dim, TextAlignmentOptions.Left);
            Box(progressText.rectTransform, new Vector2(0, 0), new Vector2(300, 36), new Vector2(24, 14));
            var bar = Bar(row.transform, "ProgressBar", Panel, Accent);
            Box((RectTransform)bar.parent, new Vector2(0, 0), new Vector2(340, 22), new Vector2(24, 54));
            var action = Btn(row.transform, "Action", "LOCKED", Coin, Ink, 26);
            Box((RectTransform)action.transform, new Vector2(1, 0.5f), new Vector2(220, 90), new Vector2(-20, 0));
            return row.gameObject;
        }

        static GameObject BuildLeaderboardRow(Transform content)
        {
            var row = Img(content, "RowTemplate", Dark);
            row.gameObject.AddComponent<CanvasGroup>();
            var le = row.gameObject.AddComponent<LayoutElement>(); le.minHeight = 120; le.preferredHeight = 120;
            var rank = Txt(row.transform, "Rank", "#1", 36, Coin, TextAlignmentOptions.Left);
            Box(rank.rectTransform, new Vector2(0, 0.5f), new Vector2(110, 60), new Vector2(20, 0));
            var swatch = Img(row.transform, "Swatch", Accent);
            Box((RectTransform)swatch.transform, new Vector2(0, 0.5f), new Vector2(70, 70), new Vector2(140, 0));
            var distance = Txt(row.transform, "Distance", "0 m", 34, White, TextAlignmentOptions.Left);
            Box(distance.rectTransform, new Vector2(0, 0.5f), new Vector2(280, 60), new Vector2(230, 0));
            var date = Txt(row.transform, "Date", "", 22, Dim, TextAlignmentOptions.Right);
            Box(date.rectTransform, new Vector2(1, 0.5f), new Vector2(220, 50), new Vector2(-20, 0));
            return row.gameObject;
        }

        // ── Game HUD ─────────────────────────────────────────────────────────
        static void BuildGame()
        {
            var (root, canvas, rawCanvas) = NewScreen("GameUI");
            var ui = root.AddComponent<GameHudUI>();
            ui.coinFly = canvas.gameObject.AddComponent<CoinFlyEffect>();
            // NOTE: ui.player is a scene reference (the PlayerCar object in
            // DriftEndless.unity) and cannot be wired from this prefab-only
            // builder — assign it manually in the Inspector once, same as
            // GameController.player.

            ui.distanceText = Txt(canvas, "Distance", "0 m", 40, White, TextAlignmentOptions.TopLeft); Box(ui.distanceText.rectTransform, new Vector2(0, 1), new Vector2(420, 60), new Vector2(40, -50)); ui.distanceText.gameObject.SetActive(false);
            ui.scoreText = Txt(canvas, "Score", "0", 96, White, TextAlignmentOptions.Top); Box(ui.scoreText.rectTransform, new Vector2(0.5f, 1), new Vector2(700, 130), new Vector2(0, -40)); ui.scoreText.gameObject.SetActive(false);

            // drift coins chip (top-center)
            var dc = Img(canvas, "DriftCoins", Panel); Box((RectTransform)dc.transform, new Vector2(0.5f, 1), new Vector2(300, 80), new Vector2(0, -50));
            var dcd = Img(dc.transform, "Dot", Coin); Box((RectTransform)dcd.transform, new Vector2(0, 0.5f), new Vector2(46, 46), new Vector2(24, 0));
            ui.driftCoinsText = Txt(dc.transform, "Value", "0", 36, White, TextAlignmentOptions.Left); Box(ui.driftCoinsText.rectTransform, new Vector2(0, 0.5f), new Vector2(150, 60), new Vector2(64, 0));
            ui.driftCoinsPendingText = Txt(dc.transform, "Pending", "+0", 28, Coin, TextAlignmentOptions.Center); Box(ui.driftCoinsPendingText.rectTransform, new Vector2(0.5f, 0), new Vector2(220, 40), new Vector2(0, -12)); ui.driftCoinsPendingText.gameObject.SetActive(false);
            ui.multiplierText = Txt(canvas, "Mult", "x1.0", 48, Accent2, TextAlignmentOptions.Top); Box(ui.multiplierText.rectTransform, new Vector2(0.5f, 1), new Vector2(320, 60), new Vector2(0, -180)); ui.multiplierText.gameObject.SetActive(false);

            ui.pauseButton = Btn(canvas, "Pause", "II", Panel, White, 40); Box((RectTransform)ui.pauseButton.transform, new Vector2(1, 1), new Vector2(96, 96), new Vector2(-40, -50));

            // health bar (top-left under distance)
            ui.healthFill = Bar(canvas, "HealthBar", Dark, Health); Box((RectTransform)ui.healthFill.parent, new Vector2(0, 1), new Vector2(360, 28), new Vector2(40, -120));

            // boost meter + button (bottom-right)
            ui.boostFill = Bar(canvas, "BoostBar", Dark, Accent); Box((RectTransform)ui.boostFill.parent, new Vector2(1, 0), new Vector2(340, 28), new Vector2(-200, 70));
            ui.boostButton = Btn(canvas, "Boost", "BOOST", Accent, Ink, 36); Box((RectTransform)ui.boostButton.transform, new Vector2(1, 0), new Vector2(300, 130), new Vector2(-40, 120));

            ui.countdownText = Txt(canvas, "Countdown", "3", 220, Accent, TextAlignmentOptions.Center); Box(ui.countdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(600, 320), new Vector2(0, 120));
            ui.nearMissText = Txt(canvas, "NearMiss", "NEAR MISS!", 56, Accent2, TextAlignmentOptions.Center); Box(ui.nearMissText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760, 90), new Vector2(0, -160));

            // pause panel — scrim parents under the raw canvas so it's full-bleed;
            // Card keeps its existing fixed pixel padding.
            var pause = Scrim(rawCanvas, out var pcard, 70, 360); ui.pausePanel = pause.gameObject;
            CardTitle(pcard, "PAUSED");
            ui.resumeButton = StackBtn(pcard, "Resume", "RESUME", Accent, Ink, 0);
            ui.restartButton = StackBtn(pcard, "Restart", "RESTART", Coin, Ink, 1);
            ui.homeButton = StackBtn(pcard, "Home", "HOME", Panel, White, 2);

            // end panel — same full-bleed-scrim fix as the pause panel above.
            var end = Scrim(rawCanvas, out var ecard, 60, 240); ui.endPanel = end.gameObject;
            CardTitle(ecard, "RUN OVER");

            var banner = Img(ecard, "ResultBanner", Dark);
            ui.resultBannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            Box((RectTransform)banner.transform, new Vector2(0.5f, 1), new Vector2(720, 112), new Vector2(0, -126));
            var bannerText = Txt(banner.transform, "Label", "DRIFT COMPLETE", 42, Accent);
            Stretch(bannerText.rectTransform);

            var hero = Img(ecard, "CarShowcase", new Color(0.08f, 0.10f, 0.15f, 1f));
            Box((RectTransform)hero.transform, new Vector2(0.5f, 1), new Vector2(760, 250), new Vector2(0, -275));
            var road = Img(hero.transform, "RoadStripe", new Color(0.04f, 0.05f, 0.08f, 1f));
            Box((RectTransform)road.transform, new Vector2(0.5f, 0.5f), new Vector2(690, 72), new Vector2(0, -55));
            var skidL = Img(hero.transform, "SkidLeft", Accent2);
            Box((RectTransform)skidL.transform, new Vector2(0.5f, 0.5f), new Vector2(260, 18), new Vector2(-145, -36));
            var skidR = Img(hero.transform, "SkidRight", Accent);
            Box((RectTransform)skidR.transform, new Vector2(0.5f, 0.5f), new Vector2(260, 18), new Vector2(145, -36));
            var car = Img(hero.transform, "CarBadge", Coin);
            Box((RectTransform)car.transform, new Vector2(0.5f, 0.5f), new Vector2(360, 108), new Vector2(0, 34));
            var carName = Txt(car.transform, "Label", "DRIFT CAR", 42, Ink);
            Stretch(carName.rectTransform);

            var distanceCard = Img(ecard, "DistanceCard", new Color(0.11f, 0.13f, 0.18f, 1f));
            ui.distanceCardGroup = distanceCard.gameObject.AddComponent<CanvasGroup>();
            Box((RectTransform)distanceCard.transform, new Vector2(0.5f, 0.5f), new Vector2(760, 240), new Vector2(0, 150));
            var distanceLabel = Txt(distanceCard.transform, "Label", "DISTANCE", 34, Dim);
            Box(distanceLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(500, 52), new Vector2(0, -24));
            ui.endScore = Txt(distanceCard.transform, "Score", "0 m", 106, White);
            Box(ui.endScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(680, 130), new Vector2(0, 16));
            ui.endBest = Txt(distanceCard.transform, "Best", "BEST 0 m", 36, Coin);
            Box(ui.endBest.rectTransform, new Vector2(0.5f, 0), new Vector2(640, 52), new Vector2(0, 22));
            ui.endNewBest = Txt(ecard, "NewBest", "NEW BEST!", 38, Accent2);
            Box(ui.endNewBest.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(440, 58), new Vector2(0, -4));

            var rewards = Img(ecard, "Rewards", new Color(0, 0, 0, 0));
            ui.rewardsGroup = rewards.gameObject.AddComponent<CanvasGroup>();
            Box((RectTransform)rewards.transform, new Vector2(0.5f, 0.5f), new Vector2(760, 130), new Vector2(0, -84));
            var coinsChip = Img(rewards.transform, "CoinsReward", Dark);
            Box((RectTransform)coinsChip.transform, new Vector2(0, 0.5f), new Vector2(360, 110), new Vector2(0, 0));
            var coinDot = Img(coinsChip.transform, "Dot", Coin);
            Box((RectTransform)coinDot.transform, new Vector2(0, 0.5f), new Vector2(58, 58), new Vector2(34, 0));
            var coinLabel = Txt(coinsChip.transform, "Label", "COINS", 24, Dim, TextAlignmentOptions.Left);
            Box(coinLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(180, 32), new Vector2(108, 24));
            ui.endCoins = Txt(coinsChip.transform, "Value", "+0", 42, Coin, TextAlignmentOptions.Left);
            Box(ui.endCoins.rectTransform, new Vector2(0, 0.5f), new Vector2(210, 54), new Vector2(108, -16));

            var xpChip = Img(rewards.transform, "XpReward", Dark);
            Box((RectTransform)xpChip.transform, new Vector2(1, 0.5f), new Vector2(360, 110), new Vector2(0, 0));
            var xpDot = Img(xpChip.transform, "Dot", Accent);
            Box((RectTransform)xpDot.transform, new Vector2(0, 0.5f), new Vector2(58, 58), new Vector2(34, 0));
            var xpLabel = Txt(xpChip.transform, "Label", "XP", 24, Dim, TextAlignmentOptions.Left);
            Box(xpLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(180, 32), new Vector2(108, 24));
            ui.endXp = Txt(xpChip.transform, "Value", "+0 XP", 42, Accent, TextAlignmentOptions.Left);
            Box(ui.endXp.rectTransform, new Vector2(0, 0.5f), new Vector2(210, 54), new Vector2(108, -16));

            ui.retryButton = Btn(ecard, "Retry", "RETRY", Coin, Ink, 56);
            ui.retryGroup = ui.retryButton.gameObject.AddComponent<CanvasGroup>();
            Box((RectTransform)ui.retryButton.transform, new Vector2(0.5f, 0), new Vector2(620, 150), new Vector2(0, 178));
            ui.endHomeButton = Btn(ecard, "EndHome", "HOME", Panel, White, 42);
            ui.endHomeGroup = ui.endHomeButton.gameObject.AddComponent<CanvasGroup>();
            Box((RectTransform)ui.endHomeButton.transform, new Vector2(0.5f, 0), new Vector2(480, 104), new Vector2(0, 54));

            Save(root, Dir + "/GameUI.prefab");
        }

        // ── Garage ───────────────────────────────────────────────────────────
        const float GarageCardWidth = 1080f;

        static void BuildGarage()
        {
            var (root, canvas, rawCanvas) = NewScreen("GarageUI");
            var ui = root.AddComponent<GarageUI>();
            // No Bg image — this is a real 3D scene (Garage.unity); the turntable
            // car renders directly behind this transparent-background canvas.

            // top bar: level + currencies + home
            ui.levelText = Txt(canvas, "Level", "GARAGE LVL 1", 34, White, TextAlignmentOptions.Left);
            Box(ui.levelText.rectTransform, new Vector2(0, 1), new Vector2(420, 60), new Vector2(40, -50));

            ui.coinsText = Chip(canvas, "Coins", Coin, new Vector2(1, 1), new Vector2(-40, -50));
            ui.gemsText  = Chip(canvas, "Gems", Gem, new Vector2(1, 1), new Vector2(-40, -140));

            ui.homeButton = Btn(canvas, "Home", "HOME", Panel, White, 28);
            Box((RectTransform)ui.homeButton.transform, new Vector2(0, 0), new Vector2(220, 90), new Vector2(40, 40));

            // drag-catcher over the 3D viewport region (upper ~60% of the screen) —
            // invisible (alpha 0) but still raycastable, spatially separate from the
            // carousel band below so the two drag gestures never conflict.
            var dragCatcherImg = Img(canvas, "DragCatcher", new Color(0, 0, 0, 0));
            var dragRt = (RectTransform)dragCatcherImg.transform;
            dragRt.anchorMin = new Vector2(0, 0.42f); dragRt.anchorMax = new Vector2(1, 1);
            dragRt.offsetMin = Vector2.zero; dragRt.offsetMax = Vector2.zero;
            ui.dragCatcher = dragCatcherImg.gameObject.AddComponent<GarageDragCatcher>();

            // carousel band (lower ~35% of the screen)
            var carouselArea = RT(canvas, "CarouselArea");
            carouselArea.anchorMin = new Vector2(0, 0.03f); carouselArea.anchorMax = new Vector2(1, 0.38f);
            carouselArea.offsetMin = Vector2.zero; carouselArea.offsetMax = Vector2.zero;

            var viewportImg = Img(carouselArea, "Viewport", new Color(0, 0, 0, 0));
            var viewportRt = (RectTransform)viewportImg.transform; Stretch(viewportRt);
            viewportImg.gameObject.AddComponent<RectMask2D>();

            // Cards are positioned manually by GarageUI.Populate() (anchoredPosition =
            // index * runtime viewport width), not via a HorizontalLayoutGroup —
            // script-driven LayoutElement width changes didn't reliably propagate
            // through Unity's automatic layout pass timing in practice; direct
            // RectTransform control is simpler and fully deterministic here, and
            // SnapCarousel already assumes exact index*cardWidth spacing anyway.
            var contentRt = RT(viewportRt, "Content");
            contentRt.anchorMin = new Vector2(0, 0); contentRt.anchorMax = new Vector2(0, 1);
            contentRt.pivot = new Vector2(0, 0.5f);

            var scrollRect = carouselArea.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true; scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            ui.carousel = carouselArea.gameObject.AddComponent<SnapCarousel>();
            var soCarousel = new SerializedObject(ui.carousel);
            soCarousel.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            soCarousel.FindProperty("content").objectReferenceValue = contentRt;
            soCarousel.ApplyModifiedProperties();

            ui.content = contentRt;
            ui.cardTemplate = BuildGarageCard(contentRt);

            // purchase confirmation — its own PopupHandler under the raw canvas
            // (Garage is a separate scene from Menu, can't share PopupHandlers).
            var ph = RT(rawCanvas, "PopupHandler"); Stretch(ph);
            ui.popups = ph.gameObject.AddComponent<PopupHandler>();
            ui.purchaseConfirmPopup = BuildPurchaseConfirmPopup();

            Save(root, Dir + "/GarageUI.prefab");
        }

        static GameObject BuildGarageCard(Transform content)
        {
            var card = Img(content, "CardTemplate", new Color(0.12f, 0.14f, 0.19f, 0.92f));
            var cardRt = (RectTransform)card.transform;
            cardRt.anchorMin = new Vector2(0, 0); cardRt.anchorMax = new Vector2(0, 1); cardRt.pivot = new Vector2(0, 0.5f);
            cardRt.sizeDelta = new Vector2(GarageCardWidth, 0); // width corrected to the true runtime viewport width by GarageUI.Populate()

            var name = Txt(card.transform, "Name", "Vehicle", 48, White);
            Box(name.rectTransform, new Vector2(0.5f, 1), new Vector2(700, 70), new Vector2(0, -20));

            GarageSpecBar(card.transform, "SpeedBar", "SPEED", 0);
            GarageSpecBar(card.transform, "HandlingBar", "HANDLING", 1);
            GarageSpecBar(card.transform, "BoostBar", "BOOST", 2);

            var skinsLabel = Txt(card.transform, "SkinsLabel", "SKINS", 22, Dim, TextAlignmentOptions.Left);
            Box(skinsLabel.rectTransform, new Vector2(0, 1), new Vector2(300, 34), new Vector2(40, -265));
            for (int i = 0; i < GarageSkinSlots; i++) GarageSkinSlot(card.transform, i);

            var coinsPrice = Txt(card.transform, "CoinsPrice", "0", 34, Coin, TextAlignmentOptions.Left);
            Box(coinsPrice.rectTransform, new Vector2(0, 0), new Vector2(240, 50), new Vector2(40, 90));
            var gemsPrice = Txt(card.transform, "GemsPrice", "0", 34, Gem, TextAlignmentOptions.Left);
            Box(gemsPrice.rectTransform, new Vector2(0, 0), new Vector2(240, 50), new Vector2(300, 90));

            var action = Btn(card.transform, "Action", "BUY", Coin, Ink, 34);
            Box((RectTransform)action.transform, new Vector2(1, 0), new Vector2(300, 90), new Vector2(-40, 40));

            return card.gameObject;
        }

        static void GarageSpecBar(Transform card, string name, string label, int row)
        {
            float y = -110 - row * 50;
            var lbl = Txt(card, name + "Label", label, 22, Dim, TextAlignmentOptions.Left);
            Box(lbl.rectTransform, new Vector2(0, 1), new Vector2(200, 34), new Vector2(40, y));
            var bar = Bar(card, name, Dark, Accent);
            Box((RectTransform)bar.parent, new Vector2(0, 1), new Vector2(760, 20), new Vector2(260, y - 6));
        }

        const int GarageSkinSlots = 4;
        const float GarageSkinSlotSize = 170f;
        const float GarageSkinSlotGap = 30f;

        /// <summary>
        /// One skin swatch slot: a tintable "Swatch" child (set at runtime to the
        /// skin's colour) plus a "Lock" overlay (dark scrim + price text) shown
        /// when unowned. GarageUI.FillCard finds these by name ("Skin0".."SkinN-1").
        /// </summary>
        static void GarageSkinSlot(Transform card, int index)
        {
            float rowWidth = GarageSkinSlots * GarageSkinSlotSize + (GarageSkinSlots - 1) * GarageSkinSlotGap;
            float startX = (GarageCardWidth - rowWidth) * 0.5f;
            float x = startX + index * (GarageSkinSlotSize + GarageSkinSlotGap);

            var border = Img(card, "Skin" + index, Panel);
            Box((RectTransform)border.transform, new Vector2(0, 1), new Vector2(GarageSkinSlotSize, GarageSkinSlotSize), new Vector2(x, -300));
            var btn = border.gameObject.AddComponent<Button>(); btn.targetGraphic = border;

            var swatch = Img(border.transform, "Swatch", White);
            swatch.raycastTarget = false;
            Box((RectTransform)swatch.transform, new Vector2(0.5f, 0.5f), new Vector2(GarageSkinSlotSize - 16f, GarageSkinSlotSize - 16f), Vector2.zero);

            var lockOverlay = Img(border.transform, "Lock", new Color(0f, 0f, 0f, 0.6f));
            lockOverlay.raycastTarget = false;
            Stretch((RectTransform)lockOverlay.transform);
            var priceText = Txt(lockOverlay.transform, "Price", "0", 22, White);
            Stretch(priceText.rectTransform);
        }

        static GameObject BuildPurchaseConfirmPopup()
        {
            var (rt, cardRt, _) = PopupRoot("PurchaseConfirmPopup", 90, 600);
            var pc = rt.gameObject.AddComponent<PurchaseConfirmPopup>();
            var t = Txt(cardRt, "Title", "CONFIRM PURCHASE?", 50, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(600, 80), new Vector2(0, -30));
            pc.closeButton = Btn(cardRt, "Close", "X", Accent2, Ink, 40); Box((RectTransform)pc.closeButton.transform, new Vector2(1, 1), new Vector2(80, 80), new Vector2(-20, -20));

            pc.nameText = Txt(cardRt, "Name", "Vehicle", 40, White); Box(pc.nameText.rectTransform, new Vector2(0.5f, 1), new Vector2(500, 60), new Vector2(0, -140));
            pc.priceText = Txt(cardRt, "Price", "0 COINS", 34, Coin); Box(pc.priceText.rectTransform, new Vector2(0.5f, 1), new Vector2(500, 60), new Vector2(0, -210));

            pc.confirmButton = Btn(cardRt, "Confirm", "CONFIRM", Coin, Ink, 36);
            Box((RectTransform)pc.confirmButton.transform, new Vector2(0.5f, 0), new Vector2(420, 100), new Vector2(0, 40));

            return SavePopup(rt.gameObject, Dir + "/Popups/PurchaseConfirmPopup.prefab");
        }

        static void CardTitle(Transform card, string s)
        { var t = Txt(card, "Title", s, 62, Accent); Box(t.rectTransform, new Vector2(0.5f, 1), new Vector2(640, 90), new Vector2(0, -40)); }

        static Button StackBtn(Transform card, string n, string label, Color bg, Color fg, int i)
        { var b = Btn(card, n, label, bg, fg, 46); Box((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(540, 130), new Vector2(0, 140 - i * 160)); return b; }
    }
}
#endif
