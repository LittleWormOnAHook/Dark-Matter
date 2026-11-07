using Project.Features.Locomotion;
using Project.Survival;
using UnityEngine;

namespace Project.Features.Climb
{
    [CreateAssetMenu(menuName = "Dark Matter/Player/Climb Dash Profile", fileName = "DM_ClimbDashProfile")]
    public sealed class DM_ClimbDashProfile : ScriptableObject
    {
        public const string ResourcesPath = "Climb/DM_ClimbDashProfile";

        private static DM_ClimbDashProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        /// <summary>Canonical Resources asset. Play-mode inspector edits apply immediately.</summary>
        public static DM_ClimbDashProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DM_ClimbDashProfile>(ResourcesPath);
                return live;
            }
        }

        public static DM_ClimbDashProfile Resolve(DM_ClimbDashProfile assigned)
        {
            DM_ClimbDashProfile canonical = Live;
            return canonical != null ? canonical : assigned;
        }

        [Header("Slope cutoffs (degrees from up)")]
        [Tooltip("At or under this, the player walks/runs even if the mesh is Climbable. Matches Invector slopeLimit (75).")]
        [Range(20f, 89f)]
        public float walkMaxSlopeDeg = 75f;

        [Tooltip("Must be steeper than walkMax to climb. Usually the same number.")]
        [Range(20f, 89f)]
        public float climbMinSlopeDeg = 75f;

        [Tooltip("Past this, the face is too inverted to cling.")]
        [Range(90f, 170f)]
        public float climbMaxSlopeDeg = 115f;

        [Header("Raycast distances")]
        [Tooltip("Chest ray / spherecast that grabs a wall you are facing.")]
        [Range(0.4f, 4f)]
        public float attachRange = 1.4f;

        [Tooltip("How far a climb-jump can reach another Climbable.")]
        [Range(0.8f, 8f)]
        public float climbJumpRange = 3f;

        [Range(0.05f, 0.5f)]
        public float probeRadius = 0.18f;

        [Tooltip("Must face the wall at least this much (1 = dead-on).")]
        [Range(0.15f, 1f)]
        public float faceDotMin = 0.15f;

        [Header("Attach")]
        [Range(0.1f, 1.2f)]
        public float standOff = 0.35f;

        [Tooltip("Start climb only after a jump (second Space). First Space is the jump.")]
        public bool startClimbNeedsAirborne = true;

        [Tooltip("Start climb only while holding forward (W).")]
        public bool startClimbNeedsForward = true;

        [Header("Hand grab")]
        [Tooltip("How far the wrist sits off the wall so fingers rest on the surface instead of through it.")]
        [Range(0.02f, 0.25f)]
        public float handPalmOffset = 0.11f;

        [Tooltip("Left/right hand spacing on the wall.")]
        [Range(0.08f, 0.4f)]
        public float handSpread = 0.18f;

        [Tooltip("Grab when airborne and a climbable is in attachRange. Off â€” start climb is second Space + forward.")]
        public bool autoGrabInAir;

        [Header("Motion")]
        [Range(0.2f, 6f)]
        public float moveSpeed = 2.6f;

        [Tooltip("Hold Shift while clinging to multiply crawl speed.")]
        [Range(1f, 2.5f)]
        public float climbShiftMul = 1.35f;

        [Range(0.01f, 0.5f)]
        public float climbInputDamp = 0.1f;

        [Header("Release / drop-to-hang")]
        [Tooltip("Jump while climbing or hanging always lets go.")]
        public bool jumpReleases = true;

        [Tooltip("How hard jump-release pushes you off the wall.")]
        [Range(0f, 6f)]
        public float releasePush = 1.6f;

        [Tooltip("Short climb-leap speed (m/s) along the climb direction.")]
        [Range(2f, 12f)]
        public float climbLeapSpeed = 1.45f;

        [Tooltip("Space cling hop distance (meters). ~1 body-width; hold Space only slight stretch.")]
        [Range(0.5f, 12f)]
        public float clingHop = 0.72f;

        [Tooltip("After one Space leap, WASD toward a Climbable snaps back. Seconds.")]
        [Range(0.12f, 1.2f)]
        public float climbLeapRegrab = 0.55f;

        [Tooltip("E-drop shove away from the wall (m/s).")]
        [Range(0.5f, 6f)]
        public float dropPush = 2.4f;

        [Tooltip("Must leave the wall this far before a NEW start-climb can stick. Leap regrab ignores this.")]
        [Range(0.4f, 3f)]
        public float detachBuffer = 1.15f;

        [Tooltip("Unused auto-cling distance. Start climb is second Space + forward.")]
        [Range(3f, 20f)]
        public float highFallGrabMeters = 6f;

        [Tooltip("Air steer time after an E-drop or hop.")]
        [Range(0.2f, 2f)]
        public float airControlSeconds = 0.95f;

        [Tooltip("From a walkable ledge top: walk forward into the void, hold S toward the drop, or press E near the lip â€” reverse-mantles into hang (180 to face the wall). Standing still at the edge does not trigger.")]
        public bool dropToHang = true;

        [Tooltip("How far below the lip we look for a climbable face when drop-to-hang / reverse mantle starts.")]
        [Range(0.4f, 4f)]
        public float dropToHangRange = 1.8f;

        [Header("Overhang lip")]
        [Tooltip("When climb-up stops under a soffit or rock shelf, climb onto the nearest lip.")]
        public bool enableOverhangGrab = true;

        [Tooltip("How far above the hands to look for a soffit or rock shelf.")]
        [Range(0.4f, 2.2f)]
        public float overhangReachUp = 1.15f;

        [Tooltip("How far back from the wall the nearest lip may be (slabs and cliff shelves).")]
        [Range(0.4f, 2.2f)]
        public float overhangReachBack = 1.4f;

        [Tooltip("Minimum seconds for the climb-up onto the lip. Longer grabs use climb speed.")]
        [Range(0.2f, 1.4f)]
        public float overhangGrabSeconds = 0.78f;

        [Tooltip("Outward shelf probe start (meters from climb wall). Lower catches thin/short lips.")]
        [Range(0.02f, 0.25f)]
        public float overhangMinProbeOut = 0.05f;

        [Tooltip("Planar lip distance from the climb wall that counts as deep protrusion. Drives short-hop grab â€” not vertical thickness.")]
        [Range(0.25f, 1.6f)]
        public float overhangDeepProtrusion = 0.65f;

        [Tooltip("Deep lips: fraction of planar travel done as a short hop toward the lip. Higher = less arm stretch.")]
        [Range(0.35f, 0.98f)]
        public float overhangShortHopPull = 0.94f;

        [Tooltip("Extra hop arc height (meters) on deep short-hop lip grabs.")]
        [Range(0f, 0.45f)]
        public float overhangShortHopArc = 0.22f;

        [Tooltip("Deep grab: IK weight ramps in after this fraction of the hop. Higher delays hand IK until the body is closer.")]
        [Range(0f, 0.9f)]
        public float overhangIkBlendStart = 0.72f;

        [Tooltip("Body hang inset from the lip along the wall normal.")]
        [Range(0.05f, 0.4f)]
        public float overhangHangInset = 0.18f;

        [Tooltip("Seconds for a deep short-hop lip grab. Deep hop uses this only (no speedDur shorten).")]
        [Range(0.12f, 1.25f)]
        public float overhangShortHopSeconds = 0.85f;

        [Header("Mantle")]
        public bool enableMantle = true;

        [Tooltip("W only starts a mantle when there is no climbable wall left above the hands.")]
        public bool mantleRequiresOpenLip = true;

        [Tooltip("Hand / lip height from the feet. Wall-above probes start at the chest under this.")]
        [Range(0.9f, 1.6f)]
        public float handHeight = 1.18f;

        [Tooltip("Meters onto the top from the lip hit. 0 plants on the lip. Negative pulls back toward the wall.")]
        [Range(-0.5f, 1.2f)]
        public float mantleForward = 0.42f;

        [Tooltip("How high above the hang the down-probe starts.")]
        [Range(0.8f, 2.2f)]
        public float mantleProbeUp = 1.5f;

        [Tooltip("How far the lip/floor probe searches down.")]
        [Range(0.6f, 2.4f)]
        public float mantleProbeDown = 1.7f;

        [Tooltip("0 = flush with the found top. Positive lifts the plant, negative sinks feet into the ledge.")]
        [Range(-0.2f, 0.2f)]
        public float mantlePlantHeight = 0.04f;

        [Tooltip("Legacy alias. Ignored; plant uses mantlePlantHeight including 0 and negatives.")]
        [Range(-0.2f, 0.2f)]
        public float mantleStandPad = 0.04f;

        [Tooltip("Seconds to lerp hang â†’ lip â†’ stand.")]
        [Range(0.5f, 2.4f)]
        public float mantleSeconds = 1.4f;

        [Tooltip("Ignore fall-land / ragdoll this long after a mantle.")]
        [Range(0.5f, 4f)]
        public float mantleIgnoreLands = 2.6f;

        [Header("Landing")]
        [Tooltip("Jump/Landing: drops shorter than this use short bounce (LandLow). From this height up to the damage band uses hero LandHigh; higher still hero + damage (jetpack grace on lethal).")]
        [Range(0.5f, 8f)]
        public float heroDropMeters = 2.6f;

        [Tooltip("Fall this far or more (while falling) is death ragdoll + retry, unless jetpack grace applies.")]
        [Range(8f, 200f)]
        public float lethalDropMeters = 100f;

        [Tooltip("No fall damage below this height. Damage starts at the greater of this and (lethal minus fallDamageLethalPercent).")]
        [Range(8f, 80f)]
        public float fallDamageStartMeters = 40f;

        [Tooltip("Damage starts this fraction below lethal (0.3 = 30% less than lethal). Combined with fallDamageStartMeters via max().")]
        [Range(0.1f, 0.6f)]
        public float fallDamageLethalPercent = 0.3f;

        [Tooltip("Health fraction removed when landing between the no-damage floor and lethal height.")]
        [Range(0.1f, 1f)]
        public float fallDamageHealthFraction = 0.5f;

        [Tooltip("After jetpack Space/boost is released, wait this many seconds before a lethal-height fall can kill. Still boosting, or land inside this window, is always hero.")]
        [Range(0f, 20f)]
        public float jetpackLethalDelay = 6f;

        [Header("Locomotion â€” walk / run / sprint")]
        [Tooltip("Default slow walk = Invector runningSpeed Ã— this (0.5 = half prior jog speed).")]
        [Range(0.25f, 1f)]
        public float slowWalkSpeedMultiplier = 0.5f;

        [Tooltip("Shift hold jog = Invector runningSpeed Ã— this.")]
        [Range(0.5f, 1.5f)]
        public float jogSpeedMultiplier = 1f;

        [Tooltip("Double-tap Shift burst = Invector sprintSpeed Ã— this (+20% = 1.2). Stamina drain scales by the same factor.")]
        [Min(1f)]
        public float sprintBurstSpeedMultiplier = 1.2f;

        [Tooltip("Max seconds between Shift taps to arm the sprint burst.")]
        [Min(0.05f)]
        public float shiftDoubleTapWindow = 0.28f;

        // 2026-10-20c: Strafe locomotion plan P1 (DMStrafeLocomotionDirector). Defaults = Anthony's 2026-10-19 decisions.
        [Header("Locomotion - strafe / backpedal")]
        [Tooltip("Off = legacy free locomotion. Hybrid = free inside the forward cone, strafe outside (side steps / diagonals / backpedal). CombatContext = strafe only with a drawn weapon near enemies. AlwaysStrafe = always.")]
        public DMStrafeLocomotionMode strafeLocoMode = DMStrafeLocomotionMode.Hybrid;
        [Tooltip("Hybrid: input within this angle of camera forward uses the free tree (turn-lean run). 67 = W, W+A, W+D on keyboard; A/D/S strafe.")]
        [Range(0f, 90f)] public float strafeLocoForwardConeHalfAngle = 67f; // 2026-10-20e: 30 -> 67 (W+A / W+D free)
        [Tooltip("Degrees of hysteresis around the forward cone (stops analog flicker).")]
        [Range(0f, 30f)] public float strafeLocoHysteresis = 10f;
        [Tooltip("Minimum seconds between free/strafe switches.")]
        [Range(0f, 1f)] public float strafeLocoMinHoldSeconds = 0.15f;
        [Tooltip("Weapon drawn + enemy within the engage radius = combat context (holds camera yaw while idle; see Combat Ignores Forward Cone).")]
        public bool strafeLocoCombatFullStrafe = true;
        [Tooltip("2026-10-20e: ON = legacy full strafe in combat (W also strafes, slower). OFF = combat keeps the forward free cone (W / W+A / W+D free at normal speed).")]
        public bool strafeLocoCombatIgnoresForwardCone;
        [Tooltip("Enemy distance (m) that counts as engaged for combat context.")]
        [Min(0f)] public float strafeLocoCombatEngageRadius = 12f;
        [Tooltip("Burst sprint (double-tap Shift) only arms when input is within this angle of camera forward.")]
        [Range(0f, 180f)] public float strafeLocoSprintConeHalfAngle = 50f;
        [Tooltip("Stick magnitude below which the input angle is ignored (pad).")]
        [Range(0f, 0.9f)] public float strafeLocoPadDeadzoneMagnitude = 0.2f;
        [Tooltip("On-screen label: director mode / yield reason / input angle.")]
        public bool strafeLocoDebugOverlay;

        [Header("Body weight (ground secondary motion, 2026-10-20l/n)")]
        [Tooltip("Master toggle: grounded spine/chest lag, lean, stop carry, arm trailing, landing hip drop (like the jetpack limb dangle). Visual only.")]
        public bool bodyWeightEnabled;
        [Tooltip("Intensity preset (2026-10-22b, all based on the fair Custom snapshot). Custom = your values. Subtle x0.65, Visible x1.2, Extreme x6 (stress test). Only magnitudes scale; timings, springs, deadzones and ratios stay.")]
        public BodyWeightPreset bodyWeightPreset = BodyWeightPreset.Custom;
        [Tooltip("Global multiplier on every body-weight offset (rotations, hip drop, CoM shift, swing). 1 = authored values. Only NaN/explosion guards clamp the result.")]
        [Range(0f, 10f)] public float bodyWeightIntensity = 1f;
        [Tooltip("Verification multiplier stacked on top of intensity (e.g. 4). Set back to 1 when done.")]
        [Range(1f, 10f)] public float bodyWeightDebugExaggerate = 1f;
        [Tooltip("On-screen live readout: attached / enabled / weight / gates / offsets.")]
        public bool bodyWeightDebugReadout;
        [Tooltip("Effect scale while aiming (keeps aim IK clean). 0 = off while aiming.")]
        [Range(0f, 1f)] public float bodyWeightAimScale = 0.15f;
        [Tooltip("Smoothing (s) of the measured body acceleration / turn rate.")]
        [Range(0.01f, 0.5f)] public float bodyWeightAccelSmooth = 0.08f;
        [Tooltip("2026-10-21a: planar speed (m/s) below which body velocity counts as zero when there is no move input (idle contact noise).")]
        [Range(0f, 1f)] public float bodyWeightIdleSpeedDeadzone = 0.2f;
        [Tooltip("2026-10-21a: turn rate (deg/s) below which yaw counts as zero (idle / tiny in-place turns).")]
        [Range(0f, 90f)] public float bodyWeightYawRateDeadzone = 20f;
        [Tooltip("2026-10-21a: acceleration (m/s^2) below which the lean / arm-trail input is zeroed.")]
        [Range(0f, 10f)] public float bodyWeightAccelDeadzone = 1.5f;
        [Tooltip("Spine pitch (deg per m/s^2) leaning into forward acceleration.")]
        [Range(0f, 10f)] public float bodyWeightLeanAccel = 0.6f;
        [Tooltip("Spine pitch (deg per m/s^2) carried forward when braking / stopping.")]
        [Range(0f, 10f)] public float bodyWeightStopCarry = 0.6f;
        [Tooltip("Spine roll (deg per m/s^2) leaning into lateral acceleration / turns.")]
        [Range(0f, 10f)] public float bodyWeightLeanTurn = 0.9f;
        [Tooltip("Spine/chest yaw lag behind body turns (deg per 100 deg/s).")]
        [Range(0f, 30f)] public float bodyWeightYawLag = 4f;
        [Tooltip("Clamp for spine + chest offsets before intensity (deg).")]
        [Range(0f, 90f)] public float bodyWeightMaxSpineDegrees = 14f;
        [Tooltip("Spine spring smoothing (s). Higher = heavier, slower.")]
        [Range(0.02f, 1f)] public float bodyWeightSpineSmooth = 0.2f;
        [Tooltip("Trunk settle spring damping ratio (1 = no overshoot; lower = sway past and settle after stops).")]
        [Range(0.1f, 1.2f)] public float bodyWeightSettleDamping = 0.4f;
        [Tooltip("Chest phase lag behind spine/hips (s). Breaks up the rigid-block torso on turns.")]
        [Range(0f, 0.6f)] public float bodyWeightChestLag = 0.12f;
        [Tooltip("Arm trailing strength (deg per m/s^2), like the jetpack dangle inertia.")]
        [Range(0f, 10f)] public float bodyWeightArmTrail = 1.3f;
        [Tooltip("Clamp for arm offsets before intensity (deg).")]
        [Range(0f, 90f)] public float bodyWeightMaxArmDegrees = 18f;
        [Tooltip("Arm spring smoothing (s). Elbows/wrists use progressively more lag.")]
        [Range(0.02f, 1f)] public float bodyWeightArmSmooth = 0.15f;
        [Tooltip("Elbow (forearm) whip relative to the upper arm.")]
        [Range(0f, 4f)] public float bodyWeightElbowTrail = 1.5f;
        [Tooltip("Wrist (hand) whip relative to the upper arm.")]
        [Range(0f, 4f)] public float bodyWeightWristTrail = 2.3f;
        [Tooltip("Hip drop impulse on landing, scaled by fall speed.")]
        [Range(0f, 1f)] public float bodyWeightLandingDrop = 0.08f;
        [Tooltip("Landing recovery speed (fraction of hip spring used while rising back up). Lower = slower recovery.")]
        [Range(0.05f, 1f)] public float bodyWeightLandingRecovery = 0.4f;
        [Tooltip("Hip dip per sharp direction change.")]
        [Range(0f, 1f)] public float bodyWeightDirChangeDrop = 0.03f;
        [Tooltip("Acceleration (m/s^2) that counts as a sharp direction change.")]
        [Range(1f, 30f)] public float bodyWeightDirChangeAccel = 8f;
        [Tooltip("Hip spring stiffness (compression side).")]
        [Range(10f, 400f)] public float bodyWeightHipSpring = 90f;
        [Tooltip("Max hip drop before intensity (m).")]
        [Range(0f, 0.6f)] public float bodyWeightMaxHipDrop = 0.1f;

        [Header("Body weight: regions (biomechanics)")]
        [Tooltip("Pelvis: leads body turns (deg per 100 deg/s of turn).")]
        [Range(0f, 20f)] public float bodyWeightPelvisLead = 3f;
        [Tooltip("Pelvis / CoM spring smoothing (s).")]
        [Range(0.02f, 1f)] public float bodyWeightPelvisSmooth = 0.12f;
        [Tooltip("Shoulder-hip counter-rotation: chest turns against the pelvis lead (0-1 of pelvis yaw).")]
        [Range(0f, 3f)] public float bodyWeightCounterRotation = 0.8f;
        [Tooltip("Centre-of-mass / weight shift: hips slide toward lateral acceleration (m per m/s^2).")]
        [Range(0f, 0.1f)] public float bodyWeightComShift = 0.012f;
        [Tooltip("Max CoM shift before intensity (m).")]
        [Range(0f, 0.4f)] public float bodyWeightMaxComShift = 0.05f;
        [Tooltip("Anticipation: lean toward the input direction before velocity catches up (deg).")]
        [Range(0f, 30f)] public float bodyWeightAnticipation = 4f;
        [Tooltip("Knee/hip compression when braking (feet stay planted by foot IK).")]
        [Range(0f, 0.3f)] public float bodyWeightDecelCompression = 0.02f;
        [Tooltip("Head stabilisation: neck+head cancel this share of trunk offsets (gaze stays level). 0-1.")]
        [Range(0f, 1f)] public float bodyWeightHeadStabilize = 0.5f;
        [Tooltip("Neck/head spring smoothing (s).")]
        [Range(0.02f, 0.6f)] public float bodyWeightHeadSmooth = 0.12f;

        [Header("Body weight: melee swing spring")]
        [Tooltip("Torso/arm/pelvis follow-through from the swinging hand's angular velocity (deg per 100 deg/s). Visual only; hitboxes/timing unchanged. 0 = off.")]
        [Range(0f, 10f)] public float bodyWeightSwingAmount = 1f;
        [Tooltip("Swing spring stiffness. Higher = snappier settle.")]
        [Range(10f, 400f)] public float bodyWeightSwingStiffness = 60f;
        [Tooltip("Swing spring damping ratio (1 = no overshoot, lower = more overshoot).")]
        [Range(0.1f, 1.2f)] public float bodyWeightSwingDamping = 0.5f;
        [Tooltip("Clamp for swing offsets before intensity (deg).")]
        [Range(0f, 90f)] public float bodyWeightSwingMaxDegrees = 12f;

        [Header("Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity)")]
        [Tooltip("Master toggle for melee windup / follow-through / impact jolt, ranged recoil absorption and draw/holster weight.")]
        public bool bodyWeightWeaponEnabled = true;
        [Tooltip("Weight factor: unarmed melee.")]
        [Range(0f, 5f)] public float bodyWeightWeaponUnarmed = 0.6f;
        [Tooltip("Weight factor: melee weapon in hand.")]
        [Range(0f, 5f)] public float bodyWeightWeaponMelee = 1f;
        [Tooltip("Extra multiplier during strong / charged swings.")]
        [Range(1f, 5f)] public float bodyWeightWeaponHeavyMult = 1.6f;
        [Tooltip("Weight factor: ranged weapon in hand.")]
        [Range(0f, 5f)] public float bodyWeightWeaponRanged = 1f;
        [Tooltip("Follow-through gain on the swing spring (x Swing Amount). Round 1 was effectively 1.")]
        [Range(0f, 20f)] public float bodyWeightWeaponSwingGain = 4f;
        [Tooltip("Windup anticipation: torso counter-twists against the hand's angular acceleration at swing start (deg per 1000 deg/s^2).")]
        [Range(0f, 20f)] public float bodyWeightWeaponWindup = 3f;
        [Tooltip("Share of the swing that pulls the hips round (pelvis yaw).")]
        [Range(0f, 3f)] public float bodyWeightWeaponHipPull = 0.6f;
        [Tooltip("Impact jolt on melee contact (hit-stop start): torso bounce back (deg).")]
        [Range(0f, 60f)] public float bodyWeightWeaponImpactJolt = 8f;
        [Tooltip("Impact hip dip on melee contact (m).")]
        [Range(0f, 0.3f)] public float bodyWeightWeaponImpactHipDrop = 0.03f;
        [Tooltip("Ranged: per-shot spine kick (deg). Chest counter-rotates so the shoulders / aim stay on target.")]
        [Range(0f, 40f)] public float bodyWeightWeaponRecoilKick = 4f;
        [Tooltip("Ranged: per-shot hip push back (m).")]
        [Range(0f, 0.2f)] public float bodyWeightWeaponRecoilHipPush = 0.02f;
        [Tooltip("Recoil / draw spring stiffness.")]
        [Range(20f, 600f)] public float bodyWeightWeaponRecoilStiffness = 180f;
        [Tooltip("Recoil / draw spring damping ratio (lower = more settle wobble).")]
        [Range(0.1f, 1.2f)] public float bodyWeightWeaponRecoilDamping = 0.45f;
        [Tooltip("Draw / holster: forward torso dip + right arm sag (deg).")]
        [Range(0f, 45f)] public float bodyWeightWeaponDrawDip = 6f;
        [Tooltip("Draw / holster hip drop (m).")]
        [Range(0f, 0.3f)] public float bodyWeightWeaponDrawHipDrop = 0.025f;
        [Header("Body weight: hit reactions (2026-10-20w, visual only)")]
        [Tooltip("Player takes damage: directional torso impulse (bend away), hip shove, head whip, spring recovery.")]
        public bool bodyWeightHitEnabled = true;
        [Tooltip("Peak torso bend away from the hit (deg) at reference damage.")]
        [Range(0f, 60f)] public float bodyWeightHitImpulse = 7f;
        [Tooltip("Hip shove in the push direction (m) at reference damage.")]
        [Range(0f, 0.3f)] public float bodyWeightHitHipShove = 0.025f;
        [Tooltip("Head whip (deg) at reference damage; lags the torso and settles.")]
        [Range(0f, 60f)] public float bodyWeightHitHeadWhip = 5f;
        [Tooltip("Recovery spring stiffness (higher = snappier return).")]
        [Range(30f, 600f)] public float bodyWeightHitRecovery = 110f;
        [Tooltip("Recovery damping ratio (lower = more wobble).")]
        [Range(0.15f, 1.5f)] public float bodyWeightHitDamping = 0.5f;
        [Tooltip("Damage that gives scale 1.")]
        [Range(1f, 300f)] public float bodyWeightHitDamageRef = 20f;
        [Tooltip("Poise damage counted as extra damage (x this).")]
        [Range(0f, 5f)] public float bodyWeightHitPoiseWeight = 0.5f;
        [Tooltip("Smallest / largest hit scale.")]
        [Range(0f, 2f)] public float bodyWeightHitMinScale = 0.35f;
        [Range(0.1f, 6f)] public float bodyWeightHitMaxScale = 2f;

        [Header("Body weight: foot planting (2026-10-20w, no NavMesh)")]
        [Tooltip("Pins planted feet during stops / slow turns with a two-bone leg IK (capped), lowers the hips when the plant is out of reach. Off while climbing/hanging/airborne.")]
        public bool bodyWeightFootPlantEnabled = true;
        [Tooltip("Planting only below this body speed (m/s).")]
        [Range(0f, 10f)] public float bodyWeightFootPlantMaxSpeed = 1.5f;
        [Tooltip("Foot counts as planted within this height above its lowest pose (m).")]
        [Range(0.01f, 0.5f)] public float bodyWeightFootPlantHeight = 0.06f;
        [Tooltip("Max distance a foot is held away from the animation before it releases (m).")]
        [Range(0f, 0.5f)] public float bodyWeightFootPlantMaxCorrection = 0.1f;
        [Tooltip("Plant blend in/out time (s).")]
        [Range(0.01f, 1f)] public float bodyWeightFootPlantBlend = 0.1f;
        [Tooltip("Max hip lowering so an out-of-reach planted foot still lands (m).")]
        [Range(0f, 0.3f)] public float bodyWeightFootPlantHipLower = 0.03f;
        [Tooltip("2026-10-24h: max frame time fed to the body-weight springs (s); longer hitches are clamped (springs are sub-stepped).")]
        [Range(0.01f, 0.2f)] public float bodyWeightMaxDt = 0.05f;

        [Header("Footsteps: trigger fit (2026-10-24h, runtime only, prefab untouched)")]
        [Tooltip("Moves the Invector foot-step trigger spheres from the ankle down to the sole at runtime (Kade/mixamo ankles sit ~9-10 cm above the sole, so the 10 cm spheres barely touched the ground).")]
        public bool bodyWeightFootstepTriggerFit = true;
        [Tooltip("How far the trigger sphere reaches below the measured sole (m).")]
        [Range(0f, 0.3f)] public float bodyWeightFootstepTriggerSoleReach = 0.03f;
        [Tooltip("Trigger sphere radius after fit (m).")]
        [Range(0.02f, 0.5f)] public float bodyWeightFootstepTriggerRadius = 0.08f;
        [Tooltip("Extra sole height correction (m, + = sole is lower than the toe-bone estimate).")]
        [Range(-0.2f, 0.2f)] public float bodyWeightFootstepSoleOffset = 0f;

        [Header("Wall hand touch (2026-10-24o, runtime only, prefab untouched)")]
        [Tooltip("Free wall-side hand touches flat, near-vertical walls while walking/jogging (not sprint/aim/attack/reload/dodge/jetpack/climb/actions).")]
        public bool bodyWeightWallHandEnabled = true;
        public bool bodyWeightWallHandLeft = true;
        public bool bodyWeightWallHandRight = true;
        [Tooltip("Max shoulder-to-wall distance (m).")]
        [Range(0.2f, 1.5f)] public float bodyWeightWallHandReach = 0.7f;
        [Tooltip("Hand height relative to the shoulder (m).")]
        [Range(-0.6f, 0.6f)] public float bodyWeightWallHandHeight = 0f;
        [Tooltip("How far ahead of the shoulder the hand lands, along the wall (m).")]
        [Range(-0.3f, 0.9f)] public float bodyWeightWallHandForwardOffset = 0.25f;
        [Tooltip("Blend in/out time (s).")]
        [Range(0.05f, 1.5f)] public float bodyWeightWallHandBlendTime = 0.25f;
        [Tooltip("Max wall angle away from straight sideways (deg). Small = only walls you walk parallel to.")]
        [Range(5f, 85f)] public float bodyWeightWallHandMaxAngleDeg = 40f;
        [Tooltip("Max wall tilt from vertical (deg).")]
        [Range(0f, 45f)] public float bodyWeightWallHandMaxTiltDeg = 15f;
        [Tooltip("Max normal disagreement between probe rays (deg). Lower = flatter walls only.")]
        [Range(1f, 45f)] public float bodyWeightWallHandFlatnessDeg = 8f;
        [Tooltip("Minimum wall width along the walk direction (m).")]
        [Range(0.1f, 2f)] public float bodyWeightWallHandMinWidth = 0.5f;
        [Tooltip("Wall layers the hand may touch (2026-10-24q default: Default + Climbable). A hit matching EITHER these layers OR the tag list is accepted.")]
        public LayerMask bodyWeightWallHandLayers = (1 << 0) | (1 << 23); // Default + Climbable
        [Tooltip("Wall tag allow-list (any layer). Hit accepted if its collider (or root) has one of these tags. Unknown/empty tags are ignored.")]
        public string[] bodyWeightWallHandTags = { "Climbable" };
        [Tooltip("24ab: extra clearance (m) kept between the body capsule and the wall while a hand touch is active, so the arm has room (0 = off).")]
        [Range(0f, 0.6f)] public float bodyWeightWallHandStandoff = 0.15f;
        [Tooltip("24ab: how fast the standoff blends in/out and pushes the body away (per second). Higher = firmer.")]
        [Range(0.5f, 20f)] public float bodyWeightWallHandStandoffBlendSpeed = 5f;
        [Tooltip("24ab: how strongly the elbow bends out away from the wall (0 = old elbow-out/down hint).")]
        [Range(0f, 1.5f)] public float bodyWeightWallHandElbowAway = 0.6f;
        public bool bodyWeightWallHandDebug = false;

        /// <summary>0 = Subtle, 1 = Default, 2 = Extreme.</summary>
        public void ApplyWallHandPreset(int preset)
        {
            bodyWeightWallHandEnabled = bodyWeightWallHandLeft = bodyWeightWallHandRight = true;
            int baseMask = LayerMask.GetMask("Default", "Climbable");
            if (preset == 2)
            {
                bodyWeightWallHandLayers = baseMask | LayerMask.GetMask("Building", "PW_Object_Large", "PW_Object_Medium");
                bodyWeightWallHandTags = new[] { "Climbable", "Climb", "FreeClimb", "WallRun", "Building" };
            }
            else
            {
                bodyWeightWallHandLayers = baseMask;
                bodyWeightWallHandTags = new[] { "Climbable" };
            }
            switch (preset)
            {
                case 0: bodyWeightWallHandReach = 0.55f; bodyWeightWallHandHeight = -0.05f; bodyWeightWallHandForwardOffset = 0.15f; bodyWeightWallHandBlendTime = 0.35f; bodyWeightWallHandMaxAngleDeg = 25f; bodyWeightWallHandMaxTiltDeg = 8f; bodyWeightWallHandFlatnessDeg = 5f; bodyWeightWallHandMinWidth = 0.8f; bodyWeightWallHandStandoff = 0.08f; bodyWeightWallHandStandoffBlendSpeed = 3f; bodyWeightWallHandElbowAway = 0.3f; break;
                case 2: bodyWeightWallHandReach = 1.2f; bodyWeightWallHandHeight = 0.15f; bodyWeightWallHandForwardOffset = 0.45f; bodyWeightWallHandBlendTime = 0.12f; bodyWeightWallHandMaxAngleDeg = 70f; bodyWeightWallHandMaxTiltDeg = 35f; bodyWeightWallHandFlatnessDeg = 25f; bodyWeightWallHandMinWidth = 0.2f; bodyWeightWallHandStandoff = 0.3f; bodyWeightWallHandStandoffBlendSpeed = 8f; bodyWeightWallHandElbowAway = 1f; break;
                default: bodyWeightWallHandReach = 0.7f; bodyWeightWallHandHeight = 0f; bodyWeightWallHandForwardOffset = 0.25f; bodyWeightWallHandBlendTime = 0.25f; bodyWeightWallHandMaxAngleDeg = 40f; bodyWeightWallHandMaxTiltDeg = 15f; bodyWeightWallHandFlatnessDeg = 8f; bodyWeightWallHandMinWidth = 0.5f; bodyWeightWallHandStandoff = 0.15f; bodyWeightWallHandStandoffBlendSpeed = 5f; bodyWeightWallHandElbowAway = 0.6f; break;
            }
        }

        [Header("Body weight: breathing / idle (2026-10-20w)")]
        public bool bodyWeightBreathEnabled = true;
        [Tooltip("Resting breaths per second.")]
        [Range(0.05f, 2f)] public float bodyWeightBreathRate = 0.25f;
        [Tooltip("Resting chest rise (deg).")]
        [Range(0f, 10f)] public float bodyWeightBreathDepth = 0.8f;
        [Tooltip("Resting shoulder lift (deg).")]
        [Range(0f, 10f)] public float bodyWeightBreathShoulder = 0.6f;
        [Tooltip("Extra breaths per second at full exertion (sprint / jetpack).")]
        [Range(0f, 3f)] public float bodyWeightBreathExertRate = 0.45f;
        [Tooltip("Extra chest rise at full exertion (deg).")]
        [Range(0f, 15f)] public float bodyWeightBreathExertDepth = 1.8f;
        [Tooltip("Seconds of sprint / jetpack to reach full exertion.")]
        [Range(0.05f, 30f)] public float bodyWeightBreathExertRise = 4f;
        [Tooltip("Seconds to recover from exertion (time constant).")]
        [Range(0.1f, 60f)] public float bodyWeightBreathExertDecay = 5f;
        [Tooltip("Breathing kept while aiming (0-1).")]
        [Range(0f, 1f)] public float bodyWeightBreathAimScale = 0.25f;
        [Tooltip("2026-10-20y aim fade: upper-body channels (pelvis yaw, spine, chest, shoulders, arms, hands, neck, head; incl. breathing, hit reactions, swing/settle springs, chest lag) while aiming. 0 = rock steady.")]
        [Range(0f, 1f)] public float bodyWeightAimUpperBodyScale = 0f;
        [Tooltip("2026-10-20y aim fade: time (s) to fade the upper body in/out when aim starts/ends.")]
        [Range(0.01f, 1f)] public float bodyWeightAimFadeTime = 0.12f;
        [Tooltip("2026-10-20y aim fade: hip sway (COM shift / hit shove) kept while aiming (moves the whole gun).")]
        [Range(0f, 1f)] public float bodyWeightAimHipSwayScale = 0f;
        [Tooltip("2026-10-20y aim recoil: kick kept while aiming (impulse on the shot frame only).")]
        [Range(0f, 2f)] public float bodyWeightAimRecoilScale = 0.6f;
        [Tooltip("2026-10-20y aim recoil: settle speed multiplier while aiming (stiffness x this; critically damped) so the kick is gone in a few frames.")]
        [Range(1f, 8f)] public float bodyWeightAimRecoilSettle = 3f;

        [Header("Body weight: enemies (2026-10-20w, visual only, never touches weapons)")]
        public bool bodyWeightEnemyEnabled = true;
        [Tooltip("Lean into acceleration (deg per m/s^2).")]
        [Range(0f, 10f)] public float bodyWeightEnemyLean = 0.8f;
        [Tooltip("Forward carry when stopping (deg per m/s^2).")]
        [Range(0f, 10f)] public float bodyWeightEnemyStopCarry = 1.1f;
        [Tooltip("Lean clamp (deg).")]
        [Range(0f, 90f)] public float bodyWeightEnemyMaxDegrees = 12f;
        [Tooltip("Settle spring time (s).")]
        [Range(0.02f, 2f)] public float bodyWeightEnemySettleSmooth = 0.22f;
        [Tooltip("Settle damping ratio.")]
        [Range(0.1f, 1.5f)] public float bodyWeightEnemySettleDamping = 0.45f;
        [Tooltip("Attack windup / follow-through torso pitch (deg).")]
        [Range(0f, 40f)] public float bodyWeightEnemySwing = 4f;
        [Tooltip("Hit reaction bend away (deg) at reference damage.")]
        [Range(0f, 60f)] public float bodyWeightEnemyHitImpulse = 8f;
        [Range(1f, 300f)] public float bodyWeightEnemyHitDamageRef = 25f;
        [Tooltip("No body weight beyond this camera distance (m); off-screen enemies are skipped too.")]
        [Range(0f, 300f)] public float bodyWeightEnemyCullDistance = 40f;
        [Tooltip("Per enemy type scale: nameContains=scale; separated by ';' (first match wins). Example: Brute=1.4;Drone=0")]
        public string bodyWeightEnemyTypeScales = "Brute=1.4;Heavy=1.3;Drone=0;Turret=0";

        public enum BodyWeightPreset { Custom, Subtle, Visible, Extreme }

        /// <summary>Global multiplier: intensity x debug exaggerate. 2026-10-20t: presets are value sets now (no hidden multiplier).</summary>
        public float BodyWeightScale => Mathf.Max(0f, bodyWeightIntensity) * Mathf.Max(1f, bodyWeightDebugExaggerate);

        // ---- 2026-10-20t body weight presets (reproducible value tables, built from the code defaults) ----
        [SerializeField, HideInInspector] private BodyWeightPreset bodyWeightAppliedPresetInternal = BodyWeightPreset.Custom;

        // Not touched by presets / reset (user toggles and meta).
        private static readonly string[] BodyWeightKeep = { "bodyWeightEnabled", "bodyWeightPreset", "bodyWeightDebugReadout" };

        // Magnitude fields scaled by the preset factor (smoothing, stiffness, ratios and thresholds are not scaled).
        private static readonly string[] BodyWeightScaled =
        {
            "bodyWeightLeanAccel", "bodyWeightStopCarry", "bodyWeightLeanTurn", "bodyWeightYawLag", "bodyWeightMaxSpineDegrees",
            "bodyWeightArmTrail", "bodyWeightMaxArmDegrees", "bodyWeightLandingDrop", "bodyWeightDirChangeDrop", "bodyWeightMaxHipDrop",
            "bodyWeightPelvisLead", "bodyWeightComShift", "bodyWeightMaxComShift", "bodyWeightAnticipation", "bodyWeightDecelCompression",
            "bodyWeightSwingAmount", "bodyWeightSwingMaxDegrees",
            "bodyWeightWeaponWindup", "bodyWeightWeaponImpactJolt", "bodyWeightWeaponImpactHipDrop", "bodyWeightWeaponRecoilKick",
            "bodyWeightWeaponRecoilHipPush", "bodyWeightWeaponDrawDip", "bodyWeightWeaponDrawHipDrop",
            "bodyWeightHitImpulse", "bodyWeightHitHipShove", "bodyWeightHitHeadWhip", "bodyWeightBreathDepth", "bodyWeightBreathShoulder",
            "bodyWeightBreathExertDepth", "bodyWeightEnemyLean", "bodyWeightEnemyStopCarry", "bodyWeightEnemyMaxDegrees", "bodyWeightEnemySwing", "bodyWeightEnemyHitImpulse"
        };

        /// <summary>Subtle = code defaults (x1). Visible x2.5 and Extreme x6 match the old multiplier look; Extreme also wobbles more.</summary>
        // 2026-10-22b: presets are rebased on Anthony's Custom values (BodyWeightCustomBase) and scaled uniformly:
        // Subtle x0.65, Visible x1.2, Extreme x6 (stress test, angle caps / damping overrides below).
        public static float BodyWeightPresetFactor(BodyWeightPreset preset) =>
            preset == BodyWeightPreset.Subtle ? 0.65f : preset == BodyWeightPreset.Visible ? 1.2f : preset == BodyWeightPreset.Extreme ? 6f : 1f;

        private static System.Collections.Generic.List<System.Reflection.FieldInfo> s_bwFields;
        private static System.Collections.Generic.List<System.Reflection.FieldInfo> BodyWeightFields
        {
            get
            {
                if (s_bwFields != null) return s_bwFields;
                s_bwFields = new System.Collections.Generic.List<System.Reflection.FieldInfo>();
                foreach (var f in typeof(DM_ClimbDashProfile).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    if (f.Name.StartsWith("bodyWeight", System.StringComparison.Ordinal) && System.Array.IndexOf(BodyWeightKeep, f.Name) < 0)
                        s_bwFields.Add(f);
                return s_bwFields;
            }
        }

        // 2026-10-22b: snapshot of the Custom body-weight values Anthony rated "fair" (2026-10-22). Preset base; fields not
        // listed here (hit / foot plant / breathing / aim / enemy) equal the code defaults, which were his Custom values too.
        private static readonly System.Collections.Generic.Dictionary<string, float> BodyWeightCustomBase = new System.Collections.Generic.Dictionary<string, float>
        {
            { "bodyWeightAimScale", 0.15f }, { "bodyWeightAccelSmooth", 0.08f }, { "bodyWeightIdleSpeedDeadzone", 0.2f }, { "bodyWeightYawRateDeadzone", 20f },
            { "bodyWeightAccelDeadzone", 1.5f }, { "bodyWeightLeanAccel", 1f }, { "bodyWeightStopCarry", 1.4f }, { "bodyWeightLeanTurn", 0.9f },
            { "bodyWeightYawLag", 4f }, { "bodyWeightMaxSpineDegrees", 14f }, { "bodyWeightSpineSmooth", 0.2f }, { "bodyWeightSettleDamping", 0.4f },
            { "bodyWeightChestLag", 0.12f }, { "bodyWeightArmTrail", 1.3f }, { "bodyWeightMaxArmDegrees", 18f }, { "bodyWeightArmSmooth", 0.15f },
            { "bodyWeightElbowTrail", 1.5f }, { "bodyWeightWristTrail", 1.8f }, { "bodyWeightLandingDrop", 0.08f }, { "bodyWeightLandingRecovery", 0.3f },
            { "bodyWeightDirChangeDrop", 0.03f }, { "bodyWeightDirChangeAccel", 8f }, { "bodyWeightHipSpring", 90f }, { "bodyWeightMaxHipDrop", 0.1f },
            { "bodyWeightPelvisLead", 3f }, { "bodyWeightPelvisSmooth", 0.12f }, { "bodyWeightCounterRotation", 0.8f }, { "bodyWeightComShift", 0.012f },
            { "bodyWeightMaxComShift", 0.05f }, { "bodyWeightAnticipation", 4f }, { "bodyWeightDecelCompression", 0.02f }, { "bodyWeightHeadStabilize", 0.5f },
            { "bodyWeightHeadSmooth", 0.12f }, { "bodyWeightSwingAmount", 0.55f }, { "bodyWeightSwingStiffness", 60f }, { "bodyWeightSwingDamping", 0.5f },
            { "bodyWeightSwingMaxDegrees", 7.2f }, { "bodyWeightWeaponUnarmed", 0.6f }, { "bodyWeightWeaponMelee", 1.64f }, { "bodyWeightWeaponHeavyMult", 2.12f },
            { "bodyWeightWeaponRanged", 1f }, { "bodyWeightWeaponSwingGain", 2.03f }, { "bodyWeightWeaponWindup", 4.95f }, { "bodyWeightWeaponHipPull", 0.459f },
            { "bodyWeightWeaponImpactJolt", 8f }, { "bodyWeightWeaponImpactHipDrop", 0.03f }, { "bodyWeightWeaponRecoilKick", 4f }, { "bodyWeightWeaponRecoilHipPush", 0.02f },
            { "bodyWeightWeaponRecoilStiffness", 180f }, { "bodyWeightWeaponRecoilDamping", 0.45f }, { "bodyWeightWeaponDrawDip", 5.9f }, { "bodyWeightWeaponDrawHipDrop", 0.025f },
        };

        /// <summary>Preset table value for one field (from a fresh defaults instance).</summary>
        private static object BodyWeightPresetValue(System.Reflection.FieldInfo f, DM_ClimbDashProfile defaults, BodyWeightPreset preset)
        {
            object v = f.GetValue(defaults);
            if (f.Name == "bodyWeightIntensity") return 1f;
            if (f.Name == "bodyWeightDebugExaggerate") return 1f;
            if (v is float && BodyWeightCustomBase.TryGetValue(f.Name, out float cb)) v = cb; // 2026-10-22b: Custom base, not code defaults
            if (preset == BodyWeightPreset.Extreme)
            {
                if (f.Name == "bodyWeightSettleDamping") return 0.3f;
                if (f.Name == "bodyWeightSwingDamping") return 0.4f;
                if (f.Name == "bodyWeightWeaponRecoilDamping") return 0.35f;
                // 2026-10-20w (chaos pass): x6 on the angle caps folded the spine past 90 deg; Extreme keeps the x6 drive but caps the look.
                if (f.Name == "bodyWeightMaxSpineDegrees") return 40f;
                if (f.Name == "bodyWeightMaxArmDegrees") return 50f;
                if (f.Name == "bodyWeightSwingMaxDegrees") return 45f;
            }
            if (v is float fv && System.Array.IndexOf(BodyWeightScaled, f.Name) >= 0)
            {
                fv *= BodyWeightPresetFactor(preset);
                var r = (RangeAttribute)System.Attribute.GetCustomAttribute(f, typeof(RangeAttribute));
                if (r != null) fv = Mathf.Clamp(fv, r.min, r.max);
                return fv;
            }
            return v;
        }

        // ---- 2026-10-21b: last Custom values, kept in the asset while a preset is active ----
        [SerializeField, HideInInspector] private System.Collections.Generic.List<string> bodyWeightCustomStashNames = new System.Collections.Generic.List<string>();
        [SerializeField, HideInInspector] private System.Collections.Generic.List<string> bodyWeightCustomStashValues = new System.Collections.Generic.List<string>();
        public bool HasBodyWeightCustomStash => bodyWeightCustomStashNames != null && bodyWeightCustomStashNames.Count > 0; // bodyWeightCustomStash

        private void CaptureBodyWeightCustomStash()
        {
            bodyWeightCustomStashNames = new System.Collections.Generic.List<string>();
            bodyWeightCustomStashValues = new System.Collections.Generic.List<string>();
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var f in BodyWeightFields)
            {
                object v = f.GetValue(this);
                string s = v is float fv ? fv.ToString("R", ci) : v is System.IFormattable fm ? fm.ToString(null, ci) : v?.ToString();
                if (s == null) continue;
                bodyWeightCustomStashNames.Add(f.Name); bodyWeightCustomStashValues.Add(s);
            }
        }

        private void RestoreBodyWeightCustomStash()
        {
            if (!HasBodyWeightCustomStash || bodyWeightCustomStashValues == null) return;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            for (int i = 0; i < bodyWeightCustomStashNames.Count && i < bodyWeightCustomStashValues.Count; i++)
            {
                var f = typeof(DM_ClimbDashProfile).GetField(bodyWeightCustomStashNames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f == null || BodyWeightFields.IndexOf(f) < 0) continue;
                string s = bodyWeightCustomStashValues[i];
                try
                {
                    if (f.FieldType == typeof(float)) f.SetValue(this, float.Parse(s, ci));
                    else if (f.FieldType == typeof(int)) f.SetValue(this, int.Parse(s, ci));
                    else if (f.FieldType == typeof(bool)) f.SetValue(this, bool.Parse(s));
                    else if (f.FieldType.IsEnum) f.SetValue(this, System.Enum.Parse(f.FieldType, s));
                }
                catch (System.Exception) { }
            }
        }

        /// <summary>Writes the full value set of a preset into every body-weight field (Custom = restores the last Custom values). Intensity/Exaggerate end at 1.</summary>
        public void ApplyBodyWeightPreset(BodyWeightPreset preset)
        {
            if (preset == BodyWeightPreset.Custom)
            {
                // 2026-10-21b: coming back from a preset restores the last Custom values.
                if (bodyWeightAppliedPresetInternal != BodyWeightPreset.Custom) RestoreBodyWeightCustomStash();
                bodyWeightPreset = preset; bodyWeightAppliedPresetInternal = preset; return;
            }
            // 2026-10-21b: leaving Custom -> remember the Custom values first.
            if (bodyWeightAppliedPresetInternal == BodyWeightPreset.Custom) CaptureBodyWeightCustomStash();
            var d = CreateInstance<DM_ClimbDashProfile>();
            try { foreach (var f in BodyWeightFields) f.SetValue(this, BodyWeightPresetValue(f, d, preset)); }
            finally { if (Application.isPlaying) Destroy(d); else DestroyImmediate(d); }
            bodyWeightPreset = preset;
            bodyWeightAppliedPresetInternal = preset;
        }

        /// <summary>Every bodyWeight* field (incl. weapon weight) back to the Subtle preset (Custom base x0.65). Enabled / readout toggles kept.</summary>
        public void ResetBodyWeightToDefault() => ApplyBodyWeightPreset(BodyWeightPreset.Subtle);

        public bool BodyWeightMatchesPreset(BodyWeightPreset preset)
        {
            if (preset == BodyWeightPreset.Custom) return true;
            var d = CreateInstance<DM_ClimbDashProfile>();
            try
            {
                foreach (var f in BodyWeightFields)
                {
                    object want = BodyWeightPresetValue(f, d, preset), have = f.GetValue(this);
                    if (want is float a && have is float b) { if (Mathf.Abs(a - b) > 1e-4f) return false; }
                    else if (!Equals(want, have)) return false;
                }
                return true;
            }
            finally { if (Application.isPlaying) Destroy(d); else DestroyImmediate(d); }
        }

#if UNITY_EDITOR
        [ContextMenu("Reset Body Weight to Default")]
        private void ContextResetBodyWeight()
        {
            UnityEditor.Undo.RecordObject(this, "Reset Body Weight to Default");
            ResetBodyWeightToDefault();
            SaveBodyWeightSoon();
        }

        private bool _bwValidateQueued;

        private void OnValidate()
        {
            // Deferred one editor tick: no CreateInstance during load/validate, and Undo records cleanly.
            if (_bwValidateQueued) return;
            if (bodyWeightPreset == BodyWeightPreset.Custom && bodyWeightAppliedPresetInternal == BodyWeightPreset.Custom) return;
            _bwValidateQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                _bwValidateQueued = false;
                ValidateBodyWeightPreset();
            };
        }

        private void ValidateBodyWeightPreset()
        {
            // Preset picked -> write its value set. Any hand edit while on a preset -> Custom (values untouched).
            if (bodyWeightPreset != bodyWeightAppliedPresetInternal)
            {
                UnityEditor.Undo.RecordObject(this, "Apply Body Weight Preset");
                ApplyBodyWeightPreset(bodyWeightPreset);
                SaveBodyWeightSoon();
            }
            else if (bodyWeightPreset != BodyWeightPreset.Custom && !BodyWeightMatchesPreset(bodyWeightPreset))
            {
                UnityEditor.Undo.RecordObject(this, "Body Weight -> Custom");
                bodyWeightPreset = BodyWeightPreset.Custom;
                bodyWeightAppliedPresetInternal = BodyWeightPreset.Custom;
                SaveBodyWeightSoon();
            }
        }

        private void SaveBodyWeightSoon()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            };
        }
#endif

        [Header("Locomotion â€” jump height (gait)")]
        [Tooltip("Standing still and slow-walk jumps (Invector jumpHeight for this hop).")]
        [Min(0.35f)]
        public float standingJumpHeight = 2.5f;

        [Tooltip("Slow-walk + move uses standingJumpHeight Ã— this (1 = same as standing hop).")]
        [Min(0.5f)]
        public float walkJumpHeightMultiplier = 1f;

        [Tooltip("Shift-hold jog jump = standingJumpHeight Ã— this.")]
        [Min(1f)]
        public float jogJumpHeightMultiplier = 1.4f;

        [Tooltip("Double-tap Shift sprint burst jump = standingJumpHeight Ã— this.")]
        [Min(1f)]
        public float sprintJumpHeightMultiplier = 1.9f;

        [Tooltip("Extra scale from horizontal speed: mult *= exp(exponent Ã— speed01) / exp(exponent). 0 = gait tiers only.")]
        [Min(0f)]
        public float jumpHeightSpeedExponent = 1.35f;

        [Header("Locomotion - turn lean")]
        [Tooltip("Use these lean settings instead of Invector's raw lean. Off = stock Invector lean.")]
        public bool leanOverrideEnabled = true;

        [Tooltip("Lean multiplier while walking (1 = stock Invector). Higher = deeper lean into turns.")]
        [Range(0f, 4f)]
        public float leanWalkStrength = 1.6f;

        [Tooltip("Lean multiplier while running / sprinting (1 = stock Invector).")]
        [Range(0f, 4f)]
        public float leanRunStrength = 1.8f;

        [Tooltip("Seconds to ease into and out of the lean. Lower = snappier.")]
        [Range(0.01f, 0.6f)]
        public float leanSmoothTime = 0.12f;

        [Tooltip("Cap on the lean value sent to the animator (the lean clips top out around 1).")]
        [Range(0.2f, 2f)]
        public float leanMaxValue = 1f;

        [Tooltip("Extra procedural upper-body roll (degrees) at full lean, on top of the lean clips. 0 = clips only.")]
        [Range(0f, 25f)]
        public float leanSpineRollDegrees = 6f;

        [Tooltip("Also apply the extra spine roll while strafing / aiming.")]
        public bool leanSpineRollWhileStrafing = false;

        [Header("Steps & slopes (on foot)")]
        [Tooltip("Master switch for walking up stairs, curbs, rocks and building steps without jumping.")]
        public bool stepAssistEnabled = true;

        [Tooltip("Tallest step (meters) the player walks up. Pushed to Invector stepOffsetMaxHeight.")]
        [Range(0.05f, 1.2f)]
        public float stepMaxHeight = 0.5f;

        [Tooltip("Ledges lower than this (meters) are ignored by the step probe. Invector stepOffsetMinHeight.")]
        [Range(0f, 0.5f)]
        public float stepMinHeight = 0f;

        [Tooltip("How far past the capsule edge (meters) the step probe looks ahead. Invector stepOffsetDistance.")]
        [Range(0.02f, 0.8f)]
        public float stepProbeDistance = 0.1f;

        [Tooltip("Step onto everything you can stand on (ground layers + Building), not only Default/Climbable. Needed for built stairs.")]
        public bool stepOnGroundLayers = true;

        [Tooltip("Steepest ground (degrees from flat) you can walk up and stand on. Pushed to Invector slopeLimit.")]
        [Range(10f, 85f)]
        public float walkableSlopeDegrees = 75f;

        [Tooltip("Extra lift helper for steps with angled, chamfered or rough front faces, as long as the top of the step is walkable.")]
        public bool stepLiftHelper = true;

        [Tooltip("A face steeper than this (degrees from flat) counts as a step riser. Gentler faces are left to normal ramp walking.")]
        [Range(20f, 90f)]
        public float stepRiserMinAngle = 50f;

        [Tooltip("How fast (m/s) the helper lifts the player onto a step.")]
        [Range(0.5f, 10f)]
        public float stepLiftSpeed = 4f;

        [Tooltip("Forward push (m/s) while lifting so the feet land on the step top.")]
        [Range(0f, 3f)]
        public float stepLiftForwardPush = 0.8f;

        [Tooltip("Ignore Invector's steep-slope block while a valid step is ahead, so angled riser faces don't stop you.")]
        public bool stepIgnoreSlopeBlock = true;

        [Header("Survival â€” pools (authority)")]
        public float maxHealth = 100f;
        public float maxEnergy = 100f;
        public float maxStamina = 100f;
        public float maxOxygen = 2400f;

        [Header("Survival - passive drain")]
        [Tooltip("Passive walk/idle energy drain. Tool use (jetpack, mining, scanner) uses toolEnergyDrainPerSecond instead.")]
        public float energyDrainPerSecond;
        [Tooltip("Energy drained per second while jetpack, mining tool, or scanner is active.")]
        public float toolEnergyDrainPerSecond = 0.5f;
        [Tooltip("Energy spent per output item when crafting away from a workbench.")]
        public float handCraftEnergyPerItem = 0.5f;
        public float oxygenDrainPerSecond = 4f;

        [Header("Survival - low oxygen warning")]
        [Tooltip("Show the center-screen Low Oxygen toast when oxygen is at or below this percent (0-100).")]
        [Range(1f, 50f)]
        public float lowOxygenWarningPercent = 10f;
        [Tooltip("Flash the Low Oxygen toast this many times per second while below the threshold.")]
        [Min(0.25f)]
        public float lowOxygenFlashPerSecond = 1.5f;
        [Tooltip("Center-screen warning copy.")]
        public string lowOxygenWarningText = "Low Oxygen";
        public bool lowOxygenWarningEnabled = true;

        [Tooltip("Health drained per second while energy is below lowStatThresholdPercent.")]
        public float healthDrainPerSecond = 2f;
        [Range(1f, 99f)]
        public float lowStatThresholdPercent = 25f;
        public float oxygenDepletedHealthDrainMultiplier = 5f;

        [Header("Survival - health regen")]
        public bool enableHealthRegen;
        public float healthRegenPerSecond = 1f;
        public float healthRegenDelayAfterDamage = 5f;

        [Header("Survival â€” exposure caps & recovery")]
        public float maxThermalStress = 100f;
        public float maxRadiation = 100f;
        public float maxSulfur = 100f;
        public float maxVolcano = 100f;
        public float exposureRecoveryPerSecond = 8f;
        public float thermalRecoveryPerSecond = 12f;

        [Header("Stamina â€” idle")]
        [Tooltip("Passive regen when not sprinting, climbing, dashing, or in dash recovery.")]
        public float idleStaminaRegenPerSecond = 3f;

        [Header("Stamina â€” sprint")]
        public float sprintStaminaDrainPerSecond = 12f;
        [Tooltip("Regen while sprinting (usually 0).")]
        public float sprintStaminaRegenPerSecond;

        [Header("Stamina â€” climb")]
        [Tooltip("Flat stamina spent when attaching to a wall.")]
        public float climbStartStaminaCost = 5f;
        public float climbMoveStaminaDrainPerSecond = 8f;
        public float climbHangStaminaDrainPerSecond = 4f;
        [Tooltip("Regen while climbing (usually 0).")]
        public float climbStaminaRegenPerSecond;

        [Header("Stamina â€” dash")]
        [Tooltip("Flat stamina spent when a dash starts.")]
        public float dashStaminaCost = 22f;
        [Tooltip("Extra stamina drained over the dash duration, as a fraction of dashStaminaCost (0.2 = +20%).")]
        [Range(0f, 1f)]
        public float dashStaminaTickExtraPercent = 0.20f;
        [Tooltip("Regen while the dash slide is active (usually 0).")]
        public float dashStaminaRegenPerSecond;
        [Tooltip("Regen per second for a short window after the dash ends.")]
        public float dashRecoveryStaminaRegenPerSecond = 1.5f;
        [Tooltip("Seconds after dash ends where only dashRecoveryStaminaRegenPerSecond applies.")]
        public float dashStaminaRecoverySeconds = 2.5f;

        [Header("Dash â€” input & motion")]
        [Tooltip("Max seconds between two taps of the same WASD key.")]
        public float dashDoubleTapWindow = 0.28f;

        [Tooltip("How far the double-tap slide travels, in meters.")]
        [Range(1f, 25f)]
        public float dashDistance = 4.5f;

        [Tooltip("Dash move speed in meters per second.")]
        [Range(4f, 40f)]
        public float dashSpeed = 15.4f;

        [Tooltip("Fallback only when distance and speed are unset. Live duration is Distance / Speed.")]
        public float dashDuration = 0.18f;

        [Tooltip("Seconds before another dash can start.")]
        public float dashCooldown = 0.55f;

        [Tooltip("Allow double-tap dash while airborne without the Air Dash skill.")]
        public bool dashAllowAirDash;

        [Tooltip("0 freezes walk/run anim speed during the slide. 1 is normal.")]
        [Range(0f, 1f)]
        public float dashAnimationSpeed;

        [Tooltip("Skin applied when capsule-casting so dashes stop before penetrating colliders.")]
        public float dashCollisionSkin = 0.08f;

        [Tooltip("Double-tap dash while climbing cancels climb and slides off the wall.")]
        public bool dashDetachesFromClimb = true;

        [Tooltip("Allow double-tap dash input while clinging to a wall.")]
        public bool dashAllowedWhileClimbing = true;

        [Header("Dash â€” hologram")]
        public Color dashHologramColor = new Color(0.25f, 0.85f, 1f, 0.42f);
        public float dashHologramEmission = 4f;
        [Tooltip("Optional override. If empty, an HDRP unlit hologram is built at runtime.")]
        public Material dashHologramMaterial;

        [Header("Dash â€” speed streaks")]
        public Color dashStreakColor = new Color(0.55f, 0.95f, 1f, 0.85f);
        public int dashStreakCount = 18;
        public float dashStreakLifetime = 0.22f;
        public float dashStreakSize = 0.08f;
        public float dashStreakStretch = 3.5f;
        public float dashStreakRadius = 0.55f;
        public Material dashStreakMaterial;
        public GameObject dashStreakPrefab;

        [Header("Dash â€” smoke")]
        public Color dashSmokeColor = new Color(0.55f, 0.62f, 0.7f, 0.45f);
        public int dashSmokeCount = 14;
        public float dashSmokeLifetime = 0.55f;
        public float dashSmokeSize = 0.7f;
        public Material dashSmokeMaterial;
        public GameObject dashSmokePrefab;

        [Header("Surface")]
        public string climbableLayerName = "Climbable";
        public string climbableTag = "Climbable";

        [Tooltip("Only these layers. 0 = resolve from climbableLayerName.")]
        public LayerMask climbableLayers;

        [Header("ClingSense bubble")]
        [Tooltip("Body-centered overlap + ray fan for face/soffit/ground/stub-lip/sides (Dune-style volume feel).")]
        public bool enableClingSense = true;

        [Tooltip("Draw ClingSense bubble in Scene view while climbing (Gizmos must be enabled). Also draws when the climb controller is selected.")]
        public bool drawClingSenseGizmos = true;

        [Header("Baked probes")]
        [Tooltip("When a DMClimbProbeSet is on/near the climbable, prefer nearest baked probe for attach + StickAndMove. Mesh lip fallback remains.")]
        public bool preferBakedProbes = false;

        [Tooltip("Max distance from body/hands to consider a baked probe for attach and cling.")]
        [Range(0.4f, 3.5f)]
        public float probeReach = 1.55f;

        [Tooltip("Max step between neighboring stance pairs while climbing (WASD). Auto-raises to nearest neighbor if bake spacing is wider. Runtime also caps at 0.85m.")]
        [Range(0.35f, 3.5f)]
        public float probeStepMax = 1.6f;

        /// <summary>Jump height for the current on-foot gait (Genesis Studio â†’ Survival â†’ Walk / Run / Sprint).</summary>
        public float ResolveJumpHeight(DMLocomotionGaitController.Gait gait, bool moving, float horizontalSpeed01 = 0f)
        {
            float h = Mathf.Max(0.35f, standingJumpHeight);
            if (!moving)
                return h;

            float mult = gait switch
            {
                DMLocomotionGaitController.Gait.SprintBurst => sprintJumpHeightMultiplier,
                DMLocomotionGaitController.Gait.Jog => jogJumpHeightMultiplier,
                _ => walkJumpHeightMultiplier
            };

            if (jumpHeightSpeedExponent > 0.001f)
            {
                float t = Mathf.Clamp01(horizontalSpeed01);
                float maxBoost = Mathf.Exp(jumpHeightSpeedExponent);
                mult *= Mathf.Pow(maxBoost, t);
            }

            return h * mult;
        }

        /// <summary>Overwrites live <see cref="SurvivalStats"/> tuning from this profile (pools, drains, regen).</summary>
        public void ApplyToSurvivalStats(SurvivalStats stats)
        {
            if (stats == null)
                return;

            stats.maxHealth = Mathf.Max(1f, maxHealth);
            stats.maxEnergy = Mathf.Max(1f, maxEnergy);
            stats.maxStamina = Mathf.Max(1f, maxStamina);
            stats.maxOxygen = Mathf.Max(1f, maxOxygen);

            stats.energyDrain = energyDrainPerSecond;
            stats.toolEnergyDrainPerSecond = toolEnergyDrainPerSecond;
            stats.handCraftEnergyPerItem = handCraftEnergyPerItem;
            stats.oxygenDrainPerSecond = oxygenDrainPerSecond;
            stats.healthDrain = healthDrainPerSecond;
            stats.lowStatThreshold = lowStatThresholdPercent;
            stats.oxygenDepletedHealthDrainMultiplier = oxygenDepletedHealthDrainMultiplier;

            stats.enableHealthRegen = enableHealthRegen;
            stats.healthRegenPerSecond = healthRegenPerSecond;
            stats.healthRegenDelayAfterDamage = healthRegenDelayAfterDamage;

            stats.maxThermalStress = maxThermalStress;
            stats.maxRadiation = maxRadiation;
            stats.maxSulfur = maxSulfur;
            stats.maxVolcano = maxVolcano;
            stats.exposureRecoveryPerSecond = exposureRecoveryPerSecond;
            stats.thermalRecoveryPerSecond = thermalRecoveryPerSecond;

            stats.staminaRegenPerSecond = idleStaminaRegenPerSecond;
            stats.sprintStaminaDrainPerSecond = sprintStaminaDrainPerSecond;
        }
    }
}
