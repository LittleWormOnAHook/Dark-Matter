using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
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
    /// </summary>
    public static class GameplayKeyboardShortcuts
    {
        private static int toolHotkeyHandledFrame = -1;
        private static ToolType toolHotkeyHandledType;


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

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.jKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.J);
            else if (keyboard.iKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.I);
            else if (keyboard.mKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.M);
            else if (keyboard.kKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.K);
            else if (keyboard.pKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.P);
            else if (keyboard.uKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.U);
            else if (keyboard.tKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.T);
            else if (keyboard.lKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.L);
            else if (keyboard.gKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.G);
            else if (keyboard.cKey.wasPressedThisFrame)
                TryHandleJournalKeyCode(KeyCode.C);
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

            HandleBinocularsVsBlueprintsKey(keyboard);
        }

        public static void TryHandleDevPanel()
        {
            if (!Application.isPlaying)
                return;

            if (DMUiToolkitLoadingOverlay.IsShowing)
                return;

            if (IsTypingInTextField())
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (ctrl && keyboard.dKey.wasPressedThisFrame)
                DMUiToolkitDevPanel.Toggle();
        }

        public static void TryHandleAll()
        {
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

            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return;

            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal != null && journal.IsOpen)
                return;

            if (DMUiToolkitMenus.IsOpen)
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

            MapUI mapUi = Object.FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            if (mapUi == null)
                return;

            if (zoomIn)
                mapUi.UitkAdjustMinimapSpan(0.833f);
            if (zoomOut)
                mapUi.UitkAdjustMinimapSpan(1.2f);
        }

        private static int cinematicHandledFrame = -1;

        /// <summary>Backquote / tilde hides gameplay chrome only. Always available in-session.</summary>
        public static void TryHandleCinematicHudToggle()
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

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.backquoteKey.wasPressedThisFrame)
                return;

            cinematicHandledFrame = Time.frameCount;
            GameplayHudVisibility.ToggleCinematicChrome();
        }

        private static int escapeHandledFrame = -1;

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
                        || EnsureJournalPanel()?.TryToggleJournal() == true;
                case KeyCode.I:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Inventory)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Inventory) == true;
                case KeyCode.M:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Map)
                        || EnsureJournalPanel()?.TryToggleMapTab() == true;
                case KeyCode.K:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pet)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Pet) == true;
                case KeyCode.P:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Pioneers)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Pioneers) == true;
                case KeyCode.U:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Character)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Character) == true;
                case KeyCode.T:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Skills)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Skills) == true;
                case KeyCode.L:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Echoes)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Echoes) == true;
                case KeyCode.G:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Achievements)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Achievements) == true;
                case KeyCode.C:
                    return DMUiToolkitMenus.TryToggleJournalTab(JournalWindowId.Recipes)
                        || EnsureJournalPanel()?.TryToggleTab(JournalWindowId.Recipes) == true;
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
            inventory = Object.FindAnyObjectByType<InventorySystem>(FindObjectsInactive.Include);
            equipment = null;
            itemActions = null;
            if (inventory != null)
            {
                equipment = inventory.GetComponent<EquipmentController>()
                    ?? inventory.GetComponentInChildren<EquipmentController>(true)
                    ?? inventory.GetComponentInParent<EquipmentController>();
                itemActions = inventory.GetComponent<InventoryItemActions>()
                    ?? inventory.GetComponentInChildren<InventoryItemActions>(true);
            }
            if (equipment == null)
                equipment = Object.FindAnyObjectByType<EquipmentController>(FindObjectsInactive.Include);

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

        private static void HandleBinocularsVsBlueprintsKey(Keyboard keyboard)
        {
            // Hot Cross: B opens binoculars (and sets BL face). Recipes journal stays on C.
            if (!keyboard.bKey.wasPressedThisFrame)
                return;

            DMUiToolkitHotCross.NotifyToolFace(DMUiToolkitHotCross.ToolFace.Binoculars);
            TryUseTool(ToolType.Binoculars);
        }

        
        private const float HotCrossConsumableHoldSeconds = 0.5f;
        private const int HotCrossLostKeyFrames = 3;

        private static int hotCrossTabHandledFrame = -1;
        private static int hotCrossArmHandledFrame = -1;
        private static bool hotCrossConsumableKeyHeld;
        private static float hotCrossConsumableKeyDownTime;
        private static bool hotCrossConsumableHoldUsed;
        private static int hotCrossLostKeyFrames;

        /// <summary>
        /// Hot Cross face keys: Tab cycles TL weapon focus 1-4 (no equip), LMB arms focused TL,
        /// X tap cycles TR utility focus 5-10 (no use), X hold 0.5s uses focused TR
        /// (food/meds consume; ammo opens weapon-target popup).
        /// While ammo popup is open: X tap cycles weapons, X hold 0.5s confirms load, Esc cancels.
        /// B/N are handled via toolbar hotkeys (binoculars / scanner).
        /// </summary>
        public static void TryHandleHotCrossHotkeys()
        {
            if (!CanProcess())
            {
                if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                    DMUiToolkitHotCross.HideAmmoLoadPopup();
                ResetHotCrossConsumableHold();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                ResetHotCrossConsumableHold();
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
                TryCycleHotCrossWeaponFocus();

            UpdateHotCrossConsumableKey(keyboard);
            TryArmFocusedHotCrossWeapon();
        }

        public static bool TryHandleHotCrossKeyCode(KeyCode keyCode)
        {
            if (!CanProcess())
                return false;

            if (keyCode == KeyCode.Tab)
            {
                TryCycleHotCrossWeaponFocus();
                return true;
            }

            if (keyCode == KeyCode.X)
            {
                // Hold/tap ownership lives in TryHandleHotCrossHotkeys (Update poll).
                // Swallow UITK key so focus navigation does not steal X.
                return true;
            }

            // B is handled by HandleBinocularsVsBlueprintsKey (Update poll)  -  do not also handle here.
            return false;
        }

        private static void TryCycleHotCrossWeaponFocus()
        {
            if (UiInputGuard.BlocksGameplayEquipmentInput)
                return;
            if (Time.frameCount == hotCrossTabHandledFrame)
                return;

            hotCrossTabHandledFrame = Time.frameCount;
            int next = (DMUiToolkitHotCross.WeaponLocalIndex + 1) % 4;
            DMUiToolkitHotCross.NotifyWeaponLocalIndex(next);
        }

        private static void UpdateHotCrossConsumableKey(Keyboard keyboard)
        {
            if (UiInputGuard.BlocksGameplayEquipmentInput)
            {
                if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                    DMUiToolkitHotCross.HideAmmoLoadPopup();
                ResetHotCrossConsumableHold();
                return;
            }

            bool pressed = keyboard.xKey.wasPressedThisFrame;
            bool released = keyboard.xKey.wasReleasedThisFrame;
            bool down = keyboard.xKey.isPressed;

            if (pressed)
            {
                hotCrossConsumableKeyHeld = true;
                hotCrossConsumableKeyDownTime = Time.unscaledTime;
                hotCrossConsumableHoldUsed = false;
                hotCrossLostKeyFrames = 0;
            }

            if (hotCrossConsumableKeyHeld && down)
            {
                hotCrossLostKeyFrames = 0;
                if (!hotCrossConsumableHoldUsed
                    && Time.unscaledTime - hotCrossConsumableKeyDownTime >= HotCrossConsumableHoldSeconds)
                {
                    hotCrossConsumableHoldUsed = true;
                    if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                        ConfirmHotCrossAmmoLoad();
                    else
                        TryUseFocusedHotCrossConsumable();
                }
            }

            // Tap = release before hold threshold.
            // Lost-key: require several consecutive !isPressed frames (Input System can flicker one frame).
            bool lostKey = false;
            if (hotCrossConsumableKeyHeld && !down && !pressed && !released)
            {
                hotCrossLostKeyFrames++;
                lostKey = hotCrossLostKeyFrames >= HotCrossLostKeyFrames;
            }

            if (hotCrossConsumableKeyHeld && (released || lostKey))
            {
                if (released && !hotCrossConsumableHoldUsed)
                {
                    if (DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                        DMUiToolkitHotCross.CycleAmmoLoadHighlight();
                    else
                        TryCycleHotCrossConsumableFocus();
                }
                ResetHotCrossConsumableHold();
            }
        }

        private static void ResetHotCrossConsumableHold()
        {
            hotCrossConsumableKeyHeld = false;
            hotCrossConsumableHoldUsed = false;
            hotCrossConsumableKeyDownTime = 0f;
            hotCrossLostKeyFrames = 0;
        }

        private static void TryCycleHotCrossConsumableFocus()
        {
            const int first = 4;
            const int last = 9;
            int start = DMUiToolkitHotCross.ConsumableLocalIndex;
            int next = first + ((start - first + 1) % (last - first + 1));
            DMUiToolkitHotCross.NotifyConsumableLocalIndex(next);
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

            // Ammo: open weapon-target popup instead of silently selecting the inventory slot.
            if (item.CountsAsAmmo)
            {
                TryOpenHotCrossAmmoLoadPopup(absolute, inventory, equipment, itemActions);
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

        private static void TryOpenHotCrossAmmoLoadPopup(
            int ammoAbsoluteSlot,
            InventorySystem inventory,
            EquipmentController equipment,
            InventoryItemActions itemActions)
        {
            if (itemActions == null)
            {
                PickupToastUI.Show("Cannot load ammo");
                return;
            }

            List<InventoryItemActions.AmmoEquipOption> options = itemActions.GetAmmoEquipOptions(ammoAbsoluteSlot);
            if (options == null || options.Count == 0)
            {
                PickupToastUI.Show("No compatible weapon for this ammo");
                return;
            }

            int preferred = equipment != null ? equipment.ActiveWeaponHotbarSlot : -1;
            bool opened = DMUiToolkitHotCross.ShowAmmoLoadPopup(
                ammoAbsoluteSlot,
                options,
                preferred,
                weaponHotbar =>
                {
                    if (!TryResolveInventory(out _, out _, out InventoryItemActions actions) || actions == null)
                        return;
                    if (!actions.TryEquipAmmoToWeapon(ammoAbsoluteSlot, weaponHotbar))
                        PickupToastUI.Show("Failed to load ammo");
                });

            if (!opened)
                PickupToastUI.Show("Cannot open ammo load UI");
        }

        private static void ConfirmHotCrossAmmoLoad()
        {
            if (!DMUiToolkitHotCross.IsAmmoLoadPopupOpen)
                return;

            if (!DMUiToolkitHotCross.TryConfirmAmmoLoad())
                PickupToastUI.Show("Failed to load ammo");
        }

        private static void TryArmFocusedHotCrossWeapon()
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
