using Project.Quests;
using UnityEngine;

namespace Project.PPT
{
    public sealed class PptQuestKeywordListener : MonoBehaviour
    {
        private QuestManager questManager;

        private void Start()
        {
            questManager = QuestManager.Instance;
            if (questManager == null)
                return;

            questManager.OnQuestUpdated += HandleQuestUpdated;
            questManager.OnQuestCompleted += HandleQuestCompleted;
        }

        private void OnDestroy()
        {
            if (questManager == null)
                return;

            questManager.OnQuestUpdated -= HandleQuestUpdated;
            questManager.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void HandleQuestUpdated(QuestProgress progress)
        {
            if (progress == null)
                return;

            if (progress.status == QuestStatus.Active)
                LogQuestKeywords(progress.questId, onComplete: false);
        }

        private void HandleQuestCompleted(QuestProgress progress)
        {
            if (progress == null)
                return;

            LogQuestKeywords(progress.questId, onComplete: true);
        }

        private void LogQuestKeywords(string questId, bool onComplete)
        {
            PptRegistry registry = Resources.Load<PptRegistry>(PptRegistry.DefaultResourcePath);
            if (registry?.KeywordSources == null)
            {
                LogQuestDefinitionFallback(questId);
                return;
            }

            bool logged = false;
            for (int s = 0; s < registry.KeywordSources.Length; s++)
            {
                PptKeywordSource source = registry.KeywordSources[s];
                if (source?.QuestRules == null)
                    continue;

                for (int r = 0; r < source.QuestRules.Length; r++)
                {
                    PptKeywordSourceRule rule = source.QuestRules[r];
                    if (rule == null || !string.Equals(rule.QuestId, questId, System.StringComparison.Ordinal))
                        continue;

                    string[] ids = onComplete ? rule.KeywordIdsOnComplete : rule.KeywordIdsOnAccept;
                    PptKeywordLog.LogMany(ids, onComplete ? "Quest complete" : "Quest accepted");
                    logged = true;
                }
            }

            if (!logged)
                LogQuestDefinitionFallback(questId);
        }

        private static void LogQuestDefinitionFallback(string questId)
        {
            QuestManager quests = QuestManager.Instance;
            if (quests == null)
                return;

            QuestDefinition def = quests.GetDefinition(questId);
            if (def == null)
                return;

            string titleId = "quest_" + questId;
            PptKeywordLog.Log(titleId, "Quest: " + def.title);

            if (def.objectives == null)
                return;

            for (int i = 0; i < def.objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = def.objectives[i];
                if (objective == null || string.IsNullOrWhiteSpace(objective.description))
                    continue;

                string objectiveId = "objective_" + questId + "_" + i;
                PptKeywordLog.Log(objectiveId, objective.description);
            }
        }
    }
}
