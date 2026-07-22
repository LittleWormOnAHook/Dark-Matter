using Project.Features.Directors;
using Project.Pioneers;
using UnityEngine;

namespace Project.Features.Directors.Adapters
{
    public sealed class SimulationCommandServiceAdapter : ISimulationCommandService
    {
        public bool TryApplyIncident(SimulationIncident incident)
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster == null)
            {
                Debug.LogWarning("[Directors] SimulationCommand: no roster for incident " + incident.IncidentId);
                return false;
            }

            roster.AppendEchoChronicle(
                EchoChronicleEntry.CreateSimulationIncident(
                    incident.IncidentId,
                    incident.Severity01,
                    incident.Reason));
            return true;
        }
    }
}
