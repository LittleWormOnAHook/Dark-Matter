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
    // Runtime UI construction for the whole panel: the left column (grouped roster browser +
    // colonist summary) and right column (detail, personal loadout, trio picker, trio loadout mini
    // panels), plus the small shared widget-builder helpers (labels, scroll areas, slot buttons).
    // Split out of PioneerRosterPanelUI.cs — pure "build the hierarchy" code, no gameplay state.
    public partial class PioneerRosterPanelUI
    {
        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            theme = ShiftUiTheme.Current;

            panelRoot = new GameObject("PioneerRosterPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            JournalPanelLayout.StretchFill(panelRoot.GetComponent<RectTransform>());

            Image panelBg = panelRoot.GetComponent<Image>();
            JournalPanelLayout.StylePanelBackground(panelBg, theme);

            HorizontalLayoutGroup splitLayout = panelRoot.AddComponent<HorizontalLayoutGroup>();
            JournalPanelLayout.ApplyRootHorizontalLayout(splitLayout);

            // Left side now hosts the four grouped browser columns (Class / Echoes / Trio / Camp
            // Building) requested alongside the existing detail+loadout+trio-picker on the right.
            GameObject leftColumn = CreateColumn(panelRoot.transform, flexibleWidth: 0.60f);
            BuildLeftColumn(leftColumn.transform);

            GameObject rightColumn = CreateColumn(panelRoot.transform, flexibleWidth: 0.40f);
            BuildRightColumn(rightColumn.transform);
        }

        private void BuildLeftColumn(Transform parent)
        {
            VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
            layout.spacing = JournalPanelLayout.SectionSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = CreateLabel(parent, "Skilled Roster", JournalPanelLayout.HeaderFontSize, semiBold: true);
            JournalPanelLayout.ApplyHeaderStyle(header);

            // Four always-visible grouped browser columns: by Class, rescued Echoes, the active
            // Expedition Trio (quick view — drag/assign still happens in the picker on the right),
            // and who's benched At Camp grouped by the building they're currently working.
            GameObject columnsRow = new GameObject("GroupedColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            columnsRow.transform.SetParent(parent, false);
            LayoutElement columnsRowLayout = columnsRow.GetComponent<LayoutElement>();
            columnsRowLayout.flexibleHeight = 1f;
            columnsRowLayout.minHeight = 220f;

            HorizontalLayoutGroup columnsLayout = columnsRow.GetComponent<HorizontalLayoutGroup>();
            columnsLayout.spacing = 5f;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = true;

            classListParent = BuildScrollableSubColumn(columnsRow.transform, "By Class");
            echoListParent = BuildScrollableSubColumn(columnsRow.transform, "Echoes");
            trioListParent = BuildScrollableSubColumn(columnsRow.transform, "Expedition Trio");
            campListParent = BuildScrollableSubColumn(columnsRow.transform, "At Camp");

            GameObject colonistRow = new GameObject("ColonistSummary", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            colonistRow.transform.SetParent(parent, false);
            colonistRow.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.98f);
            colonistRow.GetComponent<LayoutElement>().minHeight = 52f;

            colonistSummaryLabel = CreateLabel(colonistRow.transform, string.Empty, JournalPanelLayout.SecondaryFontSize);
            colonistSummaryLabel.color = SurvivalPioneerUiPalette.BodyText;
            Stretch(colonistSummaryLabel.rectTransform, 10f, 8f);
        }

        /// <summary>
        /// Builds one narrow titled scroll list (title + ScrollRect/Viewport/Content) and returns the
        /// Content transform to populate — shared by all four grouped roster columns.
        /// </summary>
        private Transform BuildScrollableSubColumn(Transform parent, string title)
        {
            GameObject column = new GameObject($"Column_{title.Replace(" ", string.Empty)}", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(parent, false);
            LayoutElement columnLayout = column.GetComponent<LayoutElement>();
            columnLayout.flexibleWidth = 1f;
            columnLayout.flexibleHeight = 1f;

            VerticalLayoutGroup columnGroup = column.GetComponent<VerticalLayoutGroup>();
            columnGroup.spacing = 4f;
            columnGroup.childControlWidth = true;
            columnGroup.childControlHeight = true;
            columnGroup.childForceExpandWidth = true;
            columnGroup.childForceExpandHeight = false;

            TextMeshProUGUI subHeader = CreateLabel(column.transform, title, 12.5f, semiBold: true);
            subHeader.color = SurvivalPioneerUiPalette.WarmOffWhite;
            subHeader.alignment = TextAlignmentOptions.TopLeft;

            GameObject scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
            scrollObject.transform.SetParent(column.transform, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 220f;

            Image scrollBg = scrollObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.ScrollBackground;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMin = new Vector2(3f, 3f);
            viewportRect.offsetMax = new Vector2(-3f, -3f);
            viewport.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.35f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.padding = new RectOffset(3, 3, 3, 3);
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;

            return content.transform;
        }

        private void BuildRightColumn(Transform parent)
        {
            VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
            layout.spacing = JournalPanelLayout.SectionSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = CreateLabel(parent, "COMPANION DETAIL", JournalPanelLayout.HeaderFontSize, semiBold: true);
            JournalPanelLayout.ApplyHeaderStyle(header);

            Transform scrollContent = BuildScrollableContentArea(parent, flexibleHeight: 1f, minHeight: 300f);

            GameObject detailHost = new GameObject("DetailHost", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            detailHost.transform.SetParent(scrollContent, false);
            detailHost.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.88f);
            LayoutElement detailLayout = detailHost.GetComponent<LayoutElement>();
            detailLayout.minHeight = 80f;
            detailLayout.flexibleWidth = 1f;

            VerticalLayoutGroup detailHostLayout = detailHost.GetComponent<VerticalLayoutGroup>();
            detailHostLayout.padding = JournalPanelLayout.RowPaddingRect;
            detailHostLayout.childControlWidth = true;
            detailHostLayout.childControlHeight = true;
            detailHostLayout.childForceExpandWidth = true;
            detailHostLayout.childForceExpandHeight = false;

            ContentSizeFitter detailHostFitter = detailHost.GetComponent<ContentSizeFitter>();
            detailHostFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            detailHostFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            detailLabel = CreateWrappingLabel(detailHost.transform, "Select a skilled companion from the roster.", JournalPanelLayout.BodyFontSize);
            detailLabel.color = SurvivalPioneerUiPalette.BodyText;

            synergyHintLabel = CreateWrappingLabel(scrollContent, string.Empty, JournalPanelLayout.SecondaryFontSize);
            synergyHintLabel.color = SurvivalPioneerUiPalette.MutedText;

            TextMeshProUGUI loadoutHeader = CreateSectionHeader(scrollContent, "Loadout");

            GameObject loadoutRow = new GameObject("LoadoutSlots", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            loadoutRow.transform.SetParent(scrollContent, false);
            loadoutRow.GetComponent<LayoutElement>().minHeight = 64f;
            HorizontalLayoutGroup loadoutLayout = loadoutRow.GetComponent<HorizontalLayoutGroup>();
            loadoutLayout.spacing = 6f;
            loadoutLayout.childControlWidth = true;
            loadoutLayout.childForceExpandWidth = true;
            loadoutLayout.childControlHeight = true;
            loadoutLayout.childForceExpandHeight = true;

            weaponSlotButton = CreateLoadoutSlotButton(loadoutRow.transform, "WeaponSlot", "Weapon\n—", CycleWeaponLoadout, out weaponSlotLabel);
            toolSlotButton = CreateLoadoutSlotButton(loadoutRow.transform, "ToolSlot", "Tool\n—", CycleToolLoadout, out toolSlotLabel);
            skillSlotButton = CreateLoadoutSlotButton(loadoutRow.transform, "SkillSlot", "Skill\n—", CycleSkillLoadout, out skillSlotLabel);

            loadoutStatusLabel = CreateWrappingLabel(scrollContent, "Select a companion to edit loadout.", JournalPanelLayout.SecondaryFontSize);
            loadoutStatusLabel.color = SurvivalPioneerUiPalette.MutedText;

            TextMeshProUGUI trioHeader = CreateSectionHeader(scrollContent, "Expedition Trio");

            GameObject trioRow = new GameObject("TrioSlots", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            trioRow.transform.SetParent(scrollContent, false);
            trioRow.GetComponent<LayoutElement>().minHeight = 72f;
            HorizontalLayoutGroup trioLayout = trioRow.GetComponent<HorizontalLayoutGroup>();
            trioLayout.spacing = 6f;
            trioLayout.childControlWidth = true;
            trioLayout.childForceExpandWidth = true;
            trioLayout.childControlHeight = true;
            trioLayout.childForceExpandHeight = true;

            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
                CreateTrioSlotButton(trioRow.transform, i);

            trioStatusLabel = CreateWrappingLabel(scrollContent, string.Empty, 12f);
            trioStatusLabel.color = SurvivalPioneerUiPalette.MutedText;

            CreateSectionHeader(scrollContent, "Trio Loadouts");

            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
                CreateTrioLoadoutMiniPanel(scrollContent, i);
        }

        /// <summary>
        /// Scrollable body for the Pioneer Detail column — keeps long detail/trio loadout text readable.
        /// </summary>
        private Transform BuildScrollableContentArea(Transform parent, float flexibleHeight, float minHeight)
        {
            GameObject scrollObject = new GameObject("DetailScroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
            scrollObject.transform.SetParent(parent, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = flexibleHeight;
            scrollLayout.minHeight = minHeight;
            scrollLayout.flexibleWidth = 1f;

            Image scrollBg = scrollObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.ScrollBackground;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);
            viewport.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.25f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.padding = new RectOffset(6, 6, 6, 10);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            return content.transform;
        }

        private TextMeshProUGUI CreateSectionHeader(Transform parent, string title)
        {
            TextMeshProUGUI header = CreateLabel(parent, title, JournalPanelLayout.HeaderFontSize, semiBold: true);
            JournalPanelLayout.ApplyHeaderStyle(header);
            LayoutElement layout = header.GetComponent<LayoutElement>();
            layout.minHeight = 20f;
            layout.preferredHeight = 20f;
            return header;
        }

        private TextMeshProUGUI CreateWrappingLabel(Transform parent, string text, float size, bool semiBold = false)
        {
            TextMeshProUGUI label = CreateLabel(parent, text, size, semiBold);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            EnableAutoHeight(label, minHeight: Mathf.Max(20f, size + 6f));
            return label;
        }

        private static void EnableAutoHeight(TextMeshProUGUI label, float minHeight = 20f)
        {
            if (label == null)
                return;

            LayoutElement layout = label.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = minHeight;
                layout.flexibleWidth = 1f;
            }

            ContentSizeFitter fitter = label.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = label.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateTrioLoadoutMiniPanel(Transform parent, int slotIndex)
        {
            GameObject host = new GameObject($"TrioLoadout_{slotIndex + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(ContentSizeFitter));
            host.transform.SetParent(parent, false);
            host.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.94f);
            LayoutElement hostElement = host.GetComponent<LayoutElement>();
            hostElement.minHeight = 148f;
            hostElement.flexibleWidth = 1f;

            ContentSizeFitter hostFitter = host.GetComponent<ContentSizeFitter>();
            hostFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            hostFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup hostLayout = host.AddComponent<VerticalLayoutGroup>();
            hostLayout.padding = new RectOffset(8, 8, 6, 8);
            hostLayout.spacing = 6;
            hostLayout.childControlWidth = true;
            hostLayout.childForceExpandWidth = true;
            hostLayout.childControlHeight = true;
            hostLayout.childForceExpandHeight = false;

            trioLoadoutLabels[slotIndex] = CreateWrappingLabel(host.transform, $"Slot {slotIndex + 1} — Empty", 12f, semiBold: true);
            trioLoadoutLabels[slotIndex].color = SurvivalPioneerUiPalette.WarmOffWhite;

            Button hostButton = host.AddComponent<Button>();
            hostButton.transition = Selectable.Transition.None;
            hostButton.targetGraphic = host.GetComponent<Image>();
            int capturedLoadoutSlot = slotIndex;
            hostButton.onClick.AddListener(() => HandleTrioLoadoutPanelClicked(capturedLoadoutSlot));

            GameObject row = new GameObject("LoadoutRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(host.transform, false);
            row.GetComponent<LayoutElement>().minHeight = 68f;
            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;

            int capturedSlot = slotIndex;
            trioLoadoutWeaponButtons[slotIndex] = CreateLoadoutSlotButton(
                row.transform,
                "Weapon",
                "Wpn\n—",
                () => CycleTrioSlotWeapon(capturedSlot),
                out _);
            trioLoadoutToolButtons[slotIndex] = CreateLoadoutSlotButton(
                row.transform,
                "Tool",
                "Tool\n—",
                () => CycleTrioSlotTool(capturedSlot),
                out _);
            trioLoadoutSkillButtons[slotIndex] = CreateLoadoutSlotButton(
                row.transform,
                "Skill",
                "Skl\n—",
                () => CycleTrioSlotSkill(capturedSlot),
                out _);

            trioSpecsLabels[slotIndex] = CreateWrappingLabel(host.transform, "Specs: —", 11f);
            trioSpecsLabels[slotIndex].color = SurvivalPioneerUiPalette.MutedText;

            trioBuffLabels[slotIndex] = CreateWrappingLabel(host.transform, "Buffs: —", 11f);
            trioBuffLabels[slotIndex].color = SurvivalPioneerUiPalette.MutedText;

            trioDebuffLabels[slotIndex] = CreateWrappingLabel(host.transform, "Debuffs: —", 11f);
            trioDebuffLabels[slotIndex].color = SurvivalPioneerUiPalette.MutedText;
        }

        private void CreateTrioSlotButton(Transform parent, int slotIndex)
        {
            GameObject slotObject = new GameObject($"TrioSlot_{slotIndex + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            slotObject.transform.SetParent(parent, false);

            Image slotImage = slotObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(slotImage);
            slotImage.color = SurvivalPioneerUiPalette.SlotBackground;
            SurvivalPioneerUiPalette.StylePrimaryButton(slotObject.GetComponent<Button>(), slotImage);

            LayoutElement slotLayout = slotObject.GetComponent<LayoutElement>();
            slotLayout.flexibleWidth = 1f;
            slotLayout.minHeight = 64f;

            trioSlotLabels[slotIndex] = CreateLabel(slotObject.transform, $"Slot {slotIndex + 1}\nEmpty", 12f, semiBold: true);
            trioSlotLabels[slotIndex].alignment = TextAlignmentOptions.Center;
            trioSlotLabels[slotIndex].color = SurvivalPioneerUiPalette.WarmOffWhite;
            Stretch(trioSlotLabels[slotIndex].rectTransform, 6f, 6f);

            int capturedSlot = slotIndex;
            Button button = slotObject.GetComponent<Button>();
            trioSlotButtons[slotIndex] = button;
            button.onClick.AddListener(() => HandleTrioSlotClicked(capturedSlot));

            PioneerTrioSlotDropHandler drop = slotObject.AddComponent<PioneerTrioSlotDropHandler>();
            drop.Configure(this, slotIndex);
        }

        private Button CreateLoadoutSlotButton(
            Transform parent,
            string objectName,
            string defaultText,
            UnityEngine.Events.UnityAction onClick,
            out TextMeshProUGUI label)
        {
            GameObject slotObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            slotObject.transform.SetParent(parent, false);

            Image slotImage = slotObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(slotImage);
            slotImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.92f);

            Button button = slotObject.GetComponent<Button>();
            SurvivalPioneerUiPalette.StylePrimaryButton(button, slotImage);
            button.onClick.AddListener(onClick);

            slotObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

            label = CreateLabel(slotObject.transform, defaultText, 12f, semiBold: true);
            label.alignment = TextAlignmentOptions.Center;
            label.color = SurvivalPioneerUiPalette.WarmOffWhite;
            Stretch(label.rectTransform, 6f, 6f);
            return button;
        }

        private static GameObject CreateColumn(Transform parent, float flexibleWidth)
        {
            GameObject column = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(parent, false);
            LayoutElement layout = column.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = 1f;
            return column;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string text, float size, bool semiBold = false)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label, semiBold: semiBold);
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        private void ApplyThemeFont(TextMeshProUGUI label, bool semiBold = false)
        {
            if (theme != null)
                theme.ApplyFont(label, semiBold: semiBold);
            else
                TmpUiHelper.ApplyDefaultFont(label);
        }

        private static void Stretch(RectTransform rect, float padX = 0f, float padY = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
