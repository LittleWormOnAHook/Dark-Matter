namespace Project.Pioneers
{
    public enum SkilledPioneerClass
    {
        ArchitectEngineer = 0,
        ScienceSpecialist = 1,
        CombatTactician = 2,
        InfiltratorScout = 3
    }

    public static class SkilledPioneerClassUtility
    {
        public static string ToDisplayName(SkilledPioneerClass pioneerClass)
        {
            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => "Architect Engineer",
                SkilledPioneerClass.ScienceSpecialist => "Science Specialist",
                SkilledPioneerClass.CombatTactician => "Combat Tactician",
                SkilledPioneerClass.InfiltratorScout => "Infiltrator Scout",
                _ => pioneerClass.ToString()
            };
        }
    }
}
