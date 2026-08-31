using Invector.vCharacterController;
using Project.Features.Climb;
using Project.Features.Dash;
using Project.Features.Jetpack;
using Project.Survival;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Shared runtime pose overlay for dash (one lifted skate foot) and
    /// jetpack boost (calm hang legs). Visual only — LateUpdate bones, no IK posing.
    /// Dash snapshots because animator.speed is 0. Boost is small additive on Fly.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(3200)]
    public sealed class DMHangLegOverlay : MonoBehaviour
    {
        private const float DashLiftMeters = 0.32f;
        private const float DashFootLiftMeters = 0.24f;
        private const float DashKneePullMeters = 0.10f;
        private const float DashKneeBendDegrees = 38f;
        private const float DashBlendIn = 0.04f;
        private const float DashBlendOut = 0.10f;

        private const float HangDangleMeters = 0.07f;
        private const float HangKneeBendDegrees = 12f;
        private const float HangHipDropMeters = 0.02f;
        private const float HangToePitchDegrees = 12f;
        private const float HangBlendIn = 0.10f;
        private const float HangBlendOut = 0.16f;
        private const float HangCoastWeight = 0.4f;

        private enum Mode
        {
            Off,
            Dash,
            Boost,
        }

        [SerializeField] private Animator animator;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private DMDashController dash;
        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private DMClimbController climb;
        [SerializeField] private DMLandingDirector landing;
        [SerializeField] private SurvivalStats survival;
        [SerializeField] private vRagdoll ragdoll;

        private Transform _hips;
        private Transform _leftShin;
        private Transform _leftFoot;
        private Transform _rightShin;
        private Transform _rightFoot;

        private Mode _mode;
        private Mode _target;
        private float _weight;
        private Vector3 _dashDir = Vector3.forward;
        private bool _rightLead = true;
        private int _blendFrame = -1;
        private bool _bonesCached;

        private bool _dashSnap;
        private Vector3 _snapHipsPos;
        private Quaternion _snapHipsRot;
        private Vector3 _snapLShinPos;
        private Quaternion _snapLShinRot;
        private Vector3 _snapLFootPos;
        private Quaternion _snapLFootRot;
        private Vector3 _snapRShinPos;
        private Quaternion _snapRShinRot;
        private Vector3 _snapRFootPos;
        private Quaternion _snapRFootRot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
            if (player == null)
                return;

            Bind(player);
        }

        internal static DMHangLegOverlay Bind(GameObject player)
        {
            if (player == null)
                return null;

            DMHangLegOverlay overlay = player.GetComponent<DMHangLegOverlay>();
            if (overlay == null)
                overlay = player.AddComponent<DMHangLegOverlay>();
            overlay.EnsureRelay();
            return overlay;
        }

        private void Awake()
        {
            CacheRefs();
            EnsureRelay();
        }

        private void Start()
        {
            Debug.Log("DMHang 0830-pose2");
        }

        private void CacheRefs()
        {
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (dash == null)
                dash = GetComponent<DMDashController>();
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            if (climb == null)
                climb = GetComponent<DMClimbController>();
            if (landing == null)
                landing = GetComponent<DMLandingDirector>();
            if (survival == null)
                survival = GetComponent<SurvivalStats>() ?? GetComponentInParent<SurvivalStats>();
            if (ragdoll == null)
                ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            CacheBones();
        }

        private void CacheBones()
        {
            if (animator == null || !animator.isHuman)
                return;

            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            _leftShin = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            _leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightShin = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            _rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            _bonesCached = _hips != null && _leftFoot != null && _rightFoot != null;
        }

        internal void EnsureRelay()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (animator == null)
                return;

            DMHangLegIKRelay relay = animator.GetComponent<DMHangLegIKRelay>();
            if (relay == null)
                relay = animator.gameObject.AddComponent<DMHangLegIKRelay>();
            relay.owner = this;
        }

        private void LateUpdate()
        {
            TickBlend();
            if (_weight <= 0.001f)
            {
                ClearDashSnapshot();
                return;
            }

            // Always pose in LateUpdate when weighted. Do not skip because IK ran —
            // dash freezes animator.speed at 0 and Invector IK would hide the lift.
            ApplyBonePose();
        }

        public void ApplyAnimatorIK(int layerIndex)
        {
            if (animator == null)
                return;

            TickBlend();
            if (_weight <= 0.001f)
                return;

            // Never pose with IK. Zero foot-plant so Invector cannot overwrite the overlay.
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
        }

        private void TickBlend()
        {
            if (_blendFrame == Time.frameCount)
                return;
            _blendFrame = Time.frameCount;
            ResolveTarget();
            BlendWeight();
        }

        private bool IsBlocked()
        {
            if (motor != null && (motor.ragdolled || motor.isDead))
                return true;
            if (ragdoll != null && ragdoll.isActive)
                return true;
            if (survival != null && survival.IsDead)
                return true;
            if (climb != null && climb.IsClimbing)
                return true;
            if (landing != null && landing.IsLandingLocked)
                return true;
            if (animator == null || !animator.enabled || !animator.isHuman)
                return true;
            return false;
        }

        private void ResolveTarget()
        {
            if (IsBlocked())
            {
                _target = Mode.Off;
                return;
            }

            if (dash != null && dash.IsDashing)
            {
                _target = Mode.Dash;
                _dashDir = dash.DashDirection;
                if (_dashDir.sqrMagnitude < 0.001f)
                    _dashDir = transform.forward;
                float side = Vector3.Dot(_dashDir, transform.right);
                _rightLead = side >= -0.15f;
                return;
            }

            if (jetpack != null && jetpack.IsJetpackAnimActive)
            {
                _target = Mode.Boost;
                return;
            }

            _target = Mode.Off;
        }

        private void BlendWeight()
        {
            float dt = Time.unscaledDeltaTime;
            if (_target == Mode.Off)
            {
                float outTime = _mode == Mode.Dash ? DashBlendOut : HangBlendOut;
                _weight = Mathf.MoveTowards(_weight, 0f, dt / Mathf.Max(0.02f, outTime));
                if (_weight <= 0.001f)
                {
                    _mode = Mode.Off;
                    ClearDashSnapshot();
                }

                return;
            }

            if (_target != Mode.Dash && _mode == Mode.Dash)
                ClearDashSnapshot();

            _mode = _target;
            float goal = 1f;
            float inTime = DashBlendIn;
            if (_mode == Mode.Boost)
            {
                bool thrusting = jetpack != null && jetpack.IsBoostingNow;
                goal = thrusting ? 1f : HangCoastWeight;
                inTime = HangBlendIn;
            }

            _weight = Mathf.MoveTowards(_weight, goal, dt / Mathf.Max(0.02f, inTime));
        }

        private void ApplyBonePose()
        {
            if (!_bonesCached)
                CacheBones();
            if (!_bonesCached)
                return;

            if (_mode == Mode.Dash)
                ApplyDashPose(_weight);
            else
                ApplyHangPose(_weight);
        }

        private void CaptureDashSnapshot()
        {
            if (_dashSnap)
                return;
            if (_hips == null || _leftShin == null || _rightShin == null || _leftFoot == null || _rightFoot == null)
                return;

            _snapHipsPos = _hips.localPosition;
            _snapHipsRot = _hips.localRotation;
            _snapLShinPos = _leftShin.localPosition;
            _snapLShinRot = _leftShin.localRotation;
            _snapLFootPos = _leftFoot.localPosition;
            _snapLFootRot = _leftFoot.localRotation;
            _snapRShinPos = _rightShin.localPosition;
            _snapRShinRot = _rightShin.localRotation;
            _snapRFootPos = _rightFoot.localPosition;
            _snapRFootRot = _rightFoot.localRotation;
            _dashSnap = true;
        }

        private void RestoreDashSnapshot()
        {
            if (!_dashSnap)
                return;

            _hips.localPosition = _snapHipsPos;
            _hips.localRotation = _snapHipsRot;
            _leftShin.localPosition = _snapLShinPos;
            _leftShin.localRotation = _snapLShinRot;
            _leftFoot.localPosition = _snapLFootPos;
            _leftFoot.localRotation = _snapLFootRot;
            _rightShin.localPosition = _snapRShinPos;
            _rightShin.localRotation = _snapRShinRot;
            _rightFoot.localPosition = _snapRFootPos;
            _rightFoot.localRotation = _snapRFootRot;
        }

        private void ClearDashSnapshot()
        {
            _dashSnap = false;
        }

        private void ApplyDashPose(float w)
        {
            // animator.speed is 0 during dash — snapshot once, restore, then offset.
            CaptureDashSnapshot();
            RestoreDashSnapshot();

            Vector3 up = Vector3.up;
            Vector3 right = transform.right;
            if (_hips != null)
                _hips.position += up * (DashLiftMeters * w);

            Transform shin = _rightLead ? _rightShin : _leftShin;
            Transform foot = _rightLead ? _rightFoot : _leftFoot;
            if (shin != null)
                shin.Rotate(right, DashKneeBendDegrees * w, Space.World);

            if (foot != null)
            {
                foot.position += up * (DashFootLiftMeters * w);
                Vector3 hips = _hips != null ? _hips.position : transform.position;
                Vector3 pull = hips - foot.position;
                pull.y = 0f;
                if (pull.sqrMagnitude > 0.0001f)
                    foot.position += pull.normalized * (DashKneePullMeters * w);
            }
        }

        private void ApplyHangPose(float w)
        {
            // Fly clip is playing (animator.speed != 0). Small additive, no snapshot restore.
            Vector3 right = transform.right;
            if (_hips != null)
                _hips.position += Vector3.down * (HangHipDropMeters * w);

            PoseHangLeg(_leftShin, _leftFoot, w, right);
            PoseHangLeg(_rightShin, _rightFoot, w, right);
        }

        private void PoseHangLeg(Transform shin, Transform foot, float w, Vector3 right)
        {
            if (shin != null)
                shin.Rotate(right, HangKneeBendDegrees * w, Space.World);

            if (foot == null)
                return;

            foot.position += Vector3.down * (HangDangleMeters * w);
            foot.Rotate(right, HangToePitchDegrees * w, Space.World);
        }
    }
}
