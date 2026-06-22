using UnityEngine;
using UnityEngine.InputSystem;
using ECM2;
using Project.Core;
using Project.Interaction;
using Project.Survival;

namespace Project.Player
{
    [RequireComponent(typeof(Character))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5.8f;
        [SerializeField] private float sprintSpeed = 9.5f;

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
        private bool _opticsOpen;
        private bool _gameplayPaused;
        private float _opticsTargetFov = 40f;
        private float _opticsCurrentFov = 40f;
        private float _cameraYaw;
        private float _cameraPitch;
        private float _currentFollowDistance;
        private float _followDistanceSmoothVelocity;

        public bool IsInventoryOpen => _inventoryOpen;
        public bool IsJournalOpen => _journalOpen;
        public bool IsMapOpen => _mapOpen;
        public bool IsQuestDialogOpen => _questDialogOpen;
        public bool IsLootDialogOpen => _lootDialogOpen;
        public bool IsOpticsOpen => _opticsOpen;
        public bool BlocksCombatInput =>
            _inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _opticsOpen || IsGameplayPaused;
        public bool IsGameplayPaused => _gameplayPaused || !GameSession.HasStarted;
        public float CameraYaw => _cameraYaw;
        public float OpticsZoomFov => _opticsCurrentFov;
        public float OpticsTargetFov => _opticsTargetFov;
        public Camera GameplayCamera => _character != null ? _character.camera : null;

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

            if (cameraFollowTarget == null)
            {
                Transform existing = transform.Find("CameraFollowTarget");
                if (existing != null)
                    cameraFollowTarget = existing;
            }
        }

        private void Start()
        {
            if (GameSession.HasStarted)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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

        private void ApplyCursorState()
        {
            bool cursorFree = _inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _gameplayPaused || _opticsOpen || !GameSession.HasStarted;
            Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = cursorFree;

            if ((_inventoryOpen || _journalOpen || _mapOpen || _opticsOpen || _questDialogOpen || _lootDialogOpen) && _character != null)
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

            if (!_inventoryOpen && !_journalOpen && !_mapOpen && !_questDialogOpen && !_lootDialogOpen)
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

            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _opticsOpen)
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

            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || _opticsOpen || IsGameplayPaused)
            {
                _character.SetMovementDirection(Vector3.zero);
                return;
            }

            HandleCrouchInput();
            HandleMovement();
            HandleJump();
            HandleZoom();
        }

        private void LateUpdate()
        {
            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || IsGameplayPaused || _character == null || _character.cameraTransform == null)
                return;

            ApplyLookInput();
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
            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || IsGameplayPaused || Mouse.current == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta.sqrMagnitude > 0.0001f)
                _lookInput = mouseDelta;
        }

        private void HandleCrouchInput()
        {
            if (_inventoryOpen || _journalOpen || _mapOpen || _questDialogOpen || _lootDialogOpen || IsGameplayPaused || _survivalStats != null && _survivalStats.IsDead)
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

            if (isSprinting && _survivalStats != null)
                _survivalStats.SetEnergy(_survivalStats.CurrentEnergy - Time.deltaTime * 12f);

            Vector3 movementDirection = Vector3.right * _moveInput.x + Vector3.forward * _moveInput.y;

            if (_character.cameraTransform != null)
                movementDirection = movementDirection.relativeTo(_character.cameraTransform);

            _character.SetMovementDirection(movementDirection);
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

        private void ApplyLookInput()
        {
            if (_lookInput.sqrMagnitude < 0.0001f) return;

            Vector2 scaledLook = _lookInput * mouseSensitivity;
            _cameraYaw = MathLib.ClampAngle(_cameraYaw + scaledLook.x, -180f, 180f);

            float pitchDelta = invertLook ? -scaledLook.y : scaledLook.y;
            _cameraPitch = MathLib.ClampAngle(_cameraPitch + pitchDelta, minPitch, maxPitch);

            _lookInput = Vector2.zero;
        }

        private void HandleZoom()
        {
            if (_opticsOpen)
                return;

            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scrollWheel, 0f)) return;

            followDistance = Mathf.Clamp(followDistance - scrollWheel * 8f, followMinDistance, followMaxDistance);
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
