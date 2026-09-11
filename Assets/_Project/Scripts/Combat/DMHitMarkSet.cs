using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// One surface's hole + burst. Port of Invector impact rows, DM-named.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Hit Mark Set", fileName = "DM_HitMark_")]
    public class DMHitMarkSet : ScriptableObject
    {
        [Tooltip("Bullet-hole / scorch decals parented to the hit object.")]
        public GameObject[] decals;

        [Tooltip("One-shot sparks / dust at the impact point.")]
        public GameObject[] hitEffects;

        public GameObject PickDecal()
        {
            return Pick(decals);
        }

        public GameObject PickHitEffect()
        {
            return Pick(hitEffects);
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
