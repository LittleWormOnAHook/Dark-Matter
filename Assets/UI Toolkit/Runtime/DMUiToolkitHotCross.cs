using System.Collections.Generic;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Hot Cross gameplay hotbar  -  gold cross with four quadrant cutout icons, placed
    /// to the right of the pilot cluster. Replaces the legacy 1"10 HUD strip + tools row.
    /// Inventory menu still owns slots 1"10; this is the world HUD face only.
    /// </summary>
    [DefaultExecutionOrder(-360)]
    [DisallowMultipleComponent]
    public class DMUiToolkitHotCross : MonoBehaviour
    {
        public enum ToolFace
        {
            Binoculars = 0,
            Scanner = 1
        }

        private const int WeaponSlotCount = 4;
        private const int ConsumableFirstLocal = 4;
        private const int ConsumableLastLocal = 9;

        private static DMUiToolkitHotCross instance;

        /// <summary>Focused TL weapon hotbar local index (0-3 / inventory slots 1-4). Tab cycles focus only; LMB arms.</summary>
        private static int weaponLocalIndex;

        /// <summary>Last consumable/utility hotbar local index (4"9) shown in TR.</summary>
        private static int consumableLocalIndex = ConsumableFirstLocal;

        /// <summary>Which tool icon BL shows (B = binoculars, N = scanner). Element specials may share this face later.</summary>
        private static ToolFace toolFace = ToolFace.Binoculars;

        private UIDocument document;
        private VisualElement root;
        private VisualElement cross;
        private VisualElement quadTl;
        private VisualElement quadTr;
        private VisualElement quadBl;
        private VisualElement iconTl;
        private VisualElement iconTr;
        private VisualElement iconBl;
        private VisualElement glowTl;
        private VisualElement glowTr;
        private VisualElement glowBl;
        private DMHotCrossIconRegistry iconRegistry;
        private Label amtTl;
        private Label amtTr;
        private Label keyBl;
        private Label clipLabel;
        private WeaponAmmoState ammoState;
        private int lastClipShown = int.MinValue;
        private Color lastClipColor;
        private VisualElement ammoPopup;
        private Label ammoTitle;
        private Label ammoHint;
        private VisualElement ammoList;
        private bool bound;

        private bool ammoPopupOpen;
        private bool visualsDirty = true;
        private bool lastShown;
        private int ammoPopupAbsoluteSlot = -1;
        private readonly List<InventoryItemActions.AmmoEquipOption> ammoOptions = new List<InventoryItemActions.AmmoEquipOption>(4);
        private int ammoHighlightIndex;
        private System.Action<int> ammoConfirmHandler;

        private InventorySystem inventory;
        private EquipmentController equipment;

        public static DMUiToolkitHotCross Instance => instance;

        public static int ConsumableLocalIndex => consumableLocalIndex;

        public static int WeaponLocalIndex => weaponLocalIndex;

        public static ToolFace ActiveToolFace => toolFace;

        public static bool IsAmmoLoadPopupOpen => instance != null && instance.ammoPopupOpen;

        /// <summary>Absolute inventory slot of the ammo stack the load popup is targeting.</summary>
        public static int AmmoLoadAbsoluteSlot => instance != null ? instance.ammoPopupAbsoluteSlot : -1;

        public static int AmmoLoadHighlightIndex => instance != null ? instance.ammoHighlightIndex : 0;

        public static int AmmoLoadOptionCount => instance != null ? instance.ammoOptions.Count : 0;

        public static bool TryGetAmmoLoadHighlightedWeapon(out int weaponHotbarSlot)
        {
            weaponHotbarSlot = -1;
            if (instance == null || !instance.ammoPopupOpen || instance.ammoOptions.Count == 0)
                return false;
            int idx = Mathf.Clamp(instance.ammoHighlightIndex, 0, instance.ammoOptions.Count - 1);
            weaponHotbarSlot = instance.ammoOptions[idx].WeaponHotbarSlot;
            return true;
        }

        /// <summary>
        /// Opens the Hot Cross ammo load popup. Highlight prefers the currently drawn weapon when eligible.
        /// </summary>
        public static bool ShowAmmoLoadPopup(
            int ammoAbsoluteSlot,
            List<InventoryItemActions.AmmoEquipOption> options,
            int preferredWeaponHotbarSlot,
            System.Action<int> onConfirmWeaponHotbar)
        {
            EnsureHost();
            if (instance == null)
                return false;
            return instance.ShowAmmoLoadPopupInternal(ammoAbsoluteSlot, options, preferredWeaponHotbarSlot, onConfirmWeaponHotbar);
        }

        public static void CycleAmmoLoadHighlight()
        {
            if (instance == null || !instance.ammoPopupOpen || instance.ammoOptions.Count == 0)
                return;
            instance.ammoHighlightIndex = (instance.ammoHighlightIndex + 1) % instance.ammoOptions.Count;
            instance.RebuildAmmoList();
        }

        public static bool TryConfirmAmmoLoad()
        {
            if (instance == null || !instance.ammoPopupOpen)
                return false;
            if (!TryGetAmmoLoadHighlightedWeapon(out int weaponHotbar))
                return false;
            System.Action<int> handler = instance.ammoConfirmHandler;
            instance.HideAmmoLoadPopupInternal();
            handler?.Invoke(weaponHotbar);
            return true;
        }

        public static void HideAmmoLoadPopup()
        {
            if (instance == null)
                return;
            instance.HideAmmoLoadPopupInternal();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitHotCross EnsureHost()
        {
            if (instance != null)
            {
                // Builder rule: keep host active in hierarchy; visibility is C# SetShown only.
                if (!instance.gameObject.activeSelf)
                    instance.gameObject.SetActive(true);
                if (instance.document != null && !instance.document.enabled)
                    instance.document.enabled = true;
                return instance;
            }

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.HotCrossName,
                DMUiToolkitOverlayDocument.HotCrossUxml,
                DMUiToolkitOverlayDocument.HotCrossUss,
                DMUiToolkitOverlayDocument.HotCrossSort);
            if (doc == null)
                return null;

            if (!doc.gameObject.activeSelf)
                doc.gameObject.SetActive(true);
            if (!doc.enabled)
                doc.enabled = true;

            DMUiToolkitHotCross host = doc.GetComponent<DMUiToolkitHotCross>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitHotCross>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        /// <summary>Pointer hit-test for drag/drop (panel coords).</summary>
        public static bool IsPointerOverPanel(Vector2 panelPos)
        {
            if (instance == null || instance.cross == null)
                return false;
            if (instance.cross.resolvedStyle.display == DisplayStyle.None)
                return false;
            return instance.cross.worldBound.Contains(panelPos);
        }

        public static bool IsPointerOver(Vector2 screenPosition)
        {
            if (instance == null || instance.cross == null)
                return false;
            if (instance.cross.resolvedStyle.display == DisplayStyle.None)
                return false;

            VisualElement panelRoot = instance.document != null ? instance.document.rootVisualElement : null;
            if (panelRoot?.panel == null)
                return false;

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panelRoot.panel, screenPosition);
            return instance.cross.worldBound.Contains(panelPos);
        }

        public static void NotifyToolFace(ToolFace face)
        {
            toolFace = face;
            instance?.MarkVisualsDirty();
        }

        public static void NotifyConsumableLocalIndex(int localIndex)
        {
            if (localIndex < ConsumableFirstLocal || localIndex > ConsumableLastLocal)
                return;
            consumableLocalIndex = localIndex;
            instance?.MarkVisualsDirty();
        }

        public static void NotifyWeaponLocalIndex(int localIndex)
        {
            if (localIndex < 0 || localIndex >= WeaponSlotCount)
                return;
            weaponLocalIndex = localIndex;
            instance?.MarkVisualsDirty();
        }

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
            BindInventoryEvents();
        }

        private void OnDisable()
        {
            UnbindInventoryEvents();
        }

        private void OnDestroy()
        {
            UnbindInventoryEvents();
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            bool show = ShouldShow();
            // Only advance lastShown when the visual tree exists — otherwise a null-root
            // show=true would stick lastShown and never SetShown after BindTree succeeds.
            if (root != null)
            {
                if (show != lastShown)
                {
                    lastShown = show;
                    DMUiToolkitOverlayDocument.SetShown(root, show);
                    if (show)
                        visualsDirty = true;
                }
                else if (show
                    && root.resolvedStyle.display == DisplayStyle.None)
                {
                    // External hide / recovery left display:none without updating lastShown.
                    DMUiToolkitOverlayDocument.SetShown(root, true);
                    visualsDirty = true;
                }
            }

            if (!show)
            {
                if (ammoPopupOpen)
                    HideAmmoLoadPopupInternal();
                return;
            }

            if (inventory == null)
            {
                BindInventoryEvents();
                visualsDirty = true;
            }

            if (visualsDirty)
                Refresh();

            DMUiToolkitOverlayDocument.SetShown(ammoPopup, ammoPopupOpen);
        }

        private void MarkVisualsDirty()
        {
            visualsDirty = true;
        }

        private bool ShouldShow()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return false;
            if (!GameSession.HasStarted)
                return false;
            if (MainMenuController.BlocksGameplayHud)
                return false;
            if (DMUiToolkitLoadingOverlay.IsShowing || DMUiToolkitMainMenu.IsVisible)
                return false;
            if (GameplayHudVisibility.CinematicChromeHidden)
                return false;
            return true;
        }

        private void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("hot-cross-root") ?? tree;
            cross = tree.Q<VisualElement>("hot-cross");
            quadTl = tree.Q<VisualElement>("hot-cross-q-tl");
            quadTr = tree.Q<VisualElement>("hot-cross-q-tr");
            quadBl = tree.Q<VisualElement>("hot-cross-q-bl");
            iconTl = tree.Q<VisualElement>("hot-cross-icon-tl");
            iconTr = tree.Q<VisualElement>("hot-cross-icon-tr");
            iconBl = tree.Q<VisualElement>("hot-cross-icon-bl");
            glowTl = tree.Q<VisualElement>("hot-cross-glow-tl");
            glowTr = tree.Q<VisualElement>("hot-cross-glow-tr");
            glowBl = tree.Q<VisualElement>("hot-cross-glow-bl");
            if (iconRegistry == null)
                iconRegistry = DMHotCrossIconRegistry.LoadDefault();
            amtTl = tree.Q<Label>("hot-cross-amt-tl");
            amtTr = tree.Q<Label>("hot-cross-amt-tr");
            keyBl = tree.Q<Label>("hot-cross-key-bl");
            clipLabel = tree.Q<Label>("hot-cross-clip");
            ammoPopup = tree.Q<VisualElement>("hot-cross-ammo-popup");
            ammoTitle = tree.Q<Label>("hot-cross-ammo-title");
            ammoHint = tree.Q<Label>("hot-cross-ammo-hint");
            ammoList = tree.Q<VisualElement>("hot-cross-ammo-list");
            // Builder-visible host; runtime starts hidden via C# (never USS display:none).
            DMUiToolkitOverlayDocument.SetShown(ammoPopup, ammoPopupOpen);
            bound = root != null && cross != null;
        }

        private void BindInventoryEvents()
        {
            ResolveInventory();
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= HandleInventoryChanged;
                inventory.OnInventoryChanged += HandleInventoryChanged;
            }

            if (equipment != null)
            {
                equipment.OnSelectedHotbarChanged -= HandleHotbarSelectionChanged;
                equipment.OnSelectedHotbarChanged += HandleHotbarSelectionChanged;
                equipment.OnToolbarSelectionChanged -= HandleToolbarSelectionChanged;
                equipment.OnToolbarSelectionChanged += HandleToolbarSelectionChanged;
            }

            ResolveAmmoState();
            if (ammoState != null)
            {
                ammoState.OnAmmoChanged -= HandleAmmoChanged;
                ammoState.OnAmmoChanged += HandleAmmoChanged;
            }
        }

        private void UnbindInventoryEvents()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= HandleInventoryChanged;
            if (equipment != null)
            {
                equipment.OnSelectedHotbarChanged -= HandleHotbarSelectionChanged;
                equipment.OnToolbarSelectionChanged -= HandleToolbarSelectionChanged;
            }

            if (ammoState != null)
                ammoState.OnAmmoChanged -= HandleAmmoChanged;
        }

        private void HandleInventoryChanged() => MarkVisualsDirty();

        private void HandleAmmoChanged() => RefreshClipCount();

        private void HandleHotbarSelectionChanged(int _) => MarkVisualsDirty();

        private void HandleToolbarSelectionChanged() => MarkVisualsDirty();

        private void ResolveInventory()
        {
            if (inventory == null)
                inventory = FindAnyObjectByType<InventorySystem>(FindObjectsInactive.Include);
            if (inventory != null && equipment == null)
            {
                equipment = inventory.GetComponent<EquipmentController>()
                    ?? inventory.GetComponentInChildren<EquipmentController>(true);
            }

            if (equipment == null)
                equipment = FindAnyObjectByType<EquipmentController>(FindObjectsInactive.Include);

            ResolveAmmoState();
        }

        private void ResolveAmmoState()
        {
            if (ammoState != null)
                return;

            if (equipment != null)
                ammoState = equipment.GetComponent<WeaponAmmoState>();
            if (ammoState == null && inventory != null)
                ammoState = inventory.GetComponent<WeaponAmmoState>();
            if (ammoState == null)
                ammoState = FindAnyObjectByType<WeaponAmmoState>(FindObjectsInactive.Include);
        }

        private void Refresh()
        {
            if (!bound)
                return;

            visualsDirty = false;
            ResolveInventory();
            RefreshWeaponQuadrant();
            RefreshConsumableQuadrant();
            RefreshToolQuadrant();
            RefreshClipCount();
        }

        private void RefreshClipCount()
        {
            if (clipLabel == null)
                return;

            ResolveAmmoState();

            ItemData weapon = null;
            int slot = -1;
            if (equipment != null && equipment.HasActiveRangedWeapon())
            {
                weapon = equipment.DrawnWeaponItem;
                slot = equipment.ActiveWeaponHotbarSlot;
            }
            else if (inventory != null)
            {
                int absolute = inventory.HotbarStartIndex + Mathf.Clamp(weaponLocalIndex, 0, WeaponSlotCount - 1);
                weapon = inventory.GetItemAt(absolute);
                slot = weaponLocalIndex;
            }

            bool show = weapon != null && weapon.IsRangedWeapon && !weapon.isMiningTool;
            if (!show)
            {
                lastClipShown = int.MinValue;
                clipLabel.text = string.Empty;
                DMUiToolkitOverlayDocument.SetShown(clipLabel, false);
                return;
            }

            int loaded = ammoState != null ? ammoState.GetLoadedAmmo(slot) : 0;
            if (loaded != lastClipShown)
            {
                lastClipShown = loaded;
                clipLabel.text = loaded.ToString();
            }

            AmmoType ammoType = ammoState != null
                ? ammoState.GetLoadedAmmoType(slot)
                : weapon.defaultAmmoType;
            Color color = DMWorldAmmoHud.ResolveAmmoColor(ammoType);
            if (lastClipColor != color)
            {
                lastClipColor = color;
                clipLabel.style.color = color;
            }

            DMUiToolkitOverlayDocument.SetShown(clipLabel, true);
        }

        private void RefreshWeaponQuadrant()
        {
            ItemData item = null;
            int stack = 0;

            if (inventory != null)
            {
                weaponLocalIndex = Mathf.Clamp(weaponLocalIndex, 0, WeaponSlotCount - 1);
                int absolute = inventory.HotbarStartIndex + weaponLocalIndex;
                item = inventory.GetItemAt(absolute);
                stack = GetStackAt(absolute);
            }

            ApplyIcon(iconTl, glowTl, amtTl, item, stack);
            // Independent TL weapon focus (Tab 0-3). Chrome stays on so empty slots remain readable.
            quadTl?.EnableInClassList("hot-cross-quad--selected", true);
        }

        private void RefreshConsumableQuadrant()
        {
            ItemData item = null;
            int stack = 0;

            if (inventory != null)
            {
                consumableLocalIndex = Mathf.Clamp(consumableLocalIndex, ConsumableFirstLocal, ConsumableLastLocal);
                int absolute = inventory.HotbarStartIndex + consumableLocalIndex;
                item = inventory.GetItemAt(absolute);
                stack = GetStackAt(absolute);
            }

            ApplyIcon(iconTr, glowTr, amtTr, item, stack);
            // Independent TR consumable focus (X 4-9). Chrome stays on so empty slots remain readable.
            quadTr?.EnableInClassList("hot-cross-quad--selected", true);
        }

        private void RefreshToolQuadrant()
        {
            ItemData item = null;
            bool selected = false;

            if (inventory != null && equipment != null)
            {
                // Prefer live toolbar selection; otherwise last B/N face.
                if (equipment.IsToolbarActive && equipment.ActiveToolItem != null)
                {
                    item = equipment.ActiveToolItem;
                    selected = true;
                    if (item.toolType == ToolType.Scanner)
                        toolFace = ToolFace.Scanner;
                    else if (item.toolType == ToolType.Binoculars)
                        toolFace = ToolFace.Binoculars;
                }
                else
                {
                    int toolbarLocal = toolFace == ToolFace.Scanner
                        ? equipment.ScannerToolbarSlot
                        : equipment.BinocularsToolbarSlot;
                    int absolute = inventory.ToolbarStartIndex + toolbarLocal;
                    item = inventory.GetItemAt(absolute);
                }
            }

            ApplyIcon(iconBl, glowBl, null, item, 0);
            if (keyBl != null)
                keyBl.text = toolFace == ToolFace.Scanner ? "N" : "B";
            quadBl?.EnableInClassList("hot-cross-quad--selected", selected);
        }

        private bool ShowAmmoLoadPopupInternal(
            int ammoAbsoluteSlot,
            List<InventoryItemActions.AmmoEquipOption> options,
            int preferredWeaponHotbarSlot,
            System.Action<int> onConfirmWeaponHotbar)
        {
            if (!bound)
                BindTree();
            if (ammoPopup == null || ammoList == null || options == null || options.Count == 0)
                return false;

            ammoPopupAbsoluteSlot = ammoAbsoluteSlot;
            ammoConfirmHandler = onConfirmWeaponHotbar;
            ammoOptions.Clear();
            ammoOptions.AddRange(options);

            ammoHighlightIndex = 0;
            for (int i = 0; i < ammoOptions.Count; i++)
            {
                if (ammoOptions[i].WeaponHotbarSlot == preferredWeaponHotbarSlot)
                {
                    ammoHighlightIndex = i;
                    break;
                }
            }

            ItemData ammo = null;
            if (inventory != null)
                ammo = inventory.GetItemAt(ammoAbsoluteSlot);
            if (ammoTitle != null)
                ammoTitle.text = ammo != null ? $"Load {ammo.itemName} into" : "Load ammo into";

            ammoPopupOpen = true;
            RebuildAmmoList();
            DMUiToolkitOverlayDocument.SetShown(ammoPopup, true);
            ammoPopup.BringToFront();
            return true;
        }

        private void HideAmmoLoadPopupInternal()
        {
            ammoPopupOpen = false;
            ammoPopupAbsoluteSlot = -1;
            ammoConfirmHandler = null;
            ammoOptions.Clear();
            ammoHighlightIndex = 0;
            ammoList?.Clear();
            DMUiToolkitOverlayDocument.SetShown(ammoPopup, false);
        }

        private void RebuildAmmoList()
        {
            if (ammoList == null)
                return;

            ammoList.Clear();
            for (int i = 0; i < ammoOptions.Count; i++)
            {
                int index = i;
                InventoryItemActions.AmmoEquipOption option = ammoOptions[i];
                var row = new VisualElement();
                row.AddToClassList("hot-cross-ammo-row");
                row.EnableInClassList("hot-cross-ammo-row--selected", i == ammoHighlightIndex);
                row.pickingMode = PickingMode.Position;

                var label = new Label(option.WeaponLabel);
                label.AddToClassList("hot-cross-ammo-row-label");
                label.pickingMode = PickingMode.Ignore;
                row.Add(label);

                row.RegisterCallback<ClickEvent>(_ =>
                {
                    ammoHighlightIndex = index;
                    RebuildAmmoList();
                    System.Action<int> handler = ammoConfirmHandler;
                    int weaponSlot = option.WeaponHotbarSlot;
                    HideAmmoLoadPopupInternal();
                    handler?.Invoke(weaponSlot);
                });

                ammoList.Add(row);
            }
        }

        private int GetStackAt(int absolute)
        {
            if (inventory == null || absolute < 0 || absolute >= inventory.slots.Count)
                return 0;
            InventorySystem.InventorySlot slot = inventory.slots[absolute];
            return slot != null && !slot.IsEmpty ? slot.amount : 0;
        }

        private void ApplyIcon(VisualElement icon, VisualElement glow, Label amount, ItemData item, int stack)
        {
            if (icon == null)
                return;

            Sprite sprite = null;
            Color tint = Color.white;
            Color emissionColor = Color.white;
            float emission = 0f;
            if (iconRegistry != null)
                iconRegistry.TryResolve(item, out sprite, out tint, out emissionColor, out emission);
            else
                sprite = DMHotCrossIconRegistry.FindCutout(item);

            if (sprite != null && DMUiToolkitStyle.TrySetSpriteBackground(icon, sprite, ScaleMode.ScaleToFit))
            {
                icon.style.unityBackgroundImageTintColor = tint;
                icon.style.opacity = 1f;
                DMUiToolkitOverlayDocument.SetShown(icon, true);
            }
            else
            {
                DMUiToolkitStyle.ClearBackgroundImage(icon);
                icon.style.unityBackgroundImageTintColor = StyleKeyword.Null;
                DMUiToolkitOverlayDocument.SetShown(icon, false);
            }

            ApplyGlow(glow, sprite, emissionColor, emission);

            if (amount != null)
                amount.text = item != null && stack > 1 ? stack.ToString() : string.Empty;
        }

        private static void ApplyGlow(VisualElement glow, Sprite sprite, Color emissionColor, float emission)
        {
            if (glow == null)
                return;

            if (sprite == null || emission <= 0.01f)
            {
                DMUiToolkitStyle.ClearBackgroundImage(glow);
                glow.style.opacity = 0f;
                DMUiToolkitOverlayDocument.SetShown(glow, false);
                return;
            }

            if (!DMUiToolkitStyle.TrySetSpriteBackground(glow, sprite, ScaleMode.ScaleToFit))
            {
                glow.style.opacity = 0f;
                DMUiToolkitOverlayDocument.SetShown(glow, false);
                return;
            }

            Color glowTint = emissionColor;
            glowTint.a = 1f;
            glow.style.unityBackgroundImageTintColor = glowTint;
            glow.style.opacity = Mathf.Clamp01(emission * 0.35f);
            DMUiToolkitOverlayDocument.SetShown(glow, true);
        }
    }
}
