using System;
using System.Collections.Generic;
using Project.Player;
using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project.UI
{
    public class QuestGiverDialogUI : MonoBehaviour
    {
        private static QuestGiverDialogUI instance;

        private GameObject overlayRoot;
        private GameObject dialogPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private GameObject simpleContentRoot;
        private GameObject boardContentRoot;
        private Transform questPickerContent;
        private ScrollRect questListScroll;
        private TextMeshProUGUI leftTitleText;
        private TextMeshProUGUI leftDescriptionText;
        private TextMeshProUGUI leftObjectivesText;
        private TextMeshProUGUI rightProgressText;
        private TextMeshProUGUI rightXpText;
        private TextMeshProUGUI rightRewardsText;
        private TextMeshProUGUI rightStatusText;
        private Button primaryButton;
        private TextMeshProUGUI primaryButtonLabel;
        private Button questActionButton;
        private TextMeshProUGUI questActionButtonLabel;
        private Button abandonQuestButton;
        private TextMeshProUGUI abandonQuestButtonLabel;
        private Button directionsButton;
        private TextMeshProUGUI directionsButtonLabel;
        private Button boardDirectionsButton;
        private TextMeshProUGUI boardDirectionsButtonLabel;
        private bool abandonConfirmPending;
        private Action onClosed;
        private Action onDirectionsRequested;
        private bool built;
        private IList<QuestBoardEntry> currentEntries;
        private int selectedEntryIndex = -1;
        private CanvasGroup overlayCanvasGroup;
        private NpcDialogProximityFade proximityFade;
        private Transform npcAnchor;

        public static QuestGiverDialogUI Instance => instance;

        public static bool IsDialogOpen => instance != null && instance.overlayRoot != null && instance.overlayRoot.activeSelf;

        public static void CloseAnyOpenQuestDialog()
        {
            if (instance != null && IsDialogOpen)
                instance.Close();
        }

        public static QuestGiverDialogUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null && instance.built && (instance.questListScroll == null || instance.abandonQuestButton == null || instance.boardDirectionsButton == null))
            {
                instance.TeardownInternal();
                instance = null;
            }

            if (instance != null)
                return instance;

            GameObject host = new GameObject("QuestGiverDialogUI", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            MenuUiBuilder.StretchRectToFill(host.GetComponent<RectTransform>());
            instance = host.AddComponent<QuestGiverDialogUI>();
            instance.Build(canvasRoot);
            return instance;
        }

        public static void ResetToDefaultLayout()
        {
            TeardownAllInstances();
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
                EnsureExists(canvas.transform);
        }

        public static void EnsureBuiltForLayoutEditor()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
                EnsureExists(canvas.transform);
        }

        private static void TeardownAllInstances()
        {
            if (instance != null)
            {
                instance.TeardownInternal();
                instance = null;
            }

            QuestGiverDialogUI[] existing = FindObjectsByType<QuestGiverDialogUI>(FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] == null)
                    continue;

                existing[i].TeardownInternal();
            }

            instance = null;
        }

        private void TeardownInternal()
        {
            built = false;
            onClosed = null;
            overlayRoot = null;
            dialogPanel = null;
            titleText = null;
            bodyText = null;
            simpleContentRoot = null;
            boardContentRoot = null;
            questPickerContent = null;
            questListScroll = null;
            leftTitleText = null;
            leftDescriptionText = null;
            leftObjectivesText = null;
            rightProgressText = null;
            rightXpText = null;
            rightRewardsText = null;
            rightStatusText = null;
            primaryButton = null;
            primaryButtonLabel = null;
            questActionButton = null;
            questActionButtonLabel = null;
            abandonQuestButton = null;
            abandonQuestButtonLabel = null;
            directionsButton = null;
            directionsButtonLabel = null;
            boardDirectionsButton = null;
            boardDirectionsButtonLabel = null;
            abandonConfirmPending = false;
            onDirectionsRequested = null;
            currentEntries = null;
            selectedEntryIndex = -1;
            overlayCanvasGroup = null;
            proximityFade = null;
            npcAnchor = null;

            if (instance == this)
                instance = null;

            Destroy(gameObject);
        }

        public static void Show(
            string speakerName,
            string message,
            Action closedCallback = null,
            string primaryLabel = "Continue",
            Action directionsCallback = null,
            Transform npcAnchor = null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            QuestGiverDialogUI dialog = EnsureExists(canvas.transform);
            dialog.PresentSimple(speakerName, message, closedCallback, primaryLabel, directionsCallback, npcAnchor);
        }

        public static void ShowQuestBoard(
            string speakerName,
            string introMessage,
            IList<QuestBoardEntry> entries,
            Action closedCallback = null,
            Action directionsCallback = null,
            Transform npcAnchor = null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            QuestGiverDialogUI dialog = EnsureExists(canvas.transform);
            dialog.PresentQuestBoard(speakerName, introMessage, entries, closedCallback, directionsCallback, npcAnchor);
        }

        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            EnsureUiInput(canvasRoot);
            ShiftUiTheme theme = ShiftUiTheme.Current;

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "DialogOverlay", new Color(0f, 0f, 0f, 0.5f), blockRaycasts: true);
            overlayRoot.transform.SetAsLastSibling();
            overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
                overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            proximityFade = GetComponent<NpcDialogProximityFade>();
            if (proximityFade == null)
                proximityFade = gameObject.AddComponent<NpcDialogProximityFade>();

            dialogPanel = MenuUiBuilder.CreateCenteredModalShell(
                overlayRoot.transform,
                "Quest Giver",
                GameplayHudLayout.QuestGiverModalSize,
                out RectTransform contentArea,
                out Button headerCloseButton);
            titleText = MenuUiBuilder.GetShellTitleText(dialogPanel);
            if (titleText != null)
            {
                titleText.alignment = TextAlignmentOptions.MidlineLeft;
                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 0f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.offsetMin = new Vector2(16f, 0f);
                titleRect.offsetMax = new Vector2(-56f, 0f);
            }

            headerCloseButton.onClick.RemoveAllListeners();
            headerCloseButton.onClick.AddListener(Close);

            BuildSimpleContent(contentArea, theme);
            BuildBoardContent(contentArea, theme);

            EnforceQuestDialogLayout();
            overlayRoot.SetActive(false);
            UiFrontLayer.BringLayerToFront(canvasRoot);
        }

        private void BuildSimpleContent(RectTransform contentArea, ShiftUiTheme theme)
        {
            simpleContentRoot = new GameObject("SimpleContent", typeof(RectTransform));
            simpleContentRoot.transform.SetParent(contentArea, false);
            MenuUiBuilder.StretchRectToFill(simpleContentRoot.GetComponent<RectTransform>());

            VerticalLayoutGroup layout = simpleContentRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            bodyText = CreateText(simpleContentRoot.transform, "", theme, 22f, FontStyles.Normal);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1f;

            primaryButton = CreateButton(simpleContentRoot.transform, "Close", theme, out primaryButtonLabel);
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(Close);

            directionsButton = CreateButton(simpleContentRoot.transform, "Ask for Directions", theme, out directionsButtonLabel);
            directionsButton.onClick.RemoveAllListeners();
            directionsButton.onClick.AddListener(HandleDirectionsClicked);
            directionsButton.gameObject.SetActive(false);
        }

        private void BuildBoardContent(RectTransform contentArea, ShiftUiTheme theme)
        {
            boardContentRoot = new GameObject("BoardContent", typeof(RectTransform));
            boardContentRoot.transform.SetParent(contentArea, false);
            MenuUiBuilder.StretchRectToFill(boardContentRoot.GetComponent<RectTransform>());

            VerticalLayoutGroup rootLayout = boardContentRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(12, 12, 12, 12);
            rootLayout.spacing = 10f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            GameObject splitRow = new GameObject("SplitRow", typeof(RectTransform), typeof(LayoutElement));
            splitRow.transform.SetParent(boardContentRoot.transform, false);
            LayoutElement splitLayout = splitRow.GetComponent<LayoutElement>();
            splitLayout.flexibleHeight = 1f;
            splitLayout.minHeight = 320f;

            HorizontalLayoutGroup splitHBox = splitRow.AddComponent<HorizontalLayoutGroup>();
            splitHBox.spacing = 12f;
            splitHBox.childControlWidth = true;
            splitHBox.childControlHeight = true;
            splitHBox.childForceExpandWidth = true;
            splitHBox.childForceExpandHeight = true;

            BuildQuestListColumn(splitRow.transform, theme, out questPickerContent, out questListScroll);

            GameObject leftPanel = CreatePanel(splitRow.transform, "QuestInfoPanel", flexibleWidth: 1f);
            leftTitleText = CreateText(leftPanel.transform, "Select a quest", theme, 24f, FontStyles.Bold);
            leftDescriptionText = CreateText(leftPanel.transform, string.Empty, theme, 18f, FontStyles.Normal);
            leftDescriptionText.textWrappingMode = TextWrappingModes.Normal;
            leftObjectivesText = CreateText(leftPanel.transform, string.Empty, theme, 16f, FontStyles.Normal);
            leftObjectivesText.textWrappingMode = TextWrappingModes.Normal;
            leftObjectivesText.color = DarkMatterGenesisUiPalette.BodyText;

            GameObject rightPanel = CreatePanel(splitRow.transform, "QuestProgressPanel", flexibleWidth: 1f);
            rightStatusText = CreateText(rightPanel.transform, string.Empty, theme, 18f, FontStyles.Bold);
            rightProgressText = CreateText(rightPanel.transform, string.Empty, theme, 16f, FontStyles.Normal);
            rightProgressText.textWrappingMode = TextWrappingModes.Normal;
            rightXpText = CreateText(rightPanel.transform, string.Empty, theme, 18f, FontStyles.Bold);
            rightXpText.color = DarkMatterGenesisUiPalette.Gold;
            rightRewardsText = CreateText(rightPanel.transform, string.Empty, theme, 16f, FontStyles.Normal);
            rightRewardsText.textWrappingMode = TextWrappingModes.Normal;

            GameObject actionRow = new GameObject("ActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            actionRow.transform.SetParent(boardContentRoot.transform, false);
            HorizontalLayoutGroup actionRowLayout = actionRow.GetComponent<HorizontalLayoutGroup>();
            actionRowLayout.spacing = 10f;
            actionRowLayout.childControlWidth = true;
            actionRowLayout.childControlHeight = true;
            actionRowLayout.childForceExpandWidth = true;
            actionRowLayout.childForceExpandHeight = false;
            LayoutElement actionRowElement = actionRow.GetComponent<LayoutElement>();
            actionRowElement.minHeight = 48f;
            actionRowElement.preferredHeight = 48f;

            boardDirectionsButton = CreateButton(actionRow.transform, "Ask for Directions", theme, out boardDirectionsButtonLabel);
            boardDirectionsButton.onClick.RemoveAllListeners();
            boardDirectionsButton.onClick.AddListener(HandleDirectionsClicked);
            boardDirectionsButton.gameObject.SetActive(false);

            questActionButton = CreateButton(actionRow.transform, "Accept", theme, out questActionButtonLabel);
            questActionButton.onClick.RemoveAllListeners();
            questActionButton.onClick.AddListener(HandleQuestActionClicked);

            abandonQuestButton = CreateButton(actionRow.transform, "Abandon Quest", theme, out abandonQuestButtonLabel);
            abandonQuestButton.onClick.RemoveAllListeners();
            abandonQuestButton.onClick.AddListener(HandleAbandonClicked);
            Image abandonImage = abandonQuestButton.GetComponent<Image>();
            if (abandonImage != null)
                abandonImage.color = DarkMatterGenesisUiPalette.DeepMagenta;
            abandonQuestButton.gameObject.SetActive(false);

            boardContentRoot.SetActive(false);
        }

        private static void BuildQuestListColumn(Transform parent, ShiftUiTheme theme, out Transform listContent, out ScrollRect scroll)
        {
            GameObject listColumn = new GameObject("QuestListColumn", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            listColumn.transform.SetParent(parent, false);

            Image columnBg = listColumn.GetComponent<Image>();
            columnBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.95f);

            LayoutElement columnLayout = listColumn.GetComponent<LayoutElement>();
            columnLayout.flexibleWidth = 0.38f;
            columnLayout.minWidth = 220f;
            columnLayout.flexibleHeight = 1f;

            VerticalLayoutGroup columnVBox = listColumn.AddComponent<VerticalLayoutGroup>();
            columnVBox.padding = new RectOffset(8, 8, 8, 8);
            columnVBox.spacing = 6f;
            columnVBox.childControlWidth = true;
            columnVBox.childControlHeight = true;
            columnVBox.childForceExpandWidth = true;
            columnVBox.childForceExpandHeight = false;

            TextMeshProUGUI listHeader = CreateText(listColumn.transform, "Quests", theme, 18f, FontStyles.Bold);
            LayoutElement headerLayout = listHeader.gameObject.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 24f;

            GameObject scrollRoot = new GameObject("QuestScroll", typeof(RectTransform), typeof(LayoutElement));
            scrollRoot.transform.SetParent(listColumn.transform, false);
            LayoutElement scrollLayout = scrollRoot.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 120f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollRoot.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.padding = new RectOffset(2, 2, 2, 2);
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = scrollRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            listContent = contentObject.transform;
        }

        private static GameObject CreatePanel(Transform parent, string name, float preferredHeight = 0f, float flexibleWidth = 0f)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            panel.transform.SetParent(parent, false);

            Image bg = panel.GetComponent<Image>();
            bg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.92f);

            LayoutElement layout = panel.GetComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                layout.preferredHeight = preferredHeight;
                layout.minHeight = preferredHeight;
            }

            if (flexibleWidth > 0f)
            {
                layout.flexibleWidth = flexibleWidth;
                layout.flexibleHeight = 1f;
            }

            VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(12, 12, 12, 12);
            panelLayout.spacing = 8f;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;
            return panel;
        }

        private void PresentSimple(
            string speakerName,
            string message,
            Action closedCallback,
            string primaryLabel,
            Action directionsCallback,
            Transform anchor)
        {
            onClosed = closedCallback;
            onDirectionsRequested = directionsCallback;
            npcAnchor = anchor;
            titleText.text = string.IsNullOrEmpty(speakerName) ? "Quest Giver" : speakerName;
            bodyText.text = message ?? string.Empty;
            simpleContentRoot.SetActive(true);
            boardContentRoot.SetActive(false);
            primaryButtonLabel.text = string.IsNullOrEmpty(primaryLabel) ? "Continue" : primaryLabel;
            primaryButton.interactable = true;
            ApplyDirectionsButtons();
            OpenOverlay();
        }

        private void PresentQuestBoard(
            string speakerName,
            string introMessage,
            IList<QuestBoardEntry> entries,
            Action closedCallback,
            Action directionsCallback,
            Transform anchor)
        {
            onClosed = closedCallback;
            onDirectionsRequested = directionsCallback;
            npcAnchor = anchor;
            titleText.text = string.IsNullOrEmpty(speakerName) ? "Quest Giver" : speakerName;
            currentEntries = entries;
            selectedEntryIndex = entries != null && entries.Count > 0 ? 0 : -1;
            abandonConfirmPending = false;

            simpleContentRoot.SetActive(false);
            boardContentRoot.SetActive(true);

            ClearQuestPicker();
            ShiftUiTheme theme = ShiftUiTheme.Current;

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    int capturedIndex = i;
                    CreateQuestPickerButton(entries[i], capturedIndex, theme);
                }
            }

            if (selectedEntryIndex >= 0)
                SelectEntry(selectedEntryIndex);
            else
            {
                leftTitleText.text = "No quests available";
                leftDescriptionText.text = introMessage ?? string.Empty;
                leftObjectivesText.text = string.Empty;
                rightStatusText.text = string.Empty;
                rightProgressText.text = string.Empty;
                rightXpText.text = string.Empty;
                rightRewardsText.text = string.Empty;
                questActionButton.interactable = false;
                questActionButtonLabel.text = "Close";
                if (abandonQuestButton != null)
                    abandonQuestButton.gameObject.SetActive(false);
            }

            ApplyDirectionsButtons();
            RefreshQuestListLayout();
            OpenOverlay();
        }

        private void ApplyDirectionsButtons()
        {
            bool showDirections = onDirectionsRequested != null;

            if (directionsButton != null)
            {
                directionsButton.gameObject.SetActive(showDirections);
                directionsButton.interactable = showDirections;
            }

            if (boardDirectionsButton != null)
            {
                boardDirectionsButton.gameObject.SetActive(showDirections);
                boardDirectionsButton.interactable = showDirections;
            }
        }

        private void RefreshQuestListLayout()
        {
            if (questPickerContent is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            if (questListScroll != null)
                questListScroll.verticalNormalizedPosition = 1f;
        }

        private void CreateQuestPickerButton(QuestBoardEntry entry, int index, ShiftUiTheme theme)
        {
            GameObject row = new GameObject($"QuestRow_{index}", typeof(RectTransform));
            row.transform.SetParent(questPickerContent, false);

            Image bg = row.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = QuestUiPalette.GetRowBackgroundColor(entry.Status, index == selectedEntryIndex, theme);

            Button button = row.AddComponent<Button>();
            button.targetGraphic = bg;
            UiSoundHelper.BindButton(button);

            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 58f;
            layout.preferredHeight = 58f;
            layout.flexibleWidth = 1f;

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(10, 10, 8, 8);
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform));
            textColumn.transform.SetParent(row.transform, false);
            VerticalLayoutGroup textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            LayoutElement textColumnLayout = textColumn.AddComponent<LayoutElement>();
            textColumnLayout.flexibleWidth = 1f;

            TextMeshProUGUI title = CreateText(textColumn.transform, entry.Title, theme, 16f, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.TopLeft;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.color = QuestUiPalette.GetTitleColor(entry.Status, theme);

            TextMeshProUGUI status = CreateText(textColumn.transform, QuestUiPalette.GetStatusLabel(entry.Status), theme, 13f, FontStyles.Normal);
            status.alignment = TextAlignmentOptions.TopLeft;
            status.color = QuestUiPalette.GetStatusLabelColor(entry.Status, theme);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectEntry(index));
        }

        private void SelectEntry(int index)
        {
            if (currentEntries == null || index < 0 || index >= currentEntries.Count)
                return;

            selectedEntryIndex = index;
            QuestBoardEntry entry = currentEntries[index];

            leftTitleText.text = entry.Title;
            leftDescriptionText.text = string.IsNullOrWhiteSpace(entry.Description) ? entry.Detail : entry.Description;
            leftObjectivesText.text = string.IsNullOrWhiteSpace(entry.ObjectivesSummary)
                ? "Objectives unavailable."
                : entry.ObjectivesSummary;

            rightStatusText.text = QuestUiPalette.GetStatusLabel(entry.Status);
            rightStatusText.color = QuestUiPalette.GetStatusLabelColor(entry.Status, ShiftUiTheme.Current);
            rightProgressText.text = string.IsNullOrWhiteSpace(entry.ProgressSummary)
                ? "Progress unavailable."
                : entry.ProgressSummary;
            rightXpText.text = $"XP Reward: {Mathf.Max(0, entry.XpReward)}";
            rightRewardsText.text = string.IsNullOrWhiteSpace(entry.RewardsSummary)
                ? "No item rewards."
                : entry.RewardsSummary;

            questActionButtonLabel.text = string.IsNullOrEmpty(entry.ActionLabel) ? "Continue" : entry.ActionLabel;
            questActionButton.interactable = entry.CanSelect && entry.OnSelected != null;

            bool canAbandon = entry.CanAbandon && entry.OnAbandon != null;
            if (abandonQuestButton != null)
            {
                abandonQuestButton.gameObject.SetActive(canAbandon);
                abandonQuestButton.interactable = canAbandon;
                abandonConfirmPending = false;
                abandonQuestButtonLabel.text = "Abandon Quest";
            }

            RefreshPickerHighlights();
        }

        private void RefreshPickerHighlights()
        {
            if (questPickerContent == null || currentEntries == null)
                return;

            ShiftUiTheme theme = ShiftUiTheme.Current;
            for (int i = 0; i < questPickerContent.childCount; i++)
            {
                Transform child = questPickerContent.GetChild(i);
                Image bg = child.GetComponent<Image>();
                if (bg == null || i >= currentEntries.Count)
                    continue;

                bg.color = QuestUiPalette.GetRowBackgroundColor(currentEntries[i].Status, i == selectedEntryIndex, theme);
            }
        }

        private void HandleQuestActionClicked()
        {
            if (currentEntries == null || selectedEntryIndex < 0 || selectedEntryIndex >= currentEntries.Count)
            {
                Close();
                return;
            }

            QuestBoardEntry entry = currentEntries[selectedEntryIndex];
            if (!entry.CanSelect || entry.OnSelected == null)
            {
                Close();
                return;
            }

            Action callback = entry.OnSelected;
            Close();
            callback.Invoke();
        }

        private void HandleDirectionsClicked()
        {
            if (onDirectionsRequested == null)
                return;

            Action callback = onDirectionsRequested;
            Close();
            callback.Invoke();
        }

        private void HandleAbandonClicked()
        {
            if (currentEntries == null || selectedEntryIndex < 0 || selectedEntryIndex >= currentEntries.Count)
                return;

            QuestBoardEntry entry = currentEntries[selectedEntryIndex];
            if (!entry.CanAbandon || entry.OnAbandon == null)
                return;

            if (!abandonConfirmPending)
            {
                abandonConfirmPending = true;
                abandonQuestButtonLabel.text = "Confirm Abandon?";
                return;
            }

            abandonConfirmPending = false;
            Action callback = entry.OnAbandon;
            Close();
            callback.Invoke();
        }

        private void ClearQuestPicker()
        {
            if (questPickerContent == null)
                return;

            for (int i = questPickerContent.childCount - 1; i >= 0; i--)
                Destroy(questPickerContent.GetChild(i).gameObject);
        }

        private void OpenOverlay()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuestDialog, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            EnforceQuestDialogLayout();
            UiFrontLayer.BringLayerToFront(transform.parent);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 1f;

            proximityFade?.BeginMonitoring(npcAnchor, overlayCanvasGroup, Close);
        }

        private void EnforceQuestDialogLayout()
        {
            MenuUiBuilder.StretchRectToFill(GetComponent<RectTransform>());

            if (overlayRoot == null)
                return;

            MenuUiBuilder.StretchRectToFill(overlayRoot.GetComponent<RectTransform>());

            if (dialogPanel == null)
                return;

            MenuUiBuilder.ApplyCenteredModalShellLayout(dialogPanel, GameplayHudLayout.QuestGiverModalSize);
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

        private void Close()
        {
            proximityFade?.StopMonitoring();

            if (overlayRoot != null)
                overlayRoot.SetActive(false);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 1f;

            npcAnchor = null;

            ClearQuestPicker();
            currentEntries = null;
            selectedEntryIndex = -1;
            abandonConfirmPending = false;
            onDirectionsRequested = null;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuestDialog, false);

            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private static TextMeshProUGUI CreateText(Transform parent, string value, ShiftUiTheme theme, float size, FontStyles style)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(text, semiBold: style == FontStyles.Bold);
            else
                TmpUiHelper.ApplyDefaultFont(text);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = theme != null ? theme.secondaryTextColor : Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, ShiftUiTheme theme, out TextMeshProUGUI labelText)
        {
            GameObject buttonObject = new GameObject("Button", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;

            Image image = buttonObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.color = DarkMatterGenesisUiPalette.ButtonNormal;
            image.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            UiSoundHelper.BindButton(button);

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            labelText = textObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(labelText, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(labelText);
            labelText.text = label;
            labelText.fontSize = 24f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }
    }
}
