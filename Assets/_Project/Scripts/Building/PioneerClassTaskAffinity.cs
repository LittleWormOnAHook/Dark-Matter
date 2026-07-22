using Project.Pioneers;

namespace Project.Building
{
    public static class PioneerClassTaskAffinity
    {
        public static float GetFacilityBonus(SkilledPioneerClass pioneerClass, string buildingId)
        {
            if (MatchesLogisticsBuilding(buildingId))
            {
                if (pioneerClass == SkilledPioneerClass.LogisticsOfficer)
                    return 0.12f;
            }

            if (MatchesSalvageBuilding(buildingId))
            {
                if (pioneerClass == SkilledPioneerClass.SalvageEngineer)
                    return 0.12f;
            }

            bool isScience = buildingId.Contains("science") || buildingId.Contains("lab");
            bool isMedical = buildingId.Contains("medical") || buildingId.Contains("med") || buildingId.Contains("clinic");
            bool isCommand = buildingId.Contains("command");
            bool isProduction = buildingId.Contains("production") || buildingId.Contains("fabrication");

            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer when isCommand || isProduction => 0.08f,
                SkilledPioneerClass.ScienceSpecialist when isScience => 0.1f,
                SkilledPioneerClass.MedTech when isMedical || isScience => 0.1f,
                SkilledPioneerClass.CombatTactician when isCommand => 0.05f,
                SkilledPioneerClass.SalvageEngineer when isProduction => 0.06f,
                SkilledPioneerClass.LogisticsOfficer when isCommand => 0.06f,
                SkilledPioneerClass.IoHybrid => 0.06f,
                _ => 0.02f
            };
        }

        public static bool MatchesLogisticsBuilding(string buildingId)
        {
            string id = buildingId ?? string.Empty;
            return id.Contains("logistics")
                || id.Contains("storage")
                || id.Contains("vendor")
                || id.Contains("supply")
                || id.Contains("quartermaster");
        }

        public static bool MatchesSalvageBuilding(string buildingId)
        {
            string id = buildingId ?? string.Empty;
            return id.Contains("salvage")
                || id.Contains("reclaim")
                || id.Contains("maintenance")
                || id.Contains("repair")
                || id.Contains("upkeep")
                || id.Contains("fabrication");
        }
    }
}
