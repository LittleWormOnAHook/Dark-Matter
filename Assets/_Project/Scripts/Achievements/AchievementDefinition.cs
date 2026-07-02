using UnityEngine;

namespace Project.Achievements
{
    [CreateAssetMenu(menuName = "Project/Achievements/Achievement Definition", fileName = "NewAchievement")]
    public class AchievementDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string achievementId;
        public string title;
        [TextArea(2, 4)]
        public string description;

        [Header("Classification")]
        public AchievementCategory category = AchievementCategory.General;
        public AchievementTriggerType triggerType = AchievementTriggerType.Manual;
        public int targetCount = 1;
        public string targetId;
        public int xpReward = 50;
        public bool hidden;
        public int sortOrder;

        public string ResolvedId => string.IsNullOrEmpty(achievementId) ? name : achievementId;
    }
}
