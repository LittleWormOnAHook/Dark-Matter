using Project.AI;
using Project.Companions;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Survival;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Shared on-hit resolution (direct damage, splash/AoE, elemental status effects, impact VFX)
    /// used by both the traveling CombatProjectile and the instant hitscan beam path in
    /// CombatProjectileSpawner, so ammo behaves identically regardless of travel mode.
    /// </summary>
    public static class CombatHitResolver
    {
        public static bool IsOwnerCollider(GameObject owner, Collider collider)
        {
            if (collider == null || owner == null)
                return false;

            return collider.transform.IsChildOf(owner.transform) || collider.gameObject == owner;
        }

        /// <summary>Applies direct damage to whatever the collider resolves to, plus VFX/UI feedback.</summary>
        public static void ApplyDirectHit(
            Collider collider,
            Vector3 hitPoint,
            Vector3 travelDirection,
            float damage,
            bool isCritical,
            GameObject owner)
        {
            IDamageable damageable = DamageableUtility.GetDamageable(collider);
            if (damageable == null)
                return;

            GameObject damageSource = owner != null ? owner : collider.gameObject;
            damageable.TakeDamage(damage, damageSource, isCritical);
            NotifyEnemyProjectileHitOnAlly(damageSource, damageable, collider.transform, damage);

            Vector3 normal = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.up;
            CombatHitVfx.SpawnBloodSplatter(hitPoint, travelDirection, normal, damage);

            // EnemyHealth/CompanionHealth already show their own floating damage number inside
            // TakeDamage (shared with the melee hit path) — showing it again here would double the
            // popup on every ranged hit. Targets that don't self-report (player SurvivalStats,
            // ResourceNode, etc.) still need us to show it.
            if (!SelfReportsDamageUi(damageable))
                CombatUiSpawner.ShowDamage(damage, hitPoint, isCritical);
        }

        private static bool SelfReportsDamageUi(IDamageable damageable)
        {
            return damageable is EnemyHealth || damageable is CompanionHealth;
        }

        /// <summary>
        /// When an enemy-owned projectile/beam hits the player or a pioneer, raise squad alert events
        /// and incoming-hit VFX. CompanionHealth already raises OnCompanionAttackedBy inside TakeDamage;
        /// the player path must be handled here because SurvivalStats does not.
        /// </summary>
        private static void NotifyEnemyProjectileHitOnAlly(
            GameObject owner,
            IDamageable damageable,
            Transform hitTransform,
            float damage)
        {
            if (owner == null || damageable == null || damage <= 0f)
                return;

            EnemyHealth attacker = owner.GetComponentInParent<EnemyHealth>();
            if (attacker == null || attacker.IsDead)
                return;

            Transform receiver = hitTransform != null ? hitTransform.root : null;

            if (damageable is SurvivalStats)
            {
                PlayerCombatEvents.RaisePlayerAttackedBy(attacker);
                if (receiver != null)
                    CombatHitVfx.SpawnIncomingEnemyHit(attacker.transform, receiver, damage);
                return;
            }

            if (damageable is CompanionHealth && receiver != null)
                CombatHitVfx.SpawnIncomingEnemyHit(attacker.transform, receiver, damage);
        }

        /// <summary>Splash/AoE damage around the impact point, falling off linearly to ammoItem.splashDamageFalloff at the edge.</summary>
        public static void ApplySplash(
            ItemData ammoItem,
            Vector3 center,
            float centerDamage,
            GameObject owner,
            Collider excludeCollider)
        {
            if (ammoItem == null || !ammoItem.HasSplashDamage)
                return;

            Collider[] hits = Physics.OverlapSphere(center, ammoItem.splashRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i];
                if (hitCollider == null || hitCollider == excludeCollider || IsOwnerCollider(owner, hitCollider))
                    continue;

                IDamageable damageable = DamageableUtility.GetDamageable(hitCollider);
                if (damageable == null)
                    continue;

                Vector3 closest = hitCollider.ClosestPoint(center);
                float distance = Vector3.Distance(center, closest);
                float t = Mathf.Clamp01(distance / Mathf.Max(0.01f, ammoItem.splashRadius));
                float falloffDamage = Mathf.Lerp(centerDamage, centerDamage * ammoItem.splashDamageFalloff, t);
                if (falloffDamage <= 0.01f)
                    continue;

                damageable.TakeDamage(falloffDamage, owner, false);
                if (!SelfReportsDamageUi(damageable))
                    CombatUiSpawner.ShowDamage(falloffDamage, closest, false);
                ApplyStatusEffect(ammoItem, hitCollider, owner);
            }
        }

        /// <summary>Applies the ammo's elemental status effect (Burning/Frozen/Shocked/etc.) to whatever the collider resolves to.</summary>
        public static void ApplyStatusEffect(ItemData ammoItem, Collider collider, GameObject owner)
        {
            if (ammoItem == null || collider == null || !ammoItem.HasStatusEffect)
                return;

            CombatStatusEffect.Apply(ammoItem, collider.gameObject, owner);
        }

        public static void SpawnImpactVfx(ItemData ammoItem, ItemData weapon, Vector3 point, Vector3 normal)
        {
            GameObject prefab = ammoItem != null && ammoItem.impactVfxPrefab != null
                ? ammoItem.impactVfxPrefab
                : weapon != null ? weapon.impactVfxPrefab : null;

            if (prefab == null)
                return;

            Quaternion rotation = normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal, Vector3.up)
                : Quaternion.identity;

            GameObject instance = PoolManager.Spawn(prefab, point, rotation);
            // Reactivating a pooled instance doesn't reliably re-trigger playOnAwake particle
            // systems on every Unity version, so explicitly clear+replay them on every reuse.
            CombatVfxUtility.PlayParticleSystemsRecursive(instance);
            PoolManager.ReleaseDelayed(instance, 4f);
        }

        public static void SpawnMuzzleFlash(ItemData ammoItem, ItemData weapon, Transform muzzle)
        {
            GameObject prefab = ammoItem != null && ammoItem.muzzleFlashPrefab != null
                ? ammoItem.muzzleFlashPrefab
                : weapon != null ? weapon.muzzleFlashPrefab : null;

            if (prefab == null || muzzle == null)
                return;

            GameObject instance = PoolManager.Spawn(prefab, muzzle.position, muzzle.rotation, muzzle);
            CombatVfxUtility.PlayParticleSystemsRecursive(instance);
            PoolManager.ReleaseDelayed(instance, 2f);
        }

        /// <summary>One-shot firing sound played at the muzzle position. Falls back to the weapon's own fire sound if the loaded ammo doesn't specify one.</summary>
        public static void PlayFireSound(ItemData ammoItem, ItemData weapon, Transform muzzle)
        {
            AudioClip clip = ammoItem != null && ammoItem.fireSound != null
                ? ammoItem.fireSound
                : weapon != null ? weapon.fireSound : null;

            if (clip == null || muzzle == null)
                return;

            AudioSource.PlayClipAtPoint(clip, muzzle.position);
        }
    }
}
