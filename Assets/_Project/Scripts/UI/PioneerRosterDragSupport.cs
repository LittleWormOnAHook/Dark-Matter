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

    internal class PioneerRosterRowDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private string pioneerId;
        private CanvasGroup canvasGroup;
        private PioneerRosterPanelUI panel;

        public void Configure(PioneerRosterPanelUI ownerPanel, string id)
        {
            panel = ownerPanel;
            pioneerId = id;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(pioneerId))
                return;

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
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            PioneerRosterDragState.PioneerId = null;
            PioneerRosterDragState.SourceTrioSlot = -1;
            panel?.OnDragEnded();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || string.IsNullOrEmpty(pioneerId))
                return;

            PioneerRosterContextMenu.Instance?.ShowRosterRow(pioneerId, eventData.position);
        }
    }

    internal class PioneerTrioSlotDropHandler : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            if (string.IsNullOrEmpty(PioneerRosterDragState.PioneerId))
                return;

            if (PioneerRosterDragState.SourceTrioSlot >= 0
                && PioneerRosterDragState.SourceTrioSlot != slotIndex
                && eventData.pointerEnter == null)
            {
                panel.UnslotTrioSlot(PioneerRosterDragState.SourceTrioSlot);
            }

            PioneerRosterDragState.PioneerId = null;
            PioneerRosterDragState.SourceTrioSlot = -1;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || panel == null)
                return;

            PioneerRosterContextMenu.Instance?.ShowTrioSlot(slotIndex, eventData.position);
        }
    }
}
