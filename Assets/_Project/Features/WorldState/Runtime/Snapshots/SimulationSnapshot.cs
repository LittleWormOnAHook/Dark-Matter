namespace Project.Features.WorldState
{
    public sealed class SimulationSnapshot
    {
        public static readonly SimulationSnapshot Empty = new SimulationSnapshot();

        public int TickIndex { get; }
        public int IncidentCount { get; }
        public string LastIncidentId { get; }

        public SimulationSnapshot(int tickIndex = 0, int incidentCount = 0, string lastIncidentId = "")
        {
            TickIndex = tickIndex;
            IncidentCount = incidentCount;
            LastIncidentId = lastIncidentId ?? string.Empty;
        }
    }
}
