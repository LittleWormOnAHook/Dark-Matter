using System.Collections.Generic;
using Project.Core;
using Project.Data;
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

            GameObject instance = PoolManager.Spawn(prefab, muzzle.position, muzzle.rotation, muzzle);
            CombatVfxUtility.PlayParticleSystemsRecursive(instance);
            PoolManager.ReleaseDelayed(instance, MuzzleLifeSeconds);
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

            if (impactVfxOverride != null)
            {
                SpawnPooled(impactVfxOverride, point, LookAlongNormal(normal), null, HitEffectLifeSeconds);
                spawnedMark = true;
            }
            else if (profile != null && profile.useHitMarks)
            {
                spawnedMark = TrySpawnSurfaceMark(profile, point, normal, receiver);
            }

            if (!spawnedMark)
            {
                GameObject impact = ResolveDefaultImpact(ammoItem, weapon);
                if (impact != null)
                    SpawnPooled(impact, point, LookAlongNormal(normal), null, HitEffectLifeSeconds);
            }

            bool burn = profile != null
                ? profile.spawnLaserBurn
                : DMILaserBurnMarkSpawner.ShouldSpawnForLaserAmmo(ammoItem, weapon);
            if (burn)
                DMILaserBurnMarkSpawner.Spawn(point, normal);
        }

        private static bool TrySpawnSurfaceMark(
            DMAmmoFxProfile profile,
            Vector3 point,
            Vector3 normal,
            GameObject receiver)
        {
            string tag = receiver != null ? receiver.tag : "Untagged";
            if (!profile.TryResolveHitMark(tag, out GameObject decal, out GameObject hitEffect))
                return false;

            Quaternion rotation = LookAlongNormal(normal);
            if (decal != null)
            {
                GameObject instance = SpawnPooled(decal, point, rotation, receiver != null ? receiver.transform : null, DecalLifeSeconds);
                if (instance != null)
                    instance.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
            }

            if (hitEffect != null)
                SpawnPooled(hitEffect, point, rotation, null, HitEffectLifeSeconds);

            return decal != null || hitEffect != null;
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

            CombatVfxUtility.PlayParticleSystemsRecursive(instance);
            PoolManager.ReleaseDelayed(instance, life);
            return instance;
        }

        private static Quaternion LookAlongNormal(Vector3 normal)
        {
            return normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal, Vector3.up)
                : Quaternion.identity;
        }
    }
}
