using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Shared, audited DOTween helpers for UI game-feel. Every site in the
    /// codebase that wants a punch/fade/fill/count tween routes through here
    /// instead of calling DOTween directly — keeps the integration to one
    /// small, reviewable surface instead of scattered ad-hoc tween code.
    ///
    /// Every method kills any tween already running on its target/id before
    /// starting a new one — signals can fire faster than a tween's duration
    /// (rapid near-misses, rapid nav taps), and stacking tweens on the same
    /// target is exactly the kind of "overdoing it" this pass avoids.
    ///
    /// Every tween runs on unscaled time (<c>SetUpdate(true)</c>): the game
    /// sets <c>Time.timeScale = 0f</c> on pause and on run-failure, and the
    /// end-screen reveal (Part C.6) plays exactly at that moment — a
    /// scaled-time tween would simply never advance.
    /// </summary>
    public static class UiJuice
    {
        public static void PunchScale(RectTransform t, float strength = 0.15f, float duration = 0.25f)
        {
            if (t == null) return;
            DOTween.Kill(t, complete: false);
            t.localScale = Vector3.one;
            t.DOPunchScale(Vector3.one * strength, duration, 6, 0.8f).SetId(t).SetUpdate(true);
        }

        public static void FadeCanvasGroup(CanvasGroup cg, float to, float duration, Action onComplete = null)
        {
            if (cg == null) return;
            DOTween.Kill(cg, complete: false);
            cg.DOFade(to, duration).SetId(cg).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Count a TMP label from <paramref name="from"/> to <paramref name="to"/> over <paramref name="duration"/>.</summary>
        public static void CountTo(TMP_Text label, int from, int to, float duration)
        {
            if (label == null) return;
            DOTween.Kill(label, complete: false);
            int current = from;
            DOTween.To(() => current, v => { current = v; label.text = v.ToString(); }, to, duration)
                .SetId(label)
                .SetUpdate(true)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Staggered fade-in for a list row. Only the first <paramref name="maxStaggered"/>
        /// rows animate (index × <paramref name="perIndexDelay"/> — capped, never an
        /// unbounded stagger chain); rows beyond that appear instantly at full alpha.
        /// </summary>
        public static void StaggerIn(CanvasGroup cg, int index, int maxStaggered = 10, float perIndexDelay = 0.03f, float duration = 0.2f)
        {
            if (cg == null) return;
            if (index >= maxStaggered)
            {
                cg.alpha = 1f;
                return;
            }

            cg.alpha = 0f;
            DOTween.Kill(cg, complete: false);
            DOVirtual.DelayedCall(index * perIndexDelay, () => FadeCanvasGroup(cg, 1f, duration))
                .SetId(cg)
                .SetUpdate(true);
        }

        /// <summary>Tweens a fill RectTransform's anchorMax.x (the value GameHudUI/MainMenuUI previously set instantly).</summary>
        public static void FillTo(RectTransform fillRt, float toX01, float duration)
        {
            if (fillRt == null) return;
            DOTween.Kill(fillRt, complete: false);
            float from = fillRt.anchorMax.x;
            DOTween.To(() => from, x =>
            {
                from = x;
                var a = fillRt.anchorMax;
                a.x = x;
                fillRt.anchorMax = a;
            }, Mathf.Clamp01(toX01), duration).SetId(fillRt).SetUpdate(true).SetEase(Ease.OutQuad);
        }
    }
}
