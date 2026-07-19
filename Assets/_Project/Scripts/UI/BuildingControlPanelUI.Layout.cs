using System;
using System.Collections.Generic;
using Project.Building;
using Project.Companions;
using Project.Core;
using Project.Crafting;
using Project.Inventory;
using Project.Pioneers;
using Project.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project.UI
{
    // Everything involved in constructing the panel's runtime UI hierarchy (shell, tabs, scroll
    // panels, buttons, toggles, shared text/label helpers). No gameplay state lives here — this is
    // pure "build the widgets" code, split out of BuildingControlPanelUI.cs to keep that file to
    // the panel's open/close/tab-switch orchestration.
    public partial class BuildingControlPanelUI
    {
        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            MenuUiBuilder.StretchRectToFill(GetComponent<RectTransform>());
            EnsureUiInput(canvasRoot);
            ShiftUiTheme theme = ShiftUiTheme.Current;

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "BuildingControlOverlay", new Color(0f, 0f, 0f, 0.5f), blockRaycasts: true);
            overlayRoot.transform.SetAsLastSibling();

            GameObject shell = MenuUiBuilder.CreateFullscreenShell(
                overlayRoot.transform,
                "Building Control",
                out RectTransform contentArea,
                out Button closeButton);
            titleText = MenuUiBuilder.GetShellTitleText(shell);
            buildingSubtitleText = CreateHeaderSubtitle(shell.transform);
            closeButton.onClick.AddListener(Close);

            GameObject layoutRoot = new GameObject("Layout", typeof(RectTransform));
            layoutRoot.transform.SetParent(contentArea, false);
            RectTransform layoutRect = layoutRoot.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(layoutRect);

            VerticalLayoutGroup rootLayout = layoutRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.spacing = 0f;
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            GameObject tabRow = new GameObject("TabRow", typeof(RectTransform));
            tabRow.transform.SetParent(layoutRoot.transform, false);
            HorizontalLayoutGroup tabRowLayout = tabRow.AddComponent<HorizontalLayoutGroup>();
            tabRowLayout.padding = new RectOffset(16, 16, 8, 8);
            tabRowLayout.spacing = 6f;
            tabRowLayout.childAlignment = TextAnchor.MiddleLeft;
            tabRowLayout.childControlWidth = true;
            tabRowLayout.childControlHeight = true;
            tabRowLayout.childForceExpandWidth = false;
            tabRowLayout.childForceExpandHeight = false;

            LayoutElement tabRowLayoutElement = tabRow.AddComponent<LayoutElement>();
            tabRowLayoutElement.minHeight = 52f;
            tabRowLayoutElement.preferredHeight = 52f;

            for (int i = 0; i < TabLabels.Length; i++)
            {
                BuildingControlTab tab = (BuildingControlTab)i;
                CreateTabButton(tabRow.transform, tab, TabLabels[i], theme);
            }

            GameObject bodyHost = new GameObject("TabBody", typeof(RectTransform));
            bodyHost.transform.SetParent(layoutRoot.transform, false);
            tabBodyArea = bodyHost.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(tabBodyArea);
            LayoutElement bodyLayout = bodyHost.AddComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1f;
            bodyLayout.flexibleWidth = 1f;
            bodyLayout.minHeight = 320f;

            CreateTabPanels(tabBodyArea, theme);
            ScienceLabHealthContextMenu.EnsureExists(canvasRoot, this);

            overlayRoot.SetActive(false);
            UiFrontLayer.BringLayerToFront(canvasRoot);
        }

        private void CreateTabPanels(Transform parent, ShiftUiTheme theme)
        {
            tabPanels[BuildingControlTab.Overview] = CreateOverviewTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Pioneers] = CreatePioneersTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Production] = CreateProductionTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Craft] = CreateCraftTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Changes] = CreateChangesTabPanel(parent, theme);
            tabPanels[BuildingControlTab.Health] = CreateHealthTabPanel(parent, theme);

            ShowTab(BuildingControlTab.Overview);
        }

        private GameObject CreateHealthTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "HealthPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Injured Pioneers";
            heading.fontStyle = FontStyles.Bold;
            heading.color = SurvivalPioneerUiPalette.BodyText;

            CreateBodyText(content, theme, 18f).text =
                "Pioneers sent here after falling in combat. Right-click a row and choose Reassign when recovery is complete.";

            healthStatusLabel = CreateBodyText(content, theme, 18f);
            healthStatusLabel.color = SurvivalPioneerUiPalette.MutedText;

            GameObject listHost = new GameObject("InjuredList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listHost.transform.SetParent(content, false);
            VerticalLayoutGroup listLayout = listHost.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            healthListParent = listHost.transform;

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreateCraftTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = new GameObject("CraftPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(panel.GetComponent<RectTransform>());

            craftHost = new GameObject("CraftHost", typeof(RectTransform)).GetComponent<RectTransform>();
            craftHost.SetParent(panel.transform, false);
            MenuUiBuilder.StretchRectToFill(craftHost);

            craftStubText = CreateBodyText(panel.transform, theme, 20f);
            craftStubText.alignment = TextAlignmentOptions.TopLeft;
            RectTransform stubRect = craftStubText.GetComponent<RectTransform>();
            stubRect.anchorMin = Vector2.zero;
            stubRect.anchorMax = Vector2.one;
            stubRect.offsetMin = new Vector2(24f, 24f);
            stubRect.offsetMax = new Vector2(-24f, -24f);
            craftStubText.gameObject.SetActive(false);

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreateOverviewTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "OverviewPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Overview";
            heading.fontStyle = FontStyles.Bold;
            heading.color = SurvivalPioneerUiPalette.BodyText;

            overviewBuildingNameText = CreateBodyText(content, theme, 22f);
            overviewAssignedText = CreateBodyText(content, theme, 20f);
            overviewQueueText = CreateBodyText(content, theme, 20f);
            overviewStormText = CreateBodyText(content, theme, 20f);
            overviewStormText.fontStyle = FontStyles.Bold;
            overviewMaintenanceText = CreateBodyText(content, theme, 20f);
            overviewOutputText = CreateBodyText(content, theme, 20f);
            overviewPowerText = CreateBodyText(content, theme, 20f);

            CreateRefuelGeneratorButton(content, theme);

            CreateBodyText(content, theme, 18f).text =
                "Assign pioneers and manage production queues from the other tabs.";

            panel.SetActive(false);
            return panel;
        }

        private void CreateRefuelGeneratorButton(Transform parent, ShiftUiTheme theme)
        {
            GameObject buttonObject = new GameObject("RefuelGeneratorButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.minHeight = 44f;
            layout.preferredHeight = 44f;
            layout.preferredWidth = 260f;
            layout.flexibleWidth = 0f;

            Image background = buttonObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.85f);

            refuelGeneratorButton = buttonObject.GetComponent<Button>();
            refuelGeneratorButton.targetGraphic = background;
            UiSoundHelper.BindButton(refuelGeneratorButton);
            refuelGeneratorButton.onClick.AddListener(OnRefuelGeneratorClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            refuelGeneratorButtonLabel = labelObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(refuelGeneratorButtonLabel, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(refuelGeneratorButtonLabel);
            refuelGeneratorButtonLabel.fontSize = 16f;
            refuelGeneratorButtonLabel.alignment = TextAlignmentOptions.Center;
            refuelGeneratorButtonLabel.color = SurvivalPioneerUiPalette.BodyText;
            refuelGeneratorButtonLabel.text = "Load Plasma Fuel";
            refuelGeneratorButtonLabel.raycastTarget = false;
            MenuUiBuilder.StretchRectToFill(refuelGeneratorButtonLabel.GetComponent<RectTransform>());

            buttonObject.SetActive(false);
        }

        private GameObject CreatePioneersTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "PioneersPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Pioneer Assignments";
            heading.fontStyle = FontStyles.Bold;
            heading.color = SurvivalPioneerUiPalette.BodyText;

            CreateBodyText(content, theme, 18f).text =
                "Click a slot to cycle through available base pioneers (up to four per building).";

            for (int i = 0; i < BuildingOperationRegistry.MaxAssignedPioneers; i++)
            {
                int slotIndex = i;
                GameObject slotRow = new GameObject($"Slot{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                slotRow.transform.SetParent(content, false);
                LayoutElement rowLayout = slotRow.GetComponent<LayoutElement>();
                rowLayout.minHeight = 52f;
                rowLayout.preferredHeight = 52f;

                Image rowBackground = slotRow.GetComponent<Image>();
                MenuUiBuilder.ApplyUiSprite(rowBackground);
                rowBackground.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.95f);

                Button slotButton = slotRow.GetComponent<Button>();
                slotButton.targetGraphic = rowBackground;
                ColorBlock colors = slotButton.colors;
                colors.normalColor = rowBackground.color;
                colors.highlightedColor = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.22f);
                colors.pressedColor = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.85f);
                colors.selectedColor = colors.highlightedColor;
                slotButton.colors = colors;
                UiSoundHelper.BindButton(slotButton);
                slotButton.onClick.AddListener(() => OnPioneerSlotClicked(slotIndex));
                pioneerSlotButtons[slotIndex] = slotButton;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(slotRow.transform, false);
                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
                if (theme != null)
                    theme.ApplyFont(label);
                else
                    TmpUiHelper.ApplyDefaultFont(label);
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = theme != null ? theme.secondaryTextColor : SurvivalPioneerUiPalette.BodyText;
                label.raycastTarget = false;
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(14f, 6f);
                labelRect.offsetMax = new Vector2(-14f, -6f);
                pioneerSlotLabels[slotIndex] = label;
            }

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreateProductionTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "ProductionPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Production Queue";
            heading.fontStyle = FontStyles.Bold;
            heading.color = SurvivalPioneerUiPalette.BodyText;

            CreateBodyText(content, theme, 18f).text =
                "Queued recipes run while you are on expedition and pause during sulfur storms.";

            productionPausedOverlay = CreateBodyText(content, theme, 20f);
            productionPausedOverlay.fontStyle = FontStyles.Bold;
            productionPausedOverlay.color = SurvivalPioneerUiPalette.WarningText;
            productionPausedOverlay.gameObject.SetActive(false);

            GameObject listHost = new GameObject("QueueList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listHost.transform.SetParent(content, false);
            VerticalLayoutGroup listLayout = listHost.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 10f;
            listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            productionListParent = listHost.transform;

            panel.SetActive(false);
            return panel;
        }

        private GameObject CreateChangesTabPanel(Transform parent, ShiftUiTheme theme)
        {
            GameObject panel = CreateOperationalScrollPanel(parent, "ChangesPanel", out Transform content);
            TextMeshProUGUI heading = CreateBodyText(content, theme, 26f);
            heading.text = "Building Settings";
            heading.fontStyle = FontStyles.Bold;
            heading.color = SurvivalPioneerUiPalette.BodyText;

            CreateBodyText(content, theme, 18f).text =
                "Per-building automation and mode toggles. Changes apply to this structure only.";

            GameObject toggleHost = new GameObject("SettingsToggles", typeof(RectTransform), typeof(VerticalLayoutGroup));
            toggleHost.transform.SetParent(content, false);
            VerticalLayoutGroup toggleLayout = toggleHost.GetComponent<VerticalLayoutGroup>();
            toggleLayout.spacing = 10f;
            toggleLayout.childControlWidth = true;
            toggleLayout.childForceExpandWidth = true;
            toggleLayout.childForceExpandHeight = false;
            changesToggleHost = toggleHost.transform;

            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateOperationalScrollPanel(Transform parent, string name, out Transform content)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(panel.GetComponent<RectTransform>());

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(panel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(viewportRect);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            content = contentObject.transform;
            return panel;
        }

        private static GameObject CreateScrollableTabPanel(
            Transform parent,
            string name,
            string heading,
            params string[] paragraphs)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            MenuUiBuilder.StretchRectToFill(panel.GetComponent<RectTransform>());

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(panel.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(viewportRect);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = panel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            ShiftUiTheme theme = ShiftUiTheme.Current;
            TextMeshProUGUI headingText = CreateBodyText(content.transform, theme, 26f);
            headingText.text = heading;
            headingText.fontStyle = FontStyles.Bold;
            headingText.color = SurvivalPioneerUiPalette.BodyText;

            for (int i = 0; i < paragraphs.Length; i++)
            {
                TextMeshProUGUI paragraph = CreateBodyText(content.transform, theme, 20f);
                paragraph.text = paragraphs[i];
                paragraph.textWrappingMode = TextWrappingModes.Normal;
            }

            panel.SetActive(false);
            return panel;
        }

        private void CreateTabButton(Transform parent, BuildingControlTab tab, string label, ShiftUiTheme theme)
        {
            GameObject tabObject = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            tabObject.transform.SetParent(parent, false);

            LayoutElement layout = tabObject.AddComponent<LayoutElement>();
            layout.minWidth = 108f;
            layout.preferredWidth = 128f;
            layout.flexibleWidth = 1f;
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;

            Image background = tabObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = InactiveTabColor;
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(tabObject);

            Button button = tabObject.GetComponent<Button>();
            button.targetGraphic = background;
            UiSoundHelper.BindButton(button);

            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(tabObject.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(text, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(text);
            text.text = label;
            text.fontSize = 16f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = InactiveLabelColor;
            text.raycastTarget = false;
            MenuUiBuilder.StretchRectToFill(text.GetComponent<RectTransform>());

            tabButtonBackgrounds[tab] = background;
            tabButtonLabels[tab] = text;
            tabButtonRoots[tab] = tabObject;

            BuildingControlTab captured = tab;
            button.onClick.AddListener(() => ShowTab(captured));
        }

        private static void CreateSettingToggle(
            Transform parent,
            ShiftUiTheme theme,
            string label,
            Func<bool> readValue,
            Action<bool> writeValue)
        {
            GameObject row = new GameObject("SettingRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 44f;
            rowLayout.preferredHeight = 44f;

            HorizontalLayoutGroup rowGroup = row.GetComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 12f;
            rowGroup.padding = new RectOffset(4, 4, 4, 4);
            rowGroup.childAlignment = TextAnchor.MiddleLeft;
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandWidth = true;
            rowGroup.childControlHeight = true;

            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
            toggleObject.transform.SetParent(row.transform, false);
            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 36f;
            toggleLayout.preferredWidth = 36f;

            Image toggleBg = toggleObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(toggleBg);
            toggleBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 1f);

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = toggleBg;
            toggle.isOn = readValue();

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(toggleObject.transform, false);
            Image checkImage = checkmark.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(checkImage);
            checkImage.color = SurvivalPioneerUiPalette.RichFuchsia;
            MenuUiBuilder.StretchRectToFill(checkmark.GetComponent<RectTransform>());
            toggle.graphic = checkImage;

            toggle.onValueChanged.AddListener(value => writeValue(value));

            TextMeshProUGUI labelText = CreateBodyText(row.transform, theme, 17f);
            labelText.text = label;
            labelText.textWrappingMode = TextWrappingModes.Normal;
        }

        private static TextMeshProUGUI CreateHeaderSubtitle(Transform shellRoot)
        {
            Transform header = shellRoot.Find("Header");
            if (header == null)
                return null;

            GameObject subtitleObject = new GameObject("Subtitle", typeof(RectTransform));
            subtitleObject.transform.SetParent(header, false);
            TextMeshProUGUI subtitle = subtitleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(subtitle);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(subtitle);
            subtitle.fontSize = 14f;
            subtitle.fontStyle = FontStyles.Italic;
            subtitle.color = SurvivalPioneerUiPalette.MutedText;
            subtitle.alignment = TextAlignmentOptions.BottomLeft;
            subtitle.raycastTarget = false;

            RectTransform subtitleRect = subtitleObject.GetComponent<RectTransform>();
            subtitleRect.anchorMin = Vector2.zero;
            subtitleRect.anchorMax = Vector2.one;
            subtitleRect.offsetMin = new Vector2(20f, 6f);
            subtitleRect.offsetMax = new Vector2(-56f, -6f);
            return subtitle;
        }

        private static void EnsureUiInput(Transform canvasRoot)
        {
            Canvas canvas = canvasRoot.GetComponent<Canvas>() ?? canvasRoot.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static TextMeshProUGUI CreateBodyText(Transform parent, ShiftUiTheme theme, float size)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(text);
            else
                TmpUiHelper.ApplyDefaultFont(text);
            text.fontSize = size;
            text.color = theme != null ? theme.secondaryTextColor : SurvivalPioneerUiPalette.BodyText;
            text.raycastTarget = false;
            return text;
        }
    }
}
