using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Always-visible 2-slot tool bar for scanner and binoculars.
    /// </summary>
    public class ToolBarUI : MonoBehaviour
    {
        public const float ClusterGap = 12f * HudLayoutMetrics.HudScale;

        private static float SlotSize => HudLayoutMetrics.InventorySlotSize(64f);
        private const float SlotSpacing = 10f * HudLayoutMetrics.HudScale;
        private const float ToolbarSidePadding = 24f * HudLayoutMetrics.HudScale;
        private const float ToolbarTitleHeight = 20f * HudLayoutMetrics.HudScale;
        private const float ToolbarExtraHeight = 36f * HudLayoutMetrics.HudScale;
        private const float HotbarGap = ClusterGap;

        [SerializeField] private GameObject slotPrefab;

        private RectTransform toolbarRoot;
        private RectTransform slotsRowRect;
        private readonly List<InventorySlotUI> toolbarSlots = new List<InventorySlotUI>(2);
        private readonly List<TextMeshProUGUI> keyLabels = new List<TextMeshProUGUI>(2);
        private InventorySystem inventorySystem;
        private EquipmentController equipmentController;
        private InventoryItemActions itemActions;
        private OpticsController opticsController;
        private bool uiBuilt;
        private Transform toolbarOriginalParent;
        private int toolbarOriginalSiblingIndex;
        private bool raisedToFrontLayer;

        private const float BinocularHoldSeconds = 0.28f;
        private float binocularKeyDownUnscaledTime = -1f;
        private bool binocularHoldTriggered;

        public void EnsureBuilt(Transform canvasRoot, GameObject slotPrefabOverride = null, Transform hotbarAnchor = null)
        {
            if (uiBuilt)
                return;

            if (slotPrefabOverride != null)
                slotPrefab = slotPrefabOverride;

            inventorySystem = FindAnyObjectByType<InventorySystem>();
            if (inventorySystem != null)
            {
                equipmentController = inventorySystem.GetComponent<EquipmentController>();
                opticsController = inventorySystem.GetComponent<OpticsController>();
                itemActions = inventorySystem.GetComponent<InventoryItemActions>();
                if (itemActions == null)
                    itemActions = inventorySystem.gameObject.AddComponent<InventoryItemActions>();
            }

            toolbarRoot = new GameObject("ToolBar", typeof(RectTransform)).GetComponent<RectTransform>();
            Transform layoutParent = hotbarAnchor != null ? hotbarAnchor.parent : canvasRoot;
            toolbarRoot.SetParent(layoutParent, false);
            PositionLeftOfHotbar(hotbarAnchor);
            EnsureToolbarBackdrop();

            GameObject labelObject = new GameObject("Title", typeof(RectTransform));
            labelObject.transform.SetParent(toolbarRoot, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, HudLayoutMetrics.Scaled(-2f));
            labelRect.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(180f), ToolbarTitleHeight);

            TextMeshProUGUI title = labelObject.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
            {
                theme.ApplyFont(title, semiBold: true);
                title.color = DarkMatterGenesisUiPalette.HotbarLabelText;
            }
            else
            {
                TmpUiHelper.ApplyDefaultFont(title);
                title.color = DarkMatterGenesisUiPalette.HotbarLabelText;
            }
            title.text = "TOOLS";
            title.fontSize = HudLayoutMetrics.ScaledInt(13f);
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;

            GameObject slotsRow = new GameObject("Slots", typeof(RectTransform));
            slotsRow.transform.SetParent(toolbarRoot, false);
            slotsRowRect = slotsRow.GetComponent<RectTransform>();
            slotsRowRect.anchorMin = new Vector2(0.5f, 0f);
            slotsRowRect.anchorMax = new Vector2(0.5f, 0f);
            slotsRowRect.pivot = new Vector2(0.5f, 0f);
            slotsRowRect.anchoredPosition = new Vector2(0f, HudLayoutMetrics.Scaled(4f));
            float rowWidth = SlotSize * 2f + SlotSpacing;
            slotsRowRect.sizeDelta = new Vector2(rowWidth, SlotSize);

            HorizontalLayoutGroup layout = slotsRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = SlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateToolbarSlots(slotsRowRect);

            PetToolbarUI petToolbar = GetComponent<PetToolbarUI>();
            if (petToolbar == null)
                petToolbar = gameObject.AddComponent<PetToolbarUI>();

            petToolbar.EnsureBuilt(canvasRoot, toolbarRoot, toolbarRoot.anchoredPosition.y);

            ExpeditionPioneerHudUI pioneerHud = GetComponent<ExpeditionPioneerHudUI>();
            if (pioneerHud == null)
                pioneerHud = gameObject.AddComponent<ExpeditionPioneerHudUI>();

            pioneerHud.EnsureBuilt(layoutParent, toolbarRoot.anchoredPosition.y);

            HotbarExposureGaugeCluster gaugeCluster = GetComponent<HotbarExposureGaugeCluster>();
            if (gaugeCluster == null)
                gaugeCluster = gameObject.AddComponent<HotbarExposureGaugeCluster>();

            gaugeCluster.EnsureBuilt(layoutParent, toolbarRoot.anchoredPosition.y);

            HovercraftStatusHudUI hovercraftHud = GetComponent<HovercraftStatusHudUI>();
            if (hovercraftHud == null)
                hovercraftHud = gameObject.AddComponent<HovercraftStatusHudUI>();

            hovercraftHud.EnsureBuilt(canvasRoot);

            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged += RefreshUI;

            if (equipmentController != null)
            {
                equipmentController.OnSelectedHotbarChanged += HandleSelectionChanged;
                equipmentController.OnToolbarSelectionChanged += HandleSelectionChanged;
            }

            uiBuilt = true;
            SetGameplayVisible(false);
            RefreshUI();
        }

        public void SetGameplayVisible(bool visible)
        {
            if (MainMenuController.BlocksGameplayHud)
                visible = false;

            if (toolbarRoot != null)
                toolbarRoot.gameObject.SetActive(visible);

            PetToolbarUI petToolbar = GetComponent<PetToolbarUI>();
            if (petToolbar == null)
                petToolbar = FindAnyObjectByType<PetToolbarUI>();
            petToolbar?.SetGameplayVisible(visible);

            HotbarExposureGaugeCluster gaugeCluster = GetComponent<HotbarExposureGaugeCluster>();
            if (gaugeCluster == null)
                gaugeCluster = FindAnyObjectByType<HotbarExposureGaugeCluster>();
            gaugeCluster?.SetGameplayVisible(visible);

            ExpeditionPioneerHudUI pioneerHud = GetComponent<ExpeditionPioneerHudUI>();
            if (pioneerHud == null)
                pioneerHud = FindAnyObjectByType<ExpeditionPioneerHudUI>();
            pioneerHud?.SetGameplayVisible(visible);

            HovercraftStatusHudUI hovercraftHud = GetComponent<HovercraftStatusHudUI>();
            if (hovercraftHud == null)
                hovercraftHud = FindAnyObjectByType<HovercraftStatusHudUI>();
            hovercraftHud?.SetGameplayVisible(visible);

            if (!visible)
                return;

            InventoryUI inventory = FindAnyObjectByType<InventoryUI>();
            if (inventory != null && inventory.hotbarParent is RectTransform hotbarRect)
            {
                Canvas canvas = inventory.GetComponent<Canvas>() ?? inventory.GetComponentInParent<Canvas>();
                Transform canvasRoot = canvas != null ? canvas.transform : null;
                if (canvasRoot != null)
                {
                    EnsureRaisedToFrontLayer(canvasRoot);
                    RepositionRelativeToHotbar(hotbarRect);
                }
            }
        }

        /// <summary>
        /// Hides the pet slot, quick-access tool bar, hotbar, and companion/pioneer bar while the
        /// player is driving a vehicle (called from HovercraftStatusHudUI on a mount/dismount edge,
        /// and re-applied by GameplayHudVisibility.SetGameplayHudVisible whenever some unrelated menu
        /// close tries to restore the normal HUD while still mounted). Deliberately leaves the
        /// Temperature/Hazards cluster and the hovercraft status HUD itself untouched — unlike
        /// <see cref="SetGameplayVisible"/>, which toggles that whole group together for menu/session
        /// state. On dismount this hands control back to <see cref="GameplayHudVisibility"/> so the
        /// normal HUD rules (journal open, building panel open, etc.) are re-evaluated correctly
        /// instead of blindly forcing everything back on.
        /// </summary>
        public void SetVehicleModeHudSuppressed(bool suppressed)
        {
            if (suppressed)
            {
                if (toolbarRoot != null)
                    toolbarRoot.gameObject.SetActive(false);

                PetToolbarUI petToolbar = GetComponent<PetToolbarUI>();
                if (petToolbar == null)
                    petToolbar = FindAnyObjectByType<PetToolbarUI>();
                petToolbar?.SetGameplayVisible(false);

                ExpeditionPioneerHudUI pioneerHud = GetComponent<ExpeditionPioneerHudUI>();
                if (pioneerHud == null)
                    pioneerHud = FindAnyObjectByType<ExpeditionPioneerHudUI>();
                pioneerHud?.SetGameplayVisible(false);

                InventoryUI inventory = FindAnyObjectByType<InventoryUI>();
                if (inventory != null && inventory.hotbarParent != null)
                    inventory.hotbarParent.gameObject.SetActive(false);
            }
            else
            {
                GameplayHudVisibility.RefreshGameplayHud();
            }
        }

        public static void ApplyGameplayVisibility()
        {
            ToolBarUI toolbar = FindAnyObjectByType<ToolBarUI>();
            if (toolbar == null)
                return;

            bool visible = GameSession.HasStarted && !MainMenuController.BlocksGameplayHud;
            toolbar.SetGameplayVisible(visible);
        }

        public void RepositionRelativeToHotbar(Transform hotbarAnchor)
        {
            if (toolbarRoot == null || hotbarAnchor is not RectTransform hotbarRect)
                return;

            AlignCenteredWithHotbar(hotbarRect, hotbarRect.anchoredPosition.y);
        }

        public float GetToolbarWidth() => SlotSize * 2f + SlotSpacing + ToolbarSidePadding;

        public float GetToolbarHeight() => SlotSize + ToolbarExtraHeight;

        public RectTransform ToolbarRoot => toolbarRoot;

        public void AlignCenteredWithHotbar(RectTransform hotbarRect, float anchoredY)
        {
            if (toolbarRoot == null || hotbarRect == null)
                return;

            PetToolbarUI petToolbar = GetComponent<PetToolbarUI>();
            ExpeditionPioneerHudUI pioneerHud = GetComponent<ExpeditionPioneerHudUI>();
            HotbarExposureGaugeCluster gaugeCluster = GetComponent<HotbarExposureGaugeCluster>();
            float pioneerWidth = pioneerHud != null && pioneerHud.IsBuilt ? pioneerHud.GetClusterWidth() : 0f;
            float pioneerGap = pioneerWidth > 0f ? ExpeditionPioneerHudUI.ClusterGap : 0f;
            float petWidth = petToolbar != null && petToolbar.IsBuilt ? petToolbar.GetPetClusterWidth() : 0f;
            float petGap = petWidth > 0f ? PetToolbarUI.PetClusterGap : 0f;
            float toolbarWidth = GetToolbarWidth();
            float toolbarHeight = GetToolbarHeight();
            float hotbarWidth = hotbarRect.sizeDelta.x;
            float totalWidth = petWidth + petGap + toolbarWidth + HotbarGap + hotbarWidth + pioneerGap + pioneerWidth;
            float clusterStartX = -totalWidth * 0.5f;
            float petRight = clusterStartX + petWidth;
            float toolbarRight = petRight + petGap + toolbarWidth;
            float hotbarCenterX = toolbarRight + HotbarGap + hotbarWidth * 0.5f;
            float pioneerLeft = toolbarRight + HotbarGap + hotbarWidth + pioneerGap;

            hotbarRect.anchorMin = new Vector2(0.5f, 0f);
            hotbarRect.anchorMax = new Vector2(0.5f, 0f);
            hotbarRect.pivot = new Vector2(0.5f, 0f);
            hotbarRect.anchoredPosition = new Vector2(hotbarCenterX, anchoredY);

            toolbarRoot.anchorMin = new Vector2(0.5f, 0f);
            toolbarRoot.anchorMax = new Vector2(0.5f, 0f);
            toolbarRoot.pivot = new Vector2(1f, hotbarRect.pivot.y);
            toolbarRoot.sizeDelta = new Vector2(toolbarWidth, toolbarHeight);
            toolbarRoot.anchoredPosition = new Vector2(toolbarRight, anchoredY);

            if (petToolbar != null && petToolbar.IsBuilt)
                petToolbar.AlignLeftOfToolbarCluster(petRight, anchoredY);

            if (gaugeCluster != null && gaugeCluster.IsBuilt)
                gaugeCluster.AlignToScreenBottomLeft();

            if (pioneerHud != null && pioneerHud.IsBuilt)
                pioneerHud.AlignRightOfHotbar(pioneerLeft, anchoredY);

            FindAnyObjectByType<InventoryUI>()?.EnsureSurvivalStatsHudVisible();
        }

        public void RaiseToFrontLayer(Transform canvasRoot)
        {
            if (raisedToFrontLayer || toolbarRoot == null || canvasRoot == null)
                return;

            toolbarOriginalParent = toolbarRoot.parent;
            toolbarOriginalSiblingIndex = toolbarRoot.GetSiblingIndex();
            UiFrontLayer.ReparentToFront(toolbarRoot, canvasRoot);
            raisedToFrontLayer = true;
            GetComponent<ExpeditionPioneerHudUI>()?.EnsureRaisedToFrontLayer(canvasRoot);
            GetComponent<HotbarExposureGaugeCluster>()?.EnsureRaisedToFrontLayer(canvasRoot);
        }

        public void EnsureRaisedToFrontLayer(Transform canvasRoot)
        {
            if (toolbarRoot == null || canvasRoot == null)
                return;

            if (!raisedToFrontLayer)
            {
                RaiseToFrontLayer(canvasRoot);
                return;
            }

            UiFrontLayer.ReparentToFront(toolbarRoot, canvasRoot);
            GetComponent<ExpeditionPioneerHudUI>()?.EnsureRaisedToFrontLayer(canvasRoot);
            GetComponent<HotbarExposureGaugeCluster>()?.EnsureRaisedToFrontLayer(canvasRoot);
        }

        public void RestoreFromFrontLayer(Transform hotbarAnchor)
        {
            if (!raisedToFrontLayer || toolbarRoot == null || toolbarOriginalParent == null)
                return;

            toolbarRoot.SetParent(toolbarOriginalParent, true);
            toolbarRoot.SetSiblingIndex(Mathf.Clamp(toolbarOriginalSiblingIndex, 0, toolbarOriginalParent.childCount - 1));
            raisedToFrontLayer = false;

            if (hotbarAnchor is RectTransform hotbarRect)
            {
                RepositionRelativeToHotbar(hotbarRect);
                float pioneerLeft = ComputePioneerLeftEdge(hotbarRect);
                float anchoredY = hotbarRect.anchoredPosition.y;
                GetComponent<HotbarExposureGaugeCluster>()?.RestoreFromFrontLayer();
                GetComponent<ExpeditionPioneerHudUI>()?.RestoreFromFrontLayer(pioneerLeft, anchoredY);
            }
        }

        private float ComputePioneerLeftEdge(RectTransform hotbarRect)
        {
            ExpeditionPioneerHudUI pioneerHud = GetComponent<ExpeditionPioneerHudUI>();
            float pioneerWidth = pioneerHud != null && pioneerHud.IsBuilt ? pioneerHud.GetClusterWidth() : 0f;
            float pioneerGap = pioneerWidth > 0f ? ExpeditionPioneerHudUI.ClusterGap : 0f;
            float petWidth = GetComponent<PetToolbarUI>() is { IsBuilt: true } petToolbar ? petToolbar.GetPetClusterWidth() : 0f;
            float petGap = petWidth > 0f ? PetToolbarUI.PetClusterGap : 0f;
            float toolbarWidth = GetToolbarWidth();
            float hotbarWidth = hotbarRect.sizeDelta.x;
            float totalWidth = petWidth + petGap + toolbarWidth + HotbarGap + hotbarWidth + pioneerGap + pioneerWidth;
            float clusterStartX = -totalWidth * 0.5f;
            float toolbarRight = clusterStartX + petWidth + petGap + toolbarWidth;
            return toolbarRight + HotbarGap + hotbarWidth + pioneerGap;
        }

        private void PositionLeftOfHotbar(Transform hotbarAnchor)
        {
            if (hotbarAnchor is RectTransform hotbarRect)
            {
                float y = hotbarRect.anchoredPosition.y;
                if (hotbarRect.sizeDelta.x <= 0f)
                    hotbarRect.sizeDelta = new Vector2(hotbarRect.rect.width, hotbarRect.sizeDelta.y);

                AlignCenteredWithHotbar(hotbarRect, y);
                return;
            }

            float toolbarWidth = GetToolbarWidth();
            float toolbarHeight = GetToolbarHeight();
            toolbarRoot.anchorMin = new Vector2(0.5f, 0f);
            toolbarRoot.anchorMax = new Vector2(0.5f, 0f);
            toolbarRoot.pivot = new Vector2(0.5f, 0f);
            toolbarRoot.anchoredPosition = new Vector2(-toolbarWidth * 0.5f, HudLayoutMetrics.BottomHudInset);
            toolbarRoot.sizeDelta = new Vector2(toolbarWidth, toolbarHeight);
        }

        private void OnDestroy()
        {
            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged -= RefreshUI;

            if (equipmentController != null)
            {
                equipmentController.OnSelectedHotbarChanged -= HandleSelectionChanged;
                equipmentController.OnToolbarSelectionChanged -= HandleSelectionChanged;
            }
        }

        private void Update()
        {
            if (!GameSession.HasStarted || inventorySystem == null || equipmentController == null)
                return;

            HandleToolbarHotkeys();
        }

        private void HandleToolbarHotkeys()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.nKey.wasPressedThisFrame)
                UseTool(ToolType.Scanner);

            HandleBinocularsVsBlueprintsKey();
        }

        /// <summary>
        /// Shared B key: hold → binoculars, tap (press+release before hold threshold) → Blueprints.
        /// </summary>
        private void HandleBinocularsVsBlueprintsKey()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            UnityEngine.InputSystem.Controls.KeyControl bKey = keyboard.bKey;

            if (bKey.wasPressedThisFrame)
            {
                binocularKeyDownUnscaledTime = Time.unscaledTime;
                binocularHoldTriggered = false;
            }

            if (bKey.isPressed &&
                binocularKeyDownUnscaledTime >= 0f &&
                !binocularHoldTriggered &&
                Time.unscaledTime - binocularKeyDownUnscaledTime >= BinocularHoldSeconds)
            {
                binocularHoldTriggered = true;
                if (CanUseToolbarHotkeys())
                    UseTool(ToolType.Binoculars);
            }

            if (!bKey.wasReleasedThisFrame)
                return;

            bool wasTap = !binocularHoldTriggered && binocularKeyDownUnscaledTime >= 0f;
            binocularKeyDownUnscaledTime = -1f;
            binocularHoldTriggered = false;

            if (!wasTap || !CanUseToolbarHotkeys())
                return;

            // Tap B → Blueprints journal tab (Input System keyboard B is suppressed in UIManager).
            FindAnyObjectByType<UIManager>()?.ToggleBlueprintsFromTap();
        }

        private static bool CanUseToolbarHotkeys()
        {
            if (!GameSession.HasStarted || MainMenuController.BlocksGameplayHud)
                return false;

            // Don't steal B while typing in an InputField / TMP field.
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
            {
                GameObject selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                if (selected.GetComponent<TMPro.TMP_InputField>() != null ||
                    selected.GetComponent<UnityEngine.UI.InputField>() != null)
                    return false;
            }

            return true;
        }

        private void UseTool(ToolType toolType)
        {
            if (opticsController == null && inventorySystem != null)
                opticsController = inventorySystem.GetComponent<OpticsController>();

            if (opticsController != null)
            {
                opticsController.HandleToolHotkey(toolType);
                return;
            }

            int slot = toolType == ToolType.Scanner
                ? equipmentController.ScannerToolbarSlot
                : equipmentController.BinocularsToolbarSlot;

            equipmentController.TryEnsureToolbarTool(toolType, out _);
            equipmentController.SelectToolbarSlot(slot, allowToggleOff: true);
        }

        private void HandleSelectionChanged(int _)
        {
            RefreshSelection();
        }

        private void HandleSelectionChanged()
        {
            RefreshSelection();
        }

        private void CreateToolbarSlots(Transform parent)
        {
            toolbarSlots.Clear();
            keyLabels.Clear();

            if (inventorySystem == null || slotPrefab == null || equipmentController == null)
                return;

            int[] slotOrder =
            {
                equipmentController.ScannerToolbarSlot,
                equipmentController.BinocularsToolbarSlot
            };

            // Key hints deferred to settings keyboard/mouse/controller scheme.
            string[] keyHints = { string.Empty, string.Empty };

            for (int i = 0; i < inventorySystem.toolbarSize && i < slotOrder.Length; i++)
            {
                GameObject slotObject = Instantiate(slotPrefab, parent);
                RectTransform slotRect = slotObject.GetComponent<RectTransform>();
                if (slotRect != null)
                    slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

                InventorySlotUI slotUi = slotObject.GetComponent<InventorySlotUI>();
                if (slotUi == null)
                    continue;

                slotUi.slotIndex = inventorySystem.ToolbarStartIndex + slotOrder[i];
                slotUi.Initialize(inventorySystem);
                slotUi.SetEquipmentController(equipmentController);
                slotUi.SetItemActions(itemActions);
                slotUi.ApplyHudSlotMetrics(SlotSize);
                slotUi.SetHudAmountPresentation(plainAmountText: true);
                toolbarSlots.Add(slotUi);

                TextMeshProUGUI keyLabel = CreateToolbarKeyLabel(slotObject.transform, keyHints[i]);
                keyLabels.Add(keyLabel);
            }
        }

        private static TextMeshProUGUI CreateToolbarKeyLabel(Transform slotTransform, string keyText)
        {
            ShiftUiTheme keyTheme = ShiftUiTheme.Current;
            TextMeshProUGUI keyLabel;
            if (keyTheme != null)
            {
                keyLabel = keyTheme.CreateKeyLabel(
                    slotTransform,
                    keyText,
                    new Vector2(HudLayoutMetrics.Scaled(2f), HudLayoutMetrics.Scaled(-2f)),
                    new Vector2(HudLayoutMetrics.Scaled(22f), HudLayoutMetrics.Scaled(18f)),
                    showFrame: false);
            }
            else
            {
                GameObject keyObject = new GameObject("KeyLabel", typeof(RectTransform));
                keyObject.transform.SetParent(slotTransform, false);
                RectTransform keyRect = keyObject.GetComponent<RectTransform>();
                keyRect.anchorMin = new Vector2(0f, 1f);
                keyRect.anchorMax = new Vector2(0f, 1f);
                keyRect.pivot = new Vector2(0f, 1f);
                keyRect.anchoredPosition = new Vector2(HudLayoutMetrics.Scaled(4f), HudLayoutMetrics.Scaled(-4f));
                keyRect.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(24f), HudLayoutMetrics.Scaled(18f));

                keyLabel = keyObject.AddComponent<TextMeshProUGUI>();
                TmpUiHelper.ApplyDefaultFont(keyLabel);
                keyLabel.text = keyText;
                keyLabel.fontSize = HudLayoutMetrics.ScaledInt(14f);
                keyLabel.fontStyle = FontStyles.Bold;
                keyLabel.alignment = TextAlignmentOptions.TopLeft;
            }

            keyLabel.color = DarkMatterGenesisUiPalette.HotbarLabelText;
            return keyLabel;
        }

        private void EnsureToolbarBackdrop()
        {
            if (toolbarRoot == null || toolbarRoot.GetComponent<Image>() != null)
                return;

            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme == null)
                return;

            GameObject backdrop = new GameObject("PanelBackdrop", typeof(RectTransform));
            backdrop.transform.SetParent(toolbarRoot, false);
            backdrop.transform.SetAsFirstSibling();

            RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(HudLayoutMetrics.Scaled(-8f), HudLayoutMetrics.Scaled(-4f));
            backdropRect.offsetMax = new Vector2(HudLayoutMetrics.Scaled(8f), HudLayoutMetrics.Scaled(4f));

            Image image = backdrop.AddComponent<Image>();
            theme.ApplyPanelImage(image, large: false, alphaMultiplier: 0.92f);
        }

        public void RefreshUI()
        {
            if (!uiBuilt || inventorySystem == null)
                return;

            for (int i = 0; i < toolbarSlots.Count; i++)
            {
                int index = toolbarSlots[i].slotIndex;
                if (index >= 0 && index < inventorySystem.slots.Count)
                    toolbarSlots[i].UpdateSlot(inventorySystem.slots[index]);
            }

            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (equipmentController == null)
                return;

            for (int i = 0; i < toolbarSlots.Count; i++)
            {
                bool selected = equipmentController.IsSelectedToolbarAbsoluteIndex(toolbarSlots[i].slotIndex);
                toolbarSlots[i].SetSelected(selected);
            }
        }
    }
}
