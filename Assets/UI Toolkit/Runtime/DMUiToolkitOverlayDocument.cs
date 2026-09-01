using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Sibling UIDocument hosts for in-world overlays. Same Panel Settings instance as HUD
    /// (no nested document under UITK_Root, no second panel, no clear). Sort order is set on
    /// the UIDocument, not by cloning PanelSettings.
    /// DMUiToolkit 0901-finish
    /// </summary>
    internal static class DMUiToolkitOverlayDocument
    {
        public const string LevelUpName = "UITK_LevelUp";
        public const string DialogueName = "UITK_Dialogue";
        public const string HovercraftName = "UITK_Hovercraft";
        public const string HazardsName = "UITK_Hazards";
        public const string DeathName = "UITK_Death";
        public const string ActiveQuestName = "UITK_ActiveQuest";
        public const string DamageName = "UITK_Damage";
        public const string PickupReticleName = "UITK_PickupReticle";
        public const string HoverInteractName = "UITK_HoverInteract";
        public const string WorldMenusName = "UITK_WorldMenus";
        public const string CraftName = "UITK_Craft";
        public const string AcRewardName = "UITK_AcReward";
        public const string GameStartName = "UITK_GameStart";
        public const string ContextName = "UITK_Context";
        public const string PetChromeName = "UITK_PetChrome";

        /// <summary>Interactive modal overlays must sort above <see cref="DMUiToolkitBootstrap.HudSortingOrder"/> (95).</summary>
        public const int ModalInteractiveSort = 130;

        public const int ActiveQuestSort = 11;
        public const int HovercraftSort = 12;
        public const int HazardsSort = 13;
        public const int DamageSort = 15;
        public const int PickupReticleSort = 16;
        public const int LevelUpSort = ModalInteractiveSort;
        public const int HoverInteractSort = ModalInteractiveSort;
        public const int WorldMenusSort = ModalInteractiveSort;
        public const int CraftSort = ModalInteractiveSort;
        public const int DialogueSort = ModalInteractiveSort;
        public const int AcRewardSort = ModalInteractiveSort + 5;
        public const int DeathSort = ModalInteractiveSort;
        public const int GameStartSort = ModalInteractiveSort + 10;
        public const int ContextSort = 150;
        public const int PetChromeSort = 111;

        public static void PromoteInteractiveOverlay(UIDocument document)
        {
            if (document == null)
                return;

            if (document.sortingOrder < ModalInteractiveSort)
                document.sortingOrder = ModalInteractiveSort;
        }

        public const string LevelUpUxml = "Assets/UI Toolkit/Screens/LevelUp.uxml";
        public const string LevelUpUss = "Assets/UI Toolkit/Screens/LevelUp.uss";
        public const string DialogueUxml = "Assets/UI Toolkit/Screens/DialogueQuest.uxml";
        public const string DialogueUss = "Assets/UI Toolkit/Screens/DialogueQuest.uss";
        public const string HovercraftUxml = "Assets/UI Toolkit/Screens/Hovercraft.uxml";
        public const string HovercraftUss = "Assets/UI Toolkit/Screens/Hovercraft.uss";
        public const string HazardsUxml = "Assets/UI Toolkit/Screens/Hazards.uxml";
        public const string HazardsUss = "Assets/UI Toolkit/Screens/Hazards.uss";
        public const string DeathUxml = "Assets/UI Toolkit/Screens/DeathOverlay.uxml";
        public const string DeathUss = "Assets/UI Toolkit/Screens/DeathOverlay.uss";
        public const string ActiveQuestUxml = "Assets/UI Toolkit/Screens/ActiveQuest.uxml";
        public const string ActiveQuestUss = "Assets/UI Toolkit/Screens/ActiveQuest.uss";
        public const string DamageUxml = "Assets/UI Toolkit/Screens/DamageOverlay.uxml";
        public const string DamageUss = "Assets/UI Toolkit/Screens/DamageOverlay.uss";
        public const string PickupReticleUxml = "Assets/UI Toolkit/Screens/PickupReticle.uxml";
        public const string PickupReticleUss = "Assets/UI Toolkit/Screens/PickupReticle.uss";
        public const string HoverInteractUxml = "Assets/UI Toolkit/Screens/HoverInteract.uxml";
        public const string HoverInteractUss = "Assets/UI Toolkit/Screens/HoverInteract.uss";
        public const string WorldMenusUxml = "Assets/UI Toolkit/Screens/WorldMenus.uxml";
        public const string WorldMenusUss = "Assets/UI Toolkit/Screens/WorldMenus.uss";
        public const string CraftUxml = "Assets/UI Toolkit/Screens/Craft.uxml";
        public const string CraftUss = "Assets/UI Toolkit/Screens/Craft.uss";
        public const string AcRewardUxml = "Assets/UI Toolkit/Screens/AcReward.uxml";
        public const string AcRewardUss = "Assets/UI Toolkit/Screens/AcReward.uss";
        public const string GameStartUxml = "Assets/UI Toolkit/Screens/GameStart.uxml";
        public const string GameStartUss = "Assets/UI Toolkit/Screens/GameStart.uss";
        public const string ContextUxml = "Assets/UI Toolkit/Screens/InventoryContext.uxml";
        public const string ContextUss = "Assets/UI Toolkit/Screens/FloatingMenu.uss";
        public const string PetChromeUxml = "Assets/UI Toolkit/Screens/PetChrome.uxml";
        public const string PetChromeUss = "Assets/UI Toolkit/Screens/FloatingMenu.uss";

        public static UIDocument Ensure(string objectName, string uxmlPath, string ussPath, int sortingOrder)
        {
            if (!Application.isPlaying)
                return null;

            if (!DMUiToolkitConfig.IsEnabled)
                return null;

            DMUiToolkitBootstrap.EnsureExists();
            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            UIDocument hud = bootstrap != null ? bootstrap.HudDocument : null;

            Transform parent = null;
            PanelSettings settings = null;
            if (hud != null)
            {
                parent = hud.transform.parent;
                settings = hud.panelSettings;
            }
            else if (bootstrap != null)
            {
                parent = bootstrap.transform.parent;
                if (bootstrap.ShellDocument != null)
                    settings = bootstrap.ShellDocument.panelSettings;
            }

            GameObject host = FindNamed(objectName);
            if (host == null)
            {
                host = new GameObject(objectName);
                host.transform.SetParent(parent, false);
            }
            else if (bootstrap != null
                && host.transform != bootstrap.transform
                && host.transform.IsChildOf(bootstrap.transform))
            {
                host.transform.SetParent(parent, false);
            }
            else if (parent != null && host.transform.parent != parent && host.transform.parent == null)
            {
                host.transform.SetParent(parent, false);
            }

            UIDocument document = host.GetComponent<UIDocument>();
            if (document == null)
                document = host.AddComponent<UIDocument>();

            if (settings != null && document.panelSettings != settings)
                document.panelSettings = settings;

            document.sortingOrder = sortingOrder;

            VisualTreeAsset tree = DMUiToolkitBootstrap.LoadUxml(uxmlPath);
            if (tree != null && document.visualTreeAsset != tree)
                document.visualTreeAsset = tree;

            DMUiToolkitBootstrap.ApplyTheme(document, DMUiToolkitBootstrap.ThemeUssPath);
            if (!string.IsNullOrEmpty(ussPath))
                DMUiToolkitBootstrap.ApplyTheme(document, ussPath);

            DMUiToolkitHud.StampOverlaysOnce();
            return document;
        }

        public static GameObject FindNamed(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                    return transforms[i].gameObject;
            }

            return null;
        }

        public static bool GameplayHudWanted()
        {
            return DMUiToolkitHud.IsDriving
                && !MainMenuController.BlocksGameplayHud
                && !DMUiToolkitLoadingOverlay.IsShowing;
        }

        public static void SetShown(VisualElement element, bool shown)
        {
            if (element == null)
                return;

            DisplayStyle next = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.resolvedStyle.display == next)
                return;

            element.style.display = next;
        }

        public static void SetFillPercent(VisualElement fill, float normalized)
        {
            if (fill == null)
                return;

            fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }

        public static void HideGameObject(GameObject target)
        {
            if (target != null && target.activeSelf)
                target.SetActive(false);
        }

        public static void HideCanvas(Canvas canvas)
        {
            if (canvas != null && canvas.enabled)
                canvas.enabled = false;
        }

        public static void HideCanvasGroup(CanvasGroup group)
        {
            if (group == null)
                return;

            if (group.alpha > 0f)
                group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public static readonly Vector2 DefaultHoverOffset = new Vector2(18f, -18f);
        public static readonly Vector2 ContextMenuOffset = new Vector2(4f, -4f);
        public static readonly Vector2 ContextMenuPanelOffset = new Vector2(4f, 4f);

        /// <summary>Place a panel at the visual center of its parent (screen center for full-screen hosts).</summary>
        public static void PositionCenterOnScreen(VisualElement element)
        {
            if (element == null)
                return;

            element.style.position = Position.Absolute;
            element.style.left = Length.Percent(50);
            element.style.top = Length.Percent(50);
            element.style.right = StyleKeyword.Auto;
            element.style.bottom = StyleKeyword.Auto;
            element.style.marginLeft = 0;
            element.style.marginTop = 0;
            element.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
        }

        public static void PositionAtScreen(VisualElement element, Vector2 screenPosition)
        {
            PositionNearPointer(element, screenPosition, Vector2.zero);
        }

        /// <summary>Place a floating panel/tooltip beside the pointer (screen space, origin bottom-left).</summary>
        public static void PositionNearPointer(
            VisualElement element,
            Vector2 screenPosition,
            Vector2 screenOffset,
            VisualElement clampRoot = null)
        {
            if (element == null || element.panel == null)
                return;

            element.style.position = Position.Absolute;
            element.style.translate = new Translate(0, 0);
            element.style.right = StyleKeyword.Auto;
            element.style.bottom = StyleKeyword.Auto;
            element.style.marginLeft = 0;
            element.style.marginTop = 0;

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(element.panel, screenPosition + screenOffset);
            VisualElement parent = element.hierarchy.parent;
            if (parent != null)
                panelPos = parent.WorldToLocal(panelPos);

            element.style.left = panelPos.x;
            element.style.top = panelPos.y;

            VisualElement root = clampRoot ?? element.panel.visualTree;
            ScheduleClampFloating(element, root);
        }

        /// <summary>Journal / HUD right-click menus on a separate UIDocument: screen position from Mouse.current.</summary>
        public static void PositionContextMenu(VisualElement menuPanel, Vector2 screenPosition)
        {
            if (menuPanel == null)
                return;

            VisualElement clampRoot = menuPanel.panel != null ? menuPanel.panel.visualTree : null;
            PositionNearPointer(menuPanel, screenPosition, ContextMenuOffset, clampRoot);
        }

        /// <summary>Right-click menus on the same UIDocument/panel as the clicked slot (use PointerEvent.position).</summary>
        public static void PositionContextMenuAtPanel(
            VisualElement menuPanel,
            Vector2 panelPosition,
            VisualElement clampRoot = null)
        {
            if (menuPanel == null || menuPanel.panel == null)
                return;

            menuPanel.style.position = Position.Absolute;
            menuPanel.style.translate = new Translate(0, 0);
            menuPanel.style.right = StyleKeyword.Auto;
            menuPanel.style.bottom = StyleKeyword.Auto;
            menuPanel.style.marginLeft = 0;
            menuPanel.style.marginTop = 0;

            VisualElement parent = menuPanel.hierarchy.parent;
            Vector2 local = parent != null ? parent.WorldToLocal(panelPosition) : panelPosition;
            menuPanel.style.left = local.x + ContextMenuPanelOffset.x;
            menuPanel.style.top = local.y + ContextMenuPanelOffset.y;

            VisualElement root = clampRoot ?? menuPanel.panel.visualTree;
            ScheduleClampFloating(menuPanel, root);
        }

        public static void PositionContextMenuFlyout(
            VisualElement flyout,
            VisualElement anchor,
            VisualElement clampRoot = null,
            VisualElement alignRow = null)
        {
            if (flyout == null || anchor == null)
                return;

            VisualElement parent = flyout.hierarchy.parent;
            VisualElement root = clampRoot ?? flyout.panel?.visualTree;
            VisualElement row = alignRow ?? anchor;

            flyout.style.position = Position.Absolute;
            flyout.style.translate = new Translate(0, 0);
            flyout.style.right = StyleKeyword.Auto;
            flyout.style.bottom = StyleKeyword.Auto;
            flyout.style.marginLeft = 0;
            flyout.style.marginTop = 0;
            flyout.style.left = 0;
            flyout.style.top = 0;

            void Place(GeometryChangedEvent _)
            {
                PlaceFlyout();
            }

            void PlaceFlyout()
            {
                if (flyout.panel == null || anchor.panel == null)
                    return;

                // Flex children often report resolvedStyle.left/top as 0 — use world bounds.
                Rect anchorBounds = anchor.worldBound;
                Rect rowBounds = row != null ? row.worldBound : anchorBounds;
                if (anchorBounds.width <= 1f || rowBounds.height <= 1f)
                {
                    anchor.RegisterCallback<GeometryChangedEvent>(Place);
                    return;
                }

                anchor.UnregisterCallback<GeometryChangedEvent>(Place);

                // Prefer the owning context panel's right edge so the flyout sits beside the menu,
                // not at the overlay origin when button layout is still settling.
                float xMax = anchorBounds.xMax;
                VisualElement menuPanel = FindOwningContextPanel(anchor, parent);
                if (menuPanel != null)
                {
                    Rect panelBounds = menuPanel.worldBound;
                    if (panelBounds.width > 1f)
                        xMax = panelBounds.xMax;
                }

                Vector2 world = new Vector2(xMax + 4f, rowBounds.yMin);
                Vector2 local = parent != null ? parent.WorldToLocal(world) : world;
                flyout.style.left = local.x;
                flyout.style.top = local.y;

                float flyoutW = flyout.worldBound.width;
                float flyoutH = flyout.worldBound.height;
                if (flyoutW <= 1f || flyoutH <= 1f)
                {
                    flyout.RegisterCallback<GeometryChangedEvent>(OnFlyoutGeometry);
                    return;
                }

                if (root != null)
                    ClampFloating(flyout, root, flyoutW, flyoutH);
            }

            void OnFlyoutGeometry(GeometryChangedEvent evt)
            {
                flyout.UnregisterCallback<GeometryChangedEvent>(OnFlyoutGeometry);
                PlaceFlyout();
            }

            PlaceFlyout();
            flyout.schedule.Execute(PlaceFlyout).ExecuteLater(0);
            flyout.schedule.Execute(PlaceFlyout).ExecuteLater(1);
            flyout.schedule.Execute(PlaceFlyout).ExecuteLater(16);
        }

        private static VisualElement FindOwningContextPanel(VisualElement anchor, VisualElement stopAt)
        {
            VisualElement walk = anchor;
            while (walk != null && walk != stopAt)
            {
                if (walk.ClassListContains("dmg-fmenu-panel") && !walk.ClassListContains("dmg-fmenu-flyout"))
                    return walk;
                walk = walk.hierarchy.parent;
            }

            return null;
        }

        public static Vector2 PanelPositionToScreen(IPanel panel, Vector2 panelPosition)
        {
            if (panel == null)
                return panelPosition;

            // Unity exposes ScreenToPanel only; invert the overlay-panel mapping via corner samples.
            Vector2 panelMin = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            Vector2 panelMax = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));

            float dx = panelMax.x - panelMin.x;
            float dy = panelMax.y - panelMin.y;
            float screenX = Mathf.Abs(dx) > 0.0001f
                ? (panelPosition.x - panelMin.x) / dx * Screen.width
                : panelPosition.x;
            float screenY = Mathf.Abs(dy) > 0.0001f
                ? (panelPosition.y - panelMin.y) / dy * Screen.height
                : panelPosition.y;

            return new Vector2(screenX, screenY);
        }

        private static void ScheduleClampFloating(VisualElement element, VisualElement clampRoot)
        {
            if (element == null || clampRoot == null)
                return;

            element.schedule.Execute(() => ClampFloating(element, clampRoot)).ExecuteLater(0);
            element.schedule.Execute(() => ClampFloating(element, clampRoot)).ExecuteLater(1);
        }

        public static void PositionAtWorld(VisualElement element, Vector3 worldPosition, Camera camera)
        {
            if (element == null || element.panel == null || camera == null)
                return;

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
            {
                SetShown(element, false);
                return;
            }

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(element.panel, new Vector2(screen.x, screen.y));
            element.style.left = panelPos.x;
            element.style.top = panelPos.y;
        }

        public static void ClampFloating(VisualElement element, VisualElement clampRoot, float width = 0f, float height = 0f)
        {
            if (element == null || clampRoot == null || element.panel == null)
                return;

            VisualElement parent = element.hierarchy.parent;
            if (parent == null)
                return;

            if (width <= 0f)
                width = element.worldBound.width > 0f ? element.worldBound.width : element.resolvedStyle.width;
            if (height <= 0f)
                height = element.worldBound.height > 0f ? element.worldBound.height : element.resolvedStyle.height;
            if (width <= 0f)
                width = 240f;
            if (height <= 0f)
                height = 120f;

            Rect bounds = clampRoot.worldBound;
            if (bounds.width <= 0f || bounds.height <= 0f)
                bounds = element.panel.visualTree.worldBound;

            Rect current = element.worldBound;
            float x = current.x;
            float y = current.y;

            if (x + width > bounds.xMax)
                x = bounds.xMax - width;
            if (y + height > bounds.yMax)
                y = bounds.yMax - height;
            if (x < bounds.xMin)
                x = bounds.xMin;
            if (y < bounds.yMin)
                y = bounds.yMin;

            Vector2 local = parent.WorldToLocal(new Vector2(x, y));
            element.style.left = local.x;
            element.style.top = local.y;
        }

        public static Button MakeMenuButton(string name, string label)
        {
            Button button = new Button();
            button.name = name;
            button.text = label ?? string.Empty;
            button.AddToClassList("dmg-fmenu-btn");
            button.focusable = true;
            return button;
        }

        public static void PopulateSegments(VisualElement row, int count)
        {
            if (row == null)
                return;

            while (row.childCount < count)
            {
                VisualElement seg = new VisualElement();
                seg.AddToClassList("dmg-seg");
                seg.pickingMode = PickingMode.Ignore;
                row.Add(seg);
            }
        }

        public static void ApplySegments(VisualElement row, float normalized, Color fillColor)
        {
            if (row == null)
                return;

            int count = row.childCount;
            if (count <= 0)
                return;

            float clamped = Mathf.Clamp01(normalized);
            int lit = Mathf.RoundToInt(clamped * count);
            Color dim = new Color(1f, 1f, 1f, 0.08f);
            for (int i = 0; i < count; i++)
                row[i].style.backgroundColor = i < lit ? fillColor : dim;
        }
    }
}
