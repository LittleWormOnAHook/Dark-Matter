using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class MainMenuEnvironmentStatusBar : MonoBehaviour
    {
        private TextMeshProUGUI zoneLabel;
        private TextMeshProUGUI tempLabel;
        private TextMeshProUGUI conditionLabel;
        private TextMeshProUGUI hazardsLabel;
        private int zoneIndex;

        public void Build(Transform parent)
        {
            if (zoneLabel != null)
                return;

            GameObject bar = new GameObject("EnvironmentStatusBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(parent, false);

            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 24f);
            barRect.sizeDelta = new Vector2(720f, 48f);

            Image barBg = bar.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(barBg);
            barBg.color = SurvivalPioneerUiPalette.PanelBackground;

            HorizontalLayoutGroup layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            zoneLabel = CreateStatLabel(bar.transform, "ZONE —", 14f);
            tempLabel = CreateStatLabel(bar.transform, "TEMP —", 14f);
            conditionLabel = CreateStatLabel(bar.transform, "STATUS —", 14f);
            hazardsLabel = CreateStatLabel(bar.transform, "HAZARDS —", 13f);

            RefreshZoneDisplay();
        }

        public void CycleZone()
        {
            zoneIndex = (zoneIndex + 1) % MainMenuZoneProfile.GetDefaultZones().Count;
            RefreshZoneDisplay();
        }

        private void RefreshZoneDisplay()
        {
            IReadOnlyList<MainMenuZoneProfile> zones = MainMenuZoneProfile.GetDefaultZones();
            if (zones == null || zones.Count == 0)
                return;

            MainMenuZoneProfile zone = zones[Mathf.Clamp(zoneIndex, 0, zones.Count - 1)];
            zoneLabel.text = $"ZONE {zone.zoneId}";
            tempLabel.text = $"{zone.temperatureC:0}°C";
            conditionLabel.text = zone.surfaceCondition;
            conditionLabel.color = zone.surfaceCondition == "SAFE"
                ? SurvivalPioneerUiPalette.PositiveGreen
                : SurvivalPioneerUiPalette.DangerRed;
            hazardsLabel.text = zone.hazardsText;
        }

        private static TextMeshProUGUI CreateStatLabel(Transform parent, string text, float fontSize)
        {
            GameObject labelObj = new GameObject("Stat", typeof(RectTransform));
            labelObj.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(label, semiBold: true);
            label.text = text;
            label.fontSize = fontSize;
            label.color = SurvivalPioneerUiPalette.BodyText;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            LayoutElement layout = labelObj.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            return label;
        }
    }
}
