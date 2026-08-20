namespace Project.Features.GameState
{
    public readonly struct PptKnowledgeSnapshot
    {
        public static readonly PptKnowledgeSnapshot Empty = new PptKnowledgeSnapshot(0, System.Array.Empty<string>());

        public PptKnowledgeSnapshot(int knownKeywordCount, string[] recentKeywordIds)
        {
            KnownKeywordCount = knownKeywordCount;
            RecentKeywordIds = recentKeywordIds ?? System.Array.Empty<string>();
        }

        public int KnownKeywordCount { get; }
        public string[] RecentKeywordIds { get; }
    }
}
