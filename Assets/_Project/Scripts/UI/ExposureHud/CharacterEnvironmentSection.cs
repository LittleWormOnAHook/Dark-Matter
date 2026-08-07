using System.Collections.Generic;
using Project.Survival;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Journal Character tab environment block: mini gauges, zone line, modifier chips.
    /// </summary>
    public class CharacterEnvironmentSection : MonoBehaviour
    {
        private VerticalThermalNeedleGauge thermalGauge;
        private VerticalHazardExposureGauge hazardGauge;
        private TextMeshProUGUI zoneLabel;
        private ExposureModifierTickGrid buffGrid;
        private ExposureModifierTickGrid debuffGrid;
        private readonly List<CompanionModifierRowView> companionRows = new List<CompanionModifierRowView>(3);
        private bool built;

        public void Initialize()
        {
            if (built)
                return;

            Build();
            ExposureStatusService service = ExposureStatusService.Instance;
            if (service != null)
                service.OnSnapshotChanged += HandleSnapshotChanged;

            Refresh(ExposureStatusService.Current);
            built = true;
        }

        public void Unembed()
        {
            ExposureStatusService service = ExposureStatusService.Instance;
            if (service != null)
                service.OnSnapshotChanged -= HandleSnapshotChanged;

            built = false;
        }

        public void RefreshFromStats(SurvivalStats stats)
        {
            Refresh(ExposureStatusService.Current);
        }

        private void HandleSnapshotChanged(ExposureStatusSnapshot snapshot)
        {
            Refresh(snapshot);
        }

        private void Refresh(ExposureStatusSnapshot snapshot)
        {
            if (!built)
                return;

            thermalGauge?.Refresh(snapshot);
            hazardGauge?.Refresh(snapshot);

            if (zoneLabel != null)
            {
                if (snapshot.ActiveZoneNames != null && snapshot.ActiveZoneNames.Length > 0)
                    zoneLabel.text = $"Active zones: {string.Join(", ", snapshot.ActiveZoneNames)}";
                else if (snapshot.DominantHazard.IsClear)
                    zoneLabel.text = "Environment: EVA nominal";
                else
                    zoneLabel.text = $"Environment: {snapshot.DominantHazard.DisplayName}";
            }

            var buffs = snapshot.PlayerBuffTicks ?? System.Array.Empty<ExposureModifierTick>();
            var debuffs = snapshot.PlayerDebuffTicks ?? System.Array.Empty<ExposureModifierTick>();
            buffGrid?.SetTicks(buffs, "No crew buffs active");
            debuffGrid?.SetTicks(debuffs, "No exposure debuffs");

            RefreshCompanionRows(snapshot.ExpeditionCompanionSlots);
        }

        private void RefreshCompanionRows(CompanionExposureModifierSlot[] companionSlots)
        {
            for (int i = 0; i < companionRows.Count; i++)
            {
                CompanionModifierRowView row = companionRows[i];
                CompanionExposureModifierSlot slot = companionSlots != null && i < companionSlots.Length
                    ? companionSlots[i]
                    : null;

                if (row.NameLabel != null)
                {
                    row.NameLabel.text = slot != null && !string.IsNullOrWhiteSpace(slot.DisplayName)
                        ? slot.DisplayName
                        : $"Companion slot {i + 1}";
                }

                row.BuffGrid?.SetTicks(
                    slot?.BuffTicks ?? System.Array.Empty<ExposureModifierTick>(),
                    "No buffs");
                row.DebuffGrid?.SetTicks(
                    slot?.DebuffTicks ?? System.Array.Empty<ExposureModifierTick>(),
                    "No debuffs");
            }
        }

        private void Build()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            VerticalLayoutGroup rootLayout = gameObject.GetComponent<VerticalLayoutGroup>();
            if (rootLayout == null)
                rootLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 10f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            LayoutElement rootLayoutElement = gameObject.GetComponent<LayoutElement>();
            if (rootLayoutElement == null)
                rootLayoutElement = gameObject.AddComponent<LayoutElement>();
            // Environment gauges now match the player's full hotbar HUD size (see BuildEnvironmentBlock),
            // so this needs considerably more room than the old tiny-compact-gauge layout did.
            rootLayoutElement.minHeight = HudLayoutMetrics.Scaled(560f);
            rootLayoutElement.flexibleHeight = 1f;

            BuildEnvironmentBlock();
            BuildModifierBlock("Crew buffs", out buffGrid);
            BuildModifierBlock("Exposure debuffs", out debuffGrid);
            BuildExpeditionBlock();
        }

        private void BuildEnvironmentBlock()
        {
            Transform section = CreateSectionFrame("EnvironmentSection", "Environment");

            GameObject gaugeRow = new GameObject("GaugeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            gaugeRow.transform.SetParent(section, false);
            LayoutElement gaugeRowLayout = gaugeRow.GetComponent<LayoutElement>();
            // Non-compact gauges (see Configure calls below) render at the same size as the player's
            // real hotbar HUD (~407 raw units tall for the hazard panel at HotbarPanelScale) — this
            // reserves enough row height so nothing gets visually clipped/overlapped.
            gaugeRowLayout.minHeight = HudLayoutMetrics.Scaled(410f);
            gaugeRowLayout.preferredHeight = HudLayoutMetrics.Scaled(410f);

            HorizontalLayoutGroup gaugeLayout = gaugeRow.GetComponent<HorizontalLayoutGroup>();
            gaugeLayout.spacing = 12f;
            gaugeLayout.childAlignment = TextAnchor.LowerLeft;
            gaugeLayout.childControlWidth = false;
            gaugeLayout.childControlHeight = false;
            gaugeLayout.padding = new RectOffset(4, 4, 0, 0);

            GameObject thermalObject = new GameObject("ThermalGauge", typeof(RectTransform), typeof(VerticalThermalNeedleGauge), typeof(LayoutElement));
            thermalObject.transform.SetParent(gaugeRow.transform, false);
            thermalGauge = thermalObject.GetComponent<VerticalThermalNeedleGauge>();
            // Same non-compact configuration as the player's actual hotbar temp/hazard HUD, per request
            // to reuse "the ui from the player's UI" here instead of the old shrunken Journal variant.
            thermalGauge.Configure(compact: false);

            GameObject hazardObject = new GameObject("HazardGauge", typeof(RectTransform), typeof(VerticalHazardExposureGauge), typeof(LayoutElement));
            hazardObject.transform.SetParent(gaugeRow.transform, false);
            hazardGauge = hazardObject.GetComponent<VerticalHazardExposureGauge>();
            hazardGauge.Configure(compact: false, HazardHudIconSet.LoadDefault());

            zoneLabel = CreateBodyLabel(section, "ZoneLabel", "Environment: EVA nominal");
        }

        private void BuildModifierBlock(string heading, out ExposureModifierTickGrid grid)
        {
            Transform section = CreateSectionFrame($"{heading.Replace(" ", string.Empty)}Section", heading);
            grid = CreateTickGrid(section, "TickGrid");
        }

        private void BuildExpeditionBlock()
        {
            Transform section = CreateSectionFrame("ExpeditionSection", "Expedition crew");

            GameObject rowStrip = new GameObject("CompanionStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowStrip.transform.SetParent(section, false);
            LayoutElement stripLayout = rowStrip.GetComponent<LayoutElement>();
            stripLayout.minHeight = HudLayoutMetrics.Scaled(110f);

            HorizontalLayoutGroup rowStripLayout = rowStrip.GetComponent<HorizontalLayoutGroup>();
            rowStripLayout.spacing = 8f;
            rowStripLayout.childControlWidth = true;
            rowStripLayout.childControlHeight = true;
            rowStripLayout.childForceExpandWidth = true;
            rowStripLayout.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
                companionRows.Add(CreateCompanionCard(rowStrip.transform, i));
        }

        private CompanionModifierRowView CreateCompanionCard(Transform parent, int slotIndex)
        {
            GameObject cardObject = new GameObject($"CompanionCard_{slotIndex + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            cardObject.transform.SetParent(parent, false);

            Image cardBg = cardObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(cardBg);
            cardBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.45f);

            LayoutElement cardLayout = cardObject.GetComponent<LayoutElement>();
            cardLayout.flexibleWidth = 1f;
            cardLayout.minHeight = HudLayoutMetrics.Scaled(96f);

            VerticalLayoutGroup cardGroup = cardObject.GetComponent<VerticalLayoutGroup>();
            cardGroup.spacing = 3f;
            cardGroup.padding = new RectOffset(6, 6, 6, 6);
            cardGroup.childControlWidth = true;
            cardGroup.childControlHeight = true;
            cardGroup.childForceExpandWidth = true;
            cardGroup.childForceExpandHeight = false;

            TextMeshProUGUI nameLabel = CreateBodyLabel(cardObject.transform, $"CompanionName_{slotIndex + 1}", $"Companion slot {slotIndex + 1}");
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.color = SurvivalPioneerUiPalette.BodyText;
            nameLabel.alignment = TextAlignmentOptions.Top;

            ExposureModifierTickGrid rowBuffGrid = CreateTickGrid(cardObject.transform, $"CompanionBuffGrid_{slotIndex + 1}");
            ExposureModifierTickGrid rowDebuffGrid = CreateTickGrid(cardObject.transform, $"CompanionDebuffGrid_{slotIndex + 1}");

            return new CompanionModifierRowView
            {
                NameLabel = nameLabel,
                BuffGrid = rowBuffGrid,
                DebuffGrid = rowDebuffGrid
            };
        }

        private Transform CreateSectionFrame(string objectName, string heading)
        {
            GameObject sectionObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            sectionObject.transform.SetParent(transform, false);

            Image sectionBg = sectionObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(sectionBg);
            sectionBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.55f);

            VerticalLayoutGroup sectionLayout = sectionObject.GetComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 6f;
            sectionLayout.padding = new RectOffset(10, 10, 8, 10);
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            TextMeshProUGUI headingLabel = CreateBodyLabel(sectionObject.transform, "Heading", heading);
            headingLabel.fontSize = JournalPanelLayout.HeaderFontSize;
            headingLabel.fontStyle = FontStyles.Bold;
            headingLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            headingLabel.alignment = TextAlignmentOptions.MidlineLeft;

            return sectionObject.transform;
        }

        private TextMeshProUGUI CreateBodyLabel(Transform parent, string name, string text)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.minHeight = 18f;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = JournalPanelLayout.SecondaryFontSize;
            label.fontStyle = FontStyles.Normal;
            label.color = SurvivalPioneerUiPalette.MutedText;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private ExposureModifierTickGrid CreateTickGrid(Transform parent, string name)
        {
            GameObject gridObject = new GameObject(name, typeof(RectTransform), typeof(ExposureModifierTickGrid), typeof(LayoutElement));
            gridObject.transform.SetParent(parent, false);
            LayoutElement layout = gridObject.GetComponent<LayoutElement>();
            layout.minHeight = HudLayoutMetrics.Scaled(24f);
            layout.preferredHeight = HudLayoutMetrics.Scaled(24f);
            return gridObject.GetComponent<ExposureModifierTickGrid>();
        }

        private sealed class CompanionModifierRowView
        {
            public TextMeshProUGUI NameLabel;
            public ExposureModifierTickGrid BuffGrid;
            public ExposureModifierTickGrid DebuffGrid;
        }
    }
}
