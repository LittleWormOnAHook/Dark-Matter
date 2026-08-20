using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Main-menu Controls reference: hub (Keyboard &amp; Mouse / Controller) and scrollable image pages.
    /// </summary>
    public sealed class ControlsPanelController : MonoBehaviour
    {
        private const float WindowWidth = 920f;
        private const float WindowHeight = 640f;
        private const float FooterHeight = 44f;
        private const string KeyboardSchemeResourcePath = "UI/Controls/ControlsScheme_KeyboardMouse";
        private const string GamepadSchemeResourcePath = "UI/Controls/ControlsScheme_Gamepad";

        private enum PanelMode
        {
            Hub,
            Scheme
        }

        private GameObject panelRoot;
        private GameObject hubWindow;
        private GameObject schemeWindow;
        private TextMeshProUGUI schemeTitleLabel;
        private Transform schemeScrollContent;
        private ScrollRect schemeScrollRect;
        private ControlsSchemeDefinition keyboardScheme;
        private ControlsSchemeDefinition gamepadScheme;
        private PanelMode mode = PanelMode.Hub;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Build(Transform parent)
        {
            if (panelRoot != null)
                return;

            LoadSchemes();

            panelRoot = MenuUiBuilder.CreateFullScreenPanel(
                parent,
                "ControlsPanel",
                DarkMatterGenesisUiPalette.WithAlpha(Color.black, 0.82f),
                blockRaycasts: true);

            hubWindow = BuildHubWindow(panelRoot.transform);
            schemeWindow = BuildSchemeWindow(panelRoot.transform);
            schemeWindow.SetActive(false);

            MenuUiBuilder.CreateTopRightBackButton(panelRoot.transform, HandleBack, width: 88f, height: 30f, fontSize: 14f, inset: 14f);

            panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (panelRoot == null)
                return;

            LoadSchemes();
            ShowHub();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void HandleBack()
        {
            if (!IsOpen)
                return;

            if (mode == PanelMode.Scheme)
            {
                ShowHub();
                return;
            }

            Close();
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
            mode = PanelMode.Hub;
            if (hubWindow != null)
                hubWindow.SetActive(true);
            if (schemeWindow != null)
                schemeWindow.SetActive(false);
        }

        private void ShowScheme(ControlsSchemeDefinition scheme)
        {
            if (scheme == null)
                return;

            mode = PanelMode.Scheme;
            if (hubWindow != null)
                hubWindow.SetActive(false);
            if (schemeWindow != null)
                schemeWindow.SetActive(true);

            if (schemeTitleLabel != null)
                schemeTitleLabel.text = string.IsNullOrWhiteSpace(scheme.SchemeTitle) ? "Controls" : scheme.SchemeTitle;

            PopulateSchemePages(scheme);
        }

        private GameObject BuildHubWindow(Transform parent)
        {
            GameObject window = CreateModalWindow(parent, "ControlsHubWindow", out RectTransform windowRect);
            windowRect.sizeDelta = new Vector2(420f, 320f);

            VerticalLayoutGroup layout = window.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 12;

            TextMeshProUGUI title = MenuUiBuilder.CreateTitle(window.transform, "Controls", 24f);
            title.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI subtitle = CreateBodyLabel(
                window.transform,
                "Choose an input scheme to view the reference layout.",
                16f,
                DarkMatterGenesisUiPalette.MutedText);

            Button keyboardButton = MenuUiBuilder.CreateButton(window.transform, "Keyboard and Mouse", new Vector2(360f, 48f), 20f);
            keyboardButton.onClick.AddListener(() => ShowScheme(keyboardScheme));

            Button controllerButton = MenuUiBuilder.CreateButton(window.transform, "Controller", new Vector2(360f, 48f), 20f);
            controllerButton.onClick.AddListener(() => ShowScheme(gamepadScheme));

            Button backButton = MenuUiBuilder.CreateButton(window.transform, "Back", new Vector2(160f, 40f), 18f);
            backButton.onClick.AddListener(Close);

            return window;
        }

        private GameObject BuildSchemeWindow(Transform parent)
        {
            GameObject window = CreateModalWindow(parent, "ControlsSchemeWindow", out RectTransform windowRect);
            windowRect.sizeDelta = new Vector2(WindowWidth, WindowHeight);

            VerticalLayoutGroup layout = window.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8;

            schemeTitleLabel = MenuUiBuilder.CreateTitle(window.transform, "Controls", 22f);
            schemeTitleLabel.alignment = TextAlignmentOptions.Center;

            schemeScrollContent = BuildSchemeScrollArea(window.transform, out schemeScrollRect);

            GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            footer.transform.SetParent(window.transform, false);
            LayoutElement footerLayout = footer.GetComponent<LayoutElement>();
            footerLayout.minHeight = FooterHeight;
            footerLayout.preferredHeight = FooterHeight;

            HorizontalLayoutGroup footerHBox = footer.GetComponent<HorizontalLayoutGroup>();
            footerHBox.childAlignment = TextAnchor.MiddleCenter;
            footerHBox.spacing = 10;

            Button backButton = MenuUiBuilder.CreateButton(footer.transform, "Back", new Vector2(140f, 36f), 18f);
            backButton.onClick.AddListener(HandleBack);

            return window;
        }

        private static GameObject CreateModalWindow(Transform parent, string name, out RectTransform windowRect)
        {
            GameObject window = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            window.transform.SetParent(parent, false);

            Image windowImage = window.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(windowImage);
            DarkMatterGenesisUiPalette.ApplyPanelShellBackground(windowImage, 0.98f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(window);

            windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup windowLayout = window.GetComponent<VerticalLayoutGroup>();
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            return window;
        }

        private static Transform BuildSchemeScrollArea(Transform window, out ScrollRect scrollRect)
        {
            GameObject scrollHost = new GameObject(
                "SchemeScroll",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(LayoutElement),
                typeof(Image));
            scrollHost.transform.SetParent(window, false);

            LayoutElement scrollLayout = scrollHost.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 420f;

            Image scrollBg = scrollHost.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.55f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollHost.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-10f, -4f);

            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 16;
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = scrollHost.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return content.transform;
        }

        private void PopulateSchemePages(ControlsSchemeDefinition scheme)
        {
            if (schemeScrollContent == null)
                return;

            for (int i = schemeScrollContent.childCount - 1; i >= 0; i--)
                Destroy(schemeScrollContent.GetChild(i).gameObject);

            ControlsSchemePage[] pages = scheme.Pages;
            if (pages == null || pages.Length == 0)
            {
                CreateBodyLabel(schemeScrollContent, "No reference images assigned for this scheme.", 18f, DarkMatterGenesisUiPalette.BodyText);
                return;
            }

            for (int i = 0; i < pages.Length; i++)
            {
                ControlsSchemePage page = pages[i];
                if (page.Image == null)
                    continue;

                CreateSchemeImageRow(schemeScrollContent, page);
            }

            if (schemeScrollRect != null)
                schemeScrollRect.verticalNormalizedPosition = 1f;
        }

        private static void CreateSchemeImageRow(Transform parent, ControlsSchemePage page)
        {
            GameObject row = new GameObject("SchemePage", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minWidth = 820f;
            rowLayout.preferredWidth = 820f;

            VerticalLayoutGroup rowVBox = row.GetComponent<VerticalLayoutGroup>();
            rowVBox.spacing = 8;
            rowVBox.childAlignment = TextAnchor.UpperCenter;
            rowVBox.childControlWidth = true;
            rowVBox.childControlHeight = true;
            rowVBox.childForceExpandWidth = true;
            rowVBox.childForceExpandHeight = false;

            GameObject imageObject = new GameObject("Image", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter), typeof(LayoutElement));
            imageObject.transform.SetParent(row.transform, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = page.Image;
            image.preserveAspect = true;
            image.color = Color.white;

            AspectRatioFitter fitter = imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = page.Image.rect.width / Mathf.Max(1f, page.Image.rect.height);

            LayoutElement imageLayout = imageObject.GetComponent<LayoutElement>();
            imageLayout.minWidth = 820f;
            imageLayout.preferredWidth = 820f;
            imageLayout.flexibleWidth = 1f;

            if (!string.IsNullOrWhiteSpace(page.Caption))
            {
                TextMeshProUGUI caption = CreateBodyLabel(row.transform, page.Caption, 15f, DarkMatterGenesisUiPalette.MutedText);
                caption.alignment = TextAlignmentOptions.Center;
            }
        }

        private static TextMeshProUGUI CreateBodyLabel(Transform parent, string text, float fontSize, Color color)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
    }
}
