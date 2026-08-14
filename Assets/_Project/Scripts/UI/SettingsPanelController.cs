using System.Collections.Generic;
using Project.Core;
using Project.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class SettingsPanelController : MonoBehaviour
    {
        private const float WindowWidth = 440f;
        private const float WindowHeight = 520f;
        private const float HeaderHeight = 36f;
        private const float FooterHeight = 44f;

        private GameObject panelRoot;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Toggle postProcessingToggle;
        private Toggle rayTracingToggle;
        private Toggle minimapToggle;
        private Toggle fullscreenToggle;
        private Toggle vsyncToggle;
        private Dropdown qualityDropdown;
        private Dropdown resolutionDropdown;
        private TextMeshProUGUI masterValueLabel;
        private TextMeshProUGUI musicValueLabel;
        private TextMeshProUGUI sfxValueLabel;
        private TextMeshProUGUI graphicsAdvisoryLabel;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Build(Transform parent)
        {
            if (panelRoot != null)
                return;

            panelRoot = MenuUiBuilder.CreateFullScreenPanel(
                parent,
                "SettingsPanel",
                SurvivalPioneerUiPalette.WithAlpha(Color.black, 0.82f),
                blockRaycasts: true);

            GameObject window = new GameObject("SettingsWindow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            window.transform.SetParent(panelRoot.transform, false);

            Image windowImage = window.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(windowImage);
            SurvivalPioneerUiPalette.ApplyPanelShellBackground(windowImage, 0.98f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(window);

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(WindowWidth, WindowHeight);

            VerticalLayoutGroup windowLayout = window.GetComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(12, 12, 10, 10);
            windowLayout.spacing = 6;
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = MenuUiBuilder.CreateTitle(window.transform, "Settings", 22f);
            title.alignment = TextAlignmentOptions.Center;
            LayoutElement titleLayout = title.GetComponent<LayoutElement>();
            titleLayout.minHeight = HeaderHeight;
            titleLayout.preferredHeight = HeaderHeight;
            titleLayout.flexibleHeight = 0f;

            Transform scrollContent = BuildScrollArea(window.transform);

            CreateSectionTitle(scrollContent, "Audio");
            masterSlider = MenuUiBuilder.CreateSliderRow(scrollContent, "Master Volume", GameSettings.MasterVolume, out masterValueLabel);
            musicSlider = MenuUiBuilder.CreateSliderRow(scrollContent, "Music Volume", GameSettings.MusicVolume, out musicValueLabel);
            sfxSlider = MenuUiBuilder.CreateSliderRow(scrollContent, "SFX Volume", GameSettings.SfxVolume, out sfxValueLabel);

            CreateSectionTitle(scrollContent, "Graphics");
            qualityDropdown = MenuUiBuilder.CreateDropdownRow(scrollContent, "Quality");
            resolutionDropdown = MenuUiBuilder.CreateDropdownRow(scrollContent, "Resolution");
            fullscreenToggle = MenuUiBuilder.CreateToggleRow(scrollContent, "Fullscreen", GameSettings.Fullscreen);
            vsyncToggle = MenuUiBuilder.CreateToggleRow(scrollContent, "VSync", GameSettings.VSync);
            rayTracingToggle = MenuUiBuilder.CreateToggleRow(scrollContent, "Ray Tracing", GameSettings.RayTracingEnabled);
            postProcessingToggle = MenuUiBuilder.CreateToggleRow(scrollContent, "Post Processing", GameSettings.PostProcessingEnabled);
            graphicsAdvisoryLabel = CreateAdvisoryLabel(scrollContent);

            CreateSectionTitle(scrollContent, "Gameplay");
            minimapToggle = MenuUiBuilder.CreateCircleToggleRow(scrollContent, "Minimap", GameSettings.MinimapEnabled);

            GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(window.transform, false);
            LayoutElement buttonRowLayout = buttonRow.GetComponent<LayoutElement>();
            buttonRowLayout.minHeight = FooterHeight;
            buttonRowLayout.preferredHeight = FooterHeight;
            buttonRowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;
            buttonLayout.childForceExpandWidth = false;

            Button applyButton = MenuUiBuilder.CreateButton(
                buttonRow.transform,
                "Apply",
                new Vector2(120f, 32f),
                15f);
            applyButton.onClick.RemoveAllListeners();
            applyButton.onClick.AddListener(ApplySettings);

            // Pinned to the panel root so it stays fixed top-right outside the modal layout.
            MenuUiBuilder.CreateTopRightBackButton(
                panelRoot.transform,
                Close,
                width: 88f,
                height: 30f,
                fontSize: 14f,
                inset: 14f);

            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMasterVolume(value);
                UpdatePercentLabel(masterValueLabel, value);
            });
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMusicVolume(value);
                GameAudioManager.Instance?.RefreshVolumes();
                UpdatePercentLabel(musicValueLabel, value);
            });
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetSfxVolume(value);
                GameAudioManager.Instance?.RefreshVolumes();
                UpdatePercentLabel(sfxValueLabel, value);
            });
            postProcessingToggle.onValueChanged.RemoveAllListeners();
            postProcessingToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetPostProcessingEnabled(value);
                PostProcessingController.Instance?.ApplyFromSettings();
            });
            minimapToggle.onValueChanged.RemoveAllListeners();
            minimapToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMinimapEnabled(value);
                MapUI.ApplyMinimapEnabled(value);
            });
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(GameSettings.SetFullscreen);
            vsyncToggle.onValueChanged.RemoveAllListeners();
            vsyncToggle.onValueChanged.AddListener(GameSettings.SetVSync);
            rayTracingToggle.onValueChanged.RemoveAllListeners();
            rayTracingToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetRayTracingEnabled(value);
                RefreshGraphicsAdvisory();
            });
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(value =>
            {
                GameSettings.SetQualityLevel(value);
                RefreshGraphicsAdvisory();
            });
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(GameSettings.SetResolutionIndex);

            PopulateDropdowns();
            SyncControlsFromSettings();
            panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (panelRoot == null)
                return;

            SyncControlsFromSettings();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private static Transform BuildScrollArea(Transform window)
        {
            GameObject scrollHost = new GameObject(
                "Scroll",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(LayoutElement),
                typeof(Image));
            scrollHost.transform.SetParent(window, false);

            LayoutElement scrollLayout = scrollHost.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 180f;

            Image scrollBg = scrollHost.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.55f);
            scrollBg.raycastTarget = true;

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
            contentLayout.padding = new RectOffset(6, 6, 4, 8);
            contentLayout.spacing = 6;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject scrollbarObject = new GameObject(
                "Scrollbar",
                typeof(RectTransform),
                typeof(Image),
                typeof(Scrollbar));
            scrollbarObject.transform.SetParent(scrollHost.transform, false);
            RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(6f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.offsetMin = new Vector2(-6f, 4f);
            scrollbarRect.offsetMax = new Vector2(0f, -4f);

            Image scrollbarBg = scrollbarObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollbarBg);
            scrollbarBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.55f);

            GameObject handleArea = new GameObject("Sliding Area", typeof(RectTransform));
            handleArea.transform.SetParent(scrollbarObject.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(handleImage);
            handleImage.color = SurvivalPioneerUiPalette.RichFuchsia;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.size = 1f;

            ScrollRect scroll = scrollHost.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            return content.transform;
        }

        private void ApplySettings()
        {
            GameSettings.Save();
            GameAudioManager.Instance?.RefreshVolumes();
            PostProcessingController.Instance?.ApplyFromSettings();
            Close();
        }

        private void SyncControlsFromSettings()
        {
            masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            postProcessingToggle.SetIsOnWithoutNotify(GameSettings.PostProcessingEnabled);
            minimapToggle.SetIsOnWithoutNotify(GameSettings.MinimapEnabled);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            vsyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            rayTracingToggle.SetIsOnWithoutNotify(GameSettings.RayTracingEnabled);
            qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
            resolutionDropdown.SetValueWithoutNotify(GameSettings.GetCurrentResolutionIndex());
            RefreshGraphicsAdvisory();

            UpdatePercentLabel(masterValueLabel, GameSettings.MasterVolume);
            UpdatePercentLabel(musicValueLabel, GameSettings.MusicVolume);
            UpdatePercentLabel(sfxValueLabel, GameSettings.SfxVolume);
        }

        private void PopulateDropdowns()
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));

            resolutionDropdown.ClearOptions();
            List<string> resolutionLabels = new List<string>();
            foreach (Resolution resolution in Screen.resolutions)
                resolutionLabels.Add($"{resolution.width} x {resolution.height}");

            resolutionDropdown.AddOptions(resolutionLabels);
        }

        private static void CreateSectionTitle(Transform parent, string title)
        {
            TextMeshProUGUI label = MenuUiBuilder.CreateTitle(parent, title, 15f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = SurvivalPioneerUiPalette.Gold;

            LayoutElement layout = label.GetComponent<LayoutElement>();
            layout.minHeight = 22f;
            layout.preferredHeight = 22f;
            layout.flexibleHeight = 0f;
        }

        private static TextMeshProUGUI CreateAdvisoryLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("GraphicsAdvisory", typeof(RectTransform), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);

            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.minHeight = 36f;
            layout.preferredHeight = 36f;
            layout.flexibleHeight = 0f;

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = string.Empty;
            label.fontSize = 12f;
            label.color = SurvivalPioneerUiPalette.SoftBeigeGray;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.gameObject.SetActive(false);
            return label;
        }

        private void RefreshGraphicsAdvisory()
        {
            if (graphicsAdvisoryLabel == null)
                return;

            string summary = GameSettings.GetGraphicsAdvisorySummary();
            graphicsAdvisoryLabel.text = string.IsNullOrEmpty(summary)
                ? string.Empty
                : summary;
            graphicsAdvisoryLabel.gameObject.SetActive(!string.IsNullOrEmpty(summary));
        }

        private static void UpdatePercentLabel(TextMeshProUGUI label, float value)
        {
            if (label != null)
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
