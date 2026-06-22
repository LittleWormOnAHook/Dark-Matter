using System;

namespace Project.Quests
{
    [Serializable]
    public class QuestObjectiveDefinition
    {
        public QuestObjectiveType type = QuestObjectiveType.Custom;
        public string targetId;
        public int requiredCount = 1;
        public string description;
    }
}
