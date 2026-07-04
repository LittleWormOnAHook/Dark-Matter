using Project.AI;
using Project.Progression;
using UnityEngine;

namespace Project.AI
{
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyProgressionXp : MonoBehaviour
    {
        [SerializeField] private int xpReward = ProgressionXpDefaults.EnemyKillXp;
        [SerializeField] private string enemyXpKey;
        [SerializeField] private string enemyTypeId;

        private EnemyHealth health;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (string.IsNullOrEmpty(enemyXpKey))
                enemyXpKey = $"{gameObject.name}:{gameObject.GetEntityId()}";
        }

        private void OnEnable()
        {
            if (health != null)
                health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= HandleDied;
        }

        public void Configure(int reward, string key = null)
        {
            xpReward = reward;
            if (!string.IsNullOrEmpty(key))
                enemyXpKey = key;
        }

        private void HandleDied()
        {
            string typeId = ResolveEnemyTypeId();
            EnemyKillEvents.Notify(typeId);

            if (xpReward <= 0)
                return;

            // Respawnable enemies should reward every kill. Passing a one-time key here
            // caused only the first life of each enemy instance to grant XP.
            ProgressionRewardGranter.GrantXp(xpReward, XpSource.Combat, oneTimeKey: null, "Combat");
        }

        private string ResolveEnemyTypeId()
        {
            if (!string.IsNullOrEmpty(enemyTypeId))
                return enemyTypeId;

            return gameObject.name;
        }
    }
}
