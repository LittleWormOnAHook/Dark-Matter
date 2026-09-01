using Invector.vCharacterController;
using Project.Features.Dash;
using Project.Features.Jetpack;
using Project.Player;
using Project.Vehicles;
using UnityEngine;
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
        private const string BuildStamp = "DMClimb 0831-drop180";

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

        private Transform _anchor;
        private Vector3 _localOffset;
        private Vector3 _prevAnchorPos;
        private bool _hasPrevAnchorPos;
        private Vector3 _platformVel;
        private Vector3 _lastNormal = Vector3.back;
        private Vector3 _dampedClimbInput;
        private Vector3 _dampedClimbVel;
        private float _attachedAt = -10f;
        private float _detachedAt = -10f;
        private float _stickLostAt = -10f;
        private float _leapUntil = -10f;
        private bool _leapRegrab;
        private bool _hopping;
        private Vector3 _hopVel;
        private Vector2 _hopAxes;
        private float _hopUntil = -10f;
        private float _hopChargeAt = -10f;
        private bool _lipHang;
        private float _lastClingFeetY;
        private bool _hasClingFeetY;
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
        private bool _mantleSawClip;
        private float _mantleBeganAt;
        private Quaternion _mantleStartRot = Quaternion.identity;
        private Quaternion _mantleEndRot = Quaternion.identity;
        private Vector3 _mantleStart;
        private Vector3 _mantleLip;
        private Vector3 _ikLeft;
        private Vector3 _ikRight;
        private float _ikWeight;
        private bool _ikValid;
        private Transform _leftHand;
        private Transform _rightHand;
        private Transform _leftGrab;
        private Transform _rightGrab;
        private readonly RaycastHit[] _castHits = new RaycastHit[24];

        public bool IsClimbing => _climbing || _hopping || _mantling;
        private float MantlePlantPad => profile != null ? profile.mantlePlantHeight : 0f;
        public DMClimbProfile Profile => profile;

        public void CancelClimb()
        {
            if (_climbing || _hopping || _motorOverridden)
                ForceUnlock();
        }

        /// <summary>Retry/death: unlock climb leftovers and stand world-up.</summary>
        public void RestoreAfterDeathOrRetry()
        {
            _climbing = false;
            _hopping = false;
            _mantling = false;
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
            Debug.Log(BuildStamp);
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
                if (ReadInteractPressedThisFrame())
                    EndHop(drop: true);
                else
                    TryLeapRegrab();
                return;
            }

            if (_mantling)
                return;

            if (_climbing)
            {
                if (ReadInteractPressedThisFrame())
                {
                    _hopChargeAt = -10f;
                    DropFromClimb();
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
                    if (Time.unscaledTime - _attachedAt > 0.08f)
                        ClimbLeap(held);
                }
                return;
            }

            if (_leapRegrab && Time.unscaledTime > _leapUntil)
                _leapRegrab = false;

            TrackFallApex();

            if (TryLeapRegrab())
                return;

            FailsafeUnlocked();
        }

        private void FixedUpdate()
        {
            ResolveMask();

            if (_mantling)
                TickMantle();
            else if (_hopping)
                TickHop();
            else if (_climbing)
                StickAndMove();
            else
                SteerAirControl();
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
            if (profile != null && profile.startClimbNeedsForward && axes.y <= 0.18f)
                return false;

            if (!CanAttachNow(ignoreBuffer: true))
                return false;

            if (TryJumpToWall(out RaycastHit hit))
            {
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

            float faceDot = Vector3.Dot(transform.forward, -best.normal);
            float minDot = profile != null ? profile.faceDotMin : 0.2f;
            return faceDot >= minDot * 0.5f;
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
                // Standing on the climbable top is the point of a mantle.
                if (climbLayer >= 0 && c.gameObject.layer == climbLayer)
                    continue;
                return true;
            }
            return false;
        }

        private bool TryClearStand(RaycastHit top, Vector3 forward, out Vector3 stand)
        {
            float height = _capsule != null ? _capsule.height : 1.84f;
            float capR = _capsule != null ? _capsule.radius : 0.26f;
            float r = capR * 0.9f;
            Vector3 fwd = Flatten(forward);

            stand = top.point + Vector3.up * 0.06f + fwd * 0.3f;
            if (!StandBlocked(stand, height, r))
                return true;

            stand = top.point + Vector3.up * 0.06f + fwd * 0.55f;
            if (!StandBlocked(stand, height, r))
                return true;

            stand = top.point + Vector3.up * 0.08f + fwd * 0.8f;
            if (!StandBlocked(stand, height, r))
                return true;

            stand = default;
            return false;
        }

        private void Attach(RaycastHit hit)
        {
            _climbing = true;
            _hopping = false;
            _lipHang = false;
            _hopChargeAt = -10f;
            _hasClingFeetY = false;
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

            SnapToHitKeepHeight(hit);
            if (SprayHandholds(-_lastNormal, out _, out Vector3 avgN) && avgN.sqrMagnitude > 0.001f)
                _lastNormal = Vector3.Slerp(_lastNormal, avgN, 0.55f).normalized;
            FaceWall(_lastNormal);
            WriteAnimator(Vector2.zero, 0f, climbing: true);
            SetClimbLayerWeight(1f);
        }

        private void StickAndMove()
        {
            if (_anchor != null && _hasPrevAnchorPos && Time.fixedDeltaTime > 0.0001f)
                _platformVel = (_anchor.position - _prevAnchorPos) / Time.fixedDeltaTime;
            else
                _platformVel = Vector3.zero;

            Vector3 probeDir = -_lastNormal;
            if (probeDir.sqrMagnitude < 0.001f)
                probeDir = transform.forward;

            float stickRange = profile != null ? profile.attachRange + 0.4f : 1.8f;
            float radius = profile != null ? profile.probeRadius : 0.18f;
            Vector2 raw = ReadClimbAxes();
            bool holdingW = raw.y > 0.02f;
            bool holdingS = raw.y < -0.2f;

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
                if (holdingS && TryExitOntoGround())
                    return;
                if (!holdingS && TryAutoMantle())
                    return;
                _lipHang = true;
                if (_stickLostAt < 0f)
                    _stickLostAt = Time.unscaledTime;

                Vector3 keep = transform.position;
                if (_hasLastStick)
                {
                    float standOffLost = profile != null ? profile.standOff : 0.35f;
                    Vector3 nFlatLost = Flatten(_lastNormal);
                    keep.x = _lastStickHit.point.x + nFlatLost.x * standOffLost;
                    keep.z = _lastStickHit.point.z + nFlatLost.z * standOffLost;
                }
                SnapLipHang(ref keep);
                if (holdingS)
                {
                    Detach(addPlatformVelocity: true, faceAwayFromWall: true);
                    return;
                }
                MoveBody(keep);
                FaceWall(_lastNormal);
                WriteAnimator(Vector2.zero, 0f, climbing: true);
                return;
            }
            _stickLostAt = -10f;
            _lastStickHit = hit;
            _hasLastStick = true;

            Vector3 normal = Vector3.Slerp(_lastNormal, clingNormal, 0.45f).normalized;
            if (normal.sqrMagnitude < 0.001f)
                normal = clingNormal.sqrMagnitude > 0.001f ? clingNormal : hit.normal.normalized;
            _lastNormal = normal;

            Vector3 right = Vector3.Cross(Vector3.up, -normal);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(transform.right, -normal);
            right.Normalize();

            if (raw.y < -0.2f && TryExitOntoGround())
                return;
            if (!HasWallAbove(_lastNormal))
            {
                if (TryAutoMantle())
                    return;
                _lipHang = true;
                raw.y = Mathf.Min(raw.y, 0f);
            }
            else
            {
                _lipHang = false;
                _lastClingFeetY = transform.position.y;
                _hasClingFeetY = true;
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
            Vector3 desired = transform.position;
            desired.x = hit.point.x + normal.x * standOff;
            desired.z = hit.point.z + normal.z * standOff;
            desired.y = transform.position.y + _dampedClimbInput.y * speed * Time.fixedDeltaTime;
            desired += right * (_dampedClimbInput.x * speed * Time.fixedDeltaTime);
            if (_lipHang)
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
            float[] heights = { 0.12f, 0.28f, 0.45f, 0.7f, 1.0f, 1.35f, 1.6f };
            Vector3[] dirs =
            {
                probeDir,
                Quaternion.AngleAxis(-50f, Vector3.up) * probeDir,
                Quaternion.AngleAxis(50f, Vector3.up) * probeDir,
            };
            bool any = false;
            for (int h = 0; h < heights.Length; h++)
            {
                Vector3 origin = transform.position + Vector3.up * heights[h];
                for (int d = 0; d < dirs.Length; d++)
                {
                    if (!TryProbeRange(origin, dirs[d], stickRange + 0.5f, radius, out RaycastHit cand))
                        continue;
                    if (!IsClimbableHit(cand) || IsSelfHit(cand))
                        continue;
                    // Top of the block is walkable — do not snap cling onto it (that twitch-yaws at the lip).
                    if (Vector3.Angle(Vector3.up, cand.normal) <= 55f)
                        continue;
                    // Already clinging: keep the face even if it eases under 75°.
                    if (!any || cand.distance < hit.distance)
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
            if (AtHandLip(_lastNormal, out RaycastHit lip))
            {
                float hh = profile != null ? profile.handHeight : 1.18f;
                pos.y = lip.point.y - hh;
                return;
            }
            if (_hasClingFeetY)
                pos.y = _lastClingFeetY;
        }

        private bool AtHandLip(Vector3 wallNormal, out RaycastHit top)
        {
            top = default;
            Vector3 forward = Flatten(-wallNormal);
            if (forward.sqrMagnitude < 0.001f)
                forward = Flatten(transform.forward);

            // Hands ~1.15m. Only a walkable top sitting at the hands is a lip.
            float[] overs = { 0.32f, 0.48f, 0.2f };
            float[] ups = { 1.28f, 1.42f, 1.55f };
            RaycastHit best = default;
            float bestY = float.MaxValue;
            bool any = false;
            for (int u = 0; u < ups.Length; u++)
            {
                for (int i = 0; i < overs.Length; i++)
                {
                    Vector3 over = transform.position + Vector3.up * ups[u] + forward * overs[i];
                    if (!SurfaceCast(over, Vector3.down, 0.7f, 0.06f, out RaycastHit hit) || IsSelfHit(hit))
                        continue;
                    if (Vector3.Angle(Vector3.up, hit.normal) > 50f)
                        continue;
                    if (hit.point.y < transform.position.y + 0.85f)
                        continue;
                    if (hit.point.y > transform.position.y + 1.7f)
                        continue;
                    if (!any || hit.point.y < bestY)
                    {
                        best = hit;
                        bestY = hit.point.y;
                        any = true;
                    }
                }
            }

            if (!any)
                return false;
            top = best;
            return true;
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
            // Climb start holds W. Never invent a ledge when the lip probe misses.
            if (Time.unscaledTime - _attachedAt < 0.35f)
                return false;
            if (HasWallAbove(wallNormal))
                return false;
            if (!AtHandLip(wallNormal, out RaycastHit top))
                return false;

            Vector3 forward = -Flatten(wallNormal);
            if (forward.sqrMagnitude < 0.001f)
                forward = Flatten(transform.forward);
            float pad = MantlePlantPad;
            float fwd = profile != null ? profile.mantleForward : 0f;
            Vector3 stand = top.point + forward * fwd;
            stand.y = top.point.y + pad;
            BeginMantle(stand);
            return true;
        }

        private bool TryMantle(Vector3 wallNormal, bool requireUp = true)
        {
            if (profile != null && !profile.enableMantle)
                return false;
            if (Time.unscaledTime - _attachedAt < 0.35f)
                return false;

            if (requireUp)
            {
                Vector2 raw = ReadClimbAxes();
                if (raw.y <= 0f)
                    return false;
            }
            if (HasWallAbove(wallNormal))
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
            float fwd = profile != null ? profile.mantleForward : 0f;
            float up = profile != null ? profile.mantleProbeUp : 1.5f;
            float down = profile != null ? profile.mantleProbeDown : 1.7f;
            float pad = MantlePlantPad;
            float findFwd = Mathf.Max(0.28f, fwd);
            Vector3 probe = start + onto * findFwd + Vector3.up * up;
            RaycastHit floor = default;
            bool haveFloor = SurfaceCast(probe, Vector3.down, down, 0.12f, out floor)
                && Vector3.Angle(Vector3.up, floor.normal) <= 50f
                && IsSaneMove(floor.point)
                && floor.point.y <= start.y + 1.7f;
            if (!haveFloor && AtHandLip(_lastNormal, out floor))
                haveFloor = IsSaneMove(floor.point);
            if (haveFloor)
            {
                stand = floor.point + onto * fwd;
                stand.y = floor.point.y + pad;
            }
            else if (!IsSaneMove(stand) || stand.y > start.y + 1.55f)
            {
                stand = start + onto * 0.45f;
                stand.y = start.y;
            }

            // Rise on the hang side of the wall, then step over. A straight
            // hang→stand lerp cuts the corner through the block collider.
            Vector3 lip = start;
            lip.y = Mathf.Max(stand.y + 0.14f, start.y + 0.22f);

            _mantleStand = stand;
            _mantleFloorY = stand.y - MantlePlantPad;
            _mantleStart = start;
            _mantleLip = lip;
            _mantleStartRot = transform.rotation;
            _mantleEndRot = UprightFrom(onto);
            _mantling = true;
            _mantleSawClip = false;
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
            if (ReadClimbAxes().y < -0.2f)
            {
                _mantling = false;
                WriteAnimator(Vector2.zero, 0f, climbing: true);
                return;
            }

            WriteAnimator(Vector2.zero, 0f, climbing: false);
            SetClimbLayerWeight(1f);
            UpdateHandGrabTargets(_lastNormal);

            bool inOver = false;
            bool inStand = false;
            float standNorm = 0f;
            float overNorm = 0f;
            if (animator != null && _climbLayerIndex >= 0)
            {
                AnimatorStateInfo cur = animator.GetCurrentAnimatorStateInfo(_climbLayerIndex);
                AnimatorStateInfo nxt = animator.IsInTransition(_climbLayerIndex)
                    ? animator.GetNextAnimatorStateInfo(_climbLayerIndex)
                    : cur;
                inOver = cur.shortNameHash == ClimbMantleState || nxt.shortNameHash == ClimbMantleState;
                inStand = cur.shortNameHash == ClimbStandupState || nxt.shortNameHash == ClimbStandupState;
                if (cur.shortNameHash == ClimbMantleState)
                    overNorm = cur.normalizedTime;
                if (cur.shortNameHash == ClimbStandupState)
                    standNorm = cur.normalizedTime;
                if (inOver || inStand)
                    _mantleSawClip = true;
            }

            _mantleStand.y = _mantleFloorY + MantlePlantPad;

            float dur = profile != null ? profile.mantleSeconds : 1.4f;
            float t = Mathf.Clamp01((Time.unscaledTime - _mantleBeganAt) / dur);
            Vector3 pos;
            if (t < 0.55f)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 0.55f);
                pos = Vector3.Lerp(_mantleStart, _mantleLip, u);
            }
            else
            {
                float u = Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f);
                pos = Vector3.Lerp(_mantleLip, _mantleStand, u);
            }
            if (pos.y < _mantleStart.y)
                pos.y = _mantleStart.y;
            MoveBody(pos);

            float rotT = t < 0.55f ? 0f : Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f);
            ApplyRotation(Quaternion.Slerp(_mantleStartRot, _mantleEndRot, rotT));

            if (t < 0.98f)
                return;

            if (IsSaneMove(_mantleStand))
                MoveBody(_mantleStand);
            _mantling = false;
            Detach(addPlatformVelocity: false);
        }

        private bool TryExitOntoGround()
        {
            if (Time.unscaledTime - _attachedAt < 0.12f)
                return false;

            Vector3 feet = transform.position + Vector3.up * 0.12f;
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
            for (int i = 0; i < origins.Length; i++)
            {
                if (!Physics.Raycast(origins[i], Vector3.down, out RaycastHit hit, 0.7f, ~0, QueryTriggerInteraction.Ignore))
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
            Vector2 axes = ReadClimbAxes();
            return axes.y < -0.25f;
        }

        private bool TryDropToHang(out RaycastHit face)
        {
            face = default;
            if (profile == null || !profile.dropToHang)
                return false;
            if (motor != null && !motor.isGrounded)
                return false;

            ResolveMask();

            float hangRange = profile.dropToHangRange;
            float radius = profile.probeRadius;
            Vector3 feet = transform.position + Vector3.up * 0.12f;
            Vector3 forward = transform.forward;
            Vector3 lip = feet + forward * 0.45f;

            if (Physics.Raycast(lip, Vector3.down, 0.55f, ~0, QueryTriggerInteraction.Ignore))
                return false;

            Vector3 below = lip + Vector3.down * 0.35f;
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
            if (motor != null && (motor.isGrounded || motor.groundDistance < 1.4f))
                return false;
            if (motor != null && motor.verticalVelocity > -6f)
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
                return;
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
            SnapWorldUp();
            _hopAxes = axes;
            _hopVel = leap * speed;
            _hopUntil = Time.unscaledTime + dur;
            _leapRegrab = true;
            _leapUntil = Time.unscaledTime + dur + window;
            _leapArmedAt = Time.unscaledTime + 0.12f;
            _detachedAt = Time.unscaledTime;
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
            if (!drop && _hasLastStick)
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
            _climbing = false;
            _mantling = false;
            _hasLastStick = false;
            _ikValid = false;
            _ikWeight = 0f;
            _detachedAt = Time.unscaledTime;
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
        }

        private void ForceUnlock()
        {
            _climbing = false;
            _hopping = false;
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
                animator.applyRootMotion = _heldApplyRootMotion;

            _motorOverridden = false;
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
            if (Vector3.Angle(Vector3.up, normal) <= 55f)
                return;
            Quaternion rot = AlignToWall(normal);
            ApplyRotation(Quaternion.Slerp(transform.rotation, rot, 0.45f));
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
            // Only the Climb layer. Applying on Base too folds the arms.
            if (_climbLayerIndex >= 0 && layerIndex != _climbLayerIndex)
                return;

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
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, _ikLeft);

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, _ikRight);

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
        private void OnDrawGizmosSelected()
        {
            if (!_ikValid)
                return;
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.95f);
            Gizmos.DrawSphere(_ikLeft, 0.04f);
            Gizmos.DrawSphere(_ikRight, 0.04f);
        }
#endif
    }
}
