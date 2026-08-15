using System;
using Project.Crafting;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class RecipeScrollSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color ScrollSlotTint = DarkMatterGenesisUiPalette.SlotBackground;
        private static readonly Color ScrollSlotHoverTint = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.42f);
        private static float SlotSize => HudLayoutMetrics.InventorySlotSize(80f);
        private static float IconInset => SlotSize * (1f - HudLayoutMetrics.InventoryIconScale) * 0.5f;

        private Image backgroundImage;
        private Image iconImage;
        private string recipeId;
        private int slotIndex;
        private RecipeDefinition recipe;
        private Action<int> onLearnRequested;
        private GameObject learnConfirmPanel;
        private Button learnButton;

        public void Setup(int index, string id, RecipeDefinition recipeDefinition, Action<int> learnHandler)
        {
            slotIndex = index;
            recipeId = id;
            recipe = recipeDefinition;
            onLearnRequested = learnHandler;
            EnsureBuilt();

            Sprite icon = recipe?.DisplayIcon;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        /// <summary>
        /// Two-step confirm: right-click toggles a small "Learn" button on the slot instead of
        /// learning the recipe immediately, so a stray right-click can't accidentally burn a scroll.
        /// The recipe is only actually learned when the player explicitly clicks that button.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ToggleLearnConfirm();
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
                HideLearnConfirm();
        }

        private void ToggleLearnConfirm()
        {
            EnsureLearnConfirmBuilt();
            learnConfirmPanel.SetActive(!learnConfirmPanel.activeSelf);
        }

        private void HideLearnConfirm()
        {
            if (learnConfirmPanel != null)
                learnConfirmPanel.SetActive(false);
        }

        private void HandleLearnButtonClicked()
        {
            HideLearnConfirm();
            onLearnRequested?.Invoke(slotIndex);
        }

        private void EnsureLearnConfirmBuilt()
        {
            if (learnConfirmPanel != null)
                return;

            // Overlay Learn inside the slot bounds so RectMask2D on the pending-scroll
            // viewport cannot clip the top of the confirm button (was anchored above the tile).
            learnConfirmPanel = new GameObject("LearnConfirm", typeof(RectTransform));
            learnConfirmPanel.transform.SetParent(transform, false);

            RectTransform panelRect = learnConfirmPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.08f);
            panelRect.anchorMax = new Vector2(0.92f, 0.42f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            Image panelBackground = learnConfirmPanel.AddComponent<Image>();
            panelBackground.color = DarkMatterGenesisUiPalette.WithAlpha(Color.black, 0.88f);

            GameObject buttonObject = new GameObject("LearnButton", typeof(RectTransform));
            buttonObject.transform.SetParent(learnConfirmPanel.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.offsetMin = new Vector2(2f, 2f);
            buttonRect.offsetMax = new Vector2(-2f, -2f);

            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = DarkMatterGenesisUiPalette.RichFuchsia;
            learnButton = buttonObject.AddComponent<Button>();
            learnButton.targetGraphic = buttonImage;
            learnButton.onClick.AddListener(HandleLearnButtonClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI learnLabel = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(learnLabel);
            learnLabel.text = "Learn";
            learnLabel.fontSize = 13f;
            learnLabel.alignment = TextAlignmentOptions.Center;
            learnLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            learnLabel.raycastTarget = false;

            learnConfirmPanel.transform.SetAsLastSibling();
            learnConfirmPanel.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = ScrollSlotHoverTint;

            RecipeHoverTooltip.Instance?.Show(recipe, null, eventData.position, pendingScroll: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = ScrollSlotTint;

            RecipeHoverTooltip.HideAny();
        }

        private void EnsureBuilt()
        {
            if (backgroundImage != null)
                return;

            if (GetComponent<CanvasGroup>() == null)
                gameObject.AddComponent<CanvasGroup>();

            backgroundImage = gameObject.GetComponent<Image>();
            if (backgroundImage == null)
                backgroundImage = gameObject.AddComponent<Image>();

            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplySlotFrame(backgroundImage);
            else
                backgroundImage.color = ScrollSlotTint;

            float slotSize = SlotSize;
            LayoutElement layout = gameObject.GetComponent<LayoutElement>();
            if (layout == null)
                layout = gameObject.AddComponent<LayoutElement>();
            layout.minWidth = slotSize;
            layout.minHeight = slotSize;
            layout.preferredWidth = slotSize;
            layout.preferredHeight = slotSize;

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            float inset = IconInset;
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);
            iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
    }
}
