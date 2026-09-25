using Invector.vCharacterController;

using Project.Core;

using Project.Features.Climb;

using Project.Player;

using Project.Player.Invector;

using Project.Survival;

using UnityEngine;

using UnityEngine.InputSystem;



namespace Project.Features.Locomotion

{

    /// <summary>

    /// Three on-foot gaits for Invector:

    /// default slow walk (half prior jog speed), Shift hold jog, double-tap Shift hold sprint burst (+20% run).

    /// Tunables live on <see cref="DM_ClimbDashProfile"/> (Genesis Studio → Survival → Walk / Run / Sprint).

    /// </summary>

    [DisallowMultipleComponent]

    [DefaultExecutionOrder(510)]

    public sealed class DMLocomotionGaitController : MonoBehaviour

    {

        public enum Gait

        {

            SlowWalk = 0,

            Jog = 1,

            SprintBurst = 2

        }



        [SerializeField] private vThirdPersonMotor motor;



        private SurvivalStats _survival;

        private PioneerShooterMeleeInput _shooterInput;

        private float _baselineWalkSpeed;

        private float _baselineJogSpeed;

        private float _baselineSprintSpeed;

        private float _lastShiftPressAt = -10f;

        private int _shiftTapCount;

        private Gait _appliedGait = (Gait)(-1);

        private float _profileSlowMul = float.NaN;

        private float _profileJogMul = float.NaN;

        private float _profileBurstMul = float.NaN;

        private float _nextProfilePollUnscaled;

        private bool _gamepadAutoRunLatched;

        private bool _gamepadAutoCrouchLatched;

        public const string AutoRunLatchStamp = "controller-autorun-l3 0920";

        public const string AutoCrouchLatchStamp = "controller-autocrouch-r3 0920";

        // stamp: controller-compile-fix 0920 — Crouch lives on vThirdPersonController, not Motor.

        public Gait CurrentGait { get; private set; } = Gait.SlowWalk;

        public bool IsJogging => CurrentGait == Gait.Jog;

        public bool IsBurstSprinting => CurrentGait == Gait.SprintBurst;

        public bool IsGamepadAutoRunLatched => _gamepadAutoRunLatched;

        public bool IsGamepadAutoCrouchLatched => _gamepadAutoCrouchLatched;

        /// <summary>L3 / Sprint action toggle while on Gamepad scheme (jog latch, not hold).</summary>
        public void ToggleGamepadAutoRunLatch()
        {
            _gamepadAutoRunLatched = !_gamepadAutoRunLatched;
            Debug.Log(
                $"[DMLocomotionGaitController] {AutoRunLatchStamp} auto-run {(_gamepadAutoRunLatched ? "ON" : "OFF")}");

            if (motor == null)
                return;

            bool speedsChanged = RefreshSpeedsFromProfile();
            ApplyGaitFromInput(EffectiveShiftHeld(), false, speedsChanged);
        }

        public void ClearGamepadAutoRunLatch()
        {
            if (!_gamepadAutoRunLatched)
                return;

            _gamepadAutoRunLatched = false;
            if (motor == null)
                return;

            bool speedsChanged = RefreshSpeedsFromProfile();
            ApplyGaitFromInput(ReadKeyboardShiftHeld(), false, speedsChanged);
        }

        /// <summary>R3 / Crouch action toggle while on Gamepad scheme (crouch latch, not hold).</summary>
        public void ToggleGamepadAutoCrouchLatch()
        {
            _gamepadAutoCrouchLatched = !_gamepadAutoCrouchLatched;
            Debug.Log(
                $"[DMLocomotionGaitController] {AutoCrouchLatchStamp} auto-crouch {(_gamepadAutoCrouchLatched ? "ON" : "OFF")}");

            ApplyGamepadAutoCrouchLatchState();
        }

        public void ClearGamepadAutoCrouchLatch()
        {
            if (!_gamepadAutoCrouchLatched)
                return;

            _gamepadAutoCrouchLatched = false;
            ApplyGamepadAutoCrouchLatchState();
        }

        public void ClearGamepadLocomotionLatches()
        {
            ClearGamepadAutoRunLatch();
            ClearGamepadAutoCrouchLatch();
        }

        /// <summary>

        /// Stamina drain scale while burst sprinting — matches live speed vs authored sprint speed (e.g. 1.2× speed → 1.2× drain).

        /// </summary>

        public float SprintStaminaDrainMultiplier =>

            CurrentGait == Gait.SprintBurst && _baselineSprintSpeed > 0.01f

                ? (_baselineSprintSpeed * CurrentBurstMultiplier()) / _baselineSprintSpeed

                : 1f;



        private DM_ClimbDashProfile LiveProfile => DM_ClimbDashProfile.Live;



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

        private static void EnsureOnPlayer()

        {

            if (!Application.isPlaying)

                return;



            GameObject player = PlayerLocator.FindPlayerObject();

            if (player == null)

                player = GameObject.Find("Player_v7 Variant");

            if (player == null)

                player = GameObject.Find("Player_v7");

            if (player == null || player.GetComponent<DMLocomotionGaitController>() != null)

                return;



            player.AddComponent<DMLocomotionGaitController>();

        }



        private void Awake()

        {

            if (motor == null)

                motor = GetComponent<vThirdPersonMotor>();

            if (_survival == null)

                _survival = GetComponent<SurvivalStats>();

            if (_shooterInput == null)

                _shooterInput = GetComponent<PioneerShooterMeleeInput>();



            CacheBaselineSpeeds();

            motor.useContinuousSprint = false;

            // SurvivalStats owns sprint drain — Invector CheckStamina every FixedUpdate is wasted CPU.

            motor.sprintStamina = 0f;



            if (_shooterInput != null)

                _shooterInput.sprintInput.useInput = false;



            ApplySlowWalk();

            _appliedGait = Gait.SlowWalk;

        }



        /// <summary>Called from <see cref="PioneerShooterMeleeInput"/> once per frame after Invector input.</summary>

        public void TickLocomotion()

        {

            if (GameplayWorldSimulation.IsFrozen || motor == null || !isActiveAndEnabled)

                return;

            EnforceGamepadAutoCrouchLatch();

            bool shiftHeld = EffectiveShiftHeld();

            bool shiftPressed = ReadKeyboardShiftPressedThisFrame();



            if (!shiftPressed

                && shiftHeld == _lastShiftHeld

                && !shiftHeld

                && _shiftTapCount == 0

                && _appliedGait == Gait.SlowWalk)

            {

                if (Time.unscaledTime >= _nextProfilePollUnscaled)

                {

                    _nextProfilePollUnscaled = Time.unscaledTime + 0.5f;

                    if (RefreshSpeedsFromProfile())

                        ApplyGaitFromInput(false, false, true);

                }

                return;

            }



            _lastShiftHeld = shiftHeld;

            bool speedsChanged = RefreshSpeedsFromProfile();

            ApplyGaitFromInput(shiftHeld, shiftPressed, speedsChanged);
            ReassertMotorGaitFlags();
        }



        /// <summary>Keep Invector sprint bool aligned with burst-only gait (jog must never use sprint animator path).</summary>

        public void EnforceMotorSprintState()

        {

            if (motor == null)

                return;



            bool wantSprint = CurrentGait == Gait.SprintBurst && motor.input.sqrMagnitude > 0.01f;

            if (motor.isSprinting != wantSprint)

                motor.isSprinting = wantSprint;

        }



        private void OnDisable()

        {

            if (motor == null)

                return;



            RestoreBaselineSpeeds();

            motor.useContinuousSprint = true;

            motor.alwaysWalkByDefault = false;

            motor.isSprinting = false;

            motor.freeSpeed.walkByDefault = false;

            motor.strafeSpeed.walkByDefault = false;

            motor.speedMultiplier = 1f;

            _shiftTapCount = 0;

            _appliedGait = (Gait)(-1);



            if (_shooterInput != null)

                _shooterInput.sprintInput.useInput = true;

        }



        public void ApplyGaitFromInput(bool shiftHeld, bool shiftPressedThisFrame, bool speedsChanged = false)

        {

            if (motor == null)

                return;



            float doubleTapWindow = LiveProfile != null ? LiveProfile.shiftDoubleTapWindow : 0.28f;



            if (shiftPressedThisFrame)

            {

                if (Time.unscaledTime - _lastShiftPressAt <= doubleTapWindow)

                    _shiftTapCount = Mathf.Min(_shiftTapCount + 1, 2);

                else

                    _shiftTapCount = 1;



                _lastShiftPressAt = Time.unscaledTime;

            }



            Gait desiredGait;

            if (!shiftHeld)

            {

                if (Time.unscaledTime - _lastShiftPressAt > doubleTapWindow)

                    _shiftTapCount = 0;



                desiredGait = Gait.SlowWalk;

            }

            else if (_shiftTapCount >= 2 && CanBurstSprint())

            {

                desiredGait = Gait.SprintBurst;

            }

            else

            {

                desiredGait = Gait.Jog;

            }



            if (!speedsChanged && desiredGait == _appliedGait)
            {
                // Invector onlyWalkWhenAiming re-sets walkByDefault every Update after we ApplyJog once;
                // reassert so Shift-run stays while armed / ADS (stamp: armed-shift-run 0920).
                ReassertMotorGaitFlags();
                return;
            }



            switch (desiredGait)

            {

                case Gait.SprintBurst:

                    ApplySprintBurst();

                    break;

                case Gait.Jog:

                    ApplyJog();

                    break;

                default:

                    ApplySlowWalk();

                    break;

            }



            _appliedGait = desiredGait;

            EnforceMotorSprintState();

        }



        /// <summary>Keyboard Shift hold or latched gamepad auto-run (jog).</summary>
        public bool ComputeShiftHeldForLocomotion()
        {
            if (ReadKeyboardShiftHeld())
                return true;

            return _gamepadAutoRunLatched && DMInputSchemeRouter.IsGamepadScheme;
        }

        private bool EffectiveShiftHeld() => ComputeShiftHeldForLocomotion();

        private vThirdPersonController ResolveController()
        {
            return motor as vThirdPersonController
                   ?? GetComponent<vThirdPersonController>();
        }

        private void EnforceGamepadAutoCrouchLatch()
        {
            if (!_gamepadAutoCrouchLatched || !DMInputSchemeRouter.IsGamepadScheme)
                return;

            vThirdPersonController cc = ResolveController();
            if (cc == null)
                return;

            // Invector Crouch() is on vThirdPersonController (not Motor).
            if (!cc.isCrouching && cc.isGrounded && !cc.customAction)
                cc.Crouch();
        }

        private void ApplyGamepadAutoCrouchLatchState()
        {
            vThirdPersonController cc = ResolveController();
            if (cc == null)
                return;

            if (_gamepadAutoCrouchLatched)
            {
                if (!cc.isCrouching && cc.isGrounded && !cc.customAction)
                    cc.Crouch();
                return;
            }

            if (cc.isCrouching)
                cc.Crouch();
        }

        public static bool ReadKeyboardShiftHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                   && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        public static bool ReadKeyboardShiftPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                   && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame);
        }

        private float CurrentSlowMultiplier() =>

            LiveProfile != null ? LiveProfile.slowWalkSpeedMultiplier : 0.5f;



        private float CurrentJogMultiplier() =>

            LiveProfile != null ? LiveProfile.jogSpeedMultiplier : 1f;



        private float CurrentBurstMultiplier() =>

            LiveProfile != null ? LiveProfile.sprintBurstSpeedMultiplier : 1.2f;



        private bool RefreshSpeedsFromProfile()

        {

            float slowMul = CurrentSlowMultiplier();

            float jogMul = CurrentJogMultiplier();

            float burstMul = CurrentBurstMultiplier();



            if (Mathf.Approximately(slowMul, _profileSlowMul)

                && Mathf.Approximately(jogMul, _profileJogMul)

                && Mathf.Approximately(burstMul, _profileBurstMul))

            {

                return false;

            }



            _profileSlowMul = slowMul;

            _profileJogMul = jogMul;

            _profileBurstMul = burstMul;

            ApplySpeedsFromProfile();

            _appliedGait = (Gait)(-1);

            return true;

        }



        private void CacheBaselineSpeeds()

        {

            _baselineWalkSpeed = motor.freeSpeed.walkSpeed;

            _baselineJogSpeed = motor.freeSpeed.runningSpeed;

            _baselineSprintSpeed = motor.freeSpeed.sprintSpeed;

            RefreshSpeedsFromProfile();

        }



        private void ApplySpeedsFromProfile()

        {

            float slowMul = CurrentSlowMultiplier();

            float jogMul = CurrentJogMultiplier();

            float burstMul = CurrentBurstMultiplier();



            motor.freeSpeed.walkSpeed = _baselineJogSpeed * slowMul;

            motor.strafeSpeed.walkSpeed = _baselineJogSpeed * slowMul;

            motor.freeSpeed.runningSpeed = _baselineJogSpeed * jogMul;

            motor.strafeSpeed.runningSpeed = _baselineJogSpeed * jogMul;

            motor.freeSpeed.sprintSpeed = _baselineSprintSpeed * burstMul;

            motor.strafeSpeed.sprintSpeed = _baselineSprintSpeed * burstMul;

        }



        private void RestoreBaselineSpeeds()

        {

            motor.freeSpeed.walkSpeed = _baselineWalkSpeed;

            motor.strafeSpeed.walkSpeed = _baselineWalkSpeed;

            motor.freeSpeed.runningSpeed = _baselineJogSpeed;

            motor.strafeSpeed.runningSpeed = _baselineJogSpeed;

            motor.freeSpeed.sprintSpeed = _baselineSprintSpeed;

            motor.strafeSpeed.sprintSpeed = _baselineSprintSpeed;

        }



        private bool CanBurstSprint()

        {

            if (_survival == null)

                _survival = GetComponent<SurvivalStats>();

            if (_survival == null)

                return true;

            return _survival.CurrentStamina > 0.01f;

        }



        
        /// <summary>
        /// Re-apply walkByDefault flags for the current gait. Invector onlyWalkWhenAiming
        /// forces walk each frame while ADS; without this, Shift-run dies after the first frame when armed.
        /// stamp: armed-shift-run 0920
        /// </summary>
        public void ReassertMotorGaitFlags()
        {
            if (motor == null)
                return;

            bool walk = CurrentGait == Gait.SlowWalk || _appliedGait == Gait.SlowWalk;
            motor.freeSpeed.walkByDefault = walk;
            motor.strafeSpeed.walkByDefault = walk;
            motor.alwaysWalkByDefault = walk;
            EnforceMotorSprintState();
        }
        private void ApplySlowWalk()

        {

            motor.freeSpeed.walkByDefault = true;

            motor.strafeSpeed.walkByDefault = true;

            motor.alwaysWalkByDefault = true;

            motor.isSprinting = false;

            motor.speedMultiplier = 1f;

            CurrentGait = Gait.SlowWalk;

        }



        private void ApplyJog()

        {

            motor.freeSpeed.walkByDefault = false;

            motor.strafeSpeed.walkByDefault = false;

            motor.alwaysWalkByDefault = false;

            motor.isSprinting = false;

            motor.speedMultiplier = 1f;

            CurrentGait = Gait.Jog;

        }



        private void ApplySprintBurst()

        {

            motor.freeSpeed.walkByDefault = false;

            motor.strafeSpeed.walkByDefault = false;

            motor.alwaysWalkByDefault = false;

            motor.isSprinting = true;

            motor.speedMultiplier = 1f;

            CurrentGait = Gait.SprintBurst;

        }



        private bool _lastShiftHeld;

    }

}


