using System;
using Project.Interaction;
using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Hovercraft damage model: a shield absorbs hits first and passively regenerates a few seconds
    /// after the last hit; once the shield is fully depleted, further damage comes straight off
    /// health, which does NOT auto-regenerate (needs repair). Mirrors CompanionHealth/EnemyHealth's
    /// IDamageable shape but without respawn logic — a wrecked hovercraft just goes dead in the water.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private HovercraftController controller;

        [Header("Shield")]
        [SerializeField] private float maxShield = 60f;
        [SerializeField] private float shieldRegenPerSecond = 12f;
        [Tooltip("Seconds since the last hit before the shield starts regenerating.")]
        [SerializeField] private float shieldRegenDelay = 4f;

        [Header("Health")]
        [SerializeField] private float maxHealth = 120f;

        private float currentShield;
        private float currentHealth;
        private float lastHitTime = -999f;
        private bool destroyed;

        public event Action<float, float> ShieldChanged;
        public event Action<float, float> HealthChanged;
        public event Action<float, bool> Damaged;
        public event Action Destroyed;

        public float MaxShield => maxShield;
        public float CurrentShield => currentShield;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDestroyed => destroyed;

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<HovercraftController>();

            currentShield = maxShield;
            currentHealth = maxHealth;
        }

        private void Update()
        {
            if (destroyed || currentShield >= maxShield)
                return;

            if (Time.time - lastHitTime < shieldRegenDelay)
                return;

            float next = Mathf.Min(maxShield, currentShield + shieldRegenPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(next, currentShield))
            {
                currentShield = next;
                ShieldChanged?.Invoke(currentShield, maxShield);
            }
        }

        void IDamageable.TakeDamage(float damage, GameObject source, bool isCritical)
        {
            ApplyDamage(damage, isCritical);
        }

        public void ApplyDamage(float damage, bool isCritical = false)
        {
            if (destroyed || damage <= 0f)
                return;

            lastHitTime = Time.time;
            float remaining = damage;

            if (currentShield > 0f)
            {
                float absorbed = Mathf.Min(currentShield, remaining);
                currentShield -= absorbed;
                remaining -= absorbed;
                ShieldChanged?.Invoke(currentShield, maxShield);
            }

            if (remaining > 0f)
            {
                currentHealth = Mathf.Max(0f, currentHealth - remaining);
                HealthChanged?.Invoke(currentHealth, maxHealth);
            }

            Damaged?.Invoke(damage, isCritical);

            if (currentHealth <= 0f)
                HandleDestroyed();
        }

        /// <summary>Directly sets shield/health/destroyed state — used by save/load to restore a
        /// persisted vehicle instead of resetting it to full every load.</summary>
        public void SetState(float shield, float health, bool isDestroyed)
        {
            currentShield = Mathf.Clamp(shield, 0f, maxShield);
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            destroyed = isDestroyed || currentHealth <= 0f;

            ShieldChanged?.Invoke(currentShield, maxShield);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (controller != null)
                controller.enabled = !destroyed;
        }

        /// <summary>Full repair — call from a dock/repair-bay interaction once that exists.</summary>
        public void RepairFully()
        {
            destroyed = false;
            currentShield = maxShield;
            currentHealth = maxHealth;
            ShieldChanged?.Invoke(currentShield, maxShield);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (controller != null)
                controller.enabled = true;
        }

        private void HandleDestroyed()
        {
            destroyed = true;
            Destroyed?.Invoke();

            // Wrecked — disable the controller so it can no longer be driven/fired until repaired.
            // Passengers already aboard stay put rather than being force-ejected mid-flight.
            if (controller != null)
                controller.enabled = false;
        }
    }
}
