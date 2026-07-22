using Project.Features.WorldState;
using UnityEngine;

namespace Project.Features.Directors
{
    /// <summary>Stub SimulationDirector — future off-screen incidents via ISimulationCommandService.</summary>
    public sealed class SimulationDirectorService : IDirector
    {
        private readonly ISimulationCommandService simulationCommands;

        public string DirectorId => "Simulation";
        public int EvaluationCount { get; private set; }

        public SimulationDirectorService(ISimulationCommandService simulationCommands = null)
        {
            this.simulationCommands = simulationCommands;
        }

        public void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger)
        {
            EvaluationCount++;
            if (trigger == DirectorTrigger.ManualDebug || trigger == DirectorTrigger.SimulationTick)
            {
                Debug.Log(string.Format(
                    "[Directors] Simulation trigger={0} tick={1} incidents={2} cmd={3}",
                    trigger,
                    world != null ? world.Simulation.TickIndex : 0,
                    world != null ? world.Simulation.IncidentCount : 0,
                    simulationCommands != null ? "wired" : "unwired"));
            }
        }
    }
}
