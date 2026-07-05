using UnityEngine;

namespace DriftTherapy
{
    /// <summary>
    /// Hosts one popup at a time. Each popup (Missions / Vehicles / Shop / Trials)
    /// is its own prefab; <see cref="Open"/> instantiates it as a full-screen child
    /// of this handler (which sits under the menu Canvas) and <see cref="Close"/>
    /// tears it down. Only one popup is shown at a time.
    /// </summary>
    [DisallowMultipleComponent]
    public class PopupHandler : MonoBehaviour
    {
        GameObject current;
        public bool IsOpen => current != null;

        public GameObject Open(GameObject prefab)
        {
            Close();
            if (!prefab) return null;
            current = Instantiate(prefab, transform);
            if (current.transform is RectTransform rt)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            current.transform.SetAsLastSibling();
            return current;
        }

        public void Close()
        {
            if (current) Destroy(current);
            current = null;
        }
    }
}
