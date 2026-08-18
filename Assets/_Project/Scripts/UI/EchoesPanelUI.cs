using System;
using System.Collections.Generic;
using Project.Echoes;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class EchoesPanelUI : MonoBehaviour
    {
        private Transform embeddedParent;
        private GameObject panelRoot;
        private Transform chronicleListParent;
        private Transform buffListParent;
        private Transform signalListParent;
        private Transform dispositionListParent;
        private PioneerRosterManager roster;
        private ShiftUiTheme theme;

        public void EmbedIn(Transform parent)
        {
            if (parent == null)
                return;

            embeddedParent = parent;
            roster = PioneerRosterManager.EnsureExists();
            theme = ShiftUiTheme.Current;
            EnsureBuilt(parent);

            if (roster != null)
            {
                roster.OnEchoChronicleChanged += Refresh;
                roster.OnRosterChanged += Refresh;
            }

            Refresh();
        }

        public void Unembed()
        {
            if (roster != null)
            {
                roster.OnEchoChronicleChanged -= Refresh;
                roster.OnRosterChanged -= Refresh;
            }

            if (panelRoot != null)
                Destroy(panelRoot);

            panelRoot = null;
            chronicleListParent = null;
            buffListParent = null;
            signalListParent = null;
            dispositionListParent = null;
            embeddedParent = null;
        }

        public void Refresh()
        {
            if (panelRoot == null)
                return;

            theme = ShiftUiTheme.Current;
            roster ??= PioneerRosterManager.EnsureExists();
            RefreshChronicleSection();
            RefreshBuffSection();
            RefreshSignalSection();
            RefreshDispositionSection();
        }

        private void RefreshChronicleSection()
        {
            ClearChildren(chronicleListParent);
            if (roster == null || roster.EchoChronicle.Count == 0)
            {
                JournalPanelLayout.CreateEmptyStateCard(
                    chronicleListParent,
                    theme,
                    "No rescues logged",
                    "Neural Echo rescues and failures appear here as your chronicle grows.",
                    "Track signals in the field to begin a rescue.");
                return;
            }

            for (int i = 0; i < roster.EchoChronicle.Count; i++)
            {
                EchoChronicleEntry entry = roster.EchoChronicle[i];
                if (entry == null || entry.simulationIncident)
                    continue;

                string disposition = PioneerTraitUtility.GetDispositionLabel(entry.DispositionAtRescue);
                Color dispositionColor = GetDispositionColor(entry.DispositionAtRescue);
                string dateLabel = entry.rescuedAtUtcTicks > 0
                    ? new DateTime(entry.rescuedAtUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("MMM d · HH:mm")
                    : "Unknown time";

                string heading = entry.rescueFailed
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(DarkMatterGenesisUiPalette.DangerRed)}>Rescue Failed</color>"
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(DarkMatterGenesisUiPalette.PositiveGreen)}>Rescue Success</color>";

                CreateCardRow(
                    chronicleListParent,
                    $"{heading}  ·  {JournalPanelLayout.FormatAccentTitle(entry.echoName)}\n" +
                    $"{JournalPanelLayout.FormatHelper($"{dateLabel}  ·  {entry.classSummary}")}\n" +
                    $"<color=#{ColorUtility.ToHtmlStringRGB(dispositionColor)}>{disposition}</color>  ·  {JournalPanelLayout.FormatHelper(entry.abilitySummary)}",
                    showEchoPortrait: true);
            }

            if (chronicleListParent.childCount == 0)
            {
                JournalPanelLayout.CreateEmptyStateCard(
                    chronicleListParent,
                    theme,
                    "No rescues logged",
                    "Neural Echo rescues and failures appear here as your chronicle grows.");
            }
        }

        private void RefreshBuffSection()
        {
            ClearChildren(buffListParent);
            IReadOnlyList<string> buffs = CompanionBuffRegistry.GetActiveBuffSummaries(roster);
            if (buffs == null || buffs.Count == 0)
            {
                CreateInfoRow(buffListParent, "No companion buffs active.");
                return;
            }

            for (int i = 0; i < buffs.Count; i++)
                CreateInfoRow(buffListParent, buffs[i]);
        }

        private void RefreshSignalSection()
        {
            ClearChildren(signalListParent);
            EchoSignalRegistry.EnsureDefaultPlaceholder();
            IReadOnlyList<string> signals = EchoSignalRegistry.GetActiveSignalSummaries();
            if (signals == null || signals.Count == 0)
            {
                CreateInfoRow(signalListParent, "No active echo signals.");
                return;
            }

            for (int i = 0; i < signals.Count; i++)
                CreateInfoRow(signalListParent, signals[i]);
        }

        private void RefreshDispositionSection()
        {
            ClearChildren(dispositionListParent);
            if (roster == null || roster.SkilledPioneers.Count == 0)
            {
                CreateInfoRow(dispositionListParent, "No skilled pioneers on roster.");
                return;
            }

            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record == null || record.Kind != PioneerKind.RescuedEcho)
                    continue;

                CreateDispositionBadge(record);
            }

            if (dispositionListParent.childCount == 0)
                CreateInfoRow(dispositionListParent, "No rescued echoes on roster yet.");
        }

        private void CreateDispositionBadge(SkilledPioneerRecord record)
        {
            GameObject row = new GameObject($"Disposition_{record.id}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(dispositionListParent, false);

            Image bg = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.96f);

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = JournalPanelLayout.RowPaddingRect;
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = JournalPanelLayout.RowMinHeight;

            RawImage photo = PioneerPortraitUi.CreateCircularPortrait(row.transform, 28f);
            Image frame = PioneerPortraitUi.GetMaskImage(photo);
            PioneerPortraitUi.ApplySpriteOnly(frame, photo, PioneerPortraitResolver.ResolveEchoSpirit());

            TextMeshProUGUI nameLabel = CreateLabel(row.transform, record.displayName, JournalPanelLayout.BodyFontSize, semiBold: true);
            nameLabel.color = DarkMatterGenesisUiPalette.BodyText;
            LayoutElement nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            Color badgeColor = GetDispositionColor(record.Disposition);
            GameObject badgeObject = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badgeObject.transform.SetParent(row.transform, false);
            Image badgeBg = badgeObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(badgeBg);
            badgeBg.color = DarkMatterGenesisUiPalette.WithAlpha(badgeColor, 0.85f);
            LayoutElement badgeLayout = badgeObject.GetComponent<LayoutElement>();
            badgeLayout.preferredWidth = 80f;
            badgeLayout.minHeight = 22f;

            TextMeshProUGUI badgeLabel = CreateLabel(badgeObject.transform, PioneerTraitUtility.GetDispositionLabel(record.Disposition), JournalPanelLayout.CaptionFontSize, semiBold: true);
            badgeLabel.alignment = TextAlignmentOptions.Center;
            badgeLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            Stretch(badgeLabel.rectTransform);
        }

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            theme = ShiftUiTheme.Current;

            panelRoot = new GameObject("EchoesPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            JournalPanelLayout.StretchFill(panelRoot.GetComponent<RectTransform>());

            Image panelBg = panelRoot.GetComponent<Image>();
            JournalPanelLayout.StylePanelBackground(panelBg, theme);

            VerticalLayoutGroup rootLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            JournalPanelLayout.ApplyRootVerticalLayout(rootLayout);

            CreateSectionHeader(panelRoot.transform, "Rescue Chronicle");
            chronicleListParent = CreateSectionScroll(panelRoot.transform, 140f);

            CreateSectionHeader(panelRoot.transform, "Companion Buffs");
            buffListParent = CreateSectionScroll(panelRoot.transform, 72f);

            CreateSectionHeader(panelRoot.transform, "Active Echo Signals");
            signalListParent = CreateSectionScroll(panelRoot.transform, 64f);

            CreateSectionHeader(panelRoot.transform, "Echo Dispositions");
            dispositionListParent = CreateSectionScroll(panelRoot.transform, 80f);
        }

        private void CreateSectionHeader(Transform parent, string title)
        {
            TextMeshProUGUI header = CreateLabel(parent, title, JournalPanelLayout.HeaderFontSize, semiBold: true);
            JournalPanelLayout.ApplyHeaderStyle(header);
        }

        private Transform CreateSectionScroll(Transform parent, float minHeight)
        {
            GameObject scrollObject = new GameObject("SectionScroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
            scrollObject.transform.SetParent(parent, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.minHeight = minHeight;
            scrollLayout.preferredHeight = minHeight;
            scrollLayout.flexibleHeight = 1f;

            Image scrollBg = scrollObject.GetComponent<Image>();
            JournalPanelLayout.StyleScrollBackground(scrollBg);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMin = new Vector2(JournalPanelLayout.ScrollInset, JournalPanelLayout.ScrollInset);
            viewportRect.offsetMax = new Vector2(-JournalPanelLayout.ScrollInset, -JournalPanelLayout.ScrollInset);
            viewport.GetComponent<Image>().color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.28f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            JournalPanelLayout.ApplyListVerticalLayout(contentLayout);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            return content.transform;
        }

        private void CreateCardRow(Transform parent, string text, bool showEchoPortrait = false)
        {
            GameObject row = new GameObject("ChronicleRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            Image bg = row.GetComponent<Image>();
            JournalPanelLayout.StyleDenseCard(bg);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = JournalPanelLayout.CardMinHeight;

            HorizontalLayoutGroup rowGroup = row.GetComponent<HorizontalLayoutGroup>();
            rowGroup.padding = new RectOffset(
                (int)JournalPanelLayout.RowPaddingH,
                (int)JournalPanelLayout.RowPaddingH,
                (int)JournalPanelLayout.RowPaddingV,
                (int)JournalPanelLayout.RowPaddingV);
            rowGroup.spacing = 8f;
            rowGroup.childAlignment = TextAnchor.UpperLeft;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = false;

            if (showEchoPortrait)
            {
                RawImage photo = PioneerPortraitUi.CreateCircularPortrait(row.transform, 32f);
                Image frame = PioneerPortraitUi.GetMaskImage(photo);
                PioneerPortraitUi.ApplySpriteOnly(frame, photo, PioneerPortraitResolver.ResolveEchoSpirit());
            }

            TextMeshProUGUI label = CreateLabel(row.transform, text, JournalPanelLayout.BodyFontSize);
            label.color = DarkMatterGenesisUiPalette.BodyText;
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
        }

        private void CreateInfoRow(Transform parent, string text)
        {
            GameObject row = new GameObject("InfoRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.45f);
            row.GetComponent<LayoutElement>().minHeight = JournalPanelLayout.RowMinHeight;

            TextMeshProUGUI label = CreateLabel(row.transform, text, JournalPanelLayout.SecondaryFontSize);
            label.color = DarkMatterGenesisUiPalette.Gold;
            Stretch(label.rectTransform, JournalPanelLayout.RowPaddingH, 4f);
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string text, float size, bool semiBold = false)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label, semiBold: semiBold);
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = text;
            return label;
        }

        private static Color GetDispositionColor(EchoDisposition disposition)
        {
            return disposition switch
            {
                EchoDisposition.Friendly => DarkMatterGenesisUiPalette.PositiveGreen,
                EchoDisposition.Synced => DarkMatterGenesisUiPalette.Gold,
                EchoDisposition.HostileUntilSynced => DarkMatterGenesisUiPalette.DangerRed,
                _ => DarkMatterGenesisUiPalette.SoftBeigeGray
            };
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
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
