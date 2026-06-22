using Project.UI;
using UnityEngine;

namespace Project.AI
{
    [DisallowMultipleComponent]
    public class EnemyHealthBarPresenter : MonoBehaviour
    {
        [SerializeField] private bool showFloatingHealthBar = true;
        [SerializeField] private bool hideUntilDamaged;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2f, 0f);

        private EnemyHealth health;
        private FloatingTargetHealthBar bar;
        private bool revealed;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
        }

        private void Start()
        {
            if (!showFloatingHealthBar || health == null)
                return;

            bar = CombatUiSpawner.SpawnHealthBar(health, healthBarOffset);
            if (bar == null)
                return;

            if (hideUntilDamaged)
            {
                bar.SetVisible(false);
                health.Damaged += OnDamaged;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
        }

        private void OnDamaged(float damage, bool isCritical)
        {
            if (revealed || bar == null)
                return;

            revealed = true;
            bar.SetVisible(true);
        }
    }
}
