using UnityEngine;

namespace Project.Progression
{
    public class ExplorationXpTrigger : MonoBehaviour
    {
        [SerializeField] private string explorationId = "poi_default";
        [SerializeField] private int xpAmount = ProgressionXpDefaults.DiscoveryXp;
        [SerializeField] private bool triggerOnStartIfPlayerInside;
        [SerializeField] private bool destroyAfterGrant = true;

        private void Start()
        {
            if (triggerOnStartIfPlayerInside)
                TryGrant();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            TryGrant();
        }

        public void TryGrant()
        {
            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (progression == null || progression.HasExplorationXp(explorationId))
                return;

            if (!progression.TryMarkExplorationXp(explorationId, xpAmount))
                return;

            if (destroyAfterGrant)
                Destroy(gameObject);
        }
    }
}
