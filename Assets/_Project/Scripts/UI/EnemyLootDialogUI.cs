using System;
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
        private IEnemyLootProvider activeLootProvider;
        private Action onClosed;
        private bool built;

        public static EnemyLootDialogUI Instance => instance;

        public static bool IsDialogOpen => instance != null && instance.overlayRoot != null && instance.overlayRoot.activeSelf;

        public static void CloseAnyOpenLoot()
        {
            if (instance != null && IsDialogOpen)
                instance.Close();
        }

        public static void Show(IEnemyLootProvider lootProvider, string enemyName, string lootSummary)
        {
            Canvas canvas = ResolveGameplayCanvas();
            if (canvas == null || lootProvider == null)
                return;

            EnemyLootDialogUI dialog = EnsureExists(canvas.transform);
            dialog.Present(lootProvider, enemyName, lootSummary);
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
            MenuUiBuilder.StretchRectToFill(host.GetComponent<RectTransform>());
            instance = host.AddComponent<EnemyLootDialogUI>();
            instance.Build(canvasRoot);
            return instance;
        }

        /// <summary>
        /// Destroys any existing loot dialog host and rebuilds the default compact centered popup.
        /// </summary>
        public static void ResetToDefaultLayout()
        {
            TeardownAllInstances();
            Canvas canvas = ResolveGameplayCanvas();
            if (canvas != null)
                EnsureExists(canvas.transform);
        }

        public static void EnsureBuiltForLayoutEditor()
        {
            Canvas canvas = ResolveGameplayCanvas();
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

            EnemyLootDialogUI[] existing = FindObjectsByType<EnemyLootDialogUI>(FindObjectsInactive.Include);
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
            ReleaseLootInputCapture();
            built = false;
            activeLootProvider = null;
            onClosed = null;
            overlayRoot = null;
            dialogPanel = null;
            titleText = null;
            bodyText = null;
            lootButton = null;
            lootAllButton = null;
            closeButton = null;

            if (instance == this)
                instance = null;

            DestroyUiHost(gameObject);
        }

        private void OnDestroy()
        {
            ReleaseLootInputCapture();

            if (instance == this)
                instance = null;
        }

        private void ReleaseLootInputCapture()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetLootDialogOpen(false);
        }

        private static void DestroyUiHost(GameObject host)
        {
            if (host == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(host);
                return;
            }
#endif
            UnityEngine.Object.Destroy(host);
        }

        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            EnsureUiInput(canvasRoot);
            ShiftUiTheme theme = ShiftUiTheme.Current;

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "LootOverlay", new Color(0f, 0f, 0f, 0.5f), blockRaycasts: true);
            overlayRoot.transform.SetAsLastSibling();

            dialogPanel = MenuUiBuilder.CreateCenteredModalShell(
                overlayRoot.transform,
                "Loot",
                GameplayHudLayout.GameplayModalSize,
                out RectTransform contentArea,
                out Button headerCloseButton);
            titleText = MenuUiBuilder.GetShellTitleText(dialogPanel);
            headerCloseButton.onClick.AddListener(Close);

            VerticalLayoutGroup layout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            bodyText = CreateText(contentArea, "", theme, 22f, FontStyles.Normal);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1f;
            bodyLayout.minHeight = 72f;

            GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform));
            buttonRow.transform.SetParent(contentArea, false);
            HorizontalLayoutGroup buttonLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10f;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;
            LayoutElement buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
            buttonRowLayout.minHeight = 48f;
            buttonRowLayout.preferredHeight = 48f;

            lootButton = CreateButton(buttonRow.transform, "Loot", theme, out _);
            lootAllButton = CreateButton(buttonRow.transform, "Loot All", theme, out _);
            closeButton = CreateButton(buttonRow.transform, "Close", theme, out _);

            lootButton.onClick.AddListener(OnLootClicked);
            lootAllButton.onClick.AddListener(OnLootAllClicked);
            closeButton.onClick.AddListener(Close);

            EnforceLootDialogLayout();
            overlayRoot.SetActive(false);
            UiFrontLayer.BringLayerToFront(canvasRoot);
        }

        private void Present(IEnemyLootProvider lootProvider, string enemyName, string lootSummary)
        {
            activeLootProvider = lootProvider;
            onClosed = null;
            titleText.text = string.IsNullOrWhiteSpace(enemyName) ? "Loot" : $"Loot — {enemyName}";
            bodyText.text = lootSummary ?? string.Empty;
            RefreshButtonStates();
            OpenOverlay();
        }

        private void RefreshButtonStates()
        {
            bool hasLoot = activeLootProvider != null && activeLootProvider.HasRemainingLoot;
            lootButton.interactable = hasLoot;
            lootAllButton.interactable = hasLoot;
            if (activeLootProvider != null)
                bodyText.text = activeLootProvider.BuildLootSummary();
        }

        private void OnLootClicked()
        {
            if (activeLootProvider == null)
                return;

            activeLootProvider.TryLootNextEntry();
            RefreshButtonStates();

            if (activeLootProvider == null || !activeLootProvider.HasRemainingLoot)
                Close();
        }

        private void OnLootAllClicked()
        {
            if (activeLootProvider == null)
                return;

            activeLootProvider.TryLootAll();
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
            EnforceLootDialogLayout();
            if (transform.parent != null)
                UiFrontLayer.BringLayerToFront(transform.parent);
            Canvas.ForceUpdateCanvases();
        }

        private void Close()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(false);

            activeLootProvider = null;
            ReleaseLootInputCapture();

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

        private void EnforceLootDialogLayout()
        {
            MenuUiBuilder.StretchRectToFill(GetComponent<RectTransform>());

            if (overlayRoot == null)
                return;

            MenuUiBuilder.StretchRectToFill(overlayRoot.GetComponent<RectTransform>());

            if (dialogPanel == null)
                return;

            MenuUiBuilder.ApplyCenteredModalShellLayout(dialogPanel, GameplayHudLayout.GameplayModalSize);
        }
    }
}
