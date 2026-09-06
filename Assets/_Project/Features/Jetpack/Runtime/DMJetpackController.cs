using Invector.vCharacterController;
using Project.Progression;
using Project.Survival;
using Project.UI;
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
    /// Starfield-style jetpack fuel + vertical thrust. Planar boost steering runs after
    /// Invector air control so direction changes stay responsive on rigidbody players.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(450)]
    public sealed class DMJetpackController : MonoBehaviour
    {
        [SerializeField] private DMJetpackProfile profile;
        [SerializeField] private vThirdPersonMotor motor;
        [SerializeField] private DMJetpackInputBridge inputBridge;
        [SerializeField] private DMJetpackAnimatorDriver animatorDriver;

        private Rigidbody _rigidbody;
        private float _fuelRemaining;
        private float _boostElapsed;
        private bool _boostHeld;
        private bool _boostArmed;
        private bool _wasBoosting;
        private bool _hadJetpackFlightThisAirtime;
        private float _heroLandHoldUntil = -1f;
        private float _boostReleasedAt = -1f;
        private bool _usedJetpackThisAir;
        private const float HeroLandHoldSeconds = 4f;
        private bool _wasGrounded = true;
        private float _groundedStable;
        private bool _emptyUntilRelease;
        private const float GroundedConfirmSeconds = 0.15f;
        private bool _airSpeedOverridden;
        private bool _airSmoothOverridden;
        private bool _extraGravityOverridden;
        private float _savedExtraGravity;
        private float _savedAirSmooth;
        private Vector3 _smoothSteerPlanar;
        private Vector3 _smoothSteerVelocity;
        private SurvivalStats _survivalStats;
        private bool _noPowerBoostAttempt;
        private const float DefaultEnergyDrainPerSecond = 10f;

        public DMJetpackProfile Profile => profile;

        public DMJetpackPhase Phase { get; private set; } = DMJetpackPhase.Grounded;
        public float MaxBoostSeconds =>
            profile != null
                ? profile.maxBoostSeconds * (1f + PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.JetFuelPercent) / 100f)
                : 0f;

        public float FuelRemaining => _fuelRemaining;

        public float FuelNormalized => MaxBoostSeconds > 0f
            ? Mathf.Clamp01(_fuelRemaining / MaxBoostSeconds)
            : 0f;
        public float CurrentThrustVisual { get; private set; }

        /// <summary>True while Space/thrust is actively firing (VFX only).</summary>
        public bool IsBoostVisualActive =>
            Phase is DMJetpackPhase.BoostFull or DMJetpackPhase.BoostFade;

        /// <summary>True from first jetpack boost until landing — drives fly blend tree.</summary>
        public bool HadJetpackFlightThisAirtime => _hadJetpackFlightThisAirtime;

        public bool IsHeroLandHoldActive => Time.unscaledTime <= _heroLandHoldUntil;

        public bool IsHeroLandArmed =>
            _hadJetpackFlightThisAirtime || IsHeroLandHoldActive;

        public bool IsBoostingNow =>
            Phase is DMJetpackPhase.BoostFull or DMJetpackPhase.BoostFade;

        /// <summary>Seconds since Space/boost was released. 0 while still boosting.</summary>
        public bool UsedJetpackThisAir => _usedJetpackThisAir;

        public float SecondsSinceBoostReleased =>
            !_usedJetpackThisAir || IsBoostingNow || _boostReleasedAt < 0f
                ? 0f
                : Time.unscaledTime - _boostReleasedAt;

        public bool IsJetpackAnimActive =>
            IsHeroLandArmed && motor != null && !motor.isGrounded;

        public bool ShouldPlayJetpackLand =>
            IsHeroLandArmed && motor != null && motor.isGrounded;

        public void ArmHeroLandHold()
        {
            _heroLandHoldUntil = Time.unscaledTime + HeroLandHoldSeconds;
        }

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
            if (animatorDriver == null)
                animatorDriver = GetComponent<DMJetpackAnimatorDriver>();

            _rigidbody = motor != null ? motor.GetComponent<Rigidbody>() : GetComponent<Rigidbody>();
            _survivalStats = GetComponent<SurvivalStats>() ?? GetComponentInParent<SurvivalStats>();
            RefillFuel();
        }

        private float ScaledUpwardThrust =>
            profile != null
                ? profile.upwardThrustForce * (1f + PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.JetThrustPercent) / 100f)
                : 0f;

        private float EnergyDrainPerSecond =>
            profile != null
                ? Mathf.Max(0f, profile.energyDrainPerSecond)
                : DefaultEnergyDrainPerSecond;

        private float ScaledRegenPerSecond =>
            profile != null
                ? profile.regenSecondsPerSecond * (1f + PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.JetRegenPercent) / 100f)
                : 0f;

        public void RefillFuel()
        {
            _fuelRemaining = MaxBoostSeconds;
        }

        public void SetBoostHeld(bool held)
        {
            _boostHeld = held;
            if (!held)
            {
                _emptyUntilRelease = false;
                _noPowerBoostAttempt = false;
            }
        }

        /// <returns>True when this press was consumed for jetpack ignition (skip normal jump).</returns>
        public bool TryIgniteBoostOnJumpPress()
        {
            if (motor == null || profile == null)
                return false;

            if (motor.isGrounded)
                return false;

            if (_emptyUntilRelease || _fuelRemaining <= 0.01f)
                return false;

            if (Phase is DMJetpackPhase.BoostFull or DMJetpackPhase.BoostFade)
                return false;

            if (!HasJetpackEnergy())
            {
                _noPowerBoostAttempt = true;
                DMUiToolkitHud.ShowNoPower();
                return true;
            }

            BeginBoost();
            return true;
        }

        private void BeginBoost()
        {
            Phase = DMJetpackPhase.BoostFull;
            _boostElapsed = 0f;
            _boostArmed = true;
            _hadJetpackFlightThisAirtime = true;
            _usedJetpackThisAir = true;
            _boostReleasedAt = -1f;
            ArmHeroLandHold();
            SuppressJumpMotorState();
            if (animatorDriver == null)
                animatorDriver = GetComponent<DMJetpackAnimatorDriver>();
            if (animatorDriver != null)
                animatorDriver.NotifyBoostStarted();
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
            // A one-frame ground flicker must not clear hero-land for the real fall.
            if (motor != null && !motor.isGrounded)
                return;

            _hadJetpackFlightThisAirtime = false;
            _usedJetpackThisAir = false;
            _boostReleasedAt = -1f;
            _heroLandHoldUntil = -1f;
            _boostElapsed = 0f;
            _boostHeld = false;
            _boostArmed = false;
            _emptyUntilRelease = false;
            Phase = DMJetpackPhase.Grounded;
            CurrentThrustVisual = 0f;
            ResetPlanarSteerState();
            RestoreMotorTuning();
        }

        private void ResetPlanarSteerState()
        {
            _smoothSteerPlanar = Vector3.zero;
            _smoothSteerVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (motor == null || profile == null || _rigidbody == null)
                return;

            float dt = Time.fixedDeltaTime;
            float fuelDt = Time.fixedUnscaledDeltaTime;
            bool grounded = motor.isGrounded;
            if (grounded)
                _groundedStable += dt;
            else
                _groundedStable = 0f;

            bool firmlyGrounded = grounded && _groundedStable >= GroundedConfirmSeconds;

            if (!grounded && _wasGrounded)
                Phase = DMJetpackPhase.Airborne;

            _wasGrounded = grounded;

            // Grazing terrain must not cancel a live burn or refill the tank.
            if (firmlyGrounded)
            {
                _boostArmed = false;
                _boostElapsed = 0f;
                if (Phase != DMJetpackPhase.Grounded)
                    Phase = DMJetpackPhase.Grounded;
                CurrentThrustVisual = 0f;
                ResetPlanarSteerState();
                RestoreMotorTuning();
                RegenerateFuel(fuelDt);
                return;
            }

            if (IsJetpackAnimActive)
                SuppressJumpMotorState();

            TickAirborneBoost(dt, fuelDt);
        }

        private void TickAirborneBoost(float dt, float fuelDt)
        {
            // Boost only after an explicit airborne jump press (2nd Space), then hold.
            // Holding Space from the ground jump must not auto-ignite on takeoff.
            // After a coast this same air, a new Space press re-ignites if fuel remains.
            bool hasEnergy = HasJetpackEnergy();
            if (_boostHeld && !hasEnergy && (_usedJetpackThisAir || _boostArmed || _noPowerBoostAttempt))
                DMUiToolkitHud.ShowNoPower();

            if (!_boostArmed && _boostHeld && !_emptyUntilRelease && _usedJetpackThisAir && _fuelRemaining > 0.01f && hasEnergy)
                BeginBoost();

            bool wantsBoost = _boostArmed && _boostHeld && !_emptyUntilRelease && _fuelRemaining > 0f && hasEnergy;

            if (_wasBoosting && !wantsBoost)
            {
                _boostReleasedAt = Time.unscaledTime;
                ArmHeroLandHold();
                _boostArmed = false;
                ResetPlanarSteerState();
            }

            _wasBoosting = wantsBoost;

            if (wantsBoost)
            {
                _fuelRemaining -= fuelDt;
                _boostElapsed += fuelDt;
                SpendJetpackEnergy(fuelDt);
                if (!HasJetpackEnergy())
                {
                    CutBoostNoEnergy();
                    ApplyCoastFall(dt);
                    return;
                }

                if (_fuelRemaining <= 0f)
                {
                    CutBoostEmpty();
                    ApplyCoastFall(dt);
                    return;
                }

                float tank = MaxBoostSeconds;
                float fadeStart = Mathf.Clamp(profile.fullThrustEndSeconds, 0f, profile.maxBoostSeconds)
                    * (tank / Mathf.Max(0.01f, profile.maxBoostSeconds));
                bool canFade = fadeStart < tank - 0.05f;
                bool inFade = canFade && _boostElapsed >= fadeStart;

                Phase = inFade ? DMJetpackPhase.BoostFade : DMJetpackPhase.BoostFull;

                float thrustT = inFade
                    ? Mathf.InverseLerp(tank, fadeStart, _boostElapsed)
                    : 1f;

                CurrentThrustVisual = inFade
                    ? Mathf.Lerp(profile.thrusterAlphaMin, 1f, thrustT)
                    : 1f;

                ApplyMotorTuningForJetpack();
                ApplyJetpackAirTuning();
                ApplyVerticalThrust(dt, thrustT);
                ApplyJetpackPlanarSteering(dt);
            }
            else
            {
                if (Phase is DMJetpackPhase.BoostFull or DMJetpackPhase.BoostFade)
                    Phase = DMJetpackPhase.FreeFall;

                CurrentThrustVisual = 0f;
                RegenerateFuel(fuelDt);
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

        private void CutBoostEmpty()
        {
            _fuelRemaining = 0f;
            _emptyUntilRelease = true;
            _boostArmed = false;
            _wasBoosting = false;
            _boostReleasedAt = Time.unscaledTime;
            Phase = DMJetpackPhase.FreeFall;
            CurrentThrustVisual = 0f;
            ResetPlanarSteerState();
            ArmHeroLandHold();
            RestoreAirSpeed();
        }

        /// <summary>Stop thrust when energy is empty without dumping the fuel tank.</summary>
        private void CutBoostNoEnergy()
        {
            DMUiToolkitHud.ShowNoPower();
            _boostArmed = false;
            _wasBoosting = false;
            _boostReleasedAt = Time.unscaledTime;
            Phase = DMJetpackPhase.FreeFall;
            CurrentThrustVisual = 0f;
            ResetPlanarSteerState();
            ArmHeroLandHold();
            RestoreAirSpeed();
        }

        private bool HasJetpackEnergy()
        {
            if (_survivalStats == null)
                _survivalStats = GetComponent<SurvivalStats>() ?? GetComponentInParent<SurvivalStats>();
            return _survivalStats == null || _survivalStats.HasEnergy();
        }

        private void SpendJetpackEnergy(float fuelDt)
        {
            if (_survivalStats == null)
                return;
            _survivalStats.SpendEnergy(EnergyDrainPerSecond * fuelDt);
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

        private float GetPlanarIntent()
        {
            if (inputBridge == null || profile == null)
                return 0f;

            Vector2 local = inputBridge.LocalMoveInput;
            float deadzone = profile.animInputDeadzone;
            if (local.sqrMagnitude <= deadzone * deadzone)
                return 0f;

            return Mathf.Clamp01(local.magnitude);
        }

        private void ApplyVerticalThrust(float dt, float thrustStrength)
        {
            if (!CanApplyJetpackPhysics())
                return;

            if (motor.isJumping)
                SuppressJumpMotorState();

            float planarIntent = GetPlanarIntent();
            float gravityScale = ResolveBoostGravityScale();
            float response = profile.verticalResponse;
            float currentVy = GetVerticalVelocity();
            float verticalScale = Mathf.Lerp(1f, profile.directedVerticalScale, planarIntent);
            float targetVy = ScaledUpwardThrust * thrustStrength * verticalScale;

            float easedVy = Mathf.Lerp(currentVy, targetVy, 1f - Mathf.Exp(-response * dt));
            if (gravityScale > 0f)
                easedVy += Physics.gravity.y * gravityScale * dt;

            ApplyVerticalDelta(easedVy - currentVy);
        }

        private float ResolveBoostGravityScale()
        {
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

        private void ApplyJetpackAirTuning()
        {
            if (motor == null || profile == null)
                return;

            if (!_airSpeedOverridden)
            {
                motor.airSpeed = profile.jetpackAirSpeed;
                _airSpeedOverridden = true;
            }

            if (!_airSmoothOverridden)
            {
                _savedAirSmooth = motor.airSmooth;
                motor.airSmooth = profile.jetpackAirSmooth;
                _airSmoothOverridden = true;
            }
        }

        /// <summary>
        /// Pioneer uses rigidbody air force — without this, boost momentum resists direction changes.
        /// Runs after Invector AirControl and steers planar velocity toward camera-relative input.
        /// </summary>
        private void ApplyJetpackPlanarSteering(float dt)
        {
            if (!CanApplyJetpackPhysics() || inputBridge == null || profile == null)
                return;

            Vector2 local = inputBridge.LocalMoveInput;
            float deadzone = profile.animInputDeadzone;
            if (local.sqrMagnitude <= deadzone * deadzone)
                return;

            Transform facing = motor.transform;
            Vector3 desiredPlanar = facing.forward * local.y + facing.right * local.x;
            desiredPlanar.y = 0f;
            if (desiredPlanar.sqrMagnitude < 0.0001f)
                return;

            desiredPlanar.Normalize();

            float steerDelay = profile.jetpackSteerInputSmoothTime;
            _smoothSteerPlanar = Vector3.SmoothDamp(
                _smoothSteerPlanar,
                desiredPlanar,
                ref _smoothSteerVelocity,
                steerDelay,
                Mathf.Infinity,
                dt);

            if (_smoothSteerPlanar.sqrMagnitude < 0.0001f)
                return;

            Vector3 steerDirection = _smoothSteerPlanar.normalized;
            Vector3 targetPlanar = steerDirection * profile.jetpackAirSpeed;

            Vector3 velocity = _rigidbody.linearVelocity;
            float vertical = Vector3.Dot(velocity, MotorUp);
            Vector3 planar = velocity - vertical * MotorUp;

            float blend = 1f - Mathf.Exp(-profile.jetpackPlanarResponse * dt);
            Vector3 steeredPlanar = Vector3.Lerp(planar, targetPlanar, blend);

            if (vertical > 0f)
            {
                float bleed = Mathf.Min(vertical, profile.directedRedirect * GetPlanarIntent() * dt);
                vertical -= bleed;
                steeredPlanar += steerDirection * bleed;
            }

            _rigidbody.linearVelocity = steeredPlanar + vertical * MotorUp;
            SyncMotorVerticalVelocity();
        }

        private void RestoreAirSpeed()
        {
            if (motor == null || profile == null)
                return;

            if (_airSpeedOverridden)
            {
                motor.airSpeed = profile.defaultAirSpeed;
                _airSpeedOverridden = false;
            }

            if (_airSmoothOverridden)
            {
                motor.airSmooth = _savedAirSmooth;
                _airSmoothOverridden = false;
            }
        }

        private void RestoreMotorTuning()
        {
            RestoreAirSpeed();
            RestoreMotorPhysicsOverrides();
        }

        private void RegenerateFuel(float dt)
        {
            if (_emptyUntilRelease)
                return;

            if (ScaledRegenPerSecond <= 0f)
                return;

            _fuelRemaining = Mathf.Min(
                MaxBoostSeconds,
                _fuelRemaining + ScaledRegenPerSecond * dt);

            if (!_boostHeld && Phase is DMJetpackPhase.FreeFall or DMJetpackPhase.Airborne &&
                _fuelRemaining < MaxBoostSeconds)
            {
                _fuelRemaining = Mathf.Min(
                    MaxBoostSeconds,
                    _fuelRemaining + profile.releaseRegenBonusSeconds * dt * 0.15f);
            }
        }
    }
}
