using System.Collections.Generic;
using Project.Combat;
using Project.Data;
using Project.Core;
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
        private Label loadedAmmoLabel;
        private VisualElement loadedAmmoIcon;
        private VisualElement loadedAmmoRow;
        private WeaponAmmoState ammoState;
        private int lastClipShown = int.MinValue;
        private string lastAmmoNameShown;
        private Color lastClipColor;
        private bool lastAmmoNameGlowValid;
        private Color lastAmmoNameGlowColor;
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
        /// Optional confirm UI for ammo load. Weapon list is skipped — only the preferred/active ranged slot is used.
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
            if (bound && IsTreeStale())
            {
                // EnsureHost() re-enabling the UIDocument (or a tree reload) rebuilds the visual tree while this
                // host stays enabled, so OnEnable never re-binds: icons were then drawn into detached elements
                // and the visible slot stayed blank. Drop the stale refs and bind the live tree.
                bound = false;
                lastShown = !ShouldShow(); // force SetShown on the rebuilt root next pass
                visualsDirty = true;
            }

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

            if (inventory == null || ammoState == null)
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
            return DMUiToolkitOverlayDocument.GameplayHudWanted()
                && !DMUiToolkitMainMenu.IsVisible
                && !GameplayHudVisibility.CinematicChromeHidden;
        }

        private bool IsTreeStale()
        {
            return root == null
                || root.panel == null
                || (iconTl != null && iconTl.panel == null)
                || (iconTr != null && iconTr.panel == null)
                || (iconBl != null && iconBl.panel == null);
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
            loadedAmmoLabel = tree.Q<Label>("hot-cross-loaded-ammo");
            loadedAmmoIcon = tree.Q<VisualElement>("hot-cross-loaded-icon");
            loadedAmmoRow = tree.Q<VisualElement>("hot-cross-loaded");
            if (clipLabel != null)
                clipLabel.text = string.Empty;

            if (loadedAmmoLabel != null)
            {
                loadedAmmoLabel.text = string.Empty;
                lastClipShown = int.MinValue;
                lastAmmoNameShown = null;
                lastAmmoNameGlowValid = false;
            }

            if (loadedAmmoIcon != null)
            {
                DMUiToolkitStyle.ClearBackgroundImage(loadedAmmoIcon);
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoIcon, false);
            }

            if (loadedAmmoRow != null)
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoRow, false);
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

        // Mag load/unload also changes whether the TR ghost should show.
        private void HandleAmmoChanged() => MarkVisualsDirty();

        private void HandleHotbarSelectionChanged(int _) => MarkVisualsDirty();

        private void HandleToolbarSelectionChanged() => MarkVisualsDirty();

        private void ResolveInventory()
        {
            if (!PlayerLocator.IsLive(inventory))
                inventory = PlayerLocator.FindLiveInventory();
            if (inventory != null && !PlayerLocator.IsLive(equipment))
            {
                equipment = inventory.GetComponent<EquipmentController>()
                    ?? inventory.GetComponentInChildren<EquipmentController>();
            }

            if (!PlayerLocator.IsLive(equipment))
                equipment = PlayerLocator.FindLiveEquipment();

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
                ammoState = PlayerLocator.FindOnLivePlayer<WeaponAmmoState>();
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
            if (clipLabel == null && loadedAmmoLabel == null && loadedAmmoIcon == null)
                return;

            ResolveAmmoState();

            bool show = equipment != null && equipment.HasActiveRangedWeapon();
            ItemData weapon = show ? equipment.DrawnWeaponItem : null;
            if (show)
                show = weapon != null && weapon.IsRangedWeapon && !weapon.isMiningTool;

            if (!show)
            {
                lastClipShown = int.MinValue;
                lastAmmoNameShown = null;
                lastAmmoNameGlowValid = false;
                if (clipLabel != null)
                {
                    clipLabel.text = string.Empty;
                    DMUiToolkitOverlayDocument.SetShown(clipLabel, false);
                }

                HideLoadedAmmoRow();
                return;
            }

            int slot = equipment.ActiveWeaponHotbarSlot;
            int loaded = ammoState != null ? ammoState.GetActiveLoadedAmmo() : 0;
            ItemData loadedItem = ammoState != null ? ammoState.GetLoadedAmmoItem(slot) : null;
            AmmoType ammoType = ammoState != null
                ? ammoState.GetLoadedAmmoType(slot)
                : weapon.defaultAmmoType;
            Color color = DMWorldAmmoHud.ResolveAmmoColor(ammoType);
            string ammoName = ResolveLoadedAmmoDisplayName(slot, weapon, ammoState);
            bool colorChanged = lastClipColor != color;
            if (colorChanged)
                lastClipColor = color;

            if (clipLabel != null)
            {
                if (loaded != lastClipShown)
                {
                    lastClipShown = loaded;
                    clipLabel.text = loaded.ToString();
                }

                if (colorChanged)
                    clipLabel.style.color = color;

                DMUiToolkitOverlayDocument.SetShown(clipLabel, true);
            }

            // Always show the magazine's ammo identity (cutout + name) while a ranged weapon is drawn.
            // stamp: hotcross-loaded-persist 1006
            bool showLoadedIdentity = loaded > 0 || loadedItem != null;
            if (loadedAmmoRow != null)
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoRow, showLoadedIdentity);

            if (loadedAmmoLabel != null)
            {
                if (!string.Equals(ammoName, lastAmmoNameShown, System.StringComparison.Ordinal))
                {
                    lastAmmoNameShown = ammoName;
                    loadedAmmoLabel.text = ammoName;
                }

                if (colorChanged)
                    loadedAmmoLabel.style.color = color;

                if (!lastAmmoNameGlowValid || lastAmmoNameGlowColor != color)
                {
                    lastAmmoNameGlowColor = color;
                    lastAmmoNameGlowValid = true;
                    loadedAmmoLabel.style.textShadow = new TextShadow
                    {
                        offset = Vector2.zero,
                        blurRadius = 14f,
                        color = new Color(color.r, color.g, color.b, 0.9f)
                    };
                }

                DMUiToolkitOverlayDocument.SetShown(loadedAmmoLabel, showLoadedIdentity);
            }

            ApplyLoadedAmmoIcon(loadedItem, showLoadedIdentity);
        }

        private void HideLoadedAmmoRow()
        {
            if (loadedAmmoLabel != null)
            {
                loadedAmmoLabel.text = string.Empty;
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoLabel, false);
            }

            if (loadedAmmoIcon != null)
            {
                DMUiToolkitStyle.ClearBackgroundImage(loadedAmmoIcon);
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoIcon, false);
            }

            if (loadedAmmoRow != null)
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoRow, false);
        }

        private void ApplyLoadedAmmoIcon(ItemData ammoItem, bool show)
        {
            if (loadedAmmoIcon == null)
                return;

            if (!show || ammoItem == null)
            {
                DMUiToolkitStyle.ClearBackgroundImage(loadedAmmoIcon);
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoIcon, false);
                return;
            }

            // Ammo cutout only — never the drawn weapon's icon.
            Sprite sprite = null;
            Color tint = Color.white;
            Color emissionColor = Color.white;
            float emission = 0f;
            if (iconRegistry == null)
                iconRegistry = DMHotCrossIconRegistry.LoadDefault();
            if (iconRegistry != null)
                iconRegistry.TryResolve(ammoItem, out sprite, out tint, out emissionColor, out emission);
            if (sprite == null)
                sprite = DMHotCrossIconRegistry.FindCutout(ammoItem);
            if (sprite == null)
                sprite = ammoItem.icon;

            bool applied = sprite != null
                && DMUiToolkitStyle.TrySetSpriteBackground(loadedAmmoIcon, sprite, ScaleMode.ScaleToFit);
            if (!applied)
            {
                Sprite placeholder = GetPlaceholderIcon();
                applied = DMUiToolkitStyle.TrySetSpriteBackground(loadedAmmoIcon, placeholder, ScaleMode.ScaleToFit);
                tint = Color.white;
            }

            if (applied)
            {
                loadedAmmoIcon.style.unityBackgroundImageTintColor = tint;
                loadedAmmoIcon.style.opacity = 1f;
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoIcon, true);
            }
            else
            {
                DMUiToolkitStyle.ClearBackgroundImage(loadedAmmoIcon);
                DMUiToolkitOverlayDocument.SetShown(loadedAmmoIcon, false);
            }
        }

        private static string ResolveLoadedAmmoDisplayName(int weaponHotbarSlot, ItemData weapon, WeaponAmmoState state)
        {
            if (state != null)
            {
                ItemData loadedAmmoItem = state.GetLoadedAmmoItem(weaponHotbarSlot);
                if (loadedAmmoItem != null && !string.IsNullOrWhiteSpace(loadedAmmoItem.itemName))
                    return loadedAmmoItem.itemName;

                return FormatAmmoTypeName(state.GetLoadedAmmoType(weaponHotbarSlot));
            }

            return weapon != null ? FormatAmmoTypeName(weapon.defaultAmmoType) : "Standard";
        }

        private static string FormatAmmoTypeName(AmmoType type)
        {
            if (type == AmmoType.Gunpowder)
                return "Standard";

            return type.ToString();
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
            bool ghost = false;

            if (inventory != null)
            {
                consumableLocalIndex = Mathf.Clamp(consumableLocalIndex, ConsumableFirstLocal, ConsumableLastLocal);
                int absolute = inventory.HotbarStartIndex + consumableLocalIndex;
                item = inventory.GetItemAt(absolute);
                stack = GetStackAt(absolute);

                // Stack fully loaded into the magazine: keep showing that ammo as a ghost on TR so
                // X cycling still has a visible identity. Prefer the slot that sourced the mag;
                // otherwise any empty focus when that ammo has 0 inventory left.
                // stamp: hotcross-mag-ghost 1006
                if ((item == null || stack <= 0)
                    && TryGetMagazineGhostAmmo(out ItemData magAmmo, out int magCount, out int sourceAbsolute))
                {
                    bool ownsSlot = sourceAbsolute == absolute;
                    bool allInMag = inventory.CountItem(magAmmo) <= 0;
                    if (ownsSlot || allInMag)
                    {
                        item = magAmmo;
                        stack = magCount;
                        ghost = true;
                    }
                }
            }

            ApplyIcon(iconTr, glowTr, amtTr, item, stack, ghost);
            // Independent TR consumable focus (X 4-9). Chrome stays on so empty slots remain readable.
            quadTr?.EnableInClassList("hot-cross-quad--selected", true);
            quadTr?.EnableInClassList("hot-cross-quad--ghost", ghost);
        }

        /// <summary>
        /// Magazine ammo to ghost onto an empty Hot Cross utility focus when its stack was fully loaded.
        /// </summary>
        private bool TryGetMagazineGhostAmmo(out ItemData ammoItem, out int loadedCount, out int sourceAbsolute)
        {
            ammoItem = null;
            loadedCount = 0;
            sourceAbsolute = -1;

            ResolveAmmoState();
            if (equipment == null || ammoState == null || !equipment.HasActiveRangedWeapon())
                return false;

            ItemData weapon = equipment.DrawnWeaponItem;
            if (weapon == null || !weapon.IsRangedWeapon || weapon.isMiningTool)
                return false;

            int weaponSlot = equipment.ActiveWeaponHotbarSlot;
            loadedCount = ammoState.GetLoadedAmmo(weaponSlot);
            if (loadedCount <= 0)
                return false;

            ammoItem = ammoState.GetLoadedAmmoItem(weaponSlot);
            if (ammoItem == null || !ammoItem.CountsAsAmmo)
                return false;

            sourceAbsolute = ammoState.GetLoadedAmmoSourceSlot(weaponSlot);
            return true;
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

            ResolveInventory();
            ammoPopupAbsoluteSlot = ammoAbsoluteSlot;
            ammoConfirmHandler = onConfirmWeaponHotbar;
            ammoOptions.Clear();

            int targetWeapon = preferredWeaponHotbarSlot;
            for (int i = 0; i < options.Count; i++)
            {
                if (targetWeapon < 0 || options[i].WeaponHotbarSlot == targetWeapon)
                    ammoOptions.Add(options[i]);
            }

            if (ammoOptions.Count == 0)
                return false;

            ammoHighlightIndex = 0;

            ItemData ammo = null;
            if (inventory != null)
                ammo = inventory.GetItemAt(ammoAbsoluteSlot);
            ItemData weapon = equipment != null && targetWeapon >= 0
                ? equipment.GetHotbarItem(targetWeapon)
                : null;
            string weaponName = weapon != null ? weapon.itemName : "drawn weapon";
            if (ammoTitle != null)
                ammoTitle.text = ammo != null ? $"Load {ammo.itemName} into {weaponName}" : $"Load ammo into {weaponName}";

            if (ammoHint != null)
                ammoHint.text = "Hold confirm  |  Esc cancel";

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

        private static Sprite placeholderIcon;

        /// <summary>Generic framed-square icon for an occupied slot whose item has no cutout and no icon.</summary>
        private static Sprite GetPlaceholderIcon()
        {
            if (placeholderIcon != null)
                return placeholderIcon;

            const int size = 64;
            const int border = 6;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DMHotCrossPlaceholderIcon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[size * size];
            var edge = new Color32(235, 240, 255, 235);
            var fill = new Color32(235, 240, 255, 60);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onEdge = x < border || y < border || x >= size - border || y >= size - border;
                    pixels[y * size + x] = onEdge ? edge : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            placeholderIcon = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            placeholderIcon.name = "DMHotCrossPlaceholderIcon";
            placeholderIcon.hideFlags = HideFlags.DontSave;
            return placeholderIcon;
        }

        private void ApplyIcon(VisualElement icon, VisualElement glow, Label amount, ItemData item, int stack, bool ghost = false)
        {
            if (icon == null)
                return;

            Sprite sprite = null;
            Color tint = Color.white;
            Color emissionColor = Color.white;
            float emission = 0f;
            if (iconRegistry == null)
                iconRegistry = DMHotCrossIconRegistry.LoadDefault();
            if (iconRegistry != null)
                iconRegistry.TryResolve(item, out sprite, out tint, out emissionColor, out emission);
            if (sprite == null)
                sprite = DMHotCrossIconRegistry.FindCutout(item);

            bool applied = sprite != null && DMUiToolkitStyle.TrySetSpriteBackground(icon, sprite, ScaleMode.ScaleToFit);
            if (!applied && item != null)
            {
                // Occupied slot is never blank: Hot Cross cutout -> the item's own icon -> generic placeholder.
                // Ammo skips the game-icon registry's fuzzy name match (it can land on a weapon picture).
                // stamp: hotcross-icon-fallback 1006b
                Sprite fallback = item.CountsAsAmmo ? item.icon : DMGameIconRegistry.FindIcon(item);
                if (fallback == null || fallback == sprite)
                    fallback = item.icon;
                if (fallback != null && fallback != sprite
                    && DMUiToolkitStyle.TrySetSpriteBackground(icon, fallback, ScaleMode.ScaleToFit))
                {
                    sprite = fallback;
                    applied = true;
                }
            }

            if (!applied && item != null)
            {
                Sprite placeholder = GetPlaceholderIcon();
                if (DMUiToolkitStyle.TrySetSpriteBackground(icon, placeholder, ScaleMode.ScaleToFit))
                {
                    sprite = placeholder;
                    applied = true;
                    tint = Color.white;
                    emission = 0f;
                }
            }

            icon.EnableInClassList("hot-cross-icon--ghost", ghost && applied);
            if (applied)
            {
                icon.style.unityBackgroundImageTintColor = tint;
                icon.style.opacity = ghost ? 0.55f : 1f;
                DMUiToolkitOverlayDocument.SetShown(icon, true);
            }
            else
            {
                DMUiToolkitStyle.ClearBackgroundImage(icon);
                icon.style.unityBackgroundImageTintColor = StyleKeyword.Null;
                icon.style.opacity = 1f;
                DMUiToolkitOverlayDocument.SetShown(icon, false);
            }

            ApplyGlow(glow, sprite, emissionColor, ghost ? emission * 0.5f : emission);

            if (amount != null)
            {
                amount.EnableInClassList("hot-cross-amt--ghost", ghost && stack > 0);
                // Ghost shows magazine count even for a single round so the player sees what's in the mag.
                if (ghost && item != null && stack > 0)
                    amount.text = stack.ToString();
                else
                    amount.text = item != null && stack > 1 ? stack.ToString() : string.Empty;
            }
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
