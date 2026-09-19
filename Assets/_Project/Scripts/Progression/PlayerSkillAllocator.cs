using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Progression
{
    public static class PlayerSkillAllocator
    {
        public static bool CanAllocate(SkillDefinition skill, PlayerProgressionManager progression, out string error)
        {
            error = null;
            if (skill == null || progression == null)
            {
                error = "Missing skill or progression.";
                return false;
            }

            int currentRank = progression.GetSkillRank(skill.ResolvedId);
            if (currentRank >= skill.ClampedMaxRank)
            {
                error = "Max rank reached.";
                return false;
            }

            int requiredLevel = skill.GetRequiredPlayerLevelForNextRank(currentRank);
            if (!LevelUnlockUtility.CanAccess(progression, requiredLevel))
            {
                error = LevelUnlockUtility.FormatLevelRequiredMessage(requiredLevel);
                return false;
            }

            int nextCost = skill.GetCostForNextRank(currentRank);
            if (progression.UnspentSkillPoints < nextCost)
            {
                error = "Not enough skill points.";
                return false;
            }

            if (!ArePrerequisitesFullyMet(skill, progression, out error))
                return false;

            return true;
        }

        /// <summary>
        /// Prior-in-line skills are <see cref="SkillDefinition.prerequisiteSkillIds"/>.
        /// Each must be at <see cref="SkillDefinition.ClampedMaxRank"/> before this skill can take points.
        /// Empty prerequisites = first skill in its chain (always eligible for this check).
        /// </summary>
        public static bool ArePrerequisitesFullyMet(SkillDefinition skill, PlayerProgressionManager progression, out string error)
        {
            error = null;
            if (skill == null)
            {
                error = "Missing skill.";
                return false;
            }

            if (skill.prerequisiteSkillIds == null || skill.prerequisiteSkillIds.Length == 0)
                return true;

            if (progression == null)
            {
                error = "Missing progression.";
                return false;
            }

            for (int i = 0; i < skill.prerequisiteSkillIds.Length; i++)
            {
                string prereqId = skill.prerequisiteSkillIds[i];
                if (string.IsNullOrEmpty(prereqId))
                    continue;

                SkillDefinition prereq = SkillRegistry.Resolve(prereqId);
                if (prereq == null)
                    continue;

                int required = prereq.ClampedMaxRank;
                int have = progression.GetSkillRank(prereqId);
                if (have < required)
                {
                    string name = string.IsNullOrEmpty(prereq.displayName) ? prereqId : prereq.displayName;
                    error = $"Requires {name} at max rank ({have}/{required}).";
                    return false;
                }
            }

            return true;
        }

        public static bool TryAllocate(SkillDefinition skill, out string error)
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (!CanAllocate(skill, progression, out error))
                return false;

            int currentRank = progression.GetSkillRank(skill.ResolvedId);
            int cost = skill.GetCostForNextRank(currentRank);
            return progression.TrySpendSkillPoint(skill.ResolvedId, cost, skill.ClampedMaxRank, out error);
        }

        public static int GetMiningRank()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            return progression != null
                ? progression.GetSkillRank(SkillDefinition.MiningSkillId)
                : 0;
        }

        public static int GetHarvestingRank()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            return progression != null
                ? progression.GetSkillRank(SkillDefinition.HarvestingSkillId)
                : 0;
        }

        public static int GetGatherSkillRank(MineHarvestGatherKind gatherKind) =>
            gatherKind == MineHarvestGatherKind.Harvest ? GetHarvestingRank() : GetMiningRank();



        /// <summary>Sum of allocated ranks across all skills with the given modifier type.</summary>
        public static int GetTotalRank(SkillModifierType modifierType)
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (progression == null)
                return 0;

            int total = 0;
            foreach (SkillDefinition skill in SkillRegistry.GetAllSkills())
            {
                if (skill == null || skill.modifierType != modifierType)
                    continue;

                int rank = progression.GetSkillRank(skill.ResolvedId);
                if (rank > 0)
                    total += rank;
            }

            return total;
        }

        public static float GetTotalBonusPercent(SkillModifierType modifierType)
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (progression == null)
                return 0f;

            float total = 0f;
            foreach (SkillDefinition skill in SkillRegistry.GetAllSkills())
            {
                if (skill == null || skill.modifierType != modifierType)
                    continue;

                int rank = progression.GetSkillRank(skill.ResolvedId);
                if (rank <= 0)
                    continue;

                total += skill.GetBonusAtRank(rank);
            }

            return total;
        }

        public static float GetMeleeDamageFlatBonus() =>
            GetTotalBonusPercent(SkillModifierType.MeleeDamageFlat);

        /// <summary>Flat ranged damage from Marksman Training (all ranged weapons).</summary>
        public static float GetRangedDamageFlatBonus() =>
            GetTotalBonusPercent(SkillModifierType.RangedDamageFlat);

        /// <summary>Extra scanner fog-reveal / sweep range in meters (ScanRangeFlat).</summary>
        public static float GetScanRangeBonusMeters() =>
            GetTotalBonusPercent(SkillModifierType.ScanRangeFlat);

        /// <summary>+% weapon accuracy from skill ranks (WeaponAccuracyPercent).</summary>
        public static float GetWeaponAccuracyBonusPercent() =>
            GetTotalBonusPercent(SkillModifierType.WeaponAccuracyPercent);

        public static int GetSkillRank(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;

            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            return progression != null ? progression.GetSkillRank(skillId) : 0;
        }

        /// <summary>Splash / explosive trigger-bubble radius multiplier from ordinance skills.</summary>
        public static float GetSplashRadiusMultiplier()
        {
            float radius = 1f + GetTotalBonusPercent(SkillModifierType.SplashRadiusPercent) * 0.01f;
            radius *= 1f + GetTotalBonusPercent(SkillModifierType.SplashOverpressurePercent) * 0.01f;
            return Mathf.Max(0.05f, radius);
        }

        /// <summary>Splash / explosive payload multiplier from ordinance skills.</summary>
        public static float GetSplashDamageMultiplier()
        {
            float damage = 1f + GetTotalBonusPercent(SkillModifierType.SplashDamagePercent) * 0.01f;
            damage *= 1f + GetTotalBonusPercent(SkillModifierType.SplashOverpressurePercent) * 0.01f;
            return Mathf.Max(0.05f, damage);
        }

        /// <summary>Splash DOT duration multiplier from Hot Residue / Persistent Hazard.</summary>
        public static float GetSplashDotDurationMultiplier() =>
            Mathf.Max(0.05f, 1f + GetTotalBonusPercent(SkillModifierType.SplashDotDurationPercent) * 0.01f);

        public static bool HasHotResidue() =>
            GetSkillRank(SkillDefinition.HotResidueSkillId) > 0;

        public static bool HasClusterMunitions() =>
            GetSkillRank(SkillDefinition.ClusterMunitionsSkillId) > 0;

        public static bool HasPersistentHazard() =>
            GetSkillRank(SkillDefinition.PersistentHazardSkillId) > 0;

        /// <summary>Second-pulse damage scale. Base 48% of the first blast, plus Cluster ranks.</summary>
        public static float GetClusterPulseDamageScale() =>
            0.48f * (1f + GetTotalBonusPercent(SkillModifierType.SplashClusterPulsePercent) * 0.01f);

        /// <summary>Lingering hazard seconds. Base 2s, scaled by Persistent Hazard ranks.</summary>
        public static float GetPersistentHazardDuration() =>
            2f * (1f + GetTotalBonusPercent(SkillModifierType.SplashCloudDurationPercent) * 0.01f);

        /// <summary>Level-based weapon damage multiplier (+3%/level), applied on top of the flat skill bonus.</summary>
        public static float GetLevelWeaponDamageMultiplier()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            return progression != null ? progression.GetLevelWeaponDamageMultiplier() : 1f;
        }
    }
}
