namespace Project.Features.GameState
{
    public sealed class MissionSnapshot
    {
        public static readonly MissionSnapshot Empty = new MissionSnapshot();

        public int ActiveQuestCount { get; }
        public string PrimaryQuestId { get; }
        public string PrimaryQuestStatus { get; }
        public int PrimaryObjectiveIndex { get; }
        public int PrimaryObjectiveProgress { get; }
        public int PrimaryObjectiveRequired { get; }

        public MissionSnapshot(
            int activeQuestCount = 0,
            string primaryQuestId = "",
            string primaryQuestStatus = "",
            int primaryObjectiveIndex = 0,
            int primaryObjectiveProgress = 0,
            int primaryObjectiveRequired = 0)
        {
            ActiveQuestCount = activeQuestCount;
            PrimaryQuestId = primaryQuestId ?? string.Empty;
            PrimaryQuestStatus = primaryQuestStatus ?? string.Empty;
            PrimaryObjectiveIndex = primaryObjectiveIndex;
            PrimaryObjectiveProgress = primaryObjectiveProgress;
            PrimaryObjectiveRequired = primaryObjectiveRequired;
        }
    }
}
