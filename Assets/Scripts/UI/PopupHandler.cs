using DG.Tweening;
using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Hosts one popup at a time. Each popup (Missions / Vehicles / Shop / Trials /
    /// Leaderboard) is its own prefab; <see cref="Open"/> instantiates it as a
    /// full-screen child of this handler (which sits under the menu Canvas) and
    /// <see cref="Close"/> tears it down. Only one popup is shown at a time.
    ///
    /// If the popup root has a <see cref="CanvasGroup"/> (all popups authored by
    /// <c>UIBuilder</c> do), open/close fade+scale via <see cref="UiJuice"/>.
    /// <see cref="IsOpen"/>/<see cref="Open"/> stay synchronous — a popup is
    /// logically closed the instant <see cref="Close"/> is called, even though its
    /// GameObject may still be fading out for a few frames.
    /// </summary>
    [DisallowMultipleComponent]
    public class PopupHandler : MonoBehaviour
    {
        GameObject current;
        GameObject outgoing;
        CanvasGroup outgoingGroup;

        public bool IsOpen => current != null;

        public GameObject Open(GameObject prefab)
        {
            Close();

            // A previous popup may still be fading out from a rapid nav tap —
            // skip its exit animation rather than let two popups overlap. Must
            // kill by outgoingGroup (the tween's actual id/target), not by the
            // GameObject — killing by the wrong key leaves the fade Sequence
            // running against a Destroy()'d CanvasGroup/RectTransform next frame.
            if (outgoing != null)
            {
                if (outgoingGroup != null) DOTween.Kill(outgoingGroup, complete: false);
                Destroy(outgoing);
                outgoing = null;
                outgoingGroup = null;
            }

            if (!prefab) return null;

            current = Instantiate(prefab, transform);
            if (current.transform is RectTransform rt)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            current.transform.SetAsLastSibling();

            var cg = current.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                current.transform.localScale = Vector3.one * 0.92f;
                UiJuice.FadeCanvasGroup(cg, 1f, 0.18f);
                current.transform.DOScale(1f, 0.18f).SetId(cg).SetUpdate(true);
            }

            return current;
        }

        public void Close()
        {
            if (current == null) return;

            var toClose = current;
            current = null; // IsOpen reflects "closed" immediately; a following Open() is never blocked

            var cg = toClose.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                Destroy(toClose);
                return;
            }

            if (outgoing != null)
            {
                if (outgoingGroup != null) DOTween.Kill(outgoingGroup, complete: false);
                Destroy(outgoing);
            }
            outgoing = toClose;
            outgoingGroup = cg;

            DOTween.Kill(cg, complete: false);
            var seq = DOTween.Sequence().SetId(cg).SetUpdate(true);
            seq.Append(cg.DOFade(0f, 0.15f).SetUpdate(true));
            seq.Join(toClose.transform.DOScale(0.92f, 0.15f).SetUpdate(true));
            seq.OnComplete(() =>
            {
                if (outgoing == toClose)
                {
                    Destroy(toClose);
                    outgoing = null;
                    outgoingGroup = null;
                }
            });
        }
    }
}
