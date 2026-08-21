using UnityEngine;

namespace Project.Achievements
{
    [CreateAssetMenu(menuName = "Project/Achievements/Dynamic Achievement Template", fileName = "DynamicAchievementTemplate")]
    public class DynamicAchievementTemplate : ScriptableObject
    {
        public string templateId;
        public string titleFormat = "Gather {count} {target}";
        public string descriptionFormat = "Collect {count} units of {target}.";
        public AchievementTriggerType triggerType = AchievementTriggerType.CollectItem;
        public Vector2Int countRange = new Vector2Int(25, 75);
        public string poolTag = "resource";
        public int xpReward = 40;
        public int sortOrder = 1000;

        public string ResolvedId => string.IsNullOrEmpty(templateId) ? name : templateId;
    }
}
