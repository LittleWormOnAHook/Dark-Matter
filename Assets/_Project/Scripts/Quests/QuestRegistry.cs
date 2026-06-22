using System.Collections.Generic;
using UnityEngine;

namespace Project.Quests
{
    [CreateAssetMenu(menuName = "Project/Quests/Quest Registry", fileName = "QuestRegistry")]
    public class QuestRegistry : ScriptableObject
    {
        private static QuestRegistry cached;
        private static readonly List<QuestDefinition> RuntimeQuests = new List<QuestDefinition>();

        [SerializeField] private QuestDefinition[] quests;

        public static QuestRegistry Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<QuestRegistry>("Quests/QuestRegistry");

                return cached;
            }
        }

        public static IReadOnlyList<QuestDefinition> GetAllQuests()
        {
            List<QuestDefinition> result = new List<QuestDefinition>();

            QuestRegistry registry = Instance;
            if (registry != null && registry.quests != null)
            {
                foreach (QuestDefinition quest in registry.quests)
                {
                    if (quest != null)
                        result.Add(quest);
                }
            }

            foreach (QuestDefinition runtimeQuest in RuntimeQuests)
            {
                if (runtimeQuest != null && !ContainsQuest(result, runtimeQuest))
                    result.Add(runtimeQuest);
            }

            return result;
        }

        public static QuestDefinition Resolve(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return null;

            foreach (QuestDefinition quest in GetAllQuests())
            {
                if (quest != null && quest.ResolvedId == questId)
                    return quest;
            }

            return null;
        }

        public static void RegisterRuntimeQuest(QuestDefinition quest)
        {
            if (quest == null || ContainsQuest(RuntimeQuests, quest))
                return;

            RuntimeQuests.Add(quest);
        }

        private static bool ContainsQuest(List<QuestDefinition> list, QuestDefinition quest)
        {
            string id = quest.ResolvedId;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].ResolvedId == id)
                    return true;
            }

            return false;
        }
    }
}
