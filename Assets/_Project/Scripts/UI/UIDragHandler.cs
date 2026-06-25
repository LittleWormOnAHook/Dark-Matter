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

        private RectTransform DragTarget => targetWindow != null ? targetWindow : transform as RectTransform;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        private void OnTransformParentChanged()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            RectTransform dragTarget = DragTarget;
            if (dragTarget != null)
                dragTarget.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            RectTransform dragTarget = DragTarget;
            if (dragTarget == null)
                return;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                return;

            dragTarget.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}
