using System;
using UnityEngine;

namespace Project.Progression
{
    public enum SkillModifierType
    {
        MaxHealthPercent,
        MaxEnergyPercent,
        MaxStaminaPercent,
        MeleeDamageFlat,
        GatherSpeedPercent,
        CraftXpPercent,
        WeaponAccuracyPercent,
        RangedDamageFlat,
        ScanRangeFlat,
        MiningTier,
        HarvestingTier
    }

    [CreateAssetMenu(menuName = "Project/Progression/Skill Definition", fileName = "NewSkill")]
    public class SkillDefinition : ScriptableObject, ILevelGatedUpgrade
    {
        public const string MiningSkillId = "skill_mining";
        public const string HarvestingSkillId = "skill_harvesting";

        public string skillId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public int requiredPlayerLevel = 1;
        public int costPerRank = 1;
        public int maxRank = 3;
        public SkillModifierType modifierType = SkillModifierType.MaxHealthPercent;
        [Tooltip("Percent bonus per rank for percent-based modifiers. Flat bonus per rank for MeleeDamageFlat / RangedDamageFlat / ScanRangeFlat.")]
        public float bonusPercentPerRank = 5f;
        public string[] prerequisiteSkillIds;
        [Tooltip("Optional cost to purchase each target rank (index 0 = rank 1). Empty entries fall back to costPerRank.")]
        public int[] costPerTargetRank;

        public string ResolvedId => string.IsNullOrEmpty(skillId) ? name : skillId;
        public int RequiredPlayerLevel => requiredPlayerLevel;

        /// <summary>Skill-point cost to purchase the given target rank (1-based).</summary>
        public int GetCostForTargetRank(int targetRank)
        {
            if (targetRank < 1)
                return Mathf.Max(1, costPerRank);

            if (costPerTargetRank != null
                && targetRank - 1 < costPerTargetRank.Length
                && costPerTargetRank[targetRank - 1] > 0)
            {
                return costPerTargetRank[targetRank - 1];
            }

            return Mathf.Max(1, costPerRank);
        }

        public int GetCostForNextRank(int currentRank) =>
            GetCostForTargetRank(currentRank + 1);
    }
}
