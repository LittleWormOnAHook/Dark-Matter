using System.Collections.Generic;

namespace Project.Pioneers
{
    /// <summary>
    /// Summarizes passive companion buffs from synced and friendly echoes on the skilled roster.
    /// </summary>
    public static class CompanionBuffRegistry
    {
        public static IReadOnlyList<string> GetActiveBuffSummaries(PioneerRosterManager roster)
        {
            List<string> summaries = new List<string>();
            if (roster == null)
            {
                summaries.Add("No roster data.");
                return summaries;
            }

            IReadOnlyList<SkilledPioneerRecord> skilled = roster.SkilledPioneers;
            for (int i = 0; i < skilled.Count; i++)
            {
                SkilledPioneerRecord record = skilled[i];
                if (record == null || !ProvidesCompanionBuff(record))
                    continue;

                string passives = PioneerTraitUtility.FormatTraitList(record.passiveAbilityIds);
                summaries.Add($"{record.displayName}: {passives}");
            }

            if (summaries.Count == 0)
                summaries.Add("No synced or friendly echo passives active.");

            return summaries;
        }

        private static bool ProvidesCompanionBuff(SkilledPioneerRecord record)
        {
            if (record.passiveAbilityIds == null || record.passiveAbilityIds.Length == 0)
                return false;

            EchoDisposition disposition = record.Disposition;
            if (disposition != EchoDisposition.Friendly && disposition != EchoDisposition.Synced)
                return false;

            return record.Kind == PioneerKind.RescuedEcho
                || record.Kind == PioneerKind.NamedCatalog;
        }
    }
}
