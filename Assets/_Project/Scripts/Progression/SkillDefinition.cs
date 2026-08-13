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

    /// <summary>Journal hex skill-tree branches.</summary>
    public enum SkillTreeCategory
    {
        Melee = 0,
        Pistols = 1,
        Rifles = 2,
        Survival = 3,
        Player = 4
    }

    [CreateAssetMenu(menuName = "Project/Progression/Skill Definition", fileName = "NewSkill")]
    public class SkillDefinition : ScriptableObject, ILevelGatedUpgrade
    {
        public const string MiningSkillId = "skill_mining";
        public const string HarvestingSkillId = "skill_harvesting";
        public const int DisplayMaxRank = 5;

        public string skillId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public SkillTreeCategory treeCategory = SkillTreeCategory.Player;
        [Tooltip("Hex column in the tree (0–2).")]
        public int treeColumn;
        [Tooltip("Hex row in the tree (0 = top).")]
        public int treeRow;
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
        public int ClampedMaxRank => Mathf.Clamp(maxRank, 1, DisplayMaxRank);

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

        public static string GetCategoryDisplayName(SkillTreeCategory category) =>
            category switch
            {
                SkillTreeCategory.Melee => "Melee",
                SkillTreeCategory.Pistols => "Pistols",
                SkillTreeCategory.Rifles => "Rifles",
                SkillTreeCategory.Survival => "Survival",
                SkillTreeCategory.Player => "Player",
                _ => category.ToString()
            };
    }
}
