using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Generic elemental damage-over-time controller. Auto-attached (via <see cref="Apply"/>) to
    /// whichever GameObject carries the IDamageable component that was hit, so burning/shocked/
    /// corroded/etc. ticks work identically for the player, companions, and enemies without each
    /// health class needing its own DoT bookkeeping. Re-applying the same effect type refreshes
    /// the duration instead of stacking multiple ticking instances.
    /// </summary>
    public class CombatStatusEffectController : MonoBehaviour
    {
        private class ActiveEffect
        {
            public StatusEffectType type;
            public float damagePerTick;
            public float tickInterval;
            public float remainingDuration;
            public float nextTickTime;
            public GameObject source;
            public GameObject vfxInstance;
        }

        private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>(4);
        private IDamageable damageable;

        /// <summary>
        /// Applies (or refreshes) a status effect on the given target. targetRoot must be the exact
        /// GameObject the IDamageable component lives on — CombatProjectile resolves this from the
        /// hit collider before calling in.
        /// </summary>
        public static void Apply(
            GameObject targetRoot,
            StatusEffectType type,
            float damagePerTick,
            float tickInterval,
            float duration,
            GameObject source,
            GameObject vfxPrefab = null)
        {
            if (targetRoot == null || type == StatusEffectType.None || duration <= 0f)
                return;

            CombatStatusEffectController controller = targetRoot.GetComponent<CombatStatusEffectController>();
            if (controller == null)
                controller = targetRoot.AddComponent<CombatStatusEffectController>();

            controller.ApplyEffect(type, damagePerTick, Mathf.Max(0.1f, tickInterval), duration, source, vfxPrefab);
        }

        public bool HasEffect(StatusEffectType type)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].type == type)
                    return true;
            }

            return false;
        }

        private void Awake()
        {
            damageable = GetComponent<IDamageable>();
        }

        private void ApplyEffect(
            StatusEffectType type,
            float damagePerTick,
            float tickInterval,
            float duration,
            GameObject source,
            GameObject vfxPrefab)
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                ActiveEffect existing = activeEffects[i];
                if (existing.type != type)
                    continue;

                existing.remainingDuration = duration;
                existing.damagePerTick = damagePerTick;
                existing.tickInterval = tickInterval;
                existing.source = source;
                return;
            }

            ActiveEffect effect = new ActiveEffect
            {
                type = type,
                damagePerTick = damagePerTick,
                tickInterval = tickInterval,
                remainingDuration = duration,
                nextTickTime = Time.time + tickInterval,
                source = source,
            };

            if (vfxPrefab != null)
            {
                Transform anchor = transform;
                effect.vfxInstance = PoolManager.Spawn(vfxPrefab, anchor.position, Quaternion.identity, anchor);
            }

            activeEffects.Add(effect);
        }

        private void Update()
        {
            if (activeEffects.Count == 0)
                return;

            if (damageable == null)
            {
                damageable = GetComponent<IDamageable>();
                if (damageable == null)
                {
                    activeEffects.Clear();
                    return;
                }
            }

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveEffect effect = activeEffects[i];
                effect.remainingDuration -= Time.deltaTime;

                if (effect.remainingDuration <= 0f)
                {
                    if (effect.vfxInstance != null)
                        PoolManager.Release(effect.vfxInstance);

                    activeEffects.RemoveAt(i);
                    continue;
                }

                if (Time.time < effect.nextTickTime)
                    continue;

                effect.nextTickTime = Time.time + effect.tickInterval;
                if (effect.damagePerTick > 0f)
                    damageable.TakeDamage(effect.damagePerTick, effect.source, false);
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i].vfxInstance != null)
                    PoolManager.Release(activeEffects[i].vfxInstance);
            }

            activeEffects.Clear();
        }
    }
}
