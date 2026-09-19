using UnityEngine;

namespace Project.Player
{
    [CreateAssetMenu(menuName = "Dark Matter/Player/Landing Profile", fileName = "DMLandingProfile")]
    public sealed class DMLandingProfile : ScriptableObject
    {
        public const string ResourcesPath = "Landing/DMLandingProfile";

        private static DMLandingProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        public static DMLandingProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMLandingProfile>(ResourcesPath);
                return live;
            }
        }

        public static DMLandingProfile Resolve(DMLandingProfile assigned)
        {
            DMLandingProfile canonical = Live;
            return canonical != null ? canonical : assigned;
        }


        [Header("Jump / short bounce")]
        [Tooltip("Play owned LandLow bounce on regular jumps below the bounce ceiling (heroDropMeters unless overridden below). Off = leave regular jump land to Invector.")]
        public bool ownedBounceOnRegularJump = true;

        [Tooltip("When > 0, max drop meters that still use short bounce instead of hero LandHigh. When 0, uses ClimbDash heroDropMeters from the Jump/Landing height band.")]
        [Range(0f, 20f)]
        public float shortBounceMaxDropMeters = 0f;

        [Tooltip("Prefer Bounce animator state for short bounce when LandLow is missing.")]
        public bool preferBounceStateForShortLand = true;

        [Header("Early land timing")]
        [Tooltip("Start owned land when feet are within this distance of walkable ground (before touch).")]
        [Range(0.05f, 1.2f)]
        public float landAnticipateMaxDist = 0.52f;

        [Tooltip("Minimum downward speed (m/s, positive number) required before anticipating a land.")]
        [Range(0.5f, 12f)]
        public float landAnticipateMinFallSpeed = 1.35f;

        [Header("Foot snap")]
        [Tooltip("Gap left between foot bottom and ground after snap.")]
        [Range(0f, 0.12f)]
        public float landFeetGroundSkin = 0.03f;

        [Tooltip("Max upward correction per snap. Should be >= landAnticipateMaxDist to avoid air-gap hero lands.")]
        [Range(0.1f, 1.5f)]
        public float landMaxFeetSnap = 0.85f;

        [Header("Ground probe")]
        [Tooltip("Pivot-to-ground distance treated as on walkable ground.")]
        [Range(0.1f, 1f)]
        public float walkableDist = 0.45f;

        [Tooltip("Minimum hit normal Y to count as walkable.")]
        [Range(0.2f, 0.95f)]
        public float walkableNormalY = 0.55f;

        [Tooltip("Seconds on walkable ground before committing a land from air tracking.")]
        [Range(0.02f, 0.4f)]
        public float groundCommitSeconds = 0.06f;

        [Tooltip("Ray origin offset above pivot for down probes.")]
        [Range(0.05f, 0.6f)]
        public float probeRayOriginUp = 0.2f;

        [Tooltip("Max downward ray length for foot/ground probes.")]
        [Range(1f, 12f)]
        public float probeRayMaxDown = 5f;

        [Tooltip("Ignore probe hits farther than this from pivot (anticipate path).")]
        [Range(0.5f, 8f)]
        public float probeValidMaxDist = 3.5f;

        [Tooltip("Reject hits when pivot is this far below the hit point (negative = allow slightly below).")]
        [Range(-0.5f, 0.2f)]
        public float probePivotBelowReject = -0.25f;

        [Header("Owned clip durations (seconds)")]
        [Range(0.15f, 2f)]
        public float landLowDuration = 0.45f;

        [Range(0.3f, 2.5f)]
        public float landHighDuration = 1f;

        [Range(0.4f, 3f)]
        public float jetpackLandDuration = 1.25f;

        [Range(0.2f, 2f)]
        public float bounceLandDuration = 0.65f;

        [Header("Animator")]
        [Range(0.01f, 0.2f)]
        public float landCrossFadeSeconds = 0.03f;

        [Header("Finish land / anti double-land")]
        [Tooltip("After owned land ends, ignore new lands for at least this long.")]
        [Range(0.05f, 1f)]
        public float landGroundedEndGraceSeconds = 0.35f;

        [Tooltip("After clip timeout, keep waiting for walkable ground up to this long before forcing end.")]
        [Range(0.1f, 1.5f)]
        public float landMaxExtraWaitSeconds = 0.55f;

        [Header("Jump micro-contact skip")]
        [Tooltip("Ignore owned land when drop and fall speed are below these (tiny contacts). Regular jump bounce still plays when ownedBounceOnRegularJump is on.")]
        [Range(0.05f, 1f)]
        public float minDropMetersToLand = 0.2f;

        [Range(-6f, 0f)]
        public float minFallSpeedYToLand = -2f;

        [Header("Drop clamp (apex vs speed)")]
        [Range(0.1f, 2f)]
        public float clampDropSpeedEpsilon = 0.5f;

        [Range(0.5f, 4f)]
        public float clampDropShortCap = 1.5f;

        [Range(1f, 12f)]
        public float clampDropPhysicsSlack = 6f;

        [Header("Jetpack land state")]
        [Tooltip("When jetpack was used this airtime and LandLow/Bounce are missing, prefer Jetpack Land animator state for short bounce.")]
        public bool preferJetpackLandState = true;

        [Header("Planar slide / brake")]
        [Tooltip("When ON with Hold Planar: anti-drift mode — slider limits how long drift is suppressed (0 = whole clip).")]
        public bool zeroPlanarVelocityOnLand = true;

        [Tooltip("When ON with Zero Planar: anti-drift mode — also pin X/Z while the guard is active.")]
        public bool holdPlanarPositionDuringLand = true;

        [Tooltip("Both toggles OFF: directional slide mode — keep horizontal momentum this many seconds after touch, then brake. Either toggle ON: anti-drift guard for this many seconds (0 = whole land clip).")]
        [Range(0f, 2.5f)]
        public float slidingLandingHoldSeconds = 1.1f;
    }
}
