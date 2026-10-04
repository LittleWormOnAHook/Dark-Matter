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
    /// health class needing its own DoT bookkeeping.
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
            public Transform vfxFollow;
            public int stacks;
        }

        private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>(4);
        private readonly Dictionary<StatusEffectType, float> immunityUntil = new Dictionary<StatusEffectType, float>(4);
        private IDamageable damageable;

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
            if (IsImmune(type))
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            int maxStacks = profile != null ? profile.statusMaxStacks : 3;
            duration *= GetBossDurationMultiplier();

            for (int i = 0; i < activeEffects.Count; i++)
            {
                ActiveEffect existing = activeEffects[i];
                if (existing.type != type)
                    continue;

                existing.stacks = Mathf.Min(maxStacks, existing.stacks + 1);
                existing.remainingDuration = duration;
                existing.damagePerTick = damagePerTick;
                existing.tickInterval = tickInterval;
                existing.source = source;
                existing.vfxFollow = transform;
                CombatEvents.RaiseStatusApplied(default, type, gameObject);
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
                vfxFollow = transform,
                stacks = 1
            };

            if (vfxPrefab != null)
            {
                effect.vfxInstance = PoolManager.Spawn(
                    vfxPrefab,
                    transform.position,
                    Quaternion.identity,
                    null);
            }

            activeEffects.Add(effect);
            CombatEvents.RaiseStatusApplied(default, type, gameObject);
        }

        private bool IsImmune(StatusEffectType type)
        {
            if (!immunityUntil.TryGetValue(type, out float until))
                return false;

            if (Time.time >= until)
            {
                immunityUntil.Remove(type);
                return false;
            }

            return true;
        }

        private float GetBossDurationMultiplier()
        {
            CombatBossStatusModifier boss = GetComponent<CombatBossStatusModifier>();
            if (boss == null)
                boss = GetComponentInParent<CombatBossStatusModifier>();

            if (boss == null)
                return 1f;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float global = profile != null ? profile.statusBossMultiplier : 0.5f;
            return Mathf.Clamp(boss.StatusDurationMultiplier * global, 0.05f, 1f);
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
                    ReleaseAllVfx();
                    activeEffects.Clear();
                    return;
                }
            }

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float immunityWindow = profile != null ? profile.statusImmunityWindowSeconds : 2f;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveEffect effect = activeEffects[i];
                effect.remainingDuration -= Time.deltaTime;

                if (effect.vfxInstance != null && effect.vfxFollow != null)
                    effect.vfxInstance.transform.position = effect.vfxFollow.position;

                if (effect.remainingDuration <= 0f)
                {
                    immunityUntil[effect.type] = Time.time + immunityWindow;
                    ReleaseVfx(effect);
                    activeEffects.RemoveAt(i);
                    continue;
                }

                if (Time.time < effect.nextTickTime)
                    continue;

                effect.nextTickTime = Time.time + effect.tickInterval;
                if (effect.damagePerTick > 0f)
                    damageable.TakeDamage(effect.damagePerTick * effect.stacks, effect.source, false);
            }
        }

        private void OnDisable()
        {
            ReleaseAllVfx();
            activeEffects.Clear();
        }

        private void ReleaseAllVfx()
        {
            for (int i = 0; i < activeEffects.Count; i++)
                ReleaseVfx(activeEffects[i]);
        }

        private static void ReleaseVfx(ActiveEffect effect)
        {
            if (effect == null || effect.vfxInstance == null)
                return;

            GameObject vfx = effect.vfxInstance;
            effect.vfxInstance = null;
            effect.vfxFollow = null;

            if (vfx.transform.parent != null)
                vfx.transform.SetParent(null, true);

            PoolManager.ReleaseDelayed(vfx, 0f);
        }
    }
}
