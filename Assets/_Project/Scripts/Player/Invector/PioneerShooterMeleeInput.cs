using Invector.vCharacterController;
using Invector.vShooter;
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
        [SerializeField] private float runtimeMaxCameraDistance = 10f;

        private PioneerInvectorInputBridge _inputBridge;
        private EquipmentController _equipment;

        protected override void Start()
        {
            base.Start();
            _inputBridge = GetComponent<PioneerInvectorInputBridge>();
            _equipment = GetComponent<EquipmentController>();
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

        private void SyncPioneerCursorState()
        {
            GetComponent<PlayerController>()?.ApplyCursorState();
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

        public bool IsAimingActive => isAimingByInput || IsAiming;

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
            if (!cameraMain || tpCamera == null || !CanReadCameraInput())
                return;

            EnsureRuntimeZoomState();

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

            tpCamera.RotateCamera(x, y);

            if (!lockCameraInput && Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y / ScrollUnitsPerNotch * scrollZoomNotchScale;
                if (Mathf.Abs(scroll) > 0.001f)
                    tpCamera.Zoom(scroll);
            }
        }

        private void EnsureRuntimeZoomState()
        {
            if (tpCamera?.currentState == null)
                return;

            if (tpCamera.currentState.useZoom &&
                tpCamera.currentState.minDistance > 0.01f &&
                tpCamera.currentState.maxDistance > tpCamera.currentState.minDistance)
            {
                return;
            }

            float currentDistance = tpCamera.distance > 0.01f
                ? tpCamera.distance
                : Mathf.Max(runtimeMinCameraDistance, tpCamera.currentState.defaultDistance);

            tpCamera.currentState.useZoom = true;
            tpCamera.currentState.minDistance = Mathf.Min(runtimeMinCameraDistance, currentDistance);
            tpCamera.currentState.maxDistance = Mathf.Max(runtimeMaxCameraDistance, currentDistance);
            if (tpCamera.currentState.defaultDistance <= 0.01f)
                tpCamera.currentState.defaultDistance = currentDistance;
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

            bool isRifle = _equipment != null &&
                           EquipmentController.IsRangedWeaponItem(_equipment.DrawnWeaponItem) &&
                           _equipment.DrawnWeaponItem.weaponGrip == WeaponGrip.TwoHanded;

            float weight;
            if (IsAiming && isUsingScopeView)
                weight = isRifle
                    ? PioneerInvectorRecoilUtility.RifleScopeShotLayerWeight
                    : PioneerInvectorRecoilUtility.ScopeShotLayerWeight;
            else
                weight = isRifle
                    ? PioneerInvectorRecoilUtility.RifleShotLayerWeight
                    : PioneerInvectorRecoilUtility.ShotLayerWeight;

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
