using UnityEngine;

namespace Project.Features.Jetpack
{
    [CreateAssetMenu(
        fileName = "DMJetpackProfile",
        menuName = "Dark Matter/Jetpack/Jetpack Profile")]
    public sealed class DMJetpackProfile : ScriptableObject
    {
        [Header("Fuel")]
        [Min(1f)] public float maxBoostSeconds = 8f;
        [Min(0f)] public float releaseRegenBonusSeconds = 1f;
        [Min(0f)] public float regenSecondsPerSecond = 0.35f;

        [Header("Vertical Thrust")]
        [Min(0f)] public float fullThrustEndSeconds = 6f;
        [Tooltip("Target upward speed while holding boost (m/s).")]
        [Min(0f)] public float upwardThrustForce = 6.5f;
        [Tooltip("Target downward speed when holding S during boost (m/s, positive value).")]
        [Min(0f)] public float descendForce = 3.5f;
        [Range(0f, 1f)] public float descendInputThreshold = 0.35f;
        [Range(0f, 1f)] public float descendBrakeStrength = 0.85f;
        [Tooltip("How quickly vertical velocity eases toward the thrust target.")]
        [Min(0.5f)] public float verticalResponse = 3f;
        [Tooltip("Gravity scale while hovering at full boost with no vertical input.")]
        [Min(0f)] public float hoverGravityScale = 0.12f;
        [Tooltip("Gravity scale while intentionally descending during boost.")]
        [Min(0f)] public float boostDescendGravityScale = 0.85f;
        [Tooltip("Gravity scale during the 6–8 s thrust fade.")]
        [Min(0f)] public float fadeFallGravityScale = 0.22f;
        [Tooltip("Gravity scale after fuel is empty.")]
        [Min(0f)] public float freeFallGravityScale = 1f;
        [Tooltip("Gravity scale when boost is released mid-air.")]
        [Min(0f)] public float coastFallGravityScale = 0.75f;
        [Tooltip("Bleeds leftover upward velocity after releasing boost.")]
        [Min(0f)] public float releaseUpwardBleed = 5f;

        [Header("Horizontal Movement")]
        [Tooltip("Invector airSpeed while jetpack boost/fade is active.")]
        [Min(0.5f)] public float jetpackAirSpeed = 3.25f;
        [Tooltip("Restored airSpeed when not in jetpack boost/fade.")]
        [Min(0.5f)] public float defaultAirSpeed = 5f;

        [Header("Animation")]
        [Range(0.05f, 0.5f)] public float jetpackMoveSmoothTime = 0.22f;
        [Range(0.5f, 2.5f)] public float animBlendGain = 1.35f;
        [Range(0f, 0.35f)] public float animInputDeadzone = 0.1f;
        [Range(0.5f, 2f)] public float flyAnimSpeed = 1f;
        [Range(0.25f, 0.85f)] public float animStrafeThreshold = 0.5f;
        [Range(0.05f, 0.35f)] public float landCrossFadeSeconds = 0.12f;

        [Header("Thruster VFX")]
        [Range(0f, 1f)] public float thrusterAlphaMin = 0.1f;
        [Range(0f, 1f)] public float thrusterAlphaMax = 1f;
        [Range(0.05f, 0.5f)] public float thrusterPowerSmoothTime = 0.2f;
        [Min(0.1f)] public float flareFactorMultiplier = 0.85f;
        [Min(0.1f)] public float slowSparksFactorMultiplier = 0.8f;
        [Min(0.1f)] public float fastSparksFactorMultiplier = 0.8f;
        [Min(0.1f)] public float distortionFactorMultiplier = 0.85f;
        [Min(0.1f)] public float particleSpeedMultiplier = 0.7f;
        [Min(0.1f)] public float particleLifetimeMultiplier = 1.15f;
    }
}
