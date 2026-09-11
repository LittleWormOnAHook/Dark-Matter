using Invector;
using Project.AI;
using Project.Core;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Combat
{
    /// <summary>
    /// Spawns one-shot blood splatter particle effects at melee hit locations.
    /// </summary>
    public static class CombatHitVfx
    {
        private const string BloodSplatterResourcePath = "Combat/FX_Blood_Splatter";
        private const string BloodSplatterAssetPath =
            "Assets/Synty/PolygonGeneric/Prefabs/FX/FX_Blood_Splatter_01.prefab";

        private static GameObject bloodSplatterPrefab;

        /// <summary>
        /// Spawns hit VFX when an enemy damages the player or a companion via Invector.
        /// </summary>
        public static void SpawnIncomingEnemyHit(vDamage damage, Transform receiver)
        {
            if (damage == null || receiver == null || damage.damageValue <= 0f)
                return;

            if (!IsEnemyDamageSource(damage.sender))
                return;

            Vector3 point = ResolveHitPoint(damage.hitPosition, receiver);
            Vector3 direction = ResolveHitDirection(damage.sender, receiver, point);
            SpawnBloodSplatter(point, direction, -direction, damage.damageValue);
        }

        /// <summary>
        /// Spawns hit VFX for legacy non-Invector enemy melee swings.
        /// </summary>
        public static void SpawnIncomingEnemyHit(Transform attacker, Transform receiver, float damage)
        {
            if (attacker == null || receiver == null || damage <= 0f)
                return;

            if (!IsEnemyDamageSource(attacker))
                return;

            Vector3 point = ResolveHitPoint(Vector3.zero, receiver);
            Vector3 direction = ResolveHitDirection(attacker, receiver, point);
            SpawnBloodSplatter(point, direction, -direction, damage);
        }

        public static void SpawnBloodSplatter(Vector3 hitPoint, Vector3 hitDirection, Vector3 hitNormal, float damage = 1f)
        {
            GameObject prefab = GetBloodSplatterPrefab();
            if (prefab == null)
                return;

            Vector3 spawnPosition = hitPoint;
            if (hitNormal.sqrMagnitude > 0.0001f)
                spawnPosition += hitNormal * 0.03f;

            Vector3 sprayDirection = ResolveSprayDirection(hitDirection, hitNormal);
            if (sprayDirection.sqrMagnitude < 0.0001f)
                sprayDirection = Vector3.forward;

            Quaternion rotation = Quaternion.LookRotation(sprayDirection, Vector3.up);
            GameObject instance = PoolManager.Spawn(prefab, spawnPosition, rotation);
            if (instance == null)
                return;

            float scale = Mathf.Clamp(Mathf.Lerp(0.85f, 1.35f, damage / 25f), 0.75f, 1.5f);
            CombatHitVfxPooledInstance pooled = instance.GetComponent<CombatHitVfxPooledInstance>();
            if (pooled == null)
                pooled = instance.AddComponent<CombatHitVfxPooledInstance>();

            pooled.Play(scale);
        }

        internal static void ReleaseToPool(GameObject instance)
        {
            PoolManager.Release(instance);
        }

        private static bool IsEnemyDamageSource(Transform source)
        {
            if (source == null)
                return false;

            if (source.CompareTag("Enemy"))
                return true;

            return source.GetComponentInParent<EnemyHealth>() != null;
        }

        private static Vector3 ResolveHitPoint(Vector3 hitPosition, Transform receiver)
        {
            Vector3 point = hitPosition != Vector3.zero
                ? hitPosition
                : receiver.position + Vector3.up * 1.2f;

            Collider receiverCollider = receiver.GetComponent<Collider>();
            if (receiverCollider == null)
                receiverCollider = receiver.GetComponentInChildren<Collider>();

            if (receiverCollider != null && receiverCollider.enabled && !receiverCollider.isTrigger)
                point = receiverCollider.ClosestPoint(point);

            return point;
        }

        private static Vector3 ResolveHitDirection(Transform sender, Transform receiver, Vector3 hitPoint)
        {
            if (sender != null)
            {
                Vector3 direction = hitPoint - sender.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                    return direction.normalized;
            }

            Vector3 fallback = receiver.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        private static Vector3 ResolveSprayDirection(Vector3 hitDirection, Vector3 hitNormal)
        {
            Vector3 direction = hitDirection.sqrMagnitude > 0.0001f
                ? hitDirection.normalized
                : Vector3.forward;

            if (hitNormal.sqrMagnitude > 0.0001f)
                direction = Vector3.Slerp(direction, hitNormal, 0.4f).normalized;

            return direction.sqrMagnitude > 0.0001f ? direction : Vector3.up;
        }

        private static GameObject GetBloodSplatterPrefab()
        {
            if (bloodSplatterPrefab != null)
                return bloodSplatterPrefab;

#if UNITY_EDITOR
            bloodSplatterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BloodSplatterAssetPath);
#endif
            if (bloodSplatterPrefab == null)
                bloodSplatterPrefab = Resources.Load<GameObject>(BloodSplatterResourcePath);

            if (bloodSplatterPrefab == null)
                Debug.LogWarning("CombatHitVfx: blood splatter prefab not found.");

            return bloodSplatterPrefab;
        }
    }
}
