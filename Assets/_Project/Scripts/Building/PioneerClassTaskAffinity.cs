using Project.Pioneers;

namespace Project.Building
{
    public static class PioneerClassTaskAffinity
    {
        public static float GetFacilityBonus(SkilledPioneerClass pioneerClass, string buildingId)
        {
            string id = buildingId ?? string.Empty;
            bool isScience = id.Contains("science") || id.Contains("lab");
            bool isCommand = id.Contains("command");
            bool isProduction = id.Contains("production") || id.Contains("fabrication");

            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer when isCommand || isProduction => 0.08f,
                SkilledPioneerClass.ScienceSpecialist when isScience => 0.1f,
                SkilledPioneerClass.CombatTactician when isCommand => 0.05f,
                SkilledPioneerClass.IoHybrid => 0.06f,
                _ => 0.02f
            };
        }
    }
}
