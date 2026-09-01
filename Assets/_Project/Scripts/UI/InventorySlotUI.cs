using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using Project.Inventory;
using Project.Data;
using Project.Audio;

namespace Project.UI
{
    public class InventorySlotUI : MonoBehaviour,
        IPointerDownHandler,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public enum SelectionHighlightMode
        {
            CircleGlow,
            WeaponGoldBorder
        }

        [Header("UI")]
        public Image iconImage;
        public TextMeshProUGUI amountText;
        public Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Layout")]
        [Tooltip("When enabled, icon/amount rects are not rebuilt in Awake. Set via UI Layout Editor.")]
        [SerializeField] private bool preserveManualLayout;

        public InventorySystem.InventorySlot slot { get; private set; }
        public int slotIndex;
        public bool PreservesManualLayout => preserveManualLayout;

        private const float AmmoTypeIconScale = 0.2f;

        private InventorySystem inventory;
        private EquipmentController equipmentController;
        private InventoryItemActions itemActions;
        private WeaponAmmoState ammoState;
        private GameObject dragGhost;
        private Image selectionGlowImage;
        private Image ammoTypeIcon;
        private Color defaultBackgroundColor = DarkMatterGenesisUiPalette.SlotBackground;
        private SelectionHighlightMode selectionHighlightMode = SelectionHighlightMode.CircleGlow;
        private bool selectionVisualBuilt;
        private bool wasDragged;
        private bool isSelected;
        private bool isLocked;
        private bool suppressAmountOutline;

        private void Reset()
        {
            WireSerializedRefs(allowCreateCanvasGroup: true);
        }

        private void OnValidate()
        {
            WireSerializedRefs(allowCreateCanvasGroup: false);
        }

        private void WireSerializedRefs(bool allowCreateCanvasGroup)
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && allowCreateCanvasGroup)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (iconImage == null)
            {
                Transform icon = transform.Find("Icon");
                if (icon != null)
                    iconImage = icon.GetComponent<Image>();
            }

            if (amountText == null)
            {
                Transform amount = transform.Find("Amount");
                if (amount != null)
                    amountText = amount.GetComponent<TextMeshProUGUI>();
                if (amountText == null)
                {
                    Transform nested = transform.Find("Icon/Amount");
                    if (nested != null)
                        amountText = nested.GetComponent<TextMeshProUGUI>();
                }
            }

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();
        }

        private void Awake()
        {
            WireSerializedRefs(allowCreateCanvasGroup: true);
            ApplyShiftSlotVisuals();
            ApplyHudSlotMetrics();
        }

        private void ApplyShiftSlotVisuals()
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme == null)
            {
                if (backgroundImage != null)
                    defaultBackgroundColor = backgroundImage.color;
                return;
            }

            if (backgroundImage != null)
            {
                theme.ApplySlotFrame(backgroundImage);
                defaultBackgroundColor = backgroundImage.color;
            }

            EnsureSelectionVisual(theme);
        }

        /// <summary>Weapon hotbar slots (1–4) use a thin gold sliced border instead of circle glow.</summary>
        public void SetSelectionHighlightMode(SelectionHighlightMode mode)
        {
            if (selectionHighlightMode == mode && selectionVisualBuilt)
                return;

            selectionHighlightMode = mode;
            DestroySelectionVisual();
            ApplyShiftSlotVisuals();
        }

        private void DestroySelectionVisual()
        {
            DestroySelectionChild("SelectionGlow");
            DestroySelectionChild("SelectionBorder");
            selectionGlowImage = null;
            selectionVisualBuilt = false;
        }

        private void DestroySelectionChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return;

            DestroyImmediate(child.gameObject);
        }

        private void EnsureSelectionVisual(ShiftUiTheme theme)
        {
            if (selectionVisualBuilt || theme == null)
                return;

            if (selectionHighlightMode == SelectionHighlightMode.WeaponGoldBorder)
                theme.EnsureWeaponSelectionBorder(transform, ref selectionGlowImage);
            else
                theme.EnsureSelectionGlow(transform, ref selectionGlowImage);

            selectionVisualBuilt = true;
        }

        public void ApplyHudSlotMetrics(float? slotSizeOverride = null)
        {
            float slotSize = slotSizeOverride ?? HudLayoutMetrics.InventorySlotSize(64f);

            if (!preserveManualLayout)
            {
                RectTransform slotRect = transform as RectTransform;
                if (slotRect != null)
                    slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            }

            ApplyIconLayout();
            ConfigureAmountText();
            LayoutAmmoTypeIcon();
        }

        public void SetHudAmountPresentation(bool plainAmountText)
        {
            suppressAmountOutline = plainAmountText;
            ConfigureAmountText();
        }

        private void ApplyIconLayout()
        {
            if (iconImage == null)
                return;

            RectTransform iconRect = iconImage.rectTransform;
            float anchor = (1f - HudLayoutMetrics.InventoryIconScale) * 0.5f;
            iconRect.anchorMin = new Vector2(anchor, anchor);
            iconRect.anchorMax = new Vector2(1f - anchor, 1f - anchor);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            iconImage.preserveAspect = true;
        }

        public static float GetDragIconSize(RectTransform slotRect)
        {
            float slotSize = slotRect != null && slotRect.rect.width > 0f
                ? slotRect.rect.width
                : HudLayoutMetrics.InventorySlotSize(64f);
            return slotSize * HudLayoutMetrics.InventoryIconScale;
        }

        private void ConfigureAmountText()
        {
            if (amountText == null)
                return;

            if (iconImage != null && amountText.transform.parent != iconImage.transform)
                amountText.transform.SetParent(iconImage.transform, false);

            TmpUiHelper.ApplyDefaultFont(amountText);

            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(amountText, semiBold: true);

            RectTransform amountRect = amountText.rectTransform;
            amountRect.anchorMin = new Vector2(1f, 0f);
            amountRect.anchorMax = new Vector2(1f, 0f);
            amountRect.pivot = new Vector2(1f, 0f);
            amountRect.anchoredPosition = new Vector2(-1f, 1f);
            amountRect.sizeDelta = new Vector2(30f, 18f);

            amountText.raycastTarget = false;
            amountText.enableAutoSizing = true;
            amountText.fontSizeMin = 11f;
            amountText.fontSizeMax = 16f;
            amountText.fontStyle = FontStyles.Bold;
            amountText.color = Color.white;
            amountText.alignment = TextAlignmentOptions.BottomRight;
            amountText.margin = Vector4.zero;

            if (amountText.font == null)
                return;

            if (suppressAmountOutline)
                amountText.outlineWidth = 0f;
            else
                TmpUiHelper.TryApplyOutline(amountText, 0.25f, Color.black);
        }

        public void Initialize(InventorySystem inventorySystem)
        {
            inventory = inventorySystem;
        }

        public void SetEquipmentController(EquipmentController controller)
        {
            equipmentController = controller;
        }

        public void SetItemActions(InventoryItemActions actions)
        {
            itemActions = actions;
        }

        public void SetAmmoState(WeaponAmmoState state)
        {
            ammoState = state;
            RefreshAmmoTypeIcon();
        }

        public void UpdateSlot(InventorySystem.InventorySlot newSlot)
        {
            slot = newSlot;

            if (slot == null || slot.IsEmpty)
            {
                ClearSlot();
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = slot.item.icon;
                iconImage.enabled = true;
            }

            if (amountText != null)
            {
                bool showAmount = slot.amount > 1;
                amountText.text = slot.amount.ToString();
                amountText.gameObject.SetActive(showAmount);
            }

            ApplySelectionColor();
            RefreshAmmoTypeIcon();
        }

        public void ClearSlot()
        {
            if (iconImage != null) iconImage.enabled = false;
            if (amountText != null)
            {
                amountText.text = "";
                amountText.gameObject.SetActive(false);
            }

            ApplySelectionColor();
            RefreshAmmoTypeIcon();
        }

        /// <summary>
        /// Lower-left ammo-type badge for ranged hotbar weapons (~20% of slot size).
        /// </summary>
        public void RefreshAmmoTypeIcon()
        {
            EnsureAmmoTypeIcon();
            if (ammoTypeIcon == null)
                return;

            Sprite icon = ResolveLoadedAmmoIcon();
            if (icon == null)
            {
                ammoTypeIcon.enabled = false;
                ammoTypeIcon.sprite = null;
                return;
            }

            LayoutAmmoTypeIcon();
            ammoTypeIcon.sprite = icon;
            ammoTypeIcon.enabled = true;
        }

        private Sprite ResolveLoadedAmmoIcon()
        {
            if (isLocked || slot == null || slot.IsEmpty || slot.item == null || !slot.item.IsRangedWeapon)
                return null;

            if (inventory == null || !inventory.IsHotbarIndex(slotIndex))
                return null;

            int hotbarIndex = slotIndex - inventory.inventorySize;
            if (equipmentController != null && !equipmentController.IsWeaponHotbarSlot(hotbarIndex))
                return null;

            if (ammoState == null)
                return null;

            if (slot.item.isMiningTool)
            {
                ItemData plasma = WeaponAmmoState.ResolvePlasmaFuelItem();
                return plasma != null ? plasma.icon : null;
            }

            ItemData loadedAmmo = ammoState.GetLoadedAmmoItem(hotbarIndex);
            if (loadedAmmo != null && loadedAmmo.icon != null)
                return loadedAmmo.icon;

            ItemData fallback = WeaponAmmoState.ResolveStandardAmmoItem(slot.item);
            return fallback != null ? fallback.icon : null;
        }

        private void EnsureAmmoTypeIcon()
        {
            if (ammoTypeIcon != null)
                return;

            Transform existing = transform.Find("AmmoTypeIcon");
            if (existing != null)
            {
                ammoTypeIcon = existing.GetComponent<Image>();
                if (ammoTypeIcon == null)
                    ammoTypeIcon = existing.gameObject.AddComponent<Image>();
            }
            else
            {
                GameObject iconObject = new GameObject("AmmoTypeIcon", typeof(RectTransform));
                iconObject.transform.SetParent(transform, false);
                ammoTypeIcon = iconObject.AddComponent<Image>();
            }

            ammoTypeIcon.raycastTarget = false;
            ammoTypeIcon.preserveAspect = true;
            ammoTypeIcon.color = Color.white;
            ammoTypeIcon.enabled = false;
            LayoutAmmoTypeIcon();
            ammoTypeIcon.transform.SetAsLastSibling();
        }

        private void LayoutAmmoTypeIcon()
        {
            if (ammoTypeIcon == null)
                return;

            RectTransform slotRect = transform as RectTransform;
            float slotSize = slotRect != null && slotRect.rect.width > 0f
                ? slotRect.rect.width
                : HudLayoutMetrics.InventorySlotSize(64f);
            float iconSize = Mathf.Max(8f, slotSize * AmmoTypeIconScale);

            RectTransform iconRect = ammoTypeIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0f);
            iconRect.anchorMax = new Vector2(0f, 0f);
            iconRect.pivot = new Vector2(0f, 0f);
            iconRect.anchoredPosition = new Vector2(HudLayoutMetrics.Scaled(2f), HudLayoutMetrics.Scaled(2f));
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            ApplySelectionColor();
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            ApplySelectionColor();

            if (isLocked)
                ClearSlot();
        }

        public bool IsLocked => isLocked;

        private void ApplySelectionColor()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isLocked
                    ? DarkMatterGenesisUiPalette.LockedSlotBackground
                    : defaultBackgroundColor;
            }

            if (selectionGlowImage != null)
                selectionGlowImage.enabled = isSelected && !isLocked;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            wasDragged = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isLocked || slot == null || slot.IsEmpty || inventory == null) return;

            if (wasDragged)
            {
                wasDragged = false;
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (UiInputGuard.BlocksGameplayEquipmentInput)
                    return;

                GameAudioManager.Instance?.PlayInventoryItemClick();

                if (equipmentController != null &&
                    inventory != null &&
                    slot.item != null &&
                    slot.item.IsEquippable)
                {
                    if (inventory.IsToolbarIndex(slotIndex))
                    {
                        equipmentController.SelectToolbarSlot(inventory.ToToolbarSlotIndex(slotIndex), allowToggleOff: true);
                        return;
                    }

                    if (inventory.IsHotbarIndex(slotIndex))
                    {
                        int hotbarIndex = slotIndex - inventory.inventorySize;
                        if (equipmentController.IsWeaponHotbarSlot(hotbarIndex))
                        {
                            int weaponSlot = equipmentController.GetWeaponSlotIndexForHotbar(hotbarIndex);
                            if (weaponSlot >= 0)
                                equipmentController.SelectWeaponSlot(weaponSlot);
                        }
                        else
                            equipmentController.SelectInventorySlot(slotIndex);
                        return;
                    }
                }

                if (itemActions != null)
                    itemActions.TryUse(slotIndex);
                else
                    inventory.UseItemAt(slotIndex);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                ItemHoverTooltip.HideAny();
                GameAudioManager.Instance?.PlayInventoryItemClick();
                InventoryContextMenu.Instance?.Show(slotIndex, eventData.position);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLocked || slot == null || slot.IsEmpty || slot.item == null)
                return;

            ItemHoverTooltip.NotifyHover(this);
            ItemHoverTooltip.Instance?.Show(slot.item, slot.amount, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemHoverTooltip.NotifyHoverEnd(this);
            ItemHoverTooltip.Instance?.Hide();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            ItemHoverTooltip.HideAny();

            if (isLocked) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (slot == null || slot.IsEmpty) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            wasDragged = true;

            dragGhost = new GameObject("DragGhost", typeof(RectTransform));
            UiFrontLayer.ReparentToFront(dragGhost.transform, canvas.transform);

            Image ghostImg = dragGhost.AddComponent<Image>();
            if (iconImage != null)
            {
                ghostImg.sprite = iconImage.sprite;
                ghostImg.color = new Color(1f, 1f, 1f, 0.75f);
            }
            ghostImg.raycastTarget = false;

            RectTransform ghostRt = dragGhost.GetComponent<RectTransform>();
            float iconSize = GetDragIconSize(transform as RectTransform);
            ghostRt.sizeDelta = new Vector2(iconSize, iconSize);
            ghostImg.preserveAspect = true;

            if (iconImage != null)
                ghostRt.position = eventData.position;
            else
                ghostRt.position = eventData.position;

            if (iconImage != null)
                iconImage.color = new Color(1f, 1f, 1f, 0.35f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
                dragGhost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
            {
                Destroy(dragGhost);
                dragGhost = null;
            }

            if (iconImage != null)
                iconImage.color = Color.white;

            InventorySlotUI source = eventData.pointerDrag?.GetComponent<InventorySlotUI>();
            if (source == null || source.slot == null || source.slot.IsEmpty || source.inventory == null)
                return;

            InventorySlotUI target = FindSlotUnderPointer(eventData, out bool hitAnyUi);
            if (target != null && target != source)
            {
                if (target.IsLocked)
                    return;
                // showLevelToast: once per failed drag onto hotbar/toolbar when level-gated.
                if (!source.inventory.CanAcceptItemAt(target.slotIndex, source.slot.item, showLevelToast: true))
                    return;

                source.inventory.MoveOrMergeSlots(source.slotIndex, target.slotIndex);
                return;
            }

            if (DMUiToolkitHud.TryDropOnSlot(eventData.position, source.slotIndex))
                return;

            if (DMUiToolkitHud.IsPointerOverHotbarOrTools(eventData.position))
                return;

            // Dragged the item off the inventory panel entirely (no UI at all under the pointer,
            // meaning the drop landed on the game world view) — drop it, same as the right-click
            // "Drop" context menu action, for any item.
            if (!hitAnyUi)
                source.DropIntoWorld();
        }

        private void DropIntoWorld()
        {
            if (itemActions != null)
                itemActions.TryDrop(slotIndex);
            else
                inventory?.DropItemAt(slotIndex);
        }

        private static InventorySlotUI FindSlotUnderPointer(PointerEventData eventData, out bool hitAnyUi)
        {
            hitAnyUi = false;
            if (EventSystem.current == null)
                return null;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            hitAnyUi = results.Count > 0;

            for (int i = 0; i < results.Count; i++)
            {
                InventorySlotUI slot = results[i].gameObject.GetComponentInParent<InventorySlotUI>();
                if (slot != null)
                    return slot;
            }

            return null;
        }
    }
}
