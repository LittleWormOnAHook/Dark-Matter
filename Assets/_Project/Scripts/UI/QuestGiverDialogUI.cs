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
        private GameObject questListRoot;
        private Transform questListContent;
        private Button primaryButton;
        private TextMeshProUGUI primaryButtonLabel;
        private Action onClosed;
        private bool built;

        public static QuestGiverDialogUI Instance => instance;

        public static QuestGiverDialogUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("QuestGiverDialogUI", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<QuestGiverDialogUI>();
            instance.Build(canvasRoot);
            return instance;
        }

        public static void Show(string speakerName, string message, Action closedCallback = null, string primaryLabel = "Continue")
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            QuestGiverDialogUI dialog = EnsureExists(canvas.transform);
            dialog.PresentSimple(speakerName, message, closedCallback, primaryLabel);
        }

        public static void ShowQuestBoard(string speakerName, string introMessage, IList<QuestBoardEntry> entries, Action closedCallback = null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            QuestGiverDialogUI dialog = EnsureExists(canvas.transform);
            dialog.PresentQuestBoard(speakerName, introMessage, entries, closedCallback);
        }

        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            EnsureUiInput(canvasRoot);
            ShiftUiTheme theme = ShiftUiTheme.Current;

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "DialogOverlay", new Color(0f, 0f, 0f, 0.45f), blockRaycasts: true);
            overlayRoot.transform.SetAsLastSibling();

            dialogPanel = new GameObject("DialogPanel", typeof(RectTransform));
            dialogPanel.transform.SetParent(overlayRoot.transform, false);
            RectTransform panelRect = dialogPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(680f, 460f);

            Image panelBg = dialogPanel.AddComponent<Image>();
            if (theme != null)
                theme.ApplyPanelImage(panelBg, large: true);
            else
            {
                MenuUiBuilder.ApplyUiSprite(panelBg);
                panelBg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
            }

            VerticalLayoutGroup layout = dialogPanel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 18, 18);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            titleText = CreateText(dialogPanel.transform, "NPC", theme, 34f, FontStyles.Bold);
            bodyText = CreateText(dialogPanel.transform, "", theme, 22f, FontStyles.Normal);
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            questListRoot = new GameObject("QuestList", typeof(RectTransform));
            questListRoot.transform.SetParent(dialogPanel.transform, false);
            LayoutElement listLayout = questListRoot.AddComponent<LayoutElement>();
            listLayout.flexibleHeight = 1f;
            listLayout.minHeight = 180f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(questListRoot.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = questListRoot.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            questListContent = content.transform;

            primaryButton = CreateButton(dialogPanel.transform, "Close", theme, out primaryButtonLabel);
            primaryButton.onClick.AddListener(Close);

            questListRoot.SetActive(false);
            overlayRoot.SetActive(false);
            UiFrontLayer.BringLayerToFront(canvasRoot);
        }

        private void PresentSimple(string speakerName, string message, Action closedCallback, string primaryLabel)
        {
            onClosed = closedCallback;
            titleText.text = string.IsNullOrEmpty(speakerName) ? "Quest Giver" : speakerName;
            bodyText.text = message ?? string.Empty;
            bodyText.gameObject.SetActive(true);
            questListRoot.SetActive(false);
            primaryButtonLabel.text = string.IsNullOrEmpty(primaryLabel) ? "Continue" : primaryLabel;
            primaryButton.interactable = true;
            OpenOverlay();
        }

        private void PresentQuestBoard(string speakerName, string introMessage, IList<QuestBoardEntry> entries, Action closedCallback)
        {
            onClosed = closedCallback;
            titleText.text = string.IsNullOrEmpty(speakerName) ? "Quest Giver" : speakerName;
            bodyText.text = introMessage ?? "Choose a quest:";
            bodyText.gameObject.SetActive(true);
            questListRoot.SetActive(true);
            primaryButtonLabel.text = "Close";
            primaryButton.interactable = true;

            ClearQuestList();
            ShiftUiTheme theme = ShiftUiTheme.Current;

            for (int i = 0; i < entries.Count; i++)
            {
                QuestBoardEntry entry = entries[i];
                CreateQuestListRow(entry, theme);
            }

            OpenOverlay();
        }

        private void CreateQuestListRow(QuestBoardEntry entry, ShiftUiTheme theme)
        {
            GameObject row = new GameObject("QuestRow", typeof(RectTransform));
            row.transform.SetParent(questListContent, false);

            Image rowBg = row.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(rowBg);
            rowBg.color = entry.CanSelect
                ? QuestUiPalette.GetRowBackgroundColor(entry.Status, false, theme)
                : QuestUiPalette.GetRowBackgroundColor(entry.Status, false, theme) * new Color(1f, 1f, 1f, 0.82f);

            Button button = row.AddComponent<Button>();
            button.targetGraphic = rowBg;
            button.interactable = entry.CanSelect;
            UiSoundHelper.BindButton(button);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 10, 10);
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            LayoutElement rowElement = row.AddComponent<LayoutElement>();
            rowElement.minHeight = 72f;
            rowElement.preferredHeight = 72f;

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform));
            textColumn.transform.SetParent(row.transform, false);
            VerticalLayoutGroup textLayout = textColumn.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 4f;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            LayoutElement textColumnLayout = textColumn.AddComponent<LayoutElement>();
            textColumnLayout.flexibleWidth = 1f;

            TextMeshProUGUI title = CreateText(textColumn.transform, entry.Title, theme, 22f, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.TopLeft;
            title.color = QuestUiPalette.GetTitleColor(entry.Status, theme);
            TextMeshProUGUI detail = CreateText(textColumn.transform, entry.Detail, theme, 16f, FontStyles.Normal);
            detail.alignment = TextAlignmentOptions.TopLeft;
            detail.color = QuestUiPalette.GetStatusLabelColor(entry.Status, theme) * new Color(1f, 1f, 1f, 0.92f);
            detail.textWrappingMode = TextWrappingModes.Normal;

            GameObject actionColumn = new GameObject("Action", typeof(RectTransform));
            actionColumn.transform.SetParent(row.transform, false);
            LayoutElement actionLayout = actionColumn.AddComponent<LayoutElement>();
            actionLayout.minWidth = 120f;
            actionLayout.preferredWidth = 120f;

            TextMeshProUGUI actionLabel = CreateText(actionColumn.transform, entry.ActionLabel, theme, 18f, FontStyles.Bold);
            actionLabel.alignment = TextAlignmentOptions.MidlineRight;
            actionLabel.color = QuestUiPalette.GetStatusLabelColor(entry.Status, theme);

            if (entry.CanSelect && entry.OnSelected != null)
            {
                Action captured = entry.OnSelected;
                button.onClick.AddListener(() =>
                {
                    Close();
                    captured.Invoke();
                });
            }
        }

        private void ClearQuestList()
        {
            if (questListContent == null)
                return;

            for (int i = questListContent.childCount - 1; i >= 0; i--)
                Destroy(questListContent.GetChild(i).gameObject);
        }

        private void OpenOverlay()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            UiFrontLayer.BringLayerToFront(transform.parent);
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
            overlayRoot.SetActive(false);
            ClearQuestList();

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(false);

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
            GameObject buttonObject = new GameObject("PrimaryButton", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;

            Image image = buttonObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);
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
