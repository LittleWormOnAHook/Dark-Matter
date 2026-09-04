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
        HarvestingTier,
        JetFuelPercent,
        JetThrustPercent,
        JetRegenPercent,
        DashSpeedPercent,
        DashDistancePercent,
        DashAirUnlock,
        MaxOxygenPercent,
        OxygenConsumptionReductionPercent,
        OxygenScrubberPercent
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

    /// <summary>
    /// Designer-editable skill profile (ScriptableObject).
    /// Edit assets under Resources/Progression/Skills/{Melee|Pistols|Rifles|Survival|Player}/ - max ranks, costs, combat/survival specs, and chain prerequisites.
    /// Runtime allocation and hex UI read these fields directly (no separate SkillProfile needed).
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Progression/Skill Definition", fileName = "NewSkill")]
    public class SkillDefinition : ScriptableObject, ILevelGatedUpgrade
    {
        public const string MiningSkillId = "skill_mining";
        public const string HarvestingSkillId = "skill_harvesting";
        public const int DisplayMaxRank = 20;

        [Header("Identity")]
        public string skillId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public SkillTreeCategory treeCategory = SkillTreeCategory.Player;

        [Header("Tree Placement")]
        [Tooltip("Hex column in the tree (0-2).")]
        public int treeColumn;
        [Tooltip("Hex row in the tree (0 = top).")]
        public int treeRow;

        [Header("Ranks & Cost (editable profile)")]
        [Tooltip("Player level required before this skill can receive points.")]
        public int requiredPlayerLevel = 1;
        [Tooltip("Default skill-point cost per rank when costPerTargetRank is empty. Post-requisite skills should be base+chainDepth (first=1, next=2, ...).")]
        public int costPerRank = 1;
        [Tooltip("Max upgrade ranks for this skill (clamped 1-DisplayMaxRank for UI dots). Prior-in-line skills must reach this before dependents can upgrade.")]
        public int maxRank = 20;
        [Tooltip("Optional cost to purchase each target rank (index 0 = rank 1). Empty / non-positive entries fall back to costPerRank.")]
        public int[] costPerTargetRank;

        [Header("Combat / Survival Spec (editable profile)")]
        [Tooltip("Which runtime stat this skill feeds.")]
        public SkillModifierType modifierType = SkillModifierType.MaxHealthPercent;
        [Tooltip("Bonus applied per owned rank for percent-based modifiers. Flat bonus per rank for MeleeDamageFlat / RangedDamageFlat / ScanRangeFlat.")]
        public float bonusPercentPerRank = 5f;
        [Tooltip("Optional per-rank bonus overrides (index 0 = rank 1). Empty / unused slots fall back to bonusPercentPerRank. Sum of overrides for owned ranks is used when set.")]
        public float[] bonusPercentPerTargetRank;

        [Header("Chain Prerequisites (prior in line)")]
        [Tooltip("Prior skill(s) in this chain. Each must be fully upgraded (at maxRank) before this skill can take points. Leave empty for the first skill in a chain.")]
        public string[] prerequisiteSkillIds;

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

        /// <summary>
        /// Total combat/survival bonus contributed at the given owned rank.
        /// Uses bonusPercentPerTargetRank when present; otherwise bonusPercentPerRank * rank.
        /// </summary>
        public float GetBonusAtRank(int rank)
        {
            if (rank <= 0)
                return 0f;

            int capped = Mathf.Min(rank, ClampedMaxRank);
            if (bonusPercentPerTargetRank == null || bonusPercentPerTargetRank.Length == 0)
                return bonusPercentPerRank * capped;

            float total = 0f;
            for (int i = 0; i < capped; i++)
            {
                if (i < bonusPercentPerTargetRank.Length && bonusPercentPerTargetRank[i] != 0f)
                    total += bonusPercentPerTargetRank[i];
                else
                    total += bonusPercentPerRank;
            }

            return total;
        }

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
