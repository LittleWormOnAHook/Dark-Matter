using Invector.vCharacterController;
using UnityEngine;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// Drives the Jumps.fbx fly blend tree after jetpack ignition.
    /// Landing (regular / Jetpack Land hero / high roll or get-up) is owned by DMLandingDirector.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class DMJetpackAnimatorDriver : MonoBehaviour
    {
        private static readonly int JetpackActive = Animator.StringToHash("JetpackActive");
        private static readonly int JetpackHorizontal = Animator.StringToHash("JetpackHorizontal");
        private static readonly int JetpackVertical = Animator.StringToHash("JetpackVertical");

        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private DMJetpackInputBridge inputBridge;
        [SerializeField] private DMJetpackProfile profile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;
        private Project.Player.DMLandingDirector _landing;

        private float _smoothHorizontal;
        private float _smoothVertical;
        private float _horizontalVelocity;
        private float _verticalVelocity;

        private bool _hasJetpackActiveParam;
        private bool _hasJetpackHorizontalParam;
        private bool _hasJetpackVerticalParam;

        public bool IsBoostLanding => false;
        public bool IsHighFallRecovering => false;
        public bool IsLandingLocked => false;

        private void Reset()
        {
            jetpack = GetComponent<DMJetpackController>();
            inputBridge = GetComponent<DMJetpackInputBridge>();
            motor = GetComponent<vThirdPersonMotor>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            if (inputBridge == null)
                inputBridge = GetComponent<DMJetpackInputBridge>();
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator != null && !animator.enabled)
                animator.enabled = true;

            CacheAnimatorParameters();
            _landing = GetComponent<Project.Player.DMLandingDirector>();
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null)
                return;

            _hasJetpackActiveParam = HasParameter(JetpackActive);
            _hasJetpackHorizontalParam = HasParameter(JetpackHorizontal);
            _hasJetpackVerticalParam = HasParameter(JetpackVertical);
        }

        private void FixedUpdate()
        {
            if (animator == null || jetpack == null || motor == null)
                return;

            if (_landing == null)
                _landing = GetComponent<Project.Player.DMLandingDirector>();
            bool landingLocked = _landing != null && _landing.IsLandingLocked;

            bool animActive = jetpack.IsJetpackAnimActive && !landingLocked;

            if (animActive)
            {
                Vector2 move = inputBridge != null ? inputBridge.LocalMoveInput : Vector2.zero;
                float deadzone = profile != null ? profile.animInputDeadzone : 0.08f;
                float gain = profile != null ? profile.animBlendGain : 1.35f;
                move = DMJetpackMoveInput.ApplyDeadzoneAndGain(move, deadzone, gain);

                float smoothTime = profile != null ? profile.jetpackMoveSmoothTime : 0.22f;
                _smoothHorizontal = Mathf.SmoothDamp(
                    _smoothHorizontal, move.x, ref _horizontalVelocity, smoothTime);
                _smoothVertical = Mathf.SmoothDamp(
                    _smoothVertical, move.y, ref _verticalVelocity, smoothTime);

                float leanStrength = profile != null ? profile.animLeanStrength : 0.1f;

                if (_hasJetpackHorizontalParam)
                    animator.SetFloat(JetpackHorizontal, _smoothHorizontal * leanStrength);
                if (_hasJetpackVerticalParam)
                    animator.SetFloat(JetpackVertical, _smoothVertical * leanStrength);
            }
            else
            {
                _smoothHorizontal = 0f;
                _smoothVertical = 0f;
                _horizontalVelocity = 0f;
                _verticalVelocity = 0f;

                if (_hasJetpackHorizontalParam)
                    animator.SetFloat(JetpackHorizontal, 0f);
                if (_hasJetpackVerticalParam)
                    animator.SetFloat(JetpackVertical, 0f);
            }

            if (_hasJetpackActiveParam)
                animator.SetBool(JetpackActive, animActive);
        }

        private bool HasParameter(int hash)
        {
            if (animator == null)
                return false;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                if (animator.GetParameter(i).nameHash == hash)
                    return true;
            }

            return false;
        }
    }
}