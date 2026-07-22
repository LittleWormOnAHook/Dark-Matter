using Project.Pioneers;

namespace Project.Building
{
    /// <summary>
    /// Extra Building Control output when base-role specialists are assigned at camp (not on expedition).
    /// </summary>
    public static class BaseRoleCompanionBonusService
    {
        public const string SupplyCacheAbilityId = "supply_cache";
        public const string QuartermasterRoutesAbilityId = "quartermaster_routes";
        public const string FieldSalvageAbilityId = "field_salvage";
        public const string UpkeepPatchAbilityId = "upkeep_patch";
        public const string SignalRelayAbilityId = "signal_relay";
        public const string UplinkMatrixAbilityId = "uplink_matrix";

        private const float PassiveAssignmentBonus = 0.05f;

        public static float GetPassiveAssignmentBonus(SkilledPioneerRecord record, string buildingId)
        {
            if (record == null || record.isInExpeditionTrio || record.WorkState == PioneerWorkState.Injured)
                return 0f;

            if (record.pioneerClass == SkilledPioneerClass.LogisticsOfficer
                && PioneerTraitUtility.RecordHasAbility(record, QuartermasterRoutesAbilityId)
                && PioneerClassTaskAffinity.MatchesLogisticsBuilding(buildingId))
            {
                return PassiveAssignmentBonus;
            }

            if (record.pioneerClass == SkilledPioneerClass.SalvageEngineer
                && PioneerTraitUtility.RecordHasAbility(record, UpkeepPatchAbilityId)
                && PioneerClassTaskAffinity.MatchesSalvageBuilding(buildingId))
            {
                return PassiveAssignmentBonus;
            }

            if (record.pioneerClass == SkilledPioneerClass.CommunicationsOfficer
                && PioneerTraitUtility.RecordHasAbility(record, UplinkMatrixAbilityId)
                && PioneerClassTaskAffinity.MatchesCommunicationsBuilding(buildingId))
            {
                return PassiveAssignmentBonus;
            }

            return 0f;
        }

        public static bool IsBaseRoleClass(SkilledPioneerClass pioneerClass) =>
            pioneerClass == SkilledPioneerClass.LogisticsOfficer
            || pioneerClass == SkilledPioneerClass.SalvageEngineer
            || pioneerClass == SkilledPioneerClass.CommunicationsOfficer;
    }
}
