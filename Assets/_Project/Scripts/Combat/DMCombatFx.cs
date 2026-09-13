using System.Collections.Generic;
using Project.AI;
using Project.Companions;
using Project.Core;
using Project.Crafting;
using Project.Creatures;
using Project.Data;
using Project.Echoes;
using Project.Interaction;
using Project.PPT;
using Project.Quests;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Single runtime door for ammo combat FX. Profiles in
    /// Assets/_Project/Data/Items/Ammo own the prefabs; this class only spawns.
    /// </summary>
    public static class DMCombatFx
    {
        private const float DecalLifeSeconds = 16f;
        private const float HitEffectLifeSeconds = 4f;
        private const float MuzzleLifeSeconds = 2f;

        private static Dictionary<AmmoType, DMAmmoFxProfile> profileByType;

        public static DMAmmoFxProfile ResolveProfile(ItemData ammoItem, ItemData weapon)
        {
            ItemData ammo = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            if (ammo is DMAmmoFxProfile ammoProfile)
                return ammoProfile;
            if (ammo != null && ammo.fxProfile != null)
                return ammo.fxProfile;

            if (weapon != null && weapon.defaultAmmoItem is DMAmmoFxProfile weaponProfile)
            {
                if (ammo == null || weaponProfile.ammoType == ammo.ammoType)
                    return weaponProfile;
            }

            if (weapon != null && weapon.defaultAmmoItem != null && weapon.defaultAmmoItem.fxProfile != null)
            {
                if (ammo == null || weapon.defaultAmmoItem.ammoType == ammo.ammoType)
                    return weapon.defaultAmmoItem.fxProfile;
            }

            AmmoType type = ammo != null
                ? ammo.ammoType
                : (weapon != null ? weapon.defaultAmmoType : AmmoType.Gunpowder);
            return FindProfileForType(type);
        }

        public static GameObject ResolveMuzzleFlash(ItemData ammoItem, ItemData weapon)
        {
            DMAmmoFxProfile profile = ResolveProfile(ammoItem, weapon);
            if (profile != null)
                return profile.muzzleFlashPrefab;
            ItemData ammo = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            return ammo != null ? ammo.muzzleFlashPrefab : null;
        }

        public static GameObject ResolveTracer(ItemData ammoItem, ItemData weapon)
        {
            DMAmmoFxProfile profile = ResolveProfile(ammoItem, weapon);
            if (profile != null)
                return profile.tracerPrefab;
            ItemData ammo = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            return ammo != null ? ammo.tracerPrefab : null;
        }

        public static GameObject ResolveBeam(ItemData ammoItem, ItemData weapon)
        {
            DMAmmoFxProfile profile = ResolveProfile(ammoItem, weapon);
            if (profile != null)
                return profile.beamVfxPrefab;
            ItemData ammo = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            return ammo != null ? ammo.beamVfxPrefab : null;
        }

        public static GameObject ResolveProjectile(ItemData ammoItem, ItemData weapon)
        {
            DMAmmoFxProfile profile = ResolveProfile(ammoItem, weapon);
            if (profile != null)
                return profile.projectilePrefab;
            ItemData ammo = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            return ammo != null ? ammo.projectilePrefab : null;
        }

        public static GameObject ResolveDefaultImpact(ItemData ammoItem, ItemData weapon)
        {
            DMAmmoFxProfile profile = ResolveProfile(ammoItem, weapon);
            if (profile != null)
                return profile.impactVfxPrefab;
            ItemData ammo = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            return ammo != null ? ammo.impactVfxPrefab : null;
        }

        private static DMAmmoFxProfile FindProfileForType(AmmoType type)
        {
            if (profileByType != null && profileByType.TryGetValue(type, out DMAmmoFxProfile cached) && cached != null)
                return cached;

            ItemData[] items = ItemRegistry.GetAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null || !item.CountsAsAmmo || item.ammoType != type)
                    continue;

                DMAmmoFxProfile found = item as DMAmmoFxProfile ?? item.fxProfile;
                if (found == null)
                    continue;

                if (profileByType == null)
                    profileByType = new Dictionary<AmmoType, DMAmmoFxProfile>();
                profileByType[type] = found;
                return found;
            }

            return null;
        }

        public static void PlayMuzzle(ItemData ammoItem, ItemData weapon, Transform muzzle)
        {
            GameObject prefab = ResolveMuzzleFlash(ammoItem, weapon);
            if (prefab == null || muzzle == null)
                return;

            // World-space spawn — parenting to the muzzle socket leaves (Clone) clutter on the weapon.
            GameObject instance = PoolManager.Spawn(prefab, muzzle.position, muzzle.rotation);
            CombatVfxUtility.PreparePooledOneShotVfx(instance, MuzzleLifeSeconds);
        }

        public static void PlayWorldImpact(
            ItemData ammoItem,
            ItemData weapon,
            Vector3 point,
            Vector3 normal,
            GameObject receiver,
            GameObject impactVfxOverride = null)
        {
            DMAmmoFxProfile profile = ResolveProfile(ammoItem, weapon);
            bool spawnedMark = false;
            bool splash = HasSplashImpact(ammoItem, profile);

            Transform attach = CombatVfxUtility.ResolveImpactAttachTransform(receiver);

            if (impactVfxOverride != null)
            {
                SpawnPooled(impactVfxOverride, point, LookAlongNormal(normal), attach, HitEffectLifeSeconds);
                spawnedMark = true;
            }
            else if (profile != null && profile.useHitMarks)
            {
                spawnedMark = TrySpawnSurfaceMark(profile, point, normal, receiver, spawnHitEffects: !splash);
            }

            if (!spawnedMark || splash)
            {
                GameObject impact = impactVfxOverride != null
                    ? null
                    : ResolveDefaultImpact(ammoItem, weapon);
                if (impact != null)
                    SpawnPooled(impact, point, LookAlongNormal(normal), attach, HitEffectLifeSeconds);
            }

            bool skipDecal = ShouldSkipImpactDecal(receiver);
            bool burn = !skipDecal && (profile != null
                ? profile.spawnLaserBurn
                : DMILaserBurnMarkSpawner.ShouldSpawnForLaserAmmo(ammoItem, weapon));
            if (burn)
                DMILaserBurnMarkSpawner.Spawn(point, normal, attach);
        }

        /// <summary>
        /// Bullet holes / laser burns stay on world surfaces only — not characters or pickups.
        /// </summary>
        private static bool ShouldSkipImpactDecal(GameObject receiver)
        {
            if (receiver == null)
                return false;

            if (receiver.GetComponentInParent<EnemyHealth>() != null
                || receiver.GetComponentInParent<DMICreatureHealth>() != null
                || receiver.GetComponentInParent<CompanionHealth>() != null
                || receiver.GetComponentInParent<PioneerCompanionAgent>() != null
                || receiver.GetComponentInParent<ItemPickup>() != null
                || receiver.GetComponentInParent<RecipePickup>() != null
                || receiver.GetComponentInParent<QuestGiverNpc>() != null
                || receiver.GetComponentInParent<PptNpcInteractor>() != null
                || receiver.GetComponentInParent<EchoWorldEntity>() != null)
                return true;

            Transform t = receiver.transform;
            while (t != null)
            {
                if (t.CompareTag("Enemy") || t.CompareTag("CompanionAI"))
                    return true;
                t = t.parent;
            }

            return false;
        }

        private static bool HasSplashImpact(ItemData ammoItem, DMAmmoFxProfile profile)
        {
            if (ammoItem != null && ammoItem.HasSplashDamage)
                return true;
            return profile != null && profile.HasSplashDamage;
        }

        private static bool TrySpawnSurfaceMark(
            DMAmmoFxProfile profile,
            Vector3 point,
            Vector3 normal,
            GameObject receiver,
            bool spawnHitEffects = true)
        {
            string tag = receiver != null ? receiver.tag : "Untagged";
            if (!profile.TryResolveHitMark(tag, out GameObject decal, out GameObject hitEffect))
                return false;

            Quaternion rotation = LookAlongNormal(normal);
            if (decal != null && !ShouldSkipImpactDecal(receiver))
            {
                GameObject instance = SpawnPooled(decal, point, rotation, receiver != null ? receiver.transform : null, DecalLifeSeconds);
                if (instance != null)
                    instance.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
            }

            Transform attach = CombatVfxUtility.ResolveImpactAttachTransform(receiver);
            if (spawnHitEffects && hitEffect != null)
                SpawnPooled(hitEffect, point, rotation, attach, HitEffectLifeSeconds);

            bool spawnedDecal = decal != null && !ShouldSkipImpactDecal(receiver);
            return spawnedDecal || (spawnHitEffects && hitEffect != null);
        }

        private static GameObject SpawnPooled(
            GameObject prefab,
            Vector3 point,
            Quaternion rotation,
            Transform parent,
            float life)
        {
            GameObject instance = PoolManager.Spawn(prefab, point, rotation, parent);
            if (instance == null)
                return null;

            if (parent != null)
                instance.transform.SetPositionAndRotation(point, rotation);

            CombatVfxUtility.PreparePooledOneShotVfx(instance, life);
            return instance;
        }

        /// <summary>Stick a flying tracer / projectile visual at the impact point (used by <see cref="CombatProjectile"/>).</summary>
        public static void StickTracerAtImpact(
            GameObject tracer,
            Vector3 point,
            Vector3 normal,
            Transform attach,
            float lifeSeconds = HitEffectLifeSeconds)
        {
            if (tracer == null)
                return;

            Quaternion rotation = LookAlongNormal(normal);
            CombatVfxUtility.StickPooledVfxAtImpact(tracer, point, rotation, attach, lifeSeconds, freezeProjectileVisual: true);
        }

        private static Quaternion LookAlongNormal(Vector3 normal)
        {
            return normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal, Vector3.up)
                : Quaternion.identity;
        }
    }
}
