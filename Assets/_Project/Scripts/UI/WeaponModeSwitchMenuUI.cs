using Project.Core;
using Project.Player;
using Project.Player.Invector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Hold-R Mode Switch mini-menu: Melee / Pistols / Rifles,
    /// with Pistols → Sci-Fi Pistol LaserSight + Laser, Rifles → Survival Rifle LaserSight + Laser.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponModeSwitchMenuUI : MonoBehaviour
    {
        private static WeaponModeSwitchMenuUI instance;

        private GameObject menuRoot;
        private GameObject menuPanel;
        private GameObject pistolsSubmenuPanel;
        private GameObject riflesSubmenuPanel;
        private Transform canvasRoot;
        private WeaponModeSwitchController controller;
        private PlayerController boundPlayer;
        private TextMeshProUGUI pistolLaserSightLabel;
        private TextMeshProUGUI pistolLaserLabel;
        private Button pistolLaserSightButton;
        private Button pistolLaserButton;
        private TextMeshProUGUI laserSightLabel;
        private TextMeshProUGUI laserLabel;
        private Button laserSightButton;
        private Button laserButton;
        private int openedOnFrame = -1;

        public static bool IsOpen =>
            DMUiToolkitWorldMenus.IsWeaponOpen
            || (instance != null && instance.menuRoot != null && instance.menuRoot.activeSelf);

        public static WeaponModeSwitchMenuUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
            {
                instance.canvasRoot = canvasRootTransform;
                return instance;
            }

            WeaponModeSwitchMenuUI existing = Object.FindAnyObjectByType<WeaponModeSwitchMenuUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                existing.canvasRoot = canvasRootTransform;
                if (existing.menuRoot == null)
                    existing.Build();
                return existing;
            }

            Transform parent = canvasRootTransform != null
                ? canvasRootTransform
                : Object.FindAnyObjectByType<UIManager>()?.transform;

            GameObject host = new GameObject("WeaponModeSwitchMenu", typeof(RectTransform));
            if (parent != null)
                host.transform.SetParent(parent, false);

            WeaponModeSwitchMenuUI menu = host.AddComponent<WeaponModeSwitchMenuUI>();
            menu.canvasRoot = parent;
            menu.Build();
            instance = menu;
            return menu;
        }

        public static void HideAny()
        {
            DMUiToolkitWorldMenus.HideWeapon();
            instance?.Hide();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!IsOpen || openedOnFrame == Time.frameCount)
                return;

            // Full pause (timeScale 0) + free cursor every frame so UI mouse always works.
            ApplyMenuCursorFree();

            // Esc closes; R close is owned by WeaponModeSwitchController (after open-hold release).
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Hide();
        }

        private void Build()
        {
            RectTransform hostRect = transform as RectTransform;
            if (hostRect != null)
            {
                hostRect.anchorMin = Vector2.zero;
                hostRect.anchorMax = Vector2.one;
                hostRect.offsetMin = Vector2.zero;
                hostRect.offsetMax = Vector2.zero;
            }

            menuRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "WeaponModeSwitchRoot", Color.clear, blockRaycasts: false);
            menuRoot.SetActive(false);

            GameObject dismissOverlay = MenuUiBuilder.CreateFullScreenPanel(
                menuRoot.transform,
                "DismissOverlay",
                new Color(0f, 0f, 0f, 0.01f),
                blockRaycasts: true);
            dismissOverlay.transform.SetAsFirstSibling();
            EventTrigger dismissTrigger = dismissOverlay.AddComponent<EventTrigger>();
            EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ => Hide());
            dismissTrigger.triggers.Add(clickEntry);

            menuPanel = CreatePanel(menuRoot.transform, "MenuPanel", 220f);
            CreateTitle(menuPanel.transform, "Mode Switch");
            CreateCategoryButton(menuPanel.transform, "Melee", null);
            CreatePistolsCategoryButton();
            CreateRiflesCategoryButton();

            pistolsSubmenuPanel = CreatePanel(menuRoot.transform, "PistolsSubmenu", 200f);
            pistolsSubmenuPanel.SetActive(false);
            pistolLaserSightButton = CreateToggleButton(
                pistolsSubmenuPanel.transform,
                "LaserSight",
                out pistolLaserSightLabel,
                () => TogglePistolLaserSight());
            pistolLaserButton = CreateToggleButton(
                pistolsSubmenuPanel.transform,
                "Laser",
                out pistolLaserLabel,
                () => TogglePistolLaserBeam());

            riflesSubmenuPanel = CreatePanel(menuRoot.transform, "RiflesSubmenu", 200f);
            riflesSubmenuPanel.SetActive(false);
            laserSightButton = CreateToggleButton(
                riflesSubmenuPanel.transform,
                "LaserSight",
                out laserSightLabel,
                () => ToggleLaserSight());
            laserButton = CreateToggleButton(
                riflesSubmenuPanel.transform,
                "Laser",
                out laserLabel,
                () => ToggleLaserBeam());

            menuRoot.SetActive(false);
        }

        public void Show(WeaponModeSwitchController modeController)
        {
            if (modeController == null)
                return;

            if (DMUiToolkitWorldMenus.TryShowWeapon(modeController))
            {
                if (menuRoot != null)
                    menuRoot.SetActive(false);
                return;
            }

            if (menuRoot == null)
                Build();

            controller = modeController;
            boundPlayer = modeController.GetComponent<PlayerController>()
                          ?? PlayerLocator.FindPlayerController();
            openedOnFrame = Time.frameCount;
            RefreshToggleLabels();

            menuRoot.SetActive(true);
            if (pistolsSubmenuPanel != null)
                pistolsSubmenuPanel.SetActive(false);
            if (riflesSubmenuPanel != null)
                riflesSubmenuPanel.SetActive(false);
            PositionNearCenter(menuPanel);
            menuPanel.transform.SetAsLastSibling();

            // Full pause like Inventory — slow-mo left cursor unreliable for this mini-menu.
            boundPlayer?.SetBuildingControlOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonWeaponModeSwitch, false);
            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonWeaponModeSwitch, true);
            ApplyMenuCursorFree();
        }

        public void Hide()
        {
            DMUiToolkitWorldMenus.HideWeapon();
            if (menuRoot != null)
                menuRoot.SetActive(false);

            if (pistolsSubmenuPanel != null)
                pistolsSubmenuPanel.SetActive(false);

            if (riflesSubmenuPanel != null)
                riflesSubmenuPanel.SetActive(false);

            controller = null;
            if (boundPlayer != null)
            {
                boundPlayer.SetBuildingControlOpen(false);
                boundPlayer = null;
            }
            else
            {
                PlayerLocator.FindPlayerController()?.SetBuildingControlOpen(false);
            }

            GameplayMenuTime.SetPause(GameplayMenuTime.ReasonWeaponModeSwitch, false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonWeaponModeSwitch, false);
        }

        private void ApplyMenuCursorFree()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            boundPlayer?.ApplyCursorState();
            // Re-assert after ApplyCursorState in case optics/other flags fight — mode menu needs pointer.
            if (IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void CreatePistolsCategoryButton()
        {
            Button button = MenuUiBuilder.CreateButton(menuPanel.transform, "Pistols >", new Vector2(200f, 34f), 16f);
            button.name = "PistolsCategoryButton";

            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowPistolsSubmenu());
            trigger.triggers.Add(enterEntry);

            button.onClick.AddListener(ShowPistolsSubmenu);
        }

        private void CreateRiflesCategoryButton()
        {
            Button button = MenuUiBuilder.CreateButton(menuPanel.transform, "Rifles >", new Vector2(200f, 34f), 16f);
            button.name = "RiflesCategoryButton";

            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => ShowRiflesSubmenu());
            trigger.triggers.Add(enterEntry);

            button.onClick.AddListener(ShowRiflesSubmenu);
        }

        private void ShowPistolsSubmenu()
        {
            if (pistolsSubmenuPanel == null || menuPanel == null)
                return;

            if (riflesSubmenuPanel != null)
                riflesSubmenuPanel.SetActive(false);

            RefreshToggleLabels();
            pistolsSubmenuPanel.SetActive(true);
            PositionBeside(menuPanel, pistolsSubmenuPanel);
            pistolsSubmenuPanel.transform.SetAsLastSibling();
        }

        private void ShowRiflesSubmenu()
        {
            if (riflesSubmenuPanel == null || menuPanel == null)
                return;

            if (pistolsSubmenuPanel != null)
                pistolsSubmenuPanel.SetActive(false);

            RefreshToggleLabels();
            riflesSubmenuPanel.SetActive(true);
            PositionBeside(menuPanel, riflesSubmenuPanel);
            riflesSubmenuPanel.transform.SetAsLastSibling();
        }

        private void TogglePistolLaserSight()
        {
            if (controller == null)
                return;

            controller.SetPistolLaserSightEnabled(!controller.PistolLaserSightEnabled);
            RefreshToggleLabels();
        }

        private void TogglePistolLaserBeam()
        {
            if (controller == null)
                return;

            controller.SetPistolLaserBeamEnabled(!controller.PistolLaserBeamEnabled);
            RefreshToggleLabels();
        }

        private void ToggleLaserSight()
        {
            if (controller == null)
                return;

            controller.SetLaserSightEnabled(!controller.LaserSightEnabled);
            RefreshToggleLabels();
        }

        private void ToggleLaserBeam()
        {
            if (controller == null)
                return;

            controller.SetLaserBeamEnabled(!controller.LaserBeamEnabled);
            RefreshToggleLabels();
        }

        private void RefreshToggleLabels()
        {
            if (controller == null)
                return;

            if (pistolLaserSightLabel != null)
                pistolLaserSightLabel.text = FormatToggleLabel("LaserSight", controller.PistolLaserSightEnabled);

            if (pistolLaserLabel != null)
                pistolLaserLabel.text = FormatToggleLabel("Laser", controller.PistolLaserBeamEnabled);

            ApplyToggleButtonColor(pistolLaserSightButton, controller.PistolLaserSightEnabled);
            ApplyToggleButtonColor(pistolLaserButton, controller.PistolLaserBeamEnabled);

            if (laserSightLabel != null)
                laserSightLabel.text = FormatToggleLabel("LaserSight", controller.LaserSightEnabled);

            if (laserLabel != null)
                laserLabel.text = FormatToggleLabel("Laser", controller.LaserBeamEnabled);

            ApplyToggleButtonColor(laserSightButton, controller.LaserSightEnabled);
            ApplyToggleButtonColor(laserButton, controller.LaserBeamEnabled);
        }

        private static string FormatToggleLabel(string name, bool on) =>
            on ? $"{name}  ON" : $"{name}  OFF";

        private static void ApplyToggleButtonColor(Button button, bool on)
        {
            if (button == null)
                return;

            Image image = button.GetComponent<Image>();
            if (image == null)
                return;

            image.color = on
                ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DeepMagenta, 0.95f)
                : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.9f);
        }

        private static void CreateCategoryButton(Transform parent, string label, System.Action onClick)
        {
            Button button = MenuUiBuilder.CreateButton(parent, label, new Vector2(200f, 34f), 16f);
            button.name = label.Replace(" ", string.Empty) + "CategoryButton";
            if (onClick != null)
                button.onClick.AddListener(() => onClick.Invoke());
        }

        private static Button CreateToggleButton(
            Transform parent,
            string label,
            out TextMeshProUGUI labelText,
            System.Action onClick)
        {
            Button button = MenuUiBuilder.CreateButton(parent, label, new Vector2(184f, 34f), 15f);
            button.name = label.Replace(" ", string.Empty) + "ToggleButton";
            labelText = button.GetComponentInChildren<TextMeshProUGUI>();
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        private static void CreateTitle(Transform parent, string title)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            text.text = title;
            text.fontSize = 17f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = DarkMatterGenesisUiPalette.Gold;
            text.raycastTarget = false;

            LayoutElement layout = titleObject.AddComponent<LayoutElement>();
            layout.minHeight = 28f;
            layout.preferredHeight = 28f;
            layout.minWidth = 200f;
        }

        private static GameObject CreatePanel(Transform parent, string name, float width)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(parent, false);

            Image panelImage = panel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.PanelBackground, 0.98f);
            panelImage.raycastTarget = true;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(width, 0f);

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return panel;
        }

        private void PositionNearCenter(GameObject panel)
        {
            if (panel == null)
                return;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            RectTransform parentRect = menuRoot.transform as RectTransform;
            if (panelRect == null || parentRect == null)
                return;

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
        }

        private void PositionBeside(GameObject anchor, GameObject flyout)
        {
            if (anchor == null || flyout == null)
                return;

            RectTransform anchorRect = anchor.GetComponent<RectTransform>();
            RectTransform flyoutRect = flyout.GetComponent<RectTransform>();
            if (anchorRect == null || flyoutRect == null)
                return;

            flyoutRect.anchorMin = anchorRect.anchorMin;
            flyoutRect.anchorMax = anchorRect.anchorMax;
            flyoutRect.pivot = new Vector2(0f, 0.5f);
            flyoutRect.anchoredPosition = anchorRect.anchoredPosition + new Vector2(anchorRect.rect.width * 0.5f + 8f, 0f);
        }
    }
}
