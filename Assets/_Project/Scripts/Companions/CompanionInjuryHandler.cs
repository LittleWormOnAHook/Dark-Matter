using Project.Pioneers;
using Project.UI;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Sends fallen expedition pioneers to the Science Lab and marks them injured on the roster.
    /// </summary>
    public class CompanionInjuryHandler : MonoBehaviour
    {
        private PioneerCompanionAgent agent;
        private CompanionHealth health;
        private bool subscribed;

        private void Awake()
        {
            agent = GetComponent<PioneerCompanionAgent>();
            health = GetComponent<CompanionHealth>();
        }

        public void Bind(string pioneerRecordId)
        {
            if (health == null)
                return;

            health.Initialize(pioneerRecordId);

            if (subscribed)
                return;

            health.Died += HandleCompanionDeath;
            subscribed = true;
        }

        private void OnDestroy()
        {
            if (health != null && subscribed)
                health.Died -= HandleCompanionDeath;
        }

        private void HandleCompanionDeath()
        {
            if (agent == null || string.IsNullOrWhiteSpace(agent.PioneerRecordId))
                return;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null || !roster.TryMarkSkilledInjured(agent.PioneerRecordId, out _))
                return;

            CompanionRosterBridge bridge = FindAnyObjectByType<CompanionRosterBridge>();
            bridge?.RefreshCompanions();

            ScienceLabRecoveryStation station = ScienceLabRecoveryStation.EnsureExists();
            station?.RefreshInjuredProxies();

            PickupToastUI.Show($"{agent.DisplayName} was injured and sent to the Science Lab.");
        }
    }
}
