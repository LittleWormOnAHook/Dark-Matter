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
            GameObject instance = Object.Instantiate(prefab, spawnPosition, rotation);
            if (instance == null)
                return;

            float scale = Mathf.Clamp(Mathf.Lerp(0.85f, 1.35f, damage / 25f), 0.75f, 1.5f);
            instance.transform.localScale = Vector3.one * scale;
            PlayOneShot(instance);
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

        private static void PlayOneShot(GameObject instance)
        {
            float destroyDelay = 2f;
            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem ps in particleSystems)
            {
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.loop = false;
                ps.Play(true);
                destroyDelay = Mathf.Max(destroyDelay, main.duration + 1f);
            }

            Object.Destroy(instance, destroyDelay);
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
