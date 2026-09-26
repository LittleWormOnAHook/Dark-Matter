using System.Collections.Generic;
using Project.Building;
using Project.Inventory;
using Project.Storage;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Build-mode hotbar on the gameplay HUD. Ten slots, arrow scroll, no scrollbar.
    /// The up panel lists Stone and known buildings.
    /// </summary>
    public sealed class DMUiToolkitBuildingHotbar : MonoBehaviour
    {
        const int VisibleSlots = 10;

        static DMUiToolkitBuildingHotbar instance;

        VisualElement bar;
        ScrollView list;
        readonly List<System.Action> menuActions = new List<System.Action>();
        readonly List<Button> menuButtons = new List<Button>();
        int menuFocus;
        int selectedMenuRow = -1;
        bool menuWasOpen;
        Button materialsButton;
        Button scrollLeft;
        Button scrollRight;
        VisualElement holdRing;
        VisualElement crosshairDot;
        Label costLabel;
        InventorySystem inventoryHook;
        bool wheelStopped;
        bool ringPainted;
        readonly Label[] slotLabels = new Label[VisibleSlots];
        readonly VisualElement[] slots = new VisualElement[VisibleSlots];

        public static void Bind(DMUiToolkitHud hud, VisualElement hudRoot)
        {
            if (hud == null || hudRoot == null)
                return;

            DMUiToolkitBuildingHotbar hotbar = hud.GetComponent<DMUiToolkitBuildingHotbar>();
            if (hotbar == null)
                hotbar = hud.gameObject.AddComponent<DMUiToolkitBuildingHotbar>();
            hotbar.BindTree(hudRoot);
        }

        bool bound;

        void BindTree(VisualElement hudRoot)
        {
            instance = this;
            bar = hudRoot.Q<VisualElement>("build-bar");
            list = hudRoot.Q<ScrollView>("build-list");
            if (list != null)
            {
                list.mode = ScrollViewMode.Vertical;
                list.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                list.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                list.mouseWheelScrollSize = 32f;
            }
            materialsButton = hudRoot.Q<Button>("build-materials");
            scrollLeft = hudRoot.Q<Button>("build-scroll-left");
            scrollRight = hudRoot.Q<Button>("build-scroll-right");
            holdRing = hudRoot.Q<VisualElement>("build-hold-ring");
            costLabel = hudRoot.Q<Label>("build-cost");
            if (costLabel != null)
                costLabel.enableRichText = true;
            if (!wheelStopped)
            {
                hudRoot.RegisterCallback<WheelEvent>(OnBuildWheel, TrickleDown.TrickleDown);
                wheelStopped = true;
            }
            if (holdRing != null && !ringPainted)
            {
                holdRing.generateVisualContent += PaintHoldRing;
                holdRing.pickingMode = PickingMode.Ignore;
                ringPainted = true;
            }

            if (holdRing != null)
                holdRing.style.display = DisplayStyle.None;

            // 0925-dot: small centre dot while building so the snap target is easy to read.
            crosshairDot?.RemoveFromHierarchy();
            crosshairDot = new VisualElement { name = "build-crosshair-dot", pickingMode = PickingMode.Ignore };
            crosshairDot.style.position = Position.Absolute;
            crosshairDot.style.left = Length.Percent(50f);
            crosshairDot.style.top = Length.Percent(50f);
            crosshairDot.style.display = DisplayStyle.None;
            hudRoot.Add(crosshairDot);

            for (int i = 0; i < VisibleSlots; i++)
            {
                slots[i] = hudRoot.Q<VisualElement>("build-slot-" + i);
                slotLabels[i] = hudRoot.Q<Label>("build-slot-label-" + i);
            }

            if (!bound)
            {
                for (int i = 0; i < VisibleSlots; i++)
                {
                    int captured = i;
                    if (slots[i] != null)
                    {
                        slots[i].pickingMode = PickingMode.Position;
                        slots[i].RegisterCallback<ClickEvent>(_ => OnSlotClicked(captured));
                    }
                }

                if (materialsButton != null)
                {
                    materialsButton.pickingMode = PickingMode.Position;
                    materialsButton.focusable = false;
                    materialsButton.clicked += DMBuildingMode.ToggleMaterials;
                }

                if (scrollLeft != null)
                {
                    scrollLeft.pickingMode = PickingMode.Position;
                    scrollLeft.focusable = false;
                    scrollLeft.clicked += () => DMBuildingMode.StepSelection(-1);
                }

                if (scrollRight != null)
                {
                    scrollRight.pickingMode = PickingMode.Position;
                    scrollRight.focusable = false;
                    scrollRight.clicked += () => DMBuildingMode.StepSelection(1);
                }

                DMBuildingMode.Changed += Refresh;
                DMStorageCrateRuntime.StatesChanged += OnInventoryChanged;
                bound = true;
            }

            Refresh();
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
            DMBuildingMode.Changed -= Refresh;
            DMStorageCrateRuntime.StatesChanged -= OnInventoryChanged;
            if (inventoryHook != null)
                inventoryHook.OnInventoryChanged -= OnInventoryChanged;
        }

        void Update()
        {
            if (bar == null)
                return;

            bool show = BarVisible();
            bar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show && holdRing != null)
                holdRing.style.display = DisplayStyle.None;
            UpdateCrosshairDot(show);
            if (show)
            {
                HookInventory();
                PollSelectionKeys();
                PollHotbarWheel();
            }
        }

        void UpdateCrosshairDot(bool show)
        {
            if (crosshairDot == null)
                return;

            float size = DMBuildingGhostProfile.CrosshairDotPixels;
            if (!show || !DMBuildingMode.IsActive || size <= 0f)
            {
                crosshairDot.style.display = DisplayStyle.None;
                return;
            }

            float half = size * 0.5f;
            IStyle s = crosshairDot.style;
            s.width = size;
            s.height = size;
            s.marginLeft = -half;
            s.marginTop = -half;
            s.borderTopLeftRadius = half;
            s.borderTopRightRadius = half;
            s.borderBottomLeftRadius = half;
            s.borderBottomRightRadius = half;
            s.backgroundColor = DMBuildingGhostProfile.CrosshairDotColor;
            Color outline = DMBuildingGhostProfile.CrosshairDotOutline;
            s.borderTopWidth = 1f;
            s.borderBottomWidth = 1f;
            s.borderLeftWidth = 1f;
            s.borderRightWidth = 1f;
            s.borderTopColor = outline;
            s.borderBottomColor = outline;
            s.borderLeftColor = outline;
            s.borderRightColor = outline;
            s.display = DisplayStyle.Flex;
        }

        static void OnBuildWheel(WheelEvent evt)
        {
            if (!DMBuildingMode.IsActive)
                return;

            ApplyHotbarWheel(evt.delta.y);
            evt.StopImmediatePropagation();
        }

        static void PollHotbarWheel()
        {
            if (Mouse.current == null)
                return;

            ApplyHotbarWheel(Mouse.current.scroll.ReadValue().y);
        }

        static int wheelFrame = -1;

        static void ApplyHotbarWheel(float raw)
        {
            if (Mathf.Abs(raw) < 0.01f)
                return;
            if (Time.frameCount == wheelFrame)
                return;

            bool shift = Keyboard.current != null
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            bool alt = Keyboard.current != null
                && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
            if (shift || alt) // 0925-rotate: Shift = height, Alt = rotate
                return;

            int notches = Mathf.RoundToInt(raw / 120f);
            if (notches == 0)
                notches = raw > 0f ? 1 : -1;

            wheelFrame = Time.frameCount;
            if (DMBuildingMode.MaterialsOpen)
                NudgeMenu(-notches);
            else
                DMBuildingMode.StepSelection(-notches);
        }

        static int activateFrame = -1;

        /// <summary>
        /// E only confirms the Tab / ▲ popup. Wheel auto-selects hotbar pieces.
        /// </summary>
        public static bool TryActivateFocused()
        {
            if (instance == null || !DMBuildingMode.IsActive || !BarVisible() || !DMBuildingMode.MaterialsOpen)
                return false;
            if (Time.frameCount == activateFrame)
                return true;

            activateFrame = Time.frameCount;
            if (instance.menuActions.Count == 0)
                return true;

            int index = Mathf.Clamp(instance.menuFocus, 0, instance.menuActions.Count - 1);
            instance.menuActions[index]?.Invoke();
            return true;
        }

        public static void NudgeMenu(int direction)
        {
            if (instance == null || direction == 0 || !DMBuildingMode.MaterialsOpen)
                return;

            instance.StepMenuFocus(direction);
        }

        void HookInventory()
        {
            InventorySystem found = UnityEngine.Object.FindAnyObjectByType<InventorySystem>();
            if (found == inventoryHook)
                return;

            if (inventoryHook != null)
                inventoryHook.OnInventoryChanged -= OnInventoryChanged;

            inventoryHook = found;
            if (inventoryHook != null)
                inventoryHook.OnInventoryChanged += OnInventoryChanged;
        }

        void OnInventoryChanged()
        {
            PaintSlots();
            PaintCost();
        }

        static bool BarVisible()
        {
            return DMBuildingMode.IsActive
                && !GameplayHudVisibility.CinematicChromeHidden
                && !DMUiToolkitMenus.IsOpen;
        }

        static void PollSelectionKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (instance == null || !DMBuildingMode.MaterialsOpen)
                return;

            if (keyboard.upArrowKey.wasPressedThisFrame)
                instance.StepMenuFocus(-1);
            else if (keyboard.downArrowKey.wasPressedThisFrame)
                instance.StepMenuFocus(1);
        }

        public static bool PointerOverBar()
        {
            if (instance == null || instance.bar == null || instance.bar.panel == null || Mouse.current == null)
                return false;
            if (instance.bar.resolvedStyle.display == DisplayStyle.None)
                return false;

            Vector2 screen = Mouse.current.position.ReadValue();
            Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(instance.bar.panel, screen);
            return instance.bar.worldBound.Contains(panelPoint);
        }

        public static void SetHoldRing(bool visible, float progress, Vector3 worldPoint)
        {
            if (instance == null || instance.holdRing == null)
                return;

            if (!visible || !BarVisible())
            {
                instance.holdRing.style.display = DisplayStyle.None;
                return;
            }

            instance.holdRing.style.left = Length.Percent(50f);
            instance.holdRing.style.top = Length.Percent(50f);
            instance.holdRing.style.width = 64f;
            instance.holdRing.style.height = 64f;
            instance.holdRing.style.marginLeft = -32f;
            instance.holdRing.style.marginTop = -32f;
            instance.holdRing.userData = Mathf.Clamp01(progress);
            instance.holdRing.style.display = DisplayStyle.Flex;
            instance.holdRing.MarkDirtyRepaint();
        }

        static void PaintHoldRing(MeshGenerationContext ctx)
        {
            VisualElement element = ctx.visualElement;
            float progress = 0f;
            if (element.userData is float value)
                progress = Mathf.Clamp01(value);

            Rect rect = element.contentRect;
            if (rect.width < 2f || rect.height < 2f)
                return;

            Vector2 center = new Vector2(rect.width * 0.5f, rect.height * 0.5f);
            const float thickness = 6f;
            float radius = (rect.width * 0.5f) - thickness;
            Painter2D painter = ctx.painter2D;
            painter.lineWidth = thickness;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = new Color(1f, 1f, 1f, 0.22f);
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            if (progress <= 0.001f)
                return;

            Color gold = DarkMatterGenesisUiPalette.Gold;
            gold.a = 0.95f;
            painter.strokeColor = gold;
            painter.BeginPath();
            float sweep = 360f * progress;
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + sweep), ArcDirection.Clockwise);
            painter.Stroke();
        }

        void OnSlotClicked(int visibleIndex)
        {
            DMBuildingMode.SelectSlot(DMBuildingMode.WindowStart + visibleIndex);
        }

        void Refresh()
        {
            if (bar == null)
                return;

            bool show = BarVisible();
            bar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (list != null)
                list.style.display = show && DMBuildingMode.MaterialsOpen ? DisplayStyle.Flex : DisplayStyle.None;

            RebuildList();
            PaintSlots();
            PaintCost();
        }

        void PaintCost()
        {
            if (costLabel == null)
                return;

            DMBuildingPiece piece = DMBuildingMode.SelectedPiece;
            if (piece == null || piece.StoneCost <= 0)
            {
                costLabel.text = string.Empty;
                return;
            }

            int remaining = DMBuildingCatalog.CountStone();
            bool shortOnParts = remaining < piece.StoneCost;
            string needed = shortOnParts
                ? $"<color=#4A4A5A>{piece.StoneCost}</color>"
                : piece.StoneCost.ToString();
            costLabel.text = $"Rock {needed} / {remaining}";
        }

        void RebuildList()
        {
            if (list == null)
                return;

            list.Clear();
            menuActions.Clear();
            menuButtons.Clear();
            selectedMenuRow = -1;
            AddHeader("Components");
            AddRow("Stone", DMBuildingMode.SelectStone, DMBuildingMode.HotbarId == DMBuildingCatalog.StoneId);
            AddHeader("Buildings");

            List<DMBuildingPiece> buildings = DMBuildingCatalog.KnownBuildings();
            for (int i = 0; i < buildings.Count; i++)
            {
                DMBuildingPiece building = buildings[i];
                string id = building.Id;
                AddRow(building.DisplayName, () => DMBuildingMode.SelectBuilding(id), DMBuildingMode.HotbarId == id);
            }

            if (menuActions.Count == 0)
                menuFocus = 0;
            else if (menuFocus >= menuActions.Count)
                menuFocus = menuActions.Count - 1;

            if (DMBuildingMode.MaterialsOpen && !menuWasOpen && selectedMenuRow >= 0)
                menuFocus = selectedMenuRow;
            menuWasOpen = DMBuildingMode.MaterialsOpen;
            for (int i = 0; i < menuButtons.Count; i++)
            {
                bool current = menuButtons[i].userData is bool active && active;
                menuButtons[i].EnableInClassList(
                    "dmg-build-list-row-selected",
                    DMBuildingMode.MaterialsOpen ? i == menuFocus : current);
            }
        }

        void StepMenuFocus(int direction)
        {
            if (menuActions.Count == 0)
                return;

            menuFocus = (menuFocus + direction) % menuActions.Count;
            if (menuFocus < 0)
                menuFocus += menuActions.Count;
            Refresh();
            if (list != null && menuFocus >= 0 && menuFocus < menuButtons.Count)
                list.ScrollTo(menuButtons[menuFocus]);
        }

        void AddHeader(string text)
        {
            var header = new Label(text);
            header.AddToClassList("dmg-build-list-header");
            header.pickingMode = PickingMode.Ignore;
            list.Add(header);
        }

        void AddRow(string text, System.Action onClick, bool selected)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("dmg-build-list-row");
            button.userData = selected;
            button.focusable = false;
            button.pickingMode = PickingMode.Position;
            int index = menuActions.Count;
            menuActions.Add(onClick);
            menuButtons.Add(button);
            if (selected)
                selectedMenuRow = index;
            button.RegisterCallback<PointerEnterEvent>(_ => menuFocus = index);
            list.Add(button);
        }

        void PaintSlots()
        {
            List<DMBuildingPiece> pieces = DMBuildingCatalog.PiecesFor(DMBuildingMode.HotbarId);
            int window = DMBuildingMode.WindowStart;
            for (int i = 0; i < VisibleSlots; i++)
            {
                if (slots[i] == null)
                    continue;

                int index = window + i;
                bool filled = index < pieces.Count;
                DMBuildingPiece piece = filled ? pieces[index] : null;
                if (slotLabels[i] != null)
                    slotLabels[i].text = piece != null ? piece.DisplayName : string.Empty;

                slots[i].EnableInClassList("dmg-build-slot-selected", piece != null && index == DMBuildingMode.SelectedIndex);
                bool affordable = piece != null && DMBuildingCatalog.HasStone(piece.StoneCost);
                slots[i].EnableInClassList("dmg-build-slot-short", piece != null && !affordable);
                slots[i].style.display = DisplayStyle.Flex;
            }

            int count = pieces.Count;
            if (scrollLeft != null)
                scrollLeft.SetEnabled(count > 1);
            if (scrollRight != null)
                scrollRight.SetEnabled(count > 1);
        }
    }
}
