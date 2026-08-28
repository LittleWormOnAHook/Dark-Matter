using Invector.vCharacterController;
using UnityEngine;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// Drives the Jumps.fbx fly blend tree after jetpack ignition.
    /// Jump animation plays until boost ignites; JetpackActive then crossfades into flight.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class DMJetpackAnimatorDriver : MonoBehaviour
    {
        private static readonly int JetpackActive = Animator.StringToHash("JetpackActive");
        private static readonly int JetpackHorizontal = Animator.StringToHash("JetpackHorizontal");
        private static readonly int JetpackVertical = Animator.StringToHash("JetpackVertical");
        private static readonly int JetpackLand = Animator.StringToHash("JetpackLand");
        private static readonly int JetpackLandState = Animator.StringToHash("Jetpack Land");

        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private DMJetpackInputBridge inputBridge;
        [SerializeField] private DMJetpackProfile profile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;

        private float _smoothHorizontal;
        private float _smoothVertical;
        private float _horizontalVelocity;
        private float _verticalVelocity;
        private bool _wasGrounded = true;

        private bool _hasJetpackActiveParam;
        private bool _hasJetpackHorizontalParam;
        private bool _hasJetpackVerticalParam;
        private bool _hasJetpackLandParam;

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
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null)
                return;

            _hasJetpackActiveParam = HasParameter(JetpackActive);
            _hasJetpackHorizontalParam = HasParameter(JetpackHorizontal);
            _hasJetpackVerticalParam = HasParameter(JetpackVertical);
            _hasJetpackLandParam = HasParameter(JetpackLand);
        }

        private void FixedUpdate()
        {
            if (animator == null || jetpack == null || motor == null)
                return;

            bool grounded = motor.isGrounded;

            if (grounded && !_wasGrounded && jetpack.ShouldPlayJetpackLand)
            {
                if (_hasJetpackLandParam)
                    animator.SetTrigger(JetpackLand);

                jetpack.NotifyLanded();
            }

            _wasGrounded = grounded;

            bool animActive = jetpack.IsJetpackAnimActive;

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

                if (_hasJetpackHorizontalParam)
                    animator.SetFloat(JetpackHorizontal, _smoothHorizontal);
                if (_hasJetpackVerticalParam)
                    animator.SetFloat(JetpackVertical, _smoothVertical);
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

        private bool IsInJetpackLandState()
        {
            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == JetpackLandState)
                    return true;
            }

            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == JetpackLandState;
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
