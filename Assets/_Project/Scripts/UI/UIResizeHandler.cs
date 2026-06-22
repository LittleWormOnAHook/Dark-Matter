using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    public class UIResizeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public enum HandleCorner
        {
            BottomLeft,
            BottomRight
        }

        public RectTransform targetWindow;
        public HandleCorner corner = HandleCorner.BottomLeft;
        public Vector2 minSize = new Vector2(120f, 120f);
        public Vector2 maxSize = new Vector2(720f, 720f);
        public bool lockAspectRatio;

        private Canvas canvas;

        private void Awake()
        {
            if (targetWindow == null)
                targetWindow = transform.parent as RectTransform;

            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (targetWindow != null)
                targetWindow.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetWindow == null || canvas == null)
                return;

            Vector2 delta = eventData.delta / canvas.scaleFactor;
            Vector2 size = targetWindow.sizeDelta;

            if (corner == HandleCorner.BottomLeft)
                size += new Vector2(-delta.x, -delta.y);
            else
                size += new Vector2(delta.x, -delta.y);

            if (lockAspectRatio)
            {
                float max = Mathf.Max(size.x, size.y);
                size = Vector2.one * max;
            }

            size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
            size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);
            targetWindow.sizeDelta = size;
        }
    }
}
