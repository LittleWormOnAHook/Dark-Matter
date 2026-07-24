namespace Project.Features.Directors
{
    public readonly struct SimulationIncident
    {
        public string IncidentId { get; }
        public float Severity01 { get; }
        public string Reason { get; }

        public SimulationIncident(string incidentId, float severity01, string reason)
        {
            IncidentId = incidentId ?? string.Empty;
            Severity01 = severity01;
            Reason = reason ?? string.Empty;
        }
    }

    public interface ISimulationCommandService
    {
        bool TryApplyIncident(SimulationIncident incident);
    }
}
