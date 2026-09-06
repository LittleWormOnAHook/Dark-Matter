using UnityEngine;

namespace Project.Features.Jetpack
{
    [CreateAssetMenu(
        fileName = "DMJetpackProfile",
        menuName = "Dark Matter/Jetpack/Jetpack Profile")]
    public sealed class DMJetpackProfile : ScriptableObject
    {
        [Header("Fuel")]
        [Tooltip("How long a full tank lasts (seconds). Player variant uses Starfield.")]
        [Min(1f)] public float maxBoostSeconds = 8f;
        [Tooltip("Seconds of full thrust before fade. Set equal to Max Boost Seconds for no early cutoff.")]
        [Min(0f)] public float fullThrustEndSeconds = 8f;
        [Min(0f)] public float releaseRegenBonusSeconds = 1f;
        [Min(0f)] public float regenSecondsPerSecond = 0.35f;
        [Tooltip("Survival energy (power) spent per second while boosting.")]
        [Min(0f)] public float energyDrainPerSecond = 10f;

        [Header("Vertical Thrust")]
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
        [Tooltip("Gravity scale during the late-tank thrust fade.")]
        [Min(0f)] public float fadeFallGravityScale = 0.22f;
        [Tooltip("Gravity scale after fuel is empty.")]
        [Min(0f)] public float freeFallGravityScale = 1f;
        [Tooltip("Gravity scale when boost is released mid-air.")]
        [Min(0f)] public float coastFallGravityScale = 0.75f;
        [Tooltip("Bleeds leftover upward velocity after releasing boost.")]
        [Min(0f)] public float releaseUpwardBleed = 5f;

        [Header("Directed Boost (WASD)")]
        [Tooltip("Target planar speed while a direction is held during boost (m/s).")]
        [Min(0.5f)] public float jetpackAirSpeed = 3.25f;
        [Tooltip("How quickly planar velocity lerps toward the held WASD heading.")]
        [Min(1f)] public float jetpackPlanarResponse = 22f;
        [Tooltip("Vertical thrust left at full WASD (0 = flatten, 1 = same climb as hover).")]
        [Range(0f, 1f)] public float directedVerticalScale = 0.35f;
        [Tooltip("How quickly leftover upward speed converts into the held WASD heading (m/s per second).")]
        [Min(0f)] public float directedRedirect = 10f;
        [Tooltip("Brief delay before steer direction catches new WASD input, then planar response takes over.")]
        [Range(0.04f, 0.35f)] public float jetpackSteerInputSmoothTime = 0.11f;
        [Tooltip("Invector airSmooth override during boost (higher = snappier input follow).")]
        [Min(1f)] public float jetpackAirSmooth = 18f;
        [Tooltip("Restored airSpeed when not in jetpack boost/fade.")]
        [Min(0.5f)] public float defaultAirSpeed = 5f;

        [Header("Animation")]
        [Range(0.05f, 0.5f)] public float jetpackMoveSmoothTime = 0.22f;
        [Range(0.5f, 2.5f)] public float animBlendGain = 1.35f;
        [Tooltip("Scales directional fly pose lean in the blend tree (0.1 = 10% of full tilt).")]
        [Range(0.05f, 1f)] public float animLeanStrength = 0.1f;
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

        [Header("Thruster Audio")]
        [Tooltip("How quickly volume/pitch catch thrust.")]
        [Range(0.02f, 0.4f)] public float thrusterAudioSmooth = 0.12f;

        [Header("Thruster Audio / Layer 1 (Rumble)")]
        public AudioClip thrusterLayer1;
        [Range(0f, 1f)] public float thrusterLayer1Volume = 0.55f;
        public Vector2 thrusterLayer1Pitch = new Vector2(0.92f, 1.04f);

        [Header("Thruster Audio / Layer 2 (Bright)")]
        [Tooltip("Comes in as thrust rises.")]
        public AudioClip thrusterLayer2;
        [Range(0f, 1f)] public float thrusterLayer2Volume = 0.4f;
        public Vector2 thrusterLayer2Pitch = new Vector2(0.98f, 1.12f);
        [Range(0f, 1f)] public float thrusterLayer2Start = 0.25f;

        // Back-compat aliases used by older assets.
        public AudioClip thrusterLoop
        {
            get => thrusterLayer1;
            set => thrusterLayer1 = value;
        }

        public float thrusterVolume
        {
            get => thrusterLayer1Volume;
            set => thrusterLayer1Volume = value;
        }

        public Vector2 thrusterPitchRange
        {
            get => thrusterLayer1Pitch;
            set => thrusterLayer1Pitch = value;
        }

        private void OnValidate()
        {
            if (fullThrustEndSeconds > maxBoostSeconds)
                fullThrustEndSeconds = maxBoostSeconds;
        }
    }
}
