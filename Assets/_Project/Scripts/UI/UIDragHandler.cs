using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    public class UIDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Header("Settings")]
        [Tooltip("The RectTransform of the window that should be dragged. If null, drags this GameObject's RectTransform.")]
        public RectTransform targetWindow;

        private Canvas canvas;
        private RectTransform rectTransform;

        private void Awake()
        {
            if (targetWindow == null)
            {
                targetWindow = GetComponent<RectTransform>();
            }
            rectTransform = targetWindow;
            canvas = GetComponentInParent<Canvas>();
        }

        private void OnTransformParentChanged()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (rectTransform != null)
                rectTransform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rectTransform == null)
                return;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                return;

            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}