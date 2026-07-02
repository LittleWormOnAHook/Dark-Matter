using System.Collections.Generic;

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

            if (progression.UnspentSkillPoints < skill.costPerRank)
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

            return progression.TrySpendSkillPoint(skill.ResolvedId, skill.costPerRank, skill.maxRank, out error);
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

                total += skill.bonusPercentPerRank * rank;
            }

            return total;
        }

        public static float GetMeleeDamageFlatBonus() =>
            GetTotalBonusPercent(SkillModifierType.MeleeDamageFlat);
    }
}
