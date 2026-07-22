namespace Project.Features.GameState
{
    /// <summary>Placeholder until research systems exist.</summary>
    public sealed class ResearchSnapshot
    {
        public static readonly ResearchSnapshot Empty = new ResearchSnapshot();

        public int UnlockedNodeCount { get; }

        public ResearchSnapshot(int unlockedNodeCount = 0)
        {
            UnlockedNodeCount = unlockedNodeCount;
        }
    }
}
