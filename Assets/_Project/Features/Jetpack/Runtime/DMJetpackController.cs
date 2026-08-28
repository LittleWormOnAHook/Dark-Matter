using Invector.vCharacterController;
using UnityEngine;

namespace Project.Features.Jetpack
{
    public enum DMJetpackPhase
    {
        Grounded,
        Airborne,
        BoostFull,
        BoostFade,
        FreeFall,
    }

    /// <summary>
    /// Starfield-style jetpack fuel + vertical thrust. Horizontal motion stays on Invector air control.
    /// Vertical motion is applied through Invector's motor (AddForce + verticalVelocity), never raw
    /// linearVelocity on kinematic bodies.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(450)]
    public sealed class DMJetpackController : MonoBehaviour
    {
        [SerializeField] private DMJetpackProfile profile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private DMJetpackInputBridge inputBridge;

        private Rigidbody _rigidbody;
        private float _fuelRemaining;
        private float _boostElapsed;
        private bool _boostHeld;
        private bool _boostArmed;
        private bool _wasBoosting;
        private bool _hadJetpackFlightThisAirtime;
        private bool _wasGrounded = true;
        private bool _airSpeedOverridden;
        private bool _extraGravityOverridden;
        private float _savedExtraGravity;

        public DMJetpackPhase Phase { get; private set; } = DMJetpackPhase.Grounded;
        public float FuelNormalized => profile != null && profile.maxBoostSeconds > 0f
            ? Mathf.Clamp01(_fuelRemaining / profile.maxBoostSeconds)
            : 0f;
        public float CurrentThrustVisual { get; private set; }

        /// <summary>True while Space/thrust is actively firing (VFX only).</summary>
        public bool IsBoostVisualActive =>
            Phase is DMJetpackPhase.BoostFull or DMJetpackPhase.BoostFade;

        /// <summary>True from first jetpack boost until landing — drives fly blend tree.</summary>
        public bool IsJetpackAnimActive =>
            _hadJetpackFlightThisAirtime && motor != null && !motor.isGrounded;

        public bool ShouldPlayJetpackLand =>
            _hadJetpackFlightThisAirtime && motor != null && motor.isGrounded;

        private void Reset()
        {
            motor = GetComponent<vThirdPersonMotor>();
            inputBridge = GetComponent<DMJetpackInputBridge>();
        }

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<vThirdPersonMotor>();
            if (inputBridge == null)
                inputBridge = GetComponent<DMJetpackInputBridge>();

            _rigidbody = motor != null ? motor.GetComponent<Rigidbody>() : GetComponent<Rigidbody>();
            RefillFuel();
        }

        public void RefillFuel()
        {
            _fuelRemaining = profile != null ? profile.maxBoostSeconds : 0f;
        }

        public void SetBoostHeld(bool held)
        {
            _boostHeld = held;
        }

        /// <returns>True when this press was consumed for jetpack ignition (skip normal jump).</returns>
        public bool TryIgniteBoostOnJumpPress()
        {
            if (motor == null || profile == null)
                return false;

            if (motor.isGrounded)
                return false;

            if (_fuelRemaining <= 0.01f)
                return false;

            if (Phase is DMJetpackPhase.BoostFull or DMJetpackPhase.BoostFade)
                return false;

            BeginBoost();
            return true;
        }

        private void BeginBoost()
        {
            Phase = DMJetpackPhase.BoostFull;
            _boostElapsed = 0f;
            _boostArmed = true;
            _hadJetpackFlightThisAirtime = true;
            SuppressJumpMotorState();
        }

        /// <summary>Stop Invector jump arc / airborne locomotion from fighting jetpack thrust.</summary>
        public void SuppressJumpMotorState()
        {
            if (motor == null)
                return;

            motor.isJumping = false;
        }

        public void NotifyLanded()
        {
            _hadJetpackFlightThisAirtime = false;
            _boostElapsed = 0f;
            _boostHeld = false;
            _boostArmed = false;
            Phase = DMJetpackPhase.Grounded;
            CurrentThrustVisual = 0f;
            RestoreMotorTuning();
        }

        private void FixedUpdate()
        {
            if (motor == null || profile == null || _rigidbody == null)
                return;

            bool grounded = motor.isGrounded;
            if (grounded && !_wasGrounded)
            {
                _hadJetpackFlightThisAirtime = false;
                _boostArmed = false;

                if (Phase != DMJetpackPhase.Grounded)
                    Phase = DMJetpackPhase.Grounded;

                _boostElapsed = 0f;
                CurrentThrustVisual = 0f;
                RestoreMotorTuning();
            }
            else if (!grounded && _wasGrounded)
            {
                Phase = DMJetpackPhase.Airborne;
            }

            _wasGrounded = grounded;

            if (grounded)
            {
                RegenerateFuel(Time.fixedDeltaTime);
                CurrentThrustVisual = 0f;
                return;
            }

            if (IsJetpackAnimActive)
                SuppressJumpMotorState();

            TickAirborneBoost(Time.fixedDeltaTime);
        }

        private void TickAirborneBoost(float dt)
        {
            // Boost only after an explicit airborne jump press (2nd Space), then hold.
            // Holding Space from the ground jump must not auto-ignite on takeoff.
            bool wantsBoost = _boostArmed && _boostHeld && _fuelRemaining > 0f;

            if (_wasBoosting && !wantsBoost)
            {
                _boostArmed = false;

                if (profile.releaseRegenBonusSeconds > 0f)
                {
                    _fuelRemaining = Mathf.Min(
                        profile.maxBoostSeconds,
                        _fuelRemaining + profile.releaseRegenBonusSeconds);
                }
            }

            _wasBoosting = wantsBoost;

            if (wantsBoost)
            {
                _fuelRemaining = Mathf.Max(0f, _fuelRemaining - dt);
                _boostElapsed += dt;

                float fadeStart = profile.fullThrustEndSeconds;
                float fadeEnd = profile.maxBoostSeconds;
                bool inFade = _boostElapsed >= fadeStart && _fuelRemaining > 0f;

                Phase = inFade ? DMJetpackPhase.BoostFade : DMJetpackPhase.BoostFull;

                float thrustT = Phase == DMJetpackPhase.BoostFull
                    ? 1f
                    : Mathf.InverseLerp(fadeEnd, fadeStart, _boostElapsed);

                CurrentThrustVisual = Phase == DMJetpackPhase.BoostFade
                    ? Mathf.Lerp(profile.thrusterAlphaMin, 1f, thrustT)
                    : 1f;

                ApplyMotorTuningForJetpack();
                ApplyJetpackAirSpeed();
                ApplyVerticalThrust(dt, thrustT);

                if (_fuelRemaining <= 0f)
                {
                    Phase = DMJetpackPhase.FreeFall;
                    RestoreAirSpeed();
                }
            }
            else
            {
                if (Phase is DMJetpackPhase.BoostFull or DMJetpackPhase.BoostFade)
                    Phase = DMJetpackPhase.FreeFall;

                CurrentThrustVisual = 0f;
                RegenerateFuel(dt);
                RestoreAirSpeed();

                if (_hadJetpackFlightThisAirtime)
                {
                    ApplyMotorTuningForJetpack();
                    ApplyCoastFall(dt);
                }
                else
                {
                    RestoreMotorPhysicsOverrides();
                }
            }
        }

        private bool CanApplyJetpackPhysics()
        {
            if (_rigidbody == null || motor == null)
                return false;

            if (_rigidbody.isKinematic)
                return false;

            if (motor.ragdolled || motor.isDead)
                return false;

            return true;
        }

        private Vector3 MotorUp => motor != null ? motor.transform.up : Vector3.up;

        /// <summary>Read vertical speed through Invector's tracker when possible.</summary>
        private float GetVerticalVelocity()
        {
            if (motor != null && !CanApplyJetpackPhysics())
                return motor.verticalVelocity;

            return Vector3.Dot(_rigidbody.linearVelocity, MotorUp);
        }

        private void SyncMotorVerticalVelocity()
        {
            if (motor == null || !CanApplyJetpackPhysics())
                return;

            motor.verticalVelocity = Vector3.Dot(_rigidbody.linearVelocity, MotorUp);
        }

        /// <summary>Invector-style vertical impulse (same ForceMode as extraGravity / air control).</summary>
        private void ApplyVerticalDelta(float deltaVy)
        {
            if (!CanApplyJetpackPhysics() || Mathf.Abs(deltaVy) < 0.00001f)
                return;

            _rigidbody.AddForce(MotorUp * deltaVy, ForceMode.VelocityChange);
            SyncMotorVerticalVelocity();
        }

        private void ApplyVerticalThrust(float dt, float thrustStrength)
        {
            if (!CanApplyJetpackPhysics())
                return;

            if (motor.isJumping)
                SuppressJumpMotorState();

            float localVertical = inputBridge != null ? inputBridge.LocalVerticalInput : 0f;
            float gravityScale = ResolveBoostGravityScale(localVertical);
            float response = profile.verticalResponse;
            float currentVy = GetVerticalVelocity();
            float targetVy;

            if (localVertical < -profile.descendInputThreshold)
            {
                targetVy = -profile.descendForce * thrustStrength;
            }
            else
            {
                float ascendIntent = Mathf.Clamp01(
                    1f - Mathf.Max(0f, -localVertical) * profile.descendBrakeStrength);
                targetVy = profile.upwardThrustForce * thrustStrength * ascendIntent;
            }

            float easedVy = Mathf.Lerp(currentVy, targetVy, 1f - Mathf.Exp(-response * dt));
            if (gravityScale > 0f)
                easedVy += Physics.gravity.y * gravityScale * dt;

            ApplyVerticalDelta(easedVy - currentVy);
        }

        private float ResolveBoostGravityScale(float localVertical)
        {
            if (localVertical < -profile.descendInputThreshold)
                return profile.boostDescendGravityScale;

            if (Phase == DMJetpackPhase.BoostFade)
                return profile.fadeFallGravityScale;

            if (Phase == DMJetpackPhase.BoostFull)
                return profile.hoverGravityScale;

            return profile.fadeFallGravityScale;
        }

        private void ApplyCoastFall(float dt)
        {
            if (!CanApplyJetpackPhysics())
                return;

            float currentVy = GetVerticalVelocity();

            if (currentVy > 0f)
            {
                float bleed = Mathf.Min(currentVy, profile.releaseUpwardBleed * dt);
                ApplyVerticalDelta(-bleed);
                currentVy -= bleed;
            }

            float gravityScale = profile.coastFallGravityScale;
            if (Phase == DMJetpackPhase.FreeFall &&
                profile.freeFallGravityScale > profile.coastFallGravityScale)
            {
                gravityScale = profile.freeFallGravityScale;
            }

            ApplyVerticalDelta(Physics.gravity.y * gravityScale * dt);
        }

        private void ApplyMotorTuningForJetpack()
        {
            ApplyMotorPhysicsOverrides();
        }

        /// <summary>Jetpack owns vertical gravity while active — disable Invector extraGravity so it does not stack.</summary>
        private void ApplyMotorPhysicsOverrides()
        {
            if (motor == null || _extraGravityOverridden)
                return;

            _savedExtraGravity = motor.extraGravity;
            motor.extraGravity = 0f;
            _extraGravityOverridden = true;
        }

        private void RestoreMotorPhysicsOverrides()
        {
            if (motor == null || !_extraGravityOverridden)
                return;

            motor.extraGravity = _savedExtraGravity;
            _extraGravityOverridden = false;
        }

        private void ApplyJetpackAirSpeed()
        {
            if (motor == null || profile == null || _airSpeedOverridden)
                return;

            motor.airSpeed = profile.jetpackAirSpeed;
            _airSpeedOverridden = true;
        }

        private void RestoreAirSpeed()
        {
            if (motor == null || profile == null || !_airSpeedOverridden)
                return;

            motor.airSpeed = profile.defaultAirSpeed;
            _airSpeedOverridden = false;
        }

        private void RestoreMotorTuning()
        {
            RestoreAirSpeed();
            RestoreMotorPhysicsOverrides();
        }

        private void RegenerateFuel(float dt)
        {
            if (profile.regenSecondsPerSecond <= 0f)
                return;

            _fuelRemaining = Mathf.Min(
                profile.maxBoostSeconds,
                _fuelRemaining + profile.regenSecondsPerSecond * dt);

            if (!_boostHeld && Phase is DMJetpackPhase.FreeFall or DMJetpackPhase.Airborne &&
                _fuelRemaining < profile.maxBoostSeconds)
            {
                _fuelRemaining = Mathf.Min(
                    profile.maxBoostSeconds,
                    _fuelRemaining + profile.releaseRegenBonusSeconds * dt * 0.15f);
            }
        }
    }
}
