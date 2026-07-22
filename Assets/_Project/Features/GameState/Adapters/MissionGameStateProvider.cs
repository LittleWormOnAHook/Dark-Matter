using Project.Features.GameState;
using Project.Quests;

namespace Project.Features.GameState.Adapters
{
    public sealed class MissionGameStateProvider : IGameStateProvider
    {
        public string DomainId => "mission";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            QuestManager quests = QuestManager.Instance;
            if (quests == null)
            {
                builder.Mission = MissionSnapshot.Empty;
                return;
            }

            var all = quests.GetAllProgress();
            int active = 0;
            string primaryId = string.Empty;
            string primaryStatus = string.Empty;
            int objProgress = 0;
            int objRequired = 0;

            for (int i = 0; i < all.Count; i++)
            {
                QuestProgress progress = all[i];
                if (progress == null)
                    continue;
                if (progress.status == QuestStatus.Active)
                {
                    active++;
                    if (string.IsNullOrEmpty(primaryId))
                    {
                        primaryId = progress.questId;
                        primaryStatus = progress.status.ToString();
                        QuestDefinition def = quests.GetDefinition(progress.questId);
                        if (def != null && def.objectives != null && def.objectives.Count > 0)
                        {
                            objProgress = progress.GetObjectiveProgress(0);
                            objRequired = def.objectives[0].requiredCount;
                        }
                    }
                }
            }

            builder.Mission = new MissionSnapshot(
                activeQuestCount: active,
                primaryQuestId: primaryId,
                primaryQuestStatus: primaryStatus,
                primaryObjectiveIndex: 0,
                primaryObjectiveProgress: objProgress,
                primaryObjectiveRequired: objRequired);
        }
    }
}
