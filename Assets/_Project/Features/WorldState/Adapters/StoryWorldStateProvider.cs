using Project.Features.WorldState;
using Project.Quests;

namespace Project.Features.WorldState.Adapters
{
    public sealed class StoryWorldStateProvider : IWorldStateProvider
    {
        public string DomainId => "story";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            QuestManager quests = QuestManager.Instance;
            if (quests == null)
            {
                builder.Story = StoryProgressSnapshot.Empty;
                return;
            }

            var all = quests.GetAllProgress();
            int active = 0;
            int completed = 0;
            string primary = string.Empty;
            for (int i = 0; i < all.Count; i++)
            {
                QuestProgress p = all[i];
                if (p == null)
                    continue;
                if (p.status == QuestStatus.Active)
                {
                    active++;
                    if (string.IsNullOrEmpty(primary))
                        primary = p.questId;
                }
                else if (p.status == QuestStatus.Completed || p.status == QuestStatus.TurnedIn)
                {
                    completed++;
                }
            }

            builder.Story = new StoryProgressSnapshot(
                chapterId: active > 0 ? "active-ops" : "prologue",
                activeQuestCount: active,
                completedQuestCount: completed,
                primaryQuestId: primary);
        }
    }
}
