using Invector;
using Invector.vMelee;
using Project.Core;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// One-shot sparks at the blade contact point on a successful parry.
    /// </summary>
    public static class DMParryClashVfx
    {
        private const string SparksLongPrefabPath = "Assets/_Project/Prefabs/Combat/VFX/SparksLong.prefab";

        public static void TryPlay(vDamage damage, Transform defender, Transform attackerRoot)
        {
            if (defender == null)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            GameObject prefab = ResolveSparksPrefab(profile);
            if (prefab == null)
                return;

            float scale = profile != null ? profile.parryClashVfxScale : 1.15f;
            float lifetime = profile != null ? profile.parryClashVfxLifetimeSeconds : 0.45f;

            Vector3 clashPoint = ResolveClashPoint(damage, defender, attackerRoot);
            Vector3 flatForward = defender.position - (attackerRoot != null ? attackerRoot.position : clashPoint);
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = defender.forward;
            Quaternion rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);

            GameObject instance = PoolManager.Spawn(prefab, clashPoint, rotation);
            if (instance == null)
                return;

            instance.transform.localScale = Vector3.one * scale;
            CombatVfxUtility.PreparePooledOneShotVfx(instance, lifetime);
        }

        private static GameObject ResolveSparksPrefab(DM_CombatCoreProfile profile)
        {
            if (profile != null && profile.parryClashVfxPrefab != null)
                return profile.parryClashVfxPrefab;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SparksLongPrefabPath);
#else
            return null;
#endif
        }

        internal static Vector3 ResolveClashPoint(vDamage damage, Transform defender, Transform attackerRoot)
        {
            Vector3? attackerBlade = ResolveAttackerBladeContact(damage, attackerRoot);
            Vector3? defenderBlade = ResolveDefenderBladeContact(defender);

            if (attackerBlade.HasValue && defenderBlade.HasValue)
                return Vector3.Lerp(attackerBlade.Value, defenderBlade.Value, 0.5f);

            if (attackerBlade.HasValue)
            {
                Vector3 towardDefender = defenderBlade ?? (defender.position + Vector3.up * 1.15f);
                return Vector3.Lerp(attackerBlade.Value, towardDefender, 0.35f);
            }

            if (defenderBlade.HasValue && attackerRoot != null)
                return Vector3.Lerp(defenderBlade.Value, attackerRoot.position + Vector3.up * 1.15f, 0.35f);

            if (attackerRoot != null)
            {
                Vector3 mid = Vector3.Lerp(defender.position + Vector3.up * 1.15f, attackerRoot.position + Vector3.up * 1.15f, 0.5f);
                return mid;
            }

            return defender.position + defender.forward * 0.75f + Vector3.up * 1.15f;
        }

        private static Vector3? ResolveAttackerBladeContact(vDamage damage, Transform attackerRoot)
        {
            if (damage != null && damage.hitPosition != Vector3.zero)
                return damage.hitPosition;

            return TryGetWeaponBladeCenter(FindPrimaryMeleeWeapon(attackerRoot));
        }

        private static Vector3? ResolveDefenderBladeContact(Transform defender)
        {
            return TryGetWeaponBladeCenter(FindPrimaryMeleeWeapon(defender));
        }

        private static vMeleeWeapon FindPrimaryMeleeWeapon(Transform root)
        {
            if (root == null)
                return null;

            vMeleeManager manager = root.GetComponentInParent<vMeleeManager>()
                ?? root.GetComponentInChildren<vMeleeManager>();
            if (manager == null)
                return null;

            if (manager.rightWeapon != null)
                return manager.rightWeapon;
            return manager.leftWeapon;
        }

        private static Vector3? TryGetWeaponBladeCenter(vMeleeWeapon weapon)
        {
            if (weapon == null)
                return null;

            if (weapon.hitBoxes != null && weapon.hitBoxes.Count > 0)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                for (int i = 0; i < weapon.hitBoxes.Count; i++)
                {
                    vHitBox box = weapon.hitBoxes[i];
                    if (box == null)
                        continue;

                    Collider col = box.trigger;
                    sum += col != null ? col.bounds.center : box.transform.position;
                    count++;
                }

                if (count > 0)
                    return sum / count;
            }

            return weapon.transform.position;
        }
    }
}
