using Project.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Combat
{
    /// <summary>
    /// One ammo asset: inventory ItemData plus combat FX / hit marks.
    /// Pickups, registry, and weapons reference this asset directly.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Ammo FX Profile", fileName = "DMAmmoFxProfile_")]
    public class DMAmmoFxProfile : ItemData
    {
        [Header("Hit VFX")]
        [Tooltip("Stamp the laser burn mark (pulse / continuous laser).")]
        public bool spawnLaserBurn;

        [Header("Hit Marks (this ammo)")]
        [FormerlySerializedAs("useSurfaceMarks")]
        [Tooltip("Author holes + bursts on this ammo. Rows below override the shared catalog.")]
        public bool useHitMarks;
        [Tooltip("Default hole for this ammo when the tag has no row.")]
        public GameObject[] defaultDecals;
        [Tooltip("Default burst for this ammo when the tag has no row.")]
        public GameObject[] defaultHitEffects;
        [Tooltip("Per-tag marks for this ammo only. Empty tag list still uses the defaults above.")]
        public DMHitMarkSurface[] surfaces;
        [Tooltip("If this ammo has no row/default, use the shared catalog.")]
        public bool fallBackToCatalog = true;
        public DMAmmoFxCatalog catalog;

        private void OnEnable()
        {
            itemType = ItemType.Ammo;
            fxProfile = this;
        }

        public bool TryResolveHitMark(string tag, out GameObject decal, out GameObject hitEffect)
        {
            decal = null;
            hitEffect = null;

            if (useHitMarks && surfaces != null)
            {
                for (int i = 0; i < surfaces.Length; i++)
                {
                    DMHitMarkSurface row = surfaces[i];
                    if (row == null || !string.Equals(row.tag, tag, System.StringComparison.Ordinal))
                        continue;
                    if (!row.HasAny())
                        break;
                    decal = row.PickDecal();
                    hitEffect = row.PickHitEffect();
                    return true;
                }
            }

            if (useHitMarks)
            {
                decal = Pick(defaultDecals);
                hitEffect = Pick(defaultHitEffects);
                if (decal != null || hitEffect != null)
                    return true;
            }

            if (fallBackToCatalog && catalog != null)
            {
                DMHitMarkSet set = catalog.Resolve(tag);
                if (set != null)
                {
                    decal = set.PickDecal();
                    hitEffect = set.PickHitEffect();
                    return decal != null || hitEffect != null;
                }
            }

            return false;
        }

        public void CopyFrom(DMAmmoFxProfile source)
        {
            if (source == null || source == this)
                return;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), this);
            fxProfile = this;
            itemType = ItemType.Ammo;
        }

        public void SeedSurfacesFromCatalog()
        {
            if (catalog == null)
                return;
            catalog.WriteDefaultRows(ref surfaces);
        }

        private static GameObject Pick(GameObject[] list)
        {
            if (list == null || list.Length == 0)
                return null;
            if (list.Length == 1)
                return list[0];
            return list[Random.Range(0, list.Length)];
        }
    }
}
