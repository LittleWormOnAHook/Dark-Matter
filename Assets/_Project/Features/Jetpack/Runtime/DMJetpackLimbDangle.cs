using Invector.vCharacterController;
using Project.Features.Climb;
using Project.Features.Dash;
using Project.Player;
using Project.Survival;
using UnityEngine;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// After the animator pose, add springy limb lag from gravity and acceleration.
    /// Visual only — does not enable ragdoll or retune Player_v7.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(3300)]
    public sealed class DMJetpackLimbDangle : MonoBehaviour
    {
        private const float BlendIn = 0.14f;
        private const float BlendOut = 0.18f;
        private const float CoastWeight = 0.78f;
        private const float JumpAirWeight = 0.58f;
        private const float AccelSmooth = 0.08f;
        private const float OffsetSmooth = 0.14f;
        private const float MaxAccel = 22f;
        private const float HangStrength = 11f;
        private const float InertiaStrength = 0.85f;

        [SerializeField] private Animator animator;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Rigidbody body;
        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private DMDashController dash;
        [SerializeField] private DMClimbController climb;
        [SerializeField] private DMLandingDirector landing;
        [SerializeField] private SurvivalStats survival;
        [SerializeField] private vRagdoll ragdoll;

        private readonly Limb[] _limbs = new Limb[10];
        private Vector3 _lastVelocity;
        private Vector3 _smoothAccel;
        private Vector3 _accelVelocity;
        private bool _hasVelocity;
        private float _weight;
        private bool _bonesReady;

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

        internal static DMJetpackLimbDangle Bind(GameObject player)
        {
            if (player == null)
                return null;

            DMJetpackLimbDangle dangle = player.GetComponent<DMJetpackLimbDangle>();
            if (dangle == null)
                dangle = player.AddComponent<DMJetpackLimbDangle>();
            dangle.CacheRefs();
            return dangle;
        }

        private void Awake()
        {
            CacheRefs();
        }

        private void CacheRefs()
        {
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            if (dash == null)
                dash = GetComponent<DMDashController>();
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

            _limbs[0] = Limb.Create(animator, HumanBodyBones.LeftUpperLeg, 20f, 0.95f);
            _limbs[1] = Limb.Create(animator, HumanBodyBones.RightUpperLeg, 20f, 0.95f);
            _limbs[2] = Limb.Create(animator, HumanBodyBones.LeftLowerLeg, 28f, 1.25f);
            _limbs[3] = Limb.Create(animator, HumanBodyBones.RightLowerLeg, 28f, 1.25f);
            _limbs[4] = Limb.Create(animator, HumanBodyBones.LeftFoot, 18f, 1.4f);
            _limbs[5] = Limb.Create(animator, HumanBodyBones.RightFoot, 18f, 1.4f);
            _limbs[6] = Limb.Create(animator, HumanBodyBones.LeftUpperArm, 28f, 1.1f);
            _limbs[7] = Limb.Create(animator, HumanBodyBones.RightUpperArm, 28f, 1.1f);
            _limbs[8] = Limb.Create(animator, HumanBodyBones.LeftLowerArm, 36f, 1.45f);
            _limbs[9] = Limb.Create(animator, HumanBodyBones.RightLowerArm, 36f, 1.45f);
            _bonesReady = true;
            for (int i = 0; i < _limbs.Length; i++)
            {
                if (_limbs[i].Bone == null)
                    _bonesReady = false;
            }
        }

        internal void ResetInertia()
        {
            _hasVelocity = false;
            _smoothAccel = Vector3.zero;
            _accelVelocity = Vector3.zero;
            _weight = 0f;
        }

        private void LateUpdate()
        {
            float goal = ResolveWeight();
            float dt = Time.unscaledDeltaTime;
            float blend = goal > _weight ? BlendIn : BlendOut;
            _weight = Mathf.MoveTowards(_weight, goal, dt / Mathf.Max(0.02f, blend));
            if (_weight <= 0.001f)
            {
                _hasVelocity = false;
                _smoothAccel = Vector3.zero;
                return;
            }

            if (!_bonesReady)
                CacheBones();
            if (!_bonesReady)
                return;

            UpdateAccel(dt);
            ApplyDangle(dt);
        }

        private float ResolveWeight()
        {
            if (IsBlocked())
                return 0f;

            if (dash != null && dash.IsDashing)
                return 0f;

            if (motor == null || motor.isGrounded)
                return 0f;

            if (jetpack != null && jetpack.IsJetpackAnimActive)
                return jetpack.IsBoostingNow ? 1f : CoastWeight;

            return JumpAirWeight;
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

        private void UpdateAccel(float dt)
        {
            if (body == null)
                return;

            Vector3 velocity = body.linearVelocity;
            if (!_hasVelocity)
            {
                _lastVelocity = velocity;
                _hasVelocity = true;
                return;
            }

            Vector3 raw = (velocity - _lastVelocity) / Mathf.Max(dt, 0.0001f);
            _lastVelocity = velocity;
            raw = Vector3.ClampMagnitude(raw, MaxAccel);
            _smoothAccel = Vector3.SmoothDamp(_smoothAccel, raw, ref _accelVelocity, AccelSmooth, Mathf.Infinity, dt);
        }

        private void ApplyDangle(float dt)
        {
            Vector3 inertia = (-_smoothAccel * InertiaStrength) + (Vector3.down * HangStrength);
            for (int i = 0; i < _limbs.Length; i++)
                _limbs[i].Apply(inertia, _weight, dt);
        }

        private struct Limb
        {
            public Transform Bone;
            public float MaxDegrees;
            public float Whip;
            private Vector3 _offset;
            private Vector3 _offsetVelocity;

            public static Limb Create(Animator animator, HumanBodyBones id, float maxDegrees, float whip)
            {
                return new Limb
                {
                    Bone = animator.GetBoneTransform(id),
                    MaxDegrees = maxDegrees,
                    Whip = whip
                };
            }

            public void Apply(Vector3 worldInertia, float weight, float dt)
            {
                if (Bone == null)
                    return;

                Vector3 local = Bone.InverseTransformDirection(worldInertia);
                Vector3 target = new Vector3(
                    Mathf.Clamp(-local.y * Whip, -MaxDegrees, MaxDegrees),
                    Mathf.Clamp(local.x * Whip * 0.45f, -MaxDegrees * 0.45f, MaxDegrees * 0.45f),
                    Mathf.Clamp(local.z * Whip, -MaxDegrees, MaxDegrees));

                _offset = Vector3.SmoothDamp(_offset, target, ref _offsetVelocity, OffsetSmooth, Mathf.Infinity, dt);
                if (_offset.sqrMagnitude < 0.0001f)
                    return;

                Bone.localRotation *= Quaternion.Euler(_offset * weight);
            }
        }
    }
}
