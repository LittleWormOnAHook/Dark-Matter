using Invector.vCharacterController;
using Invector.vShooter;
using Invector;
using Invector.IK;
using Project.Core;
using Project.Data;
using Project.Features.Jetpack;
using Project.Features.Climb;
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
        private const float MouseLookScale = 0.1f;
        /// <summary>Discrete mouse-wheel zoom stops from min to max follow distance.</summary>
        private const int ZoomClickLevels = 10;
        private const int UiZoomRestoreFrames = 12;

        [Header("Pioneer Camera Zoom")]
        [SerializeField] private float runtimeMinCameraDistance = 1.6f;
        [SerializeField] private float runtimeMaxCameraDistance = 12f;
        [SerializeField] private float runtimeDefaultCameraDistance = 1.6f;
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
        private EquipmentController _equipment;
        private PlayerController _playerController;
        private bool _miningScanAimHold;
        /// <summary>Scroll zoom the player chose — preserved across aim/culling so ChangeState cannot wipe it.</summary>
        private float _preferredCameraZoom = -1f;
        private bool _wasAimingCameraLastFrame;
        private bool _wasUiBlockingLastFrame;
        private bool _wasSprintingLastFrame;
        private int _uiZoomRestoreFramesRemaining;
        private float _lockedAimZoom = -1f;
        private float _lastArmedCameraZoom = -1f;
        private Coroutine _startZoomRoutine;
        private static bool _loggedScrollStamp;

        protected override void Start()
        {
            base.Start();
            _inputBridge = GetComponent<PioneerInvectorInputBridge>();
            _jetpackInputBridge = GetComponent<DMJetpackInputBridge>();
            _climb = GetComponent<DMClimbController>();
            _equipment = GetComponent<EquipmentController>();
            _playerController = GetComponent<PlayerController>();
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
            // One more frame — loading handoff / ChangeState can rewrite distance after MarkStarted.
            yield return null;
            ApplyStartZoomIn();
            _startZoomRoutine = null;
        }

        private void ApplyStartZoomIn()
        {
            _preferredCameraZoom = runtimeMinCameraDistance;
            if (tpCamera == null)
                return;

            EnsureRuntimeZoomState();
            if (tpCamera.currentState != null)
                tpCamera.currentState.defaultDistance = runtimeMinCameraDistance;
            if (tpCamera.lerpState != null && !IsAimCameraStateName(tpCamera.lerpState.Name))
                tpCamera.lerpState.defaultDistance = runtimeMinCameraDistance;

            tpCamera.ForceSetZoomDistance(runtimeMinCameraDistance);
        }

        protected override void Update()
        {
            if (_inputBridge != null)
                _inputBridge.ApplyInputLocks(this);

            TryDrawWeaponOnAimPress();

            base.Update();
            SyncPioneerCursorState();
            PollFollowCameraZoom();
        }

        /// <summary>
        /// While held, keep aim active so mining resource scan (F / LB) can force aim without RMB/LT.
        /// Cleared when the scan key is released.
        /// </summary>
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
            base.AimInput();

            if (!_miningScanAimHold || cc == null || cc.ragdolled || CurrentActiveWeapon == null)
                return;

            isAimingByInput = true;

            // base.AimInput may have cleared strafe when RMB was not held — re-enter while scan-aiming.
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
        /// Right mouse with a sheathed weapon arms it. Ranged weapons additionally begin aiming so the
        /// same press doubles as ready-to-aim; melee weapons are only drawn. When a weapon is already
        /// drawn, right mouse falls through to the base shooter/melee aim handling unchanged.
        /// </summary>
        private void TryDrawWeaponOnAimPress()
        {
            if (_equipment == null || _equipment.IsWeaponDrawn)
                return;

            if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame)
                return;

            if (!GameSession.HasStarted || Time.timeScale <= 0f)
                return;

            if (_inputBridge != null && _inputBridge.ShouldLockGameplayInput())
                return;

            ItemData weapon = _equipment.EquippedItem;
            if (!EquipmentController.IsWeaponItem(weapon))
                return;

            if (!_equipment.DrawWeapon())
                return;

            if (EquipmentController.IsRangedWeaponItem(weapon))
                isAimingByInput = true;
        }

        protected override void LateUpdate()
        {
            if (tpCamera == null)
            {
                PlayerInvectorRuntimeSetup.EnsureThirdPersonCameraRigidbody(gameObject);
                FindCamera();
            }

            base.LateUpdate();
            SyncPioneerCursorState();
            PinAimFollowDistance();
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
        /// Called by WeaponModeSwitchController on a quick R tap (&lt;0.2s).
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

            if (!shooterManager || CurrentActiveWeapon == null || isReloading || shooterManager.isShooting)
                return;

            PerformManualReload();
        }

        private void PerformManualReload()
        {
            shootCountA = 0;
            _aimTiming = 0f;

            PioneerInvectorAmmoBridge ammoBridge = GetComponent<PioneerInvectorAmmoBridge>();

            // Invector ReloadWeapon always proceeds while isInfinityAmmo is set — gate on Pioneer
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
            get
            {
                // Guard before base IsAiming — it reads cc.isRolling and NRE's during early Awake.
                if (cc == null || shooterManager == null)
                    return isAimingByInput;

                return isAimingByInput || IsAiming;
            }
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

            if (Keyboard.current == null)
                return;

            if (cc.useContinuousSprint)
                cc.Sprint(Keyboard.current.leftShiftKey.wasPressedThisFrame);
            else
                cc.Sprint(Keyboard.current.leftShiftKey.isPressed);
        }

        public override void JumpInput()
        {
            if (!jumpInput.useInput || cc == null || !CanReadGameplayInput())
                return;

            if (_climb == null)
                _climb = GetComponent<DMClimbController>();

            if (_climb != null && _climb.TryHandleJumpPress())
                return;

            if (_jetpackInputBridge != null && _jetpackInputBridge.TryHandleJumpPress())
                return;

            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame &&
                JumpConditions())
            {
                cc.Jump(true);
            }

            if (Gamepad.current != null &&
                Gamepad.current.buttonSouth.wasPressedThisFrame &&
                JumpConditions())
            {
                cc.Jump(true);
            }
        }

        public override void CameraInput()
        {
            if (!cameraMain || !CanReadCameraInput())
                return;

            float x = 0f;
            float y = 0f;
            if (!lockCameraInput && Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                x = delta.x * MouseLookScale;
                y = delta.y * MouseLookScale;
            }

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
            tpCamera.RotateCamera(x, y);
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
                Debug.Log("DMCam 0831-scroll");
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
            if (opticsOwnsScroll || minimapOwnsScroll)
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
            float range = runtimeMaxCameraDistance - runtimeMinCameraDistance;
            float step = range / (ZoomClickLevels - 1);
            float current = tpCamera.CurrentZoom > 0.01f ? tpCamera.CurrentZoom : GetPreferredZoom();
            int currentLevel = Mathf.RoundToInt((current - runtimeMinCameraDistance) / step);
            int nextLevel = Mathf.Clamp(currentLevel - direction, 0, ZoomClickLevels - 1);
            float next = runtimeMinCameraDistance + nextLevel * step;

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
            if (zoom >= runtimeMinCameraDistance - 0.05f)
                _preferredCameraZoom = Mathf.Clamp(zoom, runtimeMinCameraDistance, runtimeMaxCameraDistance);
        }

        private float GetPreferredZoom()
        {
            if (_preferredCameraZoom >= runtimeMinCameraDistance - 0.05f)
                return Mathf.Clamp(_preferredCameraZoom, runtimeMinCameraDistance, runtimeMaxCameraDistance);
            return runtimeDefaultCameraDistance;
        }

        private float GetGameplayFollowZoom()
        {
            float preferred = GetPreferredZoom();
            bool sprinting = cc != null && cc.isSprinting && cc.input.sqrMagnitude > 0.01f && !IsAimingActive;
            if (sprinting)
            {
                return Mathf.Clamp(
                    preferred + Mathf.Max(0f, sprintZoomOutMeters),
                    runtimeMinCameraDistance,
                    runtimeMaxCameraDistance);
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
            // Do NOT ForceSet when distance alone dips from wall culling — that wiped scroll zoom.
            if (!aiming
                && tpCamera.CurrentZoom < runtimeMinCameraDistance - 0.01f
                && tpCamera.distance < runtimeMinCameraDistance - 0.01f)
            {
                tpCamera.ForceSetZoomDistance(GetGameplayFollowZoom());
                return;
            }

            if (aiming)
            {
                if (!_wasAimingCameraLastFrame || _lockedAimZoom < aimMinCameraDistance - 0.05f)
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

            // One-shot restore when leaving aim — pull back to the player's scroll preference.
            if (_wasAimingCameraLastFrame)
            {
                _wasAimingCameraLastFrame = false;
                RestorePreferredZoom(force: true);
            }

            // Slight sprint pull-out only (never jump to max distance).
            bool sprinting = cc != null && cc.isSprinting && cc.input.sqrMagnitude > 0.01f;
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
            float armedBaseline = _lastArmedCameraZoom > aimMinCameraDistance
                ? _lastArmedCameraZoom
                : currentDistance;

            if (armedBaseline < aimMinCameraDistance)
                armedBaseline = Mathf.Max(aimMinCameraDistance, GetPreferredZoom());

            _lockedAimZoom = GetWeaponAimTargetDistance(weapon, armedBaseline);

            vThirdPersonCameraState state = tpCamera.currentState;
            // Keep useZoom so CameraMovement lerps toward currentZoom instead of
            // Slerping defaultDistance from the Aim list asset (that fight jittered ADS).
            state.useZoom = true;
            state.minDistance = aimMinCameraDistance;
            state.maxDistance = Mathf.Max(_lockedAimZoom + 0.25f, aimMinCameraDistance);

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
            if (zoom >= aimMinCameraDistance - 0.05f)
                _lastArmedCameraZoom = zoom;
        }

        private void PinAimFollowDistance()
        {
            if (tpCamera?.currentState == null || !IsAimCameraStateName(tpCamera.currentStateName))
                return;

            if (_lockedAimZoom < aimMinCameraDistance - 0.01f)
                return;

            // Correct target only — do not snap distance (ForceSet fought wall-cull lerp).
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
            armedDistance = Mathf.Max(armedDistance, aimMinCameraDistance);

            if (weapon != null && weapon.weaponGrip == WeaponGrip.TwoHanded)
            {
                return Mathf.Clamp(
                    armedDistance * 0.68f,
                    aimMinCameraDistance,
                    armedDistance - 0.1f);
            }

            if (weapon != null && EquipmentController.IsRangedWeaponItem(weapon))
            {
                return Mathf.Clamp(
                    armedDistance * 0.82f,
                    aimMinCameraDistance,
                    armedDistance - 0.08f);
            }

            return Mathf.Clamp(
                armedDistance - Mathf.Max(0.12f, aimZoomPullInMeters * 0.35f),
                aimMinCameraDistance,
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

            // Always restore the player's scroll preference — never a UI-bloated follow distance.
            float preferred = GetPreferredZoom();
            if (_preferredCameraZoom < runtimeMinCameraDistance - 0.05f)
                preferred = runtimeDefaultCameraDistance;

            if (force || Mathf.Abs(tpCamera.CurrentZoom - preferred) > 0.05f)
                tpCamera.ForceSetZoomDistance(preferred);
        }

        private void ApplyFreeLookZoomRange(vThirdPersonCameraState state)
        {
            if (state == null)
                return;

            state.useZoom = true;
            state.minDistance = runtimeMinCameraDistance;
            state.maxDistance = Mathf.Max(runtimeMaxCameraDistance, state.maxDistance, GetPreferredZoom());
            if (state.defaultDistance < runtimeMinCameraDistance
                || state.defaultDistance > runtimeMaxCameraDistance * 1.5f)
            {
                state.defaultDistance = Mathf.Clamp(
                    state.defaultDistance > 0.01f ? state.defaultDistance : runtimeDefaultCameraDistance,
                    runtimeMinCameraDistance,
                    state.maxDistance);
            }
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

        private static Vector2 ReadMoveVector()
        {
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
    }
}
