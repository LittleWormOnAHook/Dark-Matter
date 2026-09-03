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
        private const float HeaderHeight = 48f;
        private const float FooterHeight = 52f;

        private static readonly int[] FrameRateOptions = { -1, 30, 60, 120, 144 };
        private static readonly string[] FrameRateLabels = { "Unlimited", "30 FPS", "60 FPS", "120 FPS", "144 FPS" };

        private GameObject panelRoot;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Slider uiScaleSlider;
        private Slider bloomIntensitySlider;
        private GameObject bloomIntensityRow;
        private Toggle postProcessingToggle;
        private Toggle bloomToggle;
        private Toggle fogToggle;
        private Toggle motionBlurToggle;
        private Toggle depthOfFieldToggle;
        private Toggle ambientOcclusionToggle;
        private Toggle colorGradingToggle;
        private Toggle vignetteToggle;
        private Toggle rayTracingToggle;
        private Toggle minimapToggle;
        private Toggle fullscreenToggle;
        private Toggle vsyncToggle;
        private Toggle dlssToggle;
        private Dropdown qualityDropdown;
        private Dropdown resolutionDropdown;
        private Dropdown frameRateDropdown;
        private Dropdown dlssQualityDropdown;
        private GameObject dlssQualityRow;
        private TextMeshProUGUI masterValueLabel;
        private TextMeshProUGUI musicValueLabel;
        private TextMeshProUGUI sfxValueLabel;
        private TextMeshProUGUI uiScaleValueLabel;
        private TextMeshProUGUI bloomIntensityValueLabel;
        private TextMeshProUGUI graphicsAdvisoryLabel;
        private System.Action closedCallback;
        private readonly List<int> resolutionSourceIndices = new List<int>(32);

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void SetClosedCallback(System.Action callback)
        {
            closedCallback = callback;
        }

        public void Build(Transform parent)
        {
            // Rebuild if a prior play-mode / nested-canvas build left an empty shell.
            if (panelRoot != null)
            {
                if (panelRoot)
                {
                    if (panelRoot.transform.childCount > 0)
                        return;

                    Object.Destroy(panelRoot);
                }

                panelRoot = null;
            }

            // Root-level overlay locked at 90% so MainCanvas UI Scale never warps Settings.
            Transform settingsCanvas = UiScaleApplier.EnsureLockedOverlayCanvas(
                parent,
                UiScaleApplier.LockedSettingsCanvasName,
                sortingOrderBoost: 50);

            // Drop any leftover empty SettingsPanel shells from nested-canvas builds.
            for (int i = settingsCanvas.childCount - 1; i >= 0; i--)
            {
                Transform child = settingsCanvas.GetChild(i);
                if (child != null && child.name == "SettingsPanel")
                    Object.Destroy(child.gameObject);
            }

            // Opaque full-screen navy so main-menu buttons do not ghost through.
            panelRoot = MenuUiBuilder.CreateFullScreenPanel(
                settingsCanvas,
                "SettingsPanel",
                DarkMatterGenesisUiPalette.PanelBackground,
                blockRaycasts: true);

            panelRoot.transform.localScale = Vector3.one;

            Image panelImage = panelRoot.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.sprite = null;
                panelImage.type = Image.Type.Simple;
                panelImage.color = DarkMatterGenesisUiPalette.PanelBackground;
            }

            VerticalLayoutGroup rootLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(56, 56, 36, 28);
            rootLayout.spacing = 18;
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = MenuUiBuilder.CreateTitle(panelRoot.transform, "SETTINGS", 34f);
            title.alignment = TextAlignmentOptions.TopLeft;
            title.color = DarkMatterGenesisUiPalette.BodyText;
            LayoutElement titleLayout = title.gameObject.GetComponent<LayoutElement>();
            if (titleLayout == null)
                titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.minHeight = HeaderHeight;
            titleLayout.preferredHeight = HeaderHeight;
            titleLayout.flexibleHeight = 0f;

            GameObject bodyRow = new GameObject("Body", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            bodyRow.transform.SetParent(panelRoot.transform, false);
            LayoutElement bodyLayoutElement = bodyRow.GetComponent<LayoutElement>();
            bodyLayoutElement.flexibleHeight = 1f;
            bodyLayoutElement.minHeight = 420f;

            HorizontalLayoutGroup bodyLayout = bodyRow.GetComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 28;
            bodyLayout.padding = new RectOffset(0, 0, 4, 0);
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            Transform leftColumn = CreateColumn(bodyRow.transform, "LeftColumn");
            Transform middleColumn = CreateMiddleColumn(bodyRow.transform);
            Transform rightColumn = CreateColumn(bodyRow.transform, "RightColumn");

            // Left — VIDEO / GAMEPLAY
            CreateSectionTitle(leftColumn, "Video");
            vsyncToggle = MenuUiBuilder.CreateToggleRow(leftColumn, "V-Sync", GameSettings.VSync);
            fullscreenToggle = MenuUiBuilder.CreateToggleRow(leftColumn, "Fullscreen", GameSettings.Fullscreen);
            resolutionDropdown = MenuUiBuilder.CreateDropdownRow(leftColumn, "Screen Resolution");
            frameRateDropdown = MenuUiBuilder.CreateDropdownRow(leftColumn, "Framerate Lock");

            if (DlssSettingsApplier.IsDlssUiAvailable())
            {
                dlssToggle = MenuUiBuilder.CreateToggleRow(leftColumn, "DLSS", GameSettings.DlssEnabled);
                dlssQualityDropdown = MenuUiBuilder.CreateDropdownRow(leftColumn, "DLSS Quality");
                dlssQualityRow = dlssQualityDropdown.transform.parent.gameObject;
                dlssQualityDropdown.ClearOptions();
                dlssQualityDropdown.AddOptions(new List<string>(DlssSettingsApplier.QualityDropdownLabels));
            }

            CreateSectionTitle(leftColumn, "Gameplay");
            minimapToggle = MenuUiBuilder.CreateToggleRow(leftColumn, "Minimap", GameSettings.MinimapEnabled);
            uiScaleSlider = MenuUiBuilder.CreateSliderRow(leftColumn, "UI Scale", GameSettings.UiScale, out uiScaleValueLabel);
            uiScaleSlider.minValue = GameSettings.UiScaleMin;
            uiScaleSlider.maxValue = GameSettings.UiScaleMax;
            uiScaleSlider.wholeNumbers = false;
            uiScaleSlider.SetValueWithoutNotify(GameSettings.UiScale);
            UpdatePercentLabel(uiScaleValueLabel, GameSettings.UiScale);

            // Middle — AUDIO
            CreateSectionTitle(middleColumn, "Audio");
            masterSlider = MenuUiBuilder.CreateSliderRow(middleColumn, "Master Volume", GameSettings.MasterVolume, out masterValueLabel);
            musicSlider = MenuUiBuilder.CreateSliderRow(middleColumn, "Music Volume", GameSettings.MusicVolume, out musicValueLabel);
            sfxSlider = MenuUiBuilder.CreateSliderRow(middleColumn, "SFX Volume", GameSettings.SfxVolume, out sfxValueLabel);

            // Right — GRAPHICS / POST PROCESSING
            CreateSectionTitle(rightColumn, "Graphics");
            qualityDropdown = MenuUiBuilder.CreateDropdownRow(rightColumn, "Overall Quality");

            CreateSectionTitle(rightColumn, "Post Processing");
            postProcessingToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Post Processing", GameSettings.PostProcessingEnabled);
            rayTracingToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Ray Tracing", GameSettings.RayTracingEnabled);
            bloomToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Bloom", GameSettings.BloomEnabled);
            fogToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Fog", GameSettings.FogEnabled);
            motionBlurToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Motion Blur", GameSettings.MotionBlurEnabled);
            depthOfFieldToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Depth of Field", GameSettings.DepthOfFieldEnabled);
            ambientOcclusionToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Ambient Occlusion", GameSettings.AmbientOcclusionEnabled);
            colorGradingToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Color Grading", GameSettings.ColorGradingEnabled);
            vignetteToggle = MenuUiBuilder.CreateToggleRow(rightColumn, "Vignette", GameSettings.VignetteEnabled);

            bloomIntensityRow = MenuUiBuilder.CreateSliderRow(
                    rightColumn,
                    "Bloom Intensity",
                    GameSettings.BloomIntensity,
                    out bloomIntensityValueLabel)
                .transform.parent.gameObject;
            bloomIntensitySlider = bloomIntensityRow.GetComponentInChildren<Slider>();
            bloomIntensitySlider.minValue = 0f;
            bloomIntensitySlider.maxValue = 1f;
            bloomIntensitySlider.wholeNumbers = false;
            UpdateBloomIntensityLabel(GameSettings.BloomIntensity);

            graphicsAdvisoryLabel = CreateAdvisoryLabel(rightColumn);

            // Footer — Back / Save (centered under middle column area)
            GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(panelRoot.transform, false);
            LayoutElement buttonRowLayout = buttonRow.GetComponent<LayoutElement>();
            buttonRowLayout.minHeight = FooterHeight;
            buttonRowLayout.preferredHeight = FooterHeight;
            buttonRowLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 16;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;
            buttonLayout.childForceExpandWidth = false;

            Button backButton = MenuUiBuilder.CreateButton(
                buttonRow.transform,
                "Back",
                new Vector2(180f, 36f),
                16f);
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);

            Button saveButton = MenuUiBuilder.CreateButton(
                buttonRow.transform,
                "Save",
                new Vector2(180f, 36f),
                16f);
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(ApplySettings);

            WireControlListeners();
            PopulateDropdowns();
            SyncControlsFromSettings();
            UpdateBloomIntensityRowVisibility(GameSettings.BloomEnabled);
            UpdateDlssControlsVisibility();
            UiScaleApplier.RefreshSettingsPanelScale();
            panelRoot.SetActive(false);
        }

        private static Transform CreateMiddleColumn(Transform parent)
        {
            GameObject column = new GameObject(
                "MiddleColumn",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            column.transform.SetParent(parent, false);

            Image image = column.GetComponent<Image>();
            image.sprite = null;
            image.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.55f);
            image.raycastTarget = false;

            VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement columnLayout = column.GetComponent<LayoutElement>();
            columnLayout.flexibleWidth = 0.95f;
            columnLayout.minWidth = 240f;
            return column.transform;
        }

        public void Open()
        {
            EnsurePanelBuilt();
            if (panelRoot == null)
                return;

            SyncControlsFromSettings();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            if (panelRoot.transform.parent != null)
                panelRoot.transform.parent.SetAsLastSibling();

            Canvas.ForceUpdateCanvases();
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            if (panelRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

            UiScaleApplier.ApplyFromSettings();
        }

        private void EnsurePanelBuilt()
        {
            if (panelRoot != null && panelRoot && panelRoot.transform.childCount > 0)
                return;

            if (panelRoot != null && panelRoot)
                Object.Destroy(panelRoot);
            panelRoot = null;

            Transform canvasRoot = transform;
            Canvas hostCanvas = GetComponent<Canvas>();
            if (hostCanvas != null)
                canvasRoot = hostCanvas.transform;
            else
            {
                Canvas main = MainMenuController.ResolveMainCanvas();
                if (main != null)
                    canvasRoot = main.transform;
            }

            Build(canvasRoot);
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            closedCallback?.Invoke();
        }

        private void WireControlListeners()
        {
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
            uiScaleSlider.onValueChanged.RemoveAllListeners();
            uiScaleSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetUiScale(value);
                UpdatePercentLabel(uiScaleValueLabel, value);
            });
            postProcessingToggle.onValueChanged.RemoveAllListeners();
            postProcessingToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetPostProcessingEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(postProcessingToggle);
            });
            bloomToggle.onValueChanged.RemoveAllListeners();
            bloomToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetBloomEnabled(value);
                UpdateBloomIntensityRowVisibility(value);
                MenuUiBuilder.RefreshToggleVisual(bloomToggle);
            });
            fogToggle.onValueChanged.RemoveAllListeners();
            fogToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetFogEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(fogToggle);
            });
            bloomIntensitySlider.onValueChanged.RemoveAllListeners();
            bloomIntensitySlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetBloomIntensity(value);
                UpdateBloomIntensityLabel(value);
            });
            motionBlurToggle.onValueChanged.RemoveAllListeners();
            motionBlurToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMotionBlurEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(motionBlurToggle);
            });
            depthOfFieldToggle.onValueChanged.RemoveAllListeners();
            depthOfFieldToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetDepthOfFieldEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(depthOfFieldToggle);
            });
            ambientOcclusionToggle.onValueChanged.RemoveAllListeners();
            ambientOcclusionToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetAmbientOcclusionEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(ambientOcclusionToggle);
            });
            colorGradingToggle.onValueChanged.RemoveAllListeners();
            colorGradingToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetColorGradingEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(colorGradingToggle);
            });
            vignetteToggle.onValueChanged.RemoveAllListeners();
            vignetteToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetVignetteEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(vignetteToggle);
            });
            minimapToggle.onValueChanged.RemoveAllListeners();
            minimapToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMinimapEnabled(value);
                MapUI.ApplyMinimapEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(minimapToggle);
            });
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetFullscreen(value);
                MenuUiBuilder.RefreshToggleVisual(fullscreenToggle);
            });
            vsyncToggle.onValueChanged.RemoveAllListeners();
            vsyncToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetVSync(value);
                MenuUiBuilder.RefreshToggleVisual(vsyncToggle);
            });
            rayTracingToggle.onValueChanged.RemoveAllListeners();
            rayTracingToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetRayTracingEnabled(value);
                MenuUiBuilder.RefreshToggleVisual(rayTracingToggle);
                RefreshGraphicsAdvisory();
            });
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(value =>
            {
                GameSettings.PreviewQualityLevel(value);
                SyncGraphicsTogglesFromSettings();
                RefreshGraphicsAdvisory();
            });
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(choiceIndex =>
            {
                if (choiceIndex < 0 || choiceIndex >= resolutionSourceIndices.Count)
                    return;
                GameSettings.SetResolutionIndex(resolutionSourceIndices[choiceIndex]);
            });
            frameRateDropdown.onValueChanged.RemoveAllListeners();
            frameRateDropdown.onValueChanged.AddListener(index =>
            {
                int frameRate = GetFrameRateFromDropdownIndex(index);
                GameSettings.SetTargetFrameRate(frameRate);
            });

            if (dlssToggle != null)
            {
                dlssToggle.onValueChanged.RemoveAllListeners();
                dlssToggle.onValueChanged.AddListener(value =>
                {
                    GameSettings.SetDlssEnabled(value);
                    UpdateDlssControlsVisibility();
                    RefreshGraphicsAdvisory();
                });
            }

            if (dlssQualityDropdown != null)
            {
                dlssQualityDropdown.onValueChanged.RemoveAllListeners();
                dlssQualityDropdown.onValueChanged.AddListener(GameSettings.SetDlssQualityDropdownIndex);
            }
        }

        private static Transform CreateColumn(Transform parent, string name)
        {
            GameObject column = new GameObject(
                name,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            column.transform.SetParent(parent, false);

            VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 0, 0);
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement columnLayout = column.GetComponent<LayoutElement>();
            columnLayout.flexibleWidth = 1f;
            columnLayout.minWidth = 260f;

            return column.transform;
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
            scrollBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.55f);
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
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup contentLayout = content.GetComponent<HorizontalLayoutGroup>();
            contentLayout.padding = new RectOffset(6, 6, 4, 8);
            contentLayout.spacing = 16;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
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
            scrollbarBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.55f);

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
            handleImage.color = DarkMatterGenesisUiPalette.RichFuchsia;
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
            bool reloading = GameSettingsUiBridge.ApplySnapshot(CapturePanelSnapshot(), reloadSceneAfterApply: true);
            if (reloading)
                Close();
        }

        private GameSettingsUiBridge.Snapshot CapturePanelSnapshot()
        {
            GameSettingsUiBridge.Snapshot snap = GameSettingsUiBridge.CaptureCurrent();
            snap.MasterVolume = masterSlider.value;
            snap.MusicVolume = musicSlider.value;
            snap.SfxVolume = sfxSlider.value;
            snap.UiScale = uiScaleSlider.value;
            snap.PostProcessingEnabled = postProcessingToggle.isOn;
            snap.BloomEnabled = bloomToggle.isOn;
            snap.BloomIntensity = bloomIntensitySlider.value;
            snap.FogEnabled = fogToggle.isOn;
            snap.MotionBlurEnabled = motionBlurToggle.isOn;
            snap.DepthOfFieldEnabled = depthOfFieldToggle.isOn;
            snap.AmbientOcclusionEnabled = ambientOcclusionToggle.isOn;
            snap.ColorGradingEnabled = colorGradingToggle.isOn;
            snap.VignetteEnabled = vignetteToggle.isOn;
            snap.MinimapEnabled = minimapToggle.isOn;
            snap.Fullscreen = fullscreenToggle.isOn;
            snap.VSync = vsyncToggle.isOn;
            snap.RayTracingEnabled = rayTracingToggle.isOn;
            snap.TargetFrameRate = GetFrameRateFromDropdownIndex(frameRateDropdown.value);
            snap.QualityLevel = qualityDropdown.value;
            if (resolutionSourceIndices != null
                && resolutionDropdown.value >= 0
                && resolutionDropdown.value < resolutionSourceIndices.Count)
                snap.ResolutionIndex = resolutionSourceIndices[resolutionDropdown.value];
            else
                snap.ResolutionIndex = resolutionDropdown.value;
            if (dlssToggle != null)
                snap.DlssEnabled = dlssToggle.isOn;
            if (dlssQualityDropdown != null)
                snap.DlssQualityDropdownIndex = dlssQualityDropdown.value;
            return snap;
        }

        private void PushPanelValuesToGameSettings()
        {
            GameSettingsUiBridge.ApplySnapshot(CapturePanelSnapshot(), reloadSceneAfterApply: false);
        }

        private void SyncGraphicsTogglesFromSettings()
        {
            postProcessingToggle.SetIsOnWithoutNotify(GameSettings.PostProcessingEnabled);
            bloomToggle.SetIsOnWithoutNotify(GameSettings.BloomEnabled);
            fogToggle.SetIsOnWithoutNotify(GameSettings.FogEnabled);
            bloomIntensitySlider.SetValueWithoutNotify(GameSettings.BloomIntensity);
            UpdateBloomIntensityLabel(GameSettings.BloomIntensity);
            UpdateBloomIntensityRowVisibility(GameSettings.BloomEnabled);
            motionBlurToggle.SetIsOnWithoutNotify(GameSettings.MotionBlurEnabled);
            depthOfFieldToggle.SetIsOnWithoutNotify(GameSettings.DepthOfFieldEnabled);
            ambientOcclusionToggle.SetIsOnWithoutNotify(GameSettings.AmbientOcclusionEnabled);
            colorGradingToggle.SetIsOnWithoutNotify(GameSettings.ColorGradingEnabled);
            vignetteToggle.SetIsOnWithoutNotify(GameSettings.VignetteEnabled);
            rayTracingToggle.SetIsOnWithoutNotify(GameSettings.RayTracingEnabled);
            RefreshAllToggleVisuals();
        }

        private void RefreshAllToggleVisuals()
        {
            MenuUiBuilder.RefreshToggleVisual(postProcessingToggle);
            MenuUiBuilder.RefreshToggleVisual(bloomToggle);
            MenuUiBuilder.RefreshToggleVisual(fogToggle);
            MenuUiBuilder.RefreshToggleVisual(motionBlurToggle);
            MenuUiBuilder.RefreshToggleVisual(depthOfFieldToggle);
            MenuUiBuilder.RefreshToggleVisual(ambientOcclusionToggle);
            MenuUiBuilder.RefreshToggleVisual(colorGradingToggle);
            MenuUiBuilder.RefreshToggleVisual(vignetteToggle);
            MenuUiBuilder.RefreshToggleVisual(fullscreenToggle);
            MenuUiBuilder.RefreshToggleVisual(vsyncToggle);
            MenuUiBuilder.RefreshToggleVisual(rayTracingToggle);
            MenuUiBuilder.RefreshToggleVisual(minimapToggle);
        }

        private void SyncControlsFromSettings()
        {
            masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            uiScaleSlider.minValue = GameSettings.UiScaleMin;
            uiScaleSlider.maxValue = GameSettings.UiScaleMax;
            uiScaleSlider.SetValueWithoutNotify(GameSettings.UiScale);
            postProcessingToggle.SetIsOnWithoutNotify(GameSettings.PostProcessingEnabled);
            bloomToggle.SetIsOnWithoutNotify(GameSettings.BloomEnabled);
            fogToggle.SetIsOnWithoutNotify(GameSettings.FogEnabled);
            bloomIntensitySlider.SetValueWithoutNotify(GameSettings.BloomIntensity);
            UpdateBloomIntensityLabel(GameSettings.BloomIntensity);
            UpdateBloomIntensityRowVisibility(GameSettings.BloomEnabled);
            motionBlurToggle.SetIsOnWithoutNotify(GameSettings.MotionBlurEnabled);
            depthOfFieldToggle.SetIsOnWithoutNotify(GameSettings.DepthOfFieldEnabled);
            ambientOcclusionToggle.SetIsOnWithoutNotify(GameSettings.AmbientOcclusionEnabled);
            colorGradingToggle.SetIsOnWithoutNotify(GameSettings.ColorGradingEnabled);
            vignetteToggle.SetIsOnWithoutNotify(GameSettings.VignetteEnabled);
            minimapToggle.SetIsOnWithoutNotify(GameSettings.MinimapEnabled);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            vsyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            rayTracingToggle.SetIsOnWithoutNotify(GameSettings.RayTracingEnabled);
            qualityDropdown.SetValueWithoutNotify(GameSettings.QualityLevel);
            int resolutionChoice = GameSettings.FindUniqueResolutionChoiceIndex(
                resolutionSourceIndices,
                GameSettings.GetCurrentResolutionIndex());
            resolutionDropdown.SetValueWithoutNotify(resolutionChoice);
            frameRateDropdown.SetValueWithoutNotify(GetFrameRateDropdownIndex(GameSettings.TargetFrameRate));
            if (dlssToggle != null)
                dlssToggle.SetIsOnWithoutNotify(GameSettings.DlssEnabled);
            if (dlssQualityDropdown != null)
                dlssQualityDropdown.SetValueWithoutNotify(
                    DlssSettingsApplier.GetQualityDropdownIndex(GameSettings.DlssQualityIndex));
            UpdateDlssControlsVisibility();
            RefreshAllToggleVisuals();
            RefreshGraphicsAdvisory();

            UpdatePercentLabel(masterValueLabel, GameSettings.MasterVolume);
            UpdatePercentLabel(musicValueLabel, GameSettings.MusicVolume);
            UpdatePercentLabel(sfxValueLabel, GameSettings.SfxVolume);
            UpdatePercentLabel(uiScaleValueLabel, GameSettings.UiScale);
        }

        private void PopulateDropdowns()
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));

            resolutionDropdown.ClearOptions();
            List<string> resolutionLabels = new List<string>();
            GameSettings.BuildUniqueResolutionChoices(resolutionLabels, resolutionSourceIndices);
            resolutionDropdown.AddOptions(resolutionLabels);

            frameRateDropdown.ClearOptions();
            frameRateDropdown.AddOptions(new List<string>(FrameRateLabels));
        }

        private static int GetFrameRateDropdownIndex(int frameRate)
        {
            for (int i = 0; i < FrameRateOptions.Length; i++)
            {
                if (FrameRateOptions[i] == frameRate)
                    return i;
            }

            return 0;
        }

        private static int GetFrameRateFromDropdownIndex(int index)
        {
            index = Mathf.Clamp(index, 0, FrameRateOptions.Length - 1);
            return FrameRateOptions[index];
        }

        private static void CreateSectionTitle(Transform parent, string title)
        {
            TextMeshProUGUI label = MenuUiBuilder.CreateTitle(parent, title.ToUpperInvariant(), 15f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = DarkMatterGenesisUiPalette.Gold;

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
            label.color = DarkMatterGenesisUiPalette.SoftBeigeGray;
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

        private void UpdateBloomIntensityLabel(float normalizedBoost)
        {
            if (bloomIntensityValueLabel == null)
                return;

            int boostPercent = Mathf.RoundToInt(normalizedBoost * 100f);
            bloomIntensityValueLabel.text = boostPercent <= 0 ? "Default" : $"+{boostPercent}%";
        }

        private void UpdateBloomIntensityRowVisibility(bool bloomEnabled)
        {
            if (bloomIntensityRow != null)
                bloomIntensityRow.SetActive(bloomEnabled);
        }

        private void UpdateDlssControlsVisibility()
        {
            bool available = DlssSettingsApplier.IsDlssUiAvailable();
            if (dlssToggle != null)
                dlssToggle.transform.parent.gameObject.SetActive(available);

            bool showQuality = available && dlssToggle != null && dlssToggle.isOn;
            if (dlssQualityRow != null)
                dlssQualityRow.SetActive(showQuality);
        }

    }
}
