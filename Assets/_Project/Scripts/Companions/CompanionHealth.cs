using System;
using Project.UI;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Lightweight health for expedition pioneers so enemies can damage them without full survival simulation.
    /// </summary>
    public class CompanionHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 80f;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2f, 0f);

        private string pioneerRecordId;
        private bool deathHandled;

        public event Action<float, float> HealthChanged;
        public event Action<float, bool> Damaged;
        public event Action Died;

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

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            NotifyHealthChanged();
            Damaged?.Invoke(damage, isCritical);

            Vector3 feedbackPosition = transform.position + Vector3.up * 1.5f;
            CombatUiSpawner.ShowDamage(damage, feedbackPosition, isCritical);

            if (CurrentHealth <= 0f)
                HandleDeath();
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
        }
    }
}
