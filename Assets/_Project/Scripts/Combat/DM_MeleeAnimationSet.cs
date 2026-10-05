using System;
using UnityEngine;

namespace Project.Combat
{
    [Serializable]
    public sealed class DMMeleeClipSlots
    {
        [Tooltip("WeakAttacks / StrongAttacks state A")]
        public AnimationClip lightA;
        public AnimationClip lightB;
        public AnimationClip lightC;
        public AnimationClip strongA;
        public AnimationClip strongB;
        public AnimationClip strongC;
        [Tooltip("Hold pose while charging strong attack (optional; falls back to strongA first frame).")]
        public AnimationClip chargeHold;
    }

    [CreateAssetMenu(menuName = "Dark Matter/Combat/Melee Animation Set", fileName = "DM_MeleeAnimationSet")]
    public sealed class DM_MeleeAnimationSet : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_MeleeAnimationSet";

        private static DM_MeleeAnimationSet live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache() => live = null;

        public static DM_MeleeAnimationSet Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DM_MeleeAnimationSet>(ResourcesPath);
                return live;
            }
        }

        [Header("One-hand sword (AttackID 1 / SwordAttack)")]
        public DMMeleeClipSlots oneHandSword = new DMMeleeClipSlots();

        [Header("Other families (catalog for future weapons)")]
        public DMMeleeClipSlots swordAndShield = new DMMeleeClipSlots();
        public DMMeleeClipSlots knife = new DMMeleeClipSlots();
        public DMMeleeClipSlots axe = new DMMeleeClipSlots();
        public DMMeleeClipSlots twoHand = new DMMeleeClipSlots();
        public DMMeleeClipSlots dualWield = new DMMeleeClipSlots();
        public DMMeleeClipSlots torch = new DMMeleeClipSlots();
    }
}
