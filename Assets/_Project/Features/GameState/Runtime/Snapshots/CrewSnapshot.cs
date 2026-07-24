namespace Project.Features.GameState
{
    public sealed class CrewMemberStateSnapshot
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string ClassLabel { get; }
        public int Level { get; }
        public bool InExpeditionTrio { get; }
        public string WorkState { get; }

        public CrewMemberStateSnapshot(
            string id = "",
            string displayName = "",
            string classLabel = "",
            int level = 1,
            bool inExpeditionTrio = false,
            string workState = "Idle")
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ClassLabel = classLabel ?? string.Empty;
            Level = level;
            InExpeditionTrio = inExpeditionTrio;
            WorkState = workState ?? string.Empty;
        }
    }

    public sealed class CrewSnapshot
    {
        public static readonly CrewSnapshot Empty = new CrewSnapshot();

        public CrewMemberStateSnapshot[] Members { get; }

        public CrewSnapshot(CrewMemberStateSnapshot[] members = null)
        {
            Members = members ?? System.Array.Empty<CrewMemberStateSnapshot>();
        }
    }
}
