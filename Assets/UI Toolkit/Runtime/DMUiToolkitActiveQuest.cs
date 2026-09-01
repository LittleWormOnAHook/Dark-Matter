using System.Collections.Generic;
using System.Text;
using Project.Core;
using Project.Player;
using Project.Quests;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK top-right quest tracker. Refresh + SetGameplayVisible forward from ActiveQuestHudUI.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-371)]
    [DisallowMultipleComponent]
    public class DMUiToolkitActiveQuest : MonoBehaviour
    {
        private static DMUiToolkitActiveQuest instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement list;
        private QuestManager questManager;
        private bool bound;
        private bool gameplayVisible = true;
        private PlayerController cachedPlayer;

        public static DMUiToolkitActiveQuest Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitActiveQuest EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.ActiveQuestName,
                DMUiToolkitOverlayDocument.ActiveQuestUxml,
                DMUiToolkitOverlayDocument.ActiveQuestUss,
                DMUiToolkitOverlayDocument.ActiveQuestSort);
            if (doc == null)
                return null;

            DMUiToolkitActiveQuest host = doc.GetComponent<DMUiToolkitActiveQuest>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitActiveQuest>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static void SetGameplayVisible(bool visible)
        {
            DMUiToolkitActiveQuest host = EnsureHost();
            if (host == null)
                return;

            host.gameplayVisible = visible;
            if (visible)
                host.Rebuild();
            else
                host.ApplyShown(false);
        }

        public static void Refresh()
        {
            EnsureHost()?.Rebuild();
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            ApplyShown(ShouldShow());
            HideUgui();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("quest-root") ?? tree;
            list = tree.Q<VisualElement>("quest-list") ?? root;
            bound = root != null;
            Subscribe();
        }

        private void Subscribe()
        {
            if (questManager == null)
                questManager = QuestManager.Instance ?? Object.FindAnyObjectByType<QuestManager>();

            if (questManager == null)
                return;

            questManager.OnQuestUpdated -= HandleQuestUpdated;
            questManager.OnQuestCompleted -= HandleQuestUpdated;
            questManager.OnQuestUpdated += HandleQuestUpdated;
            questManager.OnQuestCompleted += HandleQuestUpdated;
        }

        private void Unsubscribe()
        {
            if (questManager == null)
                return;

            questManager.OnQuestUpdated -= HandleQuestUpdated;
            questManager.OnQuestCompleted -= HandleQuestUpdated;
        }

        private void HandleQuestUpdated(QuestProgress progress)
        {
            Rebuild();
        }

        private bool ShouldShow()
        {
            if (!gameplayVisible)
                return false;
            if (!DMUiToolkitOverlayDocument.GameplayHudWanted())
                return false;
            if (!GameSession.HasStarted)
                return false;
            if (EnvironmentalCrisisHudMode.IsCrisisActive)
                return false;

            if (cachedPlayer == null)
                cachedPlayer = PlayerLocator.FindPlayerController();
            if (cachedPlayer != null && cachedPlayer.BlocksCombatInput)
                return false;

            return true;
        }

        private void ApplyShown(bool shown)
        {
            DMUiToolkitOverlayDocument.SetShown(root, shown);
        }

        private void Rebuild()
        {
            BindTree();
            if (list == null)
                return;

            list.Clear();
            Subscribe();
            if (questManager == null)
                return;

            IReadOnlyList<QuestProgress> allProgress = questManager.GetAllProgress();
            for (int i = 0; i < allProgress.Count; i++)
            {
                QuestProgress progress = allProgress[i];
                if (progress == null)
                    continue;
                if (progress.status != QuestStatus.Active && progress.status != QuestStatus.Completed)
                    continue;

                QuestDefinition definition = questManager.GetDefinition(progress.questId);
                if (definition == null)
                    continue;

                VisualElement block = new VisualElement();
                block.AddToClassList("dmg-quest-block");
                block.pickingMode = PickingMode.Ignore;

                Label title = new Label(FormatThreeWordsPerLine(definition.title));
                title.AddToClassList("dmg-quest-title");
                if (progress.status == QuestStatus.Completed)
                    title.AddToClassList("dmg-quest-title-done");
                title.pickingMode = PickingMode.Ignore;
                block.Add(title);

                if (definition.objectives != null)
                {
                    for (int o = 0; o < definition.objectives.Count; o++)
                    {
                        QuestObjectiveDefinition objective = definition.objectives[o];
                        if (objective == null)
                            continue;

                        int required = Mathf.Max(1, objective.requiredCount);
                        int current = progress.GetObjectiveProgress(o);
                        string label = string.IsNullOrEmpty(objective.description)
                            ? objective.type.ToString()
                            : objective.description;
                        Label obj = new Label(FormatObjectiveLine(label, current, required));
                        obj.AddToClassList("dmg-quest-obj");
                        obj.pickingMode = PickingMode.Ignore;
                        block.Add(obj);
                    }
                }

                Label status = new Label(progress.status == QuestStatus.Completed
                    ? FormatOneOrTwoLines("Return to quest giver")
                    : FormatOneOrTwoLines("In Progress"));
                status.AddToClassList("dmg-quest-status");
                if (progress.status == QuestStatus.Completed)
                    status.AddToClassList("dmg-quest-turnin");
                status.pickingMode = PickingMode.Ignore;
                block.Add(status);

                list.Add(block);
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
            StringBuilder builder = new StringBuilder(text.Length + 4);
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

            StringBuilder builder = new StringBuilder(text.Length + 8);
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

        private static bool uguiHidden;

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving || uguiHidden)
                return;

            ActiveQuestHudUI hud = Object.FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include);
            if (hud == null)
                return;

            CanvasGroup group = hud.GetComponent<CanvasGroup>();
            if (group != null)
                DMUiToolkitOverlayDocument.HideCanvasGroup(group);
            else
                DMUiToolkitOverlayDocument.HideGameObject(hud.gameObject);
            uguiHidden = true;
        }
    }
}
