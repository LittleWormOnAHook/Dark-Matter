using System;
using UnityEngine;

namespace Project.Pioneers
{
    [Serializable]
    public class PioneerBehaviorProfile
    {
        [Header("Behavior")]
        public PioneerFollowMode followMode = PioneerFollowMode.FollowPlayer;

        [Header("Locomotion")]
        [Range(0.1f, 1f)] public float wanderPaceScale = 0.3f;
        public float walkSpeed = 4.4f;
        public float runSpeed = 8.5f;
        public float catchUpSpeed = 8.5f;
        public float catchUpDistance = 5.5f;
        public float maxFollowDistance = 14f;
        public float stopDistance = 0.45f;
        public float formationHeadingSmoothTime = 0.45f;

        [Header("Animation")]
        public float walkAnimationSpeed = 0.7f;
        public float runAnimationSpeed = 0.9f;
        public float walkSpeedReference = 4.4f;
        public float runSpeedReference = 8.5f;

        [Header("Combat Positioning")]
        public float combatTetherRadius = 6.5f;
        public float preferredCombatDistance = 2.4f;
        public float rangedPreferredDistance = 7f;
        public float losSearchRadius = 1.5f;
        public float formationDriftDegreesPerSecond = 2.8f;

        public bool PrefersRangedSpacing(SkilledPioneerClass pioneerClass) =>
            pioneerClass == SkilledPioneerClass.InfiltratorScout
            || pioneerClass == SkilledPioneerClass.ScienceSpecialist;

        public float ResolvePreferredCombatDistance(SkilledPioneerClass pioneerClass) =>
            PrefersRangedSpacing(pioneerClass) ? rangedPreferredDistance : preferredCombatDistance;

        public PioneerBehaviorProfile Clone()
        {
            return (PioneerBehaviorProfile)MemberwiseClone();
        }
    }
}
