namespace Project.Pioneers
{
    public static class PioneerLoadoutDefaults
    {
        public static void EnsureDefaults(SkilledPioneerRecord record)
        {
            if (record == null)
                return;

            if (string.IsNullOrWhiteSpace(record.weaponItemId))
                record.weaponItemId = GetDefaultWeaponId(record.pioneerClass);

            if (string.IsNullOrWhiteSpace(record.toolItemId))
                record.toolItemId = GetDefaultToolId(record.pioneerClass);

            if (record.assignedSkillIds == null || record.assignedSkillIds.Length == 0)
                record.assignedSkillIds = record.learnedSkills != null
                    ? (string[])record.learnedSkills.Clone()
                    : System.Array.Empty<string>();
        }

        public static string GetDefaultWeaponId(SkilledPioneerClass pioneerClass)
        {
            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => "weap2_sword",
                SkilledPioneerClass.ScienceSpecialist => "weap2_sword",
                SkilledPioneerClass.CombatTactician => "Sword of Fear",
                SkilledPioneerClass.InfiltratorScout => "Spear of Fate",
                SkilledPioneerClass.IoHybrid => "weap2_sword",
                _ => "weap2_sword"
            };
        }

        public static string GetDefaultToolId(SkilledPioneerClass pioneerClass)
        {
            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => "Wood Axe",
                SkilledPioneerClass.ScienceSpecialist => "Scanner B44",
                SkilledPioneerClass.CombatTactician => "Wood Axe",
                SkilledPioneerClass.InfiltratorScout => "Binnos 250",
                SkilledPioneerClass.IoHybrid => "Scanner B44",
                _ => string.Empty
            };
        }
    }
}
