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
        private GameObject panelRoot;
        private Slider masterSlider;
        private Slider musicSlider;
        private Slider sfxSlider;
        private Toggle postProcessingToggle;
        private Toggle mapSystemToggle;
        private Toggle fullscreenToggle;
        private Toggle vsyncToggle;
        private Dropdown qualityDropdown;
        private Dropdown resolutionDropdown;
        private TextMeshProUGUI masterValueLabel;
        private TextMeshProUGUI musicValueLabel;
        private TextMeshProUGUI sfxValueLabel;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Build(Transform parent)
        {
            if (panelRoot != null)
                return;

            panelRoot = MenuUiBuilder.CreateFullScreenPanel(parent, "SettingsPanel", new Color(0f, 0f, 0f, 0.92f), blockRaycasts: true);

            GameObject window = new GameObject("SettingsWindow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            window.transform.SetParent(panelRoot.transform, false);
            Image windowImage = window.GetComponent<Image>();
            windowImage.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = Vector2.zero;
            windowRect.anchorMax = Vector2.one;
            windowRect.offsetMin = Vector2.zero;
            windowRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup windowLayout = window.GetComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(
                MenuUiBuilder.ScaledSizeInt(24f),
                MenuUiBuilder.ScaledSizeInt(24f),
                MenuUiBuilder.ScaledSizeInt(24f),
                MenuUiBuilder.ScaledSizeInt(24f));
            windowLayout.spacing = MenuUiBuilder.ScaledSizeInt(12f);
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            MenuUiBuilder.CreateTitle(window.transform, "Settings", MenuUiBuilder.ScaledSize(40f));

            CreateSectionTitle(window.transform, "Audio");
            masterSlider = MenuUiBuilder.CreateSliderRow(window.transform, "Master Volume", GameSettings.MasterVolume, out masterValueLabel);
            musicSlider = MenuUiBuilder.CreateSliderRow(window.transform, "Music Volume", GameSettings.MusicVolume, out musicValueLabel);
            sfxSlider = MenuUiBuilder.CreateSliderRow(window.transform, "SFX Volume", GameSettings.SfxVolume, out sfxValueLabel);

            CreateSectionTitle(window.transform, "Graphics");
            qualityDropdown = MenuUiBuilder.CreateDropdownRow(window.transform, "Quality");
            resolutionDropdown = MenuUiBuilder.CreateDropdownRow(window.transform, "Resolution");
            fullscreenToggle = MenuUiBuilder.CreateToggleRow(window.transform, "Fullscreen", GameSettings.Fullscreen);
            vsyncToggle = MenuUiBuilder.CreateToggleRow(window.transform, "VSync", GameSettings.VSync);
            postProcessingToggle = MenuUiBuilder.CreateToggleRow(window.transform, "Post Processing", GameSettings.PostProcessingEnabled);

            CreateSectionTitle(window.transform, "Gameplay");
            mapSystemToggle = MenuUiBuilder.CreateToggleRow(window.transform, "Map System", GameSettings.MapSystemEnabled);

            GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(window.transform, false);
            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = MenuUiBuilder.ScaledSizeInt(16f);
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = false;

            Button applyButton = MenuUiBuilder.CreateButton(
                buttonRow.transform,
                "Apply",
                new Vector2(MenuUiBuilder.ScaledSize(160f), MenuUiBuilder.ScaledSize(48f)),
                MenuUiBuilder.ScaledSize(24f));
            Button backButton = MenuUiBuilder.CreateButton(
                buttonRow.transform,
                "Back",
                new Vector2(MenuUiBuilder.ScaledSize(160f), MenuUiBuilder.ScaledSize(48f)),
                MenuUiBuilder.ScaledSize(24f));

            applyButton.onClick.AddListener(ApplySettings);
            backButton.onClick.AddListener(Close);

            masterSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMasterVolume(value);
                UpdatePercentLabel(masterValueLabel, value);
            });
            musicSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMusicVolume(value);
                GameAudioManager.Instance?.RefreshVolumes();
                UpdatePercentLabel(musicValueLabel, value);
            });
            sfxSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetSfxVolume(value);
                GameAudioManager.Instance?.RefreshVolumes();
                UpdatePercentLabel(sfxValueLabel, value);
            });
            postProcessingToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetPostProcessingEnabled(value);
                PostProcessingController.Instance?.ApplyFromSettings();
            });
            mapSystemToggle.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMapSystemEnabled(value);
                MapUI.ApplyMapSystemEnabled(value);
            });
            fullscreenToggle.onValueChanged.AddListener(GameSettings.SetFullscreen);
            vsyncToggle.onValueChanged.AddListener(GameSettings.SetVSync);
            qualityDropdown.onValueChanged.AddListener(GameSettings.SetQualityLevel);
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
            mapSystemToggle.SetIsOnWithoutNotify(GameSettings.MapSystemEnabled);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            vsyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);
            qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
            resolutionDropdown.SetValueWithoutNotify(GameSettings.GetCurrentResolutionIndex());

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
            TextMeshProUGUI label = MenuUiBuilder.CreateTitle(parent, title, MenuUiBuilder.ScaledSize(24f));
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.85f, 0.68f, 0.18f, 1f);
        }

        private static void UpdatePercentLabel(TextMeshProUGUI label, float value)
        {
            if (label != null)
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }
}
