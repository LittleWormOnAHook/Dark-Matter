using Invector.vCharacterController;
using Project.Core;
using Project.Features.Dash;
using Project.Features.Jetpack;
using Project.Player;
using Project.Progression;
using Project.Survival;
using Project.Vehicles;
using Project.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.InputSystem;

namespace Project.Features.Climb
{
    /// <summary>
    /// Climb manager on the player. Assign a Climb Profile in the inspector.
    /// First Space jumps. Second Space + forward sticks to a Climbable. E drops.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-20)]
    public sealed class DMClimbController : MonoBehaviour
    {
        public const string ResourcesPath = "Climb/DMClimbProfile";
        private const string BuildStamp = "DMClimb wall-grab-v1";
        // DMClimb probe-locomotion-v7

        private static readonly int ClimbXHash = Animator.StringToHash("ClimbX");
        private static readonly int ClimbYHash = Animator.StringToHash("ClimbY");
        private static readonly int ClimbSpeedHash = Animator.StringToHash("ClimbSpeed");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int MantleHash = Animator.StringToHash("Mantle");

        [Header("Climb Manager")]
        [SerializeField] private DMClimbProfile profile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody body;
        [SerializeField] private DMDashController dash;
        [SerializeField] private DMLandingDirector landing;
        [SerializeField] private DMJetpackController jetpack;

        private bool _climbing;
        private bool _heldLockMovement;
        private bool _heldLockAnimMovement;
        private bool _heldDisableCheckGround;
        private bool _heldApplyRootMotion;
        private bool _heldCcEnabled;
        private bool _heldCapsuleTrigger;
        private bool _heldCapsuleEnabled;
        private bool _heldKinematic;
        private bool _heldUseGravity;
        private bool _motorOverridden;
        private CharacterController _character;
        private CapsuleCollider _capsule;
        private SurvivalStats _survival;

        private Transform _anchor;
        private Vector3 _localOffset;
        private Vector3 _prevAnchorPos;
        private bool _hasPrevAnchorPos;
        private Vector3 _platformVel;
        private Vector3 _lastNormal = Vector3.back;
        private Vector3 _dampedClimbInput;
        private Vector3 _dampedClimbVel;
        private bool _hopNeedsJumpRelease;
        private float _airJumpGrabUntil = -10f;
        private float _attachedAt = -10f;
        private float _detachedAt = -10f;
        private float _walkOffSuppressUntil = -10f;
        private bool _wasGroundedForWalkOff = true;
        private float _stickLostAt = -10f;
        private float _leapUntil = -10f;
        private bool _leapRegrab;
        private bool _hopping;
        private Vector3 _hopVel;
        private Vector2 _hopAxes;
        private float _hopUntil = -10f;
        private float _hopChargeAt = -10f;
        private bool _lipHang;
        private bool _overhangHang;
        private bool _overhangGrabbing;
        private float _overhangGrabAt = -10f;
        private Vector3 _overhangGrabStart;
        private Vector3 _overhangGrabEnd;
        private Vector3 _overhangGrabMid;
        private float _overhangGrabDur = 0.35f;
        private float _overhangProtrusion;
        private bool _overhangDeepHop;
        private bool _overhangPreferMantle;
        private float _overhangResumeAt = -10f;
        private RaycastHit _overhangLip;
        private readonly DMClimbClingSense _clingSense = new DMClimbClingSense();
        private DMClimbClingSense.Sample _cling;
        private float _clingLogAt = -10f;
        private const float OverhangMaxGrab = 1.85f;
        private const float OverhangMaxLift = 1.2f;
#if UNITY_EDITOR
        private Vector3 _gizmoOverhangOrigin;
        private Vector3 _gizmoOverhangLip;
        private bool _gizmoOverhangValid;
#endif
        // free-climb-dune-v4: double-tap W to start climb.
        private float _climbWTapAt = -10f;
        private int _climbWTapCount;
        private bool _climbWWasDown;
        private float _leapArmedAt = -10f;
        private float _airControlUntil = -10f;
        private float _airApexY;
        private bool _trackedAir;
        private Vector3 _lastWallPoint;
        private Vector3 _lastWallNormal = Vector3.back;
        private int _climbLayerIndex = -1;
        private bool _hasClimbX;
        private bool _hasClimbY;
        private bool _hasClimbSpeed;
        private bool _hasIsClimbing;
        private bool _hasMantle;
        private LayerMask _mask;
        private string _tagName = "Climbable";
        private readonly Collider[] _overlapBuf = new Collider[16];
        private Vector3 _mantleStand;
        private float _mantleFloorY;
        private RaycastHit _lastStickHit;
        private bool _hasLastStick;
        private bool _mantling;
        private float _mantleUntil = -10f;
        private float _mantleBeganAt;
        private Quaternion _mantleStartRot = Quaternion.identity;
        private Quaternion _mantleEndRot = Quaternion.identity;
        private Vector3 _mantleStart;
        private Vector3 _mantleLip;
        private Vector3 _mantleOver;
        private bool _mantleSettling;
        private float _mantleSettleAt = -10f;
        private float _suppressRootMotionUntil = -10f;
        private float _suppressAnimMoveUntil = -10f;
        private bool _reverseMantling;
        private float _reverseMantleAt = -10f;
        private float _reverseMantleDur = 0.55f;
        private Vector3 _reverseMantleStart;
        private Vector3 _reverseMantleLip;
        private Vector3 _reverseMantleHang;
        private Quaternion _reverseMantleStartRot = Quaternion.identity;
        private Quaternion _reverseMantleEndRot = Quaternion.identity;
        private RaycastHit _reverseMantleFace;
        private Vector3 _ikLeft;
        private Vector3 _ikRight;
        private float _ikWeight;
        private bool _ikValid;
        private int _ikAppliedFrame = -1;
        private Transform _leftHand;
        private Transform _rightHand;
        private Transform _leftGrab;
        private Transform _rightGrab;
        private readonly RaycastHit[] _castHits = new RaycastHit[24];

        // Baked probe locomotion (DMClimb probe-locomotion-v7)
        private DMClimbProbeSet _probeSet;
        private int _probeIndex = -1;
        private DMClimbProbeSet.ProbeType _probeType = DMClimbProbeSet.ProbeType.Face;
        private bool _probeMoving;
        private Vector3 _probeMoveStart;
        private Vector3 _probeMoveEnd;
        private Vector3 _probeMoveNormal;
        private float _probeMoveAt = -10f;
        private float _probeMoveDur = 0.18f;
        private float _probeStepReadyAt = -10f;
        private float _probeMissLogAt = -10f;
        private float _probeBindLogAt = -10f;
        private float _probeNoReverseUntil = -10f;
        private Vector3 _probeLastStepFlat;
        private Vector2 _probeMoveAnimAxes;
        private int _probeMoveTarget = -1;

        public bool IsClimbing => _climbing || _hopping || _mantling || _reverseMantling;
        public bool IsMantling => _mantling;
        private float MantlePlantPad => profile != null ? profile.mantlePlantHeight : 0f;
        public DMClimbProfile Profile => profile;

        public void CancelClimb()
        {
            if (_climbing || _hopping || _reverseMantling || _motorOverridden)
                ForceUnlock();
        }

        /// <summary>Retry/death: unlock climb leftovers and stand world-up.</summary>
        public void RestoreAfterDeathOrRetry()
        {
            _climbing = false;
            _hopping = false;
            _mantling = false;
            _reverseMantling = false;
            _leapRegrab = false;
            _motorOverridden = true;
            ForceUnlock();
            if (animator != null && !animator.enabled)
                animator.enabled = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
            if (player == null || player.GetComponent<DMClimbController>() != null)
                return;

            player.AddComponent<DMClimbController>();
        }

        private void Awake()
        {
            if (_survival == null)
                _survival = ResolveSurvivalStats();
            if (profile == null)
                profile = Resources.Load<DMClimbProfile>(ResourcesPath);
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (dash == null)
                dash = GetComponent<DMDashController>();
            if (landing == null)
                landing = GetComponent<DMLandingDirector>();
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();

            _character = GetComponent<CharacterController>();
            _capsule = GetComponent<CapsuleCollider>();
            EnsureClimbableIsGround();
            ResolveMask();
            CacheAnimator();
            IdleAnimator();
        }

        private void Start()
        {
            // Startup stamp silenced.
        }

        private void OnDisable()
        {
            if (_climbing || _hopping || _motorOverridden)
                ForceUnlock();
        }

        private void Update()
        {
            if (_hopping)
            {
                if (ReadInteractPressedThisFrame() && !UiBlocksClimbDrop())
                    EndHop(drop: true);
                else
                    TryLeapRegrab();
                return;
            }

            if (_reverseMantling)
            {
                if (ReadInteractPressedThisFrame() && !UiBlocksClimbDrop())
                {
                    _reverseMantling = false;
                    DropFromClimb();
                }
                return;
            }

            if (_mantling)
            {
                if (ReadInteractPressedThisFrame() && !UiBlocksClimbDrop())
                    DropFromClimb();
                return;
            }

            if (_climbing)
            {
                if (ReadInteractPressedThisFrame() && !UiBlocksClimbDrop())
                {
                    _hopChargeAt = -10f;
                    DropFromClimb();
                    return;
                }

                // Space hop into a soffit parks you under the slab with only E to leave.
                // Jump lets go here (same as E). Normal wall cling still charges a hop.
                if (IsParkedUnderLedge())
                {
                    _hopChargeAt = -10f;
                    if (ReadJumpHeld())
                        DropFromClimb();
                    return;
                }

                // wall-grab-v1: Space that stuck you must release before a cling-hop can charge.
                if (_hopNeedsJumpRelease)
                {
                    if (!ReadJumpHeld())
                        _hopNeedsJumpRelease = false;
                    _hopChargeAt = -10f;
                    return;
                }

                if (ReadJumpHeld())
                {
                    if (_hopChargeAt < 0f)
                        _hopChargeAt = Time.unscaledTime;
                }
                else if (_hopChargeAt > 0f)
                {
                    float held = Time.unscaledTime - _hopChargeAt;
                    _hopChargeAt = -10f;
                    if (Time.unscaledTime - _attachedAt > 0.2f)
                        ClimbLeap(held);
                }
                return;
            }

            if (_leapRegrab && Time.unscaledTime > _leapUntil)
                _leapRegrab = false;

            // One jump into a wall: arm a short air-grab window on Space press.
            if (ReadJumpPressedThisFrame())
                _airJumpGrabUntil = Time.unscaledTime + 1.15f;

            TrackFallApex();
            TickDoubleWClimbStart();

            if (TryLeapRegrab())
                return;

            // Grounded climb-down: S toward edge or E near lip -> reverse mantle into hang.
            if (TryStartDropToHangFromTop())
                return;

            FailsafeUnlocked();
        }

        private void FixedUpdate()
        {
            ResolveMask();

            if (animator != null && Time.unscaledTime < _suppressRootMotionUntil)
                animator.applyRootMotion = false;

            // v2j: kill post-mantle locomotion footstep (anim delta), not a scripted slide.
            if (motor != null && Time.unscaledTime < _suppressAnimMoveUntil)
            {
                motor.lockAnimMovement = true;
                if (animator != null)
                    animator.applyRootMotion = false;
                SafeZeroVelocity();
                SetPlanarVelocity(Vector3.zero);
            }

            if (_reverseMantling)
                TickReverseMantle();
            else if (_mantling)
                TickMantle();
            else if (_hopping)
                TickHop();
            else if (_climbing)
            {
                TickClimbStamina();
                if (!_climbing)
                    return;
                StickAndMove();
            }
            else
            {
                if (_survival != null)
                    _survival.suppressStaminaRegen = false;
                if (TryAirJumpWallGrab())
                    return;
                if (TryJetpackWallGrab())
                    return;
                SteerAirControl();
            }
        }

        /// <summary>Jump: drop if climbing, else attach if a climbable is in front. Never eats a normal jump.</summary>
        public bool TryHandleJumpPress()
        {
            if (!isActiveAndEnabled)
                return false;

            bool space = ReadJumpPressedThisFrame();

            if (_hopping)
            {
                TryLeapRegrab();
                return true;
            }

            if (_climbing)
            {
                // Space is owned by Update: charge a cling hop, or mantle at a lip.
                return true;
            }

            if (!space)
                return false;

            if (TryLeapRegrab())
                return true;

            // First Space is the jump. Stick only on a second Space while
            // airborne and pushing forward, so jetpack still owns Space
            // when you are not driving at a wall.
            if (profile != null && profile.startClimbNeedsAirborne && motor != null && motor.isGrounded)
                return false;

            Vector2 axes = ReadClimbAxes();
            // free-climb-dune-v2: lighter W gate so second Space near a face latches easier.
            if (profile != null && profile.startClimbNeedsForward && axes.y <= 0.08f)
                return false;

            if (!CanAttachNow(ignoreBuffer: true))
                return false;

            if (TryJumpToWall(out RaycastHit hit))
            {
                if (!HasClimbStartStamina())
                    return false;
                if (!TryPayClimbStartStamina())
                    return false;
                Attach(hit);
                return true;
            }

            return false;
        }

        private void ResolveMask()
        {
            _tagName = profile != null && !string.IsNullOrWhiteSpace(profile.climbableTag)
                ? profile.climbableTag
                : "Climbable";

            if (profile != null && profile.climbableLayers.value != 0)
            {
                _mask = profile.climbableLayers;
                return;
            }

            string layerName = profile != null ? profile.climbableLayerName : "Climbable";
            int layer = LayerMask.NameToLayer(layerName);
            _mask = layer >= 0 ? 1 << layer : 0;
        }

        private void CacheAnimator()
        {
            _hasClimbX = false;
            _hasClimbY = false;
            _hasClimbSpeed = false;
            _hasIsClimbing = false;
            _hasMantle = false;
            _climbLayerIndex = -1;
            if (animator == null)
                return;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                int hash = animator.GetParameter(i).nameHash;
                if (hash == ClimbXHash)
                    _hasClimbX = true;
                else if (hash == ClimbYHash)
                    _hasClimbY = true;
                else if (hash == ClimbSpeedHash)
                    _hasClimbSpeed = true;
                else if (hash == IsClimbingHash)
                    _hasIsClimbing = true;
                else if (hash == MantleHash)
                    _hasMantle = true;
            }

            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetLayerName(i) == "Climb")
                {
                    _climbLayerIndex = i;
                    break;
                }
            }

            DMClimbHandIK relay = animator.GetComponent<DMClimbHandIK>();
            if (relay == null)
                relay = animator.gameObject.AddComponent<DMClimbHandIK>();
            relay.owner = this;

            _leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            _leftGrab = EnsureGrabSocket(_leftHand, "LeftHandGrab");
            _rightGrab = EnsureGrabSocket(_rightHand, "RightHandGrab");
        }

        private static Transform EnsureGrabSocket(Transform bone, string name)
        {
            if (bone == null)
                return null;
            Transform existing = bone.Find(name);
            if (existing != null)
                return existing;
            GameObject go = new GameObject(name);
            go.transform.SetParent(bone, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.045f);
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        private void IdleAnimator()
        {
            WriteAnimator(Vector2.zero, 0f, climbing: false);
            SetClimbLayerWeight(0f);
        }

        private bool CanAttachNow(bool ignoreBuffer = false)
        {
            if (!isActiveAndEnabled)
                return false;
            if (profile == null || Time.timeScale <= 0f)
                return false;
            if (PlayerVehicleState.IsMounted)
                return false;
            if (dash != null && dash.IsDashing)
                return false;
            if (landing != null && landing.IsLandingLocked)
                return false;
            if (!ignoreBuffer && InsideDetachBuffer())
                return false;
            return true;
        }




        private bool TryJumpToWall(out RaycastHit hit)
        {
            hit = default;
            bool airborne = motor == null || !motor.isGrounded;
            float jumpRange = profile != null
                ? (airborne ? Mathf.Max(profile.attachRange, profile.climbJumpRange) : profile.attachRange)
                : 2.4f;
            float radius = profile != null ? profile.probeRadius : 0.18f;
            Vector3 wish = WishJumpDir();
            Vector3 right = Vector3.Cross(Vector3.up, wish);
            if (right.sqrMagnitude < 0.001f)
                right = transform.right;
            right.Normalize();

            Vector3[] origins =
            {
                ChestOrigin(),
                transform.position + Vector3.up * 0.7f,
                transform.position + Vector3.up * 1.45f,
            };
            Vector3[] dirs =
            {
                wish,
                (wish + Vector3.up * 0.25f).normalized,
                (wish + Vector3.up * 0.55f).normalized,
                Flatten(transform.forward),
                (wish + right * 0.3f).normalized,
                (wish - right * 0.3f).normalized,
            };

            bool any = false;
            for (int o = 0; o < origins.Length; o++)
            {
                for (int i = 0; i < dirs.Length; i++)
                {
                    Vector3 dir = dirs[i];
                    if (dir.sqrMagnitude < 0.001f)
                        continue;
                    if (!TryProbeRange(origins[o], dir, jumpRange, radius, out RaycastHit cand))
                        continue;
                    if (!IsClimbableHit(cand) || !IsClimbableSlope(cand.normal))
                        continue;
                    Vector3 into = -Flatten(cand.normal);
                    if (Vector3.Dot(wish, into) < 0.18f && Vector3.Dot(Flatten(transform.forward), into) < 0.18f)
                        continue;
                    if (!any || cand.distance < hit.distance)
                        hit = cand;
                    any = true;
                }
            }

            return any;
        }

        private Vector3 WishJumpDir()
        {
            Vector2 axes = ReadClimbAxes();
            Vector3 wish = Flatten(transform.right * axes.x + transform.forward * axes.y);
            if (wish.sqrMagnitude < 0.04f)
                return Flatten(transform.forward);
            return wish.normalized;
        }

        private void FailsafeUnlocked()
        {
            if (_climbing || _hopping || _mantling)
                return;
            if (IsRagdollOrDead())
                return;

            bool stuckKinematic = body != null && body.isKinematic;
            bool stuckTrigger = _capsule != null && _capsule.isTrigger;
            if (_motorOverridden || stuckKinematic || stuckTrigger)
                ForceUnlock();
            else if (_hasIsClimbing && animator != null && animator.GetBool(IsClimbingHash))
                IdleAnimator();
        }

        private bool IsRagdollOrDead()
        {
            if (motor != null && (motor.ragdolled || motor.isDead))
                return true;
            if (landing != null && landing.IsHardFalling)
                return true;
            return false;
        }

        private static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
        }

        private bool TryProbe(Vector3 hintForward, out RaycastHit best)
        {
            best = default;
            ResolveMask();

            Vector3 origin = ChestOrigin();
            Vector3 forward = hintForward.sqrMagnitude > 0.001f ? hintForward.normalized : transform.forward;
            float range = profile != null ? profile.attachRange : 1.4f;
            float radius = profile != null ? profile.probeRadius : 0.18f;

            if (!TryProbeRange(origin, forward, range, radius, out best))
                return false;
            if (!IsClimbableHit(best) || !IsClimbableSlope(best.normal))
                return false;

            // DMClimb probe-bake-v1 — prefer nearest baked hold when present; mesh lip stays fallback.
            TrySnapToBakedProbe(ref best, origin, forward, range);

            float faceDot = Vector3.Dot(transform.forward, -best.normal);
            float minDot = profile != null ? profile.faceDotMin : 0.2f;
            return faceDot >= minDot * 0.5f;
        }

        /// <summary>DMClimb probe-locomotion-v1: if hit has a DMClimbProbeSet and preferBakedProbes, snap point/normal to nearest facing probe and bind locomotion state.</summary>
        private bool TrySnapToBakedProbe(ref RaycastHit hit, Vector3 fromPoint, Vector3 fromDir, float maxDistance)
        {
            if (profile != null && !profile.preferBakedProbes)
                return false;
            if (hit.collider == null)
                return false;

            float reach = ProbeReachDistance(maxDistance);
            DMClimbProbeSet set = hit.collider.GetComponentInParent<DMClimbProbeSet>();
            if (set == null || set.Count == 0)
                return false;

            int idx;
            Vector3 pos;
            Vector3 n;
            float radius;
            DMClimbProbeSet.ProbeType type;
            if (!set.FindNearestFacingProbe(fromPoint, fromDir, reach, out idx, out pos, out n, out radius, out type))
            {
                // Hang/overhang holds may face away from approach — allow nearest.
                if (!set.FindNearestProbe(fromPoint, reach, out idx, out pos, out n, out radius, out type))
                    return false;
            }

            hit.point = pos;
            if (n.sqrMagnitude > 0.0001f)
                hit.normal = n.normalized;
            BindProbe(set, idx, type, pos, n);
            return true;
        }

        private float ProbeReachDistance(float fallback)
        {
            if (profile == null)
                return fallback > 0f ? fallback : 1.55f;
            float r = profile.probeReach > 0.05f ? profile.probeReach : 1.55f;
            if (fallback > 0f)
                r = Mathf.Max(r, fallback);
            return r;
        }

                private float ProbeStepMax()
        {
            // Profile default; runtime also auto-scales from the live ProbeSet spacing.
            float v = profile != null && profile.probeStepMax > 0.05f ? profile.probeStepMax : 1.6f;
            return Mathf.Clamp(v, 0.5f, 2.75f);
        }

        /// <summary>
        /// Step reach from current hold: at least profile stepMax, and enough to reach the nearest
        /// other stance (pair mid / probe) so baker "Distance Between Pairs" cannot soft-lock traverse.
        /// </summary>
        private float ProbeStepMaxForSet(DMClimbProbeSet set, Vector3 fromPos, int fromIndex, Vector3 preferredDir = default)
        {
            float step = ProbeStepMax();
            if (set == null || set.Count < 2)
                return step;

            float nearestAny = float.MaxValue;
            float nearestAlong = float.MaxValue;
            bool hasPref = preferredDir.sqrMagnitude > 0.0001f;
            Vector3 pref = hasPref ? preferredDir.normalized : Vector3.zero;
            int skipPair = -1;
            if (fromIndex >= 0 && fromIndex < set.Count)
                skipPair = set.Probes[fromIndex].pairId;

            Vector3 origin = fromPos;
            if (fromIndex >= 0)
            {
                Vector3 mid = default;
                if (set.TryGetPairWorldPoses(fromIndex, out _, out _, out _, out _, out mid, out _))
                    origin = mid;
            }

            for (int i = 0; i < set.Count; i++)
            {
                if (i == fromIndex)
                    continue;
                if (skipPair >= 0 && set.Probes[i].pairId == skipPair)
                    continue;
                if (!set.GetWorldPose(i, out Vector3 pos, out _, out _, out _))
                    continue;
                Vector3 target = pos;
                if (set.TryGetPairWorldPoses(i, out _, out _, out _, out _, out Vector3 omid, out _))
                    target = omid;
                float d = Vector3.Distance(origin, target);
                if (d <= 0.05f)
                    continue;
                if (d < nearestAny)
                    nearestAny = d;
                if (hasPref)
                {
                    Vector3 to = (target - origin).normalized;
                    // Soft hemisphere: lateral/diagonal neighbors must expand reach even when vertical is closer.
                    if (Vector3.Dot(to, pref) > 0.05f && d < nearestAlong)
                        nearestAlong = d;
                }
            }

            if (nearestAlong < float.MaxValue * 0.5f)
                step = Mathf.Max(step, nearestAlong * 1.25f + 0.15f);
            else if (nearestAny < float.MaxValue * 0.5f)
                step = Mathf.Max(step, nearestAny * 1.2f + 0.1f);
            return Mathf.Clamp(step, 0.5f, 2.85f);
        }

        private void BindProbe(DMClimbProbeSet set, int index, DMClimbProbeSet.ProbeType type, Vector3 worldPos, Vector3 worldNormal)
        {
            _probeSet = set;
            _probeIndex = index;
            _probeType = type;
            _probeMoving = false;
            _probeMoveTarget = -1;
            if (worldNormal.sqrMagnitude > 0.0001f)
            {
                Vector3 n = worldNormal.normalized;
                // Snap facing immediately when the hold's normal changes (angled corner faces).
                float nDelta = _lastNormal.sqrMagnitude > 0.0001f ? Vector3.Angle(_lastNormal, n) : 180f;
                _lastNormal = n;
                if (nDelta > 12f)
                    FaceWall(n);
            }
            if (_hasLastStick)
            {
                _lastStickHit.point = worldPos;
                if (worldNormal.sqrMagnitude > 0.0001f)
                    _lastStickHit.normal = worldNormal.normalized;
            }

            if (Time.unscaledTime - _probeBindLogAt > 1.25f)
            {
                _probeBindLogAt = Time.unscaledTime;
                int count = set != null ? set.Count : 0;
                Debug.Log($"[{BuildStamp}] bind idx={index} count={count} type={type}");
            }
        }

        private void ClearProbeState()
        {
            _probeSet = null;
            _probeIndex = -1;
            _probeType = DMClimbProbeSet.ProbeType.Face;
            _probeMoving = false;
            _probeMoveTarget = -1;
        }

        private bool PreferBakedProbes()
        {
            // free-climb-dune-v1: Dune/Conan surface climb owns locomotion.
            // Probe baker assets may remain, but never own StickAndMove.
            return false;
        }

        private bool TryResolveProbeSetNear(Collider col, out DMClimbProbeSet set)
        {
            set = null;
            if (col != null)
            {
                set = col.GetComponentInParent<DMClimbProbeSet>();
                if (set != null && set.Count > 0)
                    return true;
            }
            if (_probeSet != null && _probeSet.Count > 0)
            {
                set = _probeSet;
                return true;
            }
            if (_anchor != null)
            {
                set = _anchor.GetComponentInParent<DMClimbProbeSet>();
                if (set != null && set.Count > 0)
                    return true;
            }
            if (_hasLastStick && _lastStickHit.collider != null)
            {
                set = _lastStickHit.collider.GetComponentInParent<DMClimbProbeSet>();
                if (set != null && set.Count > 0)
                    return true;
            }
            return false;
        }

        private Vector3 ProbeBodyPose(Vector3 probePos, Vector3 probeNormal)
        {
            float standOff = profile != null ? profile.standOff : 0.35f;
            Vector3 n = probeNormal.sqrMagnitude > 0.0001f ? probeNormal.normalized : _lastNormal;
            if (n.sqrMagnitude < 0.0001f)
                n = Vector3.forward;
            n.Normalize();

            // Extra stand-off on angled faces so world-aligned capsules do not sink into rock.
            float nUp = Mathf.Abs(Vector3.Dot(n, Vector3.up));
            float stand = standOff + 0.06f + Mathf.Clamp01(nUp) * 0.2f;

            float hh = profile != null ? profile.handHeight : 1.18f;
            float drop = Mathf.Clamp(hh * 0.72f, 0.55f, 1.05f);

            // Drop along the wall plane (not world Y) so angled normals do not pull the body into the mesh.
            Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, n);
            if (wallUp.sqrMagnitude < 0.0001f)
                wallUp = Vector3.up;
            wallUp.Normalize();

            Vector3 desired = probePos + n * stand - wallUp * drop;
            return desired;
        }

        private void SetProbeHandIk(Vector3 probePos, Vector3 probeNormal, float weight = 1f)
        {
            // Keep hands on the hold surface (baker edge moves included). Small palm only.
            float palm = 0.02f;
            if (profile != null)
                palm = Mathf.Clamp(profile.handPalmOffset * 0.25f, 0.01f, 0.05f);

            if (_probeSet != null && _probeIndex >= 0)
            {
                Vector3 leftPos = default, leftN = default, rightPos = default, rightN = default;
                if (_probeSet.TryGetPairWorldPoses(_probeIndex, out leftPos, out leftN, out rightPos, out rightN, out _, out _))
                {
                    Vector3 ln = leftN.sqrMagnitude > 0.0001f
                        ? leftN.normalized
                        : (probeNormal.sqrMagnitude > 0.0001f ? probeNormal.normalized : _lastNormal);
                    Vector3 rn = rightN.sqrMagnitude > 0.0001f ? rightN.normalized : ln;
                    _ikLeft = leftPos + ln * palm;
                    _ikRight = rightPos + rn * palm;
                    _ikWeight = 1f;
                    _ikValid = true;
                    return;
                }
            }

            // Unpaired / manual edge probe: both hands near this hold with stance-ish spread.
            Vector3 n = probeNormal.sqrMagnitude > 0.0001f ? probeNormal.normalized : _lastNormal;
            Vector3 right = Vector3.Cross(Vector3.up, -n);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(transform.right, -n);
            right.Normalize();
            float spread = 0.22f;
            Vector3 grab = probePos + n * palm;
            _ikLeft = grab - right * spread;
            _ikRight = grab + right * spread;
            _ikWeight = 1f;
            _ikValid = true;
        }

        private Vector3 WallUp(Vector3 normal)
        {
            Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, n);
            if (up.sqrMagnitude < 0.0001f)
                up = Vector3.up;
            return up.normalized;
        }

        private Vector3 WallRightFrom(Vector3 normal)
        {
            Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, -n);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(transform.right, -n);
            return right.normalized;
        }

        /// <summary>
        /// Probe-graph StickAndMove: WASD picks neighboring probes in wall-right / wall-up.
        /// Returns true when locomotion was fully handled (caller must return).
        /// False = no ProbeSet / no candidate — keep mesh lip8 path.
        /// </summary>

        /// <summary>True when a probe sits on a walkable top (near-up normal) — step-to would float; mantle instead.</summary>
        private static bool IsWalkableTopProbe(Vector3 worldNormal, Vector3 fromPos, Vector3 probePos)
        {
            Vector3 n = worldNormal.sqrMagnitude > 0.0001f ? worldNormal.normalized : Vector3.up;
            if (Vector3.Dot(n, Vector3.up) < 0.55f)
                return false;
            // Top holds are usually at/above hand height relative to current cling.
            return probePos.y >= fromPos.y - 0.15f;
        }

        private bool TryProbeMantleUp(Vector3 wallNormal)
        {
            return TryMantle(wallNormal, requireUp: true)
                || ForceMantleOverLip(wallNormal)
                || TryAutoMantle();
        }
        private bool TryStickAndMoveOnProbes(Vector2 raw)
        {
            if (!PreferBakedProbes())
                return false;

            if (!TryResolveProbeSetNear(_hasLastStick ? _lastStickHit.collider : null, out DMClimbProbeSet set))
            {
                if (_probeSet != null && _probeSet.Count > 0)
                    set = _probeSet;
                else
                    return false;
            }

            // Rebind only if index lost. Prefer nearest to body (manual edge moves included), not far facing.
            if (_probeIndex < 0 || _probeSet != set)
            {
                Vector3 from = transform.position + Vector3.up * (profile != null ? profile.handHeight * 0.55f : 0.65f);
                float reach = ProbeReachDistance(profile != null ? profile.attachRange : 1.4f);
                if (!set.FindNearestProbe(from, reach, out int idx, out Vector3 p, out Vector3 n, out _, out var pt)
                    && !set.FindNearestFacingProbe(from, transform.forward, reach, out idx, out p, out n, out _, out pt))
                {
                    // Still have a prior set with valid index — keep it rather than mesh snap-back.
                    if (_probeSet != null && _probeIndex >= 0 && _probeSet.GetWorldPose(_probeIndex, out _, out _, out _, out _))
                    {
                        set = _probeSet;
                    }
                    else
                        return false;
                }
                else
                    BindProbe(set, idx, pt, p, n);
            }

            // Finish in-flight step toward a neighbor.
            if (_probeMoving)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - _probeMoveAt) / Mathf.Max(0.05f, _probeMoveDur));
                float te = t * t * (3f - 2f * t); // smoothstep - natural climb step
                Vector3 pos = Vector3.Lerp(_probeMoveStart, _probeMoveEnd, te);
                if (IsSaneMove(pos))
                    MoveBody(pos);
                FaceWall(_probeMoveNormal);
                SetClimbLayerWeight(1f);

                float palm = profile != null ? profile.handPalmOffset : 0.11f;
                Vector3 lA = default, lnA = default, rA = default, rnA = default;
                Vector3 lB = default, lnB = default, rB = default, rnB = default;
                bool startPair = false;
                bool endPair = false;
                if (_probeIndex >= 0)
                    startPair = set.TryGetPairWorldPoses(_probeIndex, out lA, out lnA, out rA, out rnA, out _, out _);
                if (_probeMoveTarget >= 0)
                    endPair = set.TryGetPairWorldPoses(_probeMoveTarget, out lB, out lnB, out rB, out rnB, out _, out _);
                if (startPair && endPair)
                {
                    Vector3 leftA = lA + (lnA.sqrMagnitude > 0.0001f ? lnA.normalized : _lastNormal) * palm;
                    Vector3 rightA = rA + (rnA.sqrMagnitude > 0.0001f ? rnA.normalized : _lastNormal) * palm;
                    Vector3 leftB = lB + (lnB.sqrMagnitude > 0.0001f ? lnB.normalized : _probeMoveNormal) * palm;
                    Vector3 rightB = rB + (rnB.sqrMagnitude > 0.0001f ? rnB.normalized : _probeMoveNormal) * palm;
                    _ikLeft = Vector3.Lerp(leftA, leftB, te);
                    _ikRight = Vector3.Lerp(rightA, rightB, te);
                    _ikWeight = 1f;
                    _ikValid = true;
                }
                else
                {
                    Vector3 handA = _probeMoveStart;
                    Vector3 handB = _probeMoveEnd;
                    Vector3 nA = _lastNormal;
                    Vector3 nB = _probeMoveNormal;
                    if (_probeIndex >= 0 && set.GetWorldPose(_probeIndex, out Vector3 sp, out Vector3 sn, out _, out _))
                    {
                        handA = sp;
                        if (sn.sqrMagnitude > 0.0001f)
                            nA = sn.normalized;
                    }
                    if (_probeMoveTarget >= 0 && set.GetWorldPose(_probeMoveTarget, out Vector3 tp, out Vector3 tn, out _, out _))
                    {
                        handB = tp;
                        if (tn.sqrMagnitude > 0.0001f)
                            nB = tn.normalized;
                    }
                    SetProbeHandIk(Vector3.Lerp(handA, handB, te), Vector3.Slerp(nA, nB, te).normalized);
                }

                // Keep climb anim for the whole step even if WASD released mid-move.
                Vector2 moveAxes = _probeMoveAnimAxes.sqrMagnitude > 0.01f ? _probeMoveAnimAxes : raw;
                float animSpd = Mathf.Max(0.35f, moveAxes.magnitude) * (profile != null ? profile.moveSpeed : 1.6f);
                WriteAnimator(moveAxes, animSpd, climbing: true);

                if (t >= 0.999f)
                {
                    _probeMoving = false;
                    _probeStepReadyAt = Time.unscaledTime + 0.12f;
                    // Block immediate reverse step so A/D does not nudge-then-snap back to the prior column.
                    _probeNoReverseUntil = Time.unscaledTime + 0.28f;
                    if (_probeMoveTarget >= 0 && set.GetWorldPose(_probeMoveTarget, out Vector3 endP, out Vector3 endN, out _, out var endT))
                    {
                        BindProbe(set, _probeMoveTarget, endT, endP, endN);
                        ApplyProbePose(endP, endN, endT, raw);
                    }
                }
                return true;
            }

            if (!set.GetWorldPose(_probeIndex, out Vector3 curPos, out Vector3 curN, out _, out var curType))
            {
                Vector3 fromFix = transform.position + Vector3.up * (profile != null ? profile.handHeight * 0.55f : 0.65f);
                float reachFix = ProbeReachDistance(profile != null ? profile.attachRange : 1.4f);
                if (set.FindNearestProbe(fromFix, reachFix, out int fixIdx, out Vector3 fp, out Vector3 fn, out _, out var ft))
                {
                    BindProbe(set, fixIdx, ft, fp, fn);
                    curPos = fp;
                    curN = fn;
                    curType = ft;
                }
                else
                {
                    WriteAnimator(raw, 0f, climbing: true);
                    SetClimbLayerWeight(1f);
                    return true;
                }
            }
            _probeType = curType;
            _lastNormal = curN.sqrMagnitude > 0.0001f ? curN.normalized : _lastNormal;

            bool holdingW = raw.y > 0.02f;
            bool holdingS = raw.y < -0.2f;

            // Hang probe: W can mantle; A/D/S seek neighbors; idle holds hang pose.
            if (curType == DMClimbProbeSet.ProbeType.Hang)
            {
                if (holdingW && (TryMantle(_lastNormal, requireUp: true) || ForceMantleOverLip(_lastNormal)))
                {
                    ClearProbeState();
                    return true;
                }
            }

            // Lip / Mantle: W toward top starts mantle instead of free mesh slide.
            if (holdingW && (curType == DMClimbProbeSet.ProbeType.Lip || curType == DMClimbProbeSet.ProbeType.Mantle))
            {
                if (TryMantle(_lastNormal, requireUp: true) || ForceMantleOverLip(_lastNormal) || TryAutoMantle())
                {
                    ClearProbeState();
                    return true;
                }
            }

            if (raw.y < -0.2f && TryExitOntoGround())
            {
                ClearProbeState();
                return true;
            }

            // Near ground: auto step-down / plant (no E drop required).
            if (TryNearGroundStepDown())
            {
                ClearProbeState();
                return true;
            }

            if (Time.unscaledTime < _probeStepReadyAt)
            {
                ApplyProbePose(curPos, curN, curType, raw);
                return true;
            }

            // Deadzone on raw stick (not wall-basis magnitude) so bad normals cannot eat WASD.
            if (raw.sqrMagnitude < 0.02f)
            {
                ApplyProbePose(curPos, curN, curType, Vector2.zero);
                return true;
            }

            Vector3 wallR = WallRightFrom(_lastNormal);
            Vector3 wallU = WallUp(_lastNormal);
            Vector3 desiredDir = wallR * raw.x + wallU * raw.y;
            if (desiredDir.sqrMagnitude < 0.0001f)
            {
                // Degenerate wall basis (near-horizontal normal): fall back to player axes on the plane.
                Vector3 n = _lastNormal.sqrMagnitude > 0.0001f ? _lastNormal.normalized : Vector3.forward;
                desiredDir = Vector3.ProjectOnPlane(transform.right * raw.x + transform.up * raw.y, n);
            }
            if (desiredDir.sqrMagnitude < 0.0001f)
            {
                ApplyProbePose(curPos, curN, curType, raw);
                return true;
            }
            desiredDir.Normalize();

            float minFwd = 0.05f;
            float ax = Mathf.Abs(raw.x);
            float ay = Mathf.Abs(raw.y);
            float sx = 0f;
            if (ax > 0.01f)
            {
                sx = Mathf.Sign(raw.x);
                if (sx == 0f)
                    sx = raw.x >= 0f ? 1f : -1f;
            }
            bool pureVertical = ay > 0.28f && ay > ax + 0.12f;
            bool pureStrafe = ax > 0.18f && ay < 0.22f;
            bool diagonal = ax > 0.18f && ay > 0.18f && !pureVertical;

            if (pureStrafe)
            {
                if (wallR.sqrMagnitude > 0.0001f)
                    desiredDir = wallR * sx;
                minFwd = -0.05f;
            }
            else if (pureVertical)
            {
                // Lock W/S to wall-up so staggered neighbors do not pull the climb diagonal.
                float sy = Mathf.Sign(raw.y);
                if (sy == 0f) sy = raw.y >= 0f ? 1f : -1f;
                if (wallU.sqrMagnitude > 0.0001f)
                    desiredDir = wallU * sy;
                minFwd = 0.45f;
            }
            else if (diagonal)
            {
                minFwd = -0.12f;
            }

            // Climb-up at the rim: mantle before hunting the next probe (avoids floating top holds).
            if (holdingW && raw.y > 0.35f && TryProbeMantleUp(_lastNormal))
            {
                ClearProbeState();
                return true;
            }

            float stepMax = ProbeStepMaxForSet(set, curPos, _probeIndex, desiredDir);
            if (ax > 0.15f)
                stepMax = Mathf.Max(stepMax, ProbeStepMax(), 2.1f);
            float wide = Mathf.Clamp(Mathf.Max(stepMax * 1.55f + 0.35f, ProbeStepMax() + 0.75f), 1.4f, 3.0f);
            int next = -1;
            Vector3 nextPos = default;
            Vector3 nextN = default;
            DMClimbProbeSet.ProbeType nextType = DMClimbProbeSet.ProbeType.Face;
            bool found = false;

            if (pureStrafe || (diagonal && ax >= ay))
            {
                // A/D first: unlock side columns before vertical cone search.
                found = set.FindNearestLateralStance(curPos, wallR, wide, _probeIndex, sx,
                    out next, out nextPos, out nextN, out _, out nextType);
                if (!found)
                {
                    Vector3 around = wallR.sqrMagnitude > 0.0001f
                        ? (wallR * sx - _lastNormal * 0.35f).normalized
                        : desiredDir;
                    found = set.FindInDirection(curPos, around, wide, out next, out nextPos, out nextN, out _, out nextType,
                        fromIndex: _probeIndex, minForwardDot: -0.35f);
                }
            }

            if (!found)
            {
                found = set.FindInDirection(curPos, desiredDir, stepMax, out next, out nextPos, out nextN, out _, out nextType,
                    fromIndex: _probeIndex, minForwardDot: minFwd);
            }
            if (!found && !pureVertical)
            {
                found = set.FindInDirection(curPos, desiredDir, wide, out next, out nextPos, out nextN, out _, out nextType,
                    fromIndex: _probeIndex, minForwardDot: -0.2f);
            }
            else if (!found && pureVertical)
            {
                // Still widen reach, but keep a forward cone so staggered holds do not steal W/S.
                found = set.FindInDirection(curPos, desiredDir, wide, out next, out nextPos, out nextN, out _, out nextType,
                    fromIndex: _probeIndex, minForwardDot: 0.35f);
            }
            if (!found && !pureVertical)
            {
                found = set.FindNearestOtherStance(curPos, wide, _probeIndex, out next, out nextPos, out nextN, out _, out nextType,
                    preferredDir: desiredDir);
            }
            if (!found && (pureStrafe || diagonal))
            {
                found = set.FindNearestLateralStance(curPos, wallR, 3.0f, _probeIndex, sx,
                    out next, out nextPos, out nextN, out _, out nextType);
            }

            // Final guard: pure W/S must not accept a strongly sideways neighbor.
            if (found && pureVertical)
            {
                Vector3 delta = nextPos - curPos;
                if (set.TryGetPairWorldPoses(next, out _, out _, out _, out _, out Vector3 midV, out _))
                    delta = midV - (set.TryGetPairWorldPoses(_probeIndex, out _, out _, out _, out _, out Vector3 midFrom, out _) ? midFrom : curPos);
                float along = Vector3.Dot(delta, desiredDir);
                float sideways = (delta - desiredDir * along).magnitude;
                if (along < 0.05f || sideways > Mathf.Max(0.45f, along * 0.55f))
                    found = false;
            }

            if (!found)
            {
                if (holdingW && TryProbeMantleUp(_lastNormal))
                {
                    ClearProbeState();
                    return true;
                }
                if (Time.unscaledTime - _probeMissLogAt > 0.5f)
                {
                    _probeMissLogAt = Time.unscaledTime;
                    Debug.Log($"[{BuildStamp}] probe step MISS raw={raw} stepMax={stepMax:F2} wide={wide:F2} count={set.Count} idx={_probeIndex}");
                }
                // Hold still — do not keep climb-move anim while stuck (reads as sliding).
                ApplyProbePose(curPos, curN, curType, Vector2.zero);
                return true;
            }

            // Reject immediate reverse along the last step (snap-back).
            if (found && Time.unscaledTime < _probeNoReverseUntil && _probeLastStepFlat.sqrMagnitude > 0.0001f)
            {
                Vector3 candDelta = nextPos - curPos;
                if (set.TryGetPairWorldPoses(next, out _, out _, out _, out _, out Vector3 nMid, out _))
                    candDelta = nMid - curPos;
                if (Vector3.Dot(candDelta, _probeLastStepFlat) < -0.02f)
                {
                    found = false;
                    if (holdingW && TryProbeMantleUp(_lastNormal))
                    {
                        ClearProbeState();
                        return true;
                    }
                    ApplyProbePose(curPos, curN, curType, Vector2.zero);
                    return true;
                }
            }

            // Next hold is a walkable top / Lip / Mantle while climbing up — mantle, do not step into air.
            if (holdingW && raw.y > 0.25f
                && (nextType == DMClimbProbeSet.ProbeType.Lip
                    || nextType == DMClimbProbeSet.ProbeType.Mantle
                    || IsWalkableTopProbe(nextN, curPos, nextPos)))
            {
                if (TryProbeMantleUp(nextN.sqrMagnitude > 0.0001f ? nextN : _lastNormal)
                    || TryProbeMantleUp(_lastNormal))
                {
                    ClearProbeState();
                    return true;
                }
                // Mantle missed: stay on current hold (do not teleport onto floating top probe).
                ApplyProbePose(curPos, curN, curType, raw);
                return true;
            }

            // Step to neighbor (body rides pair midpoint when available).
            Vector3 startBody = transform.position;
            Vector3 endProbePos = nextPos;
            Vector3 endProbeN = nextN;
            if (set.TryGetPairWorldPoses(next, out _, out _, out _, out _, out Vector3 nextMid, out Vector3 nextMidN))
            {
                endProbePos = nextMid;
                if (nextMidN.sqrMagnitude > 0.0001f)
                    endProbeN = nextMidN.normalized;
            }
            Vector3 endBody = ProbeBodyPose(endProbePos, endProbeN);
            float dist = Vector3.Distance(startBody, endBody);
            float speed = profile != null ? profile.moveSpeed : 1.6f;
            if (ReadShiftHeld())
                speed *= profile != null ? profile.climbShiftMul : 1.35f;
            _probeMoving = true;
            _probeMoveStart = startBody;
            _probeMoveEnd = endBody;
            _probeMoveNormal = endProbeN.sqrMagnitude > 0.0001f ? endProbeN.normalized : _lastNormal;
            _probeMoveTarget = next;
            _probeMoveAt = Time.unscaledTime;
            // Natural climb step: slower than freerun moveSpeed so anim can read.
            float stepSpeed = Mathf.Clamp(speed * 0.45f, 0.55f, 1.05f);
            _probeMoveDur = Mathf.Clamp(dist / stepSpeed, 0.28f, 0.7f);
            _probeLastStepFlat = Vector3.ProjectOnPlane(endBody - startBody, _probeMoveNormal);
            // Latch anim axes for the full step duration (release mid-step must not freeze the climb clip).
            if (raw.sqrMagnitude > 0.04f)
                _probeMoveAnimAxes = raw.normalized;
            else if (_probeLastStepFlat.sqrMagnitude > 0.0001f)
            {
                Vector3 flat = _probeLastStepFlat.normalized;
                Vector3 wr = WallRightFrom(_probeMoveNormal);
                Vector3 wu = WallUp(_probeMoveNormal);
                _probeMoveAnimAxes = new Vector2(Vector3.Dot(flat, wr), Vector3.Dot(flat, wu));
                if (_probeMoveAnimAxes.sqrMagnitude > 0.0001f)
                    _probeMoveAnimAxes.Normalize();
                else
                    _probeMoveAnimAxes = Vector2.up;
            }
            else
                _probeMoveAnimAxes = Vector2.up;
            // Do not ApplyProbePose(current) here — that snapped Kade back onto the old hold the same frame.
            float stepAnimSpd = Mathf.Max(0.35f, _probeMoveAnimAxes.magnitude) * (profile != null ? profile.moveSpeed : 1.6f);
            WriteAnimator(_probeMoveAnimAxes, stepAnimSpd, climbing: true);
            SetClimbLayerWeight(1f);
            return true;
        }

        private void ApplyProbePose(Vector3 probePos, Vector3 probeNormal, DMClimbProbeSet.ProbeType type, Vector2 raw)
        {
            Vector3 n = probeNormal.sqrMagnitude > 0.0001f ? probeNormal.normalized : _lastNormal;
            Vector3 bodyProbePos = probePos;
            Vector3 bodyN = n;
            if (_probeSet != null && _probeIndex >= 0)
            {
                Vector3 midPos = default, midN = default;
                if (_probeSet.TryGetPairWorldPoses(_probeIndex, out _, out _, out _, out _, out midPos, out midN))
                {
                    bodyProbePos = midPos;
                    if (midN.sqrMagnitude > 0.0001f)
                        bodyN = midN.normalized;
                }
            }
            _lastNormal = bodyN;
            _probeType = type;
            Vector3 body = ProbeBodyPose(bodyProbePos, bodyN);
            // Only correct when drifted — re-applying every FixedUpdate fought anim/physics (down-slide + snap-up).
            if (IsSaneMove(body) && (body - transform.position).sqrMagnitude > 0.0025f)
                MoveBody(body);
            FaceWall(bodyN);
            SetClimbLayerWeight(1f);
            SetProbeHandIk(probePos, n);

            _lastStickHit.point = bodyProbePos;
            _lastStickHit.normal = bodyN;
            _hasLastStick = true;
            if (_probeSet != null)
            {
                _anchor = _probeSet.transform;
                _localOffset = _anchor.InverseTransformPoint(body);
                _prevAnchorPos = _anchor.position;
                _hasPrevAnchorPos = true;
            }

            float speed = profile != null ? profile.moveSpeed : 1.6f;
            float stick = new Vector2(raw.x, raw.y).magnitude;
            // Idle/hold: zero move speed so stuck A/D does not look like wall sliding.
            float climbSpeed = stick < 0.08f ? 0f : stick * speed;
            WriteAnimator(stick < 0.08f ? Vector2.zero : raw, climbSpeed, climbing: true);

            // Hang type keeps lip-hang style gate so W can mantle.
            _lipHang = type == DMClimbProbeSet.ProbeType.Hang || type == DMClimbProbeSet.ProbeType.Lip;
        }

        private bool TryProbeRange(Vector3 origin, Vector3 forward, float range, float radius, out RaycastHit best)
        {
            best = default;
            Vector3[] dirs =
            {
                forward,
                (forward + Vector3.up * 0.2f).normalized,
                (forward - Vector3.up * 0.15f).normalized,
                (forward + transform.right * 0.2f).normalized,
                (forward - transform.right * 0.2f).normalized,
            };

            bool any = false;
            for (int i = 0; i < dirs.Length; i++)
            {
                if (!SurfaceCast(origin, dirs[i], range, radius, out RaycastHit hit))
                    continue;
                if (!IsClimbableHit(hit) || !IsClimbableSlope(hit.normal))
                    continue;
                if (!any || hit.distance < best.distance)
                    best = hit;
                any = true;
            }

            return any;
        }

        private bool SurfaceCast(Vector3 origin, Vector3 dir, float range, float radius, out RaycastHit hit)
        {
            hit = default;
            if (dir.sqrMagnitude < 0.0001f)
                return false;
            dir.Normalize();
            QueryTriggerInteraction q = QueryTriggerInteraction.Ignore;
            const int query = ~0;
            int n = radius > 0.001f
                ? Physics.SphereCastNonAlloc(origin, radius, dir, _castHits, range, query, q)
                : Physics.RaycastNonAlloc(origin, dir, _castHits, range, query, q);
            float best = float.MaxValue;
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit cand = _castHits[i];
                if (IsSelfHit(cand) || cand.collider == null)
                    continue;
                if (cand.distance < best)
                {
                    best = cand.distance;
                    hit = cand;
                    any = true;
                }
            }
            return any;
        }

        private Vector3 WallRight(Vector3 wallNormal)
        {
            Vector3 n = wallNormal.sqrMagnitude > 0.001f ? wallNormal.normalized : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, -n);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.ProjectOnPlane(transform.right, n);
            right.Normalize();
            if (Vector3.Dot(right, transform.right) < 0f)
                right = -right;
            return right;
        }

        private bool IsClimbableHit(RaycastHit hit)
        {
            if (hit.collider == null)
                return false;
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                return false;

            if (_mask.value != 0 && (_mask.value & (1 << hit.collider.gameObject.layer)) != 0)
                return true;

            return !string.IsNullOrEmpty(_tagName) && hit.collider.CompareTag(_tagName);
        }

        private bool IsClimbableSlope(Vector3 normal)
        {
            float angle = Vector3.Angle(Vector3.up, normal);
            float walkMax = profile != null ? profile.walkMaxSlopeDeg : 45f;
            float climbMin = profile != null ? profile.climbMinSlopeDeg : 45f;
            float climbMax = profile != null ? profile.climbMaxSlopeDeg : 115f;
            float need = Mathf.Max(walkMax, climbMin);
            return angle > need && angle <= climbMax;
        }

        private Vector3 ChestOrigin()
        {
            float height = _capsule != null ? _capsule.height : 1.8f;
            float centerY = _capsule != null ? _capsule.center.y : height * 0.5f;
            return transform.position + transform.up * (centerY + height * 0.15f);
        }

        private void SafeZeroVelocity()
        {
            if (body == null || body.isKinematic)
                return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void SafeSetLinearVelocity(Vector3 velocity)
        {
            if (body == null || body.isKinematic)
                return;
            body.linearVelocity = velocity;
        }

        private bool IsSelfHit(RaycastHit hit)
        {
            if (hit.collider == null)
                return true;
            Transform t = hit.collider.transform;
            return t == transform || t.IsChildOf(transform);
        }

        /// <summary>
        /// 3–5 short hand/chest casts into the wall. Best-fit cling point plus averaged
        /// normals so curved rock does not snap to a single triangle.
        /// Additive — does not replace TryStickWall / attach / jump-to-wall probes.
        /// </summary>
        private bool SprayHandholds(Vector3 intoWall, out RaycastHit best, out Vector3 avgNormal)
        {
            best = default;
            avgNormal = Vector3.zero;
            Vector3 n = intoWall.sqrMagnitude > 0.001f ? intoWall.normalized : transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, n);
            if (right.sqrMagnitude < 0.001f)
                right = transform.right;
            right.Normalize();

            Vector3 pos = transform.position;
            Vector3[] origins =
            {
                pos + Vector3.up * 1.25f - right * 0.22f,
                pos + Vector3.up * 1.25f + right * 0.22f,
                pos + Vector3.up * 1.10f,
                pos + Vector3.up * 1.50f,
                pos + Vector3.up * 0.75f,
                pos + Vector3.up * 0.40f - right * 0.16f,
                pos + Vector3.up * 0.40f + right * 0.16f,
                pos + Vector3.up * 0.55f,
            };
            Vector3[] dirs =
            {
                n,
                n,
                n,
                (n + right * 0.15f).normalized,
                (n - right * 0.15f).normalized,
                n,
                n,
                (n + Vector3.up * 0.2f).normalized,
            };

            float range = 0.75f;
            float radius = 0.08f;
            int hits = 0;
            float bestDist = float.MaxValue;

            for (int i = 0; i < origins.Length; i++)
            {
                if (!SurfaceCast(origins[i], dirs[i], range, radius, out RaycastHit hit))
                    continue;
                if (IsSelfHit(hit) || !IsClimbableHit(hit))
                    continue;
                if (Vector3.Angle(Vector3.up, hit.normal) <= 55f)
                    continue;
                avgNormal += hit.normal.normalized;
                hits++;
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = hit;
                }
            }

            if (hits == 0)
                return false;

            avgNormal /= hits;
            if (avgNormal.sqrMagnitude < 0.001f)
                avgNormal = best.normal;
            else
                avgNormal.Normalize();
            return true;
        }

        private bool StandBlocked(Vector3 stand, float height, float radius)
        {
            Vector3 p1 = stand + Vector3.up * radius;
            Vector3 p2 = stand + Vector3.up * Mathf.Max(radius + 0.05f, height * 0.9f);
            int n = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, _overlapBuf, ~0, QueryTriggerInteraction.Ignore);
            int climbLayer = LayerMask.NameToLayer(profile != null ? profile.climbableLayerName : "Climbable");
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlapBuf[i];
                if (c == null)
                    continue;
                if (c == _capsule)
                    continue;
                if (c.transform == transform || c.transform.IsChildOf(transform))
                    continue;
                // Climbable: allow standing on top. Only reject deep volume sinks (chest inside slab).
                if (climbLayer >= 0 && c.gameObject.layer == climbLayer)
                {
                    Vector3 foot = stand + Vector3.up * 0.08f;
                    Vector3 chest = stand + Vector3.up * Mathf.Max(0.55f, radius + 0.35f);
                    Vector3 closestChest = c.ClosestPoint(chest);
                    Vector3 closestFoot = c.ClosestPoint(foot);
                    // Feet on top deck.
                    if (closestFoot.y <= stand.y + 0.18f && (chest - closestChest).sqrMagnitude > radius * radius * 0.25f)
                        continue;
                    // Chest deeply inside collider volume.
                    if ((chest - closestChest).sqrMagnitude < 0.0004f && closestChest.y > stand.y + 0.35f)
                        return true;
                    continue;
                }
                return true;
            }
            return false;
        }

        private bool TryClearStand(RaycastHit top, Vector3 forward, out Vector3 stand)
        {
            float height = _capsule != null ? _capsule.height : 1.84f;
            float capR = _capsule != null ? _capsule.radius : 0.26f;
            float r = capR * 0.85f;
            Vector3 fwd = Flatten(forward);
            float pad = Mathf.Max(0.04f, MantlePlantPad);

            // v1j: plant just onto the deck — long forward offsets clipped through thin tops.
            float[] dist = { 0.12f, 0.2f, 0.28f, 0.38f, 0.5f };
            for (int i = 0; i < dist.Length; i++)
            {
                stand = top.point + Vector3.up * pad + fwd * dist[i];
                if (!StandBlocked(stand, height, r))
                    return true;
            }

            stand = default;
            return false;
        }


        private SurvivalStats Survival
        {
            get
            {
                if (_survival == null)
                    _survival = ResolveSurvivalStats();
                return _survival;
            }
        }

        private SurvivalStats ResolveSurvivalStats()
        {
            SurvivalStats stats = GetComponent<SurvivalStats>();
            if (stats == null)
                stats = GetComponentInParent<SurvivalStats>();
            if (stats == null)
                stats = GetComponentInChildren<SurvivalStats>(true);
            return stats;
        }

        private bool HasClimbStartStamina()
        {
            SurvivalStats stats = Survival;
            if (stats == null)
                return true;
            float cost = profile != null ? profile.climbStartStaminaCost : 5f;
            return cost <= 0f || stats.HasStamina(cost);
        }

        private bool TryPayClimbStartStamina()
        {
            SurvivalStats stats = Survival;
            if (stats == null)
                return true;
            float cost = profile != null ? profile.climbStartStaminaCost : 5f;
            return cost <= 0f || stats.TryConsumeStamina(cost);
        }

        private void TickClimbStamina()
        {
            SurvivalStats stats = Survival;
            if (stats == null)
                return;

            // Regen otherwise overpowers this drain (~12/s regen vs ~1 dash/s).
            stats.suppressStaminaRegen = true;

            // 1 stamina dash/sec while climb-moving; half when hanging/idle on wall.
            // Dash size matches pilot stamina arc (maxStamina / unlockedDashCount).
            int unlockedDashes = ResolveUnlockedStaminaDashCount();
            float maxStamina = Mathf.Max(1f, stats.maxStamina);
            // Guard: never allow a degenerate dash count to zero-out drain.
            float dashCost = maxStamina / Mathf.Max(1, unlockedDashes);
            float legacy = profile != null ? Mathf.Max(0f, profile.climbStaminaDrainPerSecond) : 8f;
            // Prefer dash-sized drain; floor with legacy profile rate so spend is always visible.
            float moveRate = Mathf.Max(dashCost, legacy * 0.35f);
            float moveMag = new Vector2(_dampedClimbInput.x, _dampedClimbInput.y).magnitude;
            bool hangingOrIdle = _lipHang || moveMag < 0.08f;
            float drainPerSec = hangingOrIdle ? moveRate * 0.5f : moveRate;
            if (drainPerSec > 0f)
                stats.SpendStamina(drainPerSec * Time.fixedDeltaTime);

            if (stats.CurrentStamina <= 0.01f)
                DropFromClimb();
        }

        /// <summary>Matches DMUiToolkitPilotCluster stamina arc dash count.</summary>
        private static int ResolveUnlockedStaminaDashCount()
        {
            const float StaminaSweepDeg = 136f;
            const float DashSweep = 3.35f;
            const float DashGap = 2.05f;
            const int LockedArcDashCount = 4;
            float pitch = DashSweep + DashGap;
            int total = Mathf.Max(LockedArcDashCount + 1, Mathf.FloorToInt((StaminaSweepDeg + 0.01f) / pitch));
            int baseDashCount = Mathf.Max(1, total - LockedArcDashCount);
            int bonus = Mathf.Clamp(PlayerSkillAllocator.GetTotalRank(SkillModifierType.MaxStaminaPercent), 0, LockedArcDashCount);
            return baseDashCount + bonus;
        }

        private void Attach(RaycastHit hit)
        {
            SurvivalStats attachStats = Survival;
            if (attachStats != null)
                attachStats.suppressStaminaRegen = true;

            _climbing = true;
            ClearProbeState(); // free-climb-dune-v1: never bind probe graph on attach
            _hopping = false;
            _lipHang = false;
            ClearOverhangState();
            _hopChargeAt = -10f;
            _hopNeedsJumpRelease = true;
            _airJumpGrabUntil = -10f;
            _attachedAt = Time.unscaledTime;
            _stickLostAt = -10f;
            _leapRegrab = false;
            _lastNormal = hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : -transform.forward;
            _dampedClimbInput = Vector3.zero;
            _dampedClimbVel = Vector3.zero;

            SafeZeroVelocity();

            if (motor != null)
            {
                motor.lockMovement = true;
                motor.lockAnimMovement = true;
                motor.disableCheckGround = true;
                motor.input = Vector3.zero;
                motor.inputMagnitude = 0f;
                motor.isJumping = false;
                motor.verticalVelocity = 0f;
                motor.DisableGravityAndCollision();
                motor.enabled = false;
                _motorOverridden = true;
            }

            if (body != null)
            {
                body.useGravity = false;
                body.isKinematic = true;
                _motorOverridden = true;
            }

            if (_character != null)
            {
                _heldCcEnabled = _character.enabled;
                _character.enabled = false;
            }

            if (_capsule != null)
            {
                _heldCapsuleTrigger = _capsule.isTrigger;
                _heldCapsuleEnabled = _capsule.enabled;
                _capsule.isTrigger = true;
            }

            IgnorePlayerClimbCollision(true);

            if (animator != null)
            {
                _heldApplyRootMotion = animator.applyRootMotion;
                animator.applyRootMotion = false;
            }

            // Prefer baked probe bind (set during TryProbe → TrySnapToBakedProbe).
            if (PreferBakedProbes() && _probeSet != null && _probeIndex >= 0
                && _probeSet.GetWorldPose(_probeIndex, out Vector3 pPos, out Vector3 pN, out _, out var pType))
            {
                BindProbe(_probeSet, _probeIndex, pType, pPos, pN);
                ApplyProbePose(pPos, pN, pType, Vector2.zero);
            }
            else
            {
                ClearProbeState();
                // free-climb-dune-v4: grounded start -> lowest face point; jump-grab -> nearest face height.
                bool fromAir = motor == null || !motor.isGrounded;
                if (!fromAir)
                    SnapToLowestClimbPoint(hit);
                else
                    SnapToNearestFace(hit);
                if (SprayHandholds(-_lastNormal, out _, out Vector3 avgN) && avgN.sqrMagnitude > 0.001f)
                    _lastNormal = Vector3.Slerp(_lastNormal, avgN, 0.55f).normalized;
                FaceWall(_lastNormal);
                // Opportunistic bind if a ProbeSet exists on the hit but facing snap missed.
                if (PreferBakedProbes() && hit.collider != null)
                {
                    DMClimbProbeSet set = hit.collider.GetComponentInParent<DMClimbProbeSet>();
                    if (set != null && set.Count > 0)
                    {
                        float reach = ProbeReachDistance(profile != null ? profile.attachRange : 1.4f);
                        Vector3 from = ChestOrigin();
                        if (set.FindNearestFacingProbe(from, transform.forward, reach, out int idx, out Vector3 bp, out Vector3 bn, out _, out var bt)
                            || set.FindNearestProbe(hit.point, reach, out idx, out bp, out bn, out _, out bt))
                        {
                            BindProbe(set, idx, bt, bp, bn);
                            ApplyProbePose(bp, bn, bt, Vector2.zero);
                        }
                    }
                }
            }
            WriteAnimator(Vector2.zero, 0f, climbing: true);
            SetClimbLayerWeight(1f);
        }


        private bool ClingSenseEnabled()
        {
            return profile == null || profile.enableClingSense;
        }

        private void RefreshClingSense()
        {
            if (!ClingSenseEnabled())
            {
                _cling = default;
                return;
            }

            float deep = OverhangDeepProtrusion();
            _clingSense.DeepLipMeters = deep;
            _clingSense.WalkMaxSlopeDeg = profile != null ? Mathf.Min(profile.walkMaxSlopeDeg, 50f) : 45f;
            _clingSense.ClimbMinSlopeDeg = profile != null ? Mathf.Max(50f, profile.climbMinSlopeDeg - 10f) : 55f;
            // cling-sense-v1g: 360° fan every 20°, rays 1–2m, chest + head.
            _clingSense.BubbleRadius = profile != null ? Mathf.Clamp(profile.attachRange * 0.75f, 1.05f, 1.85f) : 1.35f;
            _clingSense.RayRange = profile != null ? Mathf.Clamp(profile.attachRange + 0.35f, 1.0f, 2.0f) : 1.75f;
            _clingSense.SphereStepDeg = 20f;
            _clingSense.EnableSphereFan = false; // mantle-simple-v2: directed probes only

            Vector3 n = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal : -transform.forward;
            _cling = _clingSense.Refresh(
                transform,
                n,
                _mask,
                _tagName,
                profile != null ? profile.handHeight : 1.18f,
                profile != null ? profile.standOff : 0.35f,
                IsSelfHit,
                IsClimbableHit);

            if (_cling.hasFace && _cling.faceNormal.sqrMagnitude > 0.001f)
            {
                // Soft blend so bubble face guides cling without fighting corners.
                // v1h: while climbing hard up, blend less so WallUp stays stable (W crawl fix).
                float blend = 0.35f;
                Vector2 upAxes = ReadClimbAxes();
                if (upAxes.y > 0.35f)
                    blend = 0.12f;
                _lastNormal = Vector3.Slerp(_lastNormal, _cling.faceNormal, blend).normalized;
                _lastStickHit = _cling.faceHit;
                _hasLastStick = true;
            }

            if (Time.unscaledTime - _clingLogAt > 1.25f && _climbing)
            {
                _clingLogAt = Time.unscaledTime;
                Debug.Log($"[{BuildStamp}] bubble face={_cling.hasFace} soffit={_cling.hasSoffit} ground={_cling.hasWalkableBelow} lip={_cling.hasLip} stub={_cling.isStubLip} deep={_cling.isDeepLip} protrude={_cling.lipProtrusion:F2} sideL={_cling.hasSideL} sideR={_cling.hasSideR} sphereHits={_cling.sphereHitCount}/{_cling.sphereRayCount} range={_cling.sphereRange:F2}");
            }
        }

        private void StickAndMove()
        {
            if (_anchor != null && _hasPrevAnchorPos && Time.fixedDeltaTime > 0.0001f)
                _platformVel = (_anchor.position - _prevAnchorPos) / Time.fixedDeltaTime;
            else
                _platformVel = Vector3.zero;

            if (ClingSenseEnabled())
                RefreshClingSense();
            else
                _cling = default;

            Vector2 raw = ReadClimbAxes();
            bool holdingW = raw.y > 0.02f;
            bool holdingS = raw.y < -0.2f;

            if (_overhangGrabbing)
            {
                // lip8: abort grab onto thick underside — never finish into zero-input hang lock.
                if (_overhangLip.collider != null
                    && (LipContactIsThickSlabUnderside(_overhangLip) || LipIsUndersideCorner(_overhangLip, _lastNormal)))
                {
                    _overhangGrabbing = false;
                    // Keep lip for under-face probe; resume/escape clear overhang on success.
                    if (TryResumeClimbFromOverhang())
                    {
                        if (_mantling || !_climbing)
                            return;
                        // face stick — fall through StickAndMove
                    }
                    else if (TryEscapeIllegalOverhangHang())
                    {
                        if (_mantling || !_climbing)
                            return;
                        if (_overhangHang)
                            ClearOverhangState();
                        // fall through face stick
                    }
                    else
                    {
                        ClearOverhangState();
                        DropFromClimb();
                        return;
                    }
                }
                else if (holdingS)
                {
                    DropFromClimb();
                    return;
                }
                else
                {
                    TickOverhangGrab();
                    return;
                }
            }

            if (_overhangHang)
            {
                // cling-sense-v1f: floating hang with nothing above the ledge — escape immediately.
                if (TryEscapeOrphanOverhangHang())
                {
                    if (_mantling || !_climbing)
                        return;
                    if (!_overhangHang)
                    {
                        // fall through to face StickAndMove
                    }
                    else
                        return;
                }

                // lip8: never remain in kinematic underside hang
                if (OverhangHangIsIllegalUnderside())
                {
                    if (!TryEscapeIllegalOverhangHang())
                    {
                        DropFromClimb();
                        return;
                    }
                    // Mantle took over, or climb ended.
                    if (_mantling || !_climbing)
                        return;
                    // Face stick: fall through StickAndMove. Top-lip promote: keep hang handling.
                    if (!_overhangHang)
                    {
                        // fall through
                    }
                }

                if (_overhangHang)
                {
                    if (holdingS)
                    {
                        // lip8: Hang+S MUST TryResumeClimbFromOverhang BEFORE any Drop/ExitOntoGround.
                        // Only E (Update) or no climbable wall under lip may full-drop.
                        if (TryResumeClimbFromOverhang())
                        {
                            // Fall through into normal stick/move with down input.
                        }
                        else
                        {
                            Debug.Log($"[{BuildStamp}] hang+S resume FAIL -> Drop (no Climbable under lip)");
                            DropFromClimb();
                            return;
                        }
                    }
                    else
                    {
                        // Hang + W must complete mantle (mid ledge included). Never clear hang on W
                        // alone - that re-entered StickAndMove, re-grabbed, and stuttered.
                        // Thick mid: refine mid-slab/soffit lock up to the top outer lip before mantle.
                        if (holdingW)
                        {
                            if (_overhangLip.collider != null
                                && RefineLipToTopEdge(_overhangLip, _lastNormal, out RaycastHit topLip)
                                && IsWalkableLipNormal(topLip.normal)
                                && !LipIsUndersideCorner(topLip, _lastNormal)
                                && !LipContactIsThickSlabUnderside(topLip))
                            {
                                _overhangLip = topLip;
                                _lastStickHit = topLip;
                                Vector3 hangFix = OverhangHangPos(topLip);
                                if (IsSaneMove(hangFix))
                                    MoveBody(hangFix);
                                SetOverhangHandIk();
                            }
                            if (TryMantle(_lastNormal, requireUp: true) || ForceMantleOverLip(_lastNormal))
                                return;
                            // free-climb-dune-v5: short/shallow hang + W must escape, not soft-lock.
                            if (_overhangPreferMantle || _overhangProtrusion < OverhangDeepProtrusion())
                            {
                                if (TryResumeClimbFromOverhang())
                                    return;
                                ClearOverhangState();
                                _overhangResumeAt = Time.unscaledTime + 0.2f;
                                _lipHang = false;
                                Debug.Log($"[{BuildStamp}] short hang+W -> clear face");
                                return;
                            }
                            // Deep hang mantle miss: brief gate so Clear->regrab cannot loop.
                            _overhangResumeAt = Mathf.Max(_overhangResumeAt, Time.unscaledTime + 0.4f);
                        }
                        // A/D: face-stick resume OR lip shimmy. Do not require S to unlock lateral.
                        if (Mathf.Abs(raw.x) > 0.18f && TryResumeClimbFromOverhang())
                        {
                            // Fall through into normal stick/move.
                        }
                        else if (raw.sqrMagnitude > 0.04f
                                 && PreferBakedProbes()
                                 && TryResolveProbeSetNear(_overhangLip.collider != null ? _overhangLip.collider : (_hasLastStick ? _lastStickHit.collider : null), out _))
                        {
                            // Probe bake present: leave hang lock so WASD can step the graph.
                            ClearOverhangState();
                            // Fall through into probe StickAndMove.
                        }
                        else
                        {
                            HoldOverhangHang(raw);
                            return;
                        }
                    }
                }
            }

            // Probe graph owns climb when preferBakedProbes + a ProbeSet is in play.
            // Do NOT fall through to mesh stick — that fights probe steps (snap-back).
            if (PreferBakedProbes())
            {
                if (TryStickAndMoveOnProbes(raw))
                    return;
                if (_probeSet != null && _probeIndex >= 0
                    && _probeSet.GetWorldPose(_probeIndex, out Vector3 holdP, out Vector3 holdN, out _, out var holdT))
                {
                    ApplyProbePose(holdP, holdN, holdT, raw);
                    return;
                }
            }

            Vector3 probeDir = -_lastNormal;
            if (probeDir.sqrMagnitude < 0.001f)
                probeDir = transform.forward;

            // free-climb-dune-v2: stickier cling cast so WASD does not lose the face on angles.
            float stickRange = profile != null ? profile.attachRange + 0.65f : 2.1f;
            float radius = profile != null ? Mathf.Max(profile.probeRadius, 0.24f) : 0.24f;

            bool stuck = TryStickWall(probeDir, stickRange, radius, out RaycastHit hit);
            Vector3 clingNormal = stuck && hit.normal.sqrMagnitude > 0.001f
                ? hit.normal.normalized
                : _lastNormal;

            if (SprayHandholds(probeDir, out RaycastHit sprayHit, out Vector3 sprayNormal))
            {
                if (stuck)
                {
                    Vector3 blended = clingNormal + sprayNormal;
                    if (blended.sqrMagnitude > 0.001f)
                        clingNormal = blended.normalized;
                }
                else
                {
                    hit = sprayHit;
                    clingNormal = sprayNormal.sqrMagnitude > 0.001f
                        ? sprayNormal
                        : sprayHit.normal.normalized;
                    stuck = true;
                }
            }

            if (!stuck && TryStickLastHit(probeDir, stickRange, radius, out hit))
            {
                stuck = true;
                clingNormal = hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : _lastNormal;
            }

            if (!stuck)
            {
                // v1j: crease / interior corner — recover via ClingSense sides or interior wrap before drop/freeze.
                if (TryRecoverStickInCorner(probeDir, stickRange, radius, out hit))
                {
                    stuck = true;
                    clingNormal = hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : _lastNormal;
                }
            }

            if (!stuck)
            {
                if (holdingS)
                {
                    if (TryExitOntoGround())
                        return;
                    DropFromClimb();
                    return;
                }
                bool lostSoffit = TryProbeSoffit(_lastNormal, out _);
                // Walk-off / freefall onto the same ledge must not magnetic auto-mantle.
                // Only intentional climb-up reaches for the lip.
                if (holdingW && !lostSoffit && TryAutoMantle())
                    return;
                if (holdingW && TryStartOverhangGrab())
                {
                    TickOverhangGrab();
                    return;
                }
                _lipHang = true;
                if (_stickLostAt < 0f)
                    _stickLostAt = Time.unscaledTime;

                // Hold last face briefly — don't DropFromClimb on a one-frame crease miss.
                Vector3 keep = transform.position;
                if (_hasLastStick)
                {
                    float standOffLost = profile != null ? profile.standOff : 0.35f;
                    Vector3 nFlatLost = Flatten(_lastNormal);
                    keep.x = _lastStickHit.point.x + nFlatLost.x * standOffLost;
                    keep.z = _lastStickHit.point.z + nFlatLost.z * standOffLost;
                }
                SnapLipHang(ref keep);
                MoveBody(keep);
                FaceWall(_lastNormal);
                WriteAnimator(new Vector2(raw.x, 0f), Mathf.Abs(raw.x) * (profile != null ? profile.moveSpeed : 1.6f) * 0.35f, climbing: true);
                return;
            }
            _stickLostAt = -10f;
            _lastStickHit = hit;
            _hasLastStick = true;

            // free-climb-dune-v3: if cling surface eased into walkable flat, step off automatically.
            // v1j: don't auto-drop while strafing — interior corners often read "flat" for a frame.
            float flatMax = profile != null ? Mathf.Min(profile.walkMaxSlopeDeg, 50f) : 45f;
            if (Vector3.Angle(Vector3.up, clingNormal) <= flatMax && Mathf.Abs(raw.x) < 0.18f && raw.y < 0.2f)
            {
                if (TryExitOntoGroundInternal(maxDown: 1.2f, requireLowProbe: false)
                    || TryAutoDropOntoFlatSurface())
                    return;
            }

            // free-climb-dune-v3: snap facing faster on corner normal changes.
            float turnDeg = Vector3.Angle(_lastNormal, clingNormal);
            // v1j: rate-limit corner turns so exterior wrap eases instead of 90° snapping.
            Vector3 targetN = clingNormal;
            const float maxTurnDeg = 42f;
            if (turnDeg > maxTurnDeg && _lastNormal.sqrMagnitude > 0.001f)
                targetN = Vector3.Slerp(_lastNormal, clingNormal, maxTurnDeg / turnDeg).normalized;
            float slerpT = turnDeg > 28f ? 0.5f : 0.42f;
            Vector3 normal = Vector3.Slerp(_lastNormal, targetN, slerpT).normalized;
            if (normal.sqrMagnitude < 0.001f)
                normal = clingNormal.sqrMagnitude > 0.001f ? clingNormal : hit.normal.normalized;
            _lastNormal = normal;

            Vector3 right = Vector3.Cross(Vector3.up, -normal);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(transform.right, -normal);
            right.Normalize();

            if (raw.y < -0.2f && TryExitOntoGround())
                return;
            if (TryNearGroundStepDown())
                return;
            RaycastHit soffitHit = default;
            bool soffitBlocked = TryProbeSoffit(_lastNormal, out soffitHit);
            if (_cling.valid && _cling.hasSoffit)
            {
                soffitHit = _cling.soffitHit;
                soffitBlocked = true;
            }

            // mantle-simple-v2: top edge = HasWallAbove + AtHandLip only (no sphere / cling-lip maze).
            bool wallAbove = HasWallAbove(_lastNormal);
            bool haveRim = AtHandLip(_lastNormal, out RaycastHit rimLip);
            if (haveRim && RefineLipToTopEdge(rimLip, _lastNormal, out RaycastHit rimRefined)
                && IsWalkableLipNormal(rimRefined.normal))
                rimLip = rimRefined;

            if (holdingW && Mathf.Abs(raw.x) < 0.4f && haveRim && (!wallAbove || soffitBlocked))
            {
                _overhangLip = rimLip;
                _overhangPreferMantle = true;
                if (ForceMantleOverLip(_lastNormal) || TryMantle(_lastNormal, requireUp: false))
                    return;
            }

            if (soffitBlocked)
            {
                _lipHang = true;
                if (holdingW)
                    raw.y = Mathf.Min(raw.y, 0.55f);
            }
            else if (!wallAbove && haveRim)
            {
                _lipHang = true;
            }
            else
            {
                _lipHang = false;
            }

            float damp = profile != null ? profile.climbInputDamp : 0.1f;
            _dampedClimbInput = Vector3.SmoothDamp(
                _dampedClimbInput,
                new Vector3(raw.x, raw.y, 0f),
                ref _dampedClimbVel,
                damp);

            float speed = profile != null ? profile.moveSpeed : 1.6f;
            if (ReadShiftHeld())
                speed *= profile != null ? profile.climbShiftMul : 1.35f;
            float standOff = profile != null ? profile.standOff : 0.35f;
            // free-climb-dune-v4: move from body on wall plane, then re-stick depth.
            // Resetting desired to hit.point every frame cancelled strafe and snapped to the lip.
            Vector3 wallU = WallUp(normal);
            Vector3 desired = transform.position;
            desired += right * (_dampedClimbInput.x * speed * Time.fixedDeltaTime);
            desired += wallU * (_dampedClimbInput.y * speed * Time.fixedDeltaTime);
            Vector3 along = Vector3.ProjectOnPlane(desired - hit.point, normal);
            desired = hit.point + along + normal * standOff;
            if (soffitBlocked)
            {
                float hh = profile != null ? profile.handHeight : 1.18f;
                desired.y = Mathf.Min(desired.y, soffitHit.point.y - hh);
            }
            // Soft lip clamp only when nearly idle at the rim — never while strafing/descending.
            if (_lipHang && Mathf.Abs(raw.x) < 0.12f && raw.y > -0.05f && raw.y < 0.25f)
                SnapLipHang(ref desired);

            MoveBody(desired);
            FaceWall(normal);

            _anchor = hit.transform;
            if (_anchor != null)
            {
                _localOffset = _anchor.InverseTransformPoint(desired);
                _prevAnchorPos = _anchor.position;
                _hasPrevAnchorPos = true;
            }

            float climbSpeed = new Vector2(_dampedClimbInput.x, _dampedClimbInput.y).magnitude * speed;
            WriteAnimator(new Vector2(_dampedClimbInput.x, _dampedClimbInput.y), climbSpeed, climbing: true);
            UpdateHandGrabTargets(clingNormal);
        }

        private bool TryStickWall(Vector3 probeDir, float stickRange, float radius, out RaycastHit hit)
        {
            hit = default;
            // cling-sense-v1i: prefer side face when strafing around a corner (lower threshold).
            if (_cling.valid)
            {
                Vector2 axes = ReadClimbAxes();
                Vector3 curN = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -probeDir.normalized;
                bool preferSide = Mathf.Abs(axes.x) > 0.12f;
                if (preferSide && axes.x > 0f && _cling.hasSideR
                    && IsClimbableHit(_cling.sideRHit) && !IsSelfHit(_cling.sideRHit))
                {
                    float sideTurn = Vector3.Angle(curN, _cling.sideRHit.normal);
                    float faceTurn = (_cling.hasFace && _cling.faceNormal.sqrMagnitude > 0.001f)
                        ? Vector3.Angle(curN, _cling.faceNormal) : 0f;
                    if (!_cling.hasFace || sideTurn >= faceTurn - 2f)
                    {
                        hit = _cling.sideRHit;
                        return true;
                    }
                }
                if (preferSide && axes.x < 0f && _cling.hasSideL
                    && IsClimbableHit(_cling.sideLHit) && !IsSelfHit(_cling.sideLHit))
                {
                    float sideTurn = Vector3.Angle(curN, _cling.sideLHit.normal);
                    float faceTurn = (_cling.hasFace && _cling.faceNormal.sqrMagnitude > 0.001f)
                        ? Vector3.Angle(curN, _cling.faceNormal) : 0f;
                    if (!_cling.hasFace || sideTurn >= faceTurn - 2f)
                    {
                        hit = _cling.sideLHit;
                        return true;
                    }
                }
                if (_cling.hasFace && IsClimbableHit(_cling.faceHit) && !IsSelfHit(_cling.faceHit)
                    && Vector3.Angle(Vector3.up, _cling.faceNormal) > 55f)
                {
                    hit = _cling.faceHit;
                    return true;
                }
            }
            float[] heights = { 0.12f, 0.28f, 0.45f, 0.7f, 1.0f, 1.35f, 1.6f };
            // free-climb-dune-v3: fan wider so exterior/interior corners keep a cling hit.
            Vector3 upAxis = Vector3.up;
            Vector3 wallU = WallUp(_lastNormal.sqrMagnitude > 0.001f ? _lastNormal : -probeDir);
            Vector3[] dirs =
            {
                probeDir,
                Quaternion.AngleAxis(-35f, upAxis) * probeDir,
                Quaternion.AngleAxis(35f, upAxis) * probeDir,
                Quaternion.AngleAxis(-70f, upAxis) * probeDir,
                Quaternion.AngleAxis(70f, upAxis) * probeDir,
                Quaternion.AngleAxis(-95f, upAxis) * probeDir,
                Quaternion.AngleAxis(95f, upAxis) * probeDir,
                Quaternion.AngleAxis(-45f, wallU) * probeDir,
                Quaternion.AngleAxis(45f, wallU) * probeDir,
                Quaternion.AngleAxis(-80f, wallU) * probeDir,
                Quaternion.AngleAxis(80f, wallU) * probeDir,
            };
            bool any = false;
            for (int h = 0; h < heights.Length; h++)
            {
                Vector3 origin = transform.position + Vector3.up * heights[h];
                for (int d = 0; d < dirs.Length; d++)
                {
                    Vector3 dir = dirs[d];
                    if (dir.sqrMagnitude < 0.0001f)
                        continue;
                    dir.Normalize();
                    if (!TryProbeRange(origin, dir, stickRange + 0.5f, radius, out RaycastHit cand))
                        continue;
                    if (!IsClimbableHit(cand) || IsSelfHit(cand))
                        continue;
                    // Top of the block is walkable - do not snap cling onto it (that twitch-yaws at the lip).
                    if (Vector3.Angle(Vector3.up, cand.normal) <= 55f)
                        continue;
                    if (!any || cand.distance < hit.distance)
                        hit = cand;
                    any = true;
                }
            }

            // v1i: when strafing, always try corner wrap — pick it if the turn is bigger.
            Vector2 wrapAxes = ReadClimbAxes();
            if (Mathf.Abs(wrapAxes.x) > 0.12f)
            {
                if (TryStickCornerWrap(probeDir, stickRange, radius, out RaycastHit wrapHit))
                {
                    if (!any)
                    {
                        hit = wrapHit;
                        any = true;
                    }
                    else
                    {
                        Vector3 curN = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -probeDir.normalized;
                        float baseTurn = Vector3.Angle(curN, hit.normal);
                        float wrapTurn = Vector3.Angle(curN, wrapHit.normal);
                        if (wrapTurn > baseTurn + 12f || wrapHit.distance + 0.08f < hit.distance)
                            hit = wrapHit;
                    }
                }
            }
            else if (!any)
            {
                any = TryStickCornerWrap(probeDir, stickRange, radius, out hit);
            }
            return any;
        }


        /// <summary>
        /// Interior/exterior crease recovery when primary stick misses for a frame.
        /// </summary>
        private bool TryRecoverStickInCorner(Vector3 probeDir, float stickRange, float radius, out RaycastHit hit)
        {
            hit = default;
            if (_cling.valid)
            {
                Vector2 axes = ReadClimbAxes();
                if (axes.x > 0.08f && _cling.hasSideR && IsClimbableHit(_cling.sideRHit) && !IsSelfHit(_cling.sideRHit))
                {
                    hit = _cling.sideRHit;
                    return true;
                }
                if (axes.x < -0.08f && _cling.hasSideL && IsClimbableHit(_cling.sideLHit) && !IsSelfHit(_cling.sideLHit))
                {
                    hit = _cling.sideLHit;
                    return true;
                }
                if (_cling.hasFace && IsClimbableHit(_cling.faceHit) && !IsSelfHit(_cling.faceHit)
                    && Vector3.Angle(Vector3.up, _cling.faceNormal) > 55f)
                {
                    hit = _cling.faceHit;
                    return true;
                }
                Vector3 curN = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal : -probeDir;
                if (_cling.hasSideR && IsClimbableHit(_cling.sideRHit) && !IsSelfHit(_cling.sideRHit))
                {
                    float turnR = Vector3.Angle(curN, _cling.sideRHit.normal);
                    float turnL = (_cling.hasSideL && IsClimbableHit(_cling.sideLHit) && !IsSelfHit(_cling.sideLHit))
                        ? Vector3.Angle(curN, _cling.sideLHit.normal) : -1f;
                    hit = turnL > turnR ? _cling.sideLHit : _cling.sideRHit;
                    return true;
                }
                if (_cling.hasSideL && IsClimbableHit(_cling.sideLHit) && !IsSelfHit(_cling.sideLHit))
                {
                    hit = _cling.sideLHit;
                    return true;
                }
            }

            if (TryStickInteriorCorner(probeDir, stickRange, radius, out hit))
                return true;
            if (TryStickCornerWrap(probeDir, stickRange, radius, out hit))
                return true;
            return false;
        }

        /// <summary>
        /// Concave (interior) corner: cast from slightly into the pocket toward both walls.
        /// </summary>
        private bool TryStickInteriorCorner(Vector3 probeDir, float stickRange, float radius, out RaycastHit hit)
        {
            hit = default;
            Vector3 n = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -probeDir.normalized;
            Vector3 wallR = WallRightFrom(n);
            Vector3 wallU = WallUp(n);
            Vector2 raw = ReadClimbAxes();
            float side = Mathf.Abs(raw.x) > 0.08f ? Mathf.Sign(raw.x) : 0f;

            Vector3 chest = transform.position + Vector3.up * 1.05f;
            Vector3[] origins =
            {
                chest - n * 0.15f,
                chest - n * 0.28f + wallR * 0.2f,
                chest - n * 0.28f - wallR * 0.2f,
                chest + wallU * 0.15f - n * 0.2f,
            };
            float[] yaws = { 40f, 70f, 100f, -40f, -70f, -100f, 25f, -25f };
            bool any = false;
            for (int o = 0; o < origins.Length; o++)
            {
                for (int i = 0; i < yaws.Length; i++)
                {
                    float yaw = yaws[i];
                    if (side != 0f && Mathf.Sign(yaw) != side && Mathf.Abs(yaw) > 50f)
                        continue;
                    Vector3 dir = Quaternion.AngleAxis(yaw, wallU) * (-n);
                    if (dir.sqrMagnitude < 0.0001f)
                        continue;
                    dir.Normalize();
                    if (!TryProbeRange(origins[o], dir, stickRange + 0.9f, Mathf.Max(radius, 0.2f), out RaycastHit cand))
                        continue;
                    if (!IsClimbableHit(cand) || IsSelfHit(cand))
                        continue;
                    if (Vector3.Angle(Vector3.up, cand.normal) <= 55f)
                        continue;
                    if (!any || cand.distance < hit.distance)
                        hit = cand;
                    any = true;
                }
            }
            return any;
        }

        /// <summary>
        /// Cast around the current face using lateral input / wall-right so corners wrap like Dune/Conan.
        /// </summary>
        private bool TryStickCornerWrap(Vector3 probeDir, float stickRange, float radius, out RaycastHit hit)
        {
            hit = default;
            Vector3 n = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -probeDir.normalized;
            Vector3 wallR = WallRightFrom(n);
            Vector3 wallU = WallUp(n);
            Vector2 raw = ReadClimbAxes();
            float side = Mathf.Abs(raw.x) > 0.12f ? Mathf.Sign(raw.x) : 0f;

            float sidePush = side != 0f ? side : 1f;
            Vector3[] origins =
            {
                transform.position + Vector3.up * 1.05f,
                transform.position + Vector3.up * 0.7f,
                transform.position + Vector3.up * 1.35f,
                transform.position + Vector3.up * 1.05f + wallR * (sidePush * 0.35f),
                transform.position + Vector3.up * 1.05f + wallR * (sidePush * 0.55f),
                transform.position + Vector3.up * 1.05f + wallR * (sidePush * 0.75f),
                transform.position + Vector3.up * 0.85f + wallR * (sidePush * 0.45f) + n * 0.12f,
            };

            // v1i: denser yaw fan so exterior corners latch sooner.
            float[] yaws = { 35f, 50f, 65f, 80f, 95f, 110f, 130f, -35f, -50f, -65f, -80f, -95f, -110f, -130f };
            bool any = false;
            for (int o = 0; o < origins.Length; o++)
            {
                for (int i = 0; i < yaws.Length; i++)
                {
                    // Prefer the side player is pushing when strafing.
                    float yaw = yaws[i];
                    if (side != 0f && Mathf.Sign(yaw) != side && Mathf.Abs(yaw) > 45f)
                        continue;

                    Vector3 dir = Quaternion.AngleAxis(yaw, wallU) * (-n);
                    if (dir.sqrMagnitude < 0.0001f)
                        continue;
                    dir.Normalize();
                    if (!TryProbeRange(origins[o], dir, stickRange + 1.05f, Mathf.Max(radius, 0.24f), out RaycastHit cand))
                        continue;
                    if (!IsClimbableHit(cand) || IsSelfHit(cand))
                        continue;
                    if (Vector3.Angle(Vector3.up, cand.normal) <= 55f)
                        continue;
                    // Accept a real corner turn (normal differs from current face).
                    float turn = Vector3.Angle(n, cand.normal);
                    if (turn < 12f && cand.distance > 0.45f)
                        continue;
                    // Prefer larger turns when distances are similar (true wrap).
                    if (!any || cand.distance + 0.05f < hit.distance
                        || (Mathf.Abs(cand.distance - hit.distance) < 0.12f && turn > Vector3.Angle(n, hit.normal) + 8f))
                        hit = cand;
                    any = true;
                }
            }

            return any;
        }

        private bool TryStickLastHit(Vector3 probeDir, float stickRange, float radius, out RaycastHit hit)
        {
            hit = default;
            if (!_hasLastStick)
                return false;

            Vector3 n = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -probeDir;
            Vector3 off = _lastStickHit.point + n * 0.14f;
            float[] dy = { 0f, 0.18f, -0.18f, 0.36f, -0.32f };
            bool any = false;
            for (int i = 0; i < dy.Length; i++)
            {
                Vector3 o = off + Vector3.up * dy[i];
                if (!TryProbeRange(o, -n, stickRange + 0.6f, radius, out RaycastHit cand))
                    continue;
                if (!IsClimbableHit(cand) || IsSelfHit(cand))
                    continue;
                if (!any || cand.distance < hit.distance)
                    hit = cand;
                any = true;
            }
            return any;
        }

        private void SnapLipHang(ref Vector3 pos)
        {
            if (_overhangHang)
            {
                float hhHang = profile != null ? profile.handHeight : 1.18f;
                pos.y = _overhangLip.point.y - hhHang;
                return;
            }
            if (TryProbeSoffit(_lastNormal, out RaycastHit soffit))
            {
                float hhSoffit = profile != null ? profile.handHeight : 1.18f;
                pos.y = Mathf.Min(pos.y, soffit.point.y - hhSoffit);
                return;
            }
            if (AtHandLip(_lastNormal, out RaycastHit lip))
            {
                float hh = profile != null ? profile.handHeight : 1.18f;
                pos.y = lip.point.y - hh;
                return;
            }
        }

        private bool AtHandLip(Vector3 wallNormal, out RaycastHit top)
        {
            top = default;
            Vector3 forward = Flatten(-wallNormal);
            if (forward.sqrMagnitude < 0.001f)
                forward = Flatten(transform.forward);

            // Hands ~1.15m. Prefer the highest walkable top in band — thick mid slabs
            // used to win with ClosestPoint / lowest-Y mid-thickness hits.
            // Include shallow overs so thin/short lips still register.
            bool thickContext = _overhangHang || _overhangGrabbing || _overhangLip.collider != null;
            float[] overs = { 0.08f, 0.14f, 0.2f, 0.32f, 0.48f, 0.7f, 0.95f };
            float[] ups = thickContext
                ? new float[] { 1.18f, 1.35f, 1.55f, 1.75f, 1.95f, 2.15f }
                : new float[] { 1.18f, 1.28f, 1.42f, 1.55f };
            float maxAbove = thickContext ? 2.15f : 1.7f;
            float castDown = thickContext ? 1.15f : 0.7f;
            // mantle-simple-v2c: pick outer rim near hand height — highest/deep deck samples put grabs above the edge.
            float handY = transform.position.y + (profile != null ? Mathf.Clamp(profile.handHeight, 0.95f, 1.35f) : 1.12f);
            RaycastHit best = default;
            float bestScore = float.MaxValue;
            bool any = false;
            for (int u = 0; u < ups.Length; u++)
            {
                for (int i = 0; i < overs.Length; i++)
                {
                    Vector3 over = transform.position + Vector3.up * ups[u] + forward * overs[i];
                    if (!SurfaceCast(over, Vector3.down, castDown, 0.06f, out RaycastHit hit) || IsSelfHit(hit))
                        continue;
                    if (Vector3.Angle(Vector3.up, hit.normal) > 50f)
                        continue;
                    if (hit.point.y < transform.position.y + 0.85f)
                        continue;
                    if (hit.point.y > transform.position.y + maxAbove)
                        continue;
                    // Prefer near hands + least onto the deck (small forward over).
                    float onto = Vector3.Dot(hit.point - transform.position, forward);
                    float score = Mathf.Abs(hit.point.y - handY) * 1.4f + onto * 0.85f + overs[i] * 0.2f;
                    if (!any || score < bestScore)
                    {
                        best = hit;
                        bestScore = score;
                        any = true;
                    }
                }
            }

            if (!any)
                return false;
            top = best;
            return true;
        }

        private void ClearOverhangState()
        {
            _overhangHang = false;
            _overhangGrabbing = false;
            _overhangGrabAt = -10f;
            _overhangProtrusion = 0f;
            _overhangDeepHop = false;
            _overhangPreferMantle = false;
#if UNITY_EDITOR
            _gizmoOverhangValid = false;
#endif
        }

        /// <summary>
        /// True when the lip has real top/soffit geometry — false means empty air (do not hang).
        /// </summary>
        private bool LipHasSupportAboveOrOnTop(RaycastHit lip)
        {
            if (lip.collider == null)
                return false;
            if (_cling.valid && (_cling.hasLip || _cling.hasSoffit))
                return true;

            // Cast up from just above the lip and down onto the deck near the rim.
            Vector3 upOrigin = lip.point + Vector3.up * 0.05f;
            if (SurfaceCast(upOrigin, Vector3.up, 0.85f, 0.06f, out RaycastHit upHit)
                && !IsSelfHit(upHit)
                && (IsSoffitNormal(upHit.normal) || Vector3.Angle(Vector3.up, upHit.normal) <= 60f))
                return true;

            Vector3 back = Flatten(_lastNormal);
            if (back.sqrMagnitude < 0.0001f)
                back = Flatten(transform.forward);
            Vector3 deckOrigin = lip.point + back * 0.12f + Vector3.up * 0.55f;
            if (SurfaceCast(deckOrigin, Vector3.down, 0.9f, 0.08f, out RaycastHit deck)
                && !IsSelfHit(deck)
                && Vector3.Angle(Vector3.up, deck.normal) <= 60f
                && deck.point.y >= lip.point.y - 0.15f)
                return true;

            return false;
        }

        /// <summary>Bail hang when body is floating with no lip/soffit/face contact.</summary>
        private bool TryEscapeOrphanOverhangHang()
        {
            if (!_overhangHang && !_overhangGrabbing)
                return false;

            bool lipOk = _overhangLip.collider != null && LipHasSupportAboveOrOnTop(_overhangLip);
            bool soffitOk = (_cling.valid && _cling.hasSoffit) || TryProbeSoffit(_lastNormal, out _);
            bool faceOk = (_cling.valid && _cling.hasFace)
                || TryStickWall(-(_lastNormal.sqrMagnitude > 0.001f ? _lastNormal : transform.forward),
                    profile != null ? profile.attachRange + 0.65f : 2.1f,
                    profile != null ? profile.probeRadius : 0.2f,
                    out _);

            if (lipOk || soffitOk)
                return false;

            // Floating: no support above and preferably no face either — clear hang.
            Debug.Log($"[{BuildStamp}] orphan hang escape face={faceOk} lip={lipOk} soffit={soffitOk}");
            if (TryResumeClimbFromOverhang())
                return true;
            ClearOverhangState();
            _lipHang = false;
            _overhangResumeAt = Time.unscaledTime + 0.3f;
            if (!faceOk)
            {
                DropFromClimb();
                return true;
            }
            return true;
        }

        private bool TryStartOverhangGrab()
        {
            if (profile == null || !profile.enableOverhangGrab)
                return false;
            if (_overhangHang || _overhangGrabbing)
                return true;
            if (Time.unscaledTime < _overhangResumeAt)
                return false;
            if (!WantsOverhangClimbUp())
                return false;

            if (!_cling.valid && ClingSenseEnabled())
                RefreshClingSense();

            // mantle-simple-v2: prefer AtHandLip; cling lip is optional foresight only.
            bool bubbleLip = _cling.valid && _cling.hasLip;
            bool bubbleSoffit = _cling.valid && _cling.hasSoffit;
            bool handLip = AtHandLip(_lastNormal, out RaycastHit handRim);
            if (!bubbleLip && !bubbleSoffit && !handLip)
                return false;

            if (handLip && !bubbleLip)
            {
                _overhangLip = handRim;
                _overhangPreferMantle = true;
                if (ForceMantleOverLip(_lastNormal) || TryMantle(_lastNormal, requireUp: false))
                    return true;
            }

            if (bubbleLip && _cling.isStubLip)
            {
                _overhangLip = _cling.lipHit;
                _overhangProtrusion = _cling.lipProtrusion;
                _overhangPreferMantle = true;
                Debug.Log($"[{BuildStamp}] bubble stub lip -> mantle/face protrude={_cling.lipProtrusion:F2}");
                if (TryMantle(_lastNormal, requireUp: false) || ForceMantleOverLip(_lastNormal))
                    return true;
                _overhangResumeAt = Time.unscaledTime + 0.35f;
                ClearOverhangState();
                _lipHang = false;
                return false;
            }

            RaycastHit lip;
            if (bubbleLip && _cling.isDeepLip)
                lip = _cling.lipHit;
            else if (!FindOverhangLip(_lastNormal, out lip))
                return false;

            // Reject lips with no nearby walkable/soffit support (hang-on-nothing).
            if (!LipHasSupportAboveOrOnTop(lip))
            {
                Debug.Log($"[{BuildStamp}] overhang REJECT unsupported lip");
                _overhangResumeAt = Time.unscaledTime + 0.25f;
                return false;
            }

            return BeginOverhangGrab(lip);
        }


        private bool WantsOverhangClimbUp()
        {
            Vector2 raw = ReadClimbAxes();
            return raw.y > 0.12f;
        }

        private bool BeginOverhangGrab(RaycastHit lip)
        {
            if (_overhangHang || _overhangGrabbing)
                return true;
            // lip8: never enter hang/grab on thick-slab underside / mid-thickness rim.
            if (LipContactIsThickSlabUnderside(lip) || LipIsUndersideCorner(lip, _lastNormal))
            {
                if (RefineLipToTopEdge(lip, _lastNormal, out RaycastHit topLip)
                    && IsWalkableLipNormal(topLip.normal)
                    && !LipContactIsThickSlabUnderside(topLip)
                    && !LipIsUndersideCorner(topLip, _lastNormal))
                {
                    lip = topLip;
                }
                else
                {
                    Debug.Log($"[{BuildStamp}] BeginOverhangGrab REJECT underside");
                    return false;
                }
            }

            Vector3 start = transform.position;
            Vector3 hang = OverhangHangPos(lip);
            Vector3 delta = hang - start;
            float planar = new Vector2(delta.x, delta.z).magnitude;
            float protrusion = MeasureLipProtrusion(lip);
            _overhangProtrusion = protrusion;
            _overhangDeepHop = protrusion >= OverhangDeepProtrusion();
            _overhangPreferMantle = !_overhangDeepHop;
            Debug.Log($"[{BuildStamp}] BeginOverhangGrab protrude={protrusion:F2} deep={_overhangDeepHop}");
            if (delta.y > OverhangMaxLift || delta.y < -0.35f)
                return false;
            if (planar > OverhangReachBack() + 0.4f)
                return false;
            if (delta.sqrMagnitude > OverhangMaxGrab * OverhangMaxGrab)
                return false;
            if (delta.sqrMagnitude < 0.008f)
            {
                _overhangLip = lip;
                _lastStickHit = lip;
                _hasLastStick = true;
                _lipHang = true;
                MoveBody(hang);
                SetOverhangHandIk(1f);
                // free-climb-dune-v5: short/shallow lips must not zero-input hang-lock.
                if (_overhangPreferMantle)
                {
                    _overhangGrabbing = false;
                    if (TryMantle(_lastNormal, requireUp: false) || ForceMantleOverLip(_lastNormal))
                        return true;
                    // Mantle miss: stay on face climb under the short lip.
                    ClearOverhangState();
                    _overhangResumeAt = Time.unscaledTime + 0.25f;
                    _lipHang = false;
                    Debug.Log($"[{BuildStamp}] short lip -> face (no hang lock) protrude={protrusion:F2}");
                    return true;
                }
                _overhangHang = true;
                _overhangDeepHop = false;
                return true;
            }

            float climbSpeed = profile != null ? Mathf.Max(0.45f, profile.moveSpeed) : 0.85f;
            float minDur = profile != null ? Mathf.Max(0.15f, profile.overhangGrabSeconds) : 0.35f;

            _overhangLip = lip;
            _lastStickHit = lip;
            _hasLastStick = true;
            _overhangGrabStart = start;
            _overhangGrabEnd = hang;

            if (_overhangDeepHop)
            {
                // Deep lip: short-hop body under the lip before IK so arms never stretch.
                float pull = profile != null ? Mathf.Clamp(profile.overhangShortHopPull, 0.55f, 0.98f) : 0.94f;
                float arc = profile != null ? Mathf.Clamp(profile.overhangShortHopArc, 0f, 0.45f) : 0.22f;
                Vector3 mid = Vector3.Lerp(start, hang, pull);
                // Bias mid onto the hang planar so the body is under the lip early.
                mid.x = Mathf.Lerp(mid.x, hang.x, 0.55f);
                mid.z = Mathf.Lerp(mid.z, hang.z, 0.55f);
                mid.y = Mathf.Max(mid.y, Mathf.Max(start.y, hang.y) + arc);
                _overhangGrabMid = mid;
                float hopSec = profile != null ? Mathf.Max(0.55f, profile.overhangShortHopSeconds) : 0.85f;
                // Deep hop: hopSec only - never Min/blend with speedDur (lip6).
                _overhangGrabDur = Mathf.Clamp(hopSec, 0.55f, 1.25f);
                _ikWeight = 0f;
                _ikValid = false;
            }
            else
            {
                // Shallow lip: climb up the wall first, then a short pull onto the lip.
                _overhangGrabMid = new Vector3(start.x, hang.y, start.z);
                float speedDur = delta.magnitude / climbSpeed;
                // Never shorten below overhangGrabSeconds (no Min that clips the hop).
                _overhangGrabDur = Mathf.Clamp(Mathf.Max(minDur, speedDur), minDur, 1.8f);
                SetOverhangHandIk(1f);
            }

            _overhangGrabAt = Time.unscaledTime;
            _overhangGrabbing = true;
            _lipHang = true;
            return true;
        }

        private void TickOverhangGrab()
        {
            if (!_overhangGrabbing)
                return;

            float dur = Mathf.Max(0.15f, _overhangGrabDur);
            float t = Mathf.Clamp01((Time.unscaledTime - _overhangGrabAt) / dur);
            Vector3 pos;
            float planar = new Vector2(
                _overhangGrabEnd.x - _overhangGrabStart.x,
                _overhangGrabEnd.z - _overhangGrabStart.z).sqrMagnitude;
            if (planar > 0.01f)
            {
                // Deep short-hop spends more of the lerp pulling onto the lip.
                float split = _overhangDeepHop ? 0.4f : 0.62f;
                if (t < split)
                {
                    float u = Mathf.SmoothStep(0f, 1f, t / split);
                    pos = Vector3.Lerp(_overhangGrabStart, _overhangGrabMid, u);
                }
                else
                {
                    float u = Mathf.SmoothStep(0f, 1f, (t - split) / Mathf.Max(0.01f, 1f - split));
                    pos = Vector3.Lerp(_overhangGrabMid, _overhangGrabEnd, u);
                }
            }
            else
            {
                pos = Vector3.Lerp(_overhangGrabStart, _overhangGrabEnd, Mathf.SmoothStep(0f, 1f, t));
            }

            MoveBody(pos);
            FaceWall(_lastNormal);
            if (_overhangDeepHop)
                SetOverhangHandIk(OverhangGrabIkWeight(t, pos));
            else
                SetOverhangHandIk(1f);
            float climbSpeed = profile != null ? profile.moveSpeed : 0.85f;
            WriteAnimator(new Vector2(0f, 1f), climbSpeed, climbing: true);

            if (_overhangLip.transform != null)
            {
                _anchor = _overhangLip.transform;
                _localOffset = _anchor.InverseTransformPoint(pos);
                _prevAnchorPos = _anchor.position;
                _hasPrevAnchorPos = true;
            }

            if (t < 1f)
                return;

            _overhangGrabbing = false;
            bool preferMantle = _overhangPreferMantle;
            _overhangDeepHop = false;
            MoveBody(_overhangGrabEnd);
            SetOverhangHandIk(1f);
            _lipHang = true;

                        if (preferMantle)
            {
                // free-climb-dune-v5: short lips mantle immediately; never park forever under a stub ledge.
                _overhangPreferMantle = true;
                if (TryMantle(_lastNormal, requireUp: false) || ForceMantleOverLip(_lastNormal))
                    return;
                if (WantsOverhangClimbUp()
                    && (TryMantle(_lastNormal, requireUp: true) || ForceMantleOverLip(_lastNormal)))
                    return;
                // Still no mantle: drop hang lock and resume face stick (short protrusion = face is right there).
                ClearOverhangState();
                _overhangResumeAt = Time.unscaledTime + 0.2f;
                _lipHang = false;
                if (TryResumeClimbFromOverhang())
                {
                    Debug.Log($"[{BuildStamp}] short lip grab done -> face resume");
                    return;
                }
                // TryResume needs hang flags; clear already happened — probe face directly.
                Vector3 wallN = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -transform.forward;
                float stickRange = profile != null ? profile.attachRange + 0.85f : 2.2f;
                float radius = profile != null ? profile.probeRadius : 0.18f;
                if (TryProbeClimbableUnderLip(wallN, stickRange, radius, out RaycastHit face)
                    || TryStickWall(-Flatten(wallN), stickRange, radius, out face))
                {
                    _lastStickHit = face;
                    _hasLastStick = true;
                    if (face.normal.sqrMagnitude > 0.001f)
                        _lastNormal = face.normal.normalized;
                    Debug.Log($"[{BuildStamp}] short lip grab done -> face stick");
                }
                return;
            }

            // Deep protrusion: park in hang under the lip (short-hop already closed the gap).
            // If climb-up is already held, try mantle immediately so mid does not lock shimmy-only.
            _overhangHang = true;
            _overhangPreferMantle = false;
            if (WantsOverhangClimbUp()
                && (TryMantle(_lastNormal, requireUp: true) || ForceMantleOverLip(_lastNormal)))
                return;
        }

        private float OverhangGrabIkWeight(float grabT, Vector3 bodyPos)
        {
            float blendStart = profile != null
                ? Mathf.Clamp(profile.overhangIkBlendStart, 0f, 0.9f)
                : 0.72f;
            float fromT = Mathf.InverseLerp(blendStart, 1f, grabT);
            Vector3 toLip = _overhangGrabEnd - bodyPos;
            toLip.y = 0f;
            float planarLeft = toLip.magnitude;
            float fromDist = Mathf.InverseLerp(Mathf.Max(0.45f, _overhangProtrusion * 0.85f), 0.1f, planarLeft);
            // Require both time gate and remaining planar gap — Max was blending IK too early on deep lips.
            return Mathf.Clamp01(Mathf.Min(fromT, fromDist));
        }

        /// <summary>Keep mantle/hang plant from flying past the lip along the onto-top axis.</summary>
        private static Vector3 ClampStandPastLip(Vector3 stand, Vector3 lipPoint, Vector3 onto, float maxPast)
        {
            if (onto.sqrMagnitude < 0.0001f)
                return stand;
            onto.Normalize();
            float along = Vector3.Dot(stand - lipPoint, onto);
            if (along > maxPast)
                stand -= onto * (along - maxPast);
            return stand;
        }

        private Vector3 OverhangHangPos(RaycastHit lip)
        {
            // mantle-simple-v2c: body under the lip corner — never root so high hands sit on/above the deck.
            RaycastHit rim = lip;
            if (RefineLipToTopEdge(lip, _lastNormal, out RaycastHit refined)
                && IsWalkableLipNormal(refined.normal))
            {
                // Keep the more hang-side of the two (don't climb onto the deck with refine).
                Vector3 backN = Flatten(_lastNormal);
                if (backN.sqrMagnitude > 0.001f
                    && Vector3.Dot(refined.point - lip.point, -backN) > 0.06f)
                    rim = lip;
                else
                    rim = refined;
            }

            Vector3 back = Flatten(_lastNormal);
            if (back.sqrMagnitude < 0.001f)
                back = Flatten(transform.forward);
            // Profile handHeight slid to ~0.93 which parks the root too high (grab above edge).
            float hh = profile != null ? profile.handHeight : 1.12f;
            hh = Mathf.Clamp(Mathf.Max(hh, 1.08f), 1.08f, 1.35f);
            float standOff = profile != null ? Mathf.Clamp(profile.standOff, 0.28f, 0.42f) : 0.32f;

            Vector3 hang;
            if (_hasLastStick && Vector3.Angle(Vector3.up, _lastStickHit.normal) > 50f)
                hang = _lastStickHit.point + back * standOff;
            else
                hang = rim.point + back * standOff;

            hang.y = rim.point.y - hh - 0.06f;
            // Hard: never past rim onto deck.
            float past = Vector3.Dot(hang - rim.point, -back);
            if (past > -0.02f)
                hang += back * (past + 0.08f);

            _overhangLip = rim;
            return hang;
        }

        private void HoldOverhangHang(Vector2 raw)
        {
            _lipHang = true;

            // lip8: any illegal underside hang — immediate escape (do not shimmy-lock under slab).
            if (OverhangHangIsIllegalUnderside())
            {
                if (!TryEscapeIllegalOverhangHang())
                    DropFromClimb();
                return;
            }

            float damp = profile != null ? profile.climbInputDamp : 0.1f;
            _dampedClimbInput = Vector3.SmoothDamp(
                _dampedClimbInput,
                new Vector3(raw.x, 0f, 0f),
                ref _dampedClimbVel,
                damp);

            float speed = profile != null ? profile.moveSpeed : 1.6f;
            if (ReadShiftHeld())
                speed *= profile != null ? profile.climbShiftMul : 1.35f;

            Vector3 right = WallRight(_lastNormal);
            Vector3 back = Flatten(_lastNormal);
            if (back.sqrMagnitude < 0.001f)
                back = Flatten(transform.forward);

            // lip7: A/D shimmy along the known lip. Do NOT re-call FindOverhangLip every
            // frame (that recenters to hands and cancels lateral travel).
            if (Mathf.Abs(_dampedClimbInput.x) > 0.04f && _overhangLip.collider != null)
            {
                Vector3 slidPt = _overhangLip.point
                    + right * (_dampedClimbInput.x * speed * Time.fixedDeltaTime);
                Vector3 origin = slidPt + Vector3.up * 0.55f + back * 0.04f;
                if (SurfaceCast(origin, Vector3.down, 0.95f, 0.06f, out RaycastHit rim)
                    && !IsSelfHit(rim)
                    && IsWalkableLipNormal(rim.normal)
                    && !LipIsUndersideCorner(rim, _lastNormal))
                {
                    _overhangLip = rim;
                    _lastStickHit = rim;
                }
            }

            Vector3 hang = OverhangHangPos(_overhangLip);
            hang += right * (_dampedClimbInput.x * speed * Time.fixedDeltaTime);

            if (IsSaneMove(hang))
                MoveBody(hang);
            FaceWall(_lastNormal);
            SetOverhangHandIk();

            if (_overhangLip.transform != null)
            {
                _anchor = _overhangLip.transform;
                _localOffset = _anchor.InverseTransformPoint(hang);
                _prevAnchorPos = _anchor.position;
                _hasPrevAnchorPos = true;
            }

            float climbSpeed = Mathf.Abs(_dampedClimbInput.x) * speed;
            WriteAnimator(new Vector2(_dampedClimbInput.x, 0f), climbSpeed, climbing: true);
        }

        private bool WantResumeClimbFromOverhang(Vector2 raw)
        {
            // Lateral / down resume wall travel. Climb-up stays in hang so mid mantle can retry.
            return Mathf.Abs(raw.x) > 0.22f || raw.y < -0.2f;
        }

        private bool TryResumeClimbFromOverhang()
        {
            Vector3 wallN = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -transform.forward;
            Vector3 probeDir = -Flatten(wallN);
            if (probeDir.sqrMagnitude < 0.001f)
                probeDir = Flatten(transform.forward);
            float stickRange = profile != null ? profile.attachRange + 0.85f : 2.2f;
            float radius = profile != null ? profile.probeRadius : 0.18f;
            RaycastHit hit;
            bool found = TryStickWall(probeDir, stickRange, radius, out hit)
                || TryStickLastHit(probeDir, stickRange, radius, out hit)
                || TryProbeClimbableUnderLip(wallN, stickRange, radius, out hit);

            if (!found)
                return false;

            _overhangHang = false;
            _overhangGrabbing = false;
            _overhangPreferMantle = false;
            _overhangResumeAt = Time.unscaledTime + 0.45f;
            _ikValid = false;
            _ikWeight = 0f;
            _lastStickHit = hit;
            _hasLastStick = true;
            if (hit.normal.sqrMagnitude > 0.001f)
                _lastNormal = hit.normal.normalized;
            _lipHang = false;
            Debug.Log($"[{BuildStamp}] resume face ok nY={_lastNormal.y:F2}");
            return true;
        }

        /// <summary>
        /// lip8: from hang / under-lip, spray for a vertical Climbable face below the known lip.
        /// Body may sit outward past the face so body-only TryStickWall misses.
        /// </summary>
        private bool TryProbeClimbableUnderLip(Vector3 wallNormal, float stickRange, float radius, out RaycastHit hit)
        {
            hit = default;
            Vector3 wallN = wallNormal.sqrMagnitude > 0.001f ? wallNormal.normalized : _lastNormal;
            Vector3 into = -Flatten(wallN);
            if (into.sqrMagnitude < 0.001f)
                into = Flatten(transform.forward);
            Vector3 right = WallRight(wallN);
            Vector3 lipPt = _overhangLip.collider != null
                ? _overhangLip.point
                : transform.position + Vector3.up * (profile != null ? profile.handHeight : 1.18f);

            float[] outs = { 0.04f, 0.12f, 0.22f, 0.36f, 0.5f };
            float[] downs = { 0.1f, 0.28f, 0.5f, 0.75f, 1.05f, 1.4f, 1.8f };
            float[] lats = { 0f, -0.22f, 0.22f, -0.4f, 0.4f };
            bool any = false;
            for (int o = 0; o < outs.Length; o++)
            {
                for (int d = 0; d < downs.Length; d++)
                {
                    for (int l = 0; l < lats.Length; l++)
                    {
                        Vector3 origin = lipPt + wallN * outs[o] - Vector3.up * downs[d] + right * lats[l];
                        if (!TryProbeRange(origin, into, stickRange, radius, out RaycastHit cand))
                            continue;
                        if (!IsClimbableHit(cand) || IsSelfHit(cand))
                            continue;
                        // Vertical-ish Climbable face only (not top deck).
                        if (Vector3.Angle(Vector3.up, cand.normal) <= 50f)
                            continue;
                        if (IsSoffitNormal(cand.normal) || LipContactIsThickSlabUnderside(cand))
                            continue;
                        if (!any || cand.distance < hit.distance)
                        {
                            hit = cand;
                            any = true;
                        }
                    }
                }
            }

            // Also pull origins from the current body toward the wall (hang inset sits outboard).
            if (!any)
            {
                float[] pull = { 0f, 0.15f, 0.3f, 0.45f };
                float[] heights = { 0.2f, 0.45f, 0.75f, 1.05f, 1.35f };
                for (int p = 0; p < pull.Length; p++)
                {
                    for (int h = 0; h < heights.Length; h++)
                    {
                        Vector3 origin = transform.position - wallN * pull[p] + Vector3.up * heights[h];
                        if (!TryProbeRange(origin, into, stickRange + 0.35f, radius, out RaycastHit cand))
                            continue;
                        if (!IsClimbableHit(cand) || IsSelfHit(cand))
                            continue;
                        if (Vector3.Angle(Vector3.up, cand.normal) <= 50f)
                            continue;
                        if (IsSoffitNormal(cand.normal))
                            continue;
                        if (!any || cand.distance < hit.distance)
                        {
                            hit = cand;
                            any = true;
                        }
                    }
                }
            }
            return any;
        }

        /// <summary>
        /// Hard ban: overhang/hang contact on the bottom face / mid-thickness of a thick slab.
        /// Top outer lip only (near collider AABB top). Vertical Climbable faces are not checked here.
        /// </summary>
        private bool LipContactIsThickSlabUnderside(RaycastHit lip)
        {
            if (lip.collider == null)
                return false;
            if (IsSoffitNormal(lip.normal) || lip.normal.y < -0.05f)
                return true;

            Bounds b = lip.collider.bounds;
            float thick = b.size.y;
            if (thick < 0.28f)
                return false;

            float top = b.max.y;
            float eps = Mathf.Clamp(thick * 0.22f, 0.1f, 0.55f);
            // Claimed lip sits below this slab's top deck → underside / mid-slab rim.
            if (lip.point.y < top - eps)
                return true;
            return false;
        }

        /// <summary>
        /// True when current overhang hang is parked under a thick soffit / bottom face.
        /// </summary>
        private bool OverhangHangIsIllegalUnderside()
        {
            // Legal top-lip hang often has a short soffit above the chest — that is OK.
            // Illegal = hang contact on thick bottom/mid face, or body trapped deep under thick AABB.
            if (_overhangLip.collider != null
                && (LipContactIsThickSlabUnderside(_overhangLip) || LipIsUndersideCorner(_overhangLip, _lastNormal)))
                return true;

            if (_overhangLip.collider != null)
            {
                Bounds b = _overhangLip.collider.bounds;
                if (b.size.y >= 0.28f)
                {
                    Vector3 chest = transform.position + Vector3.up * 0.95f;
                    Vector3 head = transform.position + Vector3.up * 1.35f;
                    if ((b.Contains(chest) || b.Contains(head)) && chest.y < b.max.y - 0.08f)
                        return true;
                    // Deep under this slab's top deck with the slab soffit immediately above.
                    float belowTop = b.max.y - chest.y;
                    if (belowTop > 0.45f
                        && SurfaceCast(chest, Vector3.up, belowTop + 0.2f, 0.06f, out RaycastHit up)
                        && !IsSelfHit(up)
                        && up.collider == _overhangLip.collider
                        && (IsSoffitNormal(up.normal) || up.normal.y < 0.2f))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Immediate escape from illegal underside hang: promote top lip, else ClearOverhang + face stick.
        /// Never leave a kinematic hang that zeroes StickAndMove under a soffit.
        /// </summary>
        private bool TryEscapeIllegalOverhangHang()
        {
            // Prefer promote to true top outer lip when reachable.
            if (_overhangLip.collider != null
                && RefineLipToTopEdge(_overhangLip, _lastNormal, out RaycastHit topLip)
                && IsWalkableLipNormal(topLip.normal)
                && !LipIsUndersideCorner(topLip, _lastNormal)
                && !LipContactIsThickSlabUnderside(topLip)
                && topLip.point.y > _overhangLip.point.y + 0.12f)
            {
                _overhangLip = topLip;
                _lastStickHit = topLip;
                Vector3 hang = OverhangHangPos(topLip);
                if (IsSaneMove(hang))
                    MoveBody(hang);
                SetOverhangHandIk();
                Debug.Log($"[{BuildStamp}] underside escape -> top lip y={topLip.point.y:F2}");
                return true; // still in legal hang
            }

            if (TryMantle(_lastNormal, requireUp: false) || ForceMantleOverLip(_lastNormal))
            {
                Debug.Log($"[{BuildStamp}] underside escape -> mantle");
                return true;
            }

            if (TryResumeClimbFromOverhang())
            {
                Debug.Log($"[{BuildStamp}] underside escape -> face stick");
                return true;
            }

            ClearOverhangState();
            _overhangResumeAt = Time.unscaledTime + 0.4f;
            _lipHang = false;
            // One more face probe after clear (same frame fall-through uses this stick).
            Vector3 wallN = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -transform.forward;
            float stickRange = profile != null ? profile.attachRange + 0.85f : 2.2f;
            float radius = profile != null ? profile.probeRadius : 0.18f;
            if (TryProbeClimbableUnderLip(wallN, stickRange, radius, out RaycastHit face)
                || TryStickWall(-Flatten(wallN), stickRange, radius, out face))
            {
                _lastStickHit = face;
                _hasLastStick = true;
                if (face.normal.sqrMagnitude > 0.001f)
                    _lastNormal = face.normal.normalized;
                Debug.Log($"[{BuildStamp}] underside escape -> ClearOverhang + face");
                return true;
            }

            Debug.Log($"[{BuildStamp}] underside escape FAIL (no face)");
            return false;
        }

        private bool IsParkedUnderLedge()
        {
            // free-climb-dune-v5: deep hang/grab is a real park; short prefer-mantle is not.
            if (_overhangGrabbing)
                return true;
            if (_overhangHang && !_overhangPreferMantle)
                return true;
            return TryProbeSoffit(_lastNormal, out _);
        }

        private void SetOverhangHandIk(float weight = 1f)
        {
            Vector3 back = Flatten(_lastNormal);
            if (back.sqrMagnitude < 0.001f)
                back = Flatten(transform.forward);
            Vector3 right = WallRight(_lastNormal);
            float spread = profile != null ? Mathf.Clamp(profile.handSpread, 0.12f, 0.28f) : 0.18f;

            Vector3 rim = _overhangLip.collider != null ? _overhangLip.point : transform.position + Vector3.up * 1.1f;
            if (_overhangLip.collider != null
                && RefineLipToTopEdge(_overhangLip, _lastNormal, out RaycastHit rimHit)
                && IsWalkableLipNormal(rimHit.normal))
            {
                // Prefer hang-side candidate.
                if (Vector3.Dot(rimHit.point - rim, -back) <= 0.06f)
                    rim = rimHit.point;
            }

            // Grip the underside/outer corner of the rim — not on the top deck (clips through edge).
            Vector3 grab = rim + back * 0.12f + Vector3.down * 0.05f;
            float past = Vector3.Dot(grab - rim, -back);
            if (past > 0f)
                grab += back * (past + 0.08f);

            _ikLeft = grab - right * spread;
            _ikRight = grab + right * spread;
            weight = Mathf.Clamp01(weight);
            _ikValid = weight > 0.02f;
            _ikWeight = weight;
        }

        private static bool IsSoffitNormal(Vector3 normal)
        {
            return normal.y < -0.2f;
        }

        private static bool IsWalkableLipNormal(Vector3 normal)
        {
            return Vector3.Angle(Vector3.up, normal) <= 60f;
        }

        /// <summary>
        /// True when the candidate lip is still the underside / bottom outer corner of a thick slab.
        /// </summary>
        private bool LipIsUndersideCorner(RaycastHit lip, Vector3 wallNormal)
        {
            // Reject soffit / mostly-down normals (underside and bottom outer corner).
            if (IsSoffitNormal(lip.normal) || lip.normal.y < -0.05f)
                return true;
            // lip8: hard AABB ban — contact below thick slab top deck is not a lip.
            if (LipContactIsThickSlabUnderside(lip))
                return true;
            Vector3 back = Flatten(wallNormal);
            if (back.sqrMagnitude < 0.001f)
                back = Flatten(transform.forward);

            // Point sits below a clear top deck on this volume -> mid-thickness / underside rim.
            Vector3 deckOrigin = lip.point + back * 0.05f + Vector3.up * 1.85f;
            if (SurfaceCast(deckOrigin, Vector3.down, 2.2f, 0.06f, out RaycastHit deck)
                && !IsSelfHit(deck)
                && IsWalkableLipNormal(deck.normal)
                && deck.point.y > lip.point.y + 0.18f)
            {
                Vector3 planar = deck.point - lip.point;
                planar.y = 0f;
                if (deck.collider == lip.collider || planar.sqrMagnitude < 1.35f)
                    return true;
            }

            // From just above the lip, a short up-cast must NOT immediately hit soffit
            // (that means we grabbed the bottom rim, not the top deck).
            Vector3 above = lip.point + Vector3.up * 0.08f + back * 0.02f;
            if (SurfaceCast(above, Vector3.up, 0.85f, 0.05f, out RaycastHit upHit)
                && !IsSelfHit(upHit)
                && IsSoffitNormal(upHit.normal)
                && upHit.distance < 0.7f)
            {
                // Allow if a clear top deck sits higher on this same volume.
                if (RefineLipToTopEdge(lip, wallNormal, out RaycastHit raised)
                    && raised.point.y > lip.point.y + 0.2f
                    && IsWalkableLipNormal(raised.normal))
                    return false;
                return true;
            }
            return false;
        }

        private bool HangBodyIsUnderSoffit()
        {
            Vector3 chest = transform.position + Vector3.up * 0.95f;
            Vector3 head = transform.position + Vector3.up * 1.25f;
            float[] reaches = { 0.85f, 1.15f };
            Vector3[] origins = { chest, head, transform.position + Vector3.up * 0.7f };
            for (int o = 0; o < origins.Length; o++)
            {
                for (int r = 0; r < reaches.Length; r++)
                {
                    if (!SurfaceCast(origins[o], Vector3.up, reaches[r], 0.06f, out RaycastHit hit) || IsSelfHit(hit))
                        continue;
                    if (IsSoffitNormal(hit.normal) && hit.distance < 0.95f)
                        return true;
                    // Thick slab bottom face with shallow normal still counts as under.
                    if (hit.collider != null && hit.normal.y < 0.15f && LipContactIsThickSlabUnderside(hit)
                        && hit.distance < 0.95f)
                        return true;
                }
            }
            return false;
        }

        private float OverhangReachUp()
        {
            return profile != null ? Mathf.Clamp(profile.overhangReachUp, 0.45f, 1.8f) : 1.15f;
        }

        private float OverhangReachBack()
        {
            return profile != null ? Mathf.Clamp(profile.overhangReachBack, 0.45f, 2.2f) : 1.4f;
        }

        private float OverhangMinProbeOut()
        {
            return profile != null ? Mathf.Clamp(profile.overhangMinProbeOut, 0.02f, 0.25f) : 0.05f;
        }

        private float OverhangDeepProtrusion()
        {
            return profile != null ? Mathf.Clamp(profile.overhangDeepProtrusion, 0.25f, 1.6f) : 0.55f;
        }

        private float OverhangHangInset()
        {
            return profile != null ? Mathf.Clamp(profile.overhangHangInset, 0.05f, 0.4f) : 0.16f;
        }

        /// <summary>
        /// How far the lip sits out from the climb wall face (planar). Ignores vertical thickness.
        /// </summary>
        private float MeasureLipProtrusion(RaycastHit lip)
        {
            // free-climb-dune-v6: ONLY how far the lip sits out from the climb face.
            // Old Max(along, bodyToLip) made stub shelves look "deep" when the body was below the lip.
            Vector3 back = Flatten(_lastNormal);
            if (back.sqrMagnitude < 0.001f)
                back = Flatten(transform.forward);
            if (back.sqrMagnitude < 0.001f)
                return 0f;

            Vector3 wallRef = _hasLastStick ? _lastStickHit.point : transform.position;
            // Project lip onto face plane along wall normal, then measure outward.
            Vector3 n = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : back;
            Vector3 facePt = wallRef;
            if (_hasLastStick)
                facePt = _lastStickHit.point;
            Vector3 toLip = lip.point - facePt;
            float along = Vector3.Dot(toLip, back);
            // Also compare lip vs body along outward axis only (ignore vertical separation).
            float fromBodyOut = Vector3.Dot(lip.point - transform.position, back);
            float standOff = profile != null ? profile.standOff : 0.35f;
            fromBodyOut = Mathf.Max(0f, fromBodyOut - standOff * 0.25f);
            // Use the SMALLER credible outward depth so stubs stay stubs.
            float depth = along > 0.01f ? along : fromBodyOut;
            if (along > 0.01f && fromBodyOut > 0.01f)
                depth = Mathf.Min(along, fromBodyOut);
            return Mathf.Max(0f, depth);
        }

        private bool TryProbeSoffit(Vector3 wallNormal, out RaycastHit soffit)
        {
            soffit = default;
            Vector3 n = Flatten(wallNormal);
            if (n.sqrMagnitude < 0.001f)
                n = Flatten(transform.forward);
            if (n.sqrMagnitude < 0.001f)
                return false;

            float hh = profile != null ? profile.handHeight : 1.18f;
            float reachUp = OverhangReachUp();
            Vector3 feet = transform.position;
            Vector3 aim = feet + Vector3.up * (hh + 0.08f);
            float[] heights = { 0.55f, 0.75f, hh - 0.28f, hh - 0.1f, hh };
            float[] outs = { -0.04f, 0.02f, 0.06f, 0.12f, 0.16f, 0.28f };
            RaycastHit best = default;
            float bestDist = float.MaxValue;
            bool any = false;

            for (int h = 0; h < heights.Length; h++)
            {
                for (int o = 0; o < outs.Length; o++)
                {
                    Vector3 origin = feet + Vector3.up * heights[h] + n * outs[o];
                    if (!SurfaceCast(origin, Vector3.up, reachUp, 0f, out RaycastHit hit) || IsSelfHit(hit))
                        continue;
                    if (!IsSoffitNormal(hit.normal))
                        continue;
                    if (hit.point.y > feet.y + hh + reachUp + 0.12f)
                        continue;
                    if (hit.point.y < feet.y + 0.7f)
                        continue;
                    float dist = (hit.point - aim).sqrMagnitude;
                    if (!any || dist < bestDist)
                    {
                        best = hit;
                        bestDist = dist;
                        any = true;
                    }
                }
            }

            if (!any)
                return false;

            soffit = best;
#if UNITY_EDITOR
            _gizmoOverhangOrigin = feet + Vector3.up * hh + n * 0.1f;
#endif
            return true;
        }

        private bool FindOverhangLip(Vector3 wallNormal, out RaycastHit lip)
        {
            lip = default;
#if UNITY_EDITOR
            _gizmoOverhangValid = false;
#endif
            Vector3 back = Flatten(wallNormal);
            if (back.sqrMagnitude < 0.001f)
                back = Flatten(transform.forward);
            if (back.sqrMagnitude < 0.001f)
                return false;

            float hh = profile != null ? profile.handHeight : 1.18f;
            float reachBack = OverhangReachBack();
            float reachUp = OverhangReachUp();
            Vector3 hands = transform.position + Vector3.up * hh;
            RaycastHit best = default;
            float bestScore = float.MaxValue;
            bool any = false;

            if (TryProbeSoffit(wallNormal, out RaycastHit ceiling)
                && TryWalkSoffitToLip(ceiling, back, reachBack, out RaycastHit walked)
                && IsReachableOverhangLip(walked, hands, reachBack, reachUp))
            {
                best = walked;
                bestScore = (walked.point - hands).sqrMagnitude;
                any = true;
            }

            Vector3 right = WallRight(wallNormal);
            Vector3 feet = transform.position;
            float minOut = OverhangMinProbeOut();
            // Shallow outs first so thin/short lips are not skipped past.
            float[] outs =
            {
                minOut,
                minOut + 0.04f,
                0.12f,
                0.18f,
                0.4f,
                0.65f,
                0.9f,
                1.15f,
                1.4f,
                reachBack
            };
            float[] lats = { 0f, -0.2f, 0.2f, -0.35f, 0.35f };
            float[] ups = { 0.06f, 0.14f, 0.28f, 0.45f, 0.7f, 1.0f };
            for (int o = 0; o < outs.Length; o++)
            {
                if (outs[o] < minOut - 0.001f || outs[o] > reachBack + 0.08f)
                    continue;
                for (int l = 0; l < lats.Length; l++)
                {
                    for (int u = 0; u < ups.Length; u++)
                    {
                        if (ups[u] > reachUp + 0.08f)
                            continue;
                        Vector3 origin = feet + Vector3.up * (hh + ups[u]) + back * outs[o] + right * lats[l];
                        if (!SurfaceCast(origin, Vector3.down, ups[u] + 0.42f, 0.06f, out RaycastHit shelf)
                            || IsSelfHit(shelf))
                            continue;
                        if (Vector3.Angle(Vector3.up, shelf.normal) > 68f)
                            continue;
                        if (shelf.normal.y < -0.05f || LipIsUndersideCorner(shelf, wallNormal))
                            continue;
                        if (!IsReachableOverhangLip(shelf, hands, reachBack, reachUp))
                            continue;
                        // Prefer top-surface lips: penalize lower (mid-slab) hits so thick
                        // geometry does not win on raw distance-to-hands alone.
                        Vector3 toShelf = shelf.point - hands;
                        Vector3 planarShelf = new Vector3(toShelf.x, 0f, toShelf.z);
                        float score = planarShelf.sqrMagnitude
                            + Mathf.Max(0f, hands.y + 0.05f - shelf.point.y) * 6f
                            - Mathf.Max(0f, shelf.point.y - hands.y) * 0.35f;
                        if (!any || score < bestScore)
                        {
                            best = shelf;
                            bestScore = score;
                            any = true;
                        }
                    }
                }
            }

            // Thin flush/short lip fallback when outward shelf probes miss.
            if (!any && AtHandLip(wallNormal, out RaycastHit handLip)
                && IsReachableOverhangLip(handLip, hands, reachBack, reachUp))
            {
                best = handLip;
                any = true;
            }

            if (!any)
                return false;

            // Promote mid-slab / soffit-rim hits to the top outer edge of thick blocks.
            // Mid protruding blocks must NOT accept underside / bottom-corner lips.
            if (RefineLipToTopEdge(best, wallNormal, out RaycastHit topEdge))
                best = topEdge;
            if (!IsWalkableLipNormal(best.normal)
                || LipIsUndersideCorner(best, wallNormal)
                || LipContactIsThickSlabUnderside(best))
            {
                // Cast from above/out (AtHandLip thick band) for the true top outer lip.
                if (AtHandLip(wallNormal, out RaycastHit handTop)
                    && IsWalkableLipNormal(handTop.normal)
                    && !LipIsUndersideCorner(handTop, wallNormal)
                    && !LipContactIsThickSlabUnderside(handTop)
                    && IsReachableOverhangLip(handTop, hands, reachBack, reachUp))
                {
                    best = handTop;
                    if (RefineLipToTopEdge(best, wallNormal, out RaycastHit raisedHand)
                        && IsWalkableLipNormal(raisedHand.normal)
                        && !LipIsUndersideCorner(raisedHand, wallNormal)
                        && !LipContactIsThickSlabUnderside(raisedHand))
                        best = raisedHand;
                }
                else
                {
                    // lip8: probe only found underside — skip overhang, stay on vertical face.
                    return false;
                }
            }

            lip = best;
#if UNITY_EDITOR
            _gizmoOverhangLip = lip.point;
            _gizmoOverhangValid = true;
#endif
            return true;
        }

        private bool TryWalkSoffitToLip(RaycastHit ceiling, Vector3 back, float reachBack, out RaycastHit lip)
        {
            lip = default;
            const float step = 0.07f;
            int steps = Mathf.Max(6, Mathf.CeilToInt(reachBack / step));
            Vector3 lastUnder = ceiling.point;
            bool foundEdge = false;
            for (int i = 1; i <= steps; i++)
            {
                Vector3 o = ceiling.point + back * (step * i);
                o.y = ceiling.point.y - 0.22f;
                if (SurfaceCast(o, Vector3.up, 0.55f, 0f, out RaycastHit under)
                    && !IsSelfHit(under)
                    && IsSoffitNormal(under.normal))
                {
                    lastUnder = under.point;
                    continue;
                }

                foundEdge = true;
                break;
            }

            if (!foundEdge)
                return false;

            Vector3 edge = lastUnder + back * 0.1f;
            RaycastHit bestTop = default;
            float bestTopY = float.MinValue;
            bool haveTop = false;
            // Cast from well above the soffit so thick block tops win over mid-volume grazes.
            float[] upStarts = { 0.45f, 0.7f, 0.95f, 1.25f, 1.55f, 1.9f };
            for (int i = 0; i < upStarts.Length; i++)
            {
                Vector3 origin = edge + Vector3.up * upStarts[i];
                if (!SurfaceCast(origin, Vector3.down, upStarts[i] + 0.25f, 0.07f, out RaycastHit top)
                    || IsSelfHit(top))
                    continue;
                if (Vector3.Angle(Vector3.up, top.normal) > 68f)
                    continue;
                if (top.point.y < lastUnder.y - 0.04f)
                    continue;
                if (top.point.y > lastUnder.y + 2.05f)
                    continue;
                Vector3 planar = top.point - edge;
                planar.y = 0f;
                if (planar.sqrMagnitude > 0.22f)
                    continue;
                // Prefer the highest walkable top near the outer rim (top lip, not mid-slab).
                if (!haveTop || top.point.y > bestTopY)
                {
                    bestTop = top;
                    bestTopY = top.point.y;
                    haveTop = true;
                }
            }

            if (haveTop)
            {
                lip = bestTop;
                return true;
            }

            // Last resort: high down-cast only. Never invent a mid-thickness / underside rim
            // (that parked hangs under thick mid slabs with zero locomotion).
            if (SurfaceCast(edge + Vector3.up * 2.4f, Vector3.down, 2.8f, 0.08f, out RaycastHit highTop)
                && !IsSelfHit(highTop)
                && Vector3.Angle(Vector3.up, highTop.normal) <= 68f
                && highTop.point.y >= lastUnder.y + 0.12f)
            {
                lip = highTop;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Promote a soffit / mid-slab / ClosestPoint-style lip to the top outer edge
        /// of thick geometry. Protrusion hang/mantle must use that top lip.
        /// </summary>
        private bool RefineLipToTopEdge(RaycastHit cand, Vector3 wallNormal, out RaycastHit topLip)
        {
            topLip = cand;
            if (cand.collider == null)
                return false;

            Vector3 back = Flatten(wallNormal);
            if (back.sqrMagnitude < 0.001f)
                back = Flatten(transform.forward);
            if (back.sqrMagnitude < 0.001f)
                return false;

            Vector3 right = WallRight(wallNormal);
            RaycastHit best = cand;
            float bestY = cand.point.y;
            bool raised = false;
            float[] outs = { 0f, 0.05f, 0.1f, 0.16f, 0.24f, -0.05f };
            float[] lats = { 0f, -0.12f, 0.12f, -0.22f, 0.22f };
            float[] upStarts = { 0.5f, 0.85f, 1.2f, 1.55f, 1.9f, 2.25f };

            for (int o = 0; o < outs.Length; o++)
            {
                for (int l = 0; l < lats.Length; l++)
                {
                    for (int u = 0; u < upStarts.Length; u++)
                    {
                        Vector3 origin = cand.point + back * outs[o] + right * lats[l];
                        origin.y = cand.point.y + upStarts[u];
                        if (!SurfaceCast(origin, Vector3.down, upStarts[u] + 0.35f, 0.07f, out RaycastHit hit)
                            || IsSelfHit(hit))
                            continue;
                        if (Vector3.Angle(Vector3.up, hit.normal) > 60f)
                            continue;
                        if (hit.point.y < cand.point.y - 0.02f)
                            continue;
                        if (hit.point.y > cand.point.y + 2.15f)
                            continue;
                        Vector3 planar = hit.point - cand.point;
                        planar.y = 0f;
                        if (planar.sqrMagnitude > 0.35f)
                            continue;
                        if (hit.point.y > bestY + 0.01f)
                        {
                            best = hit;
                            bestY = hit.point.y;
                            raised = true;
                        }
                        else if (!raised && hit.point.y >= bestY - 0.01f)
                        {
                            // Same height: prefer the more outward rim point.
                            Vector3 bestPlanar = best.point - cand.point;
                            bestPlanar.y = 0f;
                            if (Vector3.Dot(planar, back) > Vector3.Dot(bestPlanar, back) + 0.01f)
                            {
                                best = hit;
                                bestY = hit.point.y;
                            }
                        }
                    }
                }
            }

            if (!raised && best.point.y <= cand.point.y + 0.02f)
            {
                // Candidate may already be the top; keep it if walkable-up.
                if (Vector3.Angle(Vector3.up, cand.normal) <= 60f)
                {
                    topLip = cand;
                    return true;
                }
                return false;
            }

            topLip = best;
            return true;
        }

        private bool IsReachableOverhangLip(RaycastHit cand, Vector3 hands, float reachBack, float reachUp)
        {
            if (cand.collider == null || IsSelfHit(cand))
                return false;
            // Underside / bottom-corner normals are not climb lips.
            if (IsSoffitNormal(cand.normal) || !IsWalkableLipNormal(cand.normal))
                return false;
            if (LipContactIsThickSlabUnderside(cand) || LipIsUndersideCorner(cand, _lastNormal))
                return false;
            Vector3 d = cand.point - hands;
            Vector3 planar = new Vector3(d.x, 0f, d.z);
            if (planar.magnitude > reachBack + 0.4f)
                return false;
            if (d.y < -0.35f || d.y > reachUp + 0.4f)
                return false;
            float hh = profile != null ? profile.handHeight : 1.18f;
            Vector3 hang = cand.point;
            hang.y = cand.point.y - hh;
            if (hang.y - transform.position.y > OverhangMaxLift)
                return false;
            if ((hang - transform.position).sqrMagnitude > OverhangMaxGrab * OverhangMaxGrab)
                return false;
            return true;
        }


        /// <summary>
        /// True when ClingSense lip is in hand-reach (top-edge mantle commit).
        /// </summary>
        private bool ClingLipImminent()
        {
            if (!_cling.valid || !_cling.hasLip || _cling.lipHit.collider == null)
                return false;
            float hh = profile != null ? profile.handHeight : 1.18f;
            float lipRelY = _cling.lipHit.point.y - transform.position.y;
            float lipPlanar = Vector3.ProjectOnPlane(_cling.lipHit.point - transform.position, Vector3.up).magnitude;
            return lipRelY > hh * 0.3f
                && lipRelY < hh + 0.55f
                && lipPlanar < 1.1f;
        }

        private bool HasWallAbove(Vector3 wallNormal)
        {
            Vector3 n = wallNormal.sqrMagnitude > 0.001f ? wallNormal.normalized : -transform.forward;
            Vector3 into = -n;
            float chest = profile != null ? profile.handHeight - 0.16f : 1.02f;
            Vector3 off = transform.position + Vector3.up * chest + n * 0.08f;
            float[] ups = { 0.32f, 0.5f, 0.72f };
            for (int i = 0; i < ups.Length; i++)
            {
                Vector3 o = off + Vector3.up * ups[i];
                if (!SurfaceCast(o, into, 0.7f, 0.1f, out RaycastHit hit) || IsSelfHit(hit))
                    continue;
                if (!IsClimbableHit(hit))
                    continue;
                if (Vector3.Angle(Vector3.up, hit.normal) > 50f)
                    return true;
            }
            return false;
        }

        private bool TryAutoMantle()
        {
            return TryMantle(_lastNormal, requireUp: false) || ForceMantleOverLip(_lastNormal);
        }

        private bool ForceMantleOverLip(Vector3 wallNormal)
        {
            // mantle-simple-v2: AtHandLip / known overhang lip unlocks; short attach gate only.
            bool handLip = AtHandLip(wallNormal, out _);
            if (!_overhangHang && !_overhangGrabbing && !_overhangPreferMantle && !handLip
                && Time.unscaledTime - _attachedAt < 0.35f)
                return false;
            bool knownLip = _overhangHang || (_overhangLip.collider != null) || handLip || _overhangPreferMantle;
            if (HasWallAbove(wallNormal) && !knownLip)
                return false;
            RaycastHit top;
            bool haveTop = AtHandLip(wallNormal, out top);
            if (knownLip && _overhangLip.collider != null)
            {
                RaycastHit known = _overhangLip;
                if (RefineLipToTopEdge(known, wallNormal, out RaycastHit refinedKnown))
                    known = refinedKnown;
                // Thick mid: known overhang top lip wins over a lower mid-slab AtHandLip hit.
                if (!haveTop || known.point.y > top.point.y + 0.06f)
                {
                    top = known;
                    haveTop = true;
                }
            }
            if (!haveTop)
                return false;

            Vector3 forward = -Flatten(wallNormal);
            if (forward.sqrMagnitude < 0.001f)
                forward = Flatten(transform.forward);
            float pad = MantlePlantPad;
            float fwd = profile != null ? profile.mantleForward : 0.18f;
            // mantle-simple-v2c: profile had a negative mantleForward — always plant onto the deck.
            if (fwd < 0.05f) fwd = 0.18f;
            fwd = Mathf.Clamp(fwd, 0.12f, 0.28f);
            // cling-sense-v1e: plant just onto the lip — never force 0.28m+ past the rim.
            float past = Mathf.Clamp(fwd, 0.1f, 0.16f);
            Vector3 stand = top.point + forward * past;
            stand.y = top.point.y + Mathf.Clamp(pad > 0.001f ? pad : 0.03f, 0.015f, 0.04f);
            if (TryClearStand(top, forward, out Vector3 clear))
                stand = ClampStandPastLip(clear, top.point, forward, 0.14f);
            else
                stand = ClampStandPastLip(stand, top.point, forward, 0.14f);
            BeginMantle(stand);
            return true;
        }

        private bool TryMantle(Vector3 wallNormal, bool requireUp = true)
        {
            if (profile != null && !profile.enableMantle)
                return false;
            bool handLip = AtHandLip(wallNormal, out _);
            if (!_overhangHang && !_overhangGrabbing && !_overhangPreferMantle && !handLip
                && Time.unscaledTime - _attachedAt < 0.35f)
                return false;

            if (requireUp)
            {
                Vector2 raw = ReadClimbAxes();
                if (raw.y <= 0f)
                    return false;
            }
            if (HasWallAbove(wallNormal) && !_overhangHang && !handLip && !_overhangPreferMantle)
                return false;

            Vector3 forward = -Flatten(wallNormal);
            float height = _capsule != null ? _capsule.height : 1.84f;
            Vector3 feet = transform.position;
            Vector3 head = feet + Vector3.up * (height * 0.95f);

            if (!FindWalkableLedge(head, forward, feet.y - 0.35f, out RaycastHit top))
                return false;

            Vector3 stand = _mantleStand.sqrMagnitude > 0.001f
                ? _mantleStand
                : top.point + Vector3.up * 0.06f + forward * 0.18f;
            BeginMantle(stand);
            return true;
        }

        private static readonly int ClimbMantleState = Animator.StringToHash("ClimbMantle");
        private static readonly int ClimbStandupState = Animator.StringToHash("ClimbStandup");

        private void BeginMantle(Vector3 stand)
        {
            Vector3 onto = Flatten(-_lastNormal);
            Vector3 start = transform.position;
            if (onto.sqrMagnitude < 0.001f)
                onto = Flatten(transform.forward);

            // Probe from hanging chest, out over the lip, down onto the top. Never search 2m above.
            float fwd = profile != null ? profile.mantleForward : 0.18f;
            if (fwd < 0.05f) fwd = 0.18f;
            fwd = Mathf.Clamp(fwd, 0.12f, 0.28f);
            float up = profile != null ? profile.mantleProbeUp : 1.5f;
            float down = profile != null ? profile.mantleProbeDown : 1.7f;
            float pad = MantlePlantPad;
            // cling-sense-v1e: short probe — long findFwd was landing deep past the lip.
            float findFwd = Mathf.Clamp(Mathf.Max(0.14f, fwd), 0.14f, 0.32f);
            Vector3 probe = start + onto * findFwd + Vector3.up * up;
            RaycastHit floor = default;
            bool haveFloor = SurfaceCast(probe, Vector3.down, down, 0.12f, out floor)
                && Vector3.Angle(Vector3.up, floor.normal) <= 50f
                && IsSaneMove(floor.point)
                && floor.point.y <= start.y + 1.7f;
            if (!haveFloor && AtHandLip(_lastNormal, out floor))
                haveFloor = IsSaneMove(floor.point);
            // mantle-simple-v2: plant from AtHandLip / overhang rim only.
            RaycastHit rimHit = default;
            bool haveRim = false;
            if (_overhangLip.collider != null)
            {
                rimHit = _overhangLip;
                haveRim = true;
                if (RefineLipToTopEdge(_overhangLip, _lastNormal, out RaycastHit refinedRim)
                    && IsWalkableLipNormal(refinedRim.normal))
                    rimHit = refinedRim;
            }
            else if (AtHandLip(_lastNormal, out rimHit))
            {
                haveRim = true;
            }

            Vector3 lipRef = haveRim ? rimHit.point : (start + onto * 0.2f);
            float rimY = lipRef.y;
            // v2d: plant on the deck — 0.1m floor pad caused ~0.2m float-down after mantle.
            float footPad = Mathf.Clamp(pad > 0.001f ? pad : 0.03f, 0.015f, 0.04f);
            // v2e: less onto the deck — was coasting 0.1–0.2m after land.
            float plantPast = Mathf.Clamp(Mathf.Max(0.04f, fwd * 0.35f), 0.04f, 0.08f); // v2i: stay on lip, no post-land slide

            if (haveRim)
            {
                stand = lipRef + onto * plantPast;
                stand.y = rimY + footPad;
                if (SurfaceCast(stand + Vector3.up * 0.5f, Vector3.down, 0.85f, 0.1f, out RaycastHit deck)
                    && !IsSelfHit(deck)
                    && Vector3.Angle(Vector3.up, deck.normal) <= 50f)
                {
                    stand = deck.point + onto * 0.02f;
                    stand.y = deck.point.y + footPad;
                }
                stand = ClampStandPastLip(stand, lipRef, onto, 0.10f);
            }
            else if (haveFloor)
            {
                stand = floor.point;
                stand.y = floor.point.y + footPad;
                stand = ClampStandPastLip(stand, lipRef, onto, 0.10f);
            }

            Vector3 outN = Flatten(_lastNormal);
            if (outN.sqrMagnitude < 0.001f)
                outN = -onto;

            // Compact 3-phase waypoints: rise just above rim, slide over, plant.
            // Clear the rim, but keep over close to final stand Y so we don't settle 0.2m.
            Vector3 rise = start + outN * 0.08f;
            rise.y = Mathf.Max(rimY + 0.12f, start.y + 0.15f);
            Vector3 over = lipRef + onto * Mathf.Min(plantPast, 0.06f);
            over.y = Mathf.Lerp(rise.y, stand.y + 0.02f, 0.7f);

            _mantleStand = stand;
            _mantleFloorY = stand.y - footPad;
            _mantleStart = start;
            _mantleLip = rise;
            _mantleOver = over;
            _mantleStartRot = transform.rotation;
            _mantleEndRot = UprightFrom(onto);
            _mantling = true;
            _mantleSettling = false;
            ClearOverhangState();
            // After mantle starts (or aborts mid-volume), do not instantly re-lock the same lip.
            _overhangResumeAt = Mathf.Max(_overhangResumeAt, Time.unscaledTime + 0.55f);
            _mantleBeganAt = Time.unscaledTime;
            _mantleUntil = Time.unscaledTime + 3.2f;
            if (landing != null)
                landing.IgnoreLandsFor(profile != null ? profile.mantleIgnoreLands : 2.6f);

            // IsClimbing must be false: AnyState "IsClimbing -> ClimbBlend" would steal
            // the layer back and freeze the first frame of UpOver.
            SetClimbLayerWeight(1f);
            WriteAnimator(Vector2.zero, 0f, climbing: false);
            if (animator != null)
            {
                animator.speed = Mathf.Max(1f, animator.speed);
                if (_hasMantle)
                    animator.ResetTrigger(MantleHash);
                if (_climbLayerIndex >= 0)
                    animator.Play("ClimbMantle", _climbLayerIndex, 0f);
            }
            UpdateHandGrabTargets(_lastNormal);
        }

        private void TickMantle()
        {
            if (animator != null)
                animator.applyRootMotion = false;

            if (ReadClimbAxes().y < -0.2f)
            {
                _mantling = false;
                _overhangResumeAt = Mathf.Max(_overhangResumeAt, Time.unscaledTime + 0.45f);
                WriteAnimator(Vector2.zero, 0f, climbing: true);
                return;
            }

            WriteAnimator(Vector2.zero, 0f, climbing: false);
            if (!_mantleSettling)
                SetClimbLayerWeight(1f);
            UpdateHandGrabTargets(_lastNormal);

            float footPad = Mathf.Clamp(MantlePlantPad > 0.001f ? MantlePlantPad : 0.03f, 0.015f, 0.04f);
            _mantleStand.y = _mantleFloorY + footPad;
            if (_mantleOver.sqrMagnitude < 0.001f)
                _mantleOver = Vector3.Lerp(_mantleLip, _mantleStand, 0.55f);

            float dur = profile != null ? Mathf.Max(1.35f, profile.mantleSeconds) : 1.5f;
            float clockT = Mathf.Clamp01((Time.unscaledTime - _mantleBeganAt) / dur);

            // mantle-simple-v2b: follow ClimbMantle → ClimbStandup when present so root doesn't finish before the clip.
            float animT = -1f;
            bool inStandup = false;
            if (animator != null && _climbLayerIndex >= 0)
            {
                AnimatorStateInfo cur = animator.GetCurrentAnimatorStateInfo(_climbLayerIndex);
                AnimatorStateInfo nxt = animator.IsInTransition(_climbLayerIndex)
                    ? animator.GetNextAnimatorStateInfo(_climbLayerIndex)
                    : cur;
                if (cur.shortNameHash == ClimbMantleState || nxt.shortNameHash == ClimbMantleState)
                {
                    float nt = cur.shortNameHash == ClimbMantleState ? cur.normalizedTime : nxt.normalizedTime;
                    animT = Mathf.Clamp01(nt) * 0.62f;
                }
                if (cur.shortNameHash == ClimbStandupState || nxt.shortNameHash == ClimbStandupState)
                {
                    inStandup = true;
                    float nt = cur.shortNameHash == ClimbStandupState ? cur.normalizedTime : nxt.normalizedTime;
                    animT = Mathf.Clamp01(0.62f + Mathf.Clamp01(nt) * 0.38f);
                }
            }

            float t = animT >= 0f ? Mathf.Lerp(clockT, animT, 0.65f) : clockT;
            t = Mathf.Clamp01(t);

            // Two smooth phases only: rise/over, then settle onto the deck (no late Y drop snap).
            Vector3 pos;
            if (t < 0.55f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.55f);
                pos = Vector3.Lerp(_mantleStart, _mantleOver, u);
            }
            else
            {
                float u = Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f);
                pos = Vector3.Lerp(_mantleOver, _mantleStand, u);
            }
            if (pos.y < _mantleStart.y)
                pos.y = _mantleStart.y;
            MoveBody(pos);

            float rotT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.75f));
            ApplyRotation(Quaternion.Slerp(_mantleStartRot, _mantleEndRot, rotT));

            // v2h: cut as soon as body is on the deck — don't wait for ClimbStandup (that was the forward step).
            Vector3 nearStand = _mantleStand;
            float toStand = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(nearStand.x, 0f, nearStand.z));
            // Cut the instant ClimbStandup starts — that clip is the forward step.
            bool onDeck = inStandup || t >= 0.68f || (t >= 0.55f && toStand < 0.12f);
            bool clockDone = clockT >= 0.68f;
            if (!onDeck && !clockDone)
                return;

            // v2i: freeze where we already are — video showed forced slide to a far stand target.
            Vector3 plant = transform.position;
            if (SurfaceCast(plant + Vector3.up * 0.65f, Vector3.down, 1.0f, 0.1f, out RaycastHit landHit)
                && !IsSelfHit(landHit)
                && Vector3.Angle(Vector3.up, landHit.normal) <= 50f)
            {
                plant.y = landHit.point.y + footPad;
            }
            else
            {
                plant.y = _mantleFloorY + footPad;
            }

            if (!_mantleSettling)
            {
                _mantleSettling = true;
                _mantleSettleAt = Time.unscaledTime;
                _mantleStand = plant;
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                    // Weight 0 kills ClimbStandup contribution (no Empty state required).
                    SetClimbLayerWeight(0f);
                    WriteAnimator(Vector2.zero, 0f, climbing: false);
                }
                if (motor != null)
                {
                    motor.lockMovement = true;
                    motor.lockAnimMovement = true;
                    motor.verticalVelocity = 0f;
                }
            }

            plant = _mantleStand;
            if (IsSaneMove(plant))
                MoveBody(plant);
            SafeZeroVelocity();
            SetPlanarVelocity(Vector3.zero);
            _dampedClimbInput = Vector3.zero;
            _dampedClimbVel = Vector3.zero;
            if (animator != null)
                animator.applyRootMotion = false;

            float settleAge = Time.unscaledTime - _mantleSettleAt;
            Vector2 move = ReadClimbAxes();
            bool wantsMove = move.sqrMagnitude > 0.04f || ReadShiftHeld();

            // v2j: always hold briefly so locomotion can't start mid-step; WASD only after.
            if (settleAge < 0.22f)
                return;
            if (!wantsMove && settleAge < 0.32f)
                return;

            _mantleSettling = false;
            _mantling = false;
            _suppressRootMotionUntil = Time.unscaledTime + 0.7f;
            _suppressAnimMoveUntil = Time.unscaledTime + 0.45f;
            Detach(addPlatformVelocity: false);
            if (animator != null)
                animator.applyRootMotion = false;
            if (motor != null)
            {
                // Allow WASD via motor later, but block anim/root step until suppress ends.
                motor.lockMovement = false;
                motor.lockAnimMovement = true;
            }
            SafeZeroVelocity();
            SetPlanarVelocity(Vector3.zero);
        }

        private bool TryExitOntoGround()
        {
            return TryExitOntoGroundInternal(maxDown: 0.7f, requireLowProbe: false);
        }

        /// <summary>
        /// Auto plant when feet are close to walkable ground while probe-climbing (no E / no S required).
        /// </summary>
        private bool TryNearGroundStepDown()
        {
            if (Time.unscaledTime - _attachedAt < 0.2f)
                return false;
            // cling-sense-v1: bubble ground wins when clearly walkable under feet.
            if (_cling.valid && _cling.hasWalkableBelow && _cling.groundDist <= 0.92f)
            {
                if (_hasMantle && animator != null)
                    animator.SetTrigger(MantleHash);
                MoveBody(_cling.groundHit.point + Vector3.up * MantlePlantPad);
                Detach(addPlatformVelocity: false);
                Debug.Log($"[{BuildStamp}] bubble auto-drop dist={_cling.groundDist:F2}");
                return true;
            }
            if (TryExitOntoGroundInternal(maxDown: 1.55f, requireLowProbe: true))
                return true;
            return TryAutoDropOntoFlatSurface();
        }

        /// <summary>
        /// Drop off climb when a near-flat walkable surface is under/beside the feet (terrain or mesh).
        /// </summary>
        private bool TryAutoDropOntoFlatSurface()
        {
            if (Time.unscaledTime - _attachedAt < 0.2f)
                return false;

            float walkMax = profile != null ? profile.walkMaxSlopeDeg : 45f;
            // Prefer the profile walk limit; never treat steep climb faces as "flat".
            walkMax = Mathf.Clamp(walkMax, 25f, 50f);

            Vector3 feet = transform.position + Vector3.up * 0.08f;
            Vector3[] origins =
            {
                feet,
                feet + Flatten(transform.forward) * 0.35f,
                feet - Flatten(_lastNormal) * 0.25f, // out from wall onto ledge
                feet + Flatten(_lastNormal) * 0.2f,
                feet + transform.right * 0.3f,
                feet - transform.right * 0.3f,
            };

            RaycastHit best = default;
            bool any = false;
            for (int i = 0; i < origins.Length; i++)
            {
                if (!Physics.Raycast(origins[i], Vector3.down, out RaycastHit hit, 1.05f, ~0, QueryTriggerInteraction.Ignore))
                    continue;
                if (hit.collider != null &&
                    (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)))
                    continue;
                if (Vector3.Angle(Vector3.up, hit.normal) > walkMax)
                    continue;
                // Must be close — mid-face air gap should not detach.
                if (hit.distance > 0.92f)
                    continue;
                if (!any || hit.distance < best.distance)
                    best = hit;
                any = true;
            }

            if (!any)
                return false;

            if (_hasMantle && animator != null)
                animator.SetTrigger(MantleHash);

            MoveBody(best.point + Vector3.up * MantlePlantPad);
            Detach(addPlatformVelocity: false);
            Debug.Log($"[{BuildStamp}] auto-drop flat dist={best.distance:F2} nY={best.normal.y:F2}");
            return true;
        }

        private bool TryExitOntoGroundInternal(float maxDown, bool requireLowProbe)
        {
            if (Time.unscaledTime - _attachedAt < 0.12f)
                return false;

            Vector3 feet = transform.position + Vector3.up * 0.12f;

            if (requireLowProbe)
            {
                // Require a short ground hit under the feet so mid-face S-seek is unchanged.
                if (!Physics.Raycast(feet, Vector3.down, out RaycastHit low, 1.15f, ~0, QueryTriggerInteraction.Ignore))
                    return false;
                if (low.collider != null &&
                    (low.collider.transform == transform || low.collider.transform.IsChildOf(transform)))
                    return false;
                if (Vector3.Angle(Vector3.up, low.normal) > (profile != null ? profile.walkMaxSlopeDeg : 75f))
                    return false;
                // free-climb-dune-v3: plant when truly close to ground/terrain.
                if (low.distance > 0.95f)
                    return false;
            }

            Vector3[] origins =
            {
                feet,
                feet + Flatten(transform.forward) * 0.28f,
                feet - Flatten(transform.forward) * 0.22f,
                feet + transform.right * 0.22f,
                feet - transform.right * 0.22f,
                feet + Flatten(_lastNormal) * 0.3f,
            };

            float walkMax = profile != null ? profile.walkMaxSlopeDeg : 75f;
            RaycastHit best = default;
            bool any = false;
            float cast = Mathf.Max(0.35f, maxDown);
            for (int i = 0; i < origins.Length; i++)
            {
                if (!Physics.Raycast(origins[i], Vector3.down, out RaycastHit hit, cast, ~0, QueryTriggerInteraction.Ignore))
                    continue;
                if (hit.collider != null &&
                    (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)))
                    continue;
                if (Vector3.Angle(Vector3.up, hit.normal) > walkMax)
                    continue;
                if (!any || hit.distance < best.distance)
                    best = hit;
                any = true;
            }

            if (!any)
                return false;

            if (_hasMantle && animator != null)
                animator.SetTrigger(MantleHash);

            MoveBody(best.point + Vector3.up * MantlePlantPad);
            Detach(addPlatformVelocity: false);
            return true;
        }

        private bool FindWalkableLedge(Vector3 head, Vector3 forward, float minY, out RaycastHit best)
        {
            best = default;
            float walkMax = 50f;
            Vector3 up = Vector3.up;
            Vector3 normal = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : -forward;
            Vector3 intoWall = -normal;
            if (intoWall.sqrMagnitude < 0.001f)
                intoWall = forward;
            Vector3 overFwd = Flatten(-normal);
            if (overFwd.sqrMagnitude < 0.001f)
                overFwd = forward;

            // Stage 2 — walk the face up to the lip (sequential, stop on first miss).
            Vector3 lastFacePoint = _hasLastStick ? _lastStickHit.point : head;
            bool seeded = _hasLastStick;
            float[] starts = { 0.22f, 0.42f, 0.65f, 0.9f };
            Vector3 origin = transform.position + up * starts[0] + normal * 0.08f;
            for (int s = 0; s < starts.Length; s++)
            {
                Vector3 tryO = transform.position + up * starts[s] + normal * 0.08f;
                if (SurfaceCast(tryO, intoWall, 0.7f, 0.11f, out RaycastHit seed) && !IsSelfHit(seed) && IsClimbableHit(seed))
                {
                    origin = tryO;
                    lastFacePoint = seed.point;
                    seeded = true;
                    break;
                }
            }
            if (!seeded && _hasLastStick)
                origin = _lastStickHit.point + normal * 0.1f;
            const int steps = 12;
            const float step = 0.16f;
            bool hadFace = seeded;
            float faceRadius = 0.11f;
            float faceRange = 0.7f;

            for (int i = 0; i < steps; i++)
            {
                Vector3 o = origin + up * (step * i);
                if (!SurfaceCast(o, intoWall, faceRange, faceRadius, out RaycastHit face) || IsSelfHit(face))
                {
                    break;
                }

                float angle = Vector3.Angle(up, face.normal);
                if (IsClimbableHit(face) && angle <= walkMax)
                {
                    if (AcceptLedge(face, minY, walkMax) && TryClearStand(face, overFwd, out Vector3 stand))
                    {
                        best = face;
                        _mantleStand = stand;
                        return true;
                    }
                    // Lip edge walkable but stand blocked (wall continues) - still try over-top probes.
                    lastFacePoint = face.point;
                    hadFace = true;
                    break;
                }

                if (IsClimbableHit(face))
                {
                    lastFacePoint = face.point;
                    hadFace = true;
                    continue;
                }

                break;
            }

            // Stage 3 — over the lip and down. Main 0.45m, then further 0.7m, then closer 0.25m.
            Vector3 lipRef = hadFace ? lastFacePoint : head;
            float[] overs = { 0.35f, 0.5f, 0.7f, 0.95f, 0.2f };
            float downRadius = 0.1f;
            float downRange = 0.75f;
            for (int i = 0; i < overs.Length; i++)
            {
                Vector3 over = lipRef + up * 0.16f + overFwd * overs[i];
                if (!SurfaceCast(over, Vector3.down, downRange, downRadius, out RaycastHit top))
                    continue;
                if (!AcceptLedge(top, minY, walkMax))
                    continue;
                if (!TryClearStand(top, overFwd, out Vector3 stand))
                    continue;
                best = top;
                _mantleStand = stand;
                return true;
            }

            // Stage 5 — one upward-angled spherecast only if stages 2–3 found nothing.
            Vector3 chest = transform.position + up * 1.1f;
            Vector3 dir = (overFwd + up * 1.1f).normalized;
            if (SurfaceCast(chest, dir, 2.2f, 0.12f, out RaycastHit angled) && !IsSelfHit(angled))
            {
                if (AcceptLedge(angled, minY, walkMax) && TryClearStand(angled, overFwd, out Vector3 standA))
                {
                    best = angled;
                    _mantleStand = standA;
                    return true;
                }

                if (IsClimbableHit(angled) && Vector3.Angle(up, angled.normal) > walkMax)
                {
                    Vector3 over = angled.point + up * 0.25f + overFwd * 0.4f;
                    if (SurfaceCast(over, Vector3.down, downRange, downRadius, out RaycastHit top) &&
                        AcceptLedge(top, minY, walkMax) &&
                        TryClearStand(top, overFwd, out Vector3 standB))
                    {
                        best = top;
                        _mantleStand = standB;
                        return true;
                    }
                }
            }
            else
            {
                Vector3 over = chest + dir * 1.5f;
                if (SurfaceCast(over, Vector3.down, downRange, downRadius, out RaycastHit top) &&
                    AcceptLedge(top, minY, walkMax) &&
                    TryClearStand(top, overFwd, out Vector3 standC))
                {
                    best = top;
                    _mantleStand = standC;
                    return true;
                }
            }

            return false;
        }

        private bool AcceptLedge(RaycastHit hit, float minY, float walkMax)
        {
            if (hit.collider == null)
                return false;
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                return false;
            if (Vector3.Angle(Vector3.up, hit.normal) > walkMax)
                return false;
            return hit.point.y >= minY;
        }

        private void EnsureClimbableIsGround()
        {
            if (motor == null)
                return;
            int climbLayer = LayerMask.NameToLayer(profile != null ? profile.climbableLayerName : "Climbable");
            if (climbLayer < 0)
                return;
            int bit = 1 << climbLayer;
            motor.groundLayer |= bit;
            motor.stepOffsetLayer |= bit;
        }

        private bool WantsDropToHang()
        {
            // S held toward the drop while standing on top (facing out).
            Vector2 axes = ReadClimbAxes();
            return axes.y < -0.25f;
        }

        private bool WantsDropToHangInteract()
        {
            // E while grounded near lip - same drop-grab as climb E-drop, standing initiate.
            return ReadInteractPressedThisFrame() && !UiBlocksClimbDrop();
        }

        private bool TryStartDropToHangFromTop()
        {
            if (_climbing || _hopping || _mantling || _reverseMantling)
                return false;
            if (profile == null || !profile.dropToHang)
                return false;
            if (motor != null && !motor.isGrounded)
                return false;
            if (!WantsDropToHang() && !WantsDropToHangInteract())
                return false;
            if (!TryDropToHang(out RaycastHit face, out RaycastHit lip))
                return false;
            return BeginReverseMantle(face, lip);
        }

        private bool TryDropToHang(out RaycastHit face, out RaycastHit lipHit)
        {
            face = default;
            lipHit = default;
            if (profile == null || !profile.dropToHang)
                return false;
            if (motor != null && !motor.isGrounded)
                return false;

            ResolveMask();

            float hangRange = profile.dropToHangRange;
            float radius = profile != null ? profile.probeRadius : 0.18f;
            Vector3 feet = transform.position + Vector3.up * 0.12f;
            Vector3 forward = Flatten(transform.forward);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            // Require a void ahead (toward facing / drop) - do not auto-trigger on every near-edge walk.
            Vector3 ahead = feet + forward * 0.42f;
            bool groundAhead = Physics.Raycast(ahead, Vector3.down, 0.7f, ~0, QueryTriggerInteraction.Ignore);
            if (groundAhead)
                return false;

            // Top lip under / just behind the toes.
            bool haveLip = false;
            float[] lipBacks = { 0.05f, 0.12f, 0.22f, 0.32f };
            for (int i = 0; i < lipBacks.Length; i++)
            {
                Vector3 o = feet - forward * lipBacks[i] + Vector3.up * 0.35f;
                if (!SurfaceCast(o, Vector3.down, 0.7f, 0.06f, out RaycastHit top) || IsSelfHit(top))
                    continue;
                if (!IsWalkableLipNormal(top.normal))
                    continue;
                lipHit = top;
                haveLip = true;
                break;
            }
            if (!haveLip)
            {
                // Fallback: lip at the drop edge itself.
                Vector3 o = ahead + Vector3.up * 0.2f;
                if (SurfaceCast(o, Vector3.down, 0.9f, 0.06f, out RaycastHit edgeTop)
                    && !IsSelfHit(edgeTop)
                    && IsWalkableLipNormal(edgeTop.normal))
                {
                    lipHit = edgeTop;
                    haveLip = true;
                }
            }
            if (!haveLip)
                return false;

            Vector3 below = ahead + Vector3.down * 0.25f;
            Vector3[] dirs =
            {
                -forward,
                (-forward + Vector3.down * 0.35f).normalized,
                Vector3.down,
            };

            bool any = false;
            for (int i = 0; i < dirs.Length; i++)
            {
                if (!SurfaceCast(below, dirs[i], hangRange, radius, out RaycastHit hit))
                    continue;
                if (!IsClimbableHit(hit) || !IsClimbableSlope(hit.normal))
                    continue;
                if (!any || hit.distance < face.distance)
                    face = hit;
                any = true;
            }

            return any;
        }

        /// <summary>
        /// Reverse mantle: from ledge top, lerp over the outer lip into hang while yawing 180
        /// so the player ends facing the climb face (not hanging facing away).
        /// </summary>
        private bool BeginReverseMantle(RaycastHit face, RaycastHit lip)
        {
            if (_reverseMantling || _climbing || _mantling)
                return false;

            Vector3 wallN = face.normal.sqrMagnitude > 0.001f ? face.normal.normalized : -Flatten(transform.forward);
            if (wallN.sqrMagnitude < 0.001f)
                return false;

            // Capture top pose BEFORE Attach (Attach snaps to the face).
            Vector3 start = transform.position;
            Quaternion startRot = transform.rotation;

            // Enter climb kinematic lock via Attach, then restore top pose for reverse lerp.
            Attach(face);
            MoveBody(start);
            ApplyRotation(startRot);
            _lastNormal = wallN;
            _overhangLip = lip;
            if (RefineLipToTopEdge(lip, wallN, out RaycastHit topLip) && IsWalkableLipNormal(topLip.normal))
                _overhangLip = topLip;
            _lastStickHit = _overhangLip;
            _hasLastStick = true;

            Vector3 hang = OverhangHangPos(_overhangLip);
            Vector3 mid = _overhangLip.point + Flatten(wallN) * 0.06f;
            mid.y = Mathf.Max(_overhangLip.point.y + 0.05f, start.y);

            _reverseMantleStart = start;
            _reverseMantleLip = mid;
            _reverseMantleHang = hang;
            _reverseMantleStartRot = startRot;
            // 180 toward the wall: AlignToWall looks into -normal (face the climb face).
            _reverseMantleEndRot = AlignToWall(wallN);
            _reverseMantleFace = face;
            _reverseMantleDur = profile != null
                ? Mathf.Clamp(profile.overhangGrabSeconds * 0.85f, 0.35f, 0.95f)
                : 0.55f;
            _reverseMantleAt = Time.unscaledTime;
            _reverseMantling = true;
            _overhangHang = false;
            _overhangGrabbing = false;
            _climbing = true;
            if (landing != null)
                landing.IgnoreLandsFor(profile != null ? profile.mantleIgnoreLands : 2.6f);
            WriteAnimator(new Vector2(0f, -1f), profile != null ? profile.moveSpeed : 0.85f, climbing: true);
            return true;
        }

        private void TickReverseMantle()
        {
            if (!_reverseMantling)
                return;

            float dur = Mathf.Max(0.2f, _reverseMantleDur);
            float t = Mathf.Clamp01((Time.unscaledTime - _reverseMantleAt) / dur);
            Vector3 pos;
            if (t < 0.45f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                pos = Vector3.Lerp(_reverseMantleStart, _reverseMantleLip, u);
            }
            else
            {
                float u = Mathf.SmoothStep(0f, 1f, (t - 0.45f) / 0.55f);
                pos = Vector3.Lerp(_reverseMantleLip, _reverseMantleHang, u);
            }
            MoveBody(pos);
            // Full yaw to face the wall during the drop (not hang facing away).
            ApplyRotation(Quaternion.Slerp(_reverseMantleStartRot, _reverseMantleEndRot, Mathf.SmoothStep(0f, 1f, t)));
            WriteAnimator(new Vector2(0f, -1f), profile != null ? profile.moveSpeed : 0.85f, climbing: true);

            if (t < 1f)
                return;

            _reverseMantling = false;
            MoveBody(_reverseMantleHang);
            ApplyRotation(_reverseMantleEndRot);
            _lastNormal = _reverseMantleFace.normal.sqrMagnitude > 0.001f
                ? _reverseMantleFace.normal.normalized
                : _lastNormal;
            _overhangHang = true;
            _lipHang = true;
            _overhangPreferMantle = false;
            SetOverhangHandIk(1f);
            FaceWall(_lastNormal);
        }



        /// <summary>
        /// After walk-off / freefall, only Space+W or boost-into-wall may attach — not proximity alone.
        /// </summary>
        private bool WantsIntentionalAirAttach()
        {
            if (Time.unscaledTime >= _walkOffSuppressUntil)
                return true;
            if (ReadJumpPressedThisFrame() || ReadJumpHeld())
            {
                Vector2 axes = ReadClimbAxes();
                if (axes.y > 0.18f)
                    return true;
            }
            if (jetpack != null && jetpack.IsBoostingNow)
            {
                Vector2 axes = ReadClimbAxes();
                if (axes.y > 0.18f)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Jetpack into a Climbable: grab when boosting toward the wall at close range.
        /// Not a sideways hover magnet — needs forward approach / facing into the face.
        /// </summary>

        /// <summary>
        /// Walk + one Space into a climbable: stick on that jump (no second Space, no auto cling-hop).
        /// </summary>
        private bool TryAirJumpWallGrab()
        {
            if (_climbing || _hopping || _mantling || _reverseMantling)
                return false;
            if (Time.unscaledTime > _airJumpGrabUntil)
                return false;
            if (motor != null && motor.isGrounded)
                return false;
            if (!CanAttachNow())
                return false;

            Vector2 axes = ReadClimbAxes();
            if (profile != null && profile.startClimbNeedsForward && axes.y <= 0.08f)
                return false;

            if (!TryJumpToWall(out RaycastHit hit))
                return false;

            float maxDist = profile != null ? profile.attachRange + 0.25f : 1.65f;
            if (hit.distance > maxDist)
                return false;

            Vector3 into = -Flatten(hit.normal);
            if (into.sqrMagnitude < 0.001f)
                return false;
            Vector3 wish = WishJumpDir();
            float approach = Vector3.Dot(wish, into);
            float face = Vector3.Dot(Flatten(transform.forward), into);
            if (approach < 0.2f || face < 0.05f)
                return false;

            if (!HasClimbStartStamina())
                return false;
            if (!TryPayClimbStartStamina())
                return false;

            Attach(hit);
            return true;
        }

        private bool TryJetpackWallGrab()
        {
            if (_climbing || _hopping || _mantling)
                return false;
            if (jetpack == null || !jetpack.IsBoostingNow)
                return false;
            if (!CanAttachNow())
                return false;
            if (motor != null && motor.isGrounded)
                return false;
            if (!WantsIntentionalAirAttach())
                return false;

            Vector2 axes = ReadClimbAxes();
            // Intentional only: must drive toward the wall (W). Idle / freefall hover = no magnet.
            if (axes.y < 0.18f)
                return false;

            if (!TryJumpToWall(out RaycastHit hit))
                return false;

            float maxDist = profile != null ? profile.attachRange + 0.35f : 1.75f;
            if (hit.distance > maxDist)
                return false;

            Vector3 into = -Flatten(hit.normal);
            if (into.sqrMagnitude < 0.001f)
                return false;
            Vector3 wish = WishJumpDir();
            float approach = Vector3.Dot(wish, into);
            float face = Vector3.Dot(Flatten(transform.forward), into);
            // Require clear drive into the Climbable - boost beside a wall does not stick.
            // free-climb-dune-v2: looser latch so Space/W + near wall sticks like Dune.
            if (approach < 0.28f || face < 0.12f)
                return false;

            // Velocity toward the wall (not mere proximity after walk-off / freefall).
            Vector3 vel = (body != null && !body.isKinematic) ? body.linearVelocity : Vector3.zero;
            float toward = Vector3.Dot(new Vector3(vel.x, 0f, vel.z), into);
            // Strong approach (W into wall) can attach at low speed; otherwise need closing speed.
            if (toward < 0.35f && approach < 0.45f)
                return false;

            if (!HasClimbStartStamina())
                return false;
            if (!TryPayClimbStartStamina())
                return false;
            Attach(hit);
            return true;
        }

        private bool TryLeapRegrab()
        {
            if (!_leapRegrab || Time.unscaledTime > _leapUntil)
                return false;
            if (Time.unscaledTime < _leapArmedAt)
                return false;
            Vector2 axes = ReadClimbAxes();
            if (axes.sqrMagnitude < 0.04f)
                return false;
            if (!TryJumpToWall(out RaycastHit hit))
                return false;

            Attach(hit);
            return true;
        }

        private bool TryHighFallGrab()
        {
            if (_climbing || _hopping)
                return false;
            // free-climb-dune-v2: mid-fall regrab sooner (Dune-style catch).
            if (motor != null && (motor.isGrounded || motor.groundDistance < 0.85f))
                return false;
            if (motor != null && motor.verticalVelocity > -3.2f)
                return false;
            float need = profile != null ? profile.highFallGrabMeters : 6f;
            if (!_trackedAir || _airApexY - transform.position.y < need)
                return false;
            if (InsideDetachBuffer())
                return false;
            if (PlayerVehicleState.IsMounted)
                return false;
            if (dash != null && dash.IsDashing)
                return false;
            if (!TryJumpToWall(out RaycastHit hit))
                return false;

            Attach(hit);
            return true;
        }

        private void TrackFallApex()
        {
            bool grounded = motor != null && (motor.isGrounded || motor.groundDistance < 0.28f);
            float y = transform.position.y;
            if (grounded)
            {
                _airApexY = y;
                _trackedAir = false;
                _wasGroundedForWalkOff = true;
                return;
            }

            // Walk-off from a standable ledge: suppress proximity reattach until intentional input.
            if (_wasGroundedForWalkOff && !_climbing && !_hopping && !_mantling)
            {
                _wasGroundedForWalkOff = false;
                if (Time.unscaledTime >= _walkOffSuppressUntil)
                    _walkOffSuppressUntil = Time.unscaledTime + 0.9f;
            }

            if (!_trackedAir)
            {
                _airApexY = y;
                _trackedAir = true;
                return;
            }

            if (y > _airApexY)
                _airApexY = y;
        }

        private bool InsideDetachBuffer()
        {
            if (Time.unscaledTime - _detachedAt > 2.5f)
                return false;
            float buffer = profile != null ? profile.detachBuffer : 1.15f;
            Vector3 n = Flatten(_lastWallNormal);
            float away = Vector3.Dot(transform.position - _lastWallPoint, n);
            return away < buffer;
        }


        private void SteerAirControl()
        {
            if (body == null || body.isKinematic)
                return;
            if (Time.unscaledTime > _airControlUntil)
                return;

            Vector2 axes = ReadClimbAxes();
            if (axes.sqrMagnitude < 0.02f)
                return;

            Vector3 wish = transform.right * axes.x + Flatten(transform.forward) * axes.y;
            if (wish.sqrMagnitude < 0.001f)
                return;
            wish.Normalize();

            Vector3 v = body.linearVelocity;
            v += wish * 16f * Time.fixedDeltaTime;
            Vector3 planar = new Vector3(v.x, 0f, v.z);
            if (planar.magnitude > 8f)
            {
                planar = planar.normalized * 8f;
                v.x = planar.x;
                v.z = planar.z;
            }
            SafeSetLinearVelocity(v);
        }

        private void ClimbLeap(float holdSeconds = 0f)
        {
            Vector2 axes = ReadClimbAxes();
            if (axes.sqrMagnitude < 0.04f)
                axes = Vector2.up;
            axes.Normalize();

            Vector3 nFlat = Flatten(_lastNormal);
            Vector3 right = Vector3.Cross(Vector3.up, -nFlat);
            if (right.sqrMagnitude < 0.001f)
                right = transform.right;
            right.Normalize();

            Vector3 leap = right * axes.x + Vector3.up * axes.y;
            if (leap.sqrMagnitude < 0.001f)
                leap = Vector3.up;
            leap.Normalize();
            leap += nFlat * 0.35f;
            leap.Normalize();

            float maxHop = profile != null ? profile.clingHop : 12f;
            float speedBase = profile != null ? profile.climbLeapSpeed : 7.2f;
            float window = profile != null ? profile.climbLeapRegrab : 0.45f;
            float charge = Mathf.Clamp01(holdSeconds / 0.55f);
            float dist = Mathf.Lerp(Mathf.Min(2f, maxHop * 0.22f), maxHop, charge);
            float dur = Mathf.Lerp(0.18f, 0.7f, charge);
            float speed = dist / Mathf.Max(dur, 0.12f);
            if (speed < speedBase * 0.65f)
                speed = speedBase * 0.65f;

            _lastWallPoint = transform.position;
            _lastWallNormal = _lastNormal;
            _climbing = false;
            _hopping = true;
            _lipHang = false;
            ClearOverhangState();
            SnapWorldUp();
            _hopAxes = axes;
            _hopVel = leap * speed;
            _hopUntil = Time.unscaledTime + dur;
            _leapRegrab = true;
            _leapUntil = Time.unscaledTime + dur + window;
            _leapArmedAt = Time.unscaledTime + 0.12f;
            _detachedAt = Time.unscaledTime;
            _walkOffSuppressUntil = -10f;
            WriteAnimator(_hopAxes, speed * 0.15f, climbing: true);
            SetClimbLayerWeight(1f);
        }

        private void TickHop()
        {
            _hopVel += Physics.gravity * 0.4f * Time.fixedDeltaTime;
            MoveBody(transform.position + _hopVel * Time.fixedDeltaTime);
            SnapWorldUp();
            WriteAnimator(_hopAxes, 2.2f, climbing: true);
            SetClimbLayerWeight(1f);

            if (Time.unscaledTime >= _hopUntil)
                EndHop(drop: false);
        }

        private void EndHop(bool drop)
        {
            if (!_hopping && !_motorOverridden)
                return;

            _hopping = false;
            Vector3 leftover = _hopVel;
            SnapWorldUp(faceAwayFromWall: drop);
            if (!drop && TryJumpToWall(out RaycastHit grab))
            {
                Attach(grab);
                return;
            }
            if (!drop && TryAutoMantle())
                return;
            if (!drop && _hasLastStick && !TryProbeSoffit(_lastNormal, out _))
            {
                _climbing = true;
                _lipHang = true;
                Vector3 keep = transform.position;
                float standOff = profile != null ? profile.standOff : 0.35f;
                Vector3 nFlat = Flatten(_lastNormal);
                keep.x = _lastStickHit.point.x + nFlat.x * standOff;
                keep.z = _lastStickHit.point.z + nFlat.z * standOff;
                SnapLipHang(ref keep);
                MoveBody(keep);
                FaceWall(_lastNormal);
                WriteAnimator(Vector2.zero, 0f, climbing: true);
                SetClimbLayerWeight(1f);
                return;
            }
            RestoreMotor();
            _airControlUntil = Time.unscaledTime + (profile != null ? profile.airControlSeconds : 0.95f);
            if (landing != null)
                landing.IgnoreLandsFor(0.7f);

            if (body != null && !body.isKinematic)
            {
                if (drop)
                {
                    float push = profile != null ? profile.dropPush : 2.4f;
                    leftover = Flatten(_lastNormal) * push;
                    leftover.y = 0f;
                }
                else
                {
                    leftover.y = Mathf.Max(leftover.y, 1.5f);
                }
                SafeSetLinearVelocity(leftover);
            }
        }

        private void ReleaseFromClimb()
        {
            DropFromClimb();
        }

        private void DropFromClimb()
        {
            if (Survival != null)
                Survival.suppressStaminaRegen = false;

            float push = profile != null ? profile.dropPush : 2.4f;
            float air = profile != null ? profile.airControlSeconds : 0.95f;
            Vector3 off = Flatten(_lastNormal) * push;
            Detach(addPlatformVelocity: false, faceAwayFromWall: true);
            _leapRegrab = false;
            _airControlUntil = Time.unscaledTime + air;
            if (body != null && !body.isKinematic)
            {
                Vector3 v = body.linearVelocity;
                v.x = off.x;
                v.z = off.z;
                v.y = Mathf.Min(v.y, 0f);
                SafeSetLinearVelocity(v);
            }
        }

        private void Detach(bool addPlatformVelocity, bool faceAwayFromWall = false)
        {
            if (!_climbing && !_motorOverridden)
                return;

            Vector3 platformVel = addPlatformVelocity ? _platformVel : Vector3.zero;
            if (_survival != null)
                _survival.suppressStaminaRegen = false;
            _climbing = false;
            _mantling = false;
            _mantleSettling = false;
            _reverseMantling = false;
            _hasLastStick = false;
            _ikValid = false;
            _ikWeight = 0f;
            ClearOverhangState();
            _detachedAt = Time.unscaledTime;
            // Walk-off / drop: block proximity reattach briefly unless Space+W or boost-into-wall.
            if (!_leapRegrab)
                _walkOffSuppressUntil = Time.unscaledTime + 0.85f;
            _lastWallPoint = transform.position;
            _lastWallNormal = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal : transform.forward;
            _anchor = null;
            _hasPrevAnchorPos = false;
            _dampedClimbInput = Vector3.zero;
            _dampedClimbVel = Vector3.zero;

            SnapWorldUp(faceAwayFromWall);
            RestoreMotor();
            IdleAnimator();
            if (landing != null)
                landing.IgnoreLandsFor(0.7f);

            SetPlanarVelocity(new Vector3(platformVel.x, 0f, platformVel.z));

            // Climb/ESC can leave ghost journal/pause flags; clear on detach/drop so WASD/J/E recover.
            GameplayInputRecovery.RecoverGhostUiLocks();
        }

        private void ForceUnlock()
        {
            if (_survival != null)
                _survival.suppressStaminaRegen = false;
            _climbing = false;
            _hopping = false;
            _mantling = false;
            _reverseMantling = false;
            _lipHang = false;
            _ikValid = false;
            _ikWeight = 0f;
            ClearOverhangState();
            ClearProbeState();
            _anchor = null;
            _hasPrevAnchorPos = false;
            SnapWorldUp();
            RestoreMotor();
            IdleAnimator();
        }

        private void RestoreMotor()
        {
            if (motor != null)
            {
                motor.enabled = true;
                motor.EnableGravityAndCollision();
                motor.lockMovement = false;
                motor.lockAnimMovement = false;
                motor.disableCheckGround = false;
                motor.isJumping = false;
                motor.verticalVelocity = 0f;
            }

            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                SafeZeroVelocity();
            }

            if (_character != null)
                _character.enabled = true;

            if (_capsule != null)
            {
                _capsule.enabled = true;
                _capsule.isTrigger = false;
            }

            IgnorePlayerClimbCollision(false);

            if (animator != null)
            {
                // v2j: keep root motion off briefly after mantle so locomotion don't step forward.
                if (Time.unscaledTime < _suppressRootMotionUntil || Time.unscaledTime < _suppressAnimMoveUntil)
                    animator.applyRootMotion = false;
                else
                    animator.applyRootMotion = _heldApplyRootMotion;
            }
            if (motor != null && Time.unscaledTime < _suppressAnimMoveUntil)
                motor.lockAnimMovement = true;

            _motorOverridden = false;
        }

        private static bool UiBlocksClimbDrop()
        {
            PlayerController player = PlayerLocator.FindPlayerController();
            return player != null && player.BlocksCombatInput;
        }

        private void TickDoubleWClimbStart()
        {
            if (_climbing || _hopping || _mantling || _reverseMantling)
            {
                _climbWTapCount = 0;
                _climbWWasDown = false;
                return;
            }

            Vector2 axes = ReadClimbAxes();
            bool wDown = axes.y > 0.55f;
            if (wDown && !_climbWWasDown)
            {
                if (Time.unscaledTime - _climbWTapAt <= 0.38f)
                    _climbWTapCount++;
                else
                    _climbWTapCount = 1;
                _climbWTapAt = Time.unscaledTime;

                if (_climbWTapCount >= 2)
                {
                    _climbWTapCount = 0;
                    TryStartClimbFromDoubleW();
                }
            }
            else if (!wDown && Time.unscaledTime - _climbWTapAt > 0.4f)
            {
                _climbWTapCount = 0;
            }
            _climbWWasDown = wDown;
        }

        private bool TryStartClimbFromDoubleW()
        {
            if (!CanAttachNow(ignoreBuffer: true))
                return false;
            if (!HasClimbStartStamina())
                return false;
            if (!TryJumpToWall(out RaycastHit hit))
                return false;
            if (!TryPayClimbStartStamina())
                return false;

            Attach(hit);
            Debug.Log($"[{BuildStamp}] double-W climb start grounded={(motor != null && motor.isGrounded)}");
            return true;
        }

        /// <summary>Ground start: plant on the lowest climbable point of the grabbed face (no snap-to-top).</summary>
        private void SnapToLowestClimbPoint(RaycastHit hit)
        {
            float standOff = profile != null ? profile.standOff : 0.35f;
            Vector3 n = hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : _lastNormal;
            Vector3 wallU = WallUp(n);
            Vector3 bestPoint = hit.point;
            Vector3 bestN = n;
            float bestY = hit.point.y;

            Vector3 origin = hit.point + n * 0.12f;
            for (int i = 1; i <= 12; i++)
            {
                Vector3 o = origin - wallU * (0.22f * i);
                if (!TryProbeRange(o, -n, 1.5f, 0.22f, out RaycastHit cand))
                    continue;
                if (!IsClimbableHit(cand) || IsSelfHit(cand))
                    continue;
                if (!IsClimbableSlope(cand.normal))
                    continue;
                if (cand.point.y < bestY - 0.02f)
                {
                    bestY = cand.point.y;
                    bestPoint = cand.point;
                    bestN = cand.normal.normalized;
                }
            }

            _lastNormal = bestN;
            Vector3 desired = bestPoint + bestN * standOff;
            float hh = profile != null ? profile.handHeight : 1.18f;
            desired -= wallU * Mathf.Clamp(hh * 0.55f, 0.35f, 0.85f);
            MoveBody(desired);
            FaceWall(bestN);
            _lastStickHit = hit;
            _hasLastStick = true;
            _anchor = hit.transform;
            if (_anchor != null)
            {
                _localOffset = _anchor.InverseTransformPoint(desired);
                _prevAnchorPos = _anchor.position;
                _hasPrevAnchorPos = true;
            }
        }

        /// <summary>Air jump-grab: nearest face at current height (do not slide to top or bottom).</summary>
        private void SnapToNearestFace(RaycastHit hit)
        {
            SnapToHitKeepHeight(hit);
            FaceWall(hit.normal.sqrMagnitude > 0.001f ? hit.normal.normalized : _lastNormal);
            _lastStickHit = hit;
            _hasLastStick = true;
        }

        private void SnapToHitKeepHeight(RaycastHit hit)
        {
            float standOff = profile != null ? profile.standOff : 0.35f;
            Vector3 desired = transform.position;
            desired.x = hit.point.x + hit.normal.x * standOff;
            desired.z = hit.point.z + hit.normal.z * standOff;
            MoveBody(desired);
            _anchor = hit.transform;
            if (_anchor != null)
            {
                _localOffset = _anchor.InverseTransformPoint(desired);
                _prevAnchorPos = _anchor.position;
                _hasPrevAnchorPos = true;
            }
        }


        private void IgnorePlayerClimbCollision(bool ignore)
        {
            int playerLayer = gameObject.layer;
            int climbLayer = LayerMask.NameToLayer(profile != null ? profile.climbableLayerName : "Climbable");
            if (playerLayer >= 0 && climbLayer >= 0)
                Physics.IgnoreLayerCollision(playerLayer, climbLayer, ignore);
        }

        private void FaceWall(Vector3 normal)
        {
            // Probe graph owns angled faces too — do not early-out on shallow slopes (that left Kade upright and clipping).
            bool probeBound = PreferBakedProbes() && _probeSet != null && _probeIndex >= 0;
            if (!probeBound && Vector3.Angle(Vector3.up, normal) <= 55f)
                return;
            Quaternion rot = AlignToWall(normal);
            if (probeBound)
            {
                // Hard snap when the face normal changes a lot (flat -> angled corner), else fast blend.
                float delta = Quaternion.Angle(transform.rotation, rot);
                if (delta > 16f)
                    ApplyRotation(rot);
                else
                    ApplyRotation(Quaternion.Slerp(transform.rotation, rot, 0.88f));
                return;
            }
            // v1j: slower facing blend on free climb so exterior corners don't visually snap.
            float ang = Quaternion.Angle(transform.rotation, rot);
            float faceT = ang > 40f ? 0.28f : 0.4f;
            ApplyRotation(Quaternion.Slerp(transform.rotation, rot, faceT));
        }

        private static Quaternion AlignToWall(Vector3 normal)
        {
            Vector3 n = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.forward;
            Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, n);
            if (wallUp.sqrMagnitude < 0.0001f)
                wallUp = Vector3.up;
            wallUp.Normalize();

            Vector3 look = -n;
            if (Mathf.Abs(Vector3.Dot(look, wallUp)) > 0.97f)
            {
                look = Vector3.ProjectOnPlane(new Vector3(-n.x, 0f, -n.z), n);
                if (look.sqrMagnitude < 0.0001f)
                    look = Vector3.Cross(wallUp, Vector3.right);
            }
            look.Normalize();
            return Quaternion.LookRotation(look, wallUp);
        }

        private static Quaternion UprightFrom(Vector3 forward)
        {
            Vector3 f = forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.001f)
                f = Vector3.forward;
            return Quaternion.LookRotation(f.normalized, Vector3.up);
        }

        private void ApplyRotation(Quaternion rot)
        {
            if (body != null)
                body.MoveRotation(rot);
            else
                transform.rotation = rot;
        }

        /// <summary>
        /// Drop/leap/retry: stand world-up immediately. MoveRotation is deferred and
        /// gets discarded when we unlock the kinematic body on the same frame.
        /// Mantle already slerps to UprightFrom; this is a hard snap for off-wall exits.
        /// Drop-off (faceAwayFromWall) yaws look along Flatten(_lastNormal) so the mesh
        /// faces away from the wall. Camera is not retargeted.
        /// </summary>
        private void SnapWorldUp(bool faceAwayFromWall = false)
        {
            Vector3 f;
            if (faceAwayFromWall)
            {
                f = Flatten(_lastNormal);
                if (f.sqrMagnitude < 0.001f)
                    f = Flatten(transform.forward);
                if (f.sqrMagnitude < 0.001f)
                    f = Vector3.forward;
            }
            else
            {
                f = transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude < 0.001f)
                    f = Flatten(-_lastNormal);
                if (f.sqrMagnitude < 0.001f)
                    f = Vector3.forward;
            }
            Quaternion rot = Quaternion.LookRotation(f.normalized, Vector3.up);
            transform.rotation = rot;
            if (body != null)
                body.rotation = rot;
            SafeZeroVelocity();
        }

        private void SetPlanarVelocity(Vector3 planar)
        {
            if (body == null || body.isKinematic)
                return;
            Vector3 v = body.linearVelocity;
            v.x = planar.x;
            v.z = planar.z;
            v.y = 0f;
            SafeSetLinearVelocity(v);
        }

        private bool IsSaneMove(Vector3 worldPos)
        {
            if (float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y) || float.IsNaN(worldPos.z))
                return false;
            Vector3 cur = transform.position;
            if ((worldPos - cur).sqrMagnitude > 16f)
                return false;
            if (cur.sqrMagnitude > 25f && new Vector3(worldPos.x, 0f, worldPos.z).sqrMagnitude < 4f)
                return false;
            return true;
        }

        private void MoveBody(Vector3 worldPos)
        {
            if (!IsSaneMove(worldPos))
                return;
            if (body != null)
                body.MovePosition(worldPos);
            else
                transform.position = worldPos;
        }

        private void WriteAnimator(Vector2 axes, float speed, bool climbing)
        {
            if (animator == null)
                return;

            if (_hasClimbX)
                animator.SetFloat(ClimbXHash, axes.x);
            if (_hasClimbY)
                animator.SetFloat(ClimbYHash, axes.y);
            if (_hasClimbSpeed)
                animator.SetFloat(ClimbSpeedHash, speed);
            if (_hasIsClimbing)
                animator.SetBool(IsClimbingHash, climbing);
        }

        private void UpdateHandGrabTargets(Vector3 wallNormal)
        {
            if (_overhangHang || _overhangGrabbing)
            {
                SetOverhangHandIk();
                return;
            }

            if (_probeSet != null && _probeIndex >= 0
                && _probeSet.GetWorldPose(_probeIndex, out Vector3 pp, out Vector3 pn, out _, out _))
            {
                SetProbeHandIk(pp, pn.sqrMagnitude > 0.0001f ? pn : wallNormal);
                return;
            }

            // Live IK chased the look-at pose and made the arms jiggle.
            // Clip pose is close enough to the wall.
            _ikValid = false;
            _ikWeight = 0f;
        }

        private Vector3 SnapToSurface(Vector3 above, float palm)
        {
            if (SurfaceCast(above, Vector3.down, 0.28f, 0.04f, out RaycastHit hit))
                return hit.point + hit.normal * Mathf.Max(0.03f, palm * 0.35f);
            return above + Vector3.down * 0.04f;
        }

        private Vector3 ProjectHandToWall(Vector3 hand, Vector3 wallNormal, float palm)
        {
            Vector3 n = wallNormal.sqrMagnitude > 0.001f ? wallNormal.normalized : transform.forward;
            Vector3 from = hand + n * 0.28f;
            if (SurfaceCast(from, -n, 0.7f, 0.03f, out RaycastHit hit) && IsSaneMove(hit.point))
            {
                Vector3 p = hit.point + hit.normal * palm;
                p.y = hand.y;
                return p;
            }

            float dist = Vector3.Dot(hand - transform.position, n);
            return hand - n * dist + n * palm;
        }

        private Vector3 ProbeHand(Vector3 origin, Vector3 intoWall, float palm)
        {
            if (intoWall.sqrMagnitude < 0.001f)
                return origin;
            intoWall.Normalize();
            Vector3 nOut = -intoWall;

            if (SurfaceCast(origin, intoWall, 1.15f, 0.04f, out RaycastHit hit))
                return hit.point + hit.normal * palm;

            if (_hasLastStick)
                return _lastStickHit.point + nOut * palm;

            return origin + intoWall * 0.22f + nOut * palm;
        }

        public void ApplyHandIK(int layerIndex)
        {
            if (animator == null)
                return;

            bool probeBound = _probeSet != null && _probeIndex >= 0;
            bool climbLayer = _climbLayerIndex >= 0 && layerIndex == _climbLayerIndex;
            // If Climb lacks IK Pass, still drive hands from Base (0) while probe-bound / IK valid.
            bool baseFallback = layerIndex == 0 && (probeBound || _ikValid);
            if (!climbLayer && !baseFallback)
                return;

            // Once-per-frame: Climb IK Pass and Base OnAnimatorIK must not double-apply.
            if (_ikAppliedFrame == Time.frameCount)
                return;
            _ikAppliedFrame = Time.frameCount;

            if ((!_climbing && !_mantling) || !_ikValid)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
                animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
                return;
            }

            float w = _ikWeight;
            if (_mantling)
            {
                AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(_climbLayerIndex >= 0 ? _climbLayerIndex : 0);
                if (st.shortNameHash == ClimbStandupState)
                    w *= Mathf.Clamp01(1f - st.normalizedTime * 1.4f);
            }

            Vector3 n = _lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : transform.forward;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, w);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, probeBound ? 0.4f : 0f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, _ikLeft);
            if (probeBound)
            {
                Vector3 into = -(_lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : transform.forward);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(into, Vector3.up));
            }

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, probeBound ? 0.4f : 0f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, _ikRight);
            if (probeBound)
            {
                Vector3 intoR = -(_lastNormal.sqrMagnitude > 0.001f ? _lastNormal.normalized : transform.forward);
                animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(intoR, Vector3.up));
            }

            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, w * 0.45f);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, _ikLeft + n * 0.22f - right * 0.14f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, w * 0.45f);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, _ikRight + n * 0.22f + right * 0.14f);
        }

        private void SetClimbLayerWeight(float weight)
        {
            if (animator == null || _climbLayerIndex < 0)
                return;
            animator.SetLayerWeight(_climbLayerIndex, weight);
        }

        private static Vector2 ReadClimbAxes()
        {
            return DMJetpackMoveInput.ReadPlanarRaw();
        }

        private static bool ReadJumpPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
            return false;
        }

        private static bool ReadJumpHeld()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
                return true;
            return false;
        }

        private static bool ReadShiftHeld()
        {
            if (Keyboard.current != null
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
                return true;
            if (Gamepad.current != null && Gamepad.current.leftShoulder.isPressed)
                return true;
            return false;
        }

        private static bool ReadInteractPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
                return true;
            return false;
        }

#if UNITY_EDITOR
        // Always draw ClingSense in Scene while climbing (no hierarchy selection required).
        private void OnDrawGizmos()
        {
            if (!ClingSenseEnabled())
                return;
            if (profile != null && !profile.drawClingSenseGizmos)
                return;
            // Play mode: show whenever climbing (or always if we have a fresh sample).
            if (Application.isPlaying)
            {
                if (_climbing || _overhangHang || _overhangGrabbing || _mantling)
                    DrawClingSenseGizmos(forceRefresh: true);
                else if (_cling.valid)
                    DrawClingSenseGizmos(forceRefresh: false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Edit mode / selected: always preview bubble even when not playing.
            if (!Application.isPlaying)
                DrawClingSenseGizmos(forceRefresh: true);
            else if (ClingSenseEnabled() && (profile == null || profile.drawClingSenseGizmos))
                DrawClingSenseGizmos(forceRefresh: true);

            Vector3 n = Flatten(_lastNormal.sqrMagnitude > 0.001f ? _lastNormal : transform.forward);
            if (n.sqrMagnitude < 0.001f)
                n = Flatten(transform.forward);
            float hh = profile != null ? profile.handHeight : 1.18f;
            float up = OverhangReachUp();
            float back = OverhangReachBack();
            Vector3 origin = _gizmoOverhangValid
                ? _gizmoOverhangOrigin
                : transform.position + Vector3.up * hh + n * (profile != null ? profile.standOff : 0.35f);
            Gizmos.color = new Color(0.95f, 0.85f, 0.2f, 0.95f);
            Gizmos.DrawRay(origin, Vector3.up * up);
            Gizmos.color = new Color(0.85f, 0.2f, 0.75f, 0.95f);
            Gizmos.DrawRay(origin, n * back);
            if (_gizmoOverhangValid)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.95f);
                Gizmos.DrawSphere(_gizmoOverhangLip, 0.06f);
            }
            if (!_ikValid)
                return;
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.95f);
            Gizmos.DrawSphere(_ikLeft, 0.04f);
            Gizmos.DrawSphere(_ikRight, 0.04f);
        }

        private void DrawClingSenseGizmos(bool forceRefresh)
        {
            if (profile != null && !profile.drawClingSenseGizmos)
                return;
            if (!ClingSenseEnabled())
                return;

            if (forceRefresh || !_cling.valid)
                RefreshClingSense();

            float radius = Mathf.Max(0.35f, _clingSense.BubbleRadius);
            Vector3 origin = _cling.valid
                ? _cling.origin
                : transform.position + Vector3.up * (profile != null ? profile.handHeight * 0.55f : 0.65f);

            // HDRP Scene often eats faint Gizmos.DrawWireSphere — use Handles discs + bold lines.
            Handles.color = new Color(0.1f, 0.95f, 1f, 1f);
            Handles.DrawWireDisc(origin, Vector3.up, radius);
            Handles.DrawWireDisc(origin, Vector3.right, radius);
            Handles.DrawWireDisc(origin, Vector3.forward, radius);
            Handles.DrawWireDisc(origin, Vector3.up, radius * 0.5f);
            Handles.DrawWireDisc(origin, Vector3.right, radius * 0.5f);
            Handles.DrawWireDisc(origin, Vector3.forward, radius * 0.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(origin, 0.1f);

            // Always show sense axes so missing hits are still obvious.
            float axis = Mathf.Max(0.55f, _clingSense.RayRange);
            Gizmos.color = new Color(1f, 0.2f, 0.95f, 0.85f); // soffit / up
            Gizmos.DrawRay(origin, Vector3.up * axis);
            Gizmos.color = new Color(1f, 0.92f, 0.1f, 0.85f); // ground / down
            Gizmos.DrawRay(origin, Vector3.down * axis);
            Vector3 into = _lastNormal.sqrMagnitude > 0.001f ? -_lastNormal.normalized : -transform.forward;
            Gizmos.color = new Color(0.15f, 1f, 0.25f, 0.75f); // face
            Gizmos.DrawRay(origin, into * axis);

            if (!_cling.valid)
                return;

            if (_cling.hasFace)
            {
                Gizmos.color = new Color(0.15f, 1f, 0.25f, 1f);
                Gizmos.DrawLine(origin, _cling.faceHit.point);
                Gizmos.DrawSphere(_cling.faceHit.point, 0.1f);
                Gizmos.DrawRay(_cling.faceHit.point, _cling.faceNormal * 0.45f);
            }
            if (_cling.hasSoffit)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.95f, 1f);
                Gizmos.DrawLine(origin, _cling.soffitHit.point);
                Gizmos.DrawSphere(_cling.soffitHit.point, 0.09f);
            }
            if (_cling.hasWalkableBelow)
            {
                Gizmos.color = new Color(1f, 0.92f, 0.1f, 1f);
                Gizmos.DrawLine(origin, _cling.groundHit.point);
                Gizmos.DrawSphere(_cling.groundHit.point, 0.09f);
            }
            if (_cling.hasLip)
            {
                Gizmos.color = _cling.isStubLip
                    ? new Color(1f, 0.55f, 0.05f, 1f)
                    : new Color(1f, 0.12f, 0.12f, 1f);
                Gizmos.DrawLine(origin, _cling.lipHit.point);
                Gizmos.DrawSphere(_cling.lipHit.point, 0.11f);
            }
            if (_cling.hasSideL)
            {
                Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.9f);
                Gizmos.DrawLine(origin, _cling.sideLHit.point);
                Gizmos.DrawSphere(_cling.sideLHit.point, 0.09f);
            }
            if (_cling.hasSideR)
            {
                Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.9f);
                Gizmos.DrawLine(origin, _cling.sideRHit.point);
                Gizmos.DrawSphere(_cling.sideRHit.point, 0.09f);
            }

            // Sphere fan gizmo (only when enabled)
            int stored = (_clingSense != null && _clingSense.EnableSphereFan) ? _clingSense.SphereHitStored : 0;
            for (int i = 0; i < stored; i++)
            {
                Vector3 pt = _clingSense.GetSphereHitPoint(i);
                byte kind = _clingSense.GetSphereHitKind(i);
                switch (kind)
                {
                    case 1: Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.55f); break;
                    case 2: Gizmos.color = new Color(1f, 0.25f, 0.95f, 0.55f); break;
                    case 3: Gizmos.color = new Color(1f, 0.85f, 0.15f, 0.55f); break;
                    default: Gizmos.color = new Color(0.55f, 0.75f, 1f, 0.35f); break;
                }
                Gizmos.DrawLine(origin, pt);
                Gizmos.DrawSphere(pt, 0.035f);
            }

            if (_clingSense != null && _clingSense.EnableSphereFan)
            {
            // Miss envelope: sparse empty rays so the fan is visible even in open air.
            float env = Mathf.Clamp(_cling.sphereRange > 0.1f ? _cling.sphereRange : _clingSense.RayRange, 1f, 2f);
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.18f);
            for (float pitch = -90f; pitch <= 90.01f; pitch += 40f)
            {
                float pr = pitch * Mathf.Deg2Rad;
                float cp = Mathf.Cos(pr);
                float sp = Mathf.Sin(pr);
                bool pole = Mathf.Abs(pitch) >= 89.5f;
                for (float yaw = 0f; yaw < 359.9f; yaw += 40f)
                {
                    if (pole && yaw > 0.01f) break;
                    Vector3 d = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad) * cp, sp, Mathf.Cos(yaw * Mathf.Deg2Rad) * cp);
                    Gizmos.DrawRay(origin, d.normalized * env);
                }
            }
            }
        }
#endif
    }
}
