using System;
using System.Collections;
using Project.Crafting;
using Project.AI;
using Project.Companions;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.Pioneers;
using Project.Player;
using Project.Player.Invector;
using Project.Shelter;
using Project.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Shared UITK overlay for leftover in-world menus / dialogs / tooltips.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-365)]
    [DisallowMultipleComponent]
    public class DMUiToolkitWorldMenus : MonoBehaviour
    {
        private static DMUiToolkitWorldMenus instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement shelterHost;
        private Label shelterTimer;
        private Button shelterExit;
        private Button shelterStore;
        private Button shelterCancel;
        private VisualElement drillHost;
        private Label drillStatus;
        private Button drillStart;
        private Button drillStop;
        private Button drillCollect;
        private Button drillStore;
        private Button drillCancel;
        private VisualElement weaponHost;
        private VisualElement weaponPanel;
        private VisualElement pistolsSub;
        private VisualElement riflesSub;
        private Button weaponPistols;
        private Button weaponRifles;
        private Button pistolSight;
        private Button pistolBeam;
        private Button rifleSight;
        private Button rifleBeam;
        private VisualElement lootHost;
        private Label lootTitle;
        private Label lootBody;
        private Button lootNext;
        private Button lootAll;
        private Button lootClose;
        private VisualElement echoHost;
        private Label echoName;
        private Label echoBody;
        private Button echoClose;
        private VisualElement scanHost;
        private Label scanName;
        private VisualElement tamingHost;
        private VisualElement tamingFill;
        private Label tamingLabel;
        private VisualElement itemTooltip;
        private Label itemTipTitle;
        private Label itemTipBody;
        private VisualElement recipeTooltip;
        private Label recipeTipTitle;
        private Label recipeTipBody;
        private Vector2 itemTipScreenPosition;
        private Vector2 pioneerHoverScreenPosition;
        private Vector2 recipeTipScreenPosition;
        private VisualElement pioneerHover;
        private VisualElement pioneerHoverPortrait;
        private Label pioneerHoverTitle;
        private Label pioneerHoverBody;
        private Label pioneerHoverInitials;
        private VisualElement pioneerDismiss;
        private VisualElement pioneerMenu;
        private VisualElement labDismiss;
        private VisualElement labMenu;
        private bool bound;
        private bool wired;

        private bool shelterOpen;
        private bool drillOpen;
        private bool weaponOpen;
        private bool lootOpen;
        private bool echoOpen;
        private bool scanOpen;
        private bool tamingOpen;
        private bool itemTipOpen;
        private bool itemTipCentered;
        private bool recipeTipOpen;
        private bool pioneerHoverOpen;
        private bool pioneerOpen;
        private bool labOpen;
        private int weaponOpenedFrame = -1;

        private QuoraShelterController activeShelter;
        private PlayerController boundShelterPlayer;
        private DMWalkerDrillUsable activeDrill;
        private WeaponModeSwitchController weaponController;
        private PlayerController boundWeaponPlayer;
        private IEnemyLootProvider activeLoot;
        private Action echoClosed;
        private Coroutine scanRoutine;
        private Transform tamingTarget;
        private Vector3 tamingOffset = new Vector3(0f, 2.2f, 0f);
        private PioneerRosterPanelUI rosterPanel;
        private BuildingControlPanelUI labPanel;

        public static bool IsShelterOpen => instance != null && instance.shelterOpen;
        public static bool IsDrillOpen => instance != null && instance.drillOpen;
        public static bool IsWeaponOpen => instance != null && instance.weaponOpen;
        public static bool IsLootOpen => instance != null && instance.lootOpen;

        public static bool IsAnyModalOpen => instance != null && (
            instance.shelterOpen || instance.drillOpen || instance.weaponOpen
            || instance.lootOpen || instance.echoOpen);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitWorldMenus EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.WorldMenusName,
                DMUiToolkitOverlayDocument.WorldMenusUxml,
                DMUiToolkitOverlayDocument.WorldMenusUss,
                DMUiToolkitOverlayDocument.WorldMenusSort);
            if (doc == null)
                return null;

            DMUiToolkitWorldMenus host = doc.GetComponent<DMUiToolkitWorldMenus>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitWorldMenus>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShowShelter(QuoraShelterController shelter)
        {
            if (!DMUiToolkitHud.IsDriving || shelter == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowShelterInternal(shelter);
            return true;
        }

        public static bool TryShowDrill(DMWalkerDrillUsable usable)
        {
            if (!DMUiToolkitHud.IsDriving || usable == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowDrillInternal(usable);
            return true;
        }

        public static bool TryShowWeapon(WeaponModeSwitchController controller)
        {
            if (!DMUiToolkitHud.IsDriving || controller == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowWeaponInternal(controller);
            return true;
        }

        public static bool TryShowLoot(IEnemyLootProvider lootProvider, string enemyName, string lootSummary)
        {
            if (!DMUiToolkitHud.IsDriving || lootProvider == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowLootInternal(lootProvider, enemyName, lootSummary);
            return true;
        }

        public static bool TryShowEcho(string echoDisplayName, string classLine, string abilitySummary, Action closedCallback)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowEchoInternal(echoDisplayName, classLine, abilitySummary, closedCallback);
            return true;
        }

        public static bool TryShowScan(ItemData item)
        {
            if (!DMUiToolkitHud.IsDriving || item == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowScanInternal(item);
            return true;
        }

        public static bool TryShowTaming(Transform target, float progress01, string message)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowTamingInternal(target, progress01, message);
            return true;
        }

        public static bool TryShowItemTooltip(ItemData item, int amount, Vector2 screenPosition, bool centerOnScreen = false)
        {
            if (!CanShowJournalFloatingUi() || item == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowItemTipInternal(item, amount, screenPosition, centerOnScreen);
            return true;
        }

        public static bool TryShowRecipeTooltip(
            RecipeDefinition recipe,
            Vector2 screenPosition,
            bool pendingScroll = false,
            InventorySystem inventory = null)
        {
            if (!CanShowJournalFloatingUi() || recipe == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowRecipeTipInternal(recipe, screenPosition, pendingScroll, inventory);
            return true;
        }

        public static void HideRecipeTooltip() => instance?.HideRecipeTipInternal();

        public static bool TryShowJournalTip(string title, string body, Vector2 screenPosition)
        {
            if (!CanShowJournalFloatingUi())
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowJournalTipInternal(title, body, screenPosition);
            return true;
        }

        public static void HideJournalTip() => HideRecipeTooltip();

        private static bool CanShowJournalFloatingUi()
        {
            if (MainMenuController.BlocksGameplayHud || DMUiToolkitLoadingOverlay.IsShowing)
                return false;
            return DMUiToolkitHud.IsDriving || DMUiToolkitMenus.IsOpen;
        }

        public static bool TryShowPioneerRoster(PioneerRosterPanelUI panel, string pioneerId, Vector2 screenPosition)
        {
            if (!DMUiToolkitHud.IsDriving || panel == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowPioneerRosterInternal(panel, pioneerId, screenPosition);
            return true;
        }

        public static bool TryShowPioneerTrio(PioneerRosterPanelUI panel, int slotIndex, Vector2 screenPosition)
        {
            if (!DMUiToolkitHud.IsDriving || panel == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowPioneerTrioInternal(panel, slotIndex, screenPosition);
            return true;
        }

        public static bool TryShowLab(BuildingControlPanelUI panel, string pioneerId, Vector2 screenPosition)
        {
            if (!DMUiToolkitHud.IsDriving || panel == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowLabInternal(panel, pioneerId, screenPosition);
            return true;
        }

        public static void HideShelter() => instance?.HideShelterInternal();
        public static void HideDrill() => instance?.HideDrillInternal();
        public static void HideWeapon() => instance?.HideWeaponInternal();
        public static void HideLoot() => instance?.HideLootInternal();
        public static void HideEcho() => instance?.HideEchoInternal();
        public static void HideTaming() => instance?.HideTamingInternal();
        public static void HideItemTooltip() => instance?.HideItemTipInternal();

        public static bool TryShowPioneerHover(SkilledPioneerRecord record, Vector2 screenPosition)
        {
            if (!CanShowJournalFloatingUi() || record == null)
                return false;
            DMUiToolkitWorldMenus host = EnsureHost();
            if (host == null)
                return false;
            host.ShowPioneerHoverInternal(record, screenPosition);
            return true;
        }

        public static void HidePioneerHover() => instance?.HidePioneerHoverInternal();
        public static void HidePioneer() => instance?.HidePioneerInternal();
        public static void HideLab() => instance?.HideLabInternal();

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (shelterOpen)
            {
                if (UiEscapeGate.TryConsumeEscape())
                {
                    HideShelterInternal();
                    return;
                }
                RefreshShelter();
            }

            if (drillOpen)
            {
                if (UiEscapeGate.TryConsumeEscape())
                {
                    HideDrillInternal();
                    return;
                }

                if (activeDrill == null || !activeDrill.IsWithinInteractRange(GetPlayerPosition()))
                {
                    HideDrillInternal();
                    return;
                }

                RefreshDrill();
            }

            if (weaponOpen)
            {
                if (UiEscapeGate.TryConsumeEscape())
                {
                    HideWeaponInternal();
                    return;
                }

                if (Time.frameCount != weaponOpenedFrame
                    && UnityEngine.InputSystem.Mouse.current != null
                    && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                    HideWeaponInternal();
            }

            if (lootOpen)
            {
                if (UiEscapeGate.TryConsumeEscape())
                {
                    HideLootInternal();
                    return;
                }

                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                        OnLootAll();
                    else
                        OnLootNext();
                }
            }

            if (pioneerOpen && UiEscapeGate.TryConsumeEscape())
                HidePioneerInternal();
            if (labOpen && UiEscapeGate.TryConsumeEscape())
                HideLabInternal();
        }

        private static bool uguiHidden;

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            TickTaming();

            Vector2 pointer = CurrentPointerScreenPosition();
            if (itemTipOpen && !itemTipCentered)
            {
                itemTipScreenPosition = pointer;
                PositionItemTip(pointer);
            }

            if (recipeTipOpen)
            {
                recipeTipScreenPosition = pointer;
                PositionRecipeTip(pointer);
            }

            if (pioneerHoverOpen)
            {
                pioneerHoverScreenPosition = pointer;
                PositionPioneerHover(pointer);
            }

            if (!DMUiToolkitHud.IsDriving)
            {
                uguiHidden = false;
                return;
            }

            if (uguiHidden)
                return;

            HideUgui();
            uguiHidden = true;
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("world-root") ?? tree;
            shelterHost = tree.Q<VisualElement>("shelter-host");
            shelterTimer = tree.Q<Label>("shelter-timer");
            shelterExit = tree.Q<Button>("shelter-exit");
            shelterStore = tree.Q<Button>("shelter-store");
            shelterCancel = tree.Q<Button>("shelter-cancel");
            drillHost = tree.Q<VisualElement>("drill-host");
            drillStatus = tree.Q<Label>("drill-status");
            drillStart = tree.Q<Button>("drill-start");
            drillStop = tree.Q<Button>("drill-stop");
            drillCollect = tree.Q<Button>("drill-collect");
            drillStore = tree.Q<Button>("drill-store");
            drillCancel = tree.Q<Button>("drill-cancel");
            weaponHost = tree.Q<VisualElement>("weapon-host");
            weaponPanel = tree.Q<VisualElement>("weapon-panel");
            pistolsSub = tree.Q<VisualElement>("weapon-pistols-sub");
            riflesSub = tree.Q<VisualElement>("weapon-rifles-sub");
            weaponPistols = tree.Q<Button>("weapon-pistols");
            weaponRifles = tree.Q<Button>("weapon-rifles");
            pistolSight = tree.Q<Button>("weapon-pistol-sight");
            pistolBeam = tree.Q<Button>("weapon-pistol-beam");
            rifleSight = tree.Q<Button>("weapon-rifle-sight");
            rifleBeam = tree.Q<Button>("weapon-rifle-beam");
            lootHost = tree.Q<VisualElement>("loot-host");
            lootTitle = tree.Q<Label>("loot-title");
            lootBody = tree.Q<Label>("loot-body");
            lootNext = tree.Q<Button>("loot-next");
            lootAll = tree.Q<Button>("loot-all");
            lootClose = tree.Q<Button>("loot-close");
            echoHost = tree.Q<VisualElement>("echo-host");
            echoName = tree.Q<Label>("echo-name");
            echoBody = tree.Q<Label>("echo-body");
            echoClose = tree.Q<Button>("echo-close");
            scanHost = tree.Q<VisualElement>("scan-host");
            scanName = tree.Q<Label>("scan-name");
            tamingHost = tree.Q<VisualElement>("taming-host");
            tamingFill = tree.Q<VisualElement>("taming-fill");
            tamingLabel = tree.Q<Label>("taming-label");
            itemTooltip = tree.Q<VisualElement>("item-tooltip");
            itemTipTitle = tree.Q<Label>("item-tip-title");
            itemTipBody = tree.Q<Label>("item-tip-body");
            recipeTooltip = tree.Q<VisualElement>("recipe-tooltip");
            recipeTipTitle = tree.Q<Label>("recipe-tip-title");
            recipeTipBody = tree.Q<Label>("recipe-tip-body");
            pioneerHover = tree.Q<VisualElement>("pioneer-hover");
            pioneerHoverPortrait = tree.Q<VisualElement>("pioneer-hover-portrait");
            pioneerHoverTitle = tree.Q<Label>("pioneer-hover-title");
            pioneerHoverBody = tree.Q<Label>("pioneer-hover-body");
            pioneerHoverInitials = tree.Q<Label>("pioneer-hover-initials");
            pioneerDismiss = tree.Q<VisualElement>("pioneer-dismiss");
            pioneerMenu = tree.Q<VisualElement>("pioneer-menu");
            labDismiss = tree.Q<VisualElement>("lab-dismiss");
            labMenu = tree.Q<VisualElement>("lab-menu");
            Wire();
            ApplyHostVis();
            bound = root != null;
        }

        private void Wire()
        {
            if (wired)
                return;

            if (shelterExit != null) shelterExit.clicked += OnShelterExit;
            if (shelterStore != null) shelterStore.clicked += OnShelterStore;
            if (shelterCancel != null) shelterCancel.clicked += HideShelterInternal;
            if (drillStart != null) drillStart.clicked += OnDrillStart;
            if (drillStop != null) drillStop.clicked += OnDrillStop;
            if (drillCollect != null) drillCollect.clicked += OnDrillCollect;
            if (drillStore != null) drillStore.clicked += OnDrillStore;
            if (drillCancel != null) drillCancel.clicked += HideDrillInternal;
            if (weaponPistols != null)
            {
                weaponPistols.clicked += ShowPistolsSub;
                weaponPistols.RegisterCallback<PointerEnterEvent>(_ => ShowPistolsSub());
            }
            if (weaponRifles != null)
            {
                weaponRifles.clicked += ShowRiflesSub;
                weaponRifles.RegisterCallback<PointerEnterEvent>(_ => ShowRiflesSub());
            }
            if (pistolSight != null) pistolSight.clicked += TogglePistolSight;
            if (pistolBeam != null) pistolBeam.clicked += TogglePistolBeam;
            if (rifleSight != null) rifleSight.clicked += ToggleRifleSight;
            if (rifleBeam != null) rifleBeam.clicked += ToggleRifleBeam;
            if (lootNext != null) lootNext.clicked += OnLootNext;
            if (lootAll != null) lootAll.clicked += OnLootAll;
            if (lootClose != null) lootClose.clicked += HideLootInternal;
            if (echoClose != null) echoClose.clicked += HideEchoInternal;
            if (pioneerDismiss != null) pioneerDismiss.RegisterCallback<ClickEvent>(_ => HidePioneerInternal());
            if (labDismiss != null) labDismiss.RegisterCallback<ClickEvent>(_ => HideLabInternal());
            if (weaponHost != null)
                weaponHost.RegisterCallback<ClickEvent>(evt =>
                {
                    if (evt.target == weaponHost)
                        HideWeaponInternal();
                });
            wired = true;
        }

        private void ApplyHostVis()
        {
            DMUiToolkitOverlayDocument.SetShown(shelterHost, shelterOpen);
            DMUiToolkitOverlayDocument.SetShown(drillHost, drillOpen);
            DMUiToolkitOverlayDocument.SetShown(weaponHost, weaponOpen);
            DMUiToolkitOverlayDocument.SetShown(pistolsSub, false);
            DMUiToolkitOverlayDocument.SetShown(riflesSub, false);
            DMUiToolkitOverlayDocument.SetShown(lootHost, lootOpen);
            DMUiToolkitOverlayDocument.SetShown(echoHost, echoOpen);
            DMUiToolkitOverlayDocument.SetShown(scanHost, scanOpen);
            DMUiToolkitOverlayDocument.SetShown(tamingHost, tamingOpen);
            DMUiToolkitOverlayDocument.SetShown(itemTooltip, itemTipOpen);
            DMUiToolkitOverlayDocument.SetShown(recipeTooltip, recipeTipOpen);
            DMUiToolkitOverlayDocument.SetShown(pioneerHover, pioneerHoverOpen);
            DMUiToolkitOverlayDocument.SetShown(pioneerDismiss, pioneerOpen);
            DMUiToolkitOverlayDocument.SetShown(pioneerMenu, pioneerOpen);
            DMUiToolkitOverlayDocument.SetShown(labDismiss, labOpen);
            DMUiToolkitOverlayDocument.SetShown(labMenu, labOpen);

            if (shelterOpen || drillOpen || weaponOpen || lootOpen || echoOpen || scanOpen || tamingOpen
                || pioneerOpen || labOpen)
                DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
        }

        private void ShowShelterInternal(QuoraShelterController shelter)
        {
            BindTree();
            activeShelter = shelter;
            shelterOpen = true;
            DMUiToolkitOverlayDocument.SetShown(shelterHost, true);
            boundShelterPlayer = PlayerLocator.FindPlayerController();
            boundShelterPlayer?.SetBuildingControlOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuoraShelterMenu, true);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            RefreshShelter();
        }

        private void HideShelterInternal()
        {
            shelterOpen = false;
            activeShelter = null;
            DMUiToolkitOverlayDocument.SetShown(shelterHost, false);
            if (boundShelterPlayer != null)
            {
                boundShelterPlayer.SetBuildingControlOpen(false);
                boundShelterPlayer.ApplyCursorState();
                boundShelterPlayer = null;
            }
            else
            {
                PlayerLocator.FindPlayerController()?.SetBuildingControlOpen(false);
            }

            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuoraShelterMenu, false);
            GameplayInputRecovery.QueueCursorRestore();
        }

        private void RefreshShelter()
        {
            if (activeShelter == null || shelterTimer == null)
                return;

            int minutes = Mathf.FloorToInt(activeShelter.RemainingLifetimeSeconds / 60f);
            int seconds = Mathf.FloorToInt(activeShelter.RemainingLifetimeSeconds % 60f);
            shelterTimer.text = $"Deploy time remaining: {minutes:00}:{seconds:00}";
        }

        private void OnShelterExit()
        {
            activeShelter?.TryExitShelter(storeInInventory: false);
            HideShelterInternal();
        }

        private void OnShelterStore()
        {
            activeShelter?.TryExitShelter(storeInInventory: true);
            HideShelterInternal();
        }

        private void ShowDrillInternal(DMWalkerDrillUsable usable)
        {
            BindTree();
            activeDrill = usable;
            drillOpen = true;
            DMUiToolkitOverlayDocument.SetShown(drillHost, true);
            ApplyDrillInput(true);
            RefreshDrill();
        }

        private void HideDrillInternal()
        {
            drillOpen = false;
            activeDrill = null;
            DMUiToolkitOverlayDocument.SetShown(drillHost, false);
            ApplyDrillInput(false);
        }

        private void RefreshDrill()
        {
            if (activeDrill == null)
                return;

            DMWalkerDrillController controller = activeDrill.DrillController;
            bool mining = controller != null && controller.IsMining;
            bool spinning = controller != null && controller.IsSpinning;
            bool retracting = controller != null && controller.IsRetracting;
            if (drillStatus != null)
            {
                if (retracting)
                    drillStatus.text = "Status: Retracting...";
                else if (spinning)
                    drillStatus.text = "Status: Mining (spinning)";
                else if (mining)
                    drillStatus.text = "Status: Starting drill...";
                else
                    drillStatus.text = "Status: Idle";
            }

            drillStart?.SetEnabled(!mining);
            drillStop?.SetEnabled(mining && !retracting);
            drillCollect?.SetEnabled(!retracting);
            drillStore?.SetEnabled(!mining);
        }

        private void OnDrillStart()
        {
            activeDrill?.DrillController?.StartMining();
            RefreshDrill();
        }

        private void OnDrillStop()
        {
            activeDrill?.DrillController?.StopMining();
            RefreshDrill();
        }

        private void OnDrillCollect()
        {
            PickupToastUI.Show("Collect Resources - coming soon.");
        }

        private void OnDrillStore()
        {
            if (activeDrill == null)
                return;

            InventorySystem inventory = PlayerLocator.FindPlayerObject()?.GetComponent<InventorySystem>();
            bool stored = activeDrill.TryStoreFromMenu(inventory, out string message);
            if (!string.IsNullOrEmpty(message))
                PickupToastUI.Show(message);
            if (stored)
                HideDrillInternal();
        }

        private static void ApplyDrillInput(bool menuOpen)
        {
            PlayerController player = PlayerLocator.FindPlayerController();
            player?.SetBuildingControlOpen(menuOpen);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonWalkerDrillMenu, menuOpen);
            if (menuOpen)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            else
            {
                player?.ApplyCursorState();
            }
        }

        private static Vector3 GetPlayerPosition()
        {
            Transform t = PlayerLocator.FindPlayerObject()?.transform;
            return t != null ? t.position : Vector3.zero;
        }

        private void ShowWeaponInternal(WeaponModeSwitchController controller)
        {
            BindTree();
            weaponController = controller;
            boundWeaponPlayer = controller.GetComponent<PlayerController>() ?? PlayerLocator.FindPlayerController();
            weaponOpenedFrame = Time.frameCount;
            weaponOpen = true;
            DMUiToolkitOverlayDocument.SetShown(weaponHost, true);
            DMUiToolkitOverlayDocument.SetShown(pistolsSub, false);
            DMUiToolkitOverlayDocument.SetShown(riflesSub, false);
            if (weaponPanel != null)
                DMUiToolkitOverlayDocument.PositionCenterOnScreen(weaponPanel);
            boundWeaponPlayer?.SetBuildingControlOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonWeaponModeSwitch, false);
            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonWeaponModeSwitch, true);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            RefreshWeapon();
        }

        private void HideWeaponInternal()
        {
            weaponOpen = false;
            weaponController = null;
            DMUiToolkitOverlayDocument.SetShown(weaponHost, false);
            DMUiToolkitOverlayDocument.SetShown(pistolsSub, false);
            DMUiToolkitOverlayDocument.SetShown(riflesSub, false);
            if (boundWeaponPlayer != null)
            {
                boundWeaponPlayer.SetBuildingControlOpen(false);
                boundWeaponPlayer = null;
            }
            else
            {
                PlayerLocator.FindPlayerController()?.SetBuildingControlOpen(false);
            }

            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonWeaponModeSwitch, false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonWeaponModeSwitch, false);
        }

        private void ShowPistolsSub()
        {
            DMUiToolkitOverlayDocument.SetShown(riflesSub, false);
            DMUiToolkitOverlayDocument.SetShown(pistolsSub, true);
            PositionWeaponFlyout(weaponPanel, pistolsSub);
            RefreshWeapon();
        }

        private void ShowRiflesSub()
        {
            DMUiToolkitOverlayDocument.SetShown(pistolsSub, false);
            DMUiToolkitOverlayDocument.SetShown(riflesSub, true);
            PositionWeaponFlyout(weaponPanel, riflesSub);
            RefreshWeapon();
        }

        private static void PositionWeaponFlyout(VisualElement anchor, VisualElement flyout)
        {
            if (anchor == null || flyout == null)
                return;

            anchor.schedule.Execute(() =>
            {
                if (anchor == null || flyout == null)
                    return;

                flyout.style.position = Position.Absolute;
                flyout.style.top = anchor.resolvedStyle.top;
                flyout.style.left = anchor.resolvedStyle.left + anchor.resolvedStyle.width + 8f;
            }).ExecuteLater(0);
        }

        private void TogglePistolSight()
        {
            if (weaponController == null) return;
            weaponController.SetPistolLaserSightEnabled(!weaponController.PistolLaserSightEnabled);
            RefreshWeapon();
        }

        private void TogglePistolBeam()
        {
            if (weaponController == null) return;
            weaponController.SetPistolLaserBeamEnabled(!weaponController.PistolLaserBeamEnabled);
            RefreshWeapon();
        }

        private void ToggleRifleSight()
        {
            if (weaponController == null) return;
            weaponController.SetLaserSightEnabled(!weaponController.LaserSightEnabled);
            RefreshWeapon();
        }

        private void ToggleRifleBeam()
        {
            if (weaponController == null) return;
            weaponController.SetLaserBeamEnabled(!weaponController.LaserBeamEnabled);
            RefreshWeapon();
        }

        private void RefreshWeapon()
        {
            if (weaponController == null)
                return;

            if (pistolSight != null)
                pistolSight.text = FormatToggle("LaserSight", weaponController.PistolLaserSightEnabled);
            if (pistolBeam != null)
                pistolBeam.text = FormatToggle("Laser", weaponController.PistolLaserBeamEnabled);
            if (rifleSight != null)
                rifleSight.text = FormatToggle("LaserSight", weaponController.LaserSightEnabled);
            if (rifleBeam != null)
                rifleBeam.text = FormatToggle("Laser", weaponController.LaserBeamEnabled);
        }

        private static string FormatToggle(string name, bool on) => on ? $"{name}  ON" : $"{name}  OFF";

        private void ShowLootInternal(IEnemyLootProvider lootProvider, string enemyName, string lootSummary)
        {
            BindTree();
            activeLoot = lootProvider;
            lootOpen = true;
            if (lootTitle != null)
                lootTitle.text = string.IsNullOrWhiteSpace(enemyName) ? "Loot" : $"Loot - {enemyName}";
            if (lootBody != null)
                lootBody.text = lootSummary ?? string.Empty;
            DMUiToolkitOverlayDocument.SetShown(lootHost, true);
            PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            player?.SetLootDialogOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonLootDialog, true);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            RefreshLoot();
        }

        private void HideLootInternal()
        {
            lootOpen = false;
            activeLoot = null;
            DMUiToolkitOverlayDocument.SetShown(lootHost, false);
            PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            player?.SetLootDialogOpen(false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonLootDialog, false);
        }

        private void RefreshLoot()
        {
            bool hasLoot = activeLoot != null && activeLoot.HasRemainingLoot;
            lootNext?.SetEnabled(hasLoot);
            lootAll?.SetEnabled(hasLoot);
            if (activeLoot != null && lootBody != null)
                lootBody.text = activeLoot.BuildLootSummary();
        }

        private void OnLootNext()
        {
            if (activeLoot == null)
                return;
            activeLoot.TryLootNextEntry();
            RefreshLoot();
            if (activeLoot == null || !activeLoot.HasRemainingLoot)
                HideLootInternal();
        }

        private void OnLootAll()
        {
            if (activeLoot == null)
                return;
            activeLoot.TryLootAll();
            RefreshLoot();
            if (activeLoot == null || !activeLoot.HasRemainingLoot)
                HideLootInternal();
        }

        private void ShowEchoInternal(string echoDisplayName, string classLine, string abilitySummary, Action closedCallback)
        {
            BindTree();
            echoClosed = closedCallback;
            echoOpen = true;
            if (echoName != null)
                echoName.text = string.IsNullOrWhiteSpace(echoDisplayName) ? "Unknown Echo" : echoDisplayName;
            string classText = string.IsNullOrWhiteSpace(classLine) ? "Unclassified imprint" : classLine;
            string abilityText = string.IsNullOrWhiteSpace(abilitySummary) ? "Ability matrix pending analysis." : abilitySummary;
            if (echoBody != null)
                echoBody.text = $"{classText}\n\n{abilityText}\n\nThis pioneer can be assigned to base structures or listed on the Pioneer Exchange after training.";
            DMUiToolkitOverlayDocument.SetShown(echoHost, true);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void HideEchoInternal()
        {
            echoOpen = false;
            DMUiToolkitOverlayDocument.SetShown(echoHost, false);
            Action callback = echoClosed;
            echoClosed = null;
            callback?.Invoke();
        }

        private void ShowScanInternal(ItemData item)
        {
            BindTree();
            scanOpen = true;
            if (scanName != null)
                scanName.text = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
            DMUiToolkitOverlayDocument.SetShown(scanHost, true);
            if (scanRoutine != null)
                StopCoroutine(scanRoutine);
            scanRoutine = StartCoroutine(AnimateScan());
        }

        private IEnumerator AnimateScan()
        {
            if (scanHost != null)
            {
                scanHost.style.opacity = 0f;
                scanHost.style.translate = new Translate(0, 28);
            }

            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.35f);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (scanHost != null)
                {
                    scanHost.style.opacity = eased;
                    scanHost.style.translate = new Translate(0, Mathf.Lerp(28f, 0f, eased));
                }
                yield return null;
            }

            yield return new WaitForSecondsRealtime(2.3f);
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.35f);
                if (scanHost != null)
                {
                    scanHost.style.opacity = 1f - t;
                    scanHost.style.translate = new Translate(0, -18f * t);
                }
                yield return null;
            }

            scanOpen = false;
            DMUiToolkitOverlayDocument.SetShown(scanHost, false);
            scanRoutine = null;
        }

        private void ShowTamingInternal(Transform target, float progress01, string message)
        {
            BindTree();
            tamingTarget = target;
            tamingOpen = target != null;
            DMUiToolkitOverlayDocument.SetShown(tamingHost, tamingOpen);
            if (tamingFill != null)
                tamingFill.style.width = Length.Percent(Mathf.Clamp01(progress01) * 100f);
            if (tamingLabel != null)
                tamingLabel.text = message ?? string.Empty;
        }

        private void HideTamingInternal()
        {
            tamingOpen = false;
            tamingTarget = null;
            DMUiToolkitOverlayDocument.SetShown(tamingHost, false);
        }

        private void TickTaming()
        {
            if (!tamingOpen || tamingHost == null || tamingTarget == null)
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Vector3 world = tamingTarget.position + tamingOffset;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z < 0f)
            {
                DMUiToolkitOverlayDocument.SetShown(tamingHost, false);
                return;
            }

            DMUiToolkitOverlayDocument.SetShown(tamingHost, true);
            if (tamingHost.panel != null)
            {
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(tamingHost.panel, new Vector2(screen.x, screen.y));
                tamingHost.style.left = panelPos.x - 90f;
                tamingHost.style.top = panelPos.y - 14f;
            }
        }

        private static Vector2 CurrentPointerScreenPosition()
        {
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            if (UnityEngine.InputSystem.Pointer.current != null)
                return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            return Vector2.zero;
        }

        private void ShowItemTipInternal(ItemData item, int amount, Vector2 screenPosition, bool centerOnScreen)
        {
            BindTree();
            RecipeHoverTooltip.HideAny();
            HideRecipeTipInternal();
            HidePioneerHoverInternal();
            itemTipOpen = true;
            itemTipCentered = centerOnScreen;
            itemTipScreenPosition = screenPosition;
            if (itemTipTitle != null)
                itemTipTitle.text = ItemTooltipFormatter.BuildTitle(item);
            if (itemTipBody != null)
                itemTipBody.text = ItemTooltipFormatter.BuildBody(item, amount);
            DMUiToolkitOverlayDocument.SetShown(itemTooltip, true);
            PositionItemTip(screenPosition);
        }

        private void HideItemTipInternal()
        {
            itemTipOpen = false;
            itemTipCentered = false;
            DMUiToolkitOverlayDocument.SetShown(itemTooltip, false);
        }

        private void ShowRecipeTipInternal(
            RecipeDefinition recipe,
            Vector2 screenPosition,
            bool pendingScroll,
            InventorySystem inventory)
        {
            BindTree();
            HideItemTipInternal();
            HidePioneerHoverInternal();
            recipeTipOpen = true;
            recipeTipScreenPosition = screenPosition;
            if (recipeTipTitle != null)
                recipeTipTitle.text = RecipeTooltipFormatter.BuildTitle(recipe);
            if (recipeTipBody != null)
            {
                recipeTipBody.text = pendingScroll
                    ? RecipeTooltipFormatter.BuildScrollBody(recipe)
                    : RecipeTooltipFormatter.BuildBody(recipe, inventory);
            }

            DMUiToolkitOverlayDocument.SetShown(recipeTooltip, true);
            PositionRecipeTip(screenPosition);
        }

        private void HideRecipeTipInternal()
        {
            recipeTipOpen = false;
            DMUiToolkitOverlayDocument.SetShown(recipeTooltip, false);
        }

        private void ShowJournalTipInternal(string title, string body, Vector2 screenPosition)
        {
            BindTree();
            HideItemTipInternal();
            HidePioneerHoverInternal();
            recipeTipOpen = true;
            recipeTipScreenPosition = screenPosition;
            if (recipeTipTitle != null)
                recipeTipTitle.text = title ?? string.Empty;
            if (recipeTipBody != null)
                recipeTipBody.text = body ?? string.Empty;
            DMUiToolkitOverlayDocument.SetShown(recipeTooltip, true);
            PositionRecipeTip(screenPosition);
        }

        private void PositionItemTip(Vector2 screenPosition)
        {
            if (itemTooltip == null || itemTooltip.panel == null)
                return;

            if (itemTipCentered)
            {
                itemTooltip.style.width = 320f;
                DMUiToolkitOverlayDocument.PositionCenterOnScreen(itemTooltip);
                return;
            }

            itemTooltip.style.width = 240f;
            DMUiToolkitOverlayDocument.PositionNearPointer(
                itemTooltip,
                screenPosition,
                DMUiToolkitOverlayDocument.DefaultHoverOffset,
                root);
        }

        private void PositionRecipeTip(Vector2 screenPosition)
        {
            if (recipeTooltip == null)
                return;

            DMUiToolkitOverlayDocument.PositionNearPointer(
                recipeTooltip,
                screenPosition,
                DMUiToolkitOverlayDocument.DefaultHoverOffset,
                root);
        }

        private void PositionPioneerHover(Vector2 screenPosition)
        {
            if (pioneerHover == null)
                return;

            DMUiToolkitOverlayDocument.PositionNearPointer(
                pioneerHover,
                screenPosition,
                DMUiToolkitOverlayDocument.DefaultHoverOffset,
                root);
        }


        private void ShowPioneerHoverInternal(SkilledPioneerRecord record, Vector2 screenPosition)
        {
            BindTree();
            HideItemTipInternal();
            HideRecipeTipInternal();
            pioneerHoverOpen = true;
            pioneerHoverScreenPosition = screenPosition;
            if (pioneerHoverTitle != null)
                pioneerHoverTitle.text = PioneerUiLabels.GetDisplayName(record);
            if (pioneerHoverBody != null)
                pioneerHoverBody.text = PioneerHoverTooltip.BuildBody(record);
            Sprite sprite = PioneerPortraitResolver.Resolve(record);
            if (pioneerHoverPortrait != null)
            {
                if (DMUiToolkitStyle.TrySetSpriteBackground(pioneerHoverPortrait, sprite, ScaleMode.ScaleToFit))
                    pioneerHoverPortrait.style.backgroundColor = Color.clear;
                else
                {
                    DMUiToolkitStyle.ClearBackgroundImage(pioneerHoverPortrait);
                    pioneerHoverPortrait.style.backgroundColor = DarkMatterGenesisUiPalette.SlateGray;
                }
            }
            if (pioneerHoverInitials != null)
            {
                pioneerHoverInitials.text = sprite != null || record == null
                    ? string.Empty
                    : PioneerPortraitUi.BuildInitials(PioneerUiLabels.GetDisplayName(record));
            }
            DMUiToolkitOverlayDocument.SetShown(pioneerHover, true);
            PositionPioneerHover(screenPosition);
        }

        private void HidePioneerHoverInternal()
        {
            pioneerHoverOpen = false;
            DMUiToolkitOverlayDocument.SetShown(pioneerHover, false);
        }

        private void ShowPioneerRosterInternal(PioneerRosterPanelUI panel, string pioneerId, Vector2 screenPosition)
        {
            rosterPanel = panel;
            pioneerMenu.Clear();
            AddPioneerButton("Edit Loadout", () => panel.SelectPioneer(pioneerId));
            AddPioneerButton("Slot to Trio", () => panel.SlotPioneerToFirstEmpty(pioneerId));
            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                int slot = i;
                AddPioneerButton($"Slot to Slot {slot + 1}", () => panel.AssignPioneerToTrioSlot(slot, pioneerId));
            }
            AddPioneerButton("Transmute Loadout", () => panel.TransmutePioneerLoadout(pioneerId));
            PresentPioneer(screenPosition);
        }

        private void ShowPioneerTrioInternal(PioneerRosterPanelUI panel, int slotIndex, Vector2 screenPosition)
        {
            rosterPanel = panel;
            pioneerMenu.Clear();
            string assignedId = panel.GetTrioDraftId(slotIndex);
            if (string.IsNullOrEmpty(assignedId))
            {
                AddPioneerButton("Select from roster", () => panel.BeginPendingTrioSlot(slotIndex));
            }
            else
            {
                AddPioneerButton("Edit Loadout", () => panel.SelectPioneer(assignedId));
                CompanionRosterBridge bridge = UnityEngine.Object.FindAnyObjectByType<CompanionRosterBridge>();
                if (bridge != null)
                {
                    AddPioneerButton("Follow", () => bridge.SetCompanionFollowMode(assignedId, PioneerFollowMode.FollowPlayer));
                    AddPioneerButton("Hold Here", () =>
                    {
                        Vector3 holdPoint = PioneerExpeditionCommandInput.ResolveHoldPointNearPlayer();
                        float facing = Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
                        bridge.SetCompanionHold(assignedId, holdPoint, facing);
                    });
                    AddPioneerButton("Defend", () => bridge.SetCompanionFollowMode(assignedId, PioneerFollowMode.DefendPlayer));
                    AddPioneerButton("Follow Self", () => bridge.SetCompanionFollowMode(assignedId, PioneerFollowMode.FollowSelf));
                }
                AddPioneerButton("Unslot", () => panel.UnslotTrioSlot(slotIndex));
                AddPioneerButton("Transmute Pioneer", () => panel.TransmuteTrioSlot(slotIndex));
            }
            PresentPioneer(screenPosition);
        }

        private void AddPioneerButton(string label, Action action)
        {
            Button button = DMUiToolkitOverlayDocument.MakeMenuButton(label.Replace(" ", string.Empty), label);
            button.clicked += () =>
            {
                action?.Invoke();
                HidePioneerInternal();
            };
            pioneerMenu.Add(button);
        }

        private void PresentPioneer(Vector2 screenPosition)
        {
            BindTree();
            pioneerOpen = true;
            DMUiToolkitOverlayDocument.SetShown(pioneerDismiss, true);
            DMUiToolkitOverlayDocument.SetShown(pioneerMenu, true);
            DMUiToolkitOverlayDocument.PositionContextMenu(pioneerMenu, screenPosition);
        }

        private void HidePioneerInternal()
        {
            pioneerOpen = false;
            DMUiToolkitOverlayDocument.SetShown(pioneerDismiss, false);
            DMUiToolkitOverlayDocument.SetShown(pioneerMenu, false);
        }

        private void ShowLabInternal(BuildingControlPanelUI panel, string pioneerId, Vector2 screenPosition)
        {
            BindTree();
            labPanel = panel;
            labMenu.Clear();
            Button button = DMUiToolkitOverlayDocument.MakeMenuButton("Reassign", "Reassign");
            button.clicked += () =>
            {
                panel.TryReassignInjuredPioneer(pioneerId);
                HideLabInternal();
            };
            labMenu.Add(button);
            labOpen = true;
            DMUiToolkitOverlayDocument.SetShown(labDismiss, true);
            DMUiToolkitOverlayDocument.SetShown(labMenu, true);
            DMUiToolkitOverlayDocument.PositionContextMenu(labMenu, screenPosition);
        }

        private void HideLabInternal()
        {
            labOpen = false;
            DMUiToolkitOverlayDocument.SetShown(labDismiss, false);
            DMUiToolkitOverlayDocument.SetShown(labMenu, false);
        }

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving)
                return;

            HideType<QuoraShelterMenuUI>();
            HideType<WalkerDrillInteractMenuUI>();
            HideType<WeaponModeSwitchMenuUI>();
            HideType<EnemyLootDialogUI>();
            HideType<EchoRescueRevealUI>();
            HideType<ResourceScanResultUI>();
            HideType<PetTamingProgressUI>();
            HideType<ItemHoverTooltip>();
            HideType<PioneerRosterContextMenu>();
            HideType<ScienceLabHealthContextMenu>();
        }

        private static void HideType<T>() where T : Component
        {
            T found = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (found != null)
                DMUiToolkitOverlayDocument.HideGameObject(found.gameObject);
        }
    }
}
