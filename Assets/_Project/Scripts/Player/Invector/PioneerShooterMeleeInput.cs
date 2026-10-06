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
        private bool _strafeForBackwardLocomotion;
        private float _lastBlockPressTime = float.NegativeInfinity;
        private Coroutine _guardImpactRoutine;
        private const float BlockGuardPoseSeconds = 0.12f;
        private const float StrongAttackBlockSuppressSeconds = 0.2f;
        private const float StrongDamageWindowSeconds = 4.0f; // Strong B runs ~3.7 s at 1.25x; disarms early when the swing ends.
        /// <summary>
        /// Moving and standing share a layer except Hold E.
        /// FullBody (weight stays 1): light tap, charge hold, charged release A/B/C, parry.
        /// UpperBody (arms mask, hip-safe clip): Hold E One Hand Sword Combo only.
        /// Base layer is locomotion. Never crossfade Mixamo clips onto Base.
        /// </summary>
        private const string LightComboAPath = "Attacks.WeakAttacks.SwordAttack.A";
        private const string StrongSwordBPath = "Attacks.StrongAttacks.SwordAttack.B";
        private const string StrongSwordAPath = "Attacks.StrongAttacks.SwordAttack.A";
        private const string StrongSwordCPath = "Attacks.StrongAttacks.SwordAttack.C";
        private const string Parry01HitPath = "Attacks.Parry01_Hit";
        private static readonly int[] ParryWindupHashes =
        {
            Animator.StringToHash("Attacks.Parry01"),
            Animator.StringToHash("Attacks.Parry"),
            Animator.StringToHash("Parry01"),
            Animator.StringToHash("Parry"),
            Animator.StringToHash("Defense.Parry01"),
            Animator.StringToHash("Defense.Parry")
        };
        private static readonly int LightComboAHash = Animator.StringToHash(LightComboAPath);
        private static readonly int StrongSwordBHash = Animator.StringToHash(StrongSwordBPath);
        private static readonly int StrongSwordAHash = Animator.StringToHash(StrongSwordAPath);
        private static readonly int StrongSwordCHash = Animator.StringToHash(StrongSwordCPath);
        private static readonly int Parry01HitHash = Animator.StringToHash(Parry01HitPath);
        private const float ParryWindupSeconds = 0.28f;
        private const float ParryHitSeconds = 0.5f;
        private float _interactUpperClearAt = float.PositiveInfinity;
        private bool _wasAttacking;
        private bool _meleeIdleRestoreQueued;
        private float _meleeOverlayProtectUntil;
        private float _overlayEnteredAt = float.NegativeInfinity;
        private int _overlayStateHash;
        private int _emptyFullBodyHash;
        private const float FullBodyAttackExitNormalized = 0.95f;
        private const float FullBodyIdleBlend = 0.1f;
        private const float MeleeOverlayProtectSeconds = 0.22f;
        /// <summary>
        /// 1HandSwordChargeUp loops the wind-up (frames 24–49). Pin near the end so the hold is a static draw-back, not a pumping swing.
        /// </summary>
        private const float ChargeHoldNormalizedTime = 0.82f;
        private const string FullBodyIdleEmptyName = "Idle_Empty";
        /// <summary>0 = Strong A, 1 = Strong B, 2 = Strong C. Set when a charged release starts.</summary>
        public int StrongReleaseSlot { get; private set; }
        private const string StrongSwordChargeStatePath = "Attacks.StrongAttacks.SwordCharge";
        private static readonly int StrongSwordChargeStateHash = Animator.StringToHash(StrongSwordChargeStatePath);
        private bool _lightAttackHeldLast;
        private float _lightAttackHoldStart = float.NegativeInfinity;
        private bool _strongChargeArmed;
        private bool _strongChargePoseActive;
        private float _chargeArmedTime;
        private float _suppressBlockUntil;
        private bool _strongDamageArmed;
        private bool _strongDamageSawSwing;
        private float _strongDamageUntil;
        private float _strongSwingStartedAt = float.NegativeInfinity;
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
            DMMeleeCombatProfileApplier.ApplyToPlayer(PioneerInvectorBootstrap.Instance);
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

            RestoreMeleeFullBodyIdle();
        }

        private void OnEnable()
        {
            GameSession.GameStarted += HandleGameStartedZoom;
            if (GameSession.HasStarted)
                HandleGameStartedZoom();
        }

        private void OnDisable()
        {
            RestoreMeleeFullBodyIdle();
            _strongDamageArmed = false;
            _strongChargeArmed = false;
            _strongChargePoseActive = false;
            _strongSwingStartedAt = float.NegativeInfinity;
            _meleeIdleRestoreQueued = false;
            _wasAttacking = false;
            OnDisableAttack();
            PioneerMeleeDamageWindowTracker.ClearSpeedOverride();
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
            MaintainStrongChargeAnimation();
            TickLightComboChain();
            TickMeleeFullBodyIdleRestore();
            ApplyMeleeThreatFacing();
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
            UpdateBackwardStrafeWithoutTurnaround();
            cc.ControlKeepDirection();
        }

        /// <summary>
        /// S/back moves away from camera facing without spinning the body 180° (strafe-back).
        /// </summary>
        private void UpdateBackwardStrafeWithoutTurnaround()
        {
            if (cc == null)
                return;

            if (IsAimingActive || cc.lockInStrafe || cc.customAction || cc.isRolling || cc.isJumping)
            {
                _strafeForBackwardLocomotion = false;
                return;
            }

            bool backing = cc.input.z < -0.12f;
            if (backing)
            {
                cc.isStrafing = true;
                _strafeForBackwardLocomotion = true;
                return;
            }

            if (_strafeForBackwardLocomotion)
            {
                cc.isStrafing = false;
                _strafeForBackwardLocomotion = false;
            }
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

        /// <summary>
        /// Drawn sword charge owns the strong release. The base press trigger always enters SwordAttack A.
        /// </summary>
        public override void MeleeStrongAttackInput()
        {
            if (IsDrawnMeleeWeaponActive())
                return;

            base.MeleeStrongAttackInput();
        }

        public bool IsStrongMeleeDamageActive =>
            _strongDamageArmed && Time.time <= _strongDamageUntil;

        public bool IsStrongChargePoseActive => _strongChargePoseActive;

        /// <summary>Hold past charge threshold, before or during charge pose crossfade.</summary>
        public bool IsStrongChargeHoldActive => _strongChargeArmed && _lightAttackHeldLast;

        /// <summary>Charged release swing (Strong SwordAttack B) — covers clip even if FullBody state hash mismatches.</summary>
        public bool IsChargedStrongSwingActive =>
            _strongSwingStartedAt > float.NegativeInfinity
            && Time.time - _strongSwingStartedAt <= StrongDamageWindowSeconds;

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

        public override void OnDisableAttack()
        {
            base.OnDisableAttack();
            _meleeIdleRestoreQueued = true;
            _wasAttacking = false;
        }

        private void UpdateDrawnMeleeCharge()
        {
            TickUpperInteractRestore();
            TickStrongDamageWindow();

            if (!CanTrackDrawnMeleeCharge())
            {
                _lightAttackHeldLast = false;
                if (_strongChargeArmed || _strongChargePoseActive)
                    CancelChargePose();
                _strongChargeArmed = false;
                _strongChargePoseActive = false;
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
                ResetDrawnMeleeAttackTriggers();
            }

            if (held && !released)
                ResetDrawnMeleeAttackTriggers();

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

            if (!_strongDamageSawSwing)
                _strongDamageArmed = false;

            if (ReadInteractHoldActive())
                PlayInteractHoldComboAttack();
            else
                PlayLightSwordSwing();
        }

        private void TryReleaseStrongMelee()
        {
            if (!CanReleaseDrawnMelee())
                return;

            // A held block keeps FullBody in Defense, and StrongAttack is only wired from Null.
            isBlocking = false;
            animator.SetBool(vAnimatorParameters.IsBlocking, false);
            _suppressBlockUntil = Time.time + StrongAttackBlockSuppressSeconds;
            _strongChargePoseActive = false;
            PlayStrongSwordSwing();
            _strongDamageArmed = true;
            _strongDamageSawSwing = false;
            _strongDamageUntil = Time.time + StrongDamageWindowSeconds;
        }

        /// <summary>
        /// Layer policy (walk/run and standing use the same layer unless noted):
        /// Light tap, charge hold, charged A/B/C, and parry play on FullBody with weight kept at 1.
        /// That is the pre-overlay path: the attack clip plays in place and Base locomotion returns when the state exits.
        /// Hold E (One Hand Sword Combo) is the only UpperBody swing. The Mixamo clip sinks the hips on an unmasked FullBody.
        /// Never crossfade these clips on the Base layer, and never leave FullBody weight at 0.
        /// </summary>
        private void PlayLightSwordSwing()
        {
            PrepareDrawnSwordAttackId();
            ExitUpperInteract(0.1f);
            int layer = ResolveFullBodyLayer();
            if (layer >= 0)
                animator.SetLayerWeight(layer, 1f);

            MarkMeleeOverlayStarted();
            if (layer < 0)
            {
                TriggerWeakAttack();
                return;
            }

            // A tap during A or B queues the next swing. The chain is driven here, not by the WeakAttack
            // trigger: taps fire on release, and every press resets the triggers (charge needs that), so a
            // press that landed inside the controller's short A>B / B>C window used to drop the combo.
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            int activeSlot = -1;
            if (animator.IsInTransition(layer)
                && PioneerLightMeleeAnimStates.TryGetLightComboSlot(animator.GetNextAnimatorStateInfo(layer), out int nextSlot))
                activeSlot = nextSlot;
            else if (PioneerLightMeleeAnimStates.TryGetLightComboSlot(current, out int currentSlot))
                activeSlot = currentSlot;

            if (activeSlot >= 0)
            {
                if (activeSlot < LightComboPaths.Length - 1)
                {
                    _lightChainQueuedSlot = activeSlot + 1;
                    _lightChainQueuedAt = Time.time;
                    TickLightComboChain();
                }

                return;
            }

            // A tap just after A or B finished still continues the combo instead of restarting at A.
            if (_lastLightSlot >= 0 && _lastLightSlot < LightComboPaths.Length - 1
                && Time.time - _lastLightSlotSeenAt <= LightChainGraceSeconds)
            {
                PlayLightComboSlot(layer, _lastLightSlot + 1);
                return;
            }

            // First tap starts A directly. The hash needs the layer prefix (FullBody.Attacks...):
            // the bare path never matched HasState, so taps only set a trigger that Idle_Empty has no transition for.
            int lightA = ResolveStateHash(layer, LightComboAPath);
            if (lightA != 0)
            {
                animator.CrossFadeInFixedTime(lightA, 0.06f, layer, 0f);
                return;
            }

            TriggerWeakAttack();
        }

        private static readonly string[] LightComboPaths =
        {
            "Attacks.WeakAttacks.SwordAttack.A",
            "Attacks.WeakAttacks.SwordAttack.B",
            "Attacks.WeakAttacks.SwordAttack.C"
        };

        // Normalized time in A / B at which a queued tap moves on (the controller's authored chain points,
        // just before each swing's own exit at 0.75 / 0.90).
        private static readonly float[] LightChainAtNormalized = { 0.70f, 0.85f };
        private const float LightChainBufferSeconds = 1.0f;
        private const float LightChainGraceSeconds = 0.35f;
        private const float LightChainBlendSeconds = 0.1f;
        private int _lightChainQueuedSlot = -1;
        private float _lightChainQueuedAt = float.NegativeInfinity;
        private int _lastLightSlot = -1;
        private float _lastLightSlotSeenAt = float.NegativeInfinity;

        private void PlayLightComboSlot(int layer, int slot)
        {
            _lightChainQueuedSlot = -1;
            ResetDrawnMeleeAttackTriggers();
            int hash = slot >= 0 && slot < LightComboPaths.Length ? ResolveStateHash(layer, LightComboPaths[slot]) : 0;
            if (hash == 0)
            {
                TriggerWeakAttack();
                return;
            }

            MarkMeleeOverlayStarted();
            animator.CrossFadeInFixedTime(hash, LightChainBlendSeconds, layer, 0f);
            _lastLightSlot = slot;
            _lastLightSlotSeenAt = Time.time;
        }

        /// <summary>
        /// Runs every LateUpdate: remembers the last light slot seen, and fires a queued tap once the
        /// current swing reaches its chain point (or right away if that point already passed).
        /// </summary>
        private void TickLightComboChain()
        {
            if (animator == null)
                return;

            int layer = ResolveFullBodyLayer();
            if (layer < 0)
                return;

            bool inTransition = animator.IsInTransition(layer);
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            bool currentIsLight = PioneerLightMeleeAnimStates.TryGetLightComboSlot(current, out int currentSlot);
            int nextSlot = -1;
            bool nextIsLight = inTransition
                && PioneerLightMeleeAnimStates.TryGetLightComboSlot(animator.GetNextAnimatorStateInfo(layer), out nextSlot);

            if (nextIsLight)
            {
                _lastLightSlot = nextSlot;
                _lastLightSlotSeenAt = Time.time;
            }
            else if (currentIsLight)
            {
                _lastLightSlot = currentSlot;
                _lastLightSlotSeenAt = Time.time;
            }

            if (_lightChainQueuedSlot < 0)
                return;

            if (Time.time - _lightChainQueuedAt > LightChainBufferSeconds)
            {
                _lightChainQueuedSlot = -1;
                return;
            }

            // Already blending into the queued swing (or past it).
            if (nextIsLight && nextSlot >= _lightChainQueuedSlot)
            {
                _lightChainQueuedSlot = -1;
                return;
            }

            if (!currentIsLight || nextIsLight)
            {
                // The swing already left (parked or exiting). Continue if it only just ended.
                if (!currentIsLight && Time.time - _lastLightSlotSeenAt <= LightChainGraceSeconds)
                    PlayLightComboSlot(layer, _lightChainQueuedSlot);
                return;
            }

            if (currentSlot >= _lightChainQueuedSlot)
            {
                _lightChainQueuedSlot = -1;
                return;
            }

            float chainAt = currentSlot < LightChainAtNormalized.Length ? LightChainAtNormalized[currentSlot] : 0.7f;
            if (current.normalizedTime < chainAt)
                return;

            PlayLightComboSlot(layer, _lightChainQueuedSlot);
        }

        /// <summary>
        /// Charged release always crossfades FullBody Strong SwordAttack A, B, or C (favor A), walking or standing.
        /// </summary>
        private void PlayStrongSwordSwing()
        {
            PrepareDrawnSwordAttackId();
            ExitUpperInteract(0.1f);
            _strongChargePoseActive = false;

            int layer = ResolveFullBodyLayer();
            if (layer >= 0)
                animator.SetLayerWeight(layer, 1f);

            if (!TryPickStrongRelease(out int slot, out int fullBodyHash))
            {
                MarkMeleeOverlayStarted();
                TriggerStrongAttack();
                return;
            }

            StrongReleaseSlot = slot;
            _strongSwingStartedAt = Time.time;
            MarkMeleeOverlayStarted();
            ApplyStrongMeleeAnimSpeedFromLiveProfile();

            if (layer >= 0 && fullBodyHash != 0 && animator.HasState(layer, fullBodyHash))
            {
                animator.CrossFadeInFixedTime(fullBodyHash, 0.08f, layer, 0f);
                return;
            }

            TriggerStrongAttack();
        }

        /// <summary>
        /// Hold E + light release. Masked UpperBody only, hip-safe combo clip, speed from the profile (1.75).
        /// FullBody stays at weight 1 and is moved to its empty pose so it does not cover the upper swing or the legs.
        /// </summary>
        private void PlayInteractHoldComboAttack()
        {
            if (animator == null)
                return;

            PrepareDrawnSwordAttackId();
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float speed = profile != null ? profile.interactHoldComboAnimSpeed : 1.75f;
            speed = Mathf.Clamp(speed, 0.75f, 2.5f);

            int upper = ResolveUpperBodyLayer();
            int hash = ResolveStateHash(upper, "InteractHoldCombo");
            if (upper < 0 || hash == 0 || !animator.HasState(upper, hash))
                return;

            int fullBody = ResolveFullBodyLayer();
            if (fullBody >= 0)
                animator.SetLayerWeight(fullBody, 1f);
            ParkFullBodyOnEmpty();
            animator.CrossFadeInFixedTime(hash, 0.1f, upper, 0f);
            PioneerMeleeDamageWindowTracker.ApplyStrongMeleeSpeedOverride(speed);
            MarkMeleeOverlayStarted();
            _interactUpperClearAt = Time.time + 3.2f;
        }

        private static bool ReadInteractHoldActive()
        {
            return DMPlayerInputActions.IsPressed("Use");
        }

        private void TickUpperInteractRestore()
        {
            if (animator == null)
                return;

            int upper = ResolveUpperBodyLayer();
            if (upper < 0 || animator.IsInTransition(upper))
                return;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(upper);
            bool pastEnd = info.IsName("InteractHoldCombo") && info.normalizedTime >= 1.02f;
            if (!pastEnd && Time.time < _interactUpperClearAt)
                return;

            if (info.IsName("InteractHoldCombo"))
            {
                ExitUpperInteract(FullBodyIdleBlend);
                RestoreMeleeFullBodyIdle();
            }
            else
                _interactUpperClearAt = float.PositiveInfinity;
        }

        private void ExitUpperInteract(float blend)
        {
            _interactUpperClearAt = float.PositiveInfinity;
            if (animator == null)
                return;

            int upper = ResolveUpperBodyLayer();
            if (upper < 0 || animator.IsInTransition(upper))
                return;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(upper);
            if (!info.IsName("InteractHoldCombo"))
                return;

            int empty = ResolveStateHash(upper, "null");
            if (empty == 0)
                empty = ResolveStateHash(upper, "Null");
            if (empty != 0)
                animator.CrossFadeInFixedTime(empty, blend, upper, 0f);
            PioneerMeleeDamageWindowTracker.ClearSpeedOverride();
        }

        /// <summary>
        /// Puts FullBody weight back to 1. Leaves an in-progress light/strong/parry alone.
        /// Clears a charge pose that is no longer held, and the Hold E upper state.
        /// </summary>
        private void RestoreMeleeLocomotionLayers()
        {
            if (animator == null)
                return;

            ExitUpperInteract(FullBodyIdleBlend);
            int fullBody = ResolveFullBodyLayer();
            if (fullBody < 0)
                return;

            animator.SetLayerWeight(fullBody, 1f);
            if (animator.IsInTransition(fullBody))
                return;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(fullBody);
            bool chargeLeftUp = PioneerStrongMeleeAnimStates.IsStrongChargeState(info) && !_strongChargeArmed;
            if (chargeLeftUp)
                ParkFullBodyOnEmpty();
        }

        /// <summary>
        /// Parks FullBody on a verified empty state (Idle_Empty, motion null) so Base locomotion shows.
        /// Nested Attacks.Null CrossFade often never lands after Pioneer CrossFades into SwordAttack.
        /// </summary>
        private void RestoreMeleeFullBodyIdle()
        {
            if (animator == null)
                return;

            PioneerMeleeDamageWindowTracker.ClearSpeedOverride();
            ExitUpperInteract(FullBodyIdleBlend);

            int fullBody = ResolveFullBodyLayer();
            if (fullBody >= 0)
            {
                animator.SetLayerWeight(fullBody, 1f);
                ParkFullBodyOnEmpty();
            }

            isAttacking = false;
            _strongDamageArmed = false;
            _meleeIdleRestoreQueued = false;
            _wasAttacking = false;
        }

        private void MarkMeleeOverlayStarted()
        {
            _wasAttacking = true;
            _overlayEnteredAt = Time.time;
            _meleeOverlayProtectUntil = Time.time + MeleeOverlayProtectSeconds;
        }

        /// <summary>
        /// Event-independent: Mixamo/PROTOFACTOR clips have no OnDisableAttack events.
        /// Parks Idle_Empty when an attack overlay finishes. Exempts SwordCharge while the button is held.
        /// Does not use isAttacking as a gate.
        /// </summary>
        private void TickMeleeFullBodyIdleRestore()
        {
            if (animator == null)
                return;

            int upper = ResolveUpperBodyLayer();
            if (upper >= 0 && !animator.IsInTransition(upper))
            {
                AnimatorStateInfo upperInfo = animator.GetCurrentAnimatorStateInfo(upper);
                if (PioneerLightMeleeAnimStates.IsInteractHoldComboState(upperInfo)
                    && upperInfo.normalizedTime >= FullBodyAttackExitNormalized)
                    ExitUpperInteract(FullBodyIdleBlend);
            }

            int layer = ResolveFullBodyLayer();
            if (layer < 0)
                return;

            if (Time.time < _meleeOverlayProtectUntil)
                return;

            if (_guardImpactRoutine != null)
                return;

            bool held = ReadLightAttackHeld() || _lightAttackHeldLast;
            bool inTransition = animator.IsInTransition(layer);
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = inTransition ? animator.GetNextAnimatorStateInfo(layer) : current;
            AnimatorStateInfo info = IsMeleeAttackOverlayState(current) ? current : next;

            // Charge hold has no max time and must not auto-swing or get parked by the stick watchdog.
            if (_strongChargeArmed && held)
                return;

            if (IsEmptyFullBodyState(current) && !inTransition)
            {
                // Parked on an untagged empty state: anything still tagged on this layer is stale.
                // Invector's isAttacking also reads the "Attack" tag, which blocks jump, sprint and roll,
                // and "LockMovement" freezes locomotion. Interrupted transitions skip OnStateExit, so
                // those tags can survive the swing that added them.
                PurgeStaleLayerTags(layer);
                if (_meleeIdleRestoreQueued || isAttacking)
                {
                    PioneerMeleeDamageWindowTracker.ClearSpeedOverride();
                    isAttacking = false;
                    _strongDamageArmed = false;
                    _meleeIdleRestoreQueued = false;
                    _wasAttacking = false;
                }

                return;
            }

            if (held && (PioneerStrongMeleeAnimStates.IsStrongChargeState(info)
                || PioneerStrongMeleeAnimStates.IsStrongChargeState(current)
                || _strongChargePoseActive))
                return;

            bool overlay = IsMeleeAttackOverlayState(current) || IsMeleeAttackOverlayState(next);
            if (!overlay)
            {
                if (IsLegacyFullBodyNull(current) || _meleeIdleRestoreQueued)
                {
                    RestoreMeleeFullBodyIdle();
                    return;
                }

                _wasAttacking = isAttacking;
                return;
            }

            // The swing is already blending out on its own (EXIT transition). Cutting in with another
            // CrossFade interrupts that transition, which skips the swing's OnStateExit and strands its tags.
            if (inTransition && IsMeleeAttackOverlayState(current) && !IsMeleeAttackOverlayState(next))
                return;

            if (_overlayEnteredAt < 0f || info.fullPathHash != _overlayStateHash)
            {
                // Restart the elapsed timer per swing so a chained B or C is not cut short by A's start time.
                _overlayStateHash = info.fullPathHash;
                _overlayEnteredAt = Time.time;
            }

            float cycle = info.normalizedTime;
            float clipLength = info.length > 0.05f ? info.length : 0.8f;
            float speed = Mathf.Max(0.05f, animator.speed);
            bool pastNormalized = cycle >= FullBodyAttackExitNormalized;
            bool pastElapsed = Time.time - _overlayEnteredAt >= clipLength / speed + 0.12f;
            if (!pastNormalized && !pastElapsed)
                return;

            RestoreMeleeFullBodyIdle();
        }

        private float _nextStaleTagLogAt;

        /// <summary>
        /// Removes every tag left on one animator layer in all Invector tag listeners.
        /// Only call while that layer is parked on an untagged state and not in transition.
        /// </summary>
        private void PurgeStaleLayerTags(int layer)
        {
            if (cc == null || cc.animatorStateInfos == null)
                return;

            vAnimatorStateInfos.vStateInfo[] ccInfos = cc.animatorStateInfos.stateInfos;
            if (ccInfos == null || layer < 0 || layer >= ccInfos.Length || ccInfos[layer] == null
                || ccInfos[layer].tags.Count == 0)
                return;

            string cleared = string.Join(",", ccInfos[layer].tags);
            System.Collections.Generic.HashSet<vAnimatorStateInfos> seen =
                new System.Collections.Generic.HashSet<vAnimatorStateInfos> { cc.animatorStateInfos };
            vAnimatorTagBase[] behaviours = animator.GetBehaviours<vAnimatorTagBase>();
            for (int b = 0; b < behaviours.Length; b++)
            {
                if (behaviours[b] == null || behaviours[b].stateInfos == null)
                    continue;
                for (int s = 0; s < behaviours[b].stateInfos.Count; s++)
                    if (behaviours[b].stateInfos[s] != null)
                        seen.Add(behaviours[b].stateInfos[s]);
            }

            foreach (vAnimatorStateInfos infos in seen)
            {
                if (infos.stateInfos == null || layer >= infos.stateInfos.Length || infos.stateInfos[layer] == null)
                    continue;
                infos.stateInfos[layer].tags.Clear();
                infos.stateInfos[layer].shortPathHash = 0;
                infos.stateInfos[layer].normalizedTime = 0f;
            }

            if (Time.time >= _nextStaleTagLogAt)
            {
                _nextStaleTagLogAt = Time.time + 2f;
                Debug.Log($"[DM Melee] Cleared stale FullBody tags after swing: {cleared}", this);
            }
        }

        private bool IsEmptyFullBodyState(AnimatorStateInfo info)
        {
            return info.IsName(FullBodyIdleEmptyName)
                || info.IsName("Attacks.Idle_Empty")
                || info.IsName("FullBody.Idle_Empty")
                || info.IsName("FullBody.Attacks.Idle_Empty");
        }

        private static bool IsLegacyFullBodyNull(AnimatorStateInfo info)
        {
            return info.IsName("Null")
                || info.IsName("null")
                || info.IsName("Attacks.Null.Null")
                || info.IsName("FullBody.Attacks.Null.Null");
        }

        private static bool IsMeleeAttackOverlayState(AnimatorStateInfo info)
        {
            if (PioneerStrongMeleeAnimStates.TryGetStrongReleaseSlot(info, out _))
                return true;
            if (PioneerStrongMeleeAnimStates.IsStrongChargeState(info))
                return true;
            if (PioneerLightMeleeAnimStates.TryGetLightComboSlot(info, out _))
                return true;
            if (PioneerLightMeleeAnimStates.TryGetLightRandomSlot(info, out _))
                return true;
            if (PioneerLightMeleeAnimStates.IsInteractHoldComboState(info))
                return true;
            return info.IsName("Parry01")
                || info.IsName("Parry")
                || info.IsName("Parry01_Hit")
                || info.IsName("Attacks.Parry01")
                || info.IsName("Attacks.Parry")
                || info.IsName("Attacks.Parry01_Hit")
                || info.IsName("FullBody.Attacks.Parry01")
                || info.IsName("FullBody.Attacks.Parry")
                || info.IsName("FullBody.Attacks.Parry01_Hit");
        }

        private void CancelChargePose()
        {
            _strongChargePoseActive = false;
            _strongChargeArmed = false;
            RestoreMeleeFullBodyIdle();
        }

        /// <summary>
        /// Parry and guard presentation call this after parking FullBody.
        /// Clears the attack flag without a Base-layer crossfade and without dropping FullBody weight.
        /// </summary>
        private void EndSwingIfBodyIdle()
        {
            if (animator == null)
                return;

            int layer = ResolveFullBodyLayer();
            if (layer >= 0 && animator.IsInTransition(layer))
                return;

            if (layer >= 0)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
                if (IsStrongSwordState(info) || IsStrongChargeState(info)
                    || info.IsName(LightComboAPath)
                    || info.IsName("Attacks.WeakAttacks.SwordAttack.B")
                    || info.IsName("Attacks.WeakAttacks.SwordAttack.C"))
                    return;
            }

            OnDisableAttack();
        }

        private void ParkFullBodyOnEmpty()
        {
            int fullBody = ResolveFullBodyLayer();
            if (animator == null || fullBody < 0)
                return;

            animator.SetLayerWeight(fullBody, 1f);
            int hash = ResolveEmptyFullBodyHash(fullBody);
            if (hash == 0)
                return;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(fullBody);
            if (IsEmptyFullBodyState(info) && !animator.IsInTransition(fullBody))
                return;

            animator.CrossFadeInFixedTime(hash, FullBodyIdleBlend, fullBody, 0f);
        }

        private int ResolveEmptyFullBodyHash(int layer)
        {
            if (_emptyFullBodyHash != 0 && animator != null && animator.HasState(layer, _emptyFullBodyHash))
                return _emptyFullBodyHash;

            string[] paths =
            {
                FullBodyIdleEmptyName,
                "FullBody.Idle_Empty",
                "Attacks.Idle_Empty",
                "FullBody.Attacks.Idle_Empty"
            };
            for (int i = 0; i < paths.Length; i++)
            {
                int hash = ResolveStateHash(layer, paths[i]);
                if (hash != 0)
                {
                    _emptyFullBodyHash = hash;
                    return hash;
                }
            }

            return 0;
        }

        private int ResolveStateHash(int layer, string path)
        {
            if (animator == null || layer < 0 || string.IsNullOrEmpty(path))
                return 0;

            int hash = Animator.StringToHash(path);
            if (animator.HasState(layer, hash))
                return hash;

            string layerName = animator.GetLayerName(layer);
            int withLayer = Animator.StringToHash(layerName + "." + path);
            if (animator.HasState(layer, withLayer))
                return withLayer;

            return 0;
        }

        private int ResolveUpperBodyLayer()
        {
            if (cc != null && cc.upperBodyLayer >= 0)
                return cc.upperBodyLayer;
            return animator != null ? animator.GetLayerIndex("UpperBody") : -1;
        }

        private bool TryPickStrongRelease(out int slot, out int fullBodyHash)
        {
            slot = 0;
            fullBodyHash = 0;
            if (animator == null)
                return false;

            int fullLayer = ResolveFullBodyLayer();
            int hashA = ResolveStateHash(fullLayer, StrongSwordAPath);
            int hashB = ResolveStateHash(fullLayer, StrongSwordBPath);
            int hashC = ResolveStateHash(fullLayer, StrongSwordCPath);

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float wa = hashA != 0 ? Mathf.Max(0f, profile != null ? profile.strongReleaseWeightA : 0.55f) : 0f;
            float wb = hashB != 0 ? Mathf.Max(0f, profile != null ? profile.strongReleaseWeightB : 0.22f) : 0f;
            float wc = hashC != 0 ? Mathf.Max(0f, profile != null ? profile.strongReleaseWeightC : 0.23f) : 0f;
            float sum = wa + wb + wc;
            if (sum <= 0.001f)
                return false;

            float r = Random.value * sum;
            if (r < wa && hashA != 0)
            {
                slot = 0;
                fullBodyHash = hashA;
                return true;
            }

            r -= wa;
            if (r < wb && hashB != 0)
            {
                slot = 1;
                fullBodyHash = hashB;
                return true;
            }

            if (hashC != 0)
            {
                slot = 2;
                fullBodyHash = hashC;
                return true;
            }

            if (hashB != 0)
            {
                slot = 1;
                fullBodyHash = hashB;
                return true;
            }

            if (hashA != 0)
            {
                slot = 0;
                fullBodyHash = hashA;
                return true;
            }

            return false;
        }

        private static void ApplyStrongMeleeAnimSpeedFromLiveProfile()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float mult = profile != null ? profile.strongMeleeAnimSpeedMultiplier : 1.25f;
            mult = Mathf.Clamp(mult, 0.75f, 2f);
            PioneerMeleeDamageWindowTracker.ApplyStrongMeleeSpeedOverride(mult);
        }

        private void PrepareDrawnSwordAttackId()
        {
            ResetDrawnMeleeAttackTriggers();
            int attackId = AttackID;
            if (attackId <= 0)
                attackId = 1;
            animator.SetInteger(vAnimatorParameters.AttackID, attackId);
        }

        private void ResetDrawnMeleeAttackTriggers()
        {
            if (animator == null)
                return;

            animator.ResetTrigger(vAnimatorParameters.WeakAttack);
            animator.ResetTrigger(vAnimatorParameters.StrongAttack);
        }

        /// <summary>
        /// Charge pose is always FullBody SwordCharge, walking or standing. Weight stays 1.
        /// 1HandSwordChargeUp is a looping wind-up, so after the draw-back frame the state is pinned
        /// (Play at ChargeHoldNormalizedTime) until release. Release crossfades to A/B/C; cancel parks Idle_Empty.
        /// Press must not fire WeakAttack — triggers stay reset while the button is held.
        /// </summary>
        private void MaintainStrongChargeAnimation()
        {
            if (!_strongChargeArmed
                || !CanTrackDrawnMeleeCharge()
                || !ReadLightAttackHeld()
                || isBlocking
                || animator == null)
            {
                if (!_strongChargeArmed)
                    _strongChargePoseActive = false;
                return;
            }

            ResetDrawnMeleeAttackTriggers();

            int layer = ResolveFullBodyLayer();
            int standingChargeHash = ResolveStateHash(layer, StrongSwordChargeStatePath);
            if (standingChargeHash == 0)
                standingChargeHash = ResolveStateHash(layer, "SwordCharge");
            if (standingChargeHash == 0)
                standingChargeHash = StrongSwordChargeStateHash;
            if (layer < 0 || !animator.HasState(layer, standingChargeHash))
                return;

            animator.SetLayerWeight(layer, 1f);
            bool inTransition = animator.IsInTransition(layer);
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            bool inCharge = PioneerStrongMeleeAnimStates.IsStrongChargeState(current)
                || IsStrongChargeState(current);

            if (inCharge && !inTransition)
            {
                float cycle = current.normalizedTime;
                if (cycle >= 1f)
                    cycle -= Mathf.Floor(cycle);
                if (cycle >= ChargeHoldNormalizedTime || current.normalizedTime >= ChargeHoldNormalizedTime)
                    animator.Play(standingChargeHash, layer, ChargeHoldNormalizedTime);
                _strongChargePoseActive = true;
                _meleeOverlayProtectUntil = Time.time + MeleeOverlayProtectSeconds;
                return;
            }

            if (_strongChargePoseActive && inTransition)
                return;

            animator.CrossFadeInFixedTime(standingChargeHash, 0.1f, layer, 0f);
            _strongChargePoseActive = true;
            _meleeOverlayProtectUntil = Time.time + MeleeOverlayProtectSeconds;
        }

        private bool IsStrongPosePlaying()
        {
            int layer = ResolveFullBodyLayer();
            if (animator == null || layer < 0)
                return false;

            AnimatorStateInfo info = animator.IsInTransition(layer)
                ? animator.GetNextAnimatorStateInfo(layer)
                : animator.GetCurrentAnimatorStateInfo(layer);
            return IsStrongSwordState(info);
        }

        private static bool IsStrongSwordState(AnimatorStateInfo info)
        {
            if (PioneerStrongMeleeAnimStates.TryGetStrongReleaseSlot(info, out _))
                return true;
            return info.fullPathHash == StrongSwordBHash
                || info.IsName(StrongSwordBPath)
                || info.fullPathHash == StrongSwordAHash
                || info.IsName(StrongSwordAPath)
                || info.fullPathHash == StrongSwordCHash
                || info.IsName(StrongSwordCPath);
        }

        private static bool IsStrongChargeState(AnimatorStateInfo info)
        {
            return info.fullPathHash == StrongSwordChargeStateHash || info.IsName(StrongSwordChargeStatePath);
        }

        private int ResolveFullBodyLayer()
        {
            if (cc != null && cc.fullbodyLayer >= 0)
                return cc.fullbodyLayer;
            return animator != null ? animator.GetLayerIndex("FullBody") : -1;
        }

        private static bool TryGetBladeTip(Transform weapon, Vector3 grip, out Vector3 tipWorld)
        {
            tipWorld = grip;
            float bestDistance = 0f;
            bool found = false;
            bool bestIsVisual = false;
            MeshFilter[] filters = weapon.GetComponentsInChildren<MeshFilter>(false);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.sharedMesh == null)
                    continue;

                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer != null && !renderer.enabled)
                    continue;

                Bounds bounds = filter.sharedMesh.bounds;
                Vector3 extents = bounds.extents;
                int axis = 0;
                float length = extents.x;
                if (extents.y > length)
                {
                    length = extents.y;
                    axis = 1;
                }

                if (extents.z > length)
                {
                    length = extents.z;
                    axis = 2;
                }

                if (length < 0.2f)
                    continue;

                Vector3 localAxis = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
                Vector3 endA = filter.transform.TransformPoint(bounds.center + localAxis * length);
                Vector3 endB = filter.transform.TransformPoint(bounds.center - localAxis * length);
                Vector3 far = (endA - grip).sqrMagnitude >= (endB - grip).sqrMagnitude ? endA : endB;
                float distance = (far - grip).magnitude;
                bool visual = filter.name.IndexOf("Visual", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || filter.name.IndexOf("Pioneer", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool better = !found || (visual && !bestIsVisual) || (visual == bestIsVisual && distance > bestDistance);
                if (!better)
                    continue;

                bestDistance = distance;
                bestIsVisual = visual;
                tipWorld = far;
                found = true;
            }

            return found;
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

            if (IsStrongPosePlaying() || isAttacking)
                _strongDamageSawSwing = true;

            if (Time.time > _strongDamageUntil)
            {
                _strongDamageArmed = false;
                _meleeIdleRestoreQueued = true;
            }
            else if (_strongDamageSawSwing && !IsStrongPosePlaying() && !isAttacking)
            {
                _strongDamageArmed = false;
                _meleeIdleRestoreQueued = true;
            }
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
                && IsBlockingAtHitTime()
                && meleeManager != null
                && damage.sender != null
                && meleeManager.CanBlockAttack(damage.sender.position);
        }

        /// <summary>
        /// Re-read block input when the hit lands so stale isBlocking cannot absorb after a released parry tap.
        /// </summary>
        private bool IsBlockingAtHitTime()
        {
            if (cc == null)
                return isBlocking;

            return ReadBlockHeld()
                && cc.currentStamina > 0
                && !cc.customAction
                && !isAttacking;
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

            if (damage != null
                && !damage.ignoreDefense
                && successfulBlock
                && meleeManager != null)
            {
                int damageReduction = meleeManager.GetDefenseRate();
                if (damageReduction > 0)
                    damage.ReduceDamage(damageReduction);

                if (attacker != null && meleeManager.CanBreakAttack())
                    attacker.BreakAttack(meleeManager.GetDefenseRecoilID());

                meleeManager.OnDefense();
                cc.currentStaminaRecoveryDelay = damage.staminaRecoveryDelay;
                cc.currentStamina -= damage.staminaBlockCost;
            }

            if (damage != null)
                damage.hitReaction = !successfulBlock || damage.ignoreDefense;

            cc.TakeDamage(damage);

            if (!successfulBlock)
                return;

            if (isParry)
            {
                PlayParryHitReaction();
                DMParryClashVfx.TryPlay(damage, transform, damageSender);
                DMCombatCameraShake.PlayParry();
            }
            else
            {
                PlayGuardImpactReaction();
                DMCombatCameraShake.PlayBlock();
            }

            DMEnemyGuardBreakStagger.TryApplyFromBlock(attacker, transform, isParry, damageSender);
        }

        /// <summary>
        /// Short guard-impact pose. Block and parry use the same mild pose.
        /// Does not call OnRecoil — that fires ResetState and can drop the block.
        /// RecoilID 1 is the mild unarmed recoil. RecoilID 2 is the stronger low recoil
        /// and is not used here. RecoilID above 2 is recoil_hard, tagged CustomAction,
        /// which clears isBlocking. This path does not freeze animator speed.
        /// </summary>
        /// <summary>
        /// Tap-parry contact: FullBody Parry01 windup, then Parry01_Hit. Hold-block stays on the guard pose.
        /// </summary>
        private void PlayParryHitReaction()
        {
            if (animator == null)
                return;

            RestoreMeleeLocomotionLayers();
            int layer = ResolveFullBodyLayer();
            int windup = ResolveParryWindupHash(layer);
            bool hasHit = layer >= 0 && animator.HasState(layer, Parry01HitHash);
            if (windup == 0 && !hasHit)
            {
                PlayGuardImpactReaction();
                return;
            }

            animator.SetBool(vAnimatorParameters.IsBlocking, true);
            animator.SetInteger(vAnimatorParameters.DefenseID, DefenseID);
            if (windup != 0)
                animator.CrossFadeInFixedTime(windup, 0.05f, layer, 0f);
            else
                animator.CrossFadeInFixedTime(Parry01HitHash, 0.05f, layer, 0f);

            if (_guardImpactRoutine != null)
                StopCoroutine(_guardImpactRoutine);

            _guardImpactRoutine = StartCoroutine(ParryPresentationRoutine(layer, windup != 0 && hasHit));
        }

        private int ResolveParryWindupHash(int layer)
        {
            if (animator == null || layer < 0)
                return 0;

            for (int i = 0; i < ParryWindupHashes.Length; i++)
            {
                if (animator.HasState(layer, ParryWindupHashes[i]))
                    return ParryWindupHashes[i];
            }

            return 0;
        }

        private IEnumerator ParryPresentationRoutine(int layer, bool chainHit)
        {
            if (chainHit)
            {
                yield return new WaitForSeconds(ParryWindupSeconds);
                if (animator != null && layer >= 0 && animator.HasState(layer, Parry01HitHash))
                    animator.CrossFadeInFixedTime(Parry01HitHash, 0.05f, layer, 0f);
                yield return new WaitForSeconds(ParryHitSeconds);
            }
            else
            {
                yield return new WaitForSeconds(ParryWindupSeconds + ParryHitSeconds);
            }

            _guardImpactRoutine = null;
            if (animator == null || layer < 0)
                yield break;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName("Parry01") || info.IsName("Parry") || info.IsName("Parry01_Hit"))
                RestoreMeleeFullBodyIdle();
            else
                EndSwingIfBodyIdle();
        }

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
                RestoreMeleeFullBodyIdle();

            animator.SetBool(vAnimatorParameters.IsBlocking, true);
        }

        private void ApplyMeleeThreatFacing()
        {
            if (cc == null || cc.isDead || cc.customAction || cc.ragdolled)
                return;

            bool blockHeld = ReadBlockHeld();
            if (!blockHeld && !IsDrawnMeleeWeaponActive())
                return;

            if (!blockHeld && !ShouldApplyAttackThreatFacing())
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;

            if (blockHeld)
            {
                if (!IsDrawnMeleeWeaponActive() && !isBlocking)
                    return;

                DMMeleeBlockThreatFacing.ApplyBlockFacing(transform, profile);
                return;
            }

            DMMeleeBlockThreatFacing.ApplyAttackFacing(transform, profile);
        }

        private bool ShouldApplyAttackThreatFacing()
        {
            if (!IsDrawnMeleeWeaponActive())
                return false;
            if (cc.isRolling || cc.isJumping || isEquipping || lockInput || lockMeleeInput)
                return false;
            if (IsAiming || isReloading)
                return false;

            return isAttacking || _strongChargeArmed || _strongChargePoseActive;
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
