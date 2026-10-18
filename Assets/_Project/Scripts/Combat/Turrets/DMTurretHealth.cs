using System.Collections.Generic;
using Project.Interaction;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Turret HP. Player and enemies both deal damage through the shared combat pipeline.</summary>
    [DisallowMultipleComponent]
    public sealed class DMTurretHealth : MonoBehaviour, IDamageable, IDamageReceiver
    {
        private float currentHealth;
        private bool dead;
        private DMTurretRuntime runtime;

        public float CurrentHealth => currentHealth;
        public float MaxHealth =>
            runtime != null ? runtime.MaxHealth : FallbackMaxHealth();

        private static float FallbackMaxHealth()
        {
            DMTurretProfile profile = DMTurretProfile.Live;
            return profile != null ? profile.MaxHealth : 100f;
        }

        public bool IsDead => dead;
        public DMTurretFaction Faction => runtime != null ? runtime.Faction : DMTurretFaction.Enemy;

        public static bool IsTurretTransform(Transform candidate)
        {
            return candidate != null && candidate.GetComponentInParent<DMTurretHealth>() != null;
        }

        private void Awake()
        {
            runtime = GetComponent<DMTurretRuntime>();
            if (runtime == null)
                runtime = GetComponentInParent<DMTurretRuntime>();
            currentHealth = MaxHealth;
        }

        private void OnEnable()
        {
            dead = false;
            if (currentHealth <= 0f)
                currentHealth = MaxHealth;
            DMTurretRegistry.Register(this);
        }

        private void OnDisable()
        {
            DMTurretRegistry.Unregister(this);
        }

        public void ReceiveDamage(in DamageInfo info)
        {
            TakeDamage(info.Amount, info.Source, info.IsCritical);
        }

        public void TakeDamage(float damage, GameObject source, bool isCritical = false)
        {
            if (dead || damage <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (currentHealth > 0.001f)
                return;

            dead = true;
            runtime?.NotifyDestroyed();
        }

        public void RestoreFull()
        {
            dead = false;
            currentHealth = MaxHealth;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }
    }

    public static class DMTurretRegistry
    {
        private static readonly List<DMTurretHealth> Live = new List<DMTurretHealth>(16);

        public static int Count => Live.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Live.Clear();

        internal static void Register(DMTurretHealth health)
        {
            if (health == null || Live.Contains(health))
                return;
            Live.Add(health);
        }

        internal static void Unregister(DMTurretHealth health)
        {
            if (health == null)
                return;
            Live.Remove(health);
        }

        public static void CopyLiving(List<DMTurretHealth> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < Live.Count; i++)
            {
                DMTurretHealth health = Live[i];
                if (health != null && health.isActiveAndEnabled && !health.IsDead)
                    buffer.Add(health);
            }
        }

        public static DMTurretHealth FindNearest(
            Vector3 origin,
            float maxDistance,
            DMTurretFaction faction)
        {
            float bestSqr = maxDistance * maxDistance;
            DMTurretHealth best = null;
            for (int i = 0; i < Live.Count; i++)
            {
                DMTurretHealth health = Live[i];
                if (health == null || health.IsDead || !health.isActiveAndEnabled)
                    continue;
                if (health.Faction != faction)
                    continue;

                Vector3 delta = health.transform.position - origin;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;
                bestSqr = sqr;
                best = health;
            }

            return best;
        }
    }
}
