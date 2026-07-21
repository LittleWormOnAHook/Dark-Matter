using System.Collections.Generic;
using Project.Pioneers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    internal static class PioneerRosterDragState
    {
        public static string PioneerId;
        public static int SourceTrioSlot = -1;
    }

    internal class PioneerRosterRowDragHandler : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private string pioneerId;
        private CanvasGroup canvasGroup;
        private PioneerRosterPanelUI panel;
        private ScrollRect parentScrollRect;

        public void Configure(PioneerRosterPanelUI ownerPanel, string id)
        {
            panel = ownerPanel;
            pioneerId = id;
            parentScrollRect = GetComponentInParent<ScrollRect>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // Let parent ScrollRect participate so short drags still scroll the roster column.
            if (parentScrollRect != null)
                ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(pioneerId))
                return;

            if (parentScrollRect != null)
                parentScrollRect.enabled = false;

            PioneerRosterDragState.PioneerId = pioneerId;
            PioneerRosterDragState.SourceTrioSlot = -1;
            canvasGroup.alpha = 0.55f;
            canvasGroup.blocksRaycasts = false;
            panel?.OnDragStarted(pioneerId);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (parentScrollRect != null)
                parentScrollRect.enabled = true;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            if (!string.IsNullOrEmpty(PioneerRosterDragState.PioneerId))
                TryExecuteDrop(eventData);

            PioneerRosterDragState.PioneerId = null;
            PioneerRosterDragState.SourceTrioSlot = -1;
            panel?.OnDragEnded();
        }

        private static void TryExecuteDrop(PointerEventData eventData)
        {
            if (EventSystem.current == null)
                return;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            for (int i = 0; i < results.Count; i++)
            {
                PioneerTrioSlotDropHandler dropHandler = results[i].gameObject.GetComponentInParent<PioneerTrioSlotDropHandler>();
                if (dropHandler == null)
                    continue;

                ExecuteEvents.Execute(dropHandler.gameObject, eventData, ExecuteEvents.dropHandler);
                return;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || string.IsNullOrEmpty(pioneerId))
                return;

            PioneerRosterContextMenu.Instance?.ShowRosterRow(pioneerId, eventData.position);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (panel == null || string.IsNullOrEmpty(pioneerId))
                return;

            SkilledPioneerRecord record = panel.GetPioneerRecordForTooltip(pioneerId);
            if (record == null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            PioneerHoverTooltip.EnsureExists(canvas.transform);
            PioneerHoverTooltip.Instance?.Show(record, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PioneerHoverTooltip.HideAny();
        }
    }

    internal class PioneerTrioSlotDropHandler : MonoBehaviour,
        IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private int slotIndex;
        private PioneerRosterPanelUI panel;
        private CanvasGroup canvasGroup;

        public void Configure(PioneerRosterPanelUI ownerPanel, int slot)
        {
            panel = ownerPanel;
            slotIndex = slot;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (panel == null || string.IsNullOrEmpty(PioneerRosterDragState.PioneerId))
                return;

            panel.HandlePioneerDroppedOnTrioSlot(slotIndex, PioneerRosterDragState.PioneerId);
            PioneerRosterDragState.PioneerId = null;
            PioneerRosterDragState.SourceTrioSlot = -1;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (panel == null)
                return;

            string assignedId = panel.GetTrioDraftId(slotIndex);
            if (string.IsNullOrEmpty(assignedId))
                return;

            PioneerRosterDragState.PioneerId = assignedId;
            PioneerRosterDragState.SourceTrioSlot = slotIndex;
            canvasGroup.alpha = 0.65f;
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            if (string.IsNullOrEmpty(PioneerRosterDragState.PioneerId))
                return;

            if (PioneerRosterDragState.SourceTrioSlot >= 0
                && PioneerRosterDragState.SourceTrioSlot != slotIndex)
            {
                PioneerTrioSlotDropHandler dropHandler = FindDropTarget(eventData);
                if (dropHandler != null && dropHandler != this)
                {
                    dropHandler.OnDrop(eventData);
                }
                else if (eventData.pointerEnter == null)
                {
                    panel.UnslotTrioSlot(PioneerRosterDragState.SourceTrioSlot);
                }
            }

            PioneerRosterDragState.PioneerId = null;
            PioneerRosterDragState.SourceTrioSlot = -1;
        }

        private static PioneerTrioSlotDropHandler FindDropTarget(PointerEventData eventData)
        {
            if (EventSystem.current == null)
                return null;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            for (int i = 0; i < results.Count; i++)
            {
                PioneerTrioSlotDropHandler dropHandler = results[i].gameObject.GetComponentInParent<PioneerTrioSlotDropHandler>();
                if (dropHandler != null)
                    return dropHandler;
            }

            return null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || panel == null)
                return;

            PioneerRosterContextMenu.Instance?.ShowTrioSlot(slotIndex, eventData.position);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (panel == null)
                return;

            string assignedId = panel.GetTrioDraftId(slotIndex);
            if (string.IsNullOrEmpty(assignedId))
                return;

            SkilledPioneerRecord record = panel.GetPioneerRecordForTooltip(assignedId);
            if (record == null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            PioneerHoverTooltip.EnsureExists(canvas.transform);
            PioneerHoverTooltip.Instance?.Show(record, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PioneerHoverTooltip.HideAny();
        }
    }
}
