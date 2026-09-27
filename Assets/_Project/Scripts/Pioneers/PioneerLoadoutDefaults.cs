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
                SkilledPioneerClass.ArchitectEngineer => "2 Hander",
                SkilledPioneerClass.ScienceSpecialist => "2 Hander",
                SkilledPioneerClass.CombatTactician => "Sword of Fear",
                SkilledPioneerClass.InfiltratorScout => "Spear of Fate",
                SkilledPioneerClass.IoHybrid => "2 Hander",
                SkilledPioneerClass.MedTech => "2 Hander",
                SkilledPioneerClass.LogisticsOfficer => "2 Hander",
                SkilledPioneerClass.SalvageEngineer => "Wood Axe",
                SkilledPioneerClass.CommunicationsOfficer => "2 Hander",
                _ => "2 Hander"
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
                SkilledPioneerClass.MedTech => "Medpack",
                SkilledPioneerClass.LogisticsOfficer => "Scanner B44",
                SkilledPioneerClass.SalvageEngineer => "Wood Axe",
                SkilledPioneerClass.CommunicationsOfficer => "Scanner B44",
                _ => string.Empty
            };
        }
    }
}
