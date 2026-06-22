using System;
using System.Collections.Generic;
using Project.AI;
using Project.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project.UI
{
    public class EnemyLootDialogUI : MonoBehaviour
    {
        private static EnemyLootDialogUI instance;

        private GameObject overlayRoot;
        private GameObject dialogPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Button lootButton;
        private Button lootAllButton;
        private Button closeButton;
        private EnemyLootable activeLootable;
        private Action onClosed;
        private bool built;

        public static EnemyLootDialogUI Instance => instance;

        public static bool IsDialogOpen => instance != null && instance.overlayRoot != null && instance.overlayRoot.activeSelf;

        public static void Show(EnemyLootable lootable, string enemyName, string lootSummary)
        {
            Canvas canvas = ResolveGameplayCanvas();
            if (canvas == null || lootable == null)
                return;

            EnemyLootDialogUI dialog = EnsureExists(canvas.transform);
            dialog.Present(lootable, enemyName, lootSummary);
        }

        private static Canvas ResolveGameplayCanvas()
        {
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                Canvas uiCanvas = uiManager.GetComponent<Canvas>();
                if (uiCanvas != null)
                    return uiCanvas;
            }

            GameObject mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return FindAnyObjectByType<Canvas>();
        }

        public static EnemyLootDialogUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("EnemyLootDialogUI", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<EnemyLootDialogUI>();
            instance.Build(canvasRoot);
            return instance;
        }

        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            EnsureUiInput(canvasRoot);
            ShiftUiTheme theme = ShiftUiTheme.Current;

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "LootOverlay", new Color(0f, 0f, 0f, 0.45f), blockRaycasts: true);
            overlayRoot.transform.SetAsLastSibling();

            dialogPanel = new GameObject("LootDialogPanel", typeof(RectTransform));
            dialogPanel.transform.SetParent(overlayRoot.transform, false);
            RectTransform panelRect = dialogPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(560f, 360f);

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

            titleText = CreateText(dialogPanel.transform, "Loot", theme, 34f, FontStyles.Bold);
            bodyText = CreateText(dialogPanel.transform, "", theme, 22f, FontStyles.Normal);
            bodyText.textWrappingMode = TextWrappingModes.Normal;

            GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform));
            buttonRow.transform.SetParent(dialogPanel.transform, false);
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            lootButton = CreateButton(buttonRow.transform, "Loot", theme, out _);
            lootAllButton = CreateButton(buttonRow.transform, "Loot All", theme, out _);
            closeButton = CreateButton(buttonRow.transform, "Close", theme, out _);

            lootButton.onClick.AddListener(OnLootClicked);
            lootAllButton.onClick.AddListener(OnLootAllClicked);
            closeButton.onClick.AddListener(Close);

            overlayRoot.SetActive(false);
            UiFrontLayer.BringLayerToFront(canvasRoot);
        }

        private void Present(EnemyLootable lootable, string enemyName, string lootSummary)
        {
            activeLootable = lootable;
            onClosed = null;
            titleText.text = string.IsNullOrWhiteSpace(enemyName) ? "Loot" : $"Loot — {enemyName}";
            bodyText.text = lootSummary ?? string.Empty;
            RefreshButtonStates();
            OpenOverlay();
        }

        private void RefreshButtonStates()
        {
            bool hasLoot = activeLootable != null && activeLootable.HasRemainingLoot;
            lootButton.interactable = hasLoot;
            lootAllButton.interactable = hasLoot;
            if (activeLootable != null)
                bodyText.text = activeLootable.BuildLootSummary();
        }

        private void OnLootClicked()
        {
            if (activeLootable == null)
                return;

            activeLootable.TryLootNextEntry();
            RefreshButtonStates();

            if (activeLootable == null || !activeLootable.HasRemainingLoot)
                Close();
        }

        private void OnLootAllClicked()
        {
            if (activeLootable == null)
                return;

            activeLootable.TryLootAll();
            Close();
        }

        private void OpenOverlay()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetLootDialogOpen(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            if (transform.parent != null)
                UiFrontLayer.BringLayerToFront(transform.parent);
            Canvas.ForceUpdateCanvases();
        }

        private void Close()
        {
            overlayRoot.SetActive(false);
            activeLootable = null;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetLootDialogOpen(false);

            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
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
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;
            layout.flexibleWidth = 1f;

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
            labelText.fontSize = 22f;
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
