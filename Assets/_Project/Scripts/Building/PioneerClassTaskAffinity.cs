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

            if (MatchesCommunicationsBuilding(buildingId))
            {
                if (pioneerClass == SkilledPioneerClass.CommunicationsOfficer)
                    return 0.12f;
            }

            string id = buildingId ?? string.Empty;
            bool isScience = id.Contains("science") || id.Contains("lab");
            bool isMedical = id.Contains("medical") || id.Contains("med") || id.Contains("clinic");
            bool isCommand = id.Contains("command");
            bool isProduction = id.Contains("production") || id.Contains("fabrication");

            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer when isCommand || isProduction => 0.08f,
                SkilledPioneerClass.ScienceSpecialist when isScience => 0.1f,
                SkilledPioneerClass.MedTech when isMedical || isScience => 0.1f,
                SkilledPioneerClass.CombatTactician when isCommand => 0.05f,
                SkilledPioneerClass.SalvageEngineer when isProduction => 0.06f,
                SkilledPioneerClass.LogisticsOfficer when isCommand => 0.06f,
                SkilledPioneerClass.CommunicationsOfficer when isCommand => 0.06f,
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

        public static bool MatchesCommunicationsBuilding(string buildingId)
        {
            string id = buildingId ?? string.Empty;
            return id.Contains("communication")
                || id.Contains("comms")
                || id.Contains("uplink")
                || id.Contains("probe")
                || id.Contains("relay")
                || id.Contains("beacon")
                || id.Contains("resonance");
        }
    }
}
