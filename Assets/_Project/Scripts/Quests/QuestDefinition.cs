using System.Collections.Generic;
using UnityEngine;

namespace Project.Quests
{
    [CreateAssetMenu(menuName = "Project/Quests/Quest Definition", fileName = "NewQuest")]
    public class QuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string questId;
        public string title;
        [TextArea(2, 5)]
        public string description;

        [Header("Objectives")]
        public List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();

        [Header("Rewards")]
        public List<QuestRewardDefinition> rewards = new List<QuestRewardDefinition>();

        [Tooltip("Bonus XP granted when the quest is turned in (in addition to Xp reward entries).")]
        public int xpReward;

        public string ResolvedId => string.IsNullOrEmpty(questId) ? name : questId;
    }
}
