using Project.Building;
using Project.Companions;
using Project.Pet;
using UnityEngine;

namespace Project.Managers
{
    /// <summary>
    /// Ensures expedition companion and facility simulation systems exist at runtime.
    /// </summary>
    public static class CompanionSystemsBootstrap
    {
        public static void EnsureGameplaySystems(MonoBehaviour host)
        {
            if (host == null)
                return;

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

            PetManager.Instance?.ApplyToolbarVisibility();
        }
    }
}
