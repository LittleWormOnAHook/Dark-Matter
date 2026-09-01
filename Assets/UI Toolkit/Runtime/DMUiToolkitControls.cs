using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    [DisallowMultipleComponent]
    public class DMUiToolkitControls : MonoBehaviour
    {
        public const string Name = "UITK_Controls";
        public const int Sort = 21100;
        public const string UxmlPath = "Assets/UI Toolkit/Screens/Controls.uxml";
        private const string KeyboardSchemeResourcePath = "UI/Controls/ControlsScheme_KeyboardMouse";
        private const string GamepadSchemeResourcePath = "UI/Controls/ControlsScheme_Gamepad";

        private static DMUiToolkitControls instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement hub;
        private ScrollView schemeBody;
        private Label titleLabel;
        private ControlsSchemeDefinition keyboardScheme;
        private ControlsSchemeDefinition gamepadScheme;
        private bool showingScheme;
        private bool open;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitControls EnsureHost()
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return null;

            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitMenuDocument.Ensure(Name, UxmlPath, Sort);
            if (doc == null)
                return null;

            DMUiToolkitControls host = doc.GetComponent<DMUiToolkitControls>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitControls>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static void Open()
        {
            DMUiToolkitControls host = EnsureHost();
            host?.ShowInternal();
        }

        public static void Close()
        {
            instance?.HideInternal();
        }

        public static bool HandleBack()
        {
            if (!IsOpen || instance == null)
                return false;

            if (instance.showingScheme)
            {
                instance.ShowHub();
                return true;
            }

            Close();
            Object.FindAnyObjectByType<MainMenuController>()?.RestoreMenuAfterSubPanel();
            return true;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
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

            root = tree.Q<VisualElement>("controls-root") ?? tree;
            hub = tree.Q<VisualElement>("controls-hub");
            schemeBody = tree.Q<ScrollView>("controls-scheme-body");
            titleLabel = tree.Q<Label>("controls-title");
            Button keyboardButton = tree.Q<Button>("controls-keyboard");
            Button gamepadButton = tree.Q<Button>("controls-gamepad");
            Button backButton = tree.Q<Button>("controls-back");

            if (keyboardButton != null)
            {
                keyboardButton.clicked -= ShowKeyboardScheme;
                keyboardButton.clicked += ShowKeyboardScheme;
            }

            if (gamepadButton != null)
            {
                gamepadButton.clicked -= ShowGamepadScheme;
                gamepadButton.clicked += ShowGamepadScheme;
            }

            if (backButton != null)
            {
                backButton.clicked -= OnBackClicked;
                backButton.clicked += OnBackClicked;
            }

            HideInternal();
        }

        private void ShowInternal()
        {
            BindTree();
            LoadSchemes();
            ShowHub();
            open = true;
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void HideInternal()
        {
            open = false;
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void LoadSchemes()
        {
            if (keyboardScheme == null)
                keyboardScheme = Resources.Load<ControlsSchemeDefinition>(KeyboardSchemeResourcePath);
            if (gamepadScheme == null)
                gamepadScheme = Resources.Load<ControlsSchemeDefinition>(GamepadSchemeResourcePath);
        }

        private void ShowHub()
        {
            showingScheme = false;
            if (titleLabel != null)
                titleLabel.text = "CONTROLS";
            if (hub != null)
                hub.style.display = DisplayStyle.Flex;
            if (schemeBody != null)
                schemeBody.style.display = DisplayStyle.None;
        }

        private void ShowKeyboardScheme() => ShowScheme(keyboardScheme);
        private void ShowGamepadScheme() => ShowScheme(gamepadScheme);

        private void ShowScheme(ControlsSchemeDefinition scheme)
        {
            if (scheme == null || schemeBody == null)
                return;

            showingScheme = true;
            if (hub != null)
                hub.style.display = DisplayStyle.None;
            schemeBody.style.display = DisplayStyle.Flex;
            schemeBody.Clear();

            if (titleLabel != null)
                titleLabel.text = string.IsNullOrWhiteSpace(scheme.SchemeTitle) ? "CONTROLS" : scheme.SchemeTitle.ToUpperInvariant();

            ControlsSchemePage[] pages = scheme.Pages;
            if (pages == null)
                return;

            for (int i = 0; i < pages.Length; i++)
            {
                ControlsSchemePage page = pages[i];
                if (page.Image != null)
                {
                    VisualElement image = new VisualElement { pickingMode = PickingMode.Ignore };
                    image.AddToClassList("dmg-menu-scheme-image");
                    DMUiToolkitStyle.TrySetSpriteBackground(image, page.Image, ScaleMode.ScaleToFit);
                    schemeBody.Add(image);
                }

                if (!string.IsNullOrWhiteSpace(page.Caption))
                {
                    Label caption = new Label(page.Caption) { pickingMode = PickingMode.Ignore };
                    caption.AddToClassList("dmg-menu-scheme-caption");
                    schemeBody.Add(caption);
                }
            }
        }

        private void OnBackClicked() => HandleBack();
    }
}
