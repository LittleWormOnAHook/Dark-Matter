using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Project.Core;
using Project.Data;
using Project.Inventory;
using System.Collections.Generic;
using Project.Player;
using Project.Interaction;

namespace Project.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject inventoryPanel;

        [Header("Containers")]
        public Transform mainInventoryParent;
        public Transform hotbarParent;

        [Header("Prefabs")]
        public GameObject slotPrefab;

        [Header("Layout")]
        [Tooltip("When enabled, hotbar size/position from the scene is kept instead of being rebuilt at runtime.")]
        [SerializeField] private bool preserveHotbarLayout;

        [Tooltip("Optional data-driven layout applied before default code positioning.")]
        [SerializeField] private UiLayoutProfile layoutProfile;

        [SerializeField] private bool applyLayoutProfile = true;

        [Tooltip("When a profile applies, skip default anchor/offset layout on the main inventory grid.")]
        [SerializeField] private bool skipDefaultLayoutWhenProfileApplied = true;

        [Tooltip("When enabled, GridLayoutGroup cell size, spacing, and padding are not rebuilt at runtime.")]
        [SerializeField] private bool preserveMainGridLayout;

        public bool IsInventoryEmbedded => inventoryPanelEmbedded;

        private InventorySystem inventorySystem;
        private EquipmentController equipmentController;
        private InventoryItemActions itemActions;
        private List<InventorySlotUI> allSlots = new List<InventorySlotUI>();

        private Transform inventoryPanelOriginalParent;
        private bool inventoryPanelEmbedded;

        private Transform hotbarOriginalParent;
        private int hotbarOriginalSiblingIndex;
        private Transform statsPanelOriginalParent;
        private int statsPanelOriginalSiblingIndex;
        private bool hudSlotsRaised;

        private static float HotbarSlotSize => HudLayoutMetrics.InventorySlotSize(64f);
        private const float HotbarSlotSpacing = 6f * HudLayoutMetrics.HudScale;
        private const float HotbarHorizontalPadding = 14f * HudLayoutMetrics.HudScale;
        private const float HotbarExtraHeight = 18f * HudLayoutMetrics.HudScale;
        private const int MainInventoryColumns = 8;
        private const float MainInventoryInset = 12f;
        private const float MainInventorySpacing = 8f;

        private void Awake()
        {
            if (UiPreviewContext.IsActive)
                return;

            inventorySystem = FindAnyObjectByType<InventorySystem>();
            equipmentController = inventorySystem != null
                ? inventorySystem.GetComponent<EquipmentController>()
                : FindAnyObjectByType<EquipmentController>();

            if (inventorySystem != null)
            {
                itemActions = inventorySystem.GetComponent<InventoryItemActions>();
                if (itemActions == null)
                    itemActions = inventorySystem.gameObject.AddComponent<InventoryItemActions>();
            }

            EnsureInventoryClosed();
            GameplayHudVisibility.RefreshGameplayHud();
            HideLegacyPanelTitleLabels();
        }

        private void Start()
        {
            if (UiPreviewContext.IsActive)
                return;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            ApplyShiftPanelVisuals();

            if (canvas != null)
            {
                UiSoundHelper.BindButtonsInHierarchy(canvas.transform);
                ItemHoverTooltip.EnsureExists(canvas.transform);
                if (itemActions != null)
                    InventoryContextMenu.EnsureExists(canvas.transform, itemActions);
            }

            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged += RefreshUI;

            if (equipmentController != null)
            {
                equipmentController.OnSelectedHotbarChanged += HandleSelectedHotbarChanged;
                equipmentController.OnToolbarSelectionChanged += HandleToolbarSelectionChanged;
            }

            EnsureToolbar();
            CreateSlots();
            RefreshUI();
            RefreshMainInventoryLayout();
            EnsureInventoryClosed();
            GameplayHudVisibility.RefreshGameplayHud();
            HideLegacyPanelTitleLabels();
        }

        private void ApplyShiftPanelVisuals()
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme == null)
                return;

            if (inventoryPanel != null)
            {
                Image panelImage = inventoryPanel.GetComponent<Image>();
                if (panelImage != null)
                    theme.ApplyPanelImage(panelImage, large: true);
            }

            if (hotbarParent != null)
            {
                Image hotbarImage = hotbarParent.GetComponent<Image>();
                if (hotbarImage != null)
                    theme.ApplyPanelImage(hotbarImage, large: false, alphaMultiplier: 0.92f);
            }
        }

        public void EmbedInventoryPanel(Transform container)
        {
            if (inventoryPanel == null || container == null)
                return;

            if (inventoryPanelEmbedded && inventoryPanel.transform.parent == container)
            {
                inventoryPanel.SetActive(true);
                RefreshMainInventoryLayout();
                return;
            }

            if (inventoryPanelEmbedded)
                RestoreInventoryPanel();

            inventoryPanelOriginalParent = inventoryPanel.transform.parent;
            inventoryPanel.transform.SetParent(container, false);
            StretchToParent(inventoryPanel.GetComponent<RectTransform>());

            if (inventoryPanel.GetComponent<GraphicRaycaster>() == null)
                inventoryPanel.AddComponent<GraphicRaycaster>();

            inventoryPanel.SetActive(true);
            inventoryPanelEmbedded = true;
            HideLegacyPanelTitleLabels();
            Canvas.ForceUpdateCanvases();
            RefreshMainInventoryLayout();
        }

        public void SetBottomHudVisible(bool visible)
        {
            if (!visible)
            {
                if (hotbarParent != null)
                    hotbarParent.gameObject.SetActive(false);

                FindAnyObjectByType<ToolBarUI>()?.SetGameplayVisible(false);
                RestoreHudSlotsFromFrontLayer();
                return;
            }

            EnsureToolbar();

            if (hotbarParent != null)
                hotbarParent.gameObject.SetActive(true);

            Canvas canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            Transform canvasRoot = canvas != null ? canvas.transform : null;
            if (canvasRoot == null)
                return;

            if (hotbarParent != null)
            {
                if (!hudSlotsRaised)
                {
                    hotbarOriginalParent = hotbarParent.parent;
                    hotbarOriginalSiblingIndex = hotbarParent.GetSiblingIndex();
                    hudSlotsRaised = true;
                }

                UiFrontLayer.ReparentToFront(hotbarParent, canvasRoot);
                if (inventorySystem != null)
                    LayoutHotbarContainer(inventorySystem.hotbarSize);
            }

            ToolBarUI toolbar = FindAnyObjectByType<ToolBarUI>();
            toolbar?.SetGameplayVisible(true);
            toolbar?.EnsureRaisedToFrontLayer(canvasRoot);
            toolbar?.RepositionRelativeToHotbar(hotbarParent);
        }

        private void HideLegacyPanelTitleLabels()
        {
            if (inventoryPanel == null)
                return;

            Transform[] children = inventoryPanel.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child.name != "TitleText")
                    continue;

                child.gameObject.SetActive(false);
            }
        }

        public void RestoreInventoryPanel()
        {
            RestoreHudSlotsFromFrontLayer();

            if (inventoryPanel == null)
                return;

            if (inventoryPanelEmbedded && inventoryPanelOriginalParent != null)
                UiEmbedRestore.TryRestoreParent(inventoryPanel.transform, inventoryPanelOriginalParent);

            inventoryPanelEmbedded = false;
            inventoryPanel.SetActive(false);
        }

        private void RestoreHudSlotsFromFrontLayer()
        {
            if (!hudSlotsRaised)
                return;

            if (hotbarParent != null && hotbarOriginalParent != null)
            {
                hotbarParent.SetParent(hotbarOriginalParent, true);
                hotbarParent.SetSiblingIndex(Mathf.Clamp(hotbarOriginalSiblingIndex, 0, hotbarOriginalParent.childCount - 1));
                if (inventorySystem != null)
                    LayoutHotbarContainer(inventorySystem.hotbarSize);
            }

            CondensedSurvivalStatsHud statsHud = FindAnyObjectByType<CondensedSurvivalStatsHud>();
            if (statsHud != null && statsPanelOriginalParent != null)
            {
                Transform statsTransform = statsHud.transform;
                statsTransform.SetParent(statsPanelOriginalParent, true);
                statsTransform.SetSiblingIndex(Mathf.Clamp(statsPanelOriginalSiblingIndex, 0, statsPanelOriginalParent.childCount - 1));
            }

            FindAnyObjectByType<ToolBarUI>()?.RestoreFromFrontLayer(hotbarParent);
            FindAnyObjectByType<CondensedSurvivalStatsHud>()?.RefreshLayout();
            hudSlotsRaised = false;
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public void EnsureInventoryClosed()
        {
            bool panelWasOpen = inventoryPanel != null && inventoryPanel.activeSelf;
            RestoreInventoryPanel();

            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null && (panelWasOpen || pc.IsInventoryOpen))
                pc.SetInventoryOpen(false);

            CameraController cam = FindAnyObjectByType<CameraController>();
            if (cam != null)
                cam.SetInventoryOpen(false);
        }

        public static void CloseAnyOpenInventory()
        {
            InventoryUI inventoryUi = FindAnyObjectByType<InventoryUI>();
            inventoryUi?.EnsureInventoryClosed();
        }

        private void OnDestroy()
        {
            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged -= RefreshUI;

            if (equipmentController != null)
            {
                equipmentController.OnSelectedHotbarChanged -= HandleSelectedHotbarChanged;
                equipmentController.OnToolbarSelectionChanged -= HandleToolbarSelectionChanged;
            }
        }

        private void EnsureToolbar()
        {
            ToolBarUI toolbar = GetComponent<ToolBarUI>();
            if (toolbar == null)
                toolbar = FindAnyObjectByType<ToolBarUI>();
            if (toolbar == null)
                return;

            Canvas canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            Transform canvasRoot = canvas != null ? canvas.transform : transform;
            toolbar.EnsureBuilt(canvasRoot, slotPrefab, hotbarParent);
        }

        private void CreateSlots()
        {
            allSlots.Clear();

            if (inventorySystem == null) return;

            if (mainInventoryParent != null && slotPrefab != null)
            {
                foreach (Transform child in mainInventoryParent) Destroy(child.gameObject);

                for (int i = 0; i < inventorySystem.inventorySize; i++)
                {
                    GameObject slotObj = Instantiate(slotPrefab, mainInventoryParent);
                    InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
                    if (slotUI != null)
                    {
                        slotUI.slotIndex = i;
                        slotUI.Initialize(inventorySystem);
                        slotUI.SetEquipmentController(equipmentController);
                        slotUI.SetItemActions(itemActions);
                        slotUI.ApplyHudSlotMetrics(HotbarSlotSize);
                        allSlots.Add(slotUI);
                    }
                }
            }

            if (hotbarParent != null && slotPrefab != null)
            {
                ClearHotbarChildren();

                int hotbarStart = inventorySystem.inventorySize;
                for (int i = 0; i < inventorySystem.hotbarSize; i++)
                    CreateHotbarSlot(hotbarStart + i, i);

                LayoutHotbarContainer(inventorySystem.hotbarSize);
            }
        }

        public void RefreshMainInventoryLayout()
        {
            if (mainInventoryParent is not RectTransform gridRect)
                return;

            bool profileApplied = TryApplyLayoutProfile();
            bool useSavedGrid = preserveMainGridLayout || ProfileHasSavedMainGridLayout();

            if (!profileApplied)
            {
                gridRect.anchorMin = Vector2.zero;
                gridRect.anchorMax = Vector2.one;
                gridRect.pivot = new Vector2(0.5f, 0.5f);
                gridRect.anchoredPosition = Vector2.zero;
                gridRect.offsetMin = new Vector2(MainInventoryInset, MainInventoryInset);
                gridRect.offsetMax = new Vector2(-MainInventoryInset, -MainInventoryInset);
            }

            if (useSavedGrid)
            {
                GridLayoutGroup savedGrid = mainInventoryParent.GetComponent<GridLayoutGroup>();
                if (savedGrid != null)
                {
                    Canvas.ForceUpdateCanvases();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                    ApplyMetricsToMainInventorySlots(savedGrid.cellSize.x);
                }

                return;
            }

            if (inventorySystem == null)
                return;

            GridLayoutGroup grid = mainInventoryParent.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = mainInventoryParent.gameObject.AddComponent<GridLayoutGroup>();

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = MainInventoryColumns;
            grid.spacing = new Vector2(MainInventorySpacing, MainInventorySpacing);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

            float width = gridRect.rect.width;
            float height = gridRect.rect.height;
            if (width <= 0f || height <= 0f)
                return;

            int rows = Mathf.Max(1, Mathf.CeilToInt(inventorySystem.inventorySize / (float)MainInventoryColumns));
            float cellWidth = (width - MainInventorySpacing * (MainInventoryColumns - 1)) / MainInventoryColumns;
            float cellHeight = (height - MainInventorySpacing * (rows - 1)) / rows;
            float cellSize = Mathf.Min(HotbarSlotSize, Mathf.Max(24f, Mathf.Min(cellWidth, cellHeight)));
            grid.cellSize = new Vector2(cellSize, cellSize);

            ApplyMetricsToMainInventorySlots(cellSize);
        }

        private bool ProfileHasSavedMainGridLayout()
        {
            if (!applyLayoutProfile)
                return false;

            UiLayoutProfile profile = layoutProfile;
            if (profile == null)
                profile = UiLayoutProfileResolver.Load(UiPanelIds.InventoryPanel);

            if (profile == null || profile.nodes == null)
                return false;

            if (mainInventoryParent != null && inventoryPanel != null)
            {
                string path = UiLayoutProfileApplier.GetRelativePath(inventoryPanel.transform, mainInventoryParent);
                UiLayoutNodeEntry node = profile.FindNode(path);
                if (node != null && node.hasGridLayout)
                    return true;
            }

            for (int i = 0; i < profile.nodes.Count; i++)
            {
                UiLayoutNodeEntry node = profile.nodes[i];
                if (node == null || !node.hasGridLayout)
                    continue;

                if (node.relativePath == "MainInventoryGrid"
                    || node.relativePath.EndsWith("/MainInventoryGrid", System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private bool TryApplyLayoutProfile()
        {
            if (!applyLayoutProfile)
                return false;

            UiLayoutProfile profile = layoutProfile;
            if (profile == null)
                profile = UiLayoutProfileResolver.Load(UiPanelIds.InventoryPanel);

            if (profile == null || profile.nodes == null || profile.nodes.Count == 0)
                return false;

            Transform root = inventoryPanel != null ? inventoryPanel.transform : transform;
            bool applied = UiLayoutProfileApplier.Apply(root, profile, panelEmbedded: inventoryPanelEmbedded);
            return applied && skipDefaultLayoutWhenProfileApplied;
        }

        private void ApplyMetricsToMainInventorySlots(float cellSize)
        {
            if (mainInventoryParent == null)
                return;

            for (int i = 0; i < mainInventoryParent.childCount; i++)
            {
                InventorySlotUI slotUi = mainInventoryParent.GetChild(i).GetComponent<InventorySlotUI>();
                slotUi?.ApplyHudSlotMetrics(cellSize);
            }
        }

        private void ClearHotbarChildren()
        {
            foreach (Transform child in hotbarParent)
                Destroy(child.gameObject);
        }

        private void CreateHotbarSlot(int absoluteIndex, int hotbarIndex)
        {
            GameObject slotObj = Instantiate(slotPrefab, hotbarParent);
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            if (slotRect != null)
                slotRect.sizeDelta = new Vector2(HotbarSlotSize, HotbarSlotSize);

            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI == null)
                return;

            slotUI.slotIndex = absoluteIndex;
            slotUI.Initialize(inventorySystem);
            slotUI.SetEquipmentController(equipmentController);
            slotUI.SetItemActions(itemActions);
            slotUI.ApplyHudSlotMetrics(HotbarSlotSize);
            slotUI.SetHudAmountPresentation(plainAmountText: true);
            allSlots.Add(slotUI);

            string keyLabel = GetHotbarKeyLabel(hotbarIndex);
            if (!string.IsNullOrEmpty(keyLabel))
                AddHotbarKeyLabel(slotObj.transform, keyLabel);
        }

        private void LayoutHotbarContainer(int hotbarSlotCount)
        {
            if (hotbarParent is not RectTransform hotbarRect || hotbarSlotCount <= 0)
                return;

            float preservedHeight = HotbarSlotSize + HotbarExtraHeight;
            float preservedY = HudLayoutMetrics.BottomHudInset;
            float width = HotbarHorizontalPadding * 2f
                + hotbarSlotCount * HotbarSlotSize
                + Mathf.Max(0, hotbarSlotCount - 1) * HotbarSlotSpacing;

            ConfigureHotbarLayoutGroup(hotbarParent.GetComponent<HorizontalLayoutGroup>());
            ApplyHotbarSize(hotbarRect, width, preservedHeight);
            ResizeHotbarSlotChildren(hotbarRect);

            if (preserveHotbarLayout)
            {
                FindAnyObjectByType<ToolBarUI>()?.AlignCenteredWithHotbar(hotbarRect, preservedY);
                FindAnyObjectByType<CondensedSurvivalStatsHud>()?.RefreshLayout();
                return;
            }

            FindAnyObjectByType<ToolBarUI>()?.AlignCenteredWithHotbar(hotbarRect, preservedY);
            FindAnyObjectByType<CondensedSurvivalStatsHud>()?.RefreshLayout();
        }

        private void ConfigureHotbarLayoutGroup(HorizontalLayoutGroup layout)
        {
            if (layout == null)
                return;

            int padH = HudLayoutMetrics.ScaledInt(8f);
            int padV = HudLayoutMetrics.ScaledInt(6f);
            layout.spacing = HotbarSlotSpacing;
            layout.padding = new RectOffset(padH, padH, padV, padV);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
        }

        private void ResizeHotbarSlotChildren(RectTransform hotbarRect)
        {
            if (hotbarRect == null)
                return;

            for (int i = 0; i < hotbarRect.childCount; i++)
            {
                Transform child = hotbarRect.GetChild(i);
                if (child is RectTransform slotRect)
                    slotRect.sizeDelta = new Vector2(HotbarSlotSize, HotbarSlotSize);

                child.GetComponent<InventorySlotUI>()?.ApplyHudSlotMetrics(HotbarSlotSize);
            }
        }

        private static void ApplyHotbarSize(RectTransform hotbarRect, float width, float height)
        {
            hotbarRect.sizeDelta = new Vector2(width, height);
        }

        private static string GetHotbarKeyLabel(int hotbarIndex)
        {
            return hotbarIndex switch
            {
                0 => "1",
                1 => "2",
                2 => "3",
                3 => "4",
                4 => "5",
                5 => "6",
                6 => "7",
                7 => "8",
                8 => "9",
                9 => "0",
                _ => null
            };
        }

        private static void AddHotbarKeyLabel(Transform slotTransform, string keyText)
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
            {
                theme.CreateKeyLabel(
                    slotTransform,
                    keyText,
                    new Vector2(HudLayoutMetrics.Scaled(2f), HudLayoutMetrics.Scaled(-2f)),
                    new Vector2(HudLayoutMetrics.Scaled(22f), HudLayoutMetrics.Scaled(18f)),
                    showFrame: false);
                return;
            }

            GameObject keyObject = new GameObject("KeyLabel", typeof(RectTransform));
            keyObject.transform.SetParent(slotTransform, false);

            RectTransform keyRect = keyObject.GetComponent<RectTransform>();
            keyRect.anchorMin = new Vector2(0f, 1f);
            keyRect.anchorMax = new Vector2(0f, 1f);
            keyRect.pivot = new Vector2(0f, 1f);
            keyRect.anchoredPosition = new Vector2(HudLayoutMetrics.Scaled(4f), HudLayoutMetrics.Scaled(-4f));
            keyRect.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(24f), HudLayoutMetrics.Scaled(18f));

            TextMeshProUGUI keyLabel = keyObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(keyLabel);
            keyLabel.text = keyText;
            keyLabel.fontSize = HudLayoutMetrics.ScaledInt(14f);
            keyLabel.fontStyle = FontStyles.Bold;
            keyLabel.alignment = TextAlignmentOptions.TopLeft;
            keyLabel.color = ShiftUiTheme.PrimaryColor;
        }

        public void RefreshUI()
        {
            if (inventorySystem == null) return;

            RebuildSlotsIfNeeded();

            for (int i = 0; i < allSlots.Count; i++)
            {
                int index = allSlots[i].slotIndex;
                if (index >= 0 && index < inventorySystem.slots.Count)
                {
                    allSlots[i].UpdateSlot(inventorySystem.slots[index]);
                    bool selected = false;
                    if (equipmentController != null)
                    {
                        if (inventorySystem.IsToolbarIndex(index))
                            selected = equipmentController.IsSelectedToolbarAbsoluteIndex(index);
                        else if (equipmentController.IsWeaponHotbarSlot(index - inventorySystem.inventorySize))
                            selected = equipmentController.IsActiveWeaponHotbarIndex(index);
                        else
                            selected = index == equipmentController.SelectedSlotIndex;
                    }

                    allSlots[i].SetSelected(selected);
                }
            }

            FindAnyObjectByType<ToolBarUI>()?.RefreshUI();
        }

        public void RebuildSlotsIfNeeded()
        {
            if (inventorySystem == null)
                return;

            int expectedCount = inventorySystem.inventorySize + inventorySystem.hotbarSize;
            if (allSlots.Count == expectedCount)
                return;

            CreateSlots();
            RefreshMainInventoryLayout();
            if (hotbarParent != null)
                LayoutHotbarContainer(inventorySystem.hotbarSize);
        }

        private void HandleSelectedHotbarChanged(int selectedSlotIndex)
        {
            RefreshUI();
        }

        private void HandleToolbarSelectionChanged()
        {
            RefreshUI();
        }

        private void Update()
        {
            if (!GameSession.HasStarted)
                return;

            if (inventorySystem != null)
                HandleHotbarHotkeys();
        }

        private void HandleHotbarHotkeys()
        {
            if (equipmentController == null || Keyboard.current == null)
                return;

            int hotbarStartSlot = inventorySystem.inventorySize;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + equipmentController.PrimaryWeaponHotbarSlot);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + equipmentController.SecondaryWeaponHotbarSlot);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + equipmentController.TertiaryWeaponHotbarSlot);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + equipmentController.QuaternaryWeaponHotbarSlot);
            else if (Keyboard.current.digit5Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + 4);
            else if (Keyboard.current.digit6Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + 5);
            else if (Keyboard.current.digit7Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + 6);
            else if (Keyboard.current.digit8Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + 7);
            else if (Keyboard.current.digit9Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + 8);
            else if (Keyboard.current.digit0Key.wasPressedThisFrame)
                SelectOrUseHotbarSlot(hotbarStartSlot + 9);
        }

        private void SelectOrUseHotbarSlot(int slotIndex)
        {
            if (equipmentController == null || inventorySystem == null)
                return;

            ItemData item = inventorySystem.GetItemAt(slotIndex);
            if (item == null)
                return;

            if (inventorySystem.IsToolbarIndex(slotIndex))
            {
                equipmentController.SelectToolbarSlot(inventorySystem.ToToolbarSlotIndex(slotIndex));
                return;
            }

            int hotbarIndex = slotIndex - inventorySystem.inventorySize;

            if (item.IsConsumable)
            {
                if (itemActions != null)
                    itemActions.TryUse(slotIndex);
                else
                    inventorySystem.UseItemAt(slotIndex);
                return;
            }

            if (item.IsEquippable && equipmentController.IsWeaponHotbarSlot(hotbarIndex))
            {
                int weaponSlot = equipmentController.GetWeaponSlotIndexForHotbar(hotbarIndex);
                if (weaponSlot >= 0)
                    equipmentController.SelectWeaponSlot(weaponSlot);
                return;
            }

            equipmentController.SelectInventorySlot(slotIndex);
        }

        public void OnToggleInventory(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
            if (journal == null)
                return;

            journal.OpenToInventoryTab();
        }
    }
}