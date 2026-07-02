using UnityEngine;
using UnityEngine.InputSystem;
using ECM2;
using Project.Core;
using Project.Interaction;
using Project.Survival;
using Project.UI;

namespace Project.Player
{
    [RequireComponent(typeof(Character))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.4f;
        [SerializeField] private float sprintSpeed = 7.2f;

        [Header("Locomotion Animations")]
        [SerializeField] private PlayerLocomotionAnimationSettings locomotionAnimations = new();

        [Header("Third Person Camera")]
        [SerializeField] private Transform cameraFollowTarget;
        [SerializeField] private float followDistance = 7f;
        [SerializeField] private float followMinDistance = 3f;
        [SerializeField] private float followMaxDistance = 12f;
        [SerializeField] private bool invertLook = true;
        [SerializeField] private Vector2 mouseSensitivity = new Vector2(0.15f, 0.15f);
        [SerializeField] private float minPitch = -70f;
        [SerializeField] private float maxPitch = 70f;

        [Header("Camera Collision")]
        [SerializeField] private LayerMask cameraCollisionMask = ~0;
        [SerializeField] private float cameraCollisionRadius = 0.25f;
        [SerializeField] private float cameraCollisionPadding = 0.2f;
        [SerializeField] private float terrainClearance = 0.35f;

        [Header("Optics")]
        [SerializeField] private float opticsZoomLerpSpeed = 28f;
        [SerializeField] private Vector3 opticsEyeOffset = new Vector3(0f, 0.08f, 0.12f);

        private Character _character;
        private SurvivalStats _survivalStats;
        private WorldUseController _worldUse;

        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _sprintInput;
        private bool _crouchInput;
        private bool _jumpPressed;
        private bool _inventoryOpen;
        private bool _journalOpen;
        private bool _mapOpen;
        private bool _questDialogOpen;
        private bool _lootDialogOpen;
        private bool _buildingControlOpen;
        private bool _opticsOpen;
        private bool _gameplayPaused;
        private float _opticsTargetFov = 40f;
        private float _opticsCurrentFov = 40f;
        private float _cameraYaw;
        private float _cameraPitch;
        private float _currentFollowDistance;
        private float _followDistanceSmoothVelocity;
        private float _inputRecoveryCheckTimer;
        private float _locomotionRotationIdleSince = float.NegativeInfinity;
        private CombatFocusController _combatFocus;

        private const float LocomotionRotationIdleDelay = 0.12f;

        public bool IsInventoryOpen => _inventoryOpen;
        public bool IsJournalOpen => _journalOpen;
        public bool IsMapOpen => _mapOpen;
        public bool IsQuestDialogOpen => _questDialogOpen;
        public bool IsLootDialogOpen => _lootDialogOpen;
        public bool IsBuildingControlOpen => _buildingControlOpen;
        public bool IsOpticsOpen => _opticsOpen;
        public bool IsSprinting =>
            _sprintInput && _moveInput.sqrMagnitude > 0.01f && _character != null && !_character.IsCrouched();
        public bool BlocksCombatInput =>
            _inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen || _opticsOpen || IsGameplayPaused;
        public bool IsGameplayPaused => _gameplayPaused || !GameSession.HasStarted;
        public float CameraYaw => _cameraYaw;
        public float LastLookYawDelta { get; private set; }
        public Vector2 MoveInput => _moveInput;
        public float OpticsZoomFov => _opticsCurrentFov;
        public float OpticsTargetFov => _opticsTargetFov;
        public Camera GameplayCamera => _character != null ? _character.camera : null;
        public PlayerLocomotionAnimationSettings LocomotionAnimations => locomotionAnimations;

        public Vector3 OpticsEyeWorldPosition
        {
            get
            {
                Transform pivot = cameraFollowTarget != null ? cameraFollowTarget : transform;
                return pivot.position + pivot.TransformDirection(opticsEyeOffset);
            }
        }

        public Quaternion OpticsLookRotation =>
            _character?.cameraTransform != null
                ? _character.cameraTransform.rotation
                : Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);

        private void Awake()
        {
            _character = GetComponent<Character>();
            _survivalStats = GetComponent<SurvivalStats>();
            _worldUse = GetComponent<WorldUseController>();
            _combatFocus = GetComponent<CombatFocusController>();
            if (_combatFocus == null)
                _combatFocus = gameObject.AddComponent<CombatFocusController>();

            if (cameraFollowTarget == null)
            {
                Transform existing = transform.Find("CameraFollowTarget");
                if (existing != null)
                    cameraFollowTarget = existing;
            }
        }

        private void Start()
        {
            if (_character != null)
                _character.rotationMode = Character.RotationMode.OrientRotationToMovement;

            if (GameSession.HasStarted)
                GameplayInputRecovery.ReleaseAllInputCapture();
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (_character.camera == null && Camera.main != null)
                _character.camera = Camera.main;

            if (_character.cameraTransform != null)
            {
                Vector3 euler = _character.cameraTransform.eulerAngles;
                _cameraPitch = euler.x;
                _cameraYaw = euler.y;
            }

            _currentFollowDistance = followDistance;
            _character.maxWalkSpeed = walkSpeed;

            GameplayAudioUtility.EnsureListenerOnCamera(_character.camera);
        }

        public void SetInventoryOpen(bool open)
        {
            _inventoryOpen = open;
            ApplyCursorState();
        }

        public void SetJournalOpen(bool open)
        {
            _journalOpen = open;
            ApplyCursorState();

            if (open && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        public void SetMapOpen(bool open)
        {
            _mapOpen = open;
            ApplyCursorState();

            if (open && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        public void SetQuestDialogOpen(bool open)
        {
            _questDialogOpen = open;
            ApplyCursorState();

            if (open && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        public void SetLootDialogOpen(bool open)
        {
            _lootDialogOpen = open;
            ApplyCursorState();

            if (open && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        public void SetBuildingControlOpen(bool open)
        {
            _buildingControlOpen = open;
            ApplyCursorState();

            if (open && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        public void SetOpticsOpen(bool open, float zoomFov = 40f)
        {
            if (open == _opticsOpen && (!open || Mathf.Approximately(_opticsTargetFov, zoomFov)))
                return;

            _opticsOpen = open;
            _opticsTargetFov = zoomFov;
            _opticsCurrentFov = zoomFov;

            ApplyCursorState();

            if (open && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        public void SetOpticsZoomFov(float zoomFov)
        {
            _opticsTargetFov = zoomFov;
        }

        public void SetOpticsZoomTarget(float zoomFov)
        {
            _opticsTargetFov = zoomFov;
        }

        public void SnapOpticsZoom(float zoomFov)
        {
            _opticsTargetFov = zoomFov;
            _opticsCurrentFov = zoomFov;
        }

        public void AdjustOpticsZoom(float delta)
        {
            if (!_opticsOpen)
                return;

            _opticsTargetFov = Mathf.Clamp(_opticsTargetFov - delta * 4f, 16f, 70f);
        }

        public void SetGameplayPaused(bool paused)
        {
            _gameplayPaused = paused;
            ApplyCursorState();

            if (paused && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        /// <summary>
        /// Clears stale UI pause flags and reapplies cursor lock after menus / popups close.
        /// </summary>
        public void EnsureGameplayInputReady()
        {
            _inventoryOpen = false;
            _journalOpen = false;
            _mapOpen = false;
            _questDialogOpen = false;
            _lootDialogOpen = false;
            _buildingControlOpen = false;
            _gameplayPaused = false;

            PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
            if (playerInput != null)
                playerInput.enabled = true;

            ApplyCursorState();
        }

        private void ApplyCursorState()
        {
            bool cursorFree = _inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen || _gameplayPaused || !GameSession.HasStarted;

            if (_opticsOpen)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            else if (cursorFree)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if ((_inventoryOpen || _journalOpen || _mapOpen || _opticsOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen) && _character != null)
                _character.SetMovementDirection(Vector3.zero);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (IsGameplayPaused)
                return;

            _moveInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (IsGameplayPaused)
                return;

            if (!_inventoryOpen && !_journalOpen && !_mapOpen && !_questDialogOpen && !_lootDialogOpen && !_buildingControlOpen)
                _lookInput = context.ReadValue<Vector2>();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (IsGameplayPaused)
                return;

            _sprintInput = context.ReadValueAsButton();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (IsGameplayPaused)
                return;

            if (context.performed)
                _jumpPressed = true;
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (IsGameplayPaused)
                return;

            if (context.started || context.performed)
                _crouchInput = true;
            else if (context.canceled)
                _crouchInput = false;
        }

        public void OnUse(InputAction.CallbackContext context)
        {
            if (!context.started && !context.performed)
                return;

            if (!GameSession.HasStarted || _gameplayPaused)
                return;

            if (_survivalStats != null && _survivalStats.IsDead)
                return;

            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen || _opticsOpen)
                return;

            if (_worldUse == null)
                _worldUse = GetComponent<WorldUseController>();

            if (_worldUse == null)
                _worldUse = gameObject.AddComponent<WorldUseController>();

            _worldUse.TryUse();
        }

        public void OnInteract(InputAction.CallbackContext context) => OnUse(context);

        private void Update()
        {
            if (_character == null) return;

            PollLookInput();

            if (_survivalStats != null && _survivalStats.IsDead)
            {
                _character.SetMovementDirection(Vector3.zero);
                return;
            }

            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen || IsGameplayPaused)
            {
                _character.SetMovementDirection(Vector3.zero);
                return;
            }

            HandleCrouchInput();
            HandleMovement();
            HandleJump();
            HandleZoom();
            TryRecoverStaleInputBlockers();
        }

        private void TryRecoverStaleInputBlockers()
        {
            if (!GameSession.HasStarted || Time.timeScale <= 0f)
                return;

            _inputRecoveryCheckTimer += Time.unscaledDeltaTime;
            if (_inputRecoveryCheckTimer < 1f)
                return;

            _inputRecoveryCheckTimer = 0f;

            if (HasVisibleUiBlockingInput())
                return;

            if (!_journalOpen && !_mapOpen && !_lootDialogOpen && !_questDialogOpen && !_buildingControlOpen && !_gameplayPaused)
                return;

            EnsureGameplayInputReady();
        }

        private static bool HasVisibleUiBlockingInput()
        {
            FullscreenUiNavigator navigator = FullscreenUiNavigator.Instance;
            if (navigator != null && navigator.IsAnyOpen)
                return true;

            if (EnemyLootDialogUI.IsDialogOpen)
                return true;

            return false;
        }

        private void LateUpdate()
        {
            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen || IsGameplayPaused || _character == null || _character.cameraTransform == null)
                return;

            ApplyLookInput();
            _combatFocus?.UpdateFocus();
            UpdateCamera();

            if (_opticsOpen)
            {
                _opticsCurrentFov = Mathf.Lerp(
                    _opticsCurrentFov,
                    _opticsTargetFov,
                    Time.deltaTime * opticsZoomLerpSpeed);
            }
        }

        private void PollLookInput()
        {
            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen || IsGameplayPaused)
                return;

            if (Mouse.current == null)
                return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta.sqrMagnitude > 0.0001f)
                _lookInput = mouseDelta;
        }

        private void HandleCrouchInput()
        {
            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _buildingControlOpen || IsGameplayPaused || _survivalStats != null && _survivalStats.IsDead)
                return;

            bool wantCrouch = _crouchInput;
            if (Keyboard.current != null)
                wantCrouch |= Keyboard.current.leftCtrlKey.isPressed;

            if (wantCrouch)
                _character.Crouch();
            else
                _character.UnCrouch();
        }

        private void HandleMovement()
        {
            bool isMoving = _moveInput.sqrMagnitude > 0.01f;
            bool isSprinting = _sprintInput && isMoving && !_character.IsCrouched();

            _character.maxWalkSpeed = isSprinting ? sprintSpeed : walkSpeed;

            if (_survivalStats != null)
            {
                _survivalStats.SetSprinting(isSprinting);
                if (isSprinting)
                    _survivalStats.SetStamina(_survivalStats.CurrentStamina - Time.deltaTime * 12f);
            }

            Vector3 movementDirection = Vector3.right * _moveInput.x + Vector3.forward * _moveInput.y;

            if (_character.cameraTransform != null)
                movementDirection = movementDirection.relativeTo(_character.cameraTransform);

            _character.SetMovementDirection(movementDirection);
            UpdateLocomotionRotationMode(isMoving);
        }

        private void UpdateLocomotionRotationMode(bool isMoving)
        {
            if (_character == null || (_combatFocus != null && _combatFocus.IsLocked))
                return;

            if (isMoving)
            {
                _locomotionRotationIdleSince = float.NegativeInfinity;
                _character.rotationMode = Character.RotationMode.OrientRotationToViewDirection;
                return;
            }

            if (_locomotionRotationIdleSince < 0f)
                _locomotionRotationIdleSince = Time.time;

            if (Time.time - _locomotionRotationIdleSince < LocomotionRotationIdleDelay)
                return;

            _character.rotationMode = Character.RotationMode.OrientRotationToMovement;
        }

        private void HandleJump()
        {
            if (_jumpPressed)
            {
                _character.Jump();
                _jumpPressed = false;
            }

            if (Keyboard.current != null && !Keyboard.current.spaceKey.isPressed)
                _character.StopJumping();
        }

        public Vector3 GetPlanarForward()
        {
            return Quaternion.Euler(0f, _cameraYaw, 0f) * Vector3.forward;
        }

        public Vector3 GetCameraRelativeMoveDirection()
        {
            Vector3 movementDirection = Vector3.right * _moveInput.x + Vector3.forward * _moveInput.y;
            if (_character != null && _character.cameraTransform != null)
                movementDirection = movementDirection.relativeTo(_character.cameraTransform);

            movementDirection.y = 0f;
            return movementDirection;
        }

        public void ApplyCombatFocusYaw(float targetYawDegrees, float smoothLambda)
        {
            _cameraYaw = MathLib.Damp(_cameraYaw, targetYawDegrees, smoothLambda, Time.deltaTime);
            _cameraYaw = MathLib.ClampAngle(_cameraYaw, -180f, 180f);
        }

        private void ApplyLookInput()
        {
            if (_lookInput.sqrMagnitude < 0.0001f)
            {
                LastLookYawDelta = 0f;
                return;
            }

            Vector2 scaledLook = _lookInput * mouseSensitivity;
            LastLookYawDelta = scaledLook.x;
            _cameraYaw = MathLib.ClampAngle(_cameraYaw + scaledLook.x, -180f, 180f);

            float pitchDelta = invertLook ? -scaledLook.y : scaledLook.y;
            _cameraPitch = MathLib.ClampAngle(_cameraPitch + pitchDelta, minPitch, maxPitch);

            _lookInput = Vector2.zero;
        }

        private void HandleZoom()
        {
            if (_opticsOpen || Mouse.current == null)
                return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
                return;

            const float scrollUnitsPerNotch = 120f;
            followDistance = Mathf.Clamp(
                followDistance - (scroll / scrollUnitsPerNotch) * 8f,
                followMinDistance,
                followMaxDistance);
        }

        private void UpdateCamera()
        {
            Transform cameraTransform = _character.cameraTransform;

            cameraTransform.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);

            float followTarget = followDistance;
            _currentFollowDistance = Mathf.SmoothDamp(
                _currentFollowDistance,
                followTarget,
                ref _followDistanceSmoothVelocity,
                0.1f);

            Transform followTargetTransform = cameraFollowTarget != null ? cameraFollowTarget : transform;
            Vector3 pivot = followTargetTransform.position;
            Vector3 desiredPosition = pivot - cameraTransform.forward * _currentFollowDistance;
            desiredPosition = ResolveCameraCollision(pivot, desiredPosition);
            cameraTransform.position = desiredPosition;
        }

        private Vector3 ResolveCameraCollision(Vector3 pivot, Vector3 desiredPosition)
        {
            Vector3 toCamera = desiredPosition - pivot;
            float distance = toCamera.magnitude;
            if (distance > 0.01f)
            {
                Vector3 direction = toCamera / distance;
                const float castSkin = 0.2f;
                Vector3 castOrigin = pivot + direction * castSkin;
                float castDistance = Mathf.Max(0f, distance - castSkin);

                if (Physics.SphereCast(
                        castOrigin,
                        cameraCollisionRadius,
                        direction,
                        out RaycastHit hit,
                        castDistance,
                        cameraCollisionMask,
                        QueryTriggerInteraction.Ignore) &&
                    !IsPlayerCollider(hit.collider))
                {
                    desiredPosition = hit.point + hit.normal * cameraCollisionPadding;
                }
            }

            return ClampAboveTerrain(desiredPosition);
        }

        private bool IsPlayerCollider(Collider collider)
        {
            return collider != null && collider.transform.IsChildOf(transform);
        }

        private Vector3 ClampAboveTerrain(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return position;

            float terrainHeight = terrain.SampleHeight(position) + terrain.transform.position.y;
            float minimumHeight = terrainHeight + terrainClearance;
            if (position.y < minimumHeight)
                position.y = minimumHeight;

            return position;
        }

        public void RefreshCameraFollow()
        {
            if (_character == null || _character.cameraTransform == null)
                return;

            if (_character.camera == null && Camera.main != null)
                _character.camera = Camera.main;

            _cameraYaw = transform.eulerAngles.y;
            _cameraPitch = Mathf.Clamp(_cameraPitch, minPitch, maxPitch);
            _currentFollowDistance = followDistance;
            _followDistanceSmoothVelocity = 0f;
            GameplayAudioUtility.EnsureListenerOnCamera(_character.camera);
            UpdateCamera();
        }
    }

}
