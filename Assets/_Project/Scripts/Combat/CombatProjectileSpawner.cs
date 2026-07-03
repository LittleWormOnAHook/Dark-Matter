using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    public static class CombatProjectileSpawner
    {
        private const string DefaultProjectilePath = "Assets/_Project/Prefabs/Combat/Projectiles/DefaultBullet.prefab";

        public static CombatProjectile Spawn(
            GameObject owner,
            Transform muzzle,
            ItemData weapon,
            ItemData ammoItem,
            Vector3 direction,
            float spreadDegrees)
        {
            if (owner == null || muzzle == null || weapon == null)
                return null;

            GameObject prefab = ResolveProjectilePrefab(weapon, ammoItem);
            if (prefab == null)
                return null;

            Vector3 fireDirection = ApplySpread(direction, spreadDegrees);
            GameObject instance = Object.Instantiate(prefab, muzzle.position, Quaternion.LookRotation(fireDirection, Vector3.up));
            CombatProjectile projectile = instance.GetComponent<CombatProjectile>();
            if (projectile == null)
                projectile = instance.AddComponent<CombatProjectile>();

            float damage = ammoItem != null ? ammoItem.RollRangedDamage() : weapon.RollRangedDamage();
            AmmoType ammoType = ammoItem != null ? ammoItem.ammoType : weapon.defaultAmmoType;
            float speed = ammoItem != null && ammoItem.projectileSpeed > 0f
                ? ammoItem.projectileSpeed
                : weapon.projectileSpeed;

            projectile.Launch(owner, fireDirection, damage, ammoType, speed);
            return projectile;
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
