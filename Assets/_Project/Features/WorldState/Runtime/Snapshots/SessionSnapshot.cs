namespace Project.Features.WorldState
{
    public sealed class SessionSnapshot
    {
        public static readonly SessionSnapshot Empty = new SessionSnapshot();

        public string PhaseLabel { get; }
        public int SaveSlotIndex { get; }
        public bool HasStarted { get; }

        public SessionSnapshot(string phaseLabel = "Unknown", int saveSlotIndex = -1, bool hasStarted = false)
        {
            PhaseLabel = phaseLabel ?? string.Empty;
            SaveSlotIndex = saveSlotIndex;
            HasStarted = hasStarted;
        }
    }
}
