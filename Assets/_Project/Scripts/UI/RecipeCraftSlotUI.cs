using System;
using Project.Crafting;
using Project.Data;
using Project.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class RecipeCraftSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color ReadyTint = new Color(0.22f, 0.32f, 0.26f, 0.95f);
        private static readonly Color NotReadyTint = new Color(0.18f, 0.19f, 0.23f, 0.92f);
        private static readonly Color HoverTint = new Color(0.28f, 0.38f, 0.32f, 0.98f);
        private static readonly Color HoverNotReadyTint = new Color(0.26f, 0.28f, 0.32f, 0.98f);

        private static float SlotSize => HudLayoutMetrics.InventorySlotSize(64f);
        private static float IconInset => SlotSize * (1f - HudLayoutMetrics.InventoryIconScale) * 0.5f;

        private Image backgroundImage;
        private Image iconImage;
        private TextMeshProUGUI amountText;

        private RecipeDefinition recipe;
        private InventorySystem inventory;
        private bool canCraft;
        private Action onCraftRequested;

        public void Setup(RecipeDefinition recipeDefinition, bool craftable, InventorySystem inventorySystem, Action craftHandler)
        {
            recipe = recipeDefinition;
            inventory = inventorySystem;
            canCraft = craftable;
            onCraftRequested = craftHandler;
            EnsureBuilt();

            Sprite icon = recipe?.DisplayIcon;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);

            if (amountText != null)
            {
                bool showAmount = recipe != null && recipe.outputAmount > 1;
                amountText.gameObject.SetActive(showAmount);
                amountText.text = showAmount ? recipe.outputAmount.ToString() : string.Empty;
            }

            backgroundImage.color = canCraft ? ReadyTint : NotReadyTint;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !canCraft)
                return;

            onCraftRequested?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = canCraft ? HoverTint : HoverNotReadyTint;

            RecipeHoverTooltip.Instance?.Show(recipe, inventory, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (backgroundImage != null)
                backgroundImage.color = canCraft ? ReadyTint : NotReadyTint;

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
                backgroundImage.color = NotReadyTint;

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

            GameObject amountObj = new GameObject("Amount", typeof(RectTransform));
            amountObj.transform.SetParent(iconObj.transform, false);
            RectTransform amountRect = amountObj.GetComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(1f, 0f);
            amountRect.anchorMax = new Vector2(1f, 0f);
            amountRect.pivot = new Vector2(1f, 0f);
            amountRect.anchoredPosition = new Vector2(-1f, 1f);
            amountRect.sizeDelta = new Vector2(slotSize * 0.55f, slotSize * 0.35f);
            amountText = amountObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(amountText);
            amountText.fontSize = Mathf.Max(8f, HudLayoutMetrics.ScaledInt(11f));
            amountText.alignment = TextAlignmentOptions.BottomRight;
            amountText.color = Color.white;
            amountText.raycastTarget = false;
            amountText.enableAutoSizing = true;
            amountText.fontSizeMin = 7f;
            amountText.fontSizeMax = Mathf.Max(8f, HudLayoutMetrics.ScaledInt(11f));
        }
    }
}
