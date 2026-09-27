using Project.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    internal class PetToolbarSlotDropHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        private PetToolbarUI toolbarUi;

        public void Configure(PetToolbarUI owner)
        {
            toolbarUi = owner;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (toolbarUi == null || PetDragState.Pet == null)
                return;

            toolbarUi.HandlePetDropped(PetDragState.Pet);
            PetDragState.Clear();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || toolbarUi == null)
                return;

            toolbarUi.HandlePetCleared();
        }
    }
}
