using Project.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class PetSlotUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IEndDragHandler
    {
        public const int InventorySlotCount = PetManager.MaxOwnedPets;

        private Image backgroundImage;
        private Image iconImage;
        private TextMeshProUGUI emptyLabel;
        private Image selectionGlowImage;
        private PetController boundPet;
        private CanvasGroup canvasGroup;
        private float slotScale = 1f;
        private bool isBuilt;

        public PetController BoundPet => boundPet;

        public void Build(float scale)
        {
            if (isBuilt)
                return;

            slotScale = Mathf.Max(0.5f, scale);
            float slotSize = 72f * slotScale;

            if (GetComponent<CanvasGroup>() == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            else
                canvasGroup = GetComponent<CanvasGroup>();

            backgroundImage = gameObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(backgroundImage);
            backgroundImage.color = SurvivalPioneerUiPalette.SlotBackground;

            LayoutElement layout = gameObject.AddComponent<LayoutElement>();
            layout.minWidth = slotSize;
            layout.preferredWidth = slotSize;
            layout.minHeight = slotSize;
            layout.preferredHeight = slotSize;

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(transform, false);
            iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            float inset = 10f * slotScale;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);

            GameObject labelObj = new GameObject("EmptyLabel", typeof(RectTransform));
            labelObj.transform.SetParent(transform, false);
            emptyLabel = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(emptyLabel);
            emptyLabel.text = string.Empty;
            emptyLabel.fontSize = 11f * slotScale;
            emptyLabel.color = SurvivalPioneerUiPalette.SoftBeigeGray;
            emptyLabel.alignment = TextAlignmentOptions.Center;
            emptyLabel.raycastTarget = false;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.EnsureSelectionGlow(transform, ref selectionGlowImage);

            isBuilt = true;
        }

        public void Bind(PetController pet)
        {
            boundPet = pet;
            Refresh();
        }

        public void Refresh()
        {
            bool hasPet = boundPet != null;
            if (hasPet)
                boundPet.ApplyDefinition();

            if (iconImage != null)
            {
                iconImage.enabled = hasPet && boundPet.InventoryIcon != null;
                iconImage.sprite = hasPet ? boundPet.InventoryIcon : null;
                iconImage.color = Color.white;
            }

            if (emptyLabel != null)
            {
                if (hasPet && boundPet.InventoryIcon == null)
                {
                    emptyLabel.text = boundPet.DisplayName;
                    emptyLabel.enabled = true;
                }
                else
                {
                    emptyLabel.text = string.Empty;
                    emptyLabel.enabled = false;
                }
            }

            bool assigned = hasPet
                && PetManager.Instance != null
                && PetManager.Instance.ToolbarPet == boundPet;
            SetAssignedHighlight(assigned);
        }

        public void SetAssignedHighlight(bool assigned)
        {
            if (selectionGlowImage != null)
                selectionGlowImage.enabled = assigned;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (boundPet == null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                PetHoverTooltip.EnsureExists(canvas.transform);
                PetHoverTooltip.Instance?.Show(boundPet, eventData.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PetHoverTooltip.HideAny();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (boundPet == null || eventData.button != PointerEventData.InputButton.Right)
                return;

            PetHoverTooltip.HideAny();
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            PetContextMenu.EnsureExists(canvas.transform);
            PetContextMenu.Instance?.Show(boundPet, eventData.position);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (boundPet == null)
                return;

            PetDragState.Pet = boundPet;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.65f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            PetDragState.Clear();
        }
    }
}
