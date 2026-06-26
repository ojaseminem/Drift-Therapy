using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Small helper for building greybox uGUI in code. Presenters use it to compose
    /// screens without hand-authored prefabs — clean to skin with art later by
    /// swapping the Images/fonts. Portrait reference 1080×1920.
    /// </summary>
    public static class UIFactory
    {
        // ── Palette ──────────────────────────────────────────────────────────
        public static readonly Color Bg        = new Color(0.09f, 0.11f, 0.15f, 1f);
        public static readonly Color PanelCol  = new Color(0.16f, 0.19f, 0.25f, 0.96f);
        public static readonly Color PanelDark = new Color(0.11f, 0.13f, 0.18f, 0.96f);
        public static readonly Color Accent    = new Color(0.18f, 0.85f, 0.78f, 1f); // teal
        public static readonly Color Accent2   = new Color(1.00f, 0.30f, 0.62f, 1f); // magenta
        public static readonly Color Coin      = new Color(1.00f, 0.80f, 0.20f, 1f);
        public static readonly Color Gem       = new Color(0.40f, 0.75f, 1.00f, 1f);
        public static readonly Color Key       = new Color(1.00f, 0.45f, 0.45f, 1f);
        public static readonly Color TextCol    = Color.white;
        public static readonly Color TextDim   = new Color(1f, 1f, 1f, 0.6f);
        public static readonly Color Ink       = new Color(0.04f, 0.06f, 0.09f, 1f);

        // ── Canvas / event system ────────────────────────────────────────────
        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080, 1920);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // ── Primitives ───────────────────────────────────────────────────────
        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Anchor a fixed-size box at a normalized point (e.g. (0.5,1)=top-centre).</summary>
        public static RectTransform Box(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.sizeDelta = size; rt.anchoredPosition = offset;
            return rt;
        }

        public static TMP_Text Text(Transform parent, string name, string text, float size, Color color,
                                    TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Color bg, Color labelColor,
                                    Action onClick, float fontSize = 44)
        {
            var img = Panel(parent, name, bg);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var t = Text(img.transform, "Label", label, fontSize, labelColor);
            Stretch((RectTransform)t.transform);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>Horizontal progress bar; write to <paramref name="fill"/> to update (set anchorMax.x).</summary>
        public static Image ProgressBar(Transform parent, string name, Color track, Color fillColor,
                                        float fill01, out RectTransform fill)
        {
            var bg = Panel(parent, name, track);
            var f = Panel(bg.transform, "Fill", fillColor);
            fill = (RectTransform)f.transform;
            fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(Mathf.Clamp01(fill01), 1);
            fill.pivot = new Vector2(0, 0.5f); fill.offsetMin = Vector2.zero; fill.offsetMax = Vector2.zero;
            return bg;
        }

        public static void SetFill(RectTransform fill, float fill01)
        {
            var a = fill.anchorMax; a.x = Mathf.Clamp01(fill01); fill.anchorMax = a;
        }
    }
}
