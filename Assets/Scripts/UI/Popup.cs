using UnityEngine;
using UnityEngine.UI;

namespace DriftTherapy
{
    /// <summary>Base for popup prefabs: wires the close button to the parent handler.</summary>
    public class Popup : MonoBehaviour
    {
        public Button closeButton;
        protected PopupHandler Handler { get; private set; }

        protected virtual void Awake()
        {
            Handler = GetComponentInParent<PopupHandler>();
            if (closeButton)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Close()
        {
            if (Handler) Handler.Close();
            else Destroy(gameObject);
        }
    }
}
