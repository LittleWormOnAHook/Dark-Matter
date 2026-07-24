using Project.Building;
using Project.Features.GameState;
using UnityEngine;

namespace Project.Features.GameState.Adapters
{
    public sealed class PowerGameStateProvider : IGameStateProvider
    {
        public string DomainId => "power";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            PowerGenerator[] generators = Object.FindObjectsByType<PowerGenerator>();
            if (generators == null || generators.Length == 0)
            {
                builder.Power = PowerSnapshot.Empty;
                return;
            }

            int powered = 0;
            float fuelSum = 0f;
            bool critical = false;
            for (int i = 0; i < generators.Length; i++)
            {
                PowerGenerator gen = generators[i];
                if (gen == null)
                    continue;
                if (gen.HasPower)
                    powered++;
                fuelSum += gen.FuelPercent01;
                if (gen.FuelPercent01 < 0.15f)
                    critical = true;
            }

            builder.Power = new PowerSnapshot(
                generatorCount: generators.Length,
                poweredCount: powered,
                averageFuelPercent: fuelSum / generators.Length,
                anyCritical: critical);
        }
    }
}
