using UnityEngine;

namespace Project.Features.Climb
{
    [CreateAssetMenu(menuName = "Dark Matter/Player/Climb Profile", fileName = "DMClimbProfile")]
    public sealed class DMClimbProfile : ScriptableObject
    {
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

        [Tooltip("Grab when airborne and a climbable is in attachRange. Off — start climb is second Space + forward.")]
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
        public float climbLeapSpeed = 7.2f;

        [Tooltip("Cling Space-hop height (meters). Hold Space longer to go further, up to this.")]
        [Range(0.5f, 12f)]
        public float clingHop = 12f;

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

        [Tooltip("From a walkable ledge top: S toward the drop (facing out) or E near the lip reverse-mantles into hang on the outer lip (180 to face the wall), then normal climb. Does not auto-trigger on walk-near-edge.")]
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

        [Tooltip("Planar lip distance from the climb wall that counts as deep protrusion. Drives short-hop grab — not vertical thickness.")]
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

        [Tooltip("Seconds to lerp hang → lip → stand.")]
        [Range(0.5f, 2.4f)]
        public float mantleSeconds = 1.4f;

        [Tooltip("Ignore fall-land / ragdoll this long after a mantle.")]
        [Range(0.5f, 4f)]
        public float mantleIgnoreLands = 2.6f;

        [Header("Landing")]
        [Tooltip("Shorter than this is a regular Invector hop. Mid drops and jetpack use hero / Jetpack Land.")]
        [Range(0.5f, 8f)]
        public float heroDropMeters = 2.6f;

        [Tooltip("Fall this far or more (while falling) is death + retry/quit, unless jetpack grace applies.")]
        [Range(8f, 200f)]
        public float lethalDropMeters = 100f;

        [Tooltip("After jetpack Space/boost is released, wait this many seconds before a lethal-height fall can kill. Still boosting, or land inside this window, is always hero.")]
        [Range(0f, 20f)]
        public float jetpackLethalDelay = 6f;

        [Header("Stamina")]
        [Tooltip("Flat stamina spent when attaching to a wall.")]
        public float climbStartStaminaCost = 5f;
        [Tooltip("Legacy fallback only. Live drain is maxStamina/unlockedStaminaDashes per second (half when hanging).")]
        public float climbStaminaDrainPerSecond = 8f;

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
    }
}
