using Project.Building;
using Project.Features.GameState;
using System.Collections.Generic;

namespace Project.Features.GameState.Adapters
{
    public sealed class PowerGameStateProvider : IGameStateProvider
    {
        public string DomainId => "power";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            IReadOnlyList<PowerGenerator> generators = PowerGenerator.Active;
            if (generators == null || generators.Count == 0)
            {
                builder.Power = PowerSnapshot.Empty;
                return;
            }

            int powered = 0;
            float fuelSum = 0f;
            bool critical = false;
            int count = 0;
            for (int i = 0; i < generators.Count; i++)
            {
                PowerGenerator gen = generators[i];
                if (gen == null)
                    continue;

                count++;
                if (gen.HasPower)
                    powered++;
                fuelSum += gen.FuelPercent01;
                if (gen.FuelPercent01 < 0.15f)
                    critical = true;
            }

            if (count == 0)
            {
                builder.Power = PowerSnapshot.Empty;
                return;
            }

            builder.Power = new PowerSnapshot(
                generatorCount: count,
                poweredCount: powered,
                averageFuelPercent: fuelSum / count,
                anyCritical: critical);
        }
    }
}
