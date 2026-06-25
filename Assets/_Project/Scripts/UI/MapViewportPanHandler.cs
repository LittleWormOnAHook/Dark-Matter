using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    /// <summary>
    /// Drags map content inside a clipped viewport (left mouse pan).
    /// </summary>
    public class MapViewportPanHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private Canvas canvas;
        private Action<Vector2> onPanDelta;

        public void Initialize(Action<Vector2> panDeltaCallback)
        {
            onPanDelta = panDeltaCallback;
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (onPanDelta == null)
                return;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
                return;

            onPanDelta(eventData.delta / canvas.scaleFactor);
            eventData.Use();
        }
    }
}
