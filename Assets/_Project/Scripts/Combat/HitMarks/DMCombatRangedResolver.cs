using Project.AI;
using Project.Combat.Experiment.Carve;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Single ranged hit path for all nine ammo types: shared layer mask (includes DMHitbox),
    /// aim raycast merge, direct damage + enemy body marks, splash, status, and world impact.
    /// </summary>
    public static class DMCombatRangedResolver
    {
        public static int SharedLayerMask => DMEnemyHitQuery.SharedRangedLayerMask;

        public static DMAmmoDeliveryMode ResolveDelivery(ItemData ammoItem, ItemData weapon)
        {
            DMAmmoFxProfile profile = DMCombatFx.ResolveProfile(ammoItem, weapon);
            if (profile != null)
                return profile.ResolveDelivery();

            ItemData ammo = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            if (ammo != null && ammo.isHitscanBeam)
                return ammo.beamVfxPrefab != null ? DMAmmoDeliveryMode.Beam : DMAmmoDeliveryMode.Hitscan;

            return DMAmmoDeliveryMode.Projectile;
        }

        public static bool TryAimRaycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            GameObject owner,
            out RaycastHit hit,
            float startSkin = 0f,
            float minHitDistance = 0f,
            ItemData ammoItem = null)
        {
            int mask = SharedLayerMask;
            if (DMCarveExperimentCombatUtility.UsesAllPhysicsLayers(ammoItem))
                mask = DMCarveExperimentCombatUtility.AllLayersExceptPlayer;

            return RangedFireSolver.TryRaycastAim(
                origin,
                direction,
                maxDistance,
                mask,
                owner,
                out hit,
                startSkin,
                minHitDistance);
        }

        public static void ApplyHitOutcome(
            Collider collider,
            Vector3 hitPoint,
            Vector3 surfaceNormal,
            Vector3 travelDirection,
            float damage,
            bool isCritical,
            GameObject owner,
            ItemData ammoItem,
            ItemData weapon,
            bool playImpactAudio = true,
            GameObject impactVfxOverride = null)
        {
            if (collider == null)
                return;

            Vector3 normal = surfaceNormal.sqrMagnitude > 0.0001f
                ? surfaceNormal
                : (travelDirection.sqrMagnitude > 0.0001f ? -travelDirection.normalized : Vector3.up);
            ApplyHitOutcomeInternal(
                collider,
                hitPoint,
                normal,
                travelDirection,
                damage,
                isCritical,
                owner,
                ammoItem,
                weapon,
                playImpactAudio,
                impactVfxOverride);
        }

        /// <summary>
        /// After a confirmed ranged hit: damage (with enemy marks), optional splash, world FX on non-enemies only.
        /// </summary>
        public static void ApplyHitOutcome(
            RaycastHit hit,
            Vector3 travelDirection,
            float damage,
            bool isCritical,
            GameObject owner,
            ItemData ammoItem,
            ItemData weapon,
            bool playImpactAudio = true,
            GameObject impactVfxOverride = null)
        {
            if (hit.collider == null)
                return;

            ApplyHitOutcomeInternal(
                hit.collider,
                hit.point,
                hit.normal,
                travelDirection,
                damage,
                isCritical,
                owner,
                ammoItem,
                weapon,
                playImpactAudio,
                impactVfxOverride);
        }

        private static void ApplyHitOutcomeInternal(
            Collider collider,
            Vector3 hitPoint,
            Vector3 surfaceNormal,
            Vector3 travelDirection,
            float damage,
            bool isCritical,
            GameObject owner,
            ItemData ammoItem,
            ItemData weapon,
            bool playImpactAudio,
            GameObject impactVfxOverride)
        {
            Vector3 fxTravel = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.zero;
            CombatHitResolver.ApplyDirectHit(
                collider,
                hitPoint,
                fxTravel,
                damage,
                isCritical,
                owner,
                ammoItem,
                surfaceNormal: surfaceNormal,
                fxTravelDirection: fxTravel,
                fxAmmoItem: ammoItem,
                fxWeapon: weapon,
                rangedHitMarks: true);

            CombatHitResolver.ApplyStatusEffect(
                ammoItem,
                collider,
                owner,
                1f,
                false,
                0f,
                hitPoint,
                surfaceNormal,
                fxTravel);

            if (ammoItem != null && ammoItem.HasSplashDamage)
                CombatHitResolver.ApplySplash(ammoItem, hitPoint, damage, owner, collider);

            DMCombatCameraShake.TryPlayAmmoImpact(owner, ammoItem, weapon, hitPoint);
            if (playImpactAudio)
                DMVendorImpactAudio.Play(ammoItem, weapon, hitPoint); // 24n: vendor explosion audio every hit

            GameObject receiver = collider.gameObject;
            if (DMPlayerHitboxRig.IsPlayerBodyHitbox(receiver) && !DMCarveExperimentCombatUtility.BypassesWorldImpactCharacterBlocks(ammoItem))
            {
                // Player body hit: marks only (no world impact FX). Keep the hit sound and combat-impact noise.
                if (playImpactAudio)
                    CombatHitResolver.PlayImpactHitAudio(hitPoint);
                EnemyNoiseEvents.RaiseCombatImpactNoise(hitPoint, CombatHitResolver.DefaultImpactNoiseRadius, owner);
                return;
            }

            if (receiver != null && receiver.GetComponentInParent<EnemyHealth>() != null)
            {
                if (playImpactAudio)
                    CombatHitResolver.PlayImpactHitAudio(hitPoint, isCritical);
                return;
            }

            if (!ShouldPlayWorldImpact(receiver, ammoItem))
                return;

            Vector3 impactNormal = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal : -fxTravel;
            CombatHitResolver.HandleRangedWorldImpact(
                ammoItem,
                weapon,
                hitPoint,
                impactNormal,
                owner,
                receiver,
                playHitAudio: playImpactAudio,
                impactVfxOverride: impactVfxOverride);
        }

        public static bool ShouldPlayWorldImpact(GameObject receiver, ItemData ammoItem = null)
        {
            if (receiver == null)
                return true;

            if (DMCarveExperimentCombatUtility.BypassesWorldImpactCharacterBlocks(ammoItem))
                return true;

            return receiver.GetComponentInParent<EnemyHealth>() == null;
        }
    }
}
