using System.Collections.Generic;
using Project.Data;

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

            if (!LevelUnlockUtility.CanAccess(progression, skill.requiredPlayerLevel))
            {
                error = $"Requires level {skill.requiredPlayerLevel}.";
                return false;
            }

            if (progression.GetSkillRank(skill.ResolvedId) >= skill.maxRank)
            {
                error = "Max rank reached.";
                return false;
            }

            int nextCost = skill.GetCostForNextRank(progression.GetSkillRank(skill.ResolvedId));
            if (progression.UnspentSkillPoints < nextCost)
            {
                error = "Not enough skill points.";
                return false;
            }

            if (skill.prerequisiteSkillIds != null)
            {
                for (int i = 0; i < skill.prerequisiteSkillIds.Length; i++)
                {
                    string prereqId = skill.prerequisiteSkillIds[i];
                    if (string.IsNullOrEmpty(prereqId))
                        continue;

                    SkillDefinition prereq = SkillRegistry.Resolve(prereqId);
                    if (prereq == null)
                        continue;

                    if (progression.GetSkillRank(prereqId) <= 0)
                    {
                        error = $"Requires {prereq.displayName}.";
                        return false;
                    }
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
            return progression.TrySpendSkillPoint(skill.ResolvedId, cost, skill.maxRank, out error);
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

                total += skill.bonusPercentPerRank * rank;
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

        /// <summary>Level-based weapon damage multiplier (+3%/level), applied on top of the flat skill bonus.</summary>
        public static float GetLevelWeaponDamageMultiplier()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            return progression != null ? progression.GetLevelWeaponDamageMultiplier() : 1f;
        }
    }
}
