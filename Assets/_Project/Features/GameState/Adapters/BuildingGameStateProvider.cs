using Project.Building;
using Project.Core;
using Project.Features.GameState;

namespace Project.Features.GameState.Adapters
{
    public sealed class BuildingGameStateProvider : IGameStateProvider
    {
        public string DomainId => "buildings";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            BuildingOperationsSaveRecord save = BuildingOperationRegistry.BuildSaveSnapshot();
            int buildingCount = save?.entries != null ? save.entries.Length : 0;
            int queued = 0;
            if (save?.entries != null)
            {
                for (int i = 0; i < save.entries.Length; i++)
                {
                    BuildingOperationSaveEntry entry = save.entries[i];
                    if (entry?.productionRecipeNames != null)
                        queued += entry.productionRecipeNames.Length;
                }
            }

            builder.Buildings = new BuildingSnapshot(
                buildingCount: buildingCount,
                assignedPioneerCount: BuildingOperationRegistry.CountAllAssignedPioneers(),
                queuedJobs: queued);
        }
    }
}
