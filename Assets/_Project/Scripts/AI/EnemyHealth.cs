using System;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.AI
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float destroyDelay = 3f;
        public float respawnTime;
        [SerializeField] private Transform healthBarAnchor;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2f, 0f);

        private float currentHealth;
        private bool isDead;
        private bool respawnExternallyManaged;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private bool spawnCaptured;

        public event Action<float, float> HealthChanged;
        public event Action<float, bool> Damaged;
        public event Action Died;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool IsRespawnExternallyManaged => respawnExternallyManaged;
        public Transform HealthBarAnchor => healthBarAnchor != null ? healthBarAnchor : transform;
        public Vector3 HealthBarOffset => healthBarOffset;

        private void Awake()
        {
            CaptureSpawnPoint();
        }

        private void OnEnable()
        {
            currentHealth = maxHealth;
            isDead = false;
            NotifyHealthChanged();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Respawn));
        }

        public void TakeDamage(float damage, GameObject source, bool isCritical = false)
        {
            if (isDead || damage <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            NotifyHealthChanged();
            Damaged?.Invoke(damage, isCritical);

            Vector3 feedbackPosition = transform.position + Vector3.up * 1.5f;
            CombatUiSpawner.ShowDamage(damage, feedbackPosition, isCritical);

            if (currentHealth <= 0f)
                HandleDeath();
        }

        private void HandleDeath()
        {
            if (isDead)
                return;

            isDead = true;
            Died?.Invoke();

            EnemyAiController ai = GetComponent<EnemyAiController>();
            if (ai != null)
                ai.enabled = false;

            EnemyCombat combat = GetComponent<EnemyCombat>();
            if (combat != null)
                combat.enabled = false;

            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            if (respawnTime > 0f && !respawnExternallyManaged)
            {
                Invoke(nameof(Respawn), respawnTime);
                return;
            }

            if (respawnExternallyManaged)
                return;

            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }

        public void SetRespawnExternallyManaged(bool external)
        {
            respawnExternallyManaged = external;
            if (external)
                CancelInvoke(nameof(Respawn));
        }

        public void ForceRespawn()
        {
            CancelInvoke(nameof(Respawn));
            Respawn();
        }

        private void Respawn()
        {
            respawnExternallyManaged = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            isDead = false;
            currentHealth = maxHealth;
            NotifyHealthChanged();

            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;

            EnemyCombat combat = GetComponent<EnemyCombat>();
            if (combat != null)
                combat.enabled = true;

            EnemyAiController ai = GetComponent<EnemyAiController>();
            if (ai != null)
                ai.enabled = true;

            EnemyAnimationController animation = GetComponent<EnemyAnimationController>();
            if (animation != null)
            {
                animation.enabled = false;
                animation.enabled = true;
            }
        }

        private void CaptureSpawnPoint()
        {
            if (spawnCaptured)
                return;

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnCaptured = true;
        }

        private void NotifyHealthChanged()
        {
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
