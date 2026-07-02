using UnityEngine;

namespace Project.Player
{
    [System.Serializable]
    public class PlayerLocomotionAnimationSettings
    {
        [Header("Clips (Grounded blend tree overrides)")]
        [Tooltip("Optional idle override for the center of the Grounded blend tree.")]
        public AnimationClip idleAnimation;

        [Tooltip("Forward walk clip (blend tree Forward ≈ 0.5).")]
        public AnimationClip walkAnimation;

        [Tooltip("Forward run/sprint clip (blend tree Forward ≈ 1).")]
        public AnimationClip runAnimation;

        [Header("Playback")]
        [Tooltip("Animator LocomotionAnimSpeed while walking.")]
        [Range(0.25f, 1.5f)]
        public float walkAnimationSpeed = 1f;

        [Tooltip("Animator LocomotionAnimSpeed while sprinting.")]
        [Range(0.25f, 1.5f)]
        public float runAnimationSpeed = 0.85f;

        [Tooltip("Extra LocomotionAnimSpeed multiplier while blending forward+turn lean.")]
        [Range(1f, 1.6f)]
        public float leanBlendAnimSpeedMultiplier = 1.12f;

        [Tooltip("Extra LocomotionAnimSpeed multiplier on sprint forward+turn lean.")]
        [Range(1f, 1.5f)]
        public float sprintLeanAnimSpeedMultiplier = 1.18f;

        [Header("Blend Tree")]
        [Tooltip("Animator Forward value while walking.")]
        [Range(0.1f, 0.75f)]
        public float walkBlendForward = 0.5f;

        [Tooltip("Animator Forward value while sprinting.")]
        [Range(0.55f, 1f)]
        public float runBlendForward = 1f;

        [Tooltip("How quickly Forward blend toward target locomotion.")]
        [Range(0.04f, 0.35f)]
        public float locomotionSmoothTime = 0.18f;

        [Tooltip("How quickly Turn lean blends in/out (forward run banking).")]
        [Range(0.08f, 0.45f)]
        public float leanBlendSmoothTime = 0.24f;

        [Tooltip("How quickly LocomotionAnimSpeed blends between walk and run.")]
        [Range(0.04f, 0.35f)]
        public float locomotionAnimSpeedSmoothTime = 0.12f;

        [Header("Strafe (A/D only)")]
        [Tooltip("Forward input above this uses run+lean; at or below uses strafe/backward branch.")]
        [Range(0.05f, 0.25f)]
        public float strafeForwardCutoff = 0.12f;

        [Tooltip("Scales lateral blend when strafing without forward input (pure A/D or backward).")]
        [Range(0.5f, 1.5f)]
        public float strafeBlendScale = 1f;

        [Header("Forward Run")]
        [Tooltip("Unused — Turn is strafe-only. Forward run keeps Turn at 0; ECM2 rotates the body toward camera.")]
        [Range(0.08f, 0.45f)]
        public float forwardTurnMinForward = 0.15f;

        [Tooltip("Body yaw degrees per second that reach full Turn lean while running forward.")]
        [Range(45f, 240f)]
        public float forwardTurnRateDegrees = 75f;

        [Tooltip("Scales body yaw rate into Turn lean while moving forward.")]
        [Range(0.2f, 1.2f)]
        public float forwardTurnYawMix = 1f;

        [Tooltip("Scales camera yaw delta into Turn lean while moving forward.")]
        [Range(0.1f, 0.8f)]
        public float forwardLookTurnScale = 0.38f;

        [Tooltip("Max |Turn| while sprinting forward (full-body lean cap, not wide strafe).")]
        [Range(0.5f, 1.2f)]
        public float forwardLeanTurnCap = 0.92f;

        [Tooltip("Max |Turn| while walking forward.")]
        [Range(0.5f, 1.5f)]
        public float forwardWalkLeanTurnCap = 1.05f;

        [Tooltip("Reduces Forward as |Turn| increases so lean corners hit without leg spread.")]
        [Range(0f, 0.35f)]
        public float forwardLeanForwardReduction = 0.18f;

        [Header("Layer Blending")]
        [Tooltip("How quickly upper-body combat/charge layer weights fade in and out.")]
        [Range(2f, 20f)]
        public float layerWeightSmoothSpeed = 8f;

        [Tooltip("Cross-fade time when forcing the base layer back to Grounded/Airborne.")]
        [Range(0.05f, 0.35f)]
        public float baseLocomotionCrossFadeTime = 0.15f;

        public bool HasClipOverrides =>
            idleAnimation != null || walkAnimation != null || runAnimation != null;

        public float ResolveLocomotionAnimSpeed(bool isSprinting) =>
            isSprinting ? runAnimationSpeed : walkAnimationSpeed;

        public float ResolveBlendForward(bool isSprinting, float moveIntensity) =>
            (isSprinting ? runBlendForward : walkBlendForward) * moveIntensity;
    }
}
