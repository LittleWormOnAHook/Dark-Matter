using Project.Core;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Single shared fire point for the player, companions, and enemies. Spawns muzzle flash on
    /// every shot, then either resolves an instant hitscan beam (ammoItem.isHitscanBeam, e.g.
    /// lasers) or launches a traveling CombatProjectile — so one ammo asset drives consistent
    /// behavior no matter who pulled the trigger. Projectiles, muzzle flash, and impact VFX are all
    /// pooled via PoolManager instead of raw Instantiate/Destroy (ranged combat can fire many shots
    /// per second, so this is the highest-value pooling target in the project).
    /// </summary>
    public static class CombatProjectileSpawner
    {
        private const string DefaultProjectilePath = "Assets/_Project/Prefabs/Combat/Projectiles/DefaultBullet.prefab";

        public static CombatProjectile Spawn(
            GameObject owner,
            Transform muzzle,
            ItemData weapon,
            ItemData ammoItem,
            Vector3 direction,
            float spreadDegrees,
            float damageOverride = 0f)
        {
            if (owner == null || muzzle == null || weapon == null)
                return null;

            ammoItem = CombatVfxUtility.ResolveAmmoItem(weapon, ammoItem);
            Vector3 fireDirection = ApplySpread(direction, spreadDegrees);
            CombatHitResolver.SpawnMuzzleFlash(ammoItem, weapon, muzzle);
            CombatHitResolver.PlayFireSound(ammoItem, weapon, muzzle);

            float damage = ResolveShotDamage(weapon, ammoItem, damageOverride);

            if (ammoItem != null && ammoItem.isHitscanBeam)
            {
                ResolveHitscanBeam(owner, muzzle, weapon, ammoItem, fireDirection, damage);
                return null;
            }

            GameObject prefab = ResolveProjectilePrefab(weapon, ammoItem);
            if (prefab == null)
                return null;

            GameObject instance = PoolManager.Spawn(prefab, muzzle.position, Quaternion.LookRotation(fireDirection, Vector3.up));
            CombatProjectile projectile = instance.GetComponent<CombatProjectile>();
            if (projectile == null)
                projectile = instance.AddComponent<CombatProjectile>();
            AmmoType ammoType = ammoItem != null ? ammoItem.ammoType : weapon.defaultAmmoType;
            float speed = ammoItem != null && ammoItem.projectileSpeed > 0f
                ? ammoItem.projectileSpeed
                : weapon.projectileSpeed;

            projectile.Launch(owner, fireDirection, damage, ammoType, ammoItem, speed, weaponItemData: weapon);
            return projectile;
        }

        /// <summary>
        /// Instant straight-line resolution for beam-style ammo (lasers): no travel time, damage
        /// and status effects land the same frame the shot is fired.
        /// </summary>
        private static float ResolveShotDamage(ItemData weapon, ItemData ammoItem, float damageOverride)
        {
            if (damageOverride > 0f)
                return damageOverride;

            return ammoItem != null ? ammoItem.RollRangedDamage() : weapon.RollRangedDamage();
        }

        private static void ResolveHitscanBeam(
            GameObject owner,
            Transform muzzle,
            ItemData weapon,
            ItemData ammoItem,
            Vector3 direction,
            float damage)
        {
            float range = ammoItem.rangedRange > 0f ? ammoItem.rangedRange : weapon.rangedRange;
            Vector3 origin = muzzle.position;
            Vector3 endPoint = origin + direction * range;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore) &&
                !CombatHitResolver.IsOwnerCollider(owner, hit.collider))
            {
                endPoint = hit.point;
                CombatHitResolver.ApplyDirectHit(hit.collider, hit.point, direction, damage, false, owner);

                if (ammoItem.HasSplashDamage)
                    CombatHitResolver.ApplySplash(ammoItem, hit.point, damage, owner, hit.collider);

                CombatHitResolver.SpawnImpactVfx(ammoItem, weapon, hit.point, hit.normal);
                CombatStatusEffect.Apply(ammoItem, hit.collider.gameObject, owner);
            }
        }

        private static Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
        {
            if (spreadDegrees <= 0.01f || direction.sqrMagnitude < 0.0001f)
                return direction.normalized;

            Vector3 forward = direction.normalized;
            Vector2 disk = Random.insideUnitCircle * spreadDegrees;
            Quaternion spread = Quaternion.Euler(disk.y, disk.x, 0f);
            return (spread * forward).normalized;
        }

        private static GameObject ResolveProjectilePrefab(ItemData weapon, ItemData ammoItem)
        {
            if (ammoItem != null && ammoItem.projectilePrefab != null)
                return ammoItem.projectilePrefab;

            if (weapon.projectilePrefab != null)
                return weapon.projectilePrefab;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultProjectilePath);
#else
            return Resources.Load<GameObject>("Combat/DefaultBullet");
#endif
        }
    }
}
