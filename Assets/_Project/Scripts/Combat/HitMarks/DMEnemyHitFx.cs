using Project.Core;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Element rules (Combat Plan §16, D6) and pooled, budgeted splatter / spark spawns for enemy ranged hits.
    /// </summary>
    public static class DMEnemyHitFx
    {
        private const int MaxBudgetSlots = 64;
        private const float BurstAliveSeconds = 0.5f;
        private const float BurstPoolLifeSeconds = 1.25f;

        private static readonly float[] BurstTimes = new float[MaxBudgetSlots];
        private static int burstHead;
        private static Camera cachedCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (int i = 0; i < BurstTimes.Length; i++)
                BurstTimes[i] = -100f;
            burstHead = 0;
            cachedCamera = null;
        }

        public static AmmoType ResolveAmmoType(ItemData ammo, ItemData weapon, DMAmmoFxProfile profile)
        {
            ItemData resolved = CombatVfxUtility.ResolveAmmoItem(weapon, ammo);
            if (resolved != null)
                return resolved.ammoType;
            if (profile != null)
                return profile.ammoType;
            return weapon != null ? weapon.defaultAmmoType : AmmoType.Gunpowder;
        }

        /// <summary>§16: Laser never bleeds. Per-ammo override on the FX profile wins over Auto.</summary>
        public static bool AllowsBlood(DMAmmoFxProfile profile, AmmoType type)
        {
            if (profile != null && profile.enemyBlood != DMEnemyFxRule.Auto)
                return profile.enemyBlood == DMEnemyFxRule.Allow;
            return type != AmmoType.Laser;
        }

        /// <summary>§16: Ion never burns. Per-ammo override on the FX profile wins over Auto.</summary>
        public static bool AllowsBurn(DMAmmoFxProfile profile, AmmoType type)
        {
            if (profile != null && profile.enemyBurnMark != DMEnemyFxRule.Auto)
                return profile.enemyBurnMark == DMEnemyFxRule.Allow;
            return type != AmmoType.Ion;
        }

        public static bool IsWithinFxDistance(Vector3 point, float maxDistance)
        {
            if (maxDistance <= 0f)
                return true;

            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
                cachedCamera = Camera.main;
            if (cachedCamera == null)
                return true;

            return (cachedCamera.transform.position - point).sqrMagnitude <= maxDistance * maxDistance;
        }

        /// <summary>Global cap on concurrent splatter / spark bursts (oldest slot must have expired).</summary>
        public static bool TryConsumeBurstBudget(int budget)
        {
            int size = Mathf.Clamp(budget, 1, MaxBudgetSlots);
            float now = Time.time;
            int index = burstHead % size;
            if (now - BurstTimes[index] < BurstAliveSeconds)
                return false;

            BurstTimes[index] = now;
            burstHead = (index + 1) % size;
            return true;
        }

        /// <summary>Entry spray: out of the wound, biased back toward the shooter.</summary>
        public static Vector3 ResolveEntrySprayDirection(Vector3 surfaceNormal, Vector3 travelDirection, float normalBlend)
        {
            Vector3 n = surfaceNormal.sqrMagnitude > 0.0001f ? surfaceNormal.normalized : -travelDirection.normalized;
            Vector3 back = travelDirection.sqrMagnitude > 0.0001f ? -travelDirection.normalized : n;
            if (n.sqrMagnitude < 0.0001f)
                n = Vector3.up;
            Vector3 dir = Vector3.Slerp(n, back, Mathf.Clamp01(normalBlend));
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : n;
        }

        public static GameObject SpawnBurst(GameObject prefab, Vector3 point, Vector3 direction, float scale)
        {
            if (prefab == null)
                return null;

            Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            GameObject instance = PoolManager.Spawn(prefab, point, Quaternion.LookRotation(dir, up));
            if (instance == null)
                return null;

            instance.transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.2f, 3f);
            CombatVfxUtility.PreparePooledOneShotVfx(instance, BurstPoolLifeSeconds);
            return instance;
        }

        /// <summary>Small pooled FX stuck to a bone / hitbox (ember glow). World scale normalised under cm rigs.</summary>
        public static GameObject SpawnAttached(GameObject prefab, Vector3 point, Vector3 normal, Transform parent, float life)
        {
            if (prefab == null)
                return null;

            Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            Vector3 up = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(n, up);
            GameObject instance = PoolManager.Spawn(prefab, point, rotation, parent);
            if (instance == null)
                return null;

            if (parent != null)
                instance.transform.SetPositionAndRotation(point, rotation);
            CombatVfxUtility.NormalizeAttachedWorldScale(instance.transform);
            CombatVfxUtility.PreparePooledOneShotVfx(instance, life);
            return instance;
        }
    }
}
