using Invector.vCharacterController;
using Invector.vShooter;
using Invector;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
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
        private const float ScrollUnitsPerNotch = 120f;

        [Header("Pioneer Camera Zoom")]
        [SerializeField] private float scrollZoomNotchScale = 0.75f;
        [SerializeField] private float runtimeMinCameraDistance = 2.5f;
        [SerializeField] private float runtimeMaxCameraDistance = 12f;
        [SerializeField] private float runtimeDefaultCameraDistance = 5.5f;

        private PioneerInvectorInputBridge _inputBridge;
        private EquipmentController _equipment;
        private PlayerController _playerController;
        private bool _miningScanAimHold;
        /// <summary>Scroll zoom the player chose — preserved across aim/culling so ChangeState cannot wipe it.</summary>
        private float _preferredCameraZoom = -1f;
        private bool _wasAimingCameraLastFrame;

        protected override void Start()
        {
            base.Start();
            _inputBridge = GetComponent<PioneerInvectorInputBridge>();
            _equipment = GetComponent<EquipmentController>();
            _playerController = GetComponent<PlayerController>();
            SyncPioneerCursorState();
        }

        protected override void Update()
        {
            if (_inputBridge != null)
                _inputBridge.ApplyInputLocks(this);

            TryDrawWeaponOnAimPress();

            base.Update();
            SyncPioneerCursorState();
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
            base.LateUpdate();
            SyncPioneerCursorState();
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

            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame &&
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

            if (!lockCameraInput && Mouse.current != null)
            {
                // Binocular FOV zoom is owned by OpticsController — don't also change follow distance.
                bool opticsOwnsScroll = _playerController != null && _playerController.IsOpticsOpen;
                if (!opticsOwnsScroll)
                {
                    float scroll = Mouse.current.scroll.ReadValue().y / ScrollUnitsPerNotch * scrollZoomNotchScale;
                    if (Mathf.Abs(scroll) > 0.001f)
                    {
                        tpCamera.Zoom(scroll);
                        RememberPreferredZoom();
                    }
                }
            }
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
            if (zoom >= runtimeMinCameraDistance)
                _preferredCameraZoom = Mathf.Clamp(zoom, runtimeMinCameraDistance, runtimeMaxCameraDistance);
        }

        private float GetPreferredZoom()
        {
            if (_preferredCameraZoom >= runtimeMinCameraDistance)
                return Mathf.Clamp(_preferredCameraZoom, runtimeMinCameraDistance, runtimeMaxCameraDistance);
            return runtimeDefaultCameraDistance;
        }

        /// <summary>
        /// Keep free-look third-person zoom playable without fighting Aim camera states.
        /// Never rewrite Aiming lerpState (shared list refs) or ForceSet from temporary culling dips.
        /// </summary>
        private void EnsureRuntimeZoomState()
        {
            if (tpCamera?.currentState == null)
                return;

            bool aiming = IsAimCameraStateName(tpCamera.currentStateName);

            if (!aiming && tpCamera.CurrentZoom >= runtimeMinCameraDistance)
            {
                // Track live free-look zoom so aim / ChangeState cannot permanently wipe it.
                _preferredCameraZoom = Mathf.Clamp(
                    tpCamera.CurrentZoom,
                    runtimeMinCameraDistance,
                    runtimeMaxCameraDistance);
            }

            // Only repair a permanently broken zoom (e.g. optics left near-zero).
            // Do NOT ForceSet when distance alone dips from wall culling — that wiped scroll zoom.
            if (!aiming
                && tpCamera.CurrentZoom < runtimeMinCameraDistance - 0.01f
                && tpCamera.distance < runtimeMinCameraDistance - 0.01f)
            {
                tpCamera.ForceSetZoomDistance(GetPreferredZoom());
                return;
            }

            if (aiming)
            {
                _wasAimingCameraLastFrame = true;
                return;
            }

            // Free-look only: enable scroll zoom range without mutating Aim list assets via lerpState.
            // lerpState is a live reference into CameraStateList — only touch it when it is also free-look,
            // otherwise FixedUpdate Slerp copies useZoom=false from Default every physics tick and kills zoom.
            ApplyFreeLookZoomRange(tpCamera.currentState);
            if (tpCamera.lerpState != null && !IsAimCameraStateName(tpCamera.lerpState.Name))
                ApplyFreeLookZoomRange(tpCamera.lerpState);

            // One-shot restore when leaving aim — do not fight intentional scroll-in every frame.
            if (_wasAimingCameraLastFrame)
            {
                _wasAimingCameraLastFrame = false;
                float preferred = GetPreferredZoom();
                if (_preferredCameraZoom >= runtimeMinCameraDistance
                    && tpCamera.CurrentZoom + 0.05f < preferred)
                {
                    tpCamera.ForceSetZoomDistance(preferred);
                }
            }
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
            base.UpdateShooterAnimations();

            if (shooterManager == null || shotLayer < 0 || CurrentActiveWeapon == null)
                return;

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
