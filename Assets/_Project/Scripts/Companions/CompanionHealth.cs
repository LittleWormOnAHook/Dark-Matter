using System;
using Project.AI;
using Project.Interaction;
using Project.Survival.Exposure;
using Project.UI;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Lightweight health for expedition pioneers so enemies can damage them without full survival simulation.
    /// </summary>
    public class CompanionHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 80f;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2f, 0f);

        private string pioneerRecordId;
        private bool deathHandled;

        public event Action<float, float> HealthChanged;
        public event Action<float, bool> Damaged;
        public event Action Died;

        /// <summary>Global fan-out so expedition slot arcs bind even if they miss the first HealthChanged.</summary>
        public static event Action<CompanionHealth, float, float> AnyHealthChanged;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;
        public string PioneerRecordId => pioneerRecordId;
        public Transform HealthBarAnchor => healthBarAnchor != null ? healthBarAnchor : transform;
        public Vector3 HealthBarOffset => healthBarOffset;

        private void Awake()
        {
            ResetHealth();
        }

        public void Initialize(string recordId)
        {
            pioneerRecordId = recordId;
            deathHandled = false;
            ResetHealth();
            ExpeditionPioneerHudUI.NotifyCompanionHealthReady(this);
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            deathHandled = false;
            NotifyHealthChanged();
        }

        public void ApplyDamage(float damage, bool isCritical = false)
        {
            if (damage <= 0f || IsDead)
                return;

            CompanionExposureResponder exposure = GetComponent<CompanionExposureResponder>();
            if (exposure != null)
                damage *= exposure.DamageTakenMultiplier;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            NotifyHealthChanged();
            Damaged?.Invoke(damage, isCritical);

            Vector3 feedbackPosition = transform.position + Vector3.up * 1.5f;
            CombatUiSpawner.ShowDamage(damage, feedbackPosition, isCritical);

            if (CurrentHealth <= 0f)
                HandleDeath();
        }

        public void ApplyHeal(float amount)
        {
            if (amount <= 0f || IsDead)
                return;

            float previous = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            if (Mathf.Approximately(CurrentHealth, previous))
                return;

            NotifyHealthChanged();
        }

        public void ApplyHealPercent(float percent)
        {
            if (percent <= 0f || IsDead)
                return;

            ApplyHeal(maxHealth * Mathf.Clamp01(percent));
        }

        /// <summary>
        /// IDamageable entry point so the shared CombatProjectile (unified player/companion/enemy
        /// projectile pipeline) can hit and damage companions directly, not just the Invector
        /// onReceiveDamage path. Also raises the group-alert event so squadmates react to whoever
        /// fired, matching the existing Invector damage bridge behavior.
        /// </summary>
        void IDamageable.TakeDamage(float damage, GameObject source, bool isCritical)
        {
            ApplyDamage(damage, isCritical);

            EnemyHealth attacker = source != null ? source.GetComponentInParent<EnemyHealth>() : null;
            if (attacker != null)
                PlayerCombatEvents.RaiseCompanionAttackedBy(attacker);
        }

        private void HandleDeath()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            Died?.Invoke();
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            AnyHealthChanged?.Invoke(this, CurrentHealth, maxHealth);
        }
    }
}
