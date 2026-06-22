using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Top-most UI layer for drag ghosts and tooltips so they render above the journal overlay.
    /// </summary>
    public static class UiFrontLayer
    {
        private static RectTransform layerRect;

        public static Transform Get(Transform canvasRoot)
        {
            if (layerRect != null)
                return layerRect;

            GameObject layerObject = new GameObject("UiFrontLayer", typeof(RectTransform));
            layerObject.transform.SetParent(canvasRoot, false);

            layerRect = layerObject.GetComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;

            Canvas canvas = layerObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

            layerObject.AddComponent<GraphicRaycaster>();
            return layerRect;
        }

        public static void ReparentToFront(Transform target, Transform canvasRoot, bool worldPositionStays = true)
        {
            if (target == null || canvasRoot == null)
                return;

            target.SetParent(Get(canvasRoot), worldPositionStays);
            target.SetAsLastSibling();
        }

        public static void ReparentFullScreenToFront(Transform target, Transform canvasRoot)
        {
            if (target == null || canvasRoot == null)
                return;

            ReparentToFront(target, canvasRoot);
            StretchToLayer(target as RectTransform);
        }

        private static void StretchToLayer(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public static void BringLayerToFront(Transform canvasRoot)
        {
            if (layerRect == null || canvasRoot == null)
                return;

            layerRect.SetParent(canvasRoot, false);
            StretchToLayer(layerRect);
            layerRect.SetAsLastSibling();
        }
    }
}
