using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public static class UiPanelDragHandle
    {
        public static UIDragHandler EnsureHandler(RectTransform targetWindow)
        {
            if (targetWindow == null)
                return null;

            UIDragHandler dragHandler = targetWindow.GetComponent<UIDragHandler>();
            if (dragHandler == null)
                dragHandler = targetWindow.gameObject.AddComponent<UIDragHandler>();

            dragHandler.targetWindow = targetWindow;
            return dragHandler;
        }

        public static void Bind(RectTransform dragSurface, RectTransform targetWindow)
        {
            if (dragSurface == null || targetWindow == null)
                return;

            EnsureRaycastTarget(dragSurface);
            UIDragHandler handler = EnsureHandler(targetWindow);

            if (dragSurface == targetWindow)
                return;

            UiPanelDragRelay relay = dragSurface.GetComponent<UiPanelDragRelay>();
            if (relay == null)
                relay = dragSurface.gameObject.AddComponent<UiPanelDragRelay>();

            relay.Initialize(handler);

            UIDragHandler strayHandler = dragSurface.GetComponent<UIDragHandler>();
            if (strayHandler != null)
                Object.Destroy(strayHandler);
        }

        public static GameObject Create(
            Transform parent,
            RectTransform targetWindow,
            string title,
            float height,
            float titleFontSize = 12f)
        {
            GameObject dragObject = new GameObject("DragHandle", typeof(RectTransform));
            dragObject.transform.SetParent(parent, false);
            RectTransform dragRect = dragObject.GetComponent<RectTransform>();

            LayoutElement layout = dragObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;

            Image dragBg = dragObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(dragBg);
            dragBg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
            dragBg.raycastTarget = true;

            Bind(dragRect, targetWindow);

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(dragObject.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(8f, 0f);
            titleRect.offsetMax = new Vector2(-8f, 0f);
            TextMeshProUGUI label = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = title;
            label.fontSize = titleFontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.78f, 0.84f, 0.92f, 1f);
            label.raycastTarget = false;

            return dragObject;
        }

        private static void EnsureRaycastTarget(RectTransform dragSurface)
        {
            Graphic graphic = dragSurface.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image image = dragSurface.gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.001f);
                graphic = image;
            }

            graphic.raycastTarget = true;
        }
    }
}
