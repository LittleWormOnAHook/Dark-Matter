using System;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Master surface table for ranged hit marks. Lives with ammo under
    /// Assets/_Project/Data/Items/Ammo.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Ammo FX Catalog", fileName = "DMAmmoFxCatalog")]
    public class DMAmmoFxCatalog : ScriptableObject
    {
        [Serializable]
        public class Surface
        {
            [Tooltip("Unity tag on the hit object. Untagged is the Io / world default.")]
            public string tag = "Untagged";
            public DMHitMarkSet hitMark;
        }

        [SerializeField] private Surface[] surfaces =
        {
            new Surface { tag = "Untagged" },
            new Surface { tag = "Dirt" },
            new Surface { tag = "Metal" },
            new Surface { tag = "Concrete" },
            new Surface { tag = "Wood" },
            new Surface { tag = "Glass" },
            new Surface { tag = "Barrel" }
        };

        [Tooltip("Used when the hit tag has no row.")]
        [SerializeField] private DMHitMarkSet fallback;

        public DMHitMarkSet Resolve(string tag)
        {
            if (surfaces != null)
            {
                for (int i = 0; i < surfaces.Length; i++)
                {
                    Surface row = surfaces[i];
                    if (row == null || row.hitMark == null)
                        continue;
                    if (string.Equals(row.tag, tag, StringComparison.Ordinal))
                        return row.hitMark;
                }
            }

            return fallback;
        }

        public void WriteDefaultRows(ref DMHitMarkSurface[] rows)
        {
            if (surfaces == null || surfaces.Length == 0)
                return;

            rows = new DMHitMarkSurface[surfaces.Length];
            for (int i = 0; i < surfaces.Length; i++)
            {
                Surface row = surfaces[i];
                if (row == null)
                    continue;
                rows[i] = new DMHitMarkSurface
                {
                    tag = row.tag,
                    hitMark = row.hitMark
                };
            }
        }
    }
}
