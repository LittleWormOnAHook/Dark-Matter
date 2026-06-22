using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    /// <summary>
    /// Forwards drag input on a child surface to the panel's single UIDragHandler.
    /// </summary>
    public class UiPanelDragRelay : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private UIDragHandler targetHandler;

        public void Initialize(UIDragHandler handler)
        {
            targetHandler = handler;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetHandler?.OnPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            targetHandler?.OnDrag(eventData);
        }
    }
}
