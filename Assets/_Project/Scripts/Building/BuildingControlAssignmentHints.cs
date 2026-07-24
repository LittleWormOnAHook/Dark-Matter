using System.Collections.Generic;
using System.Text;
using Project.Pioneers;

namespace Project.Building
{
    /// <summary>
    /// Recommends base-role companion assignments for Building Control companion slots.
    /// </summary>
    public static class BuildingControlAssignmentHints
    {
        public enum BuildingAssignmentRole
        {
            None = 0,
            Logistics = 1,
            Salvage = 2,
            Communications = 3
        }

        public static BuildingAssignmentRole ResolveRole(string buildingId)
        {
            if (PioneerClassTaskAffinity.MatchesLogisticsBuilding(buildingId))
                return BuildingAssignmentRole.Logistics;

            if (PioneerClassTaskAffinity.MatchesSalvageBuilding(buildingId))
                return BuildingAssignmentRole.Salvage;

            if (PioneerClassTaskAffinity.MatchesCommunicationsBuilding(buildingId))
                return BuildingAssignmentRole.Communications;

            return BuildingAssignmentRole.None;
        }

        public static SkilledPioneerClass? GetRecommendedClass(BuildingAssignmentRole role)
        {
            return role switch
            {
                BuildingAssignmentRole.Logistics => SkilledPioneerClass.LogisticsOfficer,
                BuildingAssignmentRole.Salvage => SkilledPioneerClass.SalvageEngineer,
                BuildingAssignmentRole.Communications => SkilledPioneerClass.CommunicationsOfficer,
                _ => null
            };
        }

        public static string GetRoleHint(BuildingAssignmentRole role)
        {
            return role switch
            {
                BuildingAssignmentRole.Logistics =>
                    "Recommended: Logistics Officer — quartermaster routes boost storage, logistics, and vendor throughput.",
                BuildingAssignmentRole.Salvage =>
                    "Recommended: Salvage Engineer — upkeep patch speeds salvage, repairs, and fabrication maintenance.",
                BuildingAssignmentRole.Communications =>
                    "Recommended: Communications Officer — uplink matrix boosts comms, probe uplink, and relay throughput.",
                _ => "Assign base companions to raise building output. Logistics, Salvage, and Communications specialists match their facility types."
            };
        }

        public static string BuildAssignmentHint(string buildingId, PioneerRosterManager roster)
        {
            BuildingAssignmentRole role = ResolveRole(buildingId);
            StringBuilder builder = new StringBuilder(GetRoleHint(role));

            if (roster == null || role == BuildingAssignmentRole.None)
                return builder.ToString();

            SkilledPioneerClass? recommended = GetRecommendedClass(role);
            if (!recommended.HasValue)
                return builder.ToString();

            List<string> matches = GetAvailableSpecialists(roster, recommended.Value);
            if (matches.Count == 0)
            {
                builder.Append("\nNo available ");
                builder.Append(SkilledPioneerClassUtility.ToDisplayName(recommended.Value));
                builder.Append(" companions at base right now.");
                return builder.ToString();
            }

            builder.Append("\nAvailable: ");
            builder.Append(string.Join(", ", matches));
            return builder.ToString();
        }

        public static bool IsIdealAssignment(SkilledPioneerRecord record, string buildingId)
        {
            if (record == null)
                return false;

            BuildingAssignmentRole role = ResolveRole(buildingId);
            SkilledPioneerClass? recommended = GetRecommendedClass(role);
            if (!recommended.HasValue)
                return true;

            return record.pioneerClass == recommended.Value;
        }

        public static bool IsSpecializedBuilding(string buildingId) =>
            ResolveRole(buildingId) != BuildingAssignmentRole.None;

        public static List<string> GetAvailableSpecialists(PioneerRosterManager roster, SkilledPioneerClass pioneerClass)
        {
            List<string> names = new List<string>();
            if (roster == null)
                return names;

            HashSet<string> trioIds = new HashSet<string>(roster.ExpeditionTrioIds);
            IReadOnlyList<SkilledPioneerRecord> skilled = roster.SkilledPioneers;
            for (int i = 0; i < skilled.Count; i++)
            {
                SkilledPioneerRecord record = skilled[i];
                if (record == null
                    || record.pioneerClass != pioneerClass
                    || trioIds.Contains(record.id)
                    || record.WorkState == PioneerWorkState.Injured
                    || string.IsNullOrWhiteSpace(record.displayName))
                {
                    continue;
                }

                names.Add(record.displayName);
            }

            return names;
        }
    }
}
