using Project.UI;
using UnityEngine;

namespace Project.AI
{
    [DisallowMultipleComponent]
    public class EnemyHealthBarPresenter : MonoBehaviour
    {
        [SerializeField] private bool showFloatingHealthBar = true;
        [SerializeField] private bool hideUntilDamaged = true;
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

            SpawnBar();

            if (hideUntilDamaged && bar != null)
            {
                bar.SetVisible(false);
                health.Damaged += OnDamaged;
            }

            health.Died += HandleDied;
            health.Respawned += HandleRespawned;
        }

        private void SpawnBar()
        {
            if (bar != null || !showFloatingHealthBar || health == null)
                return;

            bar = CombatUiSpawner.SpawnHealthBar(health, healthBarOffset);
        }

        private void HandleDied()
        {
            DestroyHealthBar();
        }

        private void HandleRespawned()
        {
            revealed = false;
            SpawnBar();

            if (hideUntilDamaged && bar != null)
                bar.SetVisible(false);
        }

        private void DestroyHealthBar()
        {
            if (bar == null)
                return;

            Destroy(bar.gameObject);
            bar = null;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= HandleDied;
                health.Respawned -= HandleRespawned;
            }

            DestroyHealthBar();
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
