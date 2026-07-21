using UnityEngine;

namespace Project.Progression
{
    [CreateAssetMenu(menuName = "Project/Progression/Suit Upgrade Definition", fileName = "NewSuitUpgrade")]
    public class SuitUpgradeDefinition : ScriptableObject, ILevelGatedUpgrade
    {
        public string upgradeId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public int requiredPlayerLevel = 1;
        public int upgradeTier = 1;

        public int RequiredPlayerLevel => requiredPlayerLevel;
    }
}
