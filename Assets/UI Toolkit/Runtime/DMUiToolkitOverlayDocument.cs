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
        public const string PiRewardName = "UITK_PiReward";
        public const string GameStartName = "UITK_GameStart";
        public const string ContextName = "UITK_Context";
        public const string PetChromeName = "UITK_PetChrome";

        public const int ActiveQuestSort = 11;
        public const int HovercraftSort = 12;
        public const int HazardsSort = 13;
        public const int DamageSort = 15;
        public const int PickupReticleSort = 16;
        public const int LevelUpSort = 20;
        public const int HoverInteractSort = 40;
        public const int WorldMenusSort = 105;
        public const int CraftSort = 45;
        public const int DialogueSort = 50;
        public const int PiRewardSort = 55;
        public const int DeathSort = 60;
        public const int GameStartSort = 70;
        public const int ContextSort = 110;
        public const int PetChromeSort = 111;

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
        public const string PiRewardUxml = "Assets/UI Toolkit/Screens/PiReward.uxml";
        public const string PiRewardUss = "Assets/UI Toolkit/Screens/PiReward.uss";
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

            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
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

        public static void PositionAtScreen(VisualElement element, Vector2 screenPosition)
        {
            if (element == null || element.panel == null)
                return;

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(element.panel, screenPosition);
            element.style.left = panelPos.x;
            element.style.top = panelPos.y;
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

        public static void ClampFloating(VisualElement element, VisualElement root, float width, float height)
        {
            if (element == null || root == null)
                return;

            float left = element.resolvedStyle.left;
            float top = element.resolvedStyle.top;
            float maxX = Mathf.Max(0f, root.layout.width - width);
            float maxY = Mathf.Max(0f, root.layout.height - height);
            element.style.left = Mathf.Clamp(left, 0f, maxX);
            element.style.top = Mathf.Clamp(top, 0f, maxY);
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
