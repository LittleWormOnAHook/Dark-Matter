using Project.Building;
using Project.Companions;
using Project.Core;
using Project.Features.Directors.Adapters;
using Project.Features.GameState.Adapters;
using Project.Features.WorldState.Adapters;
using Project.Pet;
using Project.Pioneers;
using Project.Survival;
using Project.Survival.Exposure;
using UnityEngine;

namespace Project.Managers
{
    /// <summary>
    /// Ensures expedition companion, World Engine Features, and facility simulation systems exist at runtime.
    /// Bootstrap order (TDB): GameState → WorldState → Directors → (Communications later) → legacy bridges.
    /// </summary>
    public static class CompanionSystemsBootstrap
    {
        public static void EnsureGameplaySystems(MonoBehaviour host)
        {
            if (host == null)
                return;

            // World Engine spine (GDD B4 Run 1)
            GameStateBootstrap.EnsureExists(host);
            WorldStateBootstrap.EnsureExists(host);
            DirectorsBootstrap.EnsureExists(host);

            if (Object.FindAnyObjectByType<CompanionRosterBridge>() == null)
                host.gameObject.AddComponent<CompanionRosterBridge>();

            if (Object.FindAnyObjectByType<FacilityTaskRunner>() == null)
                host.gameObject.AddComponent<FacilityTaskRunner>();

            CompanionCombatCoordinator.EnsureExists(host);

            PetManager.EnsureExists(host);

            ScienceLabRecoveryStation.EnsureExists();

            CompanionRosterBridge bridge = Object.FindAnyObjectByType<CompanionRosterBridge>();
            if (bridge == null)
                bridge = host.gameObject.AddComponent<CompanionRosterBridge>();

            PioneerCompanionAgent defaultPrefab = PioneerCompanionDefaults.LoadDefaultAgentPrefab();
            if (defaultPrefab != null)
                bridge.SetDefaultPrefab(defaultPrefab);

            if (Object.FindAnyObjectByType<PioneerExpeditionCommandInput>() == null)
                host.gameObject.AddComponent<PioneerExpeditionCommandInput>();

            if (Object.FindAnyObjectByType<PioneerExpeditionProgressionBridge>() == null)
            {
                PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
                if (roster != null)
                    roster.gameObject.AddComponent<PioneerExpeditionProgressionBridge>();
                else
                    host.gameObject.AddComponent<PioneerExpeditionProgressionBridge>();
            }

            PetManager.Instance?.ApplyToolbarVisibility();

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null && player.GetComponent<PlayerExposureBootstrap>() == null)
                player.AddComponent<PlayerExposureBootstrap>();
        }
    }
}
