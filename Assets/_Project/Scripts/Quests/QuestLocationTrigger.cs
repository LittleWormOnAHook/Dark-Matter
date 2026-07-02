using Project.Progression;
using UnityEngine;

namespace Project.Quests
{
    [RequireComponent(typeof(Collider))]
    public class QuestLocationTrigger : MonoBehaviour
    {
        [SerializeField] private string locationId = "camp_clearing";
        [SerializeField] private bool triggerOnce = true;

        private bool hasTriggered;

        private void Awake()
        {
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnce && hasTriggered)
                return;

            if (!other.CompareTag("Player"))
                return;

            QuestManager questManager = QuestManager.Instance ?? FindAnyObjectByType<QuestManager>();
            if (questManager == null || string.IsNullOrEmpty(locationId))
                return;

            if (!questManager.NotifyLocationReached(locationId))
                return;

            ProgressionRewardGranter.GrantXp(
                ProgressionXpDefaults.DiscoveryXp,
                XpSource.Exploration,
                $"location:{locationId}",
                "Location");
            hasTriggered = true;
        }
    }
}
