using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>
    /// Standard paged-ScrollRect snap carousel: one card fills the viewport
    /// width, dragging and releasing snaps to the nearest card. Raises
    /// <see cref="IndexChanged"/> when the settled card changes — that's the
    /// wiring point between swiping and <see cref="GarageVehicleDisplay.Show"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SnapCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform content;
        [SerializeField] float snapDuration = 0.25f;

        int cardCount = 1;
        float cardWidth;

        public int CurrentIndex { get; private set; }
        public event Action<int> IndexChanged;

        /// <summary>The current per-card width (== the viewport's actual runtime width, which can differ from any edit-time-authored size once CanvasScaler blends toward the device's real aspect ratio).</summary>
        public float CardWidth => cardWidth;

        /// <summary>
        /// Call after populating cards — reads the viewport width as the per-card
        /// snap distance. Forces a synchronous canvas layout pass first:
        /// RectTransform.rect can report transient/incorrect values (seen in
        /// practice) if read mid-Start(), before Unity's own layout pass for this
        /// frame has actually run.
        /// </summary>
        public void SetCardCount(int count)
        {
            cardCount = Mathf.Max(1, count);
            if (scrollRect != null && scrollRect.viewport != null)
            {
                Canvas.ForceUpdateCanvases();
                cardWidth = scrollRect.viewport.rect.width;
            }
        }

        public void SnapTo(int index, bool animate = true)
        {
            if (content == null || cardWidth <= 0f) return;
            index = Mathf.Clamp(index, 0, cardCount - 1);
            float targetX = -index * cardWidth;

            // ScrollRect's own inertia/elastic-bounce keeps writing to
            // content.anchoredPosition every LateUpdate after a drag ends, which
            // fights the tween below and stops the carousel from ever fully
            // resting on a card. Hand exclusive control of the position to
            // DOTween for the duration of the snap, then give it back.
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.enabled = false;
            }

            DOTween.Kill(content, complete: false);
            if (animate)
            {
                content.DOAnchorPosX(targetX, snapDuration).SetId(content).SetUpdate(true).SetEase(Ease.OutQuad)
                    .OnComplete(() => { if (scrollRect != null) scrollRect.enabled = true; });
            }
            else
            {
                content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);
                if (scrollRect != null) scrollRect.enabled = true;
            }

            if (index != CurrentIndex)
            {
                CurrentIndex = index;
                IndexChanged?.Invoke(CurrentIndex);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Re-enable immediately so a new drag can interrupt an in-flight snap.
            if (scrollRect != null) scrollRect.enabled = true;
            DOTween.Kill(content, complete: false);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (content == null || cardWidth <= 0f) return;
            int nearest = Mathf.RoundToInt(-content.anchoredPosition.x / cardWidth);
            SnapTo(nearest);
        }
    }
}
