using System.Collections.Generic;
using Project.Features.GameState;
using Project.Pioneers;

namespace Project.Features.GameState.Adapters
{
    public sealed class CrewGameStateProvider : IGameStateProvider
    {
        public string DomainId => "crew";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster == null || roster.SkilledPioneers == null)
            {
                builder.Crew = CrewSnapshot.Empty;
                return;
            }

            var members = new List<CrewMemberStateSnapshot>(roster.SkilledPioneers.Count);
            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record == null)
                    continue;
                members.Add(new CrewMemberStateSnapshot(
                    id: record.id,
                    displayName: record.displayName,
                    classLabel: SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass),
                    level: record.level,
                    inExpeditionTrio: record.isInExpeditionTrio,
                    workState: record.WorkState.ToString()));
            }

            builder.Crew = new CrewSnapshot(members.ToArray());
        }
    }
}
