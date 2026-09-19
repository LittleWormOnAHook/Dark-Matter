using Project.Data;
using Project.Interaction;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Applies on-hit status effects from elemental ammo. Generic damage-over-time ticking
    /// (Burning/Frozen/Shocked/Corroded/etc.) routes through CombatStatusEffectController so it
    /// works identically for the player, companions, and enemies; ResonanceStabilizer keeps its
    /// existing bespoke EchoStabilizeReceiver hook since that's a puzzle/echo mechanic rather than
    /// straightforward damage-over-time.
    /// </summary>
    public static class CombatStatusEffect
    {
        /// <summary>Legacy entry point kept for callers that only have the raw ammo type.</summary>
        public static void Apply(AmmoType ammoType, GameObject target, GameObject source)
        {
            ApplyResonanceStabilizer(ammoType, target, source);
        }

        /// <summary>
        /// Preferred entry point: reads tick damage/interval/duration/VFX straight off the ammo
        /// ItemData so every projectile using that ammo behaves consistently.
        /// </summary>
        public static void Apply(ItemData ammoItem, GameObject target, GameObject source)
        {
            Apply(ammoItem, target, source, 1f, false, 0f);
        }

        /// <param name="durationScale">Ordinance Hot Residue / Persistent Hazard duration multiplier.</param>
        /// <param name="forceBurningIfNone">Hot Residue: apply a short Burning residue when the ammo has no DOT.</param>
        /// <param name="residueTickFallback">Tick damage used when forcing Burning and the ammo has no tick value.</param>
        public static void Apply(
            ItemData ammoItem,
            GameObject target,
            GameObject source,
            float durationScale,
            bool forceBurningIfNone,
            float residueTickFallback)
        {
            if (target == null || ammoItem == null)
                return;

            ApplyResonanceStabilizer(ammoItem.ammoType, target, source);

            StatusEffectType type = ammoItem.ResolveStatusEffect();
            float duration = ammoItem.statusEffectDuration;
            float tick = ammoItem.statusEffectDamagePerTick;
            float interval = ammoItem.statusEffectTickInterval;
            GameObject vfx = ammoItem.statusEffectVfxPrefab;

            if (type == StatusEffectType.None || duration <= 0f)
            {
                if (!forceBurningIfNone)
                    return;

                type = StatusEffectType.Burning;
                duration = duration > 0f ? duration : 3f;
                tick = tick > 0f ? tick : Mathf.Max(3f, residueTickFallback);
                interval = interval > 0.05f ? interval : 0.75f;
                vfx = null;
            }

            Collider targetCollider = target.GetComponent<Collider>();
            if (targetCollider == null)
                targetCollider = target.GetComponentInParent<Collider>();
            if (targetCollider == null)
                targetCollider = target.GetComponentInChildren<Collider>();

            IDamageable damageable = DamageableUtility.GetDamageable(targetCollider);
            MonoBehaviour damageableBehaviour = damageable as MonoBehaviour;
            if (damageableBehaviour == null)
                return;

            CombatStatusEffectController.Apply(
                damageableBehaviour.gameObject,
                type,
                tick,
                interval,
                duration * Mathf.Max(0.05f, durationScale),
                source,
                vfx);
        }

        private static void ApplyResonanceStabilizer(AmmoType ammoType, GameObject target, GameObject source)
        {
            if (target == null || ammoType != AmmoType.ResonanceStabilizer)
                return;

            EchoStabilizeReceiver receiver = target.GetComponentInParent<EchoStabilizeReceiver>();
            if (receiver != null)
                receiver.TryApplyStabilization(source, 0.22f);
        }
    }
}
