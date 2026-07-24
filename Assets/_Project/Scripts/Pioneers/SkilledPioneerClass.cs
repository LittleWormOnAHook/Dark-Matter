namespace Project.Pioneers
{
    public enum SkilledPioneerClass
    {
        ArchitectEngineer = 0,
        ScienceSpecialist = 1,
        CombatTactician = 2,
        InfiltratorScout = 3,
        IoHybrid = 4,
        MedTech = 5,
        LogisticsOfficer = 6,
        SalvageEngineer = 7,
        CommunicationsOfficer = 8
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
                SkilledPioneerClass.IoHybrid => "I/O Hybrid",
                SkilledPioneerClass.MedTech => "Med Tech",
                SkilledPioneerClass.LogisticsOfficer => "Logistics Officer",
                SkilledPioneerClass.SalvageEngineer => "Salvage Engineer",
                SkilledPioneerClass.CommunicationsOfficer => "Communications Officer",
                _ => pioneerClass.ToString()
            };
        }

        public static string ToHudLabel(SkilledPioneerClass pioneerClass)
        {
            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => "Engineer",
                SkilledPioneerClass.ScienceSpecialist => "Science",
                SkilledPioneerClass.CombatTactician => "Tactician",
                SkilledPioneerClass.InfiltratorScout => "Scout",
                SkilledPioneerClass.IoHybrid => "Hybrid",
                SkilledPioneerClass.MedTech => "Med Tech",
                SkilledPioneerClass.LogisticsOfficer => "Logistics",
                SkilledPioneerClass.SalvageEngineer => "Salvage",
                SkilledPioneerClass.CommunicationsOfficer => "Comms",
                _ => ToDisplayName(pioneerClass)
            };
        }
    }
}
