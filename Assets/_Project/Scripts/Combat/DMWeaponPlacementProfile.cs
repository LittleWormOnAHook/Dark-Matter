using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Weapon placement bake data (Drawn / Holstered slots per item id and holder bone).
    /// Editor tools read this asset; runtime does not apply placements during Play.
    /// </summary>
    public sealed class DMWeaponPlacementProfile : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Data/Combat/DM_WeaponPlacementProfile.asset";

        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Weapon item id: the slot name without its Drawn_ / Holstered_ prefix (for example Blade_Of_Chaos).")]
            public string itemId;

            [Tooltip("True = Drawn_<item> (in hand), false = Holstered_<item> (on the body).")]
            public bool drawn;

            [Tooltip("Name of the holder bone / holder object the slot sits under (for example R_Hand, Spine2, RifleHolder). Empty = any holder.")]
            public string holderBone;

            [Tooltip("Any = every body type. Otherwise this entry only applies to that body type and wins over the Any entry.")]
            public bool anyBodyType = true;

            public DMEnemyBodyType bodyType = DMEnemyBodyType.Humanoid;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;

            public string Label => (drawn ? "Drawn_" : "Holstered_") + itemId + " @ " + (string.IsNullOrEmpty(holderBone) ? "*" : holderBone) + (anyBodyType ? " [any body]" : " [" + bodyType + "]");
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>Best entry: exact holder + body type &gt; exact holder, any body &gt; any holder + body type &gt; any holder, any body.</summary>
        public Entry Find(string itemId, bool drawn, string holderBone, DMEnemyBodyType body)
        {
            Entry best = null;
            int bestScore = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e == null || e.drawn != drawn || !string.Equals(e.itemId, itemId, StringComparison.Ordinal))
                    continue;
                if (!string.IsNullOrEmpty(e.holderBone) && !string.Equals(e.holderBone, holderBone, StringComparison.Ordinal))
                    continue;
                if (!e.anyBodyType && e.bodyType != body)
                    continue;

                int score = (string.IsNullOrEmpty(e.holderBone) ? 0 : 2) + (e.anyBodyType ? 0 : 1);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }

            return best;
        }
    }
}
