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
        private static readonly Color NotReadyTint = DarkMatterGenesisUiPalette.SlotBackground;
        private static readonly Color HoverTint = new Color(0.28f, 0.38f, 0.32f, 0.98f);
        private static readonly Color HoverNotReadyTint = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.35f);
        private static readonly Color SelectedTint = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.55f);
        private static readonly Color ProgressOverlayColor = new Color(0.05f, 0.08f, 0.12f, 0.72f);
        private static readonly Color ProgressFillColor = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.85f);

        private static float SlotSize => HudLayoutMetrics.InventorySlotSize(96f);
        private static float IconInset => SlotSize * (1f - HudLayoutMetrics.InventoryIconScale) * 0.5f;

        private Image backgroundImage;
        private Image iconImage;
        private Image progressOverlay;
        private Image progressFill;
        private TextMeshProUGUI amountText;
        private Outline selectionOutline;

        private RecipeDefinition recipe;
        private InventorySystem inventory;
        private bool canCraft;
        private bool selected;
        private Action onSelected;

        public RecipeDefinition Recipe => recipe;

        public void Setup(RecipeDefinition recipeDefinition, bool craftable, InventorySystem inventorySystem, Action selectedHandler)
        {
            recipe = recipeDefinition;
            inventory = inventorySystem;
            canCraft = craftable;
            onSelected = selectedHandler;
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

            SetCraftProgress(0f);
            RefreshBackgroundColor(hovered: false);
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            if (selectionOutline != null)
                selectionOutline.enabled = isSelected;
            RefreshBackgroundColor(hovered: false);
        }

        /// <summary>0 = idle, 0–1 = craft progress (radial fills as craft completes).</summary>
        public void SetCraftProgress(float progress01)
        {
            EnsureBuilt();
            float t = Mathf.Clamp01(progress01);
            bool show = t > 0.001f;
            if (progressOverlay != null)
                progressOverlay.gameObject.SetActive(show);
            if (progressFill != null)
            {
                progressFill.gameObject.SetActive(show);
                progressFill.fillAmount = t;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            onSelected?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            RefreshBackgroundColor(hovered: true);
            RecipeHoverTooltip.Instance?.Show(recipe, inventory, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RefreshBackgroundColor(hovered: false);
            RecipeHoverTooltip.HideAny();
        }

        private void RefreshBackgroundColor(bool hovered)
        {
            if (backgroundImage == null)
                return;

            if (selected)
                backgroundImage.color = SelectedTint;
            else if (hovered)
                backgroundImage.color = canCraft ? HoverTint : HoverNotReadyTint;
            else
                backgroundImage.color = canCraft ? ReadyTint : NotReadyTint;
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

            selectionOutline = gameObject.GetComponent<Outline>();
            if (selectionOutline == null)
                selectionOutline = gameObject.AddComponent<Outline>();
            selectionOutline.effectColor = DarkMatterGenesisUiPalette.Gold;
            selectionOutline.effectDistance = new Vector2(2f, -2f);
            selectionOutline.enabled = false;

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

            GameObject overlayObj = new GameObject("CraftProgressOverlay", typeof(RectTransform));
            overlayObj.transform.SetParent(transform, false);
            StretchFull(overlayObj.GetComponent<RectTransform>());
            progressOverlay = overlayObj.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(progressOverlay);
            progressOverlay.color = ProgressOverlayColor;
            progressOverlay.raycastTarget = false;
            overlayObj.SetActive(false);

            GameObject fillObj = new GameObject("CraftProgressFill", typeof(RectTransform));
            fillObj.transform.SetParent(transform, false);
            StretchFull(fillObj.GetComponent<RectTransform>());
            progressFill = fillObj.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(progressFill);
            progressFill.color = ProgressFillColor;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Radial360;
            progressFill.fillOrigin = (int)Image.Origin360.Top;
            progressFill.fillClockwise = true;
            progressFill.fillAmount = 0f;
            progressFill.raycastTarget = false;
            fillObj.SetActive(false);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
