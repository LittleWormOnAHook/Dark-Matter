using Project.Features.WorldState;
using Project.UI;

namespace Project.Features.WorldState.Adapters
{
    /// <summary>Heuristic stub until Features/Experience telemetry module exists.</summary>
    public sealed class ExperienceWorldStateProvider : IWorldStateProvider
    {
        public string DomainId => "experience";

        public void Contribute(WorldStateSnapshotBuilder builder)
        {
            bool crisis = EnvironmentalCrisisHudMode.IsCrisisActive;
            builder.Experience = new ExperienceSnapshot(
                radioDensity01: crisis ? 0.15f : 0.4f,
                tension01: crisis ? 0.7f : 0.2f,
                preferSilence: crisis);
        }
    }
}
