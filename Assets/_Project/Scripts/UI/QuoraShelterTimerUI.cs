using Project.Core;
using Project.Shelter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// HUD countdown shown while the player is inside a deployed Quora Shelter.
    /// Reflects <see cref="QuoraShelterController.RemainingLifetimeSeconds"/>.
    /// </summary>
    public sealed class QuoraShelterTimerUI : MonoBehaviour
    {
        private const float PanelWidth = 220f;
        private const float TopInset = 72f;

        private static QuoraShelterTimerUI instance;

        private GameObject panelRoot;
        private TextMeshProUGUI captionLabel;
        private TextMeshProUGUI countdownLabel;
        private QuoraShelterController activeShelter;
        private int lastDisplayedSecond = -1;

        public static bool IsVisible =>
            instance != null && instance.panelRoot != null && instance.panelRoot.activeSelf;

        public static QuoraShelterTimerUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
            {
                if (canvasRoot != null && instance.transform.parent != canvasRoot)
                    instance.transform.SetParent(canvasRoot, false);
                return instance;
            }

            GameObject host = new GameObject("QuoraShelterTimer", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<QuoraShelterTimerUI>();
            instance.Build();
            return instance;
        }

        public static void Show(QuoraShelterController shelter)
        {
            if (shelter == null)
                return;

            Canvas canvas = ResolveGameplayCanvas();
            if (canvas == null)
                return;

            EnsureExists(canvas.transform).Present(shelter);
        }

        public static void Hide()
        {
            if (instance == null)
                return;

            instance.activeShelter = null;
            instance.lastDisplayedSecond = -1;
            if (instance.panelRoot != null)
                instance.panelRoot.SetActive(false);
        }

        private void Build()
        {
            RectTransform hostRect = transform as RectTransform;
            if (hostRect != null)
            {
                hostRect.anchorMin = Vector2.zero;
                hostRect.anchorMax = Vector2.one;
                hostRect.offsetMin = Vector2.zero;
                hostRect.offsetMax = Vector2.zero;
            }

            panelRoot = new GameObject("TimerPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelRoot.transform.SetParent(transform, false);

            Image panelImage = panelRoot.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.92f);
            panelImage.raycastTarget = false;
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(panelRoot);

            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -TopInset);
            panelRect.sizeDelta = new Vector2(PanelWidth, 0f);

            VerticalLayoutGroup layout = panelRoot.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelRoot.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            captionLabel = CreateLabel(panelRoot.transform, "Deploy time remaining", 14f, DarkMatterGenesisUiPalette.MutedText);
            countdownLabel = CreateLabel(panelRoot.transform, "10:00", 28f, DarkMatterGenesisUiPalette.Gold);

            panelRoot.SetActive(false);
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, Color color)
        {
            GameObject textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private void Present(QuoraShelterController shelter)
        {
            activeShelter = shelter;
            lastDisplayedSecond = -1;
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            RefreshCountdown(force: true);
        }

        private void Update()
        {
            if (!IsVisible || activeShelter == null)
                return;

            if (!activeShelter.IsOccupied)
            {
                Hide();
                return;
            }

            RefreshCountdown(force: false);
        }

        private void RefreshCountdown(bool force)
        {
            float remaining = Mathf.Max(0f, activeShelter.RemainingLifetimeSeconds);
            int wholeSeconds = Mathf.FloorToInt(remaining);

            if (!force && wholeSeconds == lastDisplayedSecond)
                return;

            lastDisplayedSecond = wholeSeconds;

            int minutes = wholeSeconds / 60;
            int seconds = wholeSeconds % 60;
            countdownLabel.text = $"{minutes:00}:{seconds:00}";

            if (wholeSeconds <= 60)
                countdownLabel.color = DarkMatterGenesisUiPalette.DeepMagenta;
            else if (wholeSeconds <= 120)
                countdownLabel.color = DarkMatterGenesisUiPalette.Gold;
            else
                countdownLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
        }

        private static Canvas ResolveGameplayCanvas()
        {
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                Canvas uiCanvas = uiManager.GetComponent<Canvas>();
                if (uiCanvas != null)
                    return uiCanvas;
            }

            GameObject mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return Object.FindAnyObjectByType<Canvas>();
        }
    }
}
