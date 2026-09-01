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
        private const float BinocularHoldSeconds = 0.28f;

        private static float binocularKeyDownUnscaledTime = -1f;
        private static bool binocularHoldTriggered;

        public static bool CanProcess()
        {
            if (!Application.isPlaying)
                return false;

            if (MainMenuController.BlocksGameplayHud)
                return false;

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
                TrySelectHotbarSlot(hotbarStartSlot + equipment.PrimaryWeaponHotbarSlot, inventory, equipment, itemActions);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + equipment.SecondaryWeaponHotbarSlot, inventory, equipment, itemActions);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + equipment.TertiaryWeaponHotbarSlot, inventory, equipment, itemActions);
            else if (keyboard.digit4Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + equipment.QuaternaryWeaponHotbarSlot, inventory, equipment, itemActions);
            else if (keyboard.digit5Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + 4, inventory, equipment, itemActions);
            else if (keyboard.digit6Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + 5, inventory, equipment, itemActions);
            else if (keyboard.digit7Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + 6, inventory, equipment, itemActions);
            else if (keyboard.digit8Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + 7, inventory, equipment, itemActions);
            else if (keyboard.digit9Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + 8, inventory, equipment, itemActions);
            else if (keyboard.digit0Key.wasPressedThisFrame)
                TrySelectHotbarSlot(hotbarStartSlot + 9, inventory, equipment, itemActions);
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
                TryUseTool(ToolType.Scanner);
                return;
            }

            HandleBinocularsVsBlueprintsKey(keyboard);
        }

        public static void TryHandleAll()
        {
            TryHandleJournalHotkeys();
            TryHandleHotbarHotkeys();
            TryHandleToolbarHotkeys();
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

            TrySelectHotbarSlot(hotbarStartSlot + slotOffset, inventory, equipment, itemActions);
            return true;
        }

        public static bool TryHandleToolbarKeyCode(KeyCode keyCode)
        {
            if (!CanProcess())
                return false;

            if (keyCode == KeyCode.N)
            {
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
            equipment = inventory != null
                ? inventory.GetComponent<EquipmentController>()
                : Object.FindAnyObjectByType<EquipmentController>(FindObjectsInactive.Include);
            itemActions = inventory != null
                ? inventory.GetComponent<InventoryItemActions>()
                : null;

            return inventory != null && equipment != null;
        }

        private static JournalPanelUI EnsureJournalPanel()
        {
            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal != null)
                return journal;

            UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
                return null;

            journal = ui.GetComponent<JournalPanelUI>();
            if (journal == null)
                journal = ui.gameObject.AddComponent<JournalPanelUI>();

            return journal;
        }

        private static void HandleBinocularsVsBlueprintsKey(Keyboard keyboard)
        {
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
                TryUseTool(ToolType.Binoculars);
            }

            if (!bKey.wasReleasedThisFrame)
                return;

            bool wasTap = !binocularHoldTriggered && binocularKeyDownUnscaledTime >= 0f;
            binocularKeyDownUnscaledTime = -1f;
            binocularHoldTriggered = false;

            if (!wasTap)
                return;

            JournalPanelUI journal = EnsureJournalPanel();
            if (journal == null)
                return;

            if (journal.IsOpen && journal.ActiveJournalWindow == JournalWindowId.Recipes)
                return;

            if (DMUiToolkitMenus.TrySwitchJournalTab(JournalWindowId.Recipes))
                return;

            journal.SwitchToTab(JournalWindowId.Recipes);
        }

        private static void TryUseTool(ToolType toolType)
        {
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
