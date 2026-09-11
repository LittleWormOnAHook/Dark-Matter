using System;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// One surface row for an ammo profile. Inline prefabs override the optional shared set.
    /// </summary>
    [Serializable]
    public class DMHitMarkSurface
    {
        [Tooltip("Unity tag on the hit object. Untagged is the Io / world default.")]
        public string tag = "Untagged";

        [Tooltip("Optional shared set. Leave empty and fill the arrays below to author this ammo only.")]
        public DMHitMarkSet hitMark;

        [Tooltip("Bullet-hole / scorch for this ammo on this tag.")]
        public GameObject[] decals;

        [Tooltip("Sparks / burst for this ammo on this tag.")]
        public GameObject[] hitEffects;

        public GameObject PickDecal()
        {
            GameObject fromInline = Pick(decals);
            if (fromInline != null)
                return fromInline;
            return hitMark != null ? hitMark.PickDecal() : null;
        }

        public GameObject PickHitEffect()
        {
            GameObject fromInline = Pick(hitEffects);
            if (fromInline != null)
                return fromInline;
            return hitMark != null ? hitMark.PickHitEffect() : null;
        }

        public bool HasAny()
        {
            return PickDecal() != null || PickHitEffect() != null;
        }

        private static GameObject Pick(GameObject[] list)
        {
            if (list == null || list.Length == 0)
                return null;
            if (list.Length == 1)
                return list[0];
            return list[UnityEngine.Random.Range(0, list.Length)];
        }
    }
}
