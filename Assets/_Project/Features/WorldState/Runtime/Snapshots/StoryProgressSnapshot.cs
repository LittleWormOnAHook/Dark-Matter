namespace Project.Features.WorldState
{
    public sealed class StoryProgressSnapshot
    {
        public static readonly StoryProgressSnapshot Empty = new StoryProgressSnapshot();

        public string ChapterId { get; }
        public int ActiveQuestCount { get; }
        public int CompletedQuestCount { get; }
        public string PrimaryQuestId { get; }

        public StoryProgressSnapshot(
            string chapterId = "",
            int activeQuestCount = 0,
            int completedQuestCount = 0,
            string primaryQuestId = "")
        {
            ChapterId = chapterId ?? string.Empty;
            ActiveQuestCount = activeQuestCount;
            CompletedQuestCount = completedQuestCount;
            PrimaryQuestId = primaryQuestId ?? string.Empty;
        }
    }
}
