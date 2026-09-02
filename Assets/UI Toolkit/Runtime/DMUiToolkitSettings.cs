using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>UITK Settings — four equal columns (Audio / Video / Quality / Post) with live apply.</summary>
    [DisallowMultipleComponent]
    public class DMUiToolkitSettings : MonoBehaviour
    {
        public const string Name = "UITK_Settings";
        public const int Sort = 21000;
        public const string UxmlPath = "Assets/UI Toolkit/Screens/Settings.uxml";

        private static DMUiToolkitSettings instance;

        private UIDocument document;
        private VisualElement root;
        private ScrollView body;
        private Label advisoryLabel;

        private Slider masterSlider;
        private Label masterValue;
        private Slider musicSlider;
        private Label musicValue;
        private Slider sfxSlider;
        private Label sfxValue;
        private Slider uiScaleSlider;
        private Label uiScaleValue;
        private Slider bloomIntensitySlider;
        private Label bloomIntensityValue;
        private Toggle postProcessingToggle;
        private Toggle bloomToggle;
        private Toggle fogToggle;
        private Toggle motionBlurToggle;
        private Toggle depthOfFieldToggle;
        private Toggle ambientOcclusionToggle;
        private Toggle colorGradingToggle;
        private Toggle vignetteToggle;
        private Toggle minimapToggle;
        private Toggle fullscreenToggle;
        private Toggle vsyncToggle;
        private Toggle rayTracingToggle;
        private Toggle dlssToggle;
        private DropdownField qualityDropdown;
        private DropdownField resolutionDropdown;
        private DropdownField frameRateDropdown;
        private DropdownField dlssQualityDropdown;
        private VisualElement bloomIntensityRow;
        private VisualElement dlssQualityRow;

        private readonly List<string> resolutionLabels = new List<string>(32);
        private readonly List<int> resolutionSourceIndices = new List<int>(32);

        private bool bound;
        private bool open;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitSettings EnsureHost()
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return null;

            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitMenuDocument.Ensure(Name, UxmlPath, Sort);
            if (doc == null)
                return null;

            DMUiToolkitSettings host = doc.GetComponent<DMUiToolkitSettings>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitSettings>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static void Open()
        {
            DMUiToolkitSettings host = EnsureHost();
            host?.ShowInternal();
        }

        public static void Close()
        {
            instance?.HideInternal();
        }

        public static bool HandleBack()
        {
            if (!IsOpen)
                return false;

            Close();
            FindAnyObjectByType<MainMenuController>()?.RestoreMenuAfterSubPanel();
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

            root = tree.Q<VisualElement>("settings-root") ?? tree;
            body = tree.Q<ScrollView>("settings-body");
            advisoryLabel = tree.Q<Label>("settings-advisory");
            Button applyButton = tree.Q<Button>("settings-apply");
            Button backButton = tree.Q<Button>("settings-back");

            if (applyButton != null)
            {
                applyButton.clicked -= OnApplyClicked;
                applyButton.clicked += OnApplyClicked;
            }

            if (backButton != null)
            {
                backButton.clicked -= OnBackClicked;
                backButton.clicked += OnBackClicked;
            }

            if (body != null && body.Q<VisualElement>("settings-sections") == null)
                BuildControls(body);

            // Only hide during first bind / EnsureHost — ShowInternal opens after this returns.
            if (!open)
                HideInternal();
            bound = root != null;
        }

        private void BuildControls(ScrollView scroll)
        {
            scroll.Clear();

            VisualElement sections = new VisualElement { name = "settings-sections", pickingMode = PickingMode.Ignore };
            sections.AddToClassList("dmg-settings-sections");
            scroll.Add(sections);

            VisualElement audioGrid = AddSection(sections, "AUDIO");
            VisualElement videoGrid = AddSection(sections, "VIDEO");
            VisualElement qualityGrid = AddSection(sections, "QUALITY");
            VisualElement postGrid = AddSection(sections, "POST PROCESSING");

            masterSlider = AddSliderRow(audioGrid, "Master Volume", 0f, 1f, out masterValue);
            musicSlider = AddSliderRow(audioGrid, "Music Volume", 0f, 1f, out musicValue);
            sfxSlider = AddSliderRow(audioGrid, "SFX Volume", 0f, 1f, out sfxValue);
            WirePercent(masterSlider, masterValue);
            WirePercent(musicSlider, musicValue);
            WirePercent(sfxSlider, sfxValue);

            vsyncToggle = AddToggleRow(videoGrid, "V-Sync");
            fullscreenToggle = AddToggleRow(videoGrid, "Fullscreen");
            resolutionDropdown = AddDropdownRow(videoGrid, "Resolution");
            frameRateDropdown = AddDropdownRow(videoGrid, "Framerate Lock");
            frameRateDropdown.choices = new List<string>(GameSettingsUiBridge.FrameRateLabels);

            qualityDropdown = AddDropdownRow(qualityGrid, "Quality Preset");
            qualityDropdown.choices = new List<string>(QualitySettings.names);
            uiScaleSlider = AddSliderRow(qualityGrid, "UI Scale", GameSettings.UiScaleMin, GameSettings.UiScaleMax, out uiScaleValue);
            WirePercent(uiScaleSlider, uiScaleValue);
            minimapToggle = AddToggleRow(qualityGrid, "Minimap");
            rayTracingToggle = AddToggleRow(qualityGrid, "Ray Tracing");

            if (DlssSettingsApplier.IsDlssUiAvailable())
            {
                dlssToggle = AddToggleRow(qualityGrid, "DLSS");
                dlssQualityRow = new VisualElement { pickingMode = PickingMode.Ignore };
                dlssQualityRow.AddToClassList("dmg-settings-row");
                dlssQualityRow.style.width = Length.Percent(100);
                qualityGrid.Add(dlssQualityRow);
                dlssQualityDropdown = AddDropdownInto(dlssQualityRow, "DLSS Quality");
                dlssQualityDropdown.choices = new List<string>(DlssSettingsApplier.QualityDropdownLabels);
            }

            postProcessingToggle = AddToggleRow(postGrid, "Post Processing");
            bloomToggle = AddToggleRow(postGrid, "Bloom");
            bloomIntensityRow = new VisualElement { pickingMode = PickingMode.Ignore };
            bloomIntensityRow.AddToClassList("dmg-settings-row");
            bloomIntensityRow.style.width = Length.Percent(100);
            postGrid.Add(bloomIntensityRow);
            bloomIntensitySlider = AddSliderInto(bloomIntensityRow, "Bloom Intensity", 0f, 1f, out bloomIntensityValue);
            WirePercent(bloomIntensitySlider, bloomIntensityValue);
            fogToggle = AddToggleRow(postGrid, "Fog");
            motionBlurToggle = AddToggleRow(postGrid, "Motion Blur");
            depthOfFieldToggle = AddToggleRow(postGrid, "Depth of Field");
            ambientOcclusionToggle = AddToggleRow(postGrid, "Ambient Occlusion");
            colorGradingToggle = AddToggleRow(postGrid, "Color Grading");
            vignetteToggle = AddToggleRow(postGrid, "Vignette");

            bloomToggle.RegisterValueChangedCallback(evt =>
            {
                if (bloomIntensityRow != null)
                    bloomIntensityRow.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            if (dlssToggle != null)
            {
                dlssToggle.RegisterValueChangedCallback(evt =>
                {
                    if (dlssQualityRow != null)
                        dlssQualityRow.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }
        }

        private void ShowInternal()
        {
            if (!bound || document == null || root == null)
                BindTree();
            else if (body != null && body.Q<VisualElement>("settings-sections") == null)
                BuildControls(body);

            PopulateResolutionChoices();
            SyncFromSettings();
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

        private void SyncFromSettings()
        {
            GameSettingsUiBridge.Snapshot snap = GameSettingsUiBridge.CaptureCurrent();
            masterSlider?.SetValueWithoutNotify(snap.MasterVolume);
            musicSlider?.SetValueWithoutNotify(snap.MusicVolume);
            sfxSlider?.SetValueWithoutNotify(snap.SfxVolume);
            uiScaleSlider?.SetValueWithoutNotify(snap.UiScale);
            SetPercent(masterValue, snap.MasterVolume);
            SetPercent(musicValue, snap.MusicVolume);
            SetPercent(sfxValue, snap.SfxVolume);
            SetPercent(uiScaleValue, snap.UiScale);

            postProcessingToggle?.SetValueWithoutNotify(snap.PostProcessingEnabled);
            bloomToggle?.SetValueWithoutNotify(snap.BloomEnabled);
            bloomIntensitySlider?.SetValueWithoutNotify(snap.BloomIntensity);
            SetPercent(bloomIntensityValue, snap.BloomIntensity);
            fogToggle?.SetValueWithoutNotify(snap.FogEnabled);
            motionBlurToggle?.SetValueWithoutNotify(snap.MotionBlurEnabled);
            depthOfFieldToggle?.SetValueWithoutNotify(snap.DepthOfFieldEnabled);
            ambientOcclusionToggle?.SetValueWithoutNotify(snap.AmbientOcclusionEnabled);
            colorGradingToggle?.SetValueWithoutNotify(snap.ColorGradingEnabled);
            vignetteToggle?.SetValueWithoutNotify(snap.VignetteEnabled);
            minimapToggle?.SetValueWithoutNotify(snap.MinimapEnabled);
            fullscreenToggle?.SetValueWithoutNotify(snap.Fullscreen);
            vsyncToggle?.SetValueWithoutNotify(snap.VSync);
            rayTracingToggle?.SetValueWithoutNotify(snap.RayTracingEnabled);

            if (qualityDropdown != null && QualitySettings.names.Length > 0)
            {
                int q = Mathf.Clamp(snap.QualityLevel, 0, QualitySettings.names.Length - 1);
                qualityDropdown.index = q;
                qualityDropdown.SetValueWithoutNotify(QualitySettings.names[q]);
            }

            if (resolutionDropdown != null && resolutionLabels.Count > 0)
            {
                int choice = GameSettings.FindUniqueResolutionChoiceIndex(
                    resolutionSourceIndices,
                    snap.ResolutionIndex);
                choice = Mathf.Clamp(choice, 0, resolutionLabels.Count - 1);
                resolutionDropdown.index = choice;
                resolutionDropdown.SetValueWithoutNotify(resolutionLabels[choice]);
            }

            if (frameRateDropdown != null)
            {
                int fi = GameSettingsUiBridge.FrameRateDropdownIndex(snap.TargetFrameRate);
                frameRateDropdown.index = fi;
                frameRateDropdown.SetValueWithoutNotify(GameSettingsUiBridge.FrameRateLabels[fi]);
            }

            dlssToggle?.SetValueWithoutNotify(snap.DlssEnabled);
            if (dlssQualityDropdown != null)
            {
                int di = Mathf.Clamp(
                    snap.DlssQualityDropdownIndex,
                    0,
                    DlssSettingsApplier.QualityDropdownLabels.Length - 1);
                dlssQualityDropdown.index = di;
                dlssQualityDropdown.SetValueWithoutNotify(DlssSettingsApplier.QualityDropdownLabels[di]);
            }

            if (bloomIntensityRow != null)
                bloomIntensityRow.style.display = snap.BloomEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            if (dlssQualityRow != null)
                dlssQualityRow.style.display = snap.DlssEnabled ? DisplayStyle.Flex : DisplayStyle.None;

            if (advisoryLabel != null)
            {
                string summary = GameSettings.GetGraphicsAdvisorySummary();
                advisoryLabel.text = summary ?? string.Empty;
                advisoryLabel.style.display = string.IsNullOrEmpty(summary) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private GameSettingsUiBridge.Snapshot CapturePanel()
        {
            GameSettingsUiBridge.Snapshot snap = GameSettingsUiBridge.CaptureCurrent();
            if (masterSlider != null) snap.MasterVolume = masterSlider.value;
            if (musicSlider != null) snap.MusicVolume = musicSlider.value;
            if (sfxSlider != null) snap.SfxVolume = sfxSlider.value;
            if (uiScaleSlider != null) snap.UiScale = uiScaleSlider.value;
            if (postProcessingToggle != null) snap.PostProcessingEnabled = postProcessingToggle.value;
            if (bloomToggle != null) snap.BloomEnabled = bloomToggle.value;
            if (bloomIntensitySlider != null) snap.BloomIntensity = bloomIntensitySlider.value;
            if (fogToggle != null) snap.FogEnabled = fogToggle.value;
            if (motionBlurToggle != null) snap.MotionBlurEnabled = motionBlurToggle.value;
            if (depthOfFieldToggle != null) snap.DepthOfFieldEnabled = depthOfFieldToggle.value;
            if (ambientOcclusionToggle != null) snap.AmbientOcclusionEnabled = ambientOcclusionToggle.value;
            if (colorGradingToggle != null) snap.ColorGradingEnabled = colorGradingToggle.value;
            if (vignetteToggle != null) snap.VignetteEnabled = vignetteToggle.value;
            if (minimapToggle != null) snap.MinimapEnabled = minimapToggle.value;
            if (fullscreenToggle != null) snap.Fullscreen = fullscreenToggle.value;
            if (vsyncToggle != null) snap.VSync = vsyncToggle.value;
            if (rayTracingToggle != null) snap.RayTracingEnabled = rayTracingToggle.value;
            if (qualityDropdown != null && qualityDropdown.index >= 0)
                snap.QualityLevel = qualityDropdown.index;
            if (resolutionDropdown != null && resolutionDropdown.index >= 0
                && resolutionDropdown.index < resolutionSourceIndices.Count)
                snap.ResolutionIndex = resolutionSourceIndices[resolutionDropdown.index];
            if (frameRateDropdown != null)
                snap.TargetFrameRate = GameSettingsUiBridge.FrameRateOptionAt(frameRateDropdown.index);
            if (dlssToggle != null) snap.DlssEnabled = dlssToggle.value;
            if (dlssQualityDropdown != null && dlssQualityDropdown.index >= 0)
                snap.DlssQualityDropdownIndex = dlssQualityDropdown.index;
            return snap;
        }

        private void PopulateResolutionChoices()
        {
            if (resolutionDropdown == null)
                return;

            GameSettings.BuildUniqueResolutionChoices(resolutionLabels, resolutionSourceIndices);
            resolutionDropdown.choices = new List<string>(resolutionLabels);
        }

        private void OnApplyClicked()
        {
            bool reloading = GameSettingsUiBridge.ApplySnapshot(CapturePanel(), reloadSceneAfterApply: true);
            Close();
            // Title screen after Apply, even if Play-from-scene left Phase at Playing.
            // ShowMainMenu now refuses Playing/StartPopup so a New Expedition cannot bounce;
            // ResetSession first so Apply is allowed through.
            GameSession.ResetSession();
            if (!reloading)
                FindAnyObjectByType<MainMenuController>()?.ShowMainMenu();
        }

        private void OnBackClicked() => HandleBack();

        private static VisualElement AddSection(VisualElement parent, string title)
        {
            VisualElement section = new VisualElement { pickingMode = PickingMode.Ignore };
            section.AddToClassList("dmg-settings-section");
            Label header = new Label(title) { pickingMode = PickingMode.Ignore };
            header.AddToClassList("dmg-settings-section-header");
            section.Add(header);
            VisualElement grid = new VisualElement { pickingMode = PickingMode.Ignore };
            grid.AddToClassList("dmg-settings-section-grid");
            section.Add(grid);
            parent.Add(section);
            return grid;
        }

        private static Toggle AddToggleRow(VisualElement parent, string labelText)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dmg-settings-row");
            Label label = new Label(labelText) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("dmg-settings-label");
            Toggle toggle = new Toggle { pickingMode = PickingMode.Position };
            toggle.AddToClassList("dmg-settings-toggle");
            row.Add(label);
            row.Add(toggle);
            parent.Add(row);
            return toggle;
        }

        private static Slider AddSliderRow(
            VisualElement parent,
            string labelText,
            float low,
            float high,
            out Label valueLabel)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dmg-settings-row");
            parent.Add(row);
            return AddSliderInto(row, labelText, low, high, out valueLabel);
        }

        private static Slider AddSliderInto(
            VisualElement row,
            string labelText,
            float low,
            float high,
            out Label valueLabel)
        {
            Label label = new Label(labelText) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("dmg-settings-label");
            Slider slider = new Slider(low, high)
            {
                pickingMode = PickingMode.Position,
                direction = SliderDirection.Horizontal
            };
            slider.AddToClassList("dmg-settings-slider");
            valueLabel = new Label { pickingMode = PickingMode.Ignore };
            valueLabel.AddToClassList("dmg-settings-value");
            row.Add(label);
            row.Add(slider);
            row.Add(valueLabel);
            return slider;
        }

        private static DropdownField AddDropdownRow(VisualElement parent, string labelText)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dmg-settings-row");
            parent.Add(row);
            return AddDropdownInto(row, labelText);
        }

        private static DropdownField AddDropdownInto(VisualElement row, string labelText)
        {
            Label label = new Label(labelText) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("dmg-settings-label");
            DropdownField dropdown = new DropdownField { pickingMode = PickingMode.Position };
            dropdown.AddToClassList("dmg-settings-dropdown");
            row.Add(label);
            row.Add(dropdown);
            return dropdown;
        }

        private static void WirePercent(Slider slider, Label valueLabel)
        {
            if (slider == null || valueLabel == null)
                return;
            slider.RegisterValueChangedCallback(evt => SetPercent(valueLabel, evt.newValue));
        }

        private static void SetPercent(Label valueLabel, float value)
        {
            if (valueLabel != null)
                valueLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
