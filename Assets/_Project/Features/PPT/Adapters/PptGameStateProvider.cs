using System.Linq;
using Project.Features.GameState;
using Project.PPT;

namespace Project.Features.PPT.Adapters
{
    public sealed class PptGameStateProvider : IGameStateProvider
    {
        public string DomainId => "ppt";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            string[] known = PptKeywordLog.GetKnownIds().ToArray();
            int take = known.Length > 8 ? 8 : known.Length;
            string[] recent = new string[take];
            for (int i = 0; i < take; i++)
                recent[i] = known[known.Length - take + i];

            builder.PptKnowledge = new PptKnowledgeSnapshot(known.Length, recent);
        }
    }
}
