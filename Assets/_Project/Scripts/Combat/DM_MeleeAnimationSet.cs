using System;
using UnityEngine;

namespace Project.Combat
{
    [Serializable]
    public sealed class DMMeleeClipSlots
    {
        [Tooltip("WeakAttacks / SwordAttack A — regular chained light.")]
        public AnimationClip lightA;
        [Tooltip("WeakAttacks / SwordAttack B — regular chained light.")]
        public AnimationClip lightB;
        [Tooltip("WeakAttacks / SwordAttack C — regular chained light.")]
        public AnimationClip lightC;
        [Tooltip("StrongAttacks / SwordAttack A — other heavy. Not the charge release.")]
        public AnimationClip strongA;
        [Tooltip("StrongAttacks / SwordAttack B — charged release (AttackC).")]
        public AnimationClip strongB;
        [Tooltip("StrongAttacks / SwordAttack C — other heavy.")]
        public AnimationClip strongC;
        [Tooltip("Attacks.StrongAttacks.SwordCharge hold pose. Must be a looping wind-up (1HandSwordChargeUp), not a swing.")]
        public AnimationClip chargeHold;

        [Header("Overlays (parry / interact hold)")]
        [Tooltip("1H-RH@Parry01 — tap parry presentation (optional).")]
        public AnimationClip parry01;
        [Tooltip("1H-RH@Parry01_Hit — successful parry hit reaction.")]
        public AnimationClip parry01Hit;
        [Tooltip("Hold E + light attack — One Hand Sword Combo.")]
        public AnimationClip interactHoldCombo;
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

        [Header("One-hand sword (AttackID 1 / Weak SwordAttack A→B→C combo chain)")]
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
