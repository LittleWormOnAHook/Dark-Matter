using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class UIResizeHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public enum HandleCorner
        {
            BottomLeft,
            BottomRight
        }

        public enum HandleType
        {
            BottomLeft,
            Bottom,
            BottomRight,
            Left,
            Right,
            TopLeft,
            Top,
            TopRight
        }

        public RectTransform targetWindow;
        public HandleCorner corner = HandleCorner.BottomLeft;
        public HandleType handleType = HandleType.BottomLeft;
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
            Vector2 pivot = targetWindow.pivot;
            Vector2 size = targetWindow.sizeDelta;
            Vector2 pos = targetWindow.anchoredPosition;

            ApplyResizeDelta(ref size, ref pos, pivot, delta, handleType);

            if (lockAspectRatio)
            {
                float max = Mathf.Max(size.x, size.y);
                size = Vector2.one * max;
            }

            size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
            size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);
            targetWindow.sizeDelta = size;
            targetWindow.anchoredPosition = pos;
        }

        internal static void ApplyResizeDelta(
            ref Vector2 size,
            ref Vector2 pos,
            Vector2 pivot,
            Vector2 delta,
            HandleType type)
        {
            switch (type)
            {
                case HandleType.BottomLeft:
                    size.x -= delta.x;
                    size.y -= delta.y;
                    pos.x += delta.x * pivot.x;
                    pos.y += delta.y * pivot.y;
                    break;
                case HandleType.Bottom:
                    size.y -= delta.y;
                    pos.y += delta.y * pivot.y;
                    break;
                case HandleType.BottomRight:
                    size.x += delta.x;
                    size.y -= delta.y;
                    pos.x += delta.x * (pivot.x - 1f);
                    pos.y += delta.y * pivot.y;
                    break;
                case HandleType.Left:
                    size.x -= delta.x;
                    pos.x += delta.x * pivot.x;
                    break;
                case HandleType.Right:
                    size.x += delta.x;
                    pos.x += delta.x * (pivot.x - 1f);
                    break;
                case HandleType.TopLeft:
                    size.x -= delta.x;
                    size.y += delta.y;
                    pos.x += delta.x * pivot.x;
                    pos.y += delta.y * (pivot.y - 1f);
                    break;
                case HandleType.Top:
                    size.y += delta.y;
                    pos.y += delta.y * (pivot.y - 1f);
                    break;
                case HandleType.TopRight:
                    size.x += delta.x;
                    size.y += delta.y;
                    pos.x += delta.x * (pivot.x - 1f);
                    pos.y += delta.y * (pivot.y - 1f);
                    break;
            }
        }
    }

    /// <summary>
    /// Adds draggable corner and edge resize handles to a panel.
    /// </summary>
    public static class UiPanelResizeHandles
    {
        private const float CornerSize = 14f;
        private const float EdgeThickness = 8f;

        public static void AddAll(
            Transform parent,
            RectTransform targetWindow,
            bool lockAspectRatio,
            Vector2 minSize,
            Vector2 maxSize)
        {
            if (parent == null || targetWindow == null)
                return;

            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.BottomLeft, lockAspectRatio, minSize, maxSize, CornerSize, CornerSize);
            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.BottomRight, lockAspectRatio, minSize, maxSize, CornerSize, CornerSize);
            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.TopLeft, lockAspectRatio, minSize, maxSize, CornerSize, CornerSize);
            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.TopRight, lockAspectRatio, minSize, maxSize, CornerSize, CornerSize);
            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.Left, lockAspectRatio, minSize, maxSize, EdgeThickness, 0f);
            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.Right, lockAspectRatio, minSize, maxSize, EdgeThickness, 0f);
            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.Top, lockAspectRatio, minSize, maxSize, 0f, EdgeThickness);
            CreateHandle(parent, targetWindow, UIResizeHandler.HandleType.Bottom, lockAspectRatio, minSize, maxSize, 0f, EdgeThickness);
        }

        private static void CreateHandle(
            Transform parent,
            RectTransform targetWindow,
            UIResizeHandler.HandleType type,
            bool lockAspectRatio,
            Vector2 minSize,
            Vector2 maxSize,
            float width,
            float height)
        {
            GameObject handleObject = new GameObject($"Resize_{type}", typeof(RectTransform));
            handleObject.transform.SetParent(parent, false);
            handleObject.transform.SetAsLastSibling();

            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            ConfigureHandleRect(handleRect, type, width, height);

            Image handleImage = handleObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(handleImage);
            handleImage.color = new Color(0.55f, 0.65f, 0.78f, type is UIResizeHandler.HandleType.Left
                or UIResizeHandler.HandleType.Right
                or UIResizeHandler.HandleType.Top
                or UIResizeHandler.HandleType.Bottom
                ? 0.35f
                : 0.75f);
            handleImage.raycastTarget = true;

            UIResizeHandler resizeHandler = handleObject.AddComponent<UIResizeHandler>();
            resizeHandler.targetWindow = targetWindow;
            resizeHandler.lockAspectRatio = lockAspectRatio;
            resizeHandler.minSize = minSize;
            resizeHandler.maxSize = maxSize;
            resizeHandler.handleType = type;
            resizeHandler.corner = type is UIResizeHandler.HandleType.BottomRight or UIResizeHandler.HandleType.TopRight
                ? UIResizeHandler.HandleCorner.BottomRight
                : UIResizeHandler.HandleCorner.BottomLeft;
        }

        private static void ConfigureHandleRect(RectTransform rect, UIResizeHandler.HandleType type, float width, float height)
        {
            float corner = CornerSize;
            float edgeW = width > 0f ? width : CornerSize;
            float edgeH = height > 0f ? height : CornerSize;

            switch (type)
            {
                case UIResizeHandler.HandleType.BottomLeft:
                    SetHandle(rect, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(corner, corner));
                    break;
                case UIResizeHandler.HandleType.BottomRight:
                    SetHandle(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(corner, corner));
                    break;
                case UIResizeHandler.HandleType.TopLeft:
                    SetHandle(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(corner, corner));
                    break;
                case UIResizeHandler.HandleType.TopRight:
                    SetHandle(rect, Vector2.one, Vector2.one, Vector2.one, new Vector2(corner, corner));
                    break;
                case UIResizeHandler.HandleType.Left:
                    SetHandle(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(edgeW, 48f));
                    break;
                case UIResizeHandler.HandleType.Right:
                    SetHandle(rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(edgeW, 48f));
                    break;
                case UIResizeHandler.HandleType.Top:
                    SetHandle(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(48f, edgeH));
                    break;
                case UIResizeHandler.HandleType.Bottom:
                    SetHandle(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(48f, edgeH));
                    break;
            }
        }

        private static void SetHandle(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
