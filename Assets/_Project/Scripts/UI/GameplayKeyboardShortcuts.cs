using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Project.UI
{
    /// <summary>
    /// Shared keyboard shortcuts for hotbar, journal tabs, toolbar tools, and pause menu.
    /// When UITK is enabled, <see cref="DMUiToolkitInputHost"/> is the sole caller.
    /// stamp: hotcross-tab-arm-x-ammo 0920f
    /// </summary>
    public static class GameplayKeyboardShortcuts
    {
        private static int toolHotkeyHandledFrame = -1;
        private static ToolType toolHotkeyHandledType;
        private static InventorySystem cachedInventory;
        private static EquipmentController cachedEquipment;
        private static InventoryItemActions cachedItemActions;


        public static bool CanProcess()
        {
            if (!Application.isPlaying)
                return false;

            // Clear stuck pauseOverlayActive / ghost MainMenu.IsVisible before gating.
            // Block only on real pause/menu chrome (painted IsVisible / sub-panels / loading).
            GameplayInputRecovery.ClearGhostPauseOverlay();

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return false;

            if (DMUiToolkitMainMenu.IsVisible)
                return false;

            if (DMUiToolkitMenuPanels.IsAnySubPanelOpen)
                return false;

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            bool journalOpen = navigator != null && navigator.IsAnyOpen;

            if (!GameSession.HasStarted && !journalOpen)
                return false;

            PlayerController player = PlayerLocator.FindPlayerController();
            if (player != null && player.IsGameplayPaused && !journalOpen)
                return false;

            if (IsTypingInTextField())
                return false;

            return true;
        }

        public static void TryHandleJournalHotkeys()
        {
            if (!CanProcess())
                return;

            // KBM tab letters (match Journal.uxml). Gamepad uses Journal + D-Pad/stick only.
            // KeyCode poll is required while journal is open: DMUiModalInputGate disables the Player map.
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.jKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.J);
            if (keyboard.iKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.I);
            if (keyboard.mKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.M);
            if (keyboard.kKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.K);
            if (keyboard.pKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.P);
            if (keyboard.cKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.C);
            if (keyboard.uKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.U);
            if (keyboard.tKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.T);
            if (keyboard.lKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.L);
            if (keyboard.gKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.G);
        }

        public static void TryHandleHotbarHotkeys()
        {
            if (!CanProcess())
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (!TryResolveInventory(out InventorySystem inventory, out EquipmentController equipment, out InventoryItemActions itemActions))
                return;

            int hotbarStartSlot = inventory.inventorySize;

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyWeaponLocalIndex(equipment.PrimaryWeaponHotbarSlot);
                TrySelectHotbarSlot(hotbarStartSlot + equipment.PrimaryWeaponHotbarSlot, inventory, equipment, itemActions);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyWeaponLocalIndex(equipment.SecondaryWeaponHotbarSlot);
                TrySelectHotbarSlot(hotbarStartSlot + equipment.SecondaryWeaponHotbarSlot, inventory, equipment, itemActions);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyWeaponLocalIndex(equipment.TertiaryWeaponHotbarSlot);
                TrySelectHotbarSlot(hotbarStartSlot + equipment.TertiaryWeaponHotbarSlot, inventory, equipment, itemActions);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyWeaponLocalIndex(equipment.QuaternaryWeaponHotbarSlot);
                TrySelectHotbarSlot(hotbarStartSlot + equipment.QuaternaryWeaponHotbarSlot, inventory, equipment, itemActions);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyConsumableLocalIndex(4);
                TrySelectHotbarSlot(hotbarStartSlot + 4, inventory, equipment, itemActions);
            }
            else if (keyboard.digit6Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyConsumableLocalIndex(5);
                TrySelectHotbarSlot(hotbarStartSlot + 5, inventory, equipment, itemActions);
            }
            else if (keyboard.digit7Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyConsumableLocalIndex(6);
                TrySelectHotbarSlot(hotbarStartSlot + 6, inventory, equipment, itemActions);
            }
            else if (keyboard.digit8Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyConsumableLocalIndex(7);
                TrySelectHotbarSlot(hotbarStartSlot + 7, inventory, equipment, itemActions);
            }
            else if (keyboard.digit9Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyConsumableLocalIndex(8);
                TrySelectHotbarSlot(hotbarStartSlot + 8, inventory, equipment, itemActions);
            }
            else if (keyboard.digit0Key.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyConsumableLocalIndex(9);
                TrySelectHotbarSlot(hotbarStartSlot + 9, inventory, equipment, itemActions);
            }
        }

        public static void TryHandleToolbarHotkeys()
        {
            if (!CanProcess())
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.nKey.wasPressedThisFrame)
            {
                DMUiToolkitHotCross.NotifyToolFace(DMUiToolkitHotCross.ToolFace.Scanner);
                TryUseTool(ToolType.Scanner);
                return;
            }

            HandleBinocularsKey(keyboard);
        }

        private static void HandleBinocularsKey(Keyboard keyboard)
        {
            if (!keyboard.bKey.wasPressedThisFrame)
                return;

            DMUiToolkitHotCross.NotifyToolFace(DMUiToolkitHotCross.ToolFace.Binoculars);
            TryUseTool(ToolType.Binoculars);
        }

        private static int destPanelHandledFrame = -1;

        public static void TryHandleDevPanel()
        {
            if (!Application.isPlaying)
                return;

            if (Time.frameCount == destPanelHandledFrame)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            bool alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
            if (!alt || !keyboard.vKey.wasPressedThisFrame)
                return;

            destPanelHandledFrame = Time.frameCount;
            DMUiToolkitDevPanel.Toggle();
        }

        public static void TryHandleAll()
        {
            // Journal tab letters must work while the journal locks the Player map / gameplay input.
            if (IsGameplayInputLockedByUi())
            {
                TryHandleJournalHotkeys();
                return;
            }

            TryHandleCinematicHudToggle();
            TryHandleJournalHotkeys();
            TryHandleHotbarHotkeys();
            TryHandleHotCrossHotkeys();
            TryHandleToolbarHotkeys();
            TryHandleMinimapZoom();
        }

        public static void TryHandleMinimapZoom()
        {
            if (!CanProcess())
                return;

            bool zoomIn = false;
            bool zoomOut = false;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftBracketKey.wasPressedThisFrame)
                    zoomIn = true;
                if (keyboard.rightBracketKey.wasPressedThisFrame)
                    zoomOut = true;
            }

            if (Input.GetKeyDown(KeyCode.LeftBracket))
                zoomIn = true;
            if (Input.GetKeyDown(KeyCode.RightBracket))
                zoomOut = true;

            if (!zoomIn && !zoomOut)
                return;

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return;

            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal != null && journal.IsOpen)
                return;

            if (DMUiToolkitMenus.IsOpen)
                return;

            MapUI mapUi = Object.FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            if (mapUi == null)
                return;

            if (zoomIn)
                mapUi.UitkAdjustMinimapSpan(MapUI.MinimapZoomInMultiplier);
            if (zoomOut)
                mapUi.UitkAdjustMinimapSpan(MapUI.MinimapZoomOutMultiplier);
        }


        public static void TryMinimapZoom(bool zoomIn)
        {
            if (!CanProcess())
                return;
            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return;
            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal != null && journal.IsOpen)
                return;
            if (DMUiToolkitMenus.IsOpen)
                return;
            MapUI mapUi = Object.FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            if (mapUi == null)
                return;
            if (zoomIn)
                mapUi.UitkAdjustMinimapSpan(MapUI.MinimapZoomInMultiplier);
            else
                mapUi.UitkAdjustMinimapSpan(MapUI.MinimapZoomOutMultiplier);
        }

        public static void TryToggleCinematicHudFromAction()
        {
            TryHandleCinematicHudToggle(forceFromAction: true);
        }

        private static int cinematicHandledFrame = -1;

        /// <summary>Backquote / tilde hides gameplay chrome only. Always available in-session.</summary>
        public static void TryHandleCinematicHudToggle(bool forceFromAction = false)
        {
            if (!Application.isPlaying)
                return;

            if (Time.frameCount == cinematicHandledFrame)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing || DMUiToolkitMainMenu.IsVisible)
                return;

            if (!GameSession.HasStarted)
                return;

            if (IsTypingInTextField())
                return;

            if (!forceFromAction)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null || !keyboard.backquoteKey.wasPressedThisFrame)
                    return;
            }

            cinematicHandledFrame = Time.frameCount;
            GameplayHudVisibility.ToggleCinematicChrome();
        }

        private static int escapeHandledFrame = -1;
        private static int uiCancelHandledFrame = -1;

        /// <summary>
        /// Gamepad B / UI Cancel â€” back one layer only; never opens pause (Start / Esc own pause).
        /// stamp: controller-b-dodge-dash 0920b
        /// </summary>
        public static void HandleUiCancelBack()
        {
            if (!Application.isPlaying)
                return;

            if (Time.frameCount == uiCancelHandledFrame)
                return;

            uiCancelHandledFrame = Time.frameCount;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            if (DMUiToolkitDevPanel.HandleBack())
                return;

            if (DMUiToolkitMenus.TryHideInventoryContextMenu())
                return;

            if (DMUiToolkitContext.IsOpen)
            {
                DMUiToolkitContext.Hide();
                return;
            }

            if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
            {
                DMUiToolkitHotCross.HideAmmoLoadPopup();
                ResetHotCrossConsumableHold();
                return;
            }

            if (DMUiToolkitConfig.IsEnabled && DMUiToolkitMenuPanels.TryHandleEscapeBack())
                return;

            SettingsPanelController settings = Object.FindAnyObjectByType<SettingsPanelController>();
            if (settings != null && settings.IsOpen)
            {
                settings.Close();
                return;
            }

            ControlsPanelController controls = Object.FindAnyObjectByType<ControlsPanelController>();
            if (controls != null && controls.IsOpen)
            {
                controls.HandleBack();
                return;
            }

            SaveSlotsPanelController saves = Object.FindAnyObjectByType<SaveSlotsPanelController>();
            if (saves != null && saves.IsOpen)
            {
                saves.Close();
                return;
            }

            // Journal / fullscreen windows: B pops one layer (same as Esc back), no keyboard Escape required.
            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
            {
                navigator.HandleEscape();
                return;
            }

            if (DMUiToolkitMenus.IsOpen)
            {
                navigator?.CloseAll();
                DMUiToolkitMenus.ForceHideIfNavigatorClosed();
                return;
            }

            if (DMUiToolkitMainMenu.IsVisible)
            {
                MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
                if (menu != null && MainMenuController.BlocksGameplayHud)
                    menu.InvokeResumeFromPause();
            }
        }

        /// <summary>True when Cancel/B should act as Back (journal, pause overlay, popups, settings).</summary>
        public static bool IsModalUiOpenForCancel()
        {
            if (DMUiToolkitLoadingOverlay.IsShowing || DMUiToolkitMainMenu.IsVisible)
                return true;
            if (DMUiToolkitMenuPanels.IsAnySubPanelOpen)
                return true;
            if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                return true;
            if (DMUiToolkitMenus.IsInventoryContextOpen || DMUiToolkitContext.IsOpen)
                return true;
            if (DMUiToolkitMenus.IsOpen)
                return true;
            if (MainMenuController.BlocksGameplayHud || MainMenuController.IsPauseOverlayActive)
                return true;
            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return true;
            if (GameplayMenuTime.IsInventoryPaused)
                return true;
            SettingsPanelController settings = Object.FindAnyObjectByType<SettingsPanelController>();
            if (settings != null && settings.IsOpen)
                return true;
            if (DMUiToolkitControls.IsOpen)
                return true;
            return false;
        }

        /// <summary>Menus own input: only UI Navigate / Submit / Cancel should fire. Modal UI locks gameplay.</summary>
        public static bool IsGameplayInputLockedByUi()
        {
            return IsModalUiOpenForCancel();
        }

        /// <summary>Escape: close sub-panels, journal layers, then pause menu toggle.</summary>
        public static void TryHandleEscapeAndPause()
        {
            if (!Application.isPlaying)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            HandleEscapePressed();
        }

        /// <summary>Shared escape handler for keyboard polling and Input System Cancel action.</summary>
        public static void HandleEscapePressed()
        {
            if (!Application.isPlaying)
                return;

            if (Time.frameCount == escapeHandledFrame)
                return;

            escapeHandledFrame = Time.frameCount;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            if (DMUiToolkitDevPanel.HandleBack())
                return;

            if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
            {
                DMUiToolkitHotCross.HideAmmoLoadPopup();
                ResetHotCrossConsumableHold();
                return;
            }

            if (DMUiToolkitConfig.IsEnabled && DMUiToolkitMenuPanels.TryHandleEscapeBack())
                return;

            SettingsPanelController settings = Object.FindAnyObjectByType<SettingsPanelController>();
            if (settings != null && settings.IsOpen)
            {
                settings.Close();
                return;
            }

            ControlsPanelController controls = Object.FindAnyObjectByType<ControlsPanelController>();
            if (controls != null && controls.IsOpen)
            {
                controls.HandleBack();
                return;
            }

            SaveSlotsPanelController saves = Object.FindAnyObjectByType<SaveSlotsPanelController>();
            if (saves != null && saves.IsOpen)
            {
                saves.Close();
                return;
            }

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
            {
                if (!UiEscapeGate.TryConsumeEscape())
                    return;
                navigator.HandleEscape();
                return;
            }

            if (!GameSession.HasStarted)
                return;

            // OpticsController may already have consumed Esc this frame (closes binos/scanner).
            // Do not also open the pause menu on the same press.
            if (UiEscapeGate.WasConsumedThisFrame)
                return;

            OpticsController openOptics = Object.FindAnyObjectByType<OpticsController>();
            if (openOptics != null && openOptics.IsActive)
            {
                openOptics.CloseOpticsIfActive();
                return;
            }

            PlayerController opticsPlayer = PlayerLocator.FindPlayerController();
            if (opticsPlayer != null && (opticsPlayer.IsOpticsOpen || opticsPlayer.IsBinocularCameraFrozen))
            {
                openOptics?.CloseOpticsIfActive();
                if (openOptics == null)
                    Object.FindAnyObjectByType<OpticsController>()?.CloseOpticsIfActive();
                return;
            }

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null)
                return;

            if (MainMenuController.BlocksGameplayHud)
                menu.InvokeResumeFromPause();
            else
                menu.ShowPauseMenu();
        }

        public static bool TryHandleHotbarKeyCode(KeyCode keyCode)
        {
            if (!CanProcess())
                return false;

            if (!TryResolveInventory(out InventorySystem inventory, out EquipmentController equipment, out InventoryItemActions itemActions))
                return false;

            int hotbarStartSlot = inventory.inventorySize;
            int slotOffset = keyCode switch
            {
                KeyCode.Alpha1 => equipment.PrimaryWeaponHotbarSlot,
                KeyCode.Alpha2 => equipment.SecondaryWeaponHotbarSlot,
                KeyCode.Alpha3 => equipment.TertiaryWeaponHotbarSlot,
                KeyCode.Alpha4 => equipment.QuaternaryWeaponHotbarSlot,
                KeyCode.Alpha5 => 4,
                KeyCode.Alpha6 => 5,
                KeyCode.Alpha7 => 6,
                KeyCode.Alpha8 => 7,
                KeyCode.Alpha9 => 8,
                KeyCode.Alpha0 => 9,
                _ => -1
            };

            if (slotOffset < 0)
                return false;

            if (slotOffset >= 4)
                DMUiToolkitHotCross.NotifyConsumableLocalIndex(slotOffset);
            else
                DMUiToolkitHotCross.NotifyWeaponLocalIndex(slotOffset);

            TrySelectHotbarSlot(hotbarStartSlot + slotOffset, inventory, equipment, itemActions);
            return true;
        }

        public static bool TryHandleToolbarKeyCode(KeyCode keyCode)
        {
            if (!CanProcess())
                return false;

            if (keyCode == KeyCode.B)
            {
                DMUiToolkitHotCross.NotifyToolFace(DMUiToolkitHotCross.ToolFace.Binoculars);
                TryUseTool(ToolType.Binoculars);
                return true;
            }

            if (keyCode == KeyCode.N)
            {
                DMUiToolkitHotCross.NotifyToolFace(DMUiToolkitHotCross.ToolFace.Scanner);
                TryUseTool(ToolType.Scanner);
                return true;
            }

            return false;
        }


        public static bool TryHandleJournalKeyCode(KeyCode keyCode)
        {
            if (!CanProcess())
                return false;

            switch (keyCode)
            {
                case KeyCode.J:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.JournalQuest, journalHotkey: true)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.JournalQuest) == true;
                case KeyCode.I:
                    if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Inventory))
                        return true;
                    EnsureJournalPanel()?.OpenToInventoryTab();
                    return true;
                case KeyCode.M:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Map)
                        || EnsureJournalPanel()?.TryToggleMapTab() == true;
                case KeyCode.K:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pet)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Pet) == true;
                case KeyCode.P:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pioneers)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Pioneers) == true;
                case KeyCode.C:
                    if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Character))
                        return true;
                    EnsureJournalPanel()?.OpenToCharacterTab();
                    return true;
                case KeyCode.U:
                    if (DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Recipes))
                        return true;
                    EnsureJournalPanel()?.OpenToBlueprintsTab();
                    return true;
                case KeyCode.T:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Skills)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Skills) == true;
                case KeyCode.L:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Echoes)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Echoes) == true;
                case KeyCode.G:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Achievements)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Achievements) == true;
                default:
                    return false;
            }
        }

        public static void TryHandleAllLegacy()
        {
            TryHandleAll();
        }

        public static void TrySelectHotbarSlot(
            int slotIndex,
            InventorySystem inventory,
            EquipmentController equipment,
            InventoryItemActions itemActions)
        {
            if (equipment == null || inventory == null)
                return;

            if (UiInputGuard.BlocksGameplayEquipmentInput)
                return;

            ItemData item = inventory.GetItemAt(slotIndex);
            if (item == null)
                return;

            if (inventory.IsToolbarIndex(slotIndex))
            {
                equipment.SelectToolbarSlot(inventory.ToToolbarSlotIndex(slotIndex));
                return;
            }

            int hotbarIndex = slotIndex - inventory.inventorySize;

            if (item.IsConsumable)
            {
                if (itemActions != null)
                    itemActions.TryUse(slotIndex);
                else
                    inventory.UseItemAt(slotIndex);
                return;
            }

            if (item.IsEquippable && equipment.IsWeaponHotbarSlot(hotbarIndex))
            {
                int weaponSlot = equipment.GetWeaponSlotIndexForHotbar(hotbarIndex);
                if (weaponSlot >= 0)
                    equipment.SelectWeaponSlot(weaponSlot);
                return;
            }

            equipment.SelectInventorySlot(slotIndex);
        }

        private static bool TryResolveInventory(
            out InventorySystem inventory,
            out EquipmentController equipment,
            out InventoryItemActions itemActions)
        {
            if (cachedInventory == null || cachedEquipment == null)
            {
                cachedInventory = Object.FindAnyObjectByType<InventorySystem>(FindObjectsInactive.Include);
                cachedEquipment = null;
                cachedItemActions = null;
                if (cachedInventory != null)
                {
                    cachedEquipment = cachedInventory.GetComponent<EquipmentController>()
                        ?? cachedInventory.GetComponentInChildren<EquipmentController>(true)
                        ?? cachedInventory.GetComponentInParent<EquipmentController>();
                    cachedItemActions = cachedInventory.GetComponent<InventoryItemActions>()
                        ?? cachedInventory.GetComponentInChildren<InventoryItemActions>(true);
                }

                if (cachedEquipment == null)
                    cachedEquipment = Object.FindAnyObjectByType<EquipmentController>(FindObjectsInactive.Include);
            }

            inventory = cachedInventory;
            equipment = cachedEquipment;
            itemActions = cachedItemActions;
            return inventory != null && equipment != null;
        }

        private static JournalPanelUI EnsureJournalPanel()
        {
            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal != null)
                return journal;

            UIManager ui = UIManager.EnsureExists();
            if (ui == null)
                return null;

            journal = ui.GetComponent<JournalPanelUI>();
            if (journal == null)
                journal = ui.gameObject.AddComponent<JournalPanelUI>();

            return journal;
        }

        
        private const float HotCrossConsumableHoldSeconds = 0.5f;
        private const int HotCrossLostKeyFrames = 3;

        private static int hotCrossTabHandledFrame = -1;
        private static int hotCrossArmHandledFrame = -1;
        private static bool hotCrossConsumableKeyHeld;
        private static float hotCrossConsumableKeyDownTime;
        private static bool hotCrossConsumableHoldUsed;
        private static int hotCrossLostKeyFrames;

        private static bool hotCrossGamepadAmmoHeld;
        private static float hotCrossGamepadAmmoDownTime;
        private static bool hotCrossGamepadAmmoHoldUsed;
        private static int hotCrossGamepadAmmoLostFrames;

        // Tab / Y (North): tap = cycle weapon focus; hold ~0.5s = arm focused weapon.
        private static bool hotCrossWeaponKeyHeld;
        private static float hotCrossWeaponKeyDownTime;
        private static bool hotCrossWeaponHoldUsed;
        private static int hotCrossWeaponLostFrames;

        /// <summary>
        /// Hot Cross face keys: Tab cycles TL weapon focus 1-4 (no equip), LMB arms focused TL,
        /// X / D-Pad Right tap cycles TR utility focus 5-10 (no use), hold 0.5s uses focused TR
        /// (food/meds consume; ammo loads into the drawn ranged weapon).
        /// B/N are handled via toolbar hotkeys (binoculars / scanner). J opens Journal.
        /// stamp: controller-ammo-autoload 0920
        /// </summary>
        public static void TryHandleHotCrossHotkeys()
        {
            if (!CanProcess())
            {
                if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                    DMUiToolkitHotCross.HideAmmoLoadPopup();
                ResetHotCrossConsumableHold();
                ResetHotCrossGamepadAmmoHold();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            bool wpnPressed = (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
                || (pad != null && pad.buttonNorth.wasPressedThisFrame);
            bool wpnReleased = (keyboard != null && keyboard.tabKey.wasReleasedThisFrame)
                || (pad != null && pad.buttonNorth.wasReleasedThisFrame);
            bool wpnDown = (keyboard != null && keyboard.tabKey.isPressed)
                || (pad != null && pad.buttonNorth.isPressed);
            UpdateHotCrossWeaponCycleArm(wpnPressed, wpnReleased, wpnDown);

            if (keyboard != null)
                UpdateHotCrossConsumableKey(keyboard);

            UpdateHotCrossGamepadAmmoSelect();
            // LMB still arms focused TL weapon (combat click when already drawn leaves fire alone).
            TryArmFocusedHotCrossWeaponFromMouse();
        }

        public static bool TryHandleHotCrossKeyCode(KeyCode keyCode)
        {
            if (!CanProcess())
                return false;

            if (keyCode == KeyCode.Tab)
            {
                // Update poll owns tap (cycle+arm) / hold (arm). Swallow so UITK does not steal Tab.
                return true;
            }

            if (keyCode == KeyCode.X)
            {
                // Hold/tap ownership lives in TryHandleHotCrossHotkeys (Update poll).
                // Swallow UITK key so focus navigation does not steal X.
                return true;
            }

            // B/N are handled by TryHandleToolbarHotkeys (Update poll) â€” do not also handle here.
            return false;
        }

        
        /// <summary>Tab / Y North: tap cycles to next weapon and arms/draws it; hold ~0.5s arms focused weapon without cycling.</summary>
        private static void UpdateHotCrossWeaponCycleArm(bool pressed, bool released, bool down)
        {
            const float holdSeconds = 0.5f;

            if (UiInputGuard.BlocksGameplayEquipmentInput)
            {
                hotCrossWeaponKeyHeld = false;
                hotCrossWeaponHoldUsed = false;
                hotCrossWeaponLostFrames = 0;
                return;
            }

            if (pressed)
            {
                hotCrossWeaponKeyHeld = true;
                hotCrossWeaponHoldUsed = false;
                hotCrossWeaponKeyDownTime = Time.unscaledTime;
                hotCrossWeaponLostFrames = 0;
            }

            if (hotCrossWeaponKeyHeld && down && !hotCrossWeaponHoldUsed
                && (Time.unscaledTime - hotCrossWeaponKeyDownTime) >= holdSeconds)
            {
                hotCrossWeaponHoldUsed = true;
                TryArmFocusedHotCrossWeaponNow();
            }

            if (released || (hotCrossWeaponKeyHeld && !down))
            {
                if (hotCrossWeaponKeyHeld && !hotCrossWeaponHoldUsed)
                    TryCycleHotCrossWeaponFocus();
                hotCrossWeaponKeyHeld = false;
                hotCrossWeaponHoldUsed = false;
                hotCrossWeaponLostFrames = 0;
            }
            else if (hotCrossWeaponKeyHeld && !down)
            {
                hotCrossWeaponLostFrames++;
                if (hotCrossWeaponLostFrames > 3)
                {
                    if (!hotCrossWeaponHoldUsed)
                        TryCycleHotCrossWeaponFocus();
                    hotCrossWeaponKeyHeld = false;
                    hotCrossWeaponHoldUsed = false;
                    hotCrossWeaponLostFrames = 0;
                }
            }
            else
                hotCrossWeaponLostFrames = 0;
        }

        private static void TryArmFocusedHotCrossWeaponNow()
        {
            if (Time.frameCount == hotCrossArmHandledFrame)
                return;
            if (!TryResolveInventory(out InventorySystem inventory, out EquipmentController equipment, out _))
                return;

            int local = DMUiToolkitHotCross.WeaponLocalIndex;
            int absolute = inventory.HotbarStartIndex + local;
            ItemData item = inventory.GetItemAt(absolute);
            if (item == null || !item.IsEquippable || !equipment.IsWeaponHotbarSlot(local))
                return;

            int weaponSlot = equipment.GetWeaponSlotIndexForHotbar(local);
            if (weaponSlot < 0)
                return;

            // Already drawn active â€” still force Invector shooter rebind (Tab switch can leave old gun bound).
            if (equipment.IsWeaponDrawn
                && equipment.ActiveWeaponSlot == weaponSlot
                && equipment.ActiveWeaponHotbarSlot == local)
            {
                hotCrossArmHandledFrame = Time.frameCount;
                EnsureDrawnShooterBound(equipment);
                return;
            }

            hotCrossArmHandledFrame = Time.frameCount;
            // SelectWeaponSlot toggles holster when re-selecting the same slot; only call it when switching
            // or when the slot is holstered (toggle then draws).
            bool sameSlot = equipment.ActiveWeaponSlot == weaponSlot
                && equipment.ActiveWeaponHotbarSlot == local;
            if (!sameSlot || !equipment.IsWeaponDrawn)
                equipment.SelectWeaponSlot(weaponSlot);
            if (!equipment.IsWeaponDrawn)
                equipment.DrawWeapon();
            EnsureDrawnShooterBound(equipment);
        }

        private static void EnsureDrawnShooterBound(EquipmentController equipment)
        {
            if (equipment == null)
                return;
            PioneerInvectorWeaponBridge bridge = equipment.GetComponent<PioneerInvectorWeaponBridge>();
            if (bridge == null)
                bridge = Object.FindAnyObjectByType<PioneerInvectorWeaponBridge>();
            bridge?.EnsureDrawnShooterBound();
        }

        private static void TryCycleHotCrossWeaponFocus()
        {
            if (UiInputGuard.BlocksGameplayEquipmentInput)
                return;
            if (Time.frameCount == hotCrossTabHandledFrame)
                return;
            if (!TryResolveInventory(out _, out EquipmentController equipment, out _))
                return;

            hotCrossTabHandledFrame = Time.frameCount;
            int current = DMUiToolkitHotCross.WeaponLocalIndex;
            if (!equipment.TryGetNextOccupiedWeaponHotbarLocal(current, out int next))
                return;

            DMUiToolkitHotCross.NotifyWeaponLocalIndex(next);
            // Tap Tab/Y fully switches: focus + arm/draw so fire is not required to finish the swap.
            TryArmFocusedHotCrossWeaponNow();
        }

        private static void UpdateHotCrossConsumableKey(Keyboard keyboard)
        {
            if (UiInputGuard.BlocksGameplayEquipmentInput)
            {
                ResetHotCrossConsumableHold();
                return;
            }

            bool pressed = keyboard.xKey.wasPressedThisFrame;
            bool released = keyboard.xKey.wasReleasedThisFrame;
            bool down = keyboard.xKey.isPressed;

            ProcessHotCrossConsumableChannel(
                pressed,
                released,
                down,
                ref hotCrossConsumableKeyHeld,
                ref hotCrossConsumableKeyDownTime,
                ref hotCrossConsumableHoldUsed,
                ref hotCrossLostKeyFrames);
        }

        private static void UpdateHotCrossGamepadAmmoSelect()
        {
            if (UiInputGuard.BlocksGameplayEquipmentInput)
            {
                ResetHotCrossGamepadAmmoHold();
                return;
            }

            Gamepad pad = Gamepad.current;
            if (pad == null)
            {
                ResetHotCrossGamepadAmmoHold();
                return;
            }

            bool pressed = pad.dpad.right.wasPressedThisFrame;
            bool released = pad.dpad.right.wasReleasedThisFrame;
            bool down = pad.dpad.right.isPressed;

            ProcessHotCrossConsumableChannel(
                pressed,
                released,
                down,
                ref hotCrossGamepadAmmoHeld,
                ref hotCrossGamepadAmmoDownTime,
                ref hotCrossGamepadAmmoHoldUsed,
                ref hotCrossGamepadAmmoLostFrames);
        }

        private static void ProcessHotCrossConsumableChannel(
            bool pressed,
            bool released,
            bool down,
            ref bool keyHeld,
            ref float keyDownTime,
            ref bool holdUsed,
            ref int lostKeyFrames)
        {
            if (pressed)
            {
                keyHeld = true;
                keyDownTime = Time.unscaledTime;
                holdUsed = false;
                lostKeyFrames = 0;
            }

            if (keyHeld && down)
            {
                lostKeyFrames = 0;
                if (!holdUsed && Time.unscaledTime - keyDownTime >= HotCrossConsumableHoldSeconds)
                {
                    holdUsed = true;
                    TryUseFocusedHotCrossConsumable();
                }
            }

            bool lostKey = false;
            if (keyHeld && !down && !pressed && !released)
            {
                lostKeyFrames++;
                lostKey = lostKeyFrames >= HotCrossLostKeyFrames;
            }

            if (keyHeld && (released || lostKey))
            {
                if (released && !holdUsed)
                    TryCycleHotCrossConsumableFocus();

                keyHeld = false;
                holdUsed = false;
                keyDownTime = 0f;
                lostKeyFrames = 0;
            }
        }

        private static void ResetHotCrossConsumableHold()
        {
            hotCrossConsumableKeyHeld = false;
            hotCrossConsumableHoldUsed = false;
            hotCrossConsumableKeyDownTime = 0f;
            hotCrossLostKeyFrames = 0;
        }

        private static void ResetHotCrossGamepadAmmoHold()
        {
            hotCrossGamepadAmmoHeld = false;
            hotCrossGamepadAmmoHoldUsed = false;
            hotCrossGamepadAmmoDownTime = 0f;
            hotCrossGamepadAmmoLostFrames = 0;
        }

        private static void TryCycleHotCrossConsumableFocus()
        {
            if (!TryResolveInventory(out InventorySystem inventory, out EquipmentController equipment, out InventoryItemActions itemActions))
                return;

            int current = DMUiToolkitHotCross.ConsumableLocalIndex;
            if (!equipment.TryGetNextOccupiedUtilityHotbarLocal(current, out int next))
                return;

            DMUiToolkitHotCross.NotifyConsumableLocalIndex(next);

            // KBM X / pad D-Pad Right tap: cycle Hot Cross ammo focus and auto-load into the armed ranged weapon.
            int absolute = inventory.HotbarStartIndex + next;
            ItemData item = inventory.GetItemAt(absolute);
            if (item != null && item.CountsAsAmmo && itemActions != null)
                itemActions.TryEquipAmmoToActiveRangedWeapon(absolute);
        }

        private static void TryUseFocusedHotCrossConsumable()
        {
            if (UiInputGuard.BlocksGameplayEquipmentInput)
                return;
            if (!TryResolveInventory(out InventorySystem inventory, out EquipmentController equipment, out InventoryItemActions itemActions))
                return;

            int local = DMUiToolkitHotCross.ConsumableLocalIndex;
            int absolute = inventory.HotbarStartIndex + local;
            ItemData item = inventory.GetItemAt(absolute);
            if (item == null)
                return;

            if (item.CountsAsAmmo)
            {
                if (itemActions == null || !itemActions.TryEquipAmmoToActiveRangedWeapon(absolute))
                {
                    if (itemActions != null && !itemActions.TryResolveActiveRangedWeaponHotbarSlot(out _))
                        return;
                    PickupToastUI.Show("Cannot load â€” magazine full or incompatible");
                }

                return;
            }

            // Food / meds / oxygen: consume+heal via InventoryItemActions (same as digit use path),
            // but do not fall through to SelectInventorySlot which never applies restores.
            if (item.IsConsumable)
            {
                bool used = itemActions != null
                    ? itemActions.TryUse(absolute)
                    : inventory.UseItemAt(absolute);
                if (!used)
                    PickupToastUI.Show($"Cannot use {item.itemName}");
                return;
            }

            // Storage modules etc. that expose CanUse.
            if (itemActions != null && itemActions.CanUse(absolute))
            {
                if (!itemActions.TryUse(absolute))
                    PickupToastUI.Show($"Cannot use {item.itemName}");
                return;
            }

            PickupToastUI.Show($"{item.itemName} cannot be used from Hot Cross");
        }

        private static void TryArmFocusedHotCrossWeaponFromMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;
            if (UiInputGuard.BlocksGameplayEquipmentInput)
                return;
            if (Time.frameCount == hotCrossArmHandledFrame)
                return;
            if (!TryResolveInventory(out InventorySystem inventory, out EquipmentController equipment, out _))
                return;

            int local = DMUiToolkitHotCross.WeaponLocalIndex;
            int absolute = inventory.HotbarStartIndex + local;
            ItemData item = inventory.GetItemAt(absolute);
            if (item == null || !item.IsEquippable || !equipment.IsWeaponHotbarSlot(local))
                return;

            int weaponSlot = equipment.GetWeaponSlotIndexForHotbar(local);
            if (weaponSlot < 0)
                return;

            // Already the drawn active weapon  -  leave LMB to combat fire; do not toggle holster.
            if (equipment.IsWeaponDrawn
                && equipment.ActiveWeaponSlot == weaponSlot
                && equipment.ActiveWeaponHotbarSlot == local)
                return;

            hotCrossArmHandledFrame = Time.frameCount;
            equipment.SelectWeaponSlot(weaponSlot);
            if (!equipment.IsWeaponDrawn)
                equipment.DrawWeapon();
            EnsureDrawnShooterBound(equipment);
        }



        /// <summary>Shared entry for toolbar tools from Input Actions or keyboard.</summary>
        public static void TryUseToolFromAction(ToolType toolType)
        {
            if (!CanProcess())
                return;
            if (toolType == ToolType.Scanner)
                DMUiToolkitHotCross.NotifyToolFace(DMUiToolkitHotCross.ToolFace.Scanner);
            else if (toolType == ToolType.Binoculars)
                DMUiToolkitHotCross.NotifyToolFace(DMUiToolkitHotCross.ToolFace.Binoculars);
            TryUseTool(toolType);
        }

        private static void TryUseTool(ToolType toolType)
        {
            // InputHost KeyDown(B) and Update poll can both fire in one frame after Hot Cross press-to-open.
            if (toolHotkeyHandledFrame == Time.frameCount && toolHotkeyHandledType == toolType)
                return;
            toolHotkeyHandledFrame = Time.frameCount;
            toolHotkeyHandledType = toolType;

            InventorySystem inventory = Object.FindAnyObjectByType<InventorySystem>();
            EquipmentController equipment = inventory != null
                ? inventory.GetComponent<EquipmentController>()
                : Object.FindAnyObjectByType<EquipmentController>();
            if (equipment == null)
                return;

            OpticsController optics = inventory != null
                ? inventory.GetComponent<OpticsController>()
                : Object.FindAnyObjectByType<OpticsController>();
            if (optics != null)
            {
                optics.HandleToolHotkey(toolType);
                return;
            }

            int slot = toolType == ToolType.Scanner
                ? equipment.ScannerToolbarSlot
                : equipment.BinocularsToolbarSlot;

            equipment.TryEnsureToolbarTool(toolType, out _);
            equipment.SelectToolbarSlot(slot);
        }

        private static bool IsTypingInTextField()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
                return false;

            GameObject selected = eventSystem.currentSelectedGameObject;
            return selected.GetComponent<TMP_InputField>() != null
                || selected.GetComponent<InputField>() != null;
        }
    }
}

