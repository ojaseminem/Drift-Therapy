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
    public class SnapCarousel : MonoBehaviour, IEndDragHandler
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

            DOTween.Kill(content, complete: false);
            if (animate)
                content.DOAnchorPosX(targetX, snapDuration).SetId(content).SetUpdate(true).SetEase(Ease.OutQuad);
            else
                content.anchoredPosition = new Vector2(targetX, content.anchoredPosition.y);

            if (index != CurrentIndex)
            {
                CurrentIndex = index;
                IndexChanged?.Invoke(CurrentIndex);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (content == null || cardWidth <= 0f) return;
            int nearest = Mathf.RoundToInt(-content.anchoredPosition.x / cardWidth);
            SnapTo(nearest);
        }
    }
}
