using System.Collections.Generic;
using System.Text;
using Project.Core;
using Project.Progression;
using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Right-middle tracker for quests until they are turned in at a quest giver.
    /// </summary>
    public class ActiveQuestHudUI : MonoBehaviour
    {
        [SerializeField] private bool applyRuntimeLayout = true;

        private static ActiveQuestHudUI instance;

        private RectTransform listRoot;
        private TextMeshProUGUI progressionHeader;
        private QuestManager questManager;
        private PlayerProgressionManager progression;
        private bool built;

        public static ActiveQuestHudUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("ActiveQuestHud", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<ActiveQuestHudUI>();
            instance.Build();
            return instance;
        }

        private void Build()
        {
            if (built)
                return;

            built = true;
            RectTransform rect = transform as RectTransform;
            if (applyRuntimeLayout)
            {
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.anchoredPosition = new Vector2(-HudLayoutMetrics.RightHudInset, 0f);
                rect.sizeDelta = new Vector2(220f, 340f);
            }

            GameObject headerObject = new GameObject("ProgressionHeader", typeof(RectTransform));
            headerObject.transform.SetParent(transform, false);
            RectTransform headerRect = headerObject.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(1f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 28f);

            progressionHeader = headerObject.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(progressionHeader, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(progressionHeader);
            progressionHeader.fontSize = 16f;
            progressionHeader.fontStyle = FontStyles.Bold;
            progressionHeader.alignment = TextAlignmentOptions.TopRight;
            progressionHeader.color = SurvivalPioneerUiPalette.HighlightText;
            progressionHeader.raycastTarget = false;

            GameObject listObject = new GameObject("QuestList", typeof(RectTransform));
            listObject.transform.SetParent(transform, false);
            listRoot = listObject.GetComponent<RectTransform>();
            listRoot.anchorMin = new Vector2(0f, 0f);
            listRoot.anchorMax = new Vector2(1f, 1f);
            listRoot.offsetMin = Vector2.zero;
            listRoot.offsetMax = new Vector2(0f, -32f);

            VerticalLayoutGroup layout = listObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void Start()
        {
            questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            if (questManager != null)
            {
                questManager.OnQuestUpdated += HandleQuestUpdated;
                questManager.OnQuestCompleted += HandleQuestUpdated;
            }

            progression = PlayerProgressionManager.EnsureExists();
            if (progression != null)
                progression.OnXpChanged += RefreshProgressionHeader;

            RefreshProgressionHeader();
            Refresh();
        }

        private void OnDestroy()
        {
            if (questManager != null)
            {
                questManager.OnQuestUpdated -= HandleQuestUpdated;
                questManager.OnQuestCompleted -= HandleQuestUpdated;
            }

            if (progression != null)
                progression.OnXpChanged -= RefreshProgressionHeader;
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

        private void RefreshProgressionHeader()
        {
            if (progressionHeader == null)
                return;

            progression ??= PlayerProgressionManager.EnsureExists();
            if (progression == null)
            {
                progressionHeader.text = "Lv 1  |  XP 0/100";
                return;
            }

            progressionHeader.text =
                $"Lv {progression.Level}  |  XP {progression.GetXpProgressInCurrentLevel()}/{progression.GetXpRequiredForNextLevel()}";
        }

        public void Refresh()
        {
            RefreshProgressionHeader();

            if (listRoot == null)
                return;

            foreach (Transform child in listRoot)
                Destroy(child.gameObject);

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
                ? SurvivalPioneerUiPalette.Gold
                : SurvivalPioneerUiPalette.WarmOffWhite;
            TextMeshProUGUI title = CreateLine(block.transform, FormatThreeWordsPerLine(definition.title), 24f, FontStyles.Bold, theme);
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
                    bool complete = current >= required;
                    string label = string.IsNullOrEmpty(objective.description) ? objective.type.ToString() : objective.description;
                    string line = FormatObjectiveLine(label, current, required);
                    TextMeshProUGUI objectiveText = CreateLine(block.transform, line, 20f, FontStyles.Normal, theme);
                    objectiveText.alignment = TextAlignmentOptions.TopRight;
                    objectiveText.lineSpacing = -12f;
                    objectiveText.paragraphSpacing = -6f;
                    objectiveText.margin = Vector4.zero;
                    objectiveText.color = SurvivalPioneerUiPalette.BodyText;

                    LayoutElement objectiveLayout = objectiveText.gameObject.AddComponent<LayoutElement>();
                    objectiveLayout.minHeight = 0f;
                }
            }

            if (progress.status == QuestStatus.Completed)
            {
                TextMeshProUGUI turnIn = CreateLine(block.transform, FormatOneOrTwoLines("Return to quest giver"), 18f, FontStyles.Italic, theme);
                turnIn.alignment = TextAlignmentOptions.TopRight;
                turnIn.lineSpacing = -10f;
                turnIn.margin = new Vector4(0f, 1f, 0f, 0f);
                turnIn.color = SurvivalPioneerUiPalette.RichFuchsia;
            }
            else if (progress.status == QuestStatus.Active)
            {
                TextMeshProUGUI statusLine = CreateLine(block.transform, FormatOneOrTwoLines("In Progress"), 18f, FontStyles.Italic, theme);
                statusLine.alignment = TextAlignmentOptions.TopRight;
                statusLine.lineSpacing = -10f;
                statusLine.margin = new Vector4(0f, 1f, 0f, 0f);
                statusLine.color = SurvivalPioneerUiPalette.Gold;
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
