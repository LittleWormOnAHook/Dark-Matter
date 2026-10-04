using Invector.vCharacterController;
using InvInputDevice = Invector.vCharacterController.InputDevice;
using Invector.vEventSystems;
using Invector.vMelee;
using Invector.vShooter;
using Invector;
using Project.Combat;
using Invector.IK;
using Project.Building;
using Project.Core;
using Project.Data;
using Project.Features.Jetpack;
using Project.Features.Climb;
using Project.Features.Locomotion;
using Project.Inventory;
using Project.Player;
using Project.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// Invector shooter/melee input with Pioneer UI lock integration.
    /// Reads keyboard/mouse via the Input System because legacy Input.GetAxis is unreliable in this project.
    /// </summary>
    [DefaultExecutionOrder(500)]
    [DisallowMultipleComponent]
    public class PioneerShooterMeleeInput : vShooterMeleeInput
    {
        private const float MouseLookScale = PioneerInvectorRecoilUtility.CameraInputScale;
        private const float GamepadStickDeadZone = 0.18f;
        /// <summary>Maps right-stick -1..1 into RotateCamera before Invector sensitivity.</summary>
        private const float GamepadStickLookScale = 6f;
        private const string KbmLookStamp = "controller-look-triggers 0920k";
        /// <summary>Fallback discrete mouse-wheel zoom stops when no profile is assigned.</summary>
        private const int DefaultZoomClickLevels = 10;
        private const int UiZoomRestoreFrames = 12;

        [Header("Pioneer Camera Zoom")]
        [SerializeField, Tooltip("Optional shared profile (Genesis Studio → Player → Camera). Overrides the fields below when set.")]
        private DMCameraProfile cameraProfile;
        [SerializeField] private float runtimeMinCameraDistance = 2.25f;
        [SerializeField] private float runtimeMaxCameraDistance = 12f;
        [SerializeField] private float runtimeDefaultCameraDistance = 2.25f;
        [SerializeField, Tooltip("How much closer Aiming pulls vs free-look preferred zoom.")]
        private float aimZoomPullInMeters = 0.55f;
        [SerializeField, Tooltip("Extra follow distance while sprinting (slight pull-out only).")]
        private float sprintZoomOutMeters = 0.85f;
        [SerializeField, Tooltip("Closest follow distance allowed while aiming (ADS). Can be below scroll min.")]
        private float aimMinCameraDistance = 0.78f;

        [Header("Meshy Aim Snap")]
        [SerializeField, Tooltip("Snap ranged aim IK/arm alignment for Meshy Visual swaps instead of the slow VBOT-tuned drift.")]
        private bool meshySnapAim = true;
        [SerializeField, Tooltip("Only snap when a Visual/ humanoid child is present on this prefab.")]
        private bool meshySnapAimRequiresVisual = true;

        private PioneerInvectorInputBridge _inputBridge;
        private DMJetpackInputBridge _jetpackInputBridge;
        private DMClimbController _climb;
        private DMLandingDirector _landingDirector;
        private DMLocomotionGaitController _locomotionGait;
        private float _jumpHeightBeforeOverride = float.NaN;
        private EquipmentController _equipment;
        private PlayerController _playerController;
        private PlayerInput _playerInput; // stamp: controller-compile-fix 0920
        private static bool _loggedKbmLookStamp;
        private bool _miningScanAimHold;
        /// <summary>Scroll zoom the player chose â€” preserved across aim/culling so ChangeState cannot wipe it.</summary>
        private float _preferredCameraZoom = -1f;
        private bool _wasAimingCameraLastFrame;
        private bool _wasUiBlockingLastFrame;
        private bool _wasBlockHeldLastFrame;
        private float _lastBlockPressTime = float.NegativeInfinity;
        private Coroutine _guardImpactRoutine;
        private const float BlockGuardPoseSeconds = 0.12f;
        private const float StrongAttackBlockSuppressSeconds = 0.2f;
        private const float StrongDamageWindowSeconds = 1.75f;
        /// <summary>
        /// FullBody path. The controller has no charge/hold clip, so the wind-up is a local weapon pose.
        /// StrongAttacks/SwordAttack/A plays on release.
        /// </summary>
        private const string StrongSwordStatePath = "Attacks.StrongAttacks.SwordAttack.A";
        private static readonly int StrongSwordStateHash = Animator.StringToHash(StrongSwordStatePath);
        private static readonly Vector3 StrongChargeLocalOffset = new Vector3(0.02f, 0.05f, -0.07f);
        private static readonly Vector3 StrongChargeEuler = new Vector3(-36f, 16f, 6f);
        private bool _lightAttackHeldLast;
        private float _lightAttackHoldStart = float.NegativeInfinity;
        private bool _strongChargeArmed;
        private float _chargeArmedTime;
        private float _suppressBlockUntil;
        private bool _strongDamageArmed;
        private bool _strongDamageSawSwing;
        private float _strongDamageUntil;
        private Transform _chargePoseTransform;
        private Vector3 _chargePoseRestPosition;
        private Quaternion _chargePoseRestRotation;
        private bool _chargePoseCaptured;
        private bool _wasSprintingLastFrame;
        private int _uiZoomRestoreFramesRemaining;
        private float _lockedAimZoom = -1f;
        private float _lastArmedCameraZoom = -1f;
        private Coroutine _startZoomRoutine;
        private static bool _loggedScrollStamp;
        private bool _buildLookRaised;
        private float _savedLookUpLimit;
        private float _savedLookDownLimit;
        private float _savedBuildZoom = -1f;
        private float _savedBuildZoomMax = -1f;
        protected override void Start()
        {
            base.Start();
            _inputBridge = GetComponent<PioneerInvectorInputBridge>();
            _jetpackInputBridge = GetComponent<DMJetpackInputBridge>();
            _climb = GetComponent<DMClimbController>();
            _locomotionGait = GetComponent<DMLocomotionGaitController>();
            _equipment = GetComponent<EquipmentController>();
            _playerController = GetComponent<PlayerController>();
            _playerInput = GetComponent<PlayerInput>();
            PioneerInvectorMeshyAimSnapUtility.ApplyShooterManagerSettings(gameObject, shooterManager);
            SyncPioneerCursorState();

            if (GameSession.HasStarted)
                ApplyStartZoomIn();
        }

        private void OnEnable()
        {
            GameSession.GameStarted += HandleGameStartedZoom;
            if (GameSession.HasStarted)
                HandleGameStartedZoom();
        }

        private void OnDisable()
        {
            RestoreBuildModeLookUp();
            GameSession.GameStarted -= HandleGameStartedZoom;
            if (_startZoomRoutine != null)
            {
                StopCoroutine(_startZoomRoutine);
                _startZoomRoutine = null;
            }
        }

        /// <summary>
        /// Expedition start: pull third-person follow distance all the way in (min zoom).
        /// </summary>
        private void HandleGameStartedZoom()
        {
            if (!isActiveAndEnabled)
                return;

            if (_startZoomRoutine != null)
                StopCoroutine(_startZoomRoutine);
            _startZoomRoutine = StartCoroutine(ApplyStartZoomInWhenReady());
        }

        private IEnumerator ApplyStartZoomInWhenReady()
        {
            // Wait until Invector camera Init has run (tpCamera + currentState).
            for (int i = 0; i < 8 && (tpCamera == null || tpCamera.currentState == null); i++)
                yield return null;

            ApplyStartZoomIn();
            // One more frame â€” loading handoff / ChangeState can rewrite distance after MarkStarted.
            yield return null;
            ApplyStartZoomIn();
            _startZoomRoutine = null;
        }

        private void ApplyStartZoomIn()
        {
            _preferredCameraZoom = MinCamDistance;
            if (tpCamera == null)
                return;

            EnsureRuntimeZoomState();
            if (tpCamera.currentState != null)
                tpCamera.currentState.defaultDistance = MinCamDistance;
            if (tpCamera.lerpState != null && !IsAimCameraStateName(tpCamera.lerpState.Name))
                tpCamera.lerpState.defaultDistance = MinCamDistance;

            tpCamera.ForceSetZoomDistance(MinCamDistance);
        }

        protected override void Update()
        {
            if (GameplayWorldSimulation.IsFrozen)
                return;

            TrackBlockPress();
            TryDrawWeaponOnAimPress();

            // Pad LT can be missed if AimInput runs while Player map was left disabled — nudge ADS early.
            if (!GameplayKeyboardShortcuts.IsGameplayInputLockedByUi() && ReadAimHeld())
            {
                if (_equipment == null || _equipment.IsWeaponDrawn)
                    isAimingByInput = true;
            }

            base.Update();
            SyncPioneerCursorState();
            PollFollowCameraZoom();

            if (_locomotionGait == null)
                _locomotionGait = GetComponent<DMLocomotionGaitController>();
            _locomotionGait?.TickLocomotion();
            RestoreJumpHeightIfJumpFinished();
        }

        /// <summary>
        /// While held, keep aim active so mining resource scan (F / LB) can force aim without RMB/LT.
        /// Cleared when the scan key is released.
        /// </summary>

        private DMCameraProfile ResolveCameraProfile()
        {
            if (cameraProfile != null)
                return cameraProfile;
            return DMCameraProfile.LoadOrNull();
        }

        private float MinCamDistance
        {
            get
            {
                DMCameraProfile p = ResolveCameraProfile();
                return p != null ? p.minDistance : runtimeMinCameraDistance;
            }
        }

        private float MaxCamDistance
        {
            get
            {
                DMCameraProfile p = ResolveCameraProfile();
                return p != null ? p.maxDistance : runtimeMaxCameraDistance;
            }
        }

        private float DefaultCamDistance
        {
            get
            {
                DMCameraProfile p = ResolveCameraProfile();
                return p != null ? p.defaultDistance : runtimeDefaultCameraDistance;
            }
        }

        private int ZoomLevels
        {
            get
            {
                DMCameraProfile p = ResolveCameraProfile();
                return p != null ? Mathf.Max(2, p.zoomClickLevels) : DefaultZoomClickLevels;
            }
        }

        private float AimZoomPullIn
        {
            get
            {
                DMCameraProfile p = ResolveCameraProfile();
                return p != null ? p.aimZoomPullInMeters : aimZoomPullInMeters;
            }
        }

        private float SprintZoomOut
        {
            get
            {
                DMCameraProfile p = ResolveCameraProfile();
                return p != null ? p.sprintZoomOutMeters : sprintZoomOutMeters;
            }
        }

        private float AimMinCamDistance
        {
            get
            {
                DMCameraProfile p = ResolveCameraProfile();
                return p != null ? p.aimMinCameraDistance : aimMinCameraDistance;
            }
        }

        public void SetMiningScanAimHold(bool held)
        {
            _miningScanAimHold = held;
            if (held && cc != null && !cc.ragdolled && CurrentActiveWeapon != null)
                isAimingByInput = true;
            else if (!held && (aimInput == null || !aimInput.GetButton()))
                isAimingByInput = false;
        }

        public override void AimInput()
        {
            if (cc == null || cc.ragdolled)
            {
                isAimingByInput = false;
                return;
            }

            bool opticsOpen = _playerController != null && _playerController.IsOpticsOpen;
            bool weaponDrawn = _equipment == null || _equipment.IsWeaponDrawn;
            bool drawnMeleeOnly = IsDrawnMeleeWeaponActive();
            bool rmbAimHeld = !opticsOpen
                && !drawnMeleeOnly
                && Mouse.current != null
                && Mouse.current.rightButton.isPressed
                && weaponDrawn;
            bool ltAimHeld = !opticsOpen && !drawnMeleeOnly && ReadAimHeld();
            // LT held while sheathed: draw so ADS can engage (press helper only sees wasPressed).
            if (ltAimHeld && !weaponDrawn && _equipment != null)
            {
                TryDrawWeaponOnAimPress();
                if (!_equipment.IsWeaponDrawn)
                {
                    int focusedLocal = DMUiToolkitHotCross.WeaponLocalIndex;
                    if (_equipment.IsWeaponHotbarSlot(focusedLocal)
                        && EquipmentController.IsWeaponItem(_equipment.GetHotbarItem(focusedLocal)))
                    {
                        int weaponSlot = _equipment.GetWeaponSlotIndexForHotbar(focusedLocal);
                        if (weaponSlot >= 0)
                            _equipment.SelectWeaponSlot(weaponSlot);
                    }
                    if (!_equipment.IsWeaponDrawn)
                        _equipment.DrawWeapon();
                }
                weaponDrawn = _equipment.IsWeaponDrawn;
            }

            bool wantAim = !opticsOpen && (_miningScanAimHold || rmbAimHeld || (ltAimHeld && weaponDrawn));

            if (!DMInputSchemeRouter.IsGamepadScheme)
            {
                // KBM: keep Invector GenericInput aim path, then force RMB if Input System saw it.
                base.AimInput();
                if (wantAim)
                    isAimingByInput = true;
                else if (!rmbAimHeld && !_miningScanAimHold)
                {
                    // Leave base result unless we know RMB is up — base already cleared when Mouse1 up.
                }
            }
            else
            {
                // Gamepad: muted aimInput would clear ADS every frame if we called base.
                if (!wantAim)
                {
                    isAimingByInput = false;
                    return;
                }

                if (CurrentActiveWeapon == null && weaponDrawn)
                {
                    PioneerInvectorWeaponBridge bridge = GetComponent<PioneerInvectorWeaponBridge>();
                    bridge?.EnsureDrawnShooterBound();
                }

                isAimingByInput = true;
            }

            if (!isAimingByInput)
                return;

            if (cc.locomotionType == vThirdPersonMotor.LocomotionType.FreeWithStrafe &&
                !cc.lockInStrafe &&
                !cc.isStrafing)
            {
                cc.Strafe();
            }

            if (headTrack != null)
                headTrack.alwaysFollowCamera = true;
        }

        /// <summary>
        /// ADS only from LT/RMB (isAimingByInput). Ignore Invector hipfire-aim so RT fires from the hip
        /// without pulling aim camera/strafe — hold LT to ADS, then RT to fire.
        /// </summary>
        public override bool IsAiming
        {
            get
            {
                if (lockShooterInput)
                    return false;
                if (cc == null || cc.isRolling)
                    return false;
                return isAimingByInput;
            }
        }

        /// <summary>
        /// Right mouse with a sheathed weapon arms it. Ranged weapons additionally begin aiming so the
        /// same press doubles as ready-to-aim; melee weapons are only drawn. When a weapon is already
        /// drawn, right mouse falls through to the base shooter/melee aim handling unchanged.
        /// </summary>
        private void TryDrawWeaponOnAimPress()
        {
            if (_equipment == null || _equipment.IsWeaponDrawn)
                return;

            bool rmbPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool ltPressed = DMPlayerInputActions.WasPressedThisFrame("Aim") || ReadAimPressedThisFrame();
            if (!rmbPressed && !ltPressed)
                return;

            if (!GameSession.HasStarted || Time.timeScale <= 0f)
                return;

            if (_inputBridge != null && _inputBridge.ShouldLockGameplayInput())
                return;

            if (_playerController != null && _playerController.IsOpticsOpen)
                return;

            // Hot Cross Tab focuses a TL weapon without drawing â€” arm that slot on RMB ADS.
            int focusedLocal = DMUiToolkitHotCross.WeaponLocalIndex;
            if (_equipment.IsWeaponHotbarSlot(focusedLocal)
                && EquipmentController.IsWeaponItem(_equipment.GetHotbarItem(focusedLocal)))
            {
                int weaponSlot = _equipment.GetWeaponSlotIndexForHotbar(focusedLocal);
                if (weaponSlot >= 0
                    && (_equipment.ActiveWeaponSlot != weaponSlot || !_equipment.IsWeaponDrawn))
                {
                    _equipment.SelectWeaponSlot(weaponSlot);
                }
            }

            if (!_equipment.IsWeaponDrawn && !_equipment.DrawWeapon())
                return;

            ItemData weapon = _equipment.DrawnWeaponItem ?? _equipment.EquippedItem;
            if (EquipmentController.IsRangedWeaponItem(weapon))
                isAimingByInput = true;
        }

        protected override void LateUpdate()
        {
            if (GameplayWorldSimulation.IsFrozen)
                return;

            if (tpCamera == null)
            {
                PlayerInvectorRuntimeSetup.EnsureThirdPersonCameraRigidbody(gameObject);
                FindCamera();
            }

            base.LateUpdate();
            SyncPioneerCursorState();
            PinAimFollowDistance();
            ApplyStrongMeleeChargePose();
        }

        protected override void CheckAimConditions()
        {
            if (tpCamera == null)
            {
                PlayerInvectorRuntimeSetup.EnsureThirdPersonCameraRigidbody(gameObject);
                FindCamera();
                if (tpCamera == null)
                {
                    aimConditions = false;
                    return;
                }
            }

            base.CheckAimConditions();
        }

        public override void ReloadInput()
        {
            if (cc == null || cc.customAction || cc.ragdolled)
                return;

            if (_inputBridge != null && _inputBridge.ShouldLockGameplayInput())
                return;

            // WeaponModeSwitchController owns R tap/hold via Input System Update.
            // Keep auto-reload only here; do not fire GenericInput R (legacy Input).
            WeaponModeSwitchController modeSwitch = GetComponent<WeaponModeSwitchController>();

            if (!shooterManager || CurrentActiveWeapon == null || isReloading || shooterManager.isShooting)
                return;

            if (modeSwitch == null && reloadInput.GetButtonDown())
            {
                PerformManualReload();
                return;
            }

            PioneerInvectorAmmoBridge ammoBridge = GetComponent<PioneerInvectorAmmoBridge>();

            // Pioneer ammo bridge: manual R reload only (dry-fire shows RELOAD toast).
            if (ammoBridge != null)
                return;

            if (CurrentActiveWeapon.autoReload && !shooterManager.WeaponHasLoadedAmmo())
            {
                bool canAutoReload = ammoBridge == null
                    ? shooterManager.WeaponHasUnloadedAmmo()
                    : ammoBridge.TryRequestReload(playEmptyDenyFeedback: false);

                if (!canAutoReload)
                    return;

                switch (CurrentActiveWeapon.autoReloadStyle)
                {
                    case vShooterWeapon.AutoReloadStyle.WhenAiming:
                        if (IsAiming)
                            shooterManager.ReloadWeapon();
                        break;
                    case vShooterWeapon.AutoReloadStyle.WhenShot:
                        if (shotInput.GetButtonDown())
                            shooterManager.ReloadWeapon();
                        break;
                    case vShooterWeapon.AutoReloadStyle.WhenAmmoAvailable:
                        shooterManager.ReloadWeapon();
                        break;
                }
            }
        }

        /// <summary>
        /// Called by WeaponModeSwitchController when R is released without opening Mode Switch.
        /// Works even when lockShooterInput skipped Invector ReloadInput this frame.
        /// Does not use UI-pointer soft-lock (hotbar hover must not eat R).
        /// </summary>
        public void RequestManualReloadFromModeSwitch()
        {
            if (cc == null || cc.customAction || cc.ragdolled)
                return;

            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null && (pc.BlocksCombatInput || pc.IsGameplayPaused))
                return;

            if (!shooterManager || isReloading || shooterManager.isShooting)
                return;

            if (CurrentActiveWeapon == null)
            {
                GetComponent<PioneerInvectorWeaponBridge>()?.EnsureDrawnShooterBound();
                if (CurrentActiveWeapon == null)
                {
                    EquipmentController eq = GetComponent<EquipmentController>();
                    ItemData drawn = eq != null ? eq.DrawnWeaponItem : null;
                    if (drawn == null || !drawn.IsRangedWeapon)
                        return;
                }
            }

            PerformManualReload();
        }

        private void PerformManualReload()
        {
            shootCountA = 0;
            _aimTiming = 0f;

            PioneerInvectorAmmoBridge ammoBridge = GetComponent<PioneerInvectorAmmoBridge>();

            // Invector ReloadWeapon always proceeds while isInfinityAmmo is set â€” gate on Pioneer
            // reserve ammo first, and play empty-deny SFX/head-shake instead of reload anim.
            if (ammoBridge != null)
            {
                if (ammoBridge.TryRequestReload(playEmptyDenyFeedback: true))
                    shooterManager.ReloadWeapon();
                return;
            }

            shooterManager.ReloadWeapon();
        }

        private void SyncPioneerCursorState()
        {
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();
            _playerController?.ApplyCursorState();
        }

        /// <summary>
        /// Invector uses inverted semantics (LockCursor(false) = lock). Pioneer owns cursor when menus are open.
        /// </summary>
        public override void LockCursor(bool value)
        {
            SyncPioneerCursorState();
        }

        public override void ShowCursor(bool value)
        {
            SyncPioneerCursorState();
        }

        public bool IsAimingActive
        {
            get { return isAimingByInput; }
        }

        public override void CrouchInput()
        {
            if (DMInputSchemeRouter.IsGamepadScheme)
                return;

            base.CrouchInput();
        }

        public override void MoveInput()
        {
            if (lockMoveInput || cc == null || !CanReadGameplayInput())
                return;

            Vector2 move = ReadMoveVector();
            Vector3 input = cc.input;
            input.x = move.x;
            input.z = move.y;
            cc.input = input;
            cc.ControlKeepDirection();
        }

        public override void SprintInput()
        {
            if (!sprintInput.useInput || cc == null || !CanReadGameplayInput())
                return;

            if (_locomotionGait == null)
                _locomotionGait = GetComponent<DMLocomotionGaitController>();

            // DMLocomotionGaitController ticks gaits in Update; skip Invector Sprint() when it owns input.
            if (_locomotionGait != null)
                return;

            if (Keyboard.current == null)
                return;

            if (cc.useContinuousSprint)
                cc.Sprint(Keyboard.current.leftShiftKey.wasPressedThisFrame);
            else
                cc.Sprint(Keyboard.current.leftShiftKey.isPressed);
        }

        public override void JumpInput()
        {
            // jumpInput GenericInput is muted on Gamepad scheme — still poll Space/A / Jump action.
            if (cc == null || !CanReadGameplayInput())
                return;

            if (_climb == null)
                _climb = GetComponent<DMClimbController>();

            if (_climb != null && _climb.TryHandleJumpPress())
                return;

            if (_jetpackInputBridge != null && _jetpackInputBridge.TryHandleJumpPress())
                return;

            if (!ReadJumpPressedThisFrame())
                return;

            if (_landingDirector == null)
                _landingDirector = GetComponent<DMLandingDirector>();
            if (_landingDirector != null && _landingDirector.SuppressJumpStart)
                return;

            if (!JumpConditions())
                return;

            ApplyGaitJumpHeightForThisJump();
            bool movingJump = cc.input.sqrMagnitude >= 0.01f;
            cc.Jump(true);
            if (movingJump)
                ApplyMoveJumpTakeoffImpulse();
        }

        private void ApplyGaitJumpHeightForThisJump()
        {
            DM_ClimbDashProfile profile = DM_ClimbDashProfile.Resolve(null);
            if (profile == null || cc == null)
                return;

            if (_locomotionGait == null)
                _locomotionGait = GetComponent<DMLocomotionGaitController>();

            bool moving = cc.input.sqrMagnitude >= 0.01f;
            DMLocomotionGaitController.Gait gait = _locomotionGait != null
                ? _locomotionGait.CurrentGait
                : DMLocomotionGaitController.Gait.SlowWalk;

            float speed01 = 0f;
            if (moving && cc.freeSpeed.sprintSpeed > 0.01f)
                speed01 = Mathf.Clamp01(cc.moveSpeed / cc.freeSpeed.sprintSpeed);

            float target = profile.ResolveJumpHeight(gait, moving, speed01);
            if (float.IsNaN(_jumpHeightBeforeOverride))
                _jumpHeightBeforeOverride = cc.jumpHeight;

            cc.jumpHeight = target;
        }

        private void RestoreJumpHeightIfJumpFinished()
        {
            if (cc == null || float.IsNaN(_jumpHeightBeforeOverride))
                return;
            if (cc.isJumping || cc.inJumpStarted)
                return;

            cc.jumpHeight = _jumpHeightBeforeOverride;
            _jumpHeightBeforeOverride = float.NaN;
        }

        /// <summary>JumpMove sets isJumping before the first FixedUpdate; ensure lift same frame (landing director must not cancel isJumping).</summary>
        private void ApplyMoveJumpTakeoffImpulse()
        {
            if (cc == null || !cc.isJumping)
                return;

            Rigidbody rb = cc.GetComponent<Rigidbody>();
            if (rb == null || rb.isKinematic)
                return;

            float targetY = cc.jumpHeight * cc.jumpMultiplier;
            if (targetY <= 0.01f)
                return;

            Vector3 vel = rb.linearVelocity;
            if (vel.y < targetY)
            {
                vel.y = targetY;
                rb.linearVelocity = vel;
            }
        }

        public override void InputHandle()
        {
            // Before melee conditions. BlockingInput is skipped while the base layer blends,
            // and a tap in that window never opened the parry timer.
            TrackBlockPress();
            base.InputHandle();
            ApplyDrawnMeleeBlockInput();
            UpdateDrawnMeleeCharge();
        }

        /// <summary>
        /// Drawn sword: left mouse / Attack is a tap-or-hold. Press does not swing.
        /// Release before the charge threshold is the light attack. Release after it is the strong sword swing.
        /// Once the threshold is reached the charge pose holds until release. There is no max hold and no auto-swing.
        /// Right mouse is unchanged and stays block / parry.
        /// </summary>
        public override void MeleeWeakAttackInput()
        {
            if (IsDrawnMeleeWeaponActive())
                return;

            base.MeleeWeakAttackInput();
        }

        public bool IsStrongMeleeDamageActive =>
            _strongDamageArmed && Time.time <= _strongDamageUntil;

        public override void BlockingInput()
        {
            if (animator == null || cc == null)
                return;

            if (Time.time < _suppressBlockUntil)
            {
                isBlocking = false;
                return;
            }

            isBlocking = ReadBlockHeld() && cc.currentStamina > 0 && !cc.customAction && !isAttacking;
        }

        private void UpdateDrawnMeleeCharge()
        {
            TickStrongDamageWindow();

            if (!CanTrackDrawnMeleeCharge())
            {
                _lightAttackHeldLast = false;
                _strongChargeArmed = false;
                return;
            }

            bool held = ReadLightAttackHeld();
            bool pressed = ReadLightAttackPressedThisFrame() || (held && !_lightAttackHeldLast);
            bool released = !held && _lightAttackHeldLast;
            float chargeSeconds = ResolveStrongChargeSeconds();

            // A click that goes down and up inside one frame never shows a held sample.
            if (pressed && !held)
            {
                _strongChargeArmed = false;
                _lightAttackHeldLast = false;
                TryReleaseLightMelee();
                return;
            }

            if (pressed)
            {
                _lightAttackHoldStart = Time.time;
                _strongChargeArmed = false;
            }

            if (held && Time.time - _lightAttackHoldStart >= chargeSeconds)
            {
                if (!_strongChargeArmed)
                    _chargeArmedTime = Time.time;
                _strongChargeArmed = true;
            }

            if (released)
            {
                if (_strongChargeArmed)
                    TryReleaseStrongMelee();
                else
                    TryReleaseLightMelee();
                _strongChargeArmed = false;
            }

            _lightAttackHeldLast = held;
        }

        private bool CanTrackDrawnMeleeCharge()
        {
            if (cc == null || cc.isDead || lockInput || lockMeleeInput)
                return false;
            if (!IsDrawnMeleeWeaponActive())
                return false;
            if (IsAiming || isReloading)
                return false;
            return true;
        }

        private void TryReleaseLightMelee()
        {
            if (!CanReleaseDrawnMelee())
                return;

            RestoreStrongChargePose();
            if (!_strongDamageSawSwing)
                _strongDamageArmed = false;
            animator.ResetTrigger(vAnimatorParameters.StrongAttack);
            TriggerWeakAttack();
        }

        private void TryReleaseStrongMelee()
        {
            if (!CanReleaseDrawnMelee())
                return;

            // A held block keeps FullBody in Defense, and StrongAttack is only wired from Null.
            // The sword strong clips also referenced a missing FBX, so a trigger into that state showed nothing.
            RestoreStrongChargePose();
            isBlocking = false;
            animator.SetBool(vAnimatorParameters.IsBlocking, false);
            _suppressBlockUntil = Time.time + StrongAttackBlockSuppressSeconds;
            PlayStrongSwordSwing();
            _strongDamageArmed = true;
            _strongDamageSawSwing = false;
            _strongDamageUntil = Time.time + StrongDamageWindowSeconds;
        }

        /// <summary>
        /// CrossFades FullBody into the sword strong swing. A StrongAttack trigger only leaves Null,
        /// so a block pose or an in-progress full-body transition ate it and the swing never started.
        /// </summary>
        private void PlayStrongSwordSwing()
        {
            int layer = cc != null ? cc.fullbodyLayer : -1;
            if (layer < 0)
                layer = animator.GetLayerIndex("FullBody");

            animator.ResetTrigger(vAnimatorParameters.WeakAttack);
            animator.ResetTrigger(vAnimatorParameters.StrongAttack);
            int attackId = AttackID;
            if (attackId <= 0)
                attackId = 1;
            animator.SetInteger(vAnimatorParameters.AttackID, attackId);

            if (layer >= 0 && animator.HasState(layer, StrongSwordStateHash))
            {
                animator.SetLayerWeight(layer, 1f);
                animator.CrossFadeInFixedTime(StrongSwordStateHash, 0.08f, layer, 0f);
                return;
            }

            TriggerStrongAttack();
        }

        /// <summary>
        /// Invector@ShooterMelee_Jetpack has no charge or ready clip. After the charge threshold,
        /// ease the drawn weapon back in the hand and keep that pose until release.
        /// </summary>
        private void ApplyStrongMeleeChargePose()
        {
            bool holding = _strongChargeArmed
                && CanTrackDrawnMeleeCharge()
                && ReadLightAttackHeld()
                && !isAttacking;
            if (!holding)
            {
                RestoreStrongChargePose();
                return;
            }

            Transform weapon = meleeManager != null && meleeManager.rightWeapon != null
                ? meleeManager.rightWeapon.transform
                : null;
            if (weapon == null)
            {
                RestoreStrongChargePose();
                return;
            }

            if (!_chargePoseCaptured || _chargePoseTransform != weapon)
            {
                _chargePoseTransform = weapon;
                _chargePoseRestPosition = weapon.localPosition;
                _chargePoseRestRotation = weapon.localRotation;
                _chargePoseCaptured = true;
            }

            float ease = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - _chargeArmedTime) / 0.12f));
            weapon.localPosition = _chargePoseRestPosition + StrongChargeLocalOffset * ease;
            weapon.localRotation = Quaternion.Slerp(
                _chargePoseRestRotation,
                _chargePoseRestRotation * Quaternion.Euler(StrongChargeEuler),
                ease);
        }

        private void RestoreStrongChargePose()
        {
            if (_chargePoseCaptured && _chargePoseTransform != null)
            {
                _chargePoseTransform.localPosition = _chargePoseRestPosition;
                _chargePoseTransform.localRotation = _chargePoseRestRotation;
            }

            _chargePoseCaptured = false;
            _chargePoseTransform = null;
        }

        private bool CanReleaseDrawnMelee()
        {
            if (animator == null || meleeManager == null)
                return false;
            if (!MeleeAttackStaminaConditions())
                return false;
            if (!cc.isGrounded || cc.customAction || cc.isJumping || cc.isCrouching || cc.isRolling || isEquipping)
                return false;
            return true;
        }

        private void TickStrongDamageWindow()
        {
            if (!_strongDamageArmed)
                return;

            if (isAttacking)
                _strongDamageSawSwing = true;

            if ((_strongDamageSawSwing && !isAttacking) || Time.time > _strongDamageUntil)
                _strongDamageArmed = false;
        }

        private static float ResolveStrongChargeSeconds()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float seconds = profile != null ? profile.strongMeleeChargeSeconds : 0.4f;
            return Mathf.Clamp(seconds, 0.2f, 0.8f);
        }

        private static bool ReadLightAttackHeld()
        {
            if (DMPlayerInputActions.IsPressed("Attack"))
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;
            if (Gamepad.current != null && Gamepad.current.rightShoulder.isPressed)
                return true;
            return false;
        }

        private static bool ReadLightAttackPressedThisFrame()
        {
            if (DMPlayerInputActions.WasPressedThisFrame("Attack"))
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame)
                return true;
            return false;
        }

        /// <summary>
        /// Records the block press every InputHandle, including animator transitions.
        /// BlockingInput itself is skipped while the base layer blends.
        /// </summary>
        private void TrackBlockPress()
        {
            bool blockHeld = ReadBlockHeld();
            if (ReadBlockPressedThisFrame() || (blockHeld && !_wasBlockHeldLastFrame))
                _lastBlockPressTime = Time.time;
            _wasBlockHeldLastFrame = blockHeld;
        }

        private bool IsParryWindowOpen()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float parryWindow = profile != null ? profile.parryWindowSeconds : 0.2f;
            if (parryWindow <= 0f)
                return false;

            // Same-frame taps: the hit can land in physics before Update records the press.
            if (ReadBlockPressedThisFrame())
                return true;

            return Time.time - _lastBlockPressTime <= parryWindow;
        }

        private bool IsSuccessfulMeleeBlock(vDamage damage)
        {
            return damage != null
                && !damage.ignoreDefense
                && isBlocking
                && meleeManager != null
                && damage.sender != null
                && meleeManager.CanBlockAttack(damage.sender.position);
        }

        /// <summary>
        /// A successful block or parry clears the hit. GetDefenseRate is 0 for OnlyAttack
        /// weapons, and Invector only reduces when the rate is above 0, so SurvivalStats
        /// was copying the full damageValue from onStartReceiveDamage.
        /// </summary>
        public bool TryAbsorbBlockedMelee(vDamage damage)
        {
            if (damage == null || damage.damageValue <= 0f)
                return false;
            if (!IsSuccessfulMeleeBlock(damage))
                return false;

            // GetDefenseRate() skips OnlyAttack weapons (returns 0), and Invector only
            // calls ReduceDamage when the rate is above 0. A non-zero rate still left
            // chip damage in SurvivalStats. A landed block or parry negates the hit.
            damage.damageValue = 0f;
            damage.hitReaction = false;
            return true;
        }

        public override void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)
        {
            bool successfulBlock = IsSuccessfulMeleeBlock(damage);
            bool isParry = successfulBlock && IsParryWindowOpen();
            Transform damageSender = damage != null ? damage.sender : null;
            if (successfulBlock)
                TryAbsorbBlockedMelee(damage);

            base.OnReceiveAttack(damage, attacker);

            if (!successfulBlock)
                return;

            PlayGuardImpactReaction();
            DMEnemyGuardBreakStagger.TryApplyFromBlock(attacker, transform, isParry, damageSender);
        }

        /// <summary>
        /// Short guard-impact pose. Block and parry use the same mild pose.
        /// Does not call OnRecoil — that fires ResetState and can drop the block.
        /// RecoilID 1 is the mild unarmed recoil. RecoilID 2 is the stronger low recoil
        /// and is not used here. RecoilID above 2 is recoil_hard, tagged CustomAction,
        /// which clears isBlocking. This path does not freeze animator speed.
        /// </summary>
        private void PlayGuardImpactReaction()
        {
            if (animator == null)
                return;

            animator.SetBool(vAnimatorParameters.IsBlocking, true);
            animator.SetInteger(vAnimatorParameters.DefenseID, DefenseID);
            animator.SetInteger(vAnimatorParameters.RecoilID, 1);
            animator.ResetTrigger(vAnimatorParameters.TriggerRecoil);
            animator.SetTrigger(vAnimatorParameters.TriggerRecoil);

            if (_guardImpactRoutine != null)
                StopCoroutine(_guardImpactRoutine);

            _guardImpactRoutine = StartCoroutine(ReleaseGuardImpactRoutine(BlockGuardPoseSeconds));
        }

        private IEnumerator ReleaseGuardImpactRoutine(float holdSeconds)
        {
            yield return new WaitForSeconds(holdSeconds);
            _guardImpactRoutine = null;

            if (animator == null || !isBlocking)
                yield break;

            int layer = cc != null ? cc.fullbodyLayer : animator.GetLayerIndex("FullBody");
            if (layer >= 0)
                animator.CrossFadeInFixedTime("Null", 0.06f, layer, 0f);

            animator.SetBool(vAnimatorParameters.IsBlocking, true);
        }

        private void ApplyDrawnMeleeBlockInput()
        {
            if (cc == null || cc.isDead || cc.ragdolled || lockInput || lockMeleeInput)
                return;
            if (!IsDrawnMeleeWeaponActive())
                return;
            // MeleeAttackConditions also rejects base-layer blends. A parry tap lands in that
            // blend, so block state has to update here or the hit stays a late regular block.
            if (meleeManager == null || !cc.isGrounded || cc.customAction || cc.isJumping || cc.isCrouching || cc.isRolling || isEquipping)
                return;

            BlockingInput();
        }

        private bool IsDrawnMeleeWeaponActive()
        {
            if (_equipment == null || !_equipment.IsWeaponDrawn)
                return false;

            ItemData drawn = _equipment.DrawnWeaponItem;
            return drawn != null && drawn.itemType == ItemType.MeleeWeapon;
        }

        private static bool ReadBlockHeld()
        {
            if (DMPlayerInputActions.IsPressed("Block"))
                return true;
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
                return true;
            if (Gamepad.current != null)
            {
                if (Gamepad.current.leftTrigger.ReadValue() >= 0.2f)
                    return true;
                if (Gamepad.current.leftShoulder.isPressed)
                    return true;
            }

            return false;
        }

        private static bool ReadBlockPressedThisFrame()
        {
            if (DMPlayerInputActions.WasPressedThisFrame("Block"))
                return true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null)
            {
                if (Gamepad.current.leftShoulder.wasPressedThisFrame)
                    return true;
                if (Gamepad.current.leftTrigger.wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        private static bool ReadJumpPressedThisFrame()
        {
            if (DMPlayerInputActions.WasPressedThisFrame("Jump"))
                return true;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
            return false;
        }

        public override void CameraInput()
        {
            if (!cameraMain || !CanReadCameraInput())
                return;

            float x = 0f;
            float y = 0f;
            bool fromMouse = false;
            if (!lockCameraInput)
                ReadSchemeGatedLookDelta(out x, out y, out fromMouse);

            if (invertCameraInputHorizontal)
                x *= -1f;

            if (invertCameraInputVertical)
                y *= -1f;

            if (_playerController != null && _playerController.IsBinocularCameraFrozen)
            {
                ApplyBinocularDirectLook(x, y);
                return;
            }

            if (tpCamera == null)
                return;

            EnsureRuntimeZoomState();

            // Sticky Gamepad scheme sets vInput to Joystick — mouse deltas must NOT use joystickSensitivity
            // or ADS+fire hitches (huge delta) violently spin the camera.
            bool restoreDevice = false;
            InvInputDevice previousDevice = InvInputDevice.MouseKeyboard;
            if (fromMouse && vInput.instance != null)
            {
                previousDevice = vInput.instance.inputDevice;
                restoreDevice = true;
                vInput.instance.inputDevice = InvInputDevice.MouseKeyboard;
            }

            ApplyBuildModeLookUp();
            tpCamera.RotateCamera(x, y);

            if (restoreDevice && vInput.instance != null)
                vInput.instance.inputDevice = previousDevice;
        }

        /// <summary>
        /// Invector look-up is the negative pitch (yMinLimit). Build mode lowers that limit and restores it on exit.
        /// yMaxLimit is look-down and is left alone. The saved camera asset is restored when build mode ends.
        /// </summary>
        void ApplyBuildModeLookUp()
        {
            if (tpCamera == null || tpCamera.lerpState == null)
                return;

            if (Project.Building.DMBuildingMode.IsActive)
            {
                if (!_buildLookRaised)
                {
                    _savedLookUpLimit = tpCamera.lerpState.yMinLimit;
                    _savedLookDownLimit = tpCamera.lerpState.yMaxLimit;
                    _savedBuildZoom = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : GetPreferredZoom();
                    _savedBuildZoomMax = tpCamera.lerpState.maxDistance;
                    if (_savedLookUpLimit > -5f)
                        _savedLookUpLimit = -40f;
                    if (_savedLookDownLimit < 5f)
                        _savedLookDownLimit = 80f;
                    _buildLookRaised = true;

                    float cameraMul = Project.Building.DMBuildingGhostProfile.BuildModeCameraDistanceMultiplier;
                    float cameraExtra = Project.Building.DMBuildingGhostProfile.BuildModeCameraExtraMeters;
                    float zoom = Mathf.Clamp(
                        Mathf.Max(_savedBuildZoom * cameraMul, _savedBuildZoom + cameraExtra),
                        _savedBuildZoom,
                        _savedBuildZoomMax > 0.01f ? _savedBuildZoomMax : MaxCamDistance);
                    float zoomMax = Mathf.Max(_savedBuildZoomMax > 0.01f ? _savedBuildZoomMax : MaxCamDistance, zoom);
                    tpCamera.lerpState.maxDistance = zoomMax;
                    if (tpCamera.currentState != null)
                        tpCamera.currentState.maxDistance = zoomMax;
                    tpCamera.ForceSetZoomDistance(zoom);
                }

                float lookExtra = Project.Building.DMBuildingGhostProfile.BuildLookUpDegrees;
                float lookUp = Mathf.Max(-89f, _savedLookUpLimit - lookExtra);
                float lookDown = Mathf.Min(89f, _savedLookDownLimit + lookExtra);
                tpCamera.lerpState.yMinLimit = lookUp;
                tpCamera.lerpState.yMaxLimit = lookDown;
                if (tpCamera.currentState != null)
                {
                    tpCamera.currentState.yMinLimit = lookUp;
                    tpCamera.currentState.yMaxLimit = lookDown;
                }
                return;
            }

            if (!_buildLookRaised)
                return;

            RestoreBuildModeLookUp();
        }

        void RestoreBuildModeLookUp()
        {
            if (!_buildLookRaised || tpCamera == null)
            {
                _buildLookRaised = false;
                return;
            }

            if (tpCamera.currentState != null)
            {
                tpCamera.currentState.yMinLimit = _savedLookUpLimit;
                tpCamera.currentState.yMaxLimit = _savedLookDownLimit;
                if (_savedBuildZoomMax > 0.01f)
                    tpCamera.currentState.maxDistance = _savedBuildZoomMax;
            }
            if (tpCamera.lerpState != null)
            {
                tpCamera.lerpState.yMinLimit = _savedLookUpLimit;
                tpCamera.lerpState.yMaxLimit = _savedLookDownLimit;
                if (_savedBuildZoomMax > 0.01f)
                    tpCamera.lerpState.maxDistance = _savedBuildZoomMax;
            }
            if (_savedBuildZoom > 0.01f)
                tpCamera.ForceSetZoomDistance(_savedBuildZoom);
            _savedBuildZoom = -1f;
            _savedBuildZoomMax = -1f;
            _buildLookRaised = false;
        }

        /// <summary>
        /// Single look consumer: KBM uses pointer delta only; gamepad uses right stick only.
        /// Syncs Invector vInput device with PlayerInput scheme (avoids OnGUI stick-drift flapping).
        /// stamp: controller-kbm-look 0920; pad-jump-aim-fire 0920
        /// </summary>
        private void ReadSchemeGatedLookDelta(out float x, out float y, out bool fromMouse)
        {
            x = 0f;
            y = 0f;
            fromMouse = false;

            if (_playerInput == null)
                _playerInput = GetComponent<PlayerInput>();

            if (!_loggedKbmLookStamp && Application.isPlaying)
            {
                _loggedKbmLookStamp = true;
                Debug.Log($"[PioneerShooterMeleeInput] {KbmLookStamp} look+triggers");
            }

            // Mouse delta always wins when present — sticky Gamepad scheme must not brick KBM look.
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                // Clamp hitch spikes (VFX/ADS shot frames can dump 100+ px in one delta).
                const float maxDelta = 48f;
                if (delta.sqrMagnitude > maxDelta * maxDelta)
                    delta = Vector2.ClampMagnitude(delta, maxDelta);

                if (delta.sqrMagnitude >= 0.0001f)
                {
                    fromMouse = true;
                    x = delta.x * MouseLookScale;
                    y = delta.y * MouseLookScale;
                    return;
                }
            }

            // Otherwise right stick from any pad.
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad pad = Gamepad.all[i];
                if (pad == null)
                    continue;
                Vector2 stick = pad.rightStick.ReadValue();
                if (stick.sqrMagnitude < GamepadStickDeadZone * GamepadStickDeadZone)
                    continue;
                x = stick.x * GamepadStickLookScale;
                y = stick.y * GamepadStickLookScale;
                return;
            }
        }

        /// <summary>
        /// Poll follow-distance zoom every render frame. Shooter CameraInput is physics-gated
        /// (updateIK false + Fixed animator), so wheel deltas were dropped while walking.
        /// lockCameraInput stays mouse-look only. Keep useZoom true even when CameraInput skips.
        /// </summary>
        private void PollFollowCameraZoom()
        {
            if (!_loggedScrollStamp)
            {
                _loggedScrollStamp = true;
                // Startup stamp silenced.
            }

            if (tpCamera == null)
                return;

            // Optics / minimap / UI-pause early-outs. Do not gate on lockCameraInput.
            if (!Application.isPlaying || !GameSession.HasStarted || Time.timeScale <= 0f)
                return;

            if (_inputBridge != null && _inputBridge.ShouldLockCameraInput())
                return;

            if (_playerController != null && _playerController.IsBinocularCameraFrozen)
                return;

            EnsureRuntimeZoomState();

            if (Mouse.current == null)
                return;

            bool opticsOwnsScroll = _playerController != null && _playerController.IsOpticsOpen;
            bool minimapOwnsScroll = MapUI.IsMinimapScrollZoomActive;
            if (opticsOwnsScroll || minimapOwnsScroll || DMBuildingMode.IsActive)
                return;

            ApplyMouseWheelZoom();
        }

        private void ApplyMouseWheelZoom()
        {
            if (tpCamera == null || IsAimCameraStateName(tpCamera.currentStateName))
                return;

            float raw = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(raw) < 0.01f)
                return;

            // One physical wheel tick = one of 10 discrete follow-distance levels.
            int direction = raw > 0f ? 1 : -1;
            float range = MaxCamDistance - MinCamDistance;
            float step = range / (ZoomLevels - 1);
            float current = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : GetPreferredZoom();
            int currentLevel = Mathf.RoundToInt((current - MinCamDistance) / step);
            int nextLevel = Mathf.Clamp(currentLevel - direction, 0, ZoomLevels - 1);
            float next = MinCamDistance + nextLevel * step;

            ApplyFreeLookZoomRange(tpCamera.currentState);
            if (tpCamera.lerpState != null && !IsAimCameraStateName(tpCamera.lerpState.Name))
                ApplyFreeLookZoomRange(tpCamera.lerpState);

            tpCamera.ForceSetZoomDistance(next);
            _preferredCameraZoom = next;
            _lockedAimZoom = -1f;
        }

        /// <summary>
        /// Binoculars disable vThirdPersonCamera so follow distance is not overwritten.
        /// Rotate the live gameplay camera directly; best-effort sync tpCamera angles for restore.
        /// </summary>
        private void ApplyBinocularDirectLook(float x, float y)
        {
            if (_playerController == null)
                return;

            _playerController.ApplyBinocularLookDelta(x, y);

            if (tpCamera == null || cameraMain == null)
                return;

            Vector3 normalized = cameraMain.transform.eulerAngles.NormalizeAngle();
            tpCamera.mouseY = normalized.x;
            tpCamera.mouseX = normalized.y;
        }

        private static bool IsAimCameraStateName(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return false;
            return stateName.IndexOf("Aim", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || stateName.IndexOf("Scope", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RememberPreferredZoom()
        {
            if (tpCamera == null)
                return;

            float zoom = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
            if (zoom >= MinCamDistance - 0.05f)
                _preferredCameraZoom = Mathf.Clamp(zoom, MinCamDistance, MaxCamDistance);
        }

        private float GetPreferredZoom()
        {
            if (_preferredCameraZoom >= MinCamDistance - 0.05f)
                return Mathf.Clamp(_preferredCameraZoom, MinCamDistance, MaxCamDistance);
            return DefaultCamDistance;
        }

        private float GetGameplayFollowZoom()
        {
            float preferred = GetPreferredZoom();
            bool sprinting = cc != null && cc.input.sqrMagnitude > 0.01f && !IsAimingActive &&
                (_locomotionGait != null
                    ? _locomotionGait.IsBurstSprinting
                    : cc.isSprinting);
            if (sprinting)
            {
                return Mathf.Clamp(
                    preferred + Mathf.Max(0f, SprintZoomOut),
                    MinCamDistance,
                    MaxCamDistance);
            }

            return preferred;
        }

        /// <summary>
        /// Keep free-look third-person zoom playable without fighting Aim camera states.
        /// Never rewrite Aiming lerpState (shared list refs) or ForceSet from temporary culling dips.
        /// </summary>
        private void EnsureRuntimeZoomState()
        {
            if (tpCamera?.currentState == null)
                return;

            bool uiBlocking = _playerController != null && _playerController.BlocksCombatInput;
            bool aiming = IsAimCameraStateName(tpCamera.currentStateName);

            // Journal / inventory / map can shove follow distance out. Do NOT bake that into preferred.
            if (uiBlocking)
            {
                _wasUiBlockingLastFrame = true;
                _uiZoomRestoreFramesRemaining = UiZoomRestoreFrames;
                return;
            }

            if (_wasUiBlockingLastFrame)
            {
                _wasUiBlockingLastFrame = false;
                RestorePreferredZoom(force: true);
            }

            if (_uiZoomRestoreFramesRemaining > 0)
            {
                _uiZoomRestoreFramesRemaining--;
                RestorePreferredZoom(force: true);
            }

            // Only repair a permanently broken zoom (e.g. optics left near-zero).
            // Do NOT ForceSet when distance alone dips from wall culling â€” that wiped scroll zoom.
            if (!aiming
                && tpCamera.CurrentZoom < MinCamDistance - 0.01f
                && tpCamera.distance < MinCamDistance - 0.01f)
            {
                tpCamera.ForceSetZoomDistance(GetGameplayFollowZoom());
                return;
            }

            if (aiming)
            {
                if (!_wasAimingCameraLastFrame || _lockedAimZoom < AimMinCamDistance - 0.05f)
                    LockAimZoomOnce();

                _wasAimingCameraLastFrame = true;
                return;
            }

            TrackArmedCameraZoom();

            _lockedAimZoom = -1f;

            // Free-look only: enable scroll zoom range without mutating Aim list assets via lerpState.
            ApplyFreeLookZoomRange(tpCamera.currentState);
            if (tpCamera.lerpState != null && !IsAimCameraStateName(tpCamera.lerpState.Name))
                ApplyFreeLookZoomRange(tpCamera.lerpState);

            // One-shot restore when leaving aim â€” pull back to the player's scroll preference.
            if (_wasAimingCameraLastFrame)
            {
                _wasAimingCameraLastFrame = false;
                RestorePreferredZoom(force: true);
            }

            // Slight sprint pull-out only (never jump to max distance).
            bool sprinting = cc != null && cc.input.sqrMagnitude > 0.01f &&
                (_locomotionGait != null
                    ? _locomotionGait.IsBurstSprinting
                    : cc.isSprinting);
            if (sprinting || _wasSprintingLastFrame)
            {
                float target = GetGameplayFollowZoom();
                if (Mathf.Abs(tpCamera.CurrentZoom - target) > 0.04f)
                    tpCamera.ForceSetZoomDistance(target);
            }

            _wasSprintingLastFrame = sprinting;
        }

        private void LockAimZoomOnce()
        {
            if (tpCamera?.currentState == null || !IsAimCameraStateName(tpCamera.currentStateName))
                return;

            ItemData weapon = ResolveAimZoomWeaponItem();

            float currentDistance = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
            float armedBaseline = _lastArmedCameraZoom > AimMinCamDistance
                ? _lastArmedCameraZoom
                : currentDistance;

            if (armedBaseline < AimMinCamDistance)
                armedBaseline = Mathf.Max(AimMinCamDistance, GetPreferredZoom());

            _lockedAimZoom = GetWeaponAimTargetDistance(weapon, armedBaseline);

            vThirdPersonCameraState state = tpCamera.currentState;
            // Keep useZoom so CameraMovement lerps toward currentZoom instead of
            // Slerping defaultDistance from the Aim list asset (that fight jittered ADS).
            state.useZoom = true;
            state.minDistance = AimMinCamDistance;
            state.maxDistance = Mathf.Max(_lockedAimZoom + 0.25f, AimMinCamDistance);

            float baselineFov = GetArmedBaselineFov();
            float aimFovMultiplier = GetWeaponAimFovMultiplier(weapon);
            state.fov = Mathf.Clamp(baselineFov * aimFovMultiplier, 34f, baselineFov);

            tpCamera.SetZoomTarget(_lockedAimZoom);
        }

        private void TrackArmedCameraZoom()
        {
            if (tpCamera == null || CurrentActiveWeapon == null)
                return;

            float zoom = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
            if (zoom >= AimMinCamDistance - 0.05f)
                _lastArmedCameraZoom = zoom;
        }

        private void PinAimFollowDistance()
        {
            if (tpCamera?.currentState == null || !IsAimCameraStateName(tpCamera.currentStateName))
                return;

            if (_lockedAimZoom < AimMinCamDistance - 0.01f)
                return;

            // Correct target only â€” do not snap distance (ForceSet fought wall-cull lerp).
            float live = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : tpCamera.distance;
            if (live > _lockedAimZoom + 0.12f)
                tpCamera.SetZoomTarget(_lockedAimZoom);
        }

        private ItemData ResolveAimZoomWeaponItem()
        {
            if (_equipment == null)
                return null;

            ItemData drawn = _equipment.DrawnWeaponItem;
            if (drawn != null && drawn.IsRangedWeapon)
                return drawn;

            return _equipment.EquippedItem;
        }

        private float GetWeaponAimTargetDistance(ItemData weapon, float armedDistance)
        {
            armedDistance = Mathf.Max(armedDistance, AimMinCamDistance);

            if (weapon != null && weapon.weaponGrip == WeaponGrip.TwoHanded)
            {
                return Mathf.Clamp(
                    armedDistance * 0.68f,
                    AimMinCamDistance,
                    armedDistance - 0.1f);
            }

            if (weapon != null && EquipmentController.IsRangedWeaponItem(weapon))
            {
                return Mathf.Clamp(
                    armedDistance * 0.82f,
                    AimMinCamDistance,
                    armedDistance - 0.08f);
            }

            return Mathf.Clamp(
                armedDistance - Mathf.Max(0.12f, AimZoomPullIn * 0.35f),
                AimMinCamDistance,
                armedDistance - 0.05f);
        }

        private static float GetWeaponAimFovMultiplier(ItemData weapon)
        {
            if (weapon != null && weapon.aimFovMultiplier > 0.01f)
                return weapon.aimFovMultiplier;

            return 0.78f;
        }

        private static float GetArmedBaselineFov()
        {
            return 60f;
        }

        private void SoftenAimCameraDistance()
        {
            LockAimZoomOnce();
        }

        private void RestorePreferredZoom(bool force)
        {
            if (tpCamera == null)
                return;

            // Always restore the player's scroll preference â€” never a UI-bloated follow distance.
            float preferred = GetPreferredZoom();
            if (_preferredCameraZoom < MinCamDistance - 0.05f)
                preferred = DefaultCamDistance;

            if (force || Mathf.Abs(tpCamera.CurrentZoom - preferred) > 0.05f)
                tpCamera.ForceSetZoomDistance(preferred);
        }

        private void ApplyFreeLookZoomRange(vThirdPersonCameraState state)
        {
            if (state == null)
                return;

            state.useZoom = true;
            state.minDistance = MinCamDistance;
            state.maxDistance = Mathf.Max(MaxCamDistance, state.maxDistance, GetPreferredZoom());
            if (state.defaultDistance < MinCamDistance
                || state.defaultDistance > MaxCamDistance * 1.5f)
            {
                state.defaultDistance = Mathf.Clamp(
                    state.defaultDistance > 0.01f ? state.defaultDistance : DefaultCamDistance,
                    MinCamDistance,
                    state.maxDistance);
            }
        }


        public override void ShotInput()
        {
            if (Project.Building.DMBuildingMode.IsActive)
            {
                shootCountA = 0;
                return;
            }

            // shotInput GenericInput is muted on Gamepad — drive HandleShotCount from RT / Attack / LMB.
            if (!shooterManager || CurrentActiveWeapon == null || cc == null || cc.isDead || isReloading || isAttacking || isEquipping)
            {
                if (shooterManager && CurrentActiveWeapon != null && CurrentActiveWeapon.chargeWeapon && CurrentActiveWeapon.powerCharge != 0)
                    CurrentActiveWeapon.powerCharge = 0;
                shootCountA = 0;
                return;
            }

            bool fireHeld = ReadShotHeld();
            var weapon = shooterManager.CurrentWeapon != null ? shooterManager.CurrentWeapon : CurrentActiveWeapon;

            if (IsAiming && !shooterManager.isShooting && aimConditions)
            {
                if (weapon != null)
                    HandleShotCount(weapon, fireHeld);
            }
            else if (!IsAiming)
            {
                // Hip fire on RT/LMB even if the Invector hipfireShot checkbox was left off.
                if (fireHeld && weapon != null)
                    HandleShotCount(weapon, fireHeld);
                else
                {
                    if (CurrentActiveWeapon != null && CurrentActiveWeapon.chargeWeapon && CurrentActiveWeapon.powerCharge != 0)
                        CurrentActiveWeapon.powerCharge = 0;
                    shootCountA = 0;
                }
            }
        }

        private static bool ReadAimPressedThisFrame()
        {
            return ReadTriggerPressedThisFrame(left: true);
        }

        private static bool ReadAimHeld()
        {
            if (DMPlayerInputActions.IsPressed("Aim"))
                return true;

            // ReadValue even when PlayerInput disabled Gamepad binds (KBM scheme active).
            InputAction aim = DMPlayerInputActions.Find("Aim");
            if (aim != null)
            {
                try
                {
                    if (aim.ReadValue<float>() >= 0.2f)
                        return true;
                }
                catch (System.Exception) { /* non-axis */ }
            }

            return ReadTriggerHeld(left: true);
        }

        private static bool ReadShotHeld()
        {
            if (DMPlayerInputActions.IsPressed("Attack"))
                return true;

            InputAction attack = DMPlayerInputActions.Find("Attack");
            if (attack != null)
            {
                try
                {
                    if (attack.ReadValue<float>() >= 0.2f)
                        return true;
                }
                catch (System.Exception) { /* non-axis */ }
            }

            if (ReadTriggerHeld(left: false))
                return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;
            return false;
        }

        private static bool ReadTriggerHeld(bool left)
        {
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad pad = Gamepad.all[i];
                if (pad == null)
                    continue;
                float v = left ? pad.leftTrigger.ReadValue() : pad.rightTrigger.ReadValue();
                if (v >= 0.2f)
                    return true;
            }

            // Fallback: any dualshock / generic HID gamepad-like device in the Input System list.
            foreach (UnityEngine.InputSystem.InputDevice device in InputSystem.devices)
            {
                if (device is Gamepad pad && pad != null)
                {
                    float v = left ? pad.leftTrigger.ReadValue() : pad.rightTrigger.ReadValue();
                    if (v >= 0.2f)
                        return true;
                }
            }
            return false;
        }

        private static bool ReadTriggerPressedThisFrame(bool left)
        {
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                Gamepad pad = Gamepad.all[i];
                if (pad == null)
                    continue;
                if (left ? pad.leftTrigger.wasPressedThisFrame : pad.rightTrigger.wasPressedThisFrame)
                    return true;
            }
            return false;
        }

        private bool CanReadGameplayInput()
        {
            if (!Application.isPlaying || !GameSession.HasStarted || Time.timeScale <= 0f)
                return false;

            if (_inputBridge != null && _inputBridge.ShouldLockLocomotionInput())
                return false;

            return true;
        }

        private bool CanReadCameraInput()
        {
            if (!Application.isPlaying || !GameSession.HasStarted || Time.timeScale <= 0f)
                return false;

            if (_inputBridge != null && _inputBridge.ShouldLockCameraInput())
                return false;

            return true;
        }

        public override void DoShots()
        {
            if (shooterManager is PioneerShooterManager pioneerShooter)
                pioneerShooter.SuppressNativeRecoil();
            else
                PioneerInvectorRecoilUtility.SuppressInvectorNativeRecoil(shooterManager);

            base.DoShots();
        }

        protected override void UpdateShooterAnimations()
        {
            float onlyArmsBefore = onlyArmsLayerWeight;
            base.UpdateShooterAnimations();

            if (shooterManager != null && shotLayer >= 0 && CurrentActiveWeapon != null)
            {
                ItemData weaponItem = _equipment != null ? _equipment.DrawnWeaponItem : null;
                ItemData ammoItem = null;
                if (_equipment != null)
                {
                    WeaponAmmoState ammoState = GetComponent<WeaponAmmoState>();
                    if (ammoState != null)
                        ammoItem = ammoState.GetLoadedAmmoItem(_equipment.ActiveWeaponHotbarSlot);
                }

                bool isScopeView = IsAiming && isUsingScopeView;
                float weight = PioneerInvectorRecoilUtility.ResolveShotAnimationWeight(
                    weaponItem,
                    ammoItem,
                    isScopeView);

                animator.SetLayerWeight(shotLayer, weight);
            }

            ApplyUnarmedHangWhenDrawn(onlyArmsBefore);
        }

        /// <summary>
        /// One-hand ranged hangs unarmed (OnlyArms 0, UpperBody_ID 0) until ADS / fire / reload / equip.
        /// Two-hand rifles keep the armed pose unless <see cref="PioneerAnimationPlanSettings.includeTwoHandRangedInHang"/>.
        /// </summary>
        private void ApplyUnarmedHangWhenDrawn(float onlyArmsBefore)
        {
            if (!ShouldUseOneHandHang())
                return;

            if (shooterManager == null || animator == null)
                return;

            onlyArmsLayerWeight = Mathf.Lerp(
                onlyArmsBefore,
                0f,
                shooterManager.onlyArmsSpeed * vTime.fixedDeltaTime);
            animator.SetLayerWeight(onlyArmsLayer, onlyArmsLayerWeight);
            animator.SetFloat(vAnimatorParameters.UpperBody_ID, 0f);
        }

        private bool IsTwoHandRangedDrawn()
        {
            ItemData item = _equipment != null ? _equipment.DrawnWeaponItem : null;
            if (item != null && item.IsRangedWeapon && item.weaponGrip == WeaponGrip.TwoHanded)
                return true;

            return shooterManager != null && shooterManager.GetUpperBodyID() == 2;
        }

        protected override bool CanRotateAimArm()
        {
            if (!ShouldUseMeshySnapAim())
                return base.CanRotateAimArm();

            if (cc == null || !IsAiming || !aimConditions)
                return false;

            return cc.IsAnimatorTag("Upperbody Pose");
        }

        protected override void AlignArmToAimPosition(bool isUsingLeftHand = false)
        {
            if (!ShouldUseMeshySnapAim())
            {
                base.AlignArmToAimPosition(isUsingLeftHand);
                return;
            }

            if (!shooterManager)
                return;

            if (leftArmAim == null)
                leftArmAim = new vArmAimAlign(leftUpperArm, leftLowerArm, leftHand);
            if (rightArmAim == null)
                rightArmAim = new vArmAimAlign(rightUpperArm, rightLowerArm, rightHand);

            vArmAimAlign arm = isUsingLeftHand ? leftArmAim : rightArmAim;
            armAlignmentWeight = IsAiming && aimConditions && CanRotateAimArm() ? 1f : 0f;

            if (!CurrentActiveWeapon)
                return;

            if (!shooterManager.isShooting)
                arm.UpdateDefaultAlignment();
            else
                arm.RestoreToLastAlignment();

            arm.smoothIKAlignmentPoint = shooterManager.smoothIKAlignmentPoint;
            arm.aimReference = CurrentActiveWeapon.aimReference;
            arm.smooth = shooterManager.smoothArmIKRotation;
            arm.maxVerticalAligmentAngle = shooterManager.maxVerticalAimAngle;
            arm.maxHorizontalAligmentAngle = shooterManager.maxHorizontalAimAngle;
            if (shooterManager.showCheckAimGizmos)
                arm.DrawBones(Color.blue);

            arm.AlignToArmToPosition(
                targetArmAlignmentPosition,
                armAlignmentWeight,
                CurrentActiveWeapon.alignRightUpperArmToAim,
                CurrentActiveWeapon.alignRightHandToAim);

            if (shooterManager.showCheckAimGizmos)
                arm.DrawHelpers(Color.green);
        }

        protected override void UpdateIKAdjust(bool isUsingLeftHand)
        {
            base.UpdateIKAdjust(isUsingLeftHand);

            if (!ShouldUseMeshySnapAim() || !IsAiming || IsIgnoreIK || CurrentActiveWeapon == null)
                return;

            if (isEquipping || isReloading || cc == null || cc.customAction)
                return;

            weaponIKWeight = 1f;
        }

        protected override void UpdateArmsIK(bool isUsingLeftHand = false)
        {
            if (ShouldDetachLeftSupportHand())
            {
                DetachLeftSupportHand(isUsingLeftHand);
                return;
            }

            base.UpdateArmsIK(isUsingLeftHand);

            if (!ShouldUseMeshySnapAim() || IsIgnoreIK || CurrentActiveWeapon == null)
                return;

            if (!IsAiming && !IsFiringWeapon())
                return;

            if (isEquipping || isReloading || cc == null || cc.customAction)
                return;

            supportIKWeight = 1f;
            SnapLeftSupportHand(isUsingLeftHand);
        }

        private bool ShouldUseOneHandHang()
        {
            PioneerAnimationPlanSettings settings = PioneerAnimationPlanSettings.Resolve(gameObject);
            if (settings == null || !settings.enableUnarmedHangWhenDrawn)
                return false;

            if (CurrentActiveWeapon == null)
                return false;

            if (IsTwoHandRangedDrawn() && !settings.includeTwoHandRangedInHang)
                return false;

            if (IsAiming || IsFiringWeapon() || isReloading || isEquipping)
                return false;

            return true;
        }

        private bool IsFiringWeapon()
        {
            return shooterManager != null && shooterManager.isShooting;
        }

        /// <summary>
        /// One-hand hang: drop left-hand support IK (grip / leftHandIK) so the arm hangs free.
        /// Rifles keep two-hand grip because hang is off for them.
        /// </summary>
        private bool ShouldDetachLeftSupportHand()
        {
            return ShouldUseOneHandHang();
        }

        private void DetachLeftSupportHand(bool isUsingLeftHand)
        {
            if (animator == null)
                return;

            if (LeftIK == null || !LeftIK.isValidBones)
                LeftIK = new vIKSolver(animator, AvatarIKGoal.LeftHand);
            if (RightIK == null || !RightIK.isValidBones)
                RightIK = new vIKSolver(animator, AvatarIKGoal.RightHand);

            vIKSolver targetIK = isUsingLeftHand ? RightIK : LeftIK;
            float outSpeed = shooterManager != null ? shooterManager.armIKSmoothOut : 20f;
            supportIKWeight = Mathf.Lerp(supportIKWeight, 0f, outSpeed * vTime.fixedDeltaTime);
            IsSupportHandIKEnabled = false;

            if (targetIK == null)
                return;

            targetIK.SetIKWeight(0f);
            if (shooterManager != null && shooterManager.CurrentWeaponIK)
                targetIK.AnimationToIK();
        }

        /// <summary>Resnap support hand to the weapon handIKTarget / GripPoint while aiming or firing.</summary>
        private void SnapLeftSupportHand(bool isUsingLeftHand)
        {
            if (CurrentActiveWeapon == null || CurrentActiveWeapon.handIKTargetOffset == null || animator == null)
                return;

            if (LeftIK == null || !LeftIK.isValidBones)
                LeftIK = new vIKSolver(animator, AvatarIKGoal.LeftHand);
            if (RightIK == null || !RightIK.isValidBones)
                RightIK = new vIKSolver(animator, AvatarIKGoal.RightHand);

            vIKSolver targetIK = isUsingLeftHand ? RightIK : LeftIK;
            if (targetIK == null)
                return;

            float curve = shooterManager != null && shooterManager.armIKCurve != null
                ? shooterManager.armIKCurve.Evaluate(1f)
                : 1f;
            targetIK.SetIKWeight(curve);
            targetIK.SetIKPosition(CurrentActiveWeapon.handIKTargetOffset.position);
            targetIK.SetIKRotation(CurrentActiveWeapon.handIKTargetOffset.rotation);
            if (shooterManager != null && shooterManager.CurrentWeaponIK)
                targetIK.AnimationToIK();
        }

        protected override void ApplyOffsetToTargetBone(IKOffsetTransform iKOffset, Transform target, bool isValidIK)
        {
            if (!ShouldUseMeshySnapAim() || !IsAiming)
            {
                base.ApplyOffsetToTargetBone(iKOffset, target, isValidIK);
                return;
            }

            if (target == null)
                return;

            try
            {
                target.localPosition = isValidIK && iKOffset != null ? iKOffset.position : Vector3.zero;
                target.localRotation = isValidIK && iKOffset != null
                    ? Quaternion.Euler(iKOffset.eulerAngles)
                    : Quaternion.identity;
            }
            catch
            {
                Debug.LogWarning("[PioneerShooterMeleeInput] Can't apply Meshy snap IK offset.", this);
            }
        }

        private bool ShouldUseMeshySnapAim()
        {
            if (!meshySnapAim || shooterManager == null || CurrentActiveWeapon == null)
                return false;

            if (!meshySnapAimRequiresVisual)
                return true;

            return PioneerInvectorMeshyAimSnapUtility.HasMeshyVisualRoot(gameObject);
        }


        private Vector2 ReadMoveVector()
        {
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();

            Vector2 fromInputSystem = _playerController != null ? _playerController.MoveInput : Vector2.zero;
            if (fromInputSystem.sqrMagnitude > 0.0001f)
                return fromInputSystem;

            if (DMInputSchemeRouter.IsGamepadScheme)
                return Vector2.zero;

            Vector2 move = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return move;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                move.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                move.y += 1f;

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            return move;
        }

        /// <summary>UITK HUD owns stamina/health crosshair ticks — skip Invector vHUDController updates.</summary>
        public override void UpdateHUD()
        {
            if (DMUiToolkitConfig.IsEnabled && DMUiToolkitBootstrap.IsRootActive)
                return;

            base.UpdateHUD();
        }
    }
}
