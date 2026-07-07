using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Fixed pool of small UI icons that fly from a screen position (the car)
    /// to a target RectTransform (the HUD coin chip) on a committed coin gain.
    /// The pool is built once in <see cref="Awake"/> — <see cref="Play"/> never
    /// calls Instantiate/Destroy. If every pooled icon is already mid-flight,
    /// <see cref="Play"/> is a no-op (presentation only; the coin value has
    /// already been applied via <c>GameApp</c> regardless).
    /// </summary>
    [DisallowMultipleComponent]
    public class CoinFlyEffect : MonoBehaviour
    {
        [Tooltip("RectTransform whose local space icons fly through. Defaults to this component's own RectTransform (expects to sit on the HUD Canvas root).")]
        [SerializeField] RectTransform canvasRect;
        [SerializeField] Color iconColor = new Color(1f, 0.80f, 0.20f, 1f);
        [SerializeField] int poolSize = 6;
        [SerializeField] float duration = 0.4f;
        [SerializeField] float jumpPower = 60f;

        RectTransform[] pool;
        bool[] busy;
        int nextIndex;

        void Awake()
        {
            if (canvasRect == null) canvasRect = transform as RectTransform;

            pool = new RectTransform[poolSize];
            busy = new bool[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("CoinFly_" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvasRect, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(36, 36);
                go.GetComponent<Image>().color = iconColor;
                go.SetActive(false);
                pool[i] = rt;
            }
        }

        /// <summary>Flies one pooled icon from a screen-space point to a target RectTransform's screen position.</summary>
        public void Play(Vector2 fromScreenPos, RectTransform toRect)
        {
            if (canvasRect == null || toRect == null || pool == null) return;

            int index = -1;
            for (int i = 0; i < poolSize; i++)
            {
                int candidate = (nextIndex + i) % poolSize;
                if (!busy[candidate]) { index = candidate; break; }
            }
            if (index < 0) return; // all pooled icons mid-flight — skip, never queue a 7th

            nextIndex = (index + 1) % poolSize;
            busy[index] = true;
            var icon = pool[index];

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, fromScreenPos, null, out var fromLocal);
            Vector2 toScreenPos = RectTransformUtility.WorldToScreenPoint(null, toRect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, toScreenPos, null, out var toLocal);

            icon.anchoredPosition = fromLocal;
            icon.localScale = Vector3.one;
            var img = icon.GetComponent<Image>();
            var c = img.color; c.a = 1f; img.color = c;
            icon.gameObject.SetActive(true);

            DOTween.Kill(icon, complete: false);
            var seq = DOTween.Sequence().SetId(icon).SetUpdate(true);
            seq.Append(icon.DOJump(toLocal, jumpPower, 1, duration).SetEase(Ease.OutQuad));
            seq.Join(icon.DOScale(0.6f, duration).SetEase(Ease.InQuad));
            int capturedIndex = index;
            seq.OnComplete(() =>
            {
                icon.gameObject.SetActive(false);
                busy[capturedIndex] = false;
            });
        }
    }
}
