using Project.Pet;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    internal class PetEntryDragHandler : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private PetController pet;
        private CanvasGroup canvasGroup;

        public void Configure(PetController boundPet)
        {
            pet = boundPet;
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (pet == null)
                return;

            PetDragState.Pet = pet;
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            PetDragState.Clear();
        }
    }
}
