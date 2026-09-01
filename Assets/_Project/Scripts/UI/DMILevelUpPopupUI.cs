using System.Collections;
using Project.Audio;
using Project.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Center-screen, non-modal level-up toast. Distinct from <see cref="XpToastUI"/> XP grants.
    /// </summary>
    public class DMILevelUpPopupUI : MonoBehaviour
    {
        private static DMILevelUpPopupUI instance;

        private RectTransform popupRect;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI headlineLabel;
        private TextMeshProUGUI levelLabel;
        private TextMeshProUGUI subtitleLabel;
        private Transform canvasRoot;
        private Vector2 restAnchoredPosition;
        private Coroutine activeRoutine;

        public static DMILevelUpPopupUI EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("DMILevelUpPopupUI", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            instance = host.AddComponent<DMILevelUpPopupUI>();
            instance.Build(canvasRootTransform);
            return instance;
        }

        /// <param name="newLevel">Player level after leveling.</param>
        /// <param name="levelsGained">How many levels were gained in this XP grant (usually 1).</param>
        public static void Show(int newLevel, int levelsGained = 1)
        {
            if (newLevel < 1)
                return;

            if (DMUiToolkitLevelUp.TryShowLevelUp(newLevel, levelsGained))
                return;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            DMILevelUpPopupUI popup = EnsureExists(canvas.transform);
            popup.Present(newLevel, Mathf.Max(1, levelsGained));
        }

        private void Build(Transform canvasRootTransform)
        {
            canvasRoot = canvasRootTransform;
            ShiftUiTheme theme = ShiftUiTheme.Current;

            popupRect = transform as RectTransform;
            ApplyAnchor();
            popupRect.sizeDelta = new Vector2(440f, 0f);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(440f, 0f);

            Image cardBg = card.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(cardBg);
            DarkMatterGenesisUiPalette.ApplyPanelShellBackground(cardBg, 0.94f);
            cardBg.raycastTarget = false;
            DarkMatterGenesisUiPalette.ApplyGoldTrim(card);

            VerticalLayoutGroup cardLayout = card.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(0, 0, 0, 16);
            cardLayout.spacing = 0f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            GameObject headerBar = MenuUiBuilder.CreatePanelTitleBar(card.transform, "LEVEL UP", 36f, 14f);
            TextMeshProUGUI headerLabel = headerBar.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (headerLabel != null)
            {
                headerLabel.alignment = TextAlignmentOptions.Center;
                headerLabel.color = DarkMatterGenesisUiPalette.Gold;
            }

            Outline headerOutline = headerBar.GetComponent<Outline>();
            if (headerOutline != null)
                headerOutline.effectColor = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.78f);

            GameObject body = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup));
            body.transform.SetParent(card.transform, false);
            VerticalLayoutGroup bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(20, 20, 14, 2);
            bodyLayout.spacing = 4f;
            bodyLayout.childAlignment = TextAnchor.UpperCenter;
            bodyLayout.childControlWidth = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            headlineLabel = CreateLabel(body.transform, "Headline", 28f, DarkMatterGenesisUiPalette.BodyText, FontStyles.Bold, theme);
            headlineLabel.alignment = TextAlignmentOptions.Center;
            headlineLabel.text = "You Leveled Up!";

            levelLabel = CreateLabel(body.transform, "Level", 22f, DarkMatterGenesisUiPalette.Gold, FontStyles.Bold, theme);
            levelLabel.alignment = TextAlignmentOptions.Center;

            subtitleLabel = CreateLabel(body.transform, "Subtitle", 16f, DarkMatterGenesisUiPalette.MutedText, FontStyles.Normal, theme);
            subtitleLabel.alignment = TextAlignmentOptions.Center;

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

        private void Present(int newLevel, int levelsGained)
        {
            ApplyAnchor();
            headlineLabel.text = "You Leveled Up!";
            levelLabel.text = levelsGained > 1
                ? $"Level {newLevel}  (+{levelsGained})"
                : $"Level {newLevel}";

            int skillPoints = SumSkillPointsGained(newLevel, levelsGained);
            if (skillPoints > 0)
            {
                subtitleLabel.gameObject.SetActive(true);
                subtitleLabel.text = $"+{skillPoints} skill point{(skillPoints == 1 ? string.Empty : "s")}";
            }
            else
            {
                subtitleLabel.gameObject.SetActive(false);
            }

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            GameAudioManager.Instance?.PlayLevelUp();
            UiFrontLayer.ReparentToFront(transform, canvasRoot);
            activeRoutine = StartCoroutine(AnimatePopup());
        }

        private static int SumSkillPointsGained(int newLevel, int levelsGained)
        {
            int total = 0;
            int firstLevel = newLevel - levelsGained + 1;
            for (int level = firstLevel; level <= newLevel; level++)
                total += PlayerProgressionManager.GetSkillPointsForLevel(level);

            return total;
        }

        private void ApplyAnchor()
        {
            restAnchoredPosition = GameplayHudLayout.LevelUpPopupAnchoredPosition;
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.anchoredPosition = restAnchoredPosition;
        }

        private IEnumerator AnimatePopup()
        {
            const float slideInDuration = 0.34f;
            const float holdDuration = 2.5f;
            const float fadeOutDuration = 0.35f;
            const float slideDistance = 36f;

            Vector2 startPosition = restAnchoredPosition + new Vector2(0f, -slideDistance);
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
                popupRect.anchoredPosition = restAnchoredPosition + new Vector2(0f, t * 22f);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            activeRoutine = null;
        }
    }
}
