using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// GDD Phase E — dramatic fullscreen Neural Echo rescue reveal (prototype shell).
    /// </summary>
    public class EchoRescueRevealUI : MonoBehaviour
    {
        private static EchoRescueRevealUI instance;

        private GameObject overlayRoot;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI echoNameLabel;
        private TextMeshProUGUI bodyLabel;
        private Action onClosed;
        private bool built;

        public static EchoRescueRevealUI Instance => instance;

        public static void Show(string echoDisplayName, string classLine, string abilitySummary, Action closedCallback = null)
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null)
                return;

            EchoRescueRevealUI ui = EnsureExists(canvas.transform);
            ui.Present(echoDisplayName, classLine, abilitySummary, closedCallback);
        }

        public static EchoRescueRevealUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("EchoRescueRevealUI", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<EchoRescueRevealUI>();
            instance.Build(canvasRoot);
            return instance;
        }

        private static Canvas ResolveCanvas()
        {
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null && uiManager.TryGetComponent(out Canvas canvas))
                return canvas;

            return FindAnyObjectByType<Canvas>();
        }

        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            transform.SetParent(canvasRoot, false);
            MenuUiBuilder.StretchRectToFill(GetComponent<RectTransform>());

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(
                transform,
                "EchoRevealOverlay",
                SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.92f),
                blockRaycasts: true);

            RectTransform contentArea;
            Button closeButton;
            GameObject shell = MenuUiBuilder.CreateFullscreenShell(overlayRoot.transform, "Neural Echo Rescued", out contentArea, out closeButton);
            closeButton.onClick.AddListener(Close);

            Image shellBg = shell.GetComponent<Image>();
            if (shellBg != null)
                shellBg.color = SurvivalPioneerUiPalette.PanelBackground;

            VerticalLayoutGroup layout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject iconBlock = new GameObject("EchoIcon", typeof(RectTransform), typeof(Image));
            iconBlock.transform.SetParent(contentArea, false);
            LayoutElement iconLayout = iconBlock.AddComponent<LayoutElement>();
            iconLayout.minHeight = 120f;
            iconLayout.preferredHeight = 120f;
            Image iconImage = iconBlock.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(iconImage);
            iconImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.72f);

            ShiftUiTheme theme = ShiftUiTheme.Current;

            titleLabel = CreateText(contentArea, "RESONANCE IMPRINT STABILIZED", theme, 26f, FontStyles.Bold);
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color = SurvivalPioneerUiPalette.RichFuchsia;

            echoNameLabel = CreateText(contentArea, "Echo Name", theme, 34f, FontStyles.Bold);
            echoNameLabel.alignment = TextAlignmentOptions.Center;

            bodyLabel = CreateText(contentArea, string.Empty, theme, 20f, FontStyles.Normal);
            bodyLabel.alignment = TextAlignmentOptions.TopLeft;
            bodyLabel.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement bodyLayout = bodyLabel.gameObject.AddComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1f;
            bodyLayout.minHeight = 160f;

            Button continueButton = MenuUiBuilder.CreateButton(contentArea, "Integrate Echo", new Vector2(280f, 52f), 22f);
            continueButton.onClick.AddListener(Close);

            overlayRoot.SetActive(false);
        }

        private void Present(string echoDisplayName, string classLine, string abilitySummary, Action closedCallback)
        {
            onClosed = closedCallback;
            echoNameLabel.text = string.IsNullOrWhiteSpace(echoDisplayName) ? "Unknown Echo" : echoDisplayName;

            string classText = string.IsNullOrWhiteSpace(classLine) ? "Unclassified imprint" : classLine;
            string abilityText = string.IsNullOrWhiteSpace(abilitySummary) ? "Ability matrix pending analysis." : abilitySummary;
            bodyLabel.text = $"{classText}\n\n{abilityText}\n\nThis pioneer can be assigned to base structures or listed on the Pioneer Exchange after training.";

            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Close()
        {
            overlayRoot.SetActive(false);
            Action callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string value, ShiftUiTheme theme, float size, FontStyles style)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            if (theme != null)
                theme.ApplyFont(text, semiBold: style == FontStyles.Bold, bold: style == FontStyles.Bold);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = theme != null ? theme.secondaryTextColor : SurvivalPioneerUiPalette.BodyText;
            text.raycastTarget = false;
            return text;
        }
    }
}
