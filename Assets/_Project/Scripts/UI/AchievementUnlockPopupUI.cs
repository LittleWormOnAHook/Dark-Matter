using System.Collections;
using System.Collections.Generic;
using Project.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class AchievementUnlockPopupUI : MonoBehaviour
    {
        private struct PendingPopup
        {
            public string Title;
            public string Description;
            public int XpReward;
        }

        private static AchievementUnlockPopupUI instance;

        private RectTransform popupRect;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI headerLabel;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI descriptionLabel;
        private TextMeshProUGUI xpLabel;
        private GameObject descriptionRow;
        private GameObject xpRow;
        private Transform canvasRoot;
        private Vector2 restAnchoredPosition;

        private readonly Queue<PendingPopup> pendingPopups = new Queue<PendingPopup>();
        private Coroutine activeRoutine;

        public static AchievementUnlockPopupUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("AchievementUnlockPopupUI", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            instance = host.AddComponent<AchievementUnlockPopupUI>();
            instance.Build(canvasRootTransform);
            return instance;
        }

        public static void Show(string title, string description, int xpReward)
        {
            if (string.IsNullOrEmpty(title))
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            AchievementUnlockPopupUI popup = EnsureExists(canvas.transform);
            popup.Enqueue(title, description, xpReward);
        }

        private void Enqueue(string title, string description, int xpReward)
        {
            pendingPopups.Enqueue(new PendingPopup
            {
                Title = title,
                Description = description,
                XpReward = xpReward
            });

            if (activeRoutine == null)
                activeRoutine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            while (pendingPopups.Count > 0)
            {
                PendingPopup pending = pendingPopups.Dequeue();
                yield return PresentOne(pending);
            }

            activeRoutine = null;
        }

        private void Build(Transform canvasRootTransform)
        {
            canvasRoot = canvasRootTransform;
            ShiftUiTheme theme = ShiftUiTheme.Current;

            popupRect = transform as RectTransform;
            popupRect.anchorMin = new Vector2(0.5f, 1f);
            popupRect.anchorMax = new Vector2(0.5f, 1f);
            popupRect.pivot = new Vector2(0.5f, 1f);
            restAnchoredPosition = new Vector2(0f, -140f);
            popupRect.anchoredPosition = restAnchoredPosition;
            popupRect.sizeDelta = new Vector2(520f, 0f);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 1f);
            cardRect.anchorMax = new Vector2(0.5f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(520f, 0f);

            Image cardBg = card.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(cardBg);
            cardBg.color = SurvivalPioneerUiPalette.PanelBackground;
            cardBg.raycastTarget = false;
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(card);

            VerticalLayoutGroup cardLayout = card.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(0, 0, 0, 14);
            cardLayout.spacing = 0f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            GameObject headerBar = MenuUiBuilder.CreatePanelTitleBar(card.transform, "ACHIEVEMENT UNLOCKED", 34f, 13f);
            headerLabel = headerBar.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (headerLabel != null)
            {
                headerLabel.alignment = TextAlignmentOptions.Center;
                headerLabel.color = SurvivalPioneerUiPalette.Gold;
            }

            GameObject bodyRow = new GameObject("BodyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bodyRow.transform.SetParent(card.transform, false);
            HorizontalLayoutGroup bodyLayout = bodyRow.GetComponent<HorizontalLayoutGroup>();
            bodyLayout.padding = new RectOffset(18, 18, 14, 0);
            bodyLayout.spacing = 16f;
            bodyLayout.childAlignment = TextAnchor.UpperLeft;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;
            bodyLayout.childForceExpandHeight = false;

            LayoutElement bodyLayoutElement = bodyRow.AddComponent<LayoutElement>();
            bodyLayoutElement.flexibleWidth = 1f;

            GameObject iconBlock = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconBlock.transform.SetParent(bodyRow.transform, false);
            LayoutElement iconLayout = iconBlock.AddComponent<LayoutElement>();
            iconLayout.minWidth = 72f;
            iconLayout.preferredWidth = 72f;
            iconLayout.minHeight = 72f;
            iconLayout.preferredHeight = 72f;
            Image iconImage = iconBlock.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(iconImage);
            iconImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.82f);
            iconImage.raycastTarget = false;
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(iconBlock, new Vector2(2f, -2f));

            GameObject textColumn = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            textColumn.transform.SetParent(bodyRow.transform, false);
            VerticalLayoutGroup textLayout = textColumn.GetComponent<VerticalLayoutGroup>();
            textLayout.spacing = 6f;
            textLayout.childAlignment = TextAnchor.UpperLeft;
            textLayout.childControlWidth = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            LayoutElement textColumnLayout = textColumn.AddComponent<LayoutElement>();
            textColumnLayout.flexibleWidth = 1f;

            titleLabel = CreateLabel(textColumn.transform, "Title", 26f, SurvivalPioneerUiPalette.BodyText, FontStyles.Bold, theme);
            titleLabel.alignment = TextAlignmentOptions.TopLeft;
            titleLabel.textWrappingMode = TextWrappingModes.Normal;

            descriptionRow = CreateLabelRow(textColumn.transform, out descriptionLabel, "Description", 16f,
                SurvivalPioneerUiPalette.MutedText, FontStyles.Normal, theme);
            descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
            descriptionLabel.textWrappingMode = TextWrappingModes.Normal;

            xpRow = CreateLabelRow(textColumn.transform, out xpLabel, "+0 XP", 20f,
                SurvivalPioneerUiPalette.Gold, FontStyles.Bold, theme);
            xpLabel.alignment = TextAlignmentOptions.TopLeft;

            ContentSizeFitter cardFitter = card.AddComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            float fontSize,
            Color color,
            FontStyles style,
            ShiftUiTheme theme)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(label, semiBold: style == FontStyles.Bold);
            else
                TmpUiHelper.ApplyDefaultFont(label);

            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateLabelRow(
            Transform parent,
            out TextMeshProUGUI label,
            string name,
            float fontSize,
            Color color,
            FontStyles style,
            ShiftUiTheme theme)
        {
            GameObject row = new GameObject(name + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            label = CreateLabel(row.transform, name, fontSize, color, style, theme);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            return row;
        }

        private IEnumerator PresentOne(PendingPopup pending)
        {
            titleLabel.text = pending.Title;
            bool hasDescription = !string.IsNullOrWhiteSpace(pending.Description);
            descriptionRow.SetActive(hasDescription);
            if (hasDescription)
                descriptionLabel.text = pending.Description;

            bool hasXp = pending.XpReward > 0;
            xpRow.SetActive(hasXp);
            if (hasXp)
                xpLabel.text = $"+{pending.XpReward} XP";

            GameAudioManager.Instance?.PlayAchievementUnlock();
            UiFrontLayer.ReparentToFront(transform, canvasRoot);

            yield return AnimatePopup();
        }

        private IEnumerator AnimatePopup()
        {
            const float slideInDuration = 0.38f;
            const float holdDuration = 3.8f;
            const float fadeOutDuration = 0.36f;
            const float slideDistance = 48f;

            Vector2 startPosition = restAnchoredPosition + new Vector2(0f, slideDistance);
            popupRect.anchoredPosition = startPosition;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < slideInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideInDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                popupRect.anchoredPosition = Vector2.Lerp(startPosition, restAnchoredPosition, eased);
                canvasGroup.alpha = eased;
                yield return null;
            }

            popupRect.anchoredPosition = restAnchoredPosition;
            canvasGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdDuration);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = 1f - t;
                popupRect.anchoredPosition = restAnchoredPosition + new Vector2(0f, t * 24f);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }
    }
}
