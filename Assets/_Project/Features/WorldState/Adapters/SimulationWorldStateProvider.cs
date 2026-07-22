using Project.Features.WorldState;
using Project.Pioneers;

namespace Project.Features.WorldState.Adapters
{
    public sealed class SimulationWorldStateProvider : IWorldStateProvider
    {
        public string DomainId => "simulation";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            int incidents = 0;
            string lastId = string.Empty;
            if (roster?.EchoChronicle != null)
            {
                for (int i = 0; i < roster.EchoChronicle.Count; i++)
                {
                    EchoChronicleEntry entry = roster.EchoChronicle[i];
                    if (entry == null)
                        continue;
                    if (entry.simulationIncident)
                    {
                        incidents++;
                        lastId = string.IsNullOrEmpty(entry.coreId) ? entry.id : entry.coreId;
                    }
                }
            }

            builder.Simulation = new SimulationSnapshot(
                tickIndex: 0,
                incidentCount: incidents,
                lastIncidentId: lastId);
        }
    }
}
