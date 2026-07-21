using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Shared projectile VFX helpers for player, companion, and enemy fire.
    /// </summary>
    public static class CombatVfxUtility
    {
        public static ItemData ResolveAmmoItem(ItemData weapon, ItemData ammoItem)
        {
            if (ammoItem != null)
                return ammoItem;

            return weapon != null ? weapon.defaultAmmoItem : null;
        }

        public static GameObject ResolveTracerPrefab(ItemData ammoItem, ItemData weapon)
        {
            if (ammoItem != null && ammoItem.tracerPrefab != null)
                return ammoItem.tracerPrefab;

            return weapon != null ? weapon.tracerPrefab : null;
        }

        public static void PlayParticleSystemsRecursive(GameObject root)
        {
            if (root == null)
                return;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }
    }
}
