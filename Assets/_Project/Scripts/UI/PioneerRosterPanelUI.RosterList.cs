using System.Collections;
using System.Collections.Generic;
using System.Text;
using Project.Building;
using Project.Data;
using Project.Pioneers;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    // The four grouped roster browser columns (By Class / Echoes / Expedition Trio / At Camp), the
    // colonist summary line, and roster-row selection/drag/tooltip entry points used by
    // PioneerRosterRowDragHandler and PioneerHoverTooltip. Split out of PioneerRosterPanelUI.cs.
    public partial class PioneerRosterPanelUI
    {
        internal string GetTrioDraftId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PioneerRosterManager.ExpeditionTrioSize)
                return string.Empty;

            return trioDraft[slotIndex] ?? string.Empty;
        }

        internal SkilledPioneerRecord GetPioneerRecordForTooltip(string pioneerId)
        {
            return string.IsNullOrEmpty(pioneerId) ? null : roster?.FindSkilledById(pioneerId);
        }

        internal void OnDragStarted(string pioneerId)
        {
            selectedPioneerId = pioneerId;
            RefreshRosterList();
            RefreshDetailPanel();
            RefreshLoadoutPanel();
        }

        internal void OnDragEnded()
        {
            Refresh();
        }

        internal void SelectPioneer(string pioneerId)
        {
            selectedPioneerId = pioneerId;
            Refresh();
        }

        private void RefreshColonistSummary()
        {
            ColonistAggregateState colonists = roster.GetColonistState();
            int total = roster.GetTotalPioneerCount();
            colonistSummaryLabel.text =
                $"Total {total}/{PioneerRosterManager.MaxTotalPioneers}  ·  " +
                $"Skilled {roster.SkilledPioneers.Count}/{PioneerRosterManager.MaxSkilledPioneers}\n" +
                $"Workers {colonists.workerCount}/{PioneerRosterManager.MaxWorkerPioneers}  ·  " +
                $"Available {colonists.AvailableWorkers}  ·  " +
                $"Injured {colonists.injuredCount}  ·  " +
                $"Sheltered {colonists.shelteredCount}  ·  " +
                $"Assigned {colonists.assignedToFacilityCount}";
        }

        /// <summary>
        /// Refreshes all four grouped roster browser columns (Class / Echoes / Trio / Camp Building).
        /// Kept as one entry point since every existing call site just wants "the roster list(s)
        /// re-drawn" — the actual grouping/columns live in RefreshClassColumn/RefreshEchoColumn/
        /// RefreshTrioColumn/RefreshCampColumn below.
        /// </summary>
        private void RefreshRosterList()
        {
            RefreshClassColumn();
            RefreshEchoColumn();
            RefreshTrioColumn();
            RefreshCampColumn();
        }

        private void RefreshClassColumn()
        {
            if (classListParent == null)
                return;

            ClearChildren(classListParent);

            Dictionary<SkilledPioneerClass, List<SkilledPioneerRecord>> byClass =
                new Dictionary<SkilledPioneerClass, List<SkilledPioneerRecord>>();
            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record == null)
                    continue;

                if (!byClass.TryGetValue(record.pioneerClass, out List<SkilledPioneerRecord> bucket))
                {
                    bucket = new List<SkilledPioneerRecord>();
                    byClass[record.pioneerClass] = bucket;
                }

                bucket.Add(record);
            }

            if (byClass.Count == 0)
            {
                CreateInfoRow(classListParent, "No skilled pioneers recruited yet.");
                return;
            }

            foreach (KeyValuePair<SkilledPioneerClass, List<SkilledPioneerRecord>> pair in byClass)
            {
                CreateGroupHeaderRow(classListParent, SkilledPioneerClassUtility.ToDisplayName(pair.Key));
                for (int i = 0; i < pair.Value.Count; i++)
                    CreateRosterRow(classListParent, pair.Value[i]);
            }
        }

        private void RefreshEchoColumn()
        {
            if (echoListParent == null)
                return;

            ClearChildren(echoListParent);

            bool any = false;
            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record == null || record.Kind != PioneerKind.RescuedEcho)
                    continue;

                any = true;
                CreateRosterRow(echoListParent, record);
            }

            if (!any)
                CreateInfoRow(echoListParent, "No rescued Echoes yet — sync and rescue signals out in the world.");
        }

        private void RefreshTrioColumn()
        {
            if (trioListParent == null)
                return;

            ClearChildren(trioListParent);

            bool any = false;
            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                SkilledPioneerRecord record = roster.GetExpeditionTrioRecordAtSlot(i);
                if (record == null)
                    continue;

                any = true;
                CreateRosterRow(trioListParent, record, subtitleOverride: $"Active — Slot {i + 1}");
            }

            if (!any)
                CreateInfoRow(trioListParent, "Trio is empty. Drag pioneers into the slots on the right.");
        }

        private void RefreshCampColumn()
        {
            if (campListParent == null)
                return;

            ClearChildren(campListParent);

            Dictionary<string, string> buildingNames = BuildPlacedBuildingNameLookup();
            Dictionary<string, List<SkilledPioneerRecord>> byBuilding = new Dictionary<string, List<SkilledPioneerRecord>>();
            List<SkilledPioneerRecord> unassigned = new List<SkilledPioneerRecord>();

            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record == null || record.isInExpeditionTrio || record.WorkState == PioneerWorkState.Injured)
                    continue;

                string buildingId = BuildingOperationRegistry.FindAssignedBuildingId(record.id);
                if (string.IsNullOrEmpty(buildingId))
                {
                    unassigned.Add(record);
                    continue;
                }

                if (!byBuilding.TryGetValue(buildingId, out List<SkilledPioneerRecord> bucket))
                {
                    bucket = new List<SkilledPioneerRecord>();
                    byBuilding[buildingId] = bucket;
                }

                bucket.Add(record);
            }

            if (byBuilding.Count == 0 && unassigned.Count == 0)
            {
                CreateInfoRow(campListParent, "No one is benched at camp right now.");
                return;
            }

            foreach (KeyValuePair<string, List<SkilledPioneerRecord>> pair in byBuilding)
            {
                string label = buildingNames.TryGetValue(pair.Key, out string niceName) ? niceName : pair.Key;
                CreateGroupHeaderRow(campListParent, label);
                for (int i = 0; i < pair.Value.Count; i++)
                    CreateRosterRow(campListParent, pair.Value[i], subtitleOverride: "Working");
            }

            if (unassigned.Count > 0)
            {
                CreateGroupHeaderRow(campListParent, "Unassigned");
                for (int i = 0; i < unassigned.Count; i++)
                    CreateRosterRow(campListParent, unassigned[i], subtitleOverride: "Idle at camp");
            }
        }

        /// <summary>Building id → display name for every BuildingControlPanel currently placed in the
        /// scene, used to label the Camp Building column's group headers.</summary>
        private static Dictionary<string, string> BuildPlacedBuildingNameLookup()
        {
            Dictionary<string, string> names = new Dictionary<string, string>();
            BuildingControlPanel[] panels = FindObjectsByType<BuildingControlPanel>(FindObjectsInactive.Exclude);
            for (int i = 0; i < panels.Length; i++)
            {
                BuildingControlPanel panel = panels[i];
                if (panel == null || string.IsNullOrEmpty(panel.BuildingId))
                    continue;

                names[panel.BuildingId] = string.IsNullOrEmpty(panel.BuildingDisplayName)
                    ? panel.BuildingId
                    : panel.BuildingDisplayName;
            }

            return names;
        }

        private void CreateGroupHeaderRow(Transform parent, string text)
        {
            GameObject header = new GameObject("GroupHeader", typeof(RectTransform), typeof(LayoutElement));
            header.transform.SetParent(parent, false);
            header.GetComponent<LayoutElement>().minHeight = 20f;

            TextMeshProUGUI label = CreateLabel(header.transform, text, 11f, semiBold: true);
            label.color = DarkMatterGenesisUiPalette.HighlightText;
            Stretch(label.rectTransform, 4f, 2f);
        }

        private void CreateRosterRow(Transform parent, SkilledPioneerRecord record, string subtitleOverride = null)
        {
            bool selected = record.id == selectedPioneerId;
            bool inTrio = record.isInExpeditionTrio;

            GameObject row = new GameObject($"Pioneer_{record.id}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);

            Image bg = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = selected
                ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.35f)
                : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.96f);

            if (selected)
                DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(row);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = RosterPortraitSize + 16f;
            rowLayout.preferredHeight = RosterPortraitSize + 16f;

            HorizontalLayoutGroup rowGroup = row.GetComponent<HorizontalLayoutGroup>();
            rowGroup.padding = new RectOffset(6, 6, 5, 5);
            rowGroup.spacing = 8f;
            rowGroup.childAlignment = TextAnchor.MiddleLeft;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            // Keep portrait aspect — force-expand stretches the RawImage into an ellipse.
            rowGroup.childForceExpandHeight = false;

            RawImage photo = PioneerPortraitUi.CreateCircularPortrait(row.transform, RosterPortraitSize);
            Image frame = PioneerPortraitUi.GetMaskImage(photo);
            PioneerPortraitUi.ApplyPortrait(frame, photo, null, record);

            string starterTag = record.isStarterPick ? " [Starter]" : string.Empty;
            string trioTag = inTrio && subtitleOverride == null ? "  ·  TRIO" : string.Empty;
            string stateTag = record.WorkState == PioneerWorkState.Injured ? "  ·  INJURED" : string.Empty;
            string subtitle = subtitleOverride ?? $"{SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)}  ·  Lv {record.level}";

            string nameHex = ColorUtility.ToHtmlStringRGB(
                selected ? DarkMatterGenesisUiPalette.WarmOffWhite : DarkMatterGenesisUiPalette.RichFuchsia);
            string goldHex = ColorUtility.ToHtmlStringRGB(DarkMatterGenesisUiPalette.Gold);
            string secondaryBody = BuildRosterRowSecondaryBody(record, subtitle);

            TextMeshProUGUI label = CreateLabel(row.transform, string.Empty, 12f, semiBold: selected);
            label.color = DarkMatterGenesisUiPalette.Gold;
            label.text =
                $"<color=#{nameHex}>{PioneerUiLabels.GetDisplayName(record)}</color>" +
                $"<color=#{goldHex}>{starterTag}{trioTag}{stateTag}</color>\n" +
                secondaryBody;
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            if (labelLayout != null)
                labelLayout.flexibleWidth = 1f;

            SkilledPioneerRecord captured = record;
            row.GetComponent<Button>().onClick.AddListener(() => HandleRosterEntryClicked(captured));

            PioneerRosterRowDragHandler drag = row.AddComponent<PioneerRosterRowDragHandler>();
            drag.Configure(this, record.id);
        }

        private static string BuildRosterRowSecondaryBody(SkilledPioneerRecord record, string subtitleLine)
        {
            string goldHex = ColorUtility.ToHtmlStringRGB(DarkMatterGenesisUiPalette.Gold);
            string statsLine =
                $"Rad {record.radiationResistance:P0}  ·  Exp {record.expeditionEfficiency:P0}  ·  Syn {record.combatSynergy:P0}";

            return
                $"<color=#{goldHex}>{subtitleLine}</color>\n" +
                $"<color=#{goldHex}>{statsLine}</color>";
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private void HandleRosterEntryClicked(SkilledPioneerRecord record)
        {
            if (record == null)
                return;

            if (pendingTrioSlot >= 0)
            {
                AssignToTrioSlot(pendingTrioSlot, record);
                return;
            }

            selectedPioneerId = record.id;
            RefreshRosterList();
            RefreshDetailPanel();
            RefreshLoadoutPanel();
        }

        private void CreateInfoRow(Transform parent, string message)
        {
            GameObject row = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().minHeight = 40f;
            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label);
            label.fontSize = 11.5f;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.color = DarkMatterGenesisUiPalette.Gold;
            label.text = message;
        }
    }
}
