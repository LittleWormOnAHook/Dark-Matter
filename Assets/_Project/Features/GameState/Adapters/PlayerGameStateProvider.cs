using Project.Core;
using Project.Features.GameState;
using Project.Survival;
using UnityEngine;

namespace Project.Features.GameState.Adapters
{
    public sealed class PlayerGameStateProvider : IGameStateProvider
    {
        public string DomainId => "player";

        public void Contribute(GameStateSnapshotBuilder builder)
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            SurvivalStats stats = player != null ? player.GetComponent<SurvivalStats>() : null;
            Vector3 pos = player != null ? player.transform.position : Vector3.zero;

            if (stats == null)
            {
                builder.Player = new PlayerSnapshot(posX: pos.x, posY: pos.y, posZ: pos.z);
                return;
            }

            builder.Player = new PlayerSnapshot(
                health: stats.CurrentHealth,
                maxHealth: stats.maxHealth,
                energy: stats.CurrentEnergy,
                maxEnergy: stats.maxEnergy,
                stamina: stats.CurrentStamina,
                maxStamina: stats.maxStamina,
                oxygen: stats.CurrentOxygen,
                maxOxygen: stats.maxOxygen,
                thermalStress: stats.CurrentThermalStress,
                radiation: stats.CurrentRadiation,
                sulfur: stats.CurrentSulfur,
                volcano: stats.CurrentVolcano,
                isDead: stats.IsDead,
                posX: pos.x,
                posY: pos.y,
                posZ: pos.z);
        }
    }
}
