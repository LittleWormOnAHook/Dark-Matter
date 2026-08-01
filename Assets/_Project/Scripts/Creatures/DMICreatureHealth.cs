using System;
using Project.AI;
using Project.Interaction;
using Project.Progression;
using Project.UI;
using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Health adapter for Malbers creatures. Wraps <see cref="EnemyHealth"/> so project
    /// projectiles, loot, and disintegrate continue to use the existing enemy pipeline.
    /// Malbers <c>MDamageable</c> damage is synced in via <see cref="DMICreatureBridge"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMICreatureHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyHealth legacyHealth;

        public event Action<float, float> HealthChanged;
        public event Action<float, bool> Damaged;
        public event Action<GameObject> DamagedBy;
        public event Action<float, GameObject, bool> DamagedWithSource;
        public event Action Died;
        public event Action Respawned;

        public float CurrentHealth => legacyHealth != null ? legacyHealth.CurrentHealth : 0f;
        public float MaxHealth => legacyHealth != null ? legacyHealth.MaxHealth : 0f;
        public bool IsDead => legacyHealth != null && legacyHealth.IsDead;

        private void Awake()
        {
            if (legacyHealth == null)
                legacyHealth = GetComponent<EnemyHealth>();

            if (GetComponent<EnemyProgressionXp>() == null)
                gameObject.AddComponent<EnemyProgressionXp>();
        }

        private void OnEnable()
        {
            BindLegacyEvents(true);
        }

        private void OnDisable()
        {
            BindLegacyEvents(false);
        }

        public void TakeDamage(float damage, GameObject source, bool isCritical = false)
        {
            if (legacyHealth == null)
                return;

            legacyHealth.TakeDamage(damage, source, isCritical);
        }

        public void SetRespawnExternallyManaged(bool value)
        {
            legacyHealth?.SetRespawnExternallyManaged(value);
        }

        public void FinishLootHoldAndRespawn()
        {
            legacyHealth?.FinishLootHoldAndRespawn();
        }

        private void EnsureLegacyHealth()
        {
            if (legacyHealth != null)
                return;

            legacyHealth = GetComponent<EnemyHealth>();
        }

        private void BindLegacyEvents(bool subscribe)
        {
            if (legacyHealth == null)
                return;

            if (subscribe)
            {
                legacyHealth.HealthChanged += ForwardHealthChanged;
                legacyHealth.Damaged += ForwardDamaged;
                legacyHealth.DamagedBy += ForwardDamagedBy;
                legacyHealth.DamagedWithSource += ForwardDamagedWithSource;
                legacyHealth.Died += ForwardDied;
                legacyHealth.Respawned += ForwardRespawned;
            }
            else
            {
                legacyHealth.HealthChanged -= ForwardHealthChanged;
                legacyHealth.Damaged -= ForwardDamaged;
                legacyHealth.DamagedBy -= ForwardDamagedBy;
                legacyHealth.DamagedWithSource -= ForwardDamagedWithSource;
                legacyHealth.Died -= ForwardDied;
                legacyHealth.Respawned -= ForwardRespawned;
            }
        }

        private void ForwardHealthChanged(float current, float max) => HealthChanged?.Invoke(current, max);
        private void ForwardDamaged(float amount, bool critical) => Damaged?.Invoke(amount, critical);
        private void ForwardDamagedBy(GameObject source) => DamagedBy?.Invoke(source);
        private void ForwardDamagedWithSource(float amount, GameObject source, bool critical) =>
            DamagedWithSource?.Invoke(amount, source, critical);
        private void ForwardDied() => Died?.Invoke();
        private void ForwardRespawned() => Respawned?.Invoke();
    }
}
