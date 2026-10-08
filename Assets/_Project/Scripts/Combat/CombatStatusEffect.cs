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
            Apply(ammoItem, target, source, durationScale, forceBurningIfNone, residueTickFallback, null);
        }

        public static void Apply(
            ItemData ammoItem,
            GameObject target,
            GameObject source,
            float durationScale,
            bool forceBurningIfNone,
            float residueTickFallback,
            Collider hitCollider,
            Vector3 hitPoint = default,
            Vector3 surfaceNormal = default,
            Vector3 travelDirection = default)
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

            Collider targetCollider = hitCollider;
            if (targetCollider == null)
                targetCollider = target.GetComponent<Collider>();
            if (targetCollider == null)
                targetCollider = target.GetComponentInParent<Collider>();
            if (targetCollider == null)
                targetCollider = target.GetComponentInChildren<Collider>();

            IDamageable damageable = DamageableUtility.GetDamageable(targetCollider);
            MonoBehaviour damageableBehaviour = damageable as MonoBehaviour;
            if (damageableBehaviour == null)
                return;

            ResolveVfxAttach(
                targetCollider,
                damageableBehaviour.gameObject,
                hitPoint,
                surfaceNormal,
                travelDirection,
                out Transform attachParent,
                out Vector3 worldPoint,
                out Vector3 outwardNormal);

            CombatStatusEffectController.Apply(
                damageableBehaviour.gameObject,
                type,
                tick,
                interval,
                duration * Mathf.Max(0.05f, durationScale),
                source,
                vfx,
                attachParent,
                worldPoint,
                outwardNormal);
        }

        /// <summary>
        /// Same surface + bone rules as <see cref="HitMarks.DMEnemyHitMarks"/> burn embers: impact
        /// point/normal when available, else collider torso / humanoid chest fallback.
        /// </summary>
        internal static void ResolveVfxAttach(
            Collider hitCollider,
            GameObject damageableRoot,
            Vector3 hitPoint,
            Vector3 hintNormal,
            Vector3 travelDirection,
            out Transform attachParent,
            out Vector3 worldPoint,
            out Vector3 outwardNormal)
        {
            attachParent = damageableRoot != null ? damageableRoot.transform : null;
            outwardNormal = hintNormal.sqrMagnitude > 0.0001f ? hintNormal.normalized : Vector3.up;
            worldPoint = hitPoint;

            Collider shape = hitCollider;
            if (shape == null && damageableRoot != null)
                shape = damageableRoot.GetComponentInChildren<Collider>();

            bool hasHitPoint = hitPoint.sqrMagnitude > 0.0001f;
            if (hasHitPoint && shape != null)
            {
                DMEnemyHitQuery.ResolveMarkSurface(
                    shape,
                    hitPoint,
                    travelDirection,
                    hintNormal,
                    out worldPoint,
                    out outwardNormal);
            }
            else if (shape != null)
            {
                worldPoint = shape.bounds.center;
                outwardNormal = (worldPoint - shape.transform.position).normalized;
                if (outwardNormal.sqrMagnitude < 0.0001f)
                    outwardNormal = shape.transform.up.sqrMagnitude > 0.0001f ? shape.transform.up : Vector3.up;
            }
            else if (damageableRoot != null)
            {
                TryGetTorsoFallback(damageableRoot, out attachParent, out worldPoint, out outwardNormal);
            }

            if (shape != null && DMEnemyHitQuery.TryGetHitbox(shape, out DMEnemyHitbox hitbox) && hitbox.Bone != null)
                attachParent = hitbox.Bone;
            else if (shape != null)
                attachParent = shape.transform;
            else if (attachParent == null && damageableRoot != null)
                attachParent = damageableRoot.transform;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float offset = profile != null ? profile.statusVfxNormalOffset : 0.004f;
            worldPoint += outwardNormal * offset;
        }

        private static void TryGetTorsoFallback(
            GameObject root,
            out Transform attach,
            out Vector3 point,
            out Vector3 normal)
        {
            attach = root.transform;
            normal = root.transform.forward.sqrMagnitude > 0.0001f ? root.transform.forward : Vector3.forward;
            point = root.transform.position + Vector3.up * 1.05f;

            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman)
                return;

            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null)
                chest = animator.GetBoneTransform(HumanBodyBones.Spine);
            if (chest == null)
                chest = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (chest == null)
                return;

            attach = chest;
            point = chest.position;
            normal = chest.forward.sqrMagnitude > 0.0001f ? chest.forward : normal;
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
