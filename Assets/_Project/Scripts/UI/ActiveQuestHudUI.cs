using System.Collections.Generic;
using System.Text;
using Project.Core;
using Project.Player;
using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Top-right quest tracker stacked below the minimap / compass / Range-Scan info strip.
    /// Anchored top-right (not middle) so resolution / canvas scale cannot push it into the compass.
    /// </summary>
    public class ActiveQuestHudUI : MonoBehaviour
    {
        [SerializeField] private bool applyRuntimeLayout = true;

        private static ActiveQuestHudUI instance;

        private RectTransform rootRect;
        private RectTransform listRoot;
        private QuestManager questManager;
        private CanvasGroup canvasGroup;
        private PlayerController cachedPlayer;
        private bool built;
        private bool gameplayVisible = true;
        private Vector2 lastCanvasSize;
        private int lastScreenWidth;
        private int lastScreenHeight;

        public static ActiveQuestHudUI EnsureExists(Transform canvasRoot)
        {
            if (instance == null)
                instance = Object.FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include);

            if (instance != null)
            {
                if (!instance.gameObject.activeSelf)
                    instance.gameObject.SetActive(true);
                if (canvasRoot != null && instance.transform.parent != canvasRoot)
                    instance.transform.SetParent(canvasRoot, false);
                instance.EnsureBuilt();
                instance.ApplyLockedLayout();
                instance.SetGameplayVisible(true);
                return instance;
            }

            GameObject host = new GameObject("ActiveQuestHud", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<ActiveQuestHudUI>();
            instance.EnsureBuilt();
            instance.SetGameplayVisible(true);
            return instance;
        }

        /// <summary>
        /// Session / menu / crisis gate. Transient blockers (map, journal, optics) use CanvasGroup
        /// so LateUpdate keeps running and can restore visibility afterward.
        /// </summary>
        public void SetGameplayVisible(bool visible)
        {
            gameplayVisible = visible;
            if (DMUiToolkitHud.IsDriving)
                DMUiToolkitActiveQuest.SetGameplayVisible(visible);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            ApplyPresentationVisible();
            if (visible)
            {
                ApplyLockedLayout();
                Refresh();
            }
        }

        private void EnsureBuilt()
        {
            if (built)
                return;

            built = true;
            rootRect = transform as RectTransform;
            EnsureCanvasGroup();
            ApplyLockedLayout();

            // Idempotent: reclaim existing children after domain reload / double EnsureExists.
            // Lv/XP used to live here under the compass — removed; level/XP live in Journal now.
            Transform legacyHeader = transform.Find("ProgressionHeader");
            if (legacyHeader != null)
                Destroy(legacyHeader.gameObject);

            Transform existingList = transform.Find("QuestList");
            if (existingList != null)
            {
                listRoot = existingList as RectTransform;
                if (listRoot != null)
                {
                    listRoot.offsetMin = Vector2.zero;
                    listRoot.offsetMax = Vector2.zero;
                }
            }
            else
            {
                GameObject listObject = new GameObject("QuestList", typeof(RectTransform));
                listObject.transform.SetParent(transform, false);
                listRoot = listObject.GetComponent<RectTransform>();
                listRoot.anchorMin = new Vector2(0f, 0f);
                listRoot.anchorMax = new Vector2(1f, 1f);
                listRoot.pivot = new Vector2(1f, 1f);
                listRoot.offsetMin = Vector2.zero;
                listRoot.offsetMax = Vector2.zero;

                VerticalLayoutGroup layout = listObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 2f;
                layout.childAlignment = TextAnchor.UpperRight;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = listObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Remove accidental duplicate chrome from older builds.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;
                if (child.name == "ProgressionHeader")
                {
                    Object.Destroy(child.gameObject);
                    continue;
                }

                if (child.name == "QuestList" && child != listRoot)
                    Object.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Top-right lock below the live compass / info stack (falls back to GameplayHudLayout constants).
        /// </summary>
        private void ApplyLockedLayout()
        {
            if (!applyRuntimeLayout)
                return;

            if (rootRect == null)
                rootRect = transform as RectTransform;
            if (rootRect == null)
                return;

            float width = GameplayHudLayout.QuestHudWidth;
            float maxHeight = GameplayHudLayout.QuestHudMaxHeight;

            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.sizeDelta = new Vector2(width, maxHeight);
            rootRect.localScale = Vector3.one;
            rootRect.anchoredPosition = ResolveAnchoredPositionBelowMinimapStack();

            CacheCanvasSize();
        }

        private Vector2 ResolveAnchoredPositionBelowMinimapStack()
        {
            float rightInset = GameplayHudLayout.MinimapEdgeInset;
            float topY = GameplayHudLayout.QuestHudAnchoredPosition.y;

            CompassHudUI compass = FindAnyObjectByType<CompassHudUI>();
            if (compass != null && compass.Root != null)
            {
                RectTransform compassRect = compass.Root;
                rightInset = -compassRect.anchoredPosition.x;
                topY = compassRect.anchoredPosition.y - compassRect.rect.height;
            }

            if (transform.parent != null)
            {
                // MapUI stacks InfoText on the MapUI/canvas root below the compass.
                MapUI mapUi = FindAnyObjectByType<MapUI>();
                if (mapUi != null)
                {
                    Transform mapInfo = mapUi.transform.Find("InfoText");
                    if (mapInfo is RectTransform infoRect && infoRect.gameObject.activeInHierarchy)
                    {
                        rightInset = -infoRect.anchoredPosition.x;
                        float infoBottom = infoRect.anchoredPosition.y - infoRect.rect.height;
                        topY = Mathf.Min(topY, infoBottom);
                    }
                }
            }

            return new Vector2(
                -rightInset,
                topY - GameplayHudLayout.QuestHudGapBelowInfo);
        }

        private void CacheCanvasSize()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            RectTransform canvasRect = rootRect != null ? rootRect.parent as RectTransform : null;
            lastCanvasSize = canvasRect != null ? canvasRect.rect.size : Vector2.zero;
        }

        private bool HasViewportChanged()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
                return true;

            RectTransform canvasRect = rootRect != null ? rootRect.parent as RectTransform : null;
            if (canvasRect == null)
                return false;

            Vector2 size = canvasRect.rect.size;
            return !Mathf.Approximately(size.x, lastCanvasSize.x)
                || !Mathf.Approximately(size.y, lastCanvasSize.y);
        }

        private void LateUpdate()
        {
            if (!built)
                return;

            // Keep the host active so CanvasGroup fade can restore after map / optics / journal.
            // Do not fight MainMenu HideGameplayUi (that SetActive(false) stops LateUpdate until restore).
            ApplyPresentationVisible();

            if (!IsPresentationVisible())
                return;

            if (applyRuntimeLayout && HasViewportChanged())
                ApplyLockedLayout();
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup != null)
                return;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void ApplyPresentationVisible()
        {
            EnsureCanvasGroup();
            bool visible = gameplayVisible
                && GameSession.HasStarted
                && !MainMenuController.BlocksGameplayHud
                && !ShouldHideForBlockingUi()
                && !EnvironmentalCrisisHudMode.IsCrisisActive;

            if (DMUiToolkitHud.IsDriving)
                visible = false;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private bool IsPresentationVisible()
        {
            return canvasGroup != null && canvasGroup.alpha > 0.01f;
        }

        private bool ShouldHideForBlockingUi()
        {
            if (cachedPlayer == null)
                cachedPlayer = PlayerLocator.FindPlayerController();

            return cachedPlayer != null && cachedPlayer.BlocksCombatInput;
        }

        private void OnEnable()
        {
            if (built)
            {
                ApplyLockedLayout();
                ApplyPresentationVisible();
            }
        }

        private void Start()
        {
            EnsureBuilt();
            questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            if (questManager != null)
            {
                questManager.OnQuestUpdated += HandleQuestUpdated;
                questManager.OnQuestCompleted += HandleQuestUpdated;
            }

            ApplyLockedLayout();
            Refresh();
            ApplyPresentationVisible();
        }

        private void OnDestroy()
        {
            if (questManager != null)
            {
                questManager.OnQuestUpdated -= HandleQuestUpdated;
                questManager.OnQuestCompleted -= HandleQuestUpdated;
            }

            if (instance == this)
                instance = null;
        }

        private void HandleQuestUpdated(QuestProgress progress)
        {
            Refresh();
        }

        private void EnsureQuestManagerSubscribed()
        {
            if (questManager == null)
                questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();

            if (questManager == null)
                return;

            questManager.OnQuestUpdated -= HandleQuestUpdated;
            questManager.OnQuestCompleted -= HandleQuestUpdated;
            questManager.OnQuestUpdated += HandleQuestUpdated;
            questManager.OnQuestCompleted += HandleQuestUpdated;
        }

        public void Refresh()
        {
            if (DMUiToolkitHud.IsDriving)
            {
                DMUiToolkitActiveQuest.Refresh();
                ApplyPresentationVisible();
                return;
            }

            ApplyLockedLayout();

            if (listRoot == null)
                return;

            // Detach before Destroy so VerticalLayoutGroup does not keep deferred-destroy
            // children for a frame (avoids duplicate flash on every objective tick).
            for (int i = listRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = listRoot.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            EnsureQuestManagerSubscribed();
            if (questManager == null)
                return;

            IReadOnlyList<QuestProgress> allProgress = questManager.GetAllProgress();
            for (int i = 0; i < allProgress.Count; i++)
            {
                QuestProgress progress = allProgress[i];
                if (progress == null || !ShouldTrack(progress.status))
                    continue;

                QuestDefinition definition = questManager.GetDefinition(progress.questId);
                if (definition == null)
                    continue;

                CreateTrackedQuestBlock(definition, progress);
            }
        }

        private static bool ShouldTrack(QuestStatus status)
        {
            return status == QuestStatus.Active || status == QuestStatus.Completed;
        }

        private void CreateTrackedQuestBlock(QuestDefinition definition, QuestProgress progress)
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;

            GameObject block = new GameObject($"Track_{definition.ResolvedId}", typeof(RectTransform));
            block.transform.SetParent(listRoot, false);

            VerticalLayoutGroup layout = block.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            Color titleColor = progress.status == QuestStatus.Completed
                ? DarkMatterGenesisUiPalette.Gold
                : DarkMatterGenesisUiPalette.WarmOffWhite;
            TextMeshProUGUI title = CreateLine(
                block.transform,
                FormatThreeWordsPerLine(definition.title),
                24f,
                FontStyles.Bold,
                theme);
            title.alignment = TextAlignmentOptions.TopRight;
            title.lineSpacing = -6f;
            title.margin = new Vector4(0f, 0f, 0f, 1f);
            title.color = titleColor;

            if (definition.objectives != null)
            {
                for (int i = 0; i < definition.objectives.Count; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (objective == null)
                        continue;

                    int required = Mathf.Max(1, objective.requiredCount);
                    int current = progress.GetObjectiveProgress(i);
                    string label = string.IsNullOrEmpty(objective.description) ? objective.type.ToString() : objective.description;
                    string line = FormatObjectiveLine(label, current, required);
                    TextMeshProUGUI objectiveText = CreateLine(
                        block.transform,
                        line,
                        20f,
                        FontStyles.Normal,
                        theme);
                    objectiveText.alignment = TextAlignmentOptions.TopRight;
                    objectiveText.lineSpacing = -12f;
                    objectiveText.paragraphSpacing = -6f;
                    objectiveText.margin = Vector4.zero;
                    objectiveText.color = DarkMatterGenesisUiPalette.BodyText;

                    LayoutElement objectiveLayout = objectiveText.gameObject.AddComponent<LayoutElement>();
                    objectiveLayout.minHeight = 0f;
                }
            }

            if (progress.status == QuestStatus.Completed)
            {
                TextMeshProUGUI turnIn = CreateLine(
                    block.transform,
                    FormatOneOrTwoLines("Return to quest giver"),
                    18f,
                    FontStyles.Italic,
                    theme);
                turnIn.alignment = TextAlignmentOptions.TopRight;
                turnIn.lineSpacing = -10f;
                turnIn.margin = new Vector4(0f, 1f, 0f, 0f);
                turnIn.color = DarkMatterGenesisUiPalette.RichFuchsia;
            }
            else if (progress.status == QuestStatus.Active)
            {
                TextMeshProUGUI statusLine = CreateLine(
                    block.transform,
                    FormatOneOrTwoLines("In Progress"),
                    18f,
                    FontStyles.Italic,
                    theme);
                statusLine.alignment = TextAlignmentOptions.TopRight;
                statusLine.lineSpacing = -10f;
                statusLine.margin = new Vector4(0f, 1f, 0f, 0f);
                statusLine.color = DarkMatterGenesisUiPalette.Gold;
            }
        }

        private static string FormatObjectiveLine(string label, int current, int required)
        {
            string body = FormatOneOrTwoLines(label);
            string count = $"{Mathf.Min(current, required)}/{required}";
            int newlineIndex = body.LastIndexOf('\n');
            if (newlineIndex >= 0)
            {
                string line1 = body.Substring(0, newlineIndex);
                string line2 = body.Substring(newlineIndex + 1);
                return $"{line1}\n{line2}  {count}";
            }

            return $"{body}  {count}";
        }

        private static string FormatOneOrTwoLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] words = text.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 5)
                return text.Trim();

            int firstLineCount = (words.Length + 1) / 2;
            var builder = new StringBuilder(text.Length + 4);
            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0)
                {
                    if (i == firstLineCount)
                        builder.Append('\n');
                    else
                        builder.Append(' ');
                }

                builder.Append(words[i]);
            }

            return builder.ToString();
        }

        private static string FormatThreeWordsPerLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] words = text.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 3)
                return text.Trim();

            var builder = new StringBuilder(text.Length + 8);
            for (int i = 0; i < words.Length; i++)
            {
                if (i > 0)
                {
                    if (i % 3 == 0)
                        builder.Append('\n');
                    else
                        builder.Append(' ');
                }

                builder.Append(words[i]);
            }

            return builder.ToString();
        }

        private static TextMeshProUGUI CreateLine(Transform parent, string text, float size, FontStyles style, ShiftUiTheme theme)
        {
            GameObject lineObject = new GameObject("Line", typeof(RectTransform));
            lineObject.transform.SetParent(parent, false);
            TextMeshProUGUI line = lineObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(line, semiBold: style == FontStyles.Bold);
            else
                TmpUiHelper.ApplyDefaultFont(line);
            line.text = text;
            line.fontSize = size;
            line.fontStyle = style;
            line.textWrappingMode = TextWrappingModes.Normal;
            line.raycastTarget = false;
            return line;
        }
    }
}
