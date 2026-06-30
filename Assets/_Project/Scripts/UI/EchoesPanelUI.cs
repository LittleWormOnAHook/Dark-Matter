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
                CreateInfoRow(chronicleListParent, "No echo rescues logged yet.");
                return;
            }

            for (int i = 0; i < roster.EchoChronicle.Count; i++)
            {
                EchoChronicleEntry entry = roster.EchoChronicle[i];
                if (entry == null)
                    continue;

                string disposition = PioneerTraitUtility.GetDispositionLabel(entry.DispositionAtRescue);
                Color dispositionColor = GetDispositionColor(entry.DispositionAtRescue);
                string dateLabel = entry.rescuedAtUtcTicks > 0
                    ? new DateTime(entry.rescuedAtUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("MMM d · HH:mm")
                    : "Unknown time";

                string heading = entry.rescueFailed
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.RichFuchsia)}>Rescue Failed</color>"
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.RichFuchsia)}>Rescue Success</color>";

                CreateCardRow(
                    chronicleListParent,
                    $"{heading}  ·  {entry.echoName}\n" +
                    $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.MutedText)}>{dateLabel}  ·  {entry.classSummary}</color>\n" +
                    $"<color=#{ColorUtility.ToHtmlStringRGB(dispositionColor)}>{disposition}</color>  ·  {entry.abilitySummary}");
            }
        }

        private void RefreshBuffSection()
        {
            ClearChildren(buffListParent);
            IReadOnlyList<string> buffs = CompanionBuffRegistry.GetActiveBuffSummaries(roster);
            for (int i = 0; i < buffs.Count; i++)
                CreateInfoRow(buffListParent, buffs[i]);
        }

        private void RefreshSignalSection()
        {
            ClearChildren(signalListParent);
            EchoSignalRegistry.EnsureDefaultPlaceholder();
            IReadOnlyList<string> signals = EchoSignalRegistry.GetActiveSignalSummaries();
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
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.96f);

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 36f;

            TextMeshProUGUI nameLabel = CreateLabel(row.transform, record.displayName, 14f, semiBold: true);
            nameLabel.color = SurvivalPioneerUiPalette.BodyText;
            LayoutElement nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            Color badgeColor = GetDispositionColor(record.Disposition);
            GameObject badgeObject = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badgeObject.transform.SetParent(row.transform, false);
            Image badgeBg = badgeObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(badgeBg);
            badgeBg.color = SurvivalPioneerUiPalette.WithAlpha(badgeColor, 0.85f);
            LayoutElement badgeLayout = badgeObject.GetComponent<LayoutElement>();
            badgeLayout.preferredWidth = 88f;
            badgeLayout.minHeight = 24f;

            TextMeshProUGUI badgeLabel = CreateLabel(badgeObject.transform, PioneerTraitUtility.GetDispositionLabel(record.Disposition), 12f, semiBold: true);
            badgeLabel.alignment = TextAlignmentOptions.Center;
            badgeLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            Stretch(badgeLabel.rectTransform);
        }

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            theme = ShiftUiTheme.Current;

            panelRoot = new GameObject("EchoesPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(16f, 16f);
            rootRect.offsetMax = new Vector2(-16f, -16f);

            Image panelBg = panelRoot.GetComponent<Image>();
            if (theme != null)
                theme.ApplyPanelImage(panelBg, large: true, alphaMultiplier: 0.98f);
            else
            {
                MenuUiBuilder.ApplyUiSprite(panelBg);
                panelBg.color = SurvivalPioneerUiPalette.PanelBackground;
            }

            VerticalLayoutGroup rootLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 12f;
            rootLayout.padding = new RectOffset(14, 14, 14, 14);
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            CreateSectionHeader(panelRoot.transform, "Rescue Chronicle");
            chronicleListParent = CreateSectionScroll(panelRoot.transform, 160f);

            CreateSectionHeader(panelRoot.transform, "Companion Buffs");
            buffListParent = CreateSectionScroll(panelRoot.transform, 88f);

            CreateSectionHeader(panelRoot.transform, "Active Echo Signals");
            signalListParent = CreateSectionScroll(panelRoot.transform, 72f);

            CreateSectionHeader(panelRoot.transform, "Echo Dispositions");
            dispositionListParent = CreateSectionScroll(panelRoot.transform, 96f);
        }

        private void CreateSectionHeader(Transform parent, string title)
        {
            TextMeshProUGUI header = CreateLabel(parent, title, 18f, semiBold: true);
            header.color = SurvivalPioneerUiPalette.AccentText;
        }

        private Transform CreateSectionScroll(Transform parent, float minHeight)
        {
            GameObject scrollObject = new GameObject("SectionScroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
            scrollObject.transform.SetParent(parent, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.minHeight = minHeight;
            scrollLayout.preferredHeight = minHeight;

            Image scrollBg = scrollObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.ScrollBackground;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);
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
            contentLayout.spacing = 6f;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            return content.transform;
        }

        private void CreateCardRow(Transform parent, string text)
        {
            GameObject row = new GameObject("ChronicleRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            Image bg = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.96f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(row);

            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.minHeight = 72f;

            TextMeshProUGUI label = CreateLabel(row.transform, text, 13f);
            label.color = SurvivalPioneerUiPalette.BodyText;
            Stretch(label.rectTransform, 10f, 8f);
        }

        private void CreateInfoRow(Transform parent, string text)
        {
            GameObject row = new GameObject("InfoRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.55f);
            row.GetComponent<LayoutElement>().minHeight = 32f;

            TextMeshProUGUI label = CreateLabel(row.transform, text, 13f);
            label.color = SurvivalPioneerUiPalette.MutedText;
            Stretch(label.rectTransform, 10f, 6f);
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
                EchoDisposition.Friendly => SurvivalPioneerUiPalette.PositiveGreen,
                EchoDisposition.Synced => SurvivalPioneerUiPalette.RichFuchsia,
                EchoDisposition.HostileUntilSynced => SurvivalPioneerUiPalette.RichFuchsia,
                _ => SurvivalPioneerUiPalette.SoftBeigeGray
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
