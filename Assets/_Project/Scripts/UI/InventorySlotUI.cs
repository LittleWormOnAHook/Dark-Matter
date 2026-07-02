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
        [Header("UI")]
        public Image iconImage;
        public TextMeshProUGUI amountText;
        public Image backgroundImage;

        [Header("Layout")]
        [Tooltip("When enabled, icon/amount rects are not rebuilt in Awake. Set via UI Layout Editor.")]
        [SerializeField] private bool preserveManualLayout;

        public InventorySystem.InventorySlot slot { get; private set; }
        public int slotIndex;
        public bool PreservesManualLayout => preserveManualLayout;

        private InventorySystem inventory;
        private EquipmentController equipmentController;
        private InventoryItemActions itemActions;
        private GameObject dragGhost;
        private Image selectionGlowImage;
        private Color defaultBackgroundColor = SurvivalPioneerUiPalette.SlotBackground;
        private bool wasDragged;
        private bool isSelected;
        private bool suppressAmountOutline;

        private void Awake()
        {
            if (GetComponent<CanvasGroup>() == null)
                gameObject.AddComponent<CanvasGroup>();

            if (iconImage == null)
                iconImage = transform.Find("Icon")?.GetComponent<Image>();

            if (amountText == null)
                amountText = transform.Find("Amount")?.GetComponent<TextMeshProUGUI>()
                    ?? transform.Find("Icon/Amount")?.GetComponent<TextMeshProUGUI>();

            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

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

            theme.EnsureSelectionGlow(transform, ref selectionGlowImage);
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
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            ApplySelectionColor();
        }

        private void ApplySelectionColor()
        {
            if (backgroundImage != null)
                backgroundImage.color = defaultBackgroundColor;

            if (selectionGlowImage != null)
                selectionGlowImage.enabled = isSelected;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            wasDragged = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (slot == null || slot.IsEmpty || inventory == null) return;

            if (wasDragged)
            {
                wasDragged = false;
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
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
            if (slot == null || slot.IsEmpty || slot.item == null)
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

            InventorySlotUI target = FindSlotUnderPointer(eventData);
            if (target == null || target == source)
                return;

            if (!source.inventory.CanAcceptItemAt(target.slotIndex, source.slot.item))
                return;

            source.inventory.MoveOrMergeSlots(source.slotIndex, target.slotIndex);
        }

        private static InventorySlotUI FindSlotUnderPointer(PointerEventData eventData)
        {
            if (EventSystem.current == null)
                return null;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
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
