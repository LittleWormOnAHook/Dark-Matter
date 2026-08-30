using Invector.vCharacterController;
using Project.Features.Jetpack;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Every air-to-ground land plays Jetpack Land (hero).
    /// Invector LandHigh / Landing / get-up is suppressed until that clip ends.
    /// Runs after Invector so VerticalVelocity is zero before the Animator evaluates.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class DMLandingDirector : MonoBehaviour
    {
        private const float HeroLandSpeed = 2f;
        private const float ApproachMeters = 1.6f;

        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private DMJetpackController jetpack;

        private bool _landing;
        private bool _enteredHeroState;
        private bool _heldLockMovement;
        private bool _heldLockAnimMovement;
        private bool _heldBlockFallDamage;
        private float _savedAnimatorSpeed = 1f;
        private float _clipEndsAt = -1f;
        private float _airApexY;
        private float _airVerticalVelocity;
        private bool _wasGrounded = true;

        private bool _hasVerticalVelocity;
        private bool _hasJetpackLand;
        private bool _hasLandHigh;
        private bool _hasIsGrounded;

        private static readonly int VerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int JetpackLand = Animator.StringToHash("JetpackLand");
        private static readonly int LandHighTrigger = Animator.StringToHash("LandHigh");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int JetpackLandState = Animator.StringToHash("Jetpack Land");
        private static readonly int Locomotion = Animator.StringToHash("Locomotion");
        private static readonly int[] InvectorLandStates =
        {
            Animator.StringToHash("LandHigh"),
            Animator.StringToHash("LandLow"),
            Animator.StringToHash("Landing"),
            Animator.StringToHash("Falling"),
            Animator.StringToHash("StandUpFromBelly"),
            Animator.StringToHash("StandUpFromBack"),
            Animator.StringToHash("StandUp@FromBelly"),
            Animator.StringToHash("StandUp@FromBack"),
            Animator.StringToHash("GetUpFromBelly"),
            Animator.StringToHash("GetUpFromBack"),
            Animator.StringToHash("Roll"),
        };

        public bool IsLandingLocked => _landing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
            if (player == null || player.GetComponent<DMLandingDirector>() != null)
                return;

            player.AddComponent<DMLandingDirector>();
        }

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            CacheAnimatorParameters();
        }

        private void OnDisable()
        {
            EndLanding(restoreLocks: true);
        }

        private void CacheAnimatorParameters()
        {
            _hasVerticalVelocity = false;
            _hasJetpackLand = false;
            _hasLandHigh = false;
            _hasIsGrounded = false;
            if (animator == null)
                return;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                int hash = animator.GetParameter(i).nameHash;
                if (hash == VerticalVelocity)
                    _hasVerticalVelocity = true;
                else if (hash == JetpackLand)
                    _hasJetpackLand = true;
                else if (hash == LandHighTrigger)
                    _hasLandHigh = true;
                else if (hash == IsGrounded)
                    _hasIsGrounded = true;
            }
        }

        private void Update()
        {
            if (motor == null || animator == null)
                return;

            bool grounded = motor.isGrounded;
            if (!grounded)
            {
                _airVerticalVelocity = motor.verticalVelocity;
                float y = transform.position.y;
                if (_wasGrounded)
                    _airApexY = y;
                else if (y > _airApexY)
                    _airApexY = y;
            }

            if (grounded && !_wasGrounded && !_landing)
                BeginLanding(_airApexY - transform.position.y, _airVerticalVelocity);

            _wasGrounded = grounded;

            bool approaching = !grounded && motor.groundDistance <= ApproachMeters;
            if (_landing || approaching)
                SuppressInvectorLand();

            if (!_landing)
                return;

            ApplyLock();
            SuppressInvectorLand();
            KickInvectorLandStates();
            TickHero();
        }

        private void BeginLanding(float dropMeters, float airVelocity)
        {
            if (dropMeters < 0.2f && airVelocity > -2f)
                return;

            if (!_hasJetpackLand)
                return;

            _landing = true;
            _enteredHeroState = false;
            if (motor != null)
            {
                _heldLockMovement = motor.lockMovement;
                _heldLockAnimMovement = motor.lockAnimMovement;
                _heldBlockFallDamage = motor.blockApplyFallDamage;
                motor.blockApplyFallDamage = true;
            }

            _savedAnimatorSpeed = animator.speed;
            animator.speed = HeroLandSpeed;
            ApplyLock();
            SuppressInvectorLand();
            if (_hasLandHigh)
                animator.ResetTrigger(LandHighTrigger);
            if (_hasJetpackLand)
                animator.SetTrigger(JetpackLand);
            if (animator.HasState(0, JetpackLandState))
                animator.CrossFadeInFixedTime(JetpackLandState, 0.05f, 0);
            _clipEndsAt = Time.unscaledTime + 1.25f;
        }

        private void TickHero()
        {
            bool inState = StateMatches(JetpackLandState);
            if (inState)
                _enteredHeroState = true;
            else if (_enteredHeroState)
            {
                EndLanding(restoreLocks: true);
                return;
            }

            if (_clipEndsAt > 0f && Time.unscaledTime >= _clipEndsAt)
                EndLanding(restoreLocks: true);
        }

        private void KickInvectorLandStates()
        {
            if (animator == null)
                return;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0)
                : current;

            if (IsInvectorLandHash(current.shortNameHash) || IsInvectorLandHash(next.shortNameHash))
            {
                if (animator.HasState(0, JetpackLandState))
                    animator.CrossFadeInFixedTime(JetpackLandState, 0.04f, 0);
            }
        }

        private static bool IsInvectorLandHash(int hash)
        {
            for (int i = 0; i < InvectorLandStates.Length; i++)
            {
                if (InvectorLandStates[i] == hash)
                    return true;
            }

            return false;
        }

        private bool StateMatches(int hash)
        {
            if (animator == null)
                return false;

            if (animator.IsInTransition(0) &&
                animator.GetNextAnimatorStateInfo(0).shortNameHash == hash)
                return true;

            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash;
        }

        private void ApplyLock()
        {
            if (motor == null)
                return;

            motor.lockMovement = true;
            motor.lockAnimMovement = true;
            motor.input = Vector3.zero;
            motor.inputMagnitude = 0f;
            motor.isJumping = false;
            motor.verticalVelocity = 0f;
        }

        private void SuppressInvectorLand()
        {
            if (animator == null)
                return;

            if (motor != null)
                motor.verticalVelocity = 0f;

            if (_hasVerticalVelocity)
                animator.SetFloat(VerticalVelocity, 0f);
            if (_hasLandHigh)
                animator.ResetTrigger(LandHighTrigger);
            if (_hasIsGrounded && (_landing || motor != null && motor.isGrounded))
                animator.SetBool(IsGrounded, true);
        }

        private void EndLanding(bool restoreLocks)
        {
            if (!_landing)
                return;

            _landing = false;
            _enteredHeroState = false;
            _clipEndsAt = -1f;
            SuppressInvectorLand();

            if (jetpack != null)
                jetpack.NotifyLanded();

            if (animator != null)
            {
                animator.speed = _savedAnimatorSpeed;
                if (animator.HasState(0, Locomotion))
                    animator.CrossFadeInFixedTime(Locomotion, 0.12f, 0);
            }

            if (restoreLocks && motor != null)
            {
                motor.lockMovement = _heldLockMovement;
                motor.lockAnimMovement = _heldLockAnimMovement;
                motor.blockApplyFallDamage = _heldBlockFallDamage;
            }
        }
    }
}
