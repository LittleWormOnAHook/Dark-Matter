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
        private static readonly Color ScrollSlotTint = SurvivalPioneerUiPalette.SlotBackground;
        private static readonly Color ScrollSlotHoverTint = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.42f);
        private static float SlotSize => HudLayoutMetrics.InventorySlotSize(64f);
        private static float IconInset => SlotSize * (1f - HudLayoutMetrics.InventoryIconScale) * 0.5f;

        private Image backgroundImage;
        private Image iconImage;
        private string recipeId;
        private int slotIndex;
        private RecipeDefinition recipe;
        private Action<int> onLearnRequested;

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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                onLearnRequested?.Invoke(slotIndex);
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
