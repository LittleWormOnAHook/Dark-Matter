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
            if (target == null || ammoItem == null)
                return;

            ApplyResonanceStabilizer(ammoItem.ammoType, target, source);

            if (!ammoItem.HasStatusEffect)
                return;

            IDamageable damageable = DamageableUtility.GetDamageable(target.GetComponent<Collider>());
            MonoBehaviour damageableBehaviour = damageable as MonoBehaviour;
            if (damageableBehaviour == null)
                return;

            GameObject targetRoot = damageableBehaviour.gameObject;

            CombatStatusEffectController.Apply(
                targetRoot,
                ammoItem.ResolveStatusEffect(),
                ammoItem.statusEffectDamagePerTick,
                ammoItem.statusEffectTickInterval,
                ammoItem.statusEffectDuration,
                source,
                ammoItem.statusEffectVfxPrefab);
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
