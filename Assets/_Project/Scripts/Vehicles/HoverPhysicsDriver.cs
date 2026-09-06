using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Custom hover physics: raycast springs, W/S thrust, A/D strafe or yaw, arrow up/down altitude band.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-200)]
    public class HoverPhysicsDriver : MonoBehaviour
    {
        [SerializeField] private HovercraftProfile profile;
        [SerializeField] private Transform[] hoverRayPoints;
        [SerializeField] private Transform visualRoot;

        private Rigidbody _rigidbody;
        private Vector2 _moveInput;
        private float _verticalInput;
        private bool _boosterActive;
        private float _targetAltitudeAboveGround;
        private Vector3 _visualBaseLocalEuler;
        private float _turbulenceSeed;
        private float _currentDrivePitch;
        private float _currentDriveRoll;
        private float _forwardDriveTimer;
        private bool _isOccupied;
        private bool _steerWithYaw;

        public HovercraftProfile Profile => profile;
        public bool IsOccupied => _isOccupied;
        public float CurrentPlanarSpeed { get; private set; }
        public float CurrentSpeedRatio { get; private set; }
        public float TurbulenceAmplitude { get; private set; }
        public bool BoosterActive => _boosterActive;

        /// <summary>Planar stick magnitude (0-1). Boost is reported separately.</summary>
        public float CurrentThrottle => Mathf.Clamp01(_moveInput.magnitude);

        /// <summary>Current visual drive pitch (degrees) applied to the mesh root.</summary>
        public float CurrentDrivePitch => _currentDrivePitch;

        /// <summary>Current visual drive roll / bank (degrees) applied to the mesh root.</summary>
        public float CurrentDriveRoll => _currentDriveRoll;

        /// <summary>Local tilt quaternion matching the visual root bank (excludes high-frequency turbulence).</summary>
        public Quaternion CurrentDriveTiltLocal =>
            Quaternion.Euler(_currentDrivePitch, 0f, _currentDriveRoll);

        /// <summary>
        /// Exact local rotation delta currently on <see cref="visualRoot"/> vs its rest pose
        /// (drive bank + visual turbulence). Apply in craft-root space for 1:1 camera banking.
        /// </summary>
        public Quaternion CurrentVisualBankLocal
        {
            get
            {
                if (visualRoot == null)
                    return CurrentDriveTiltLocal;

                return visualRoot.localRotation *
                       Quaternion.Inverse(Quaternion.Euler(_visualBaseLocalEuler));
            }
        }

        private readonly RaycastHit[] _raycastHits = new RaycastHit[8];
        private Collider[] _ownColliders;

        // Fallback ray point set when no hoverRayPoints are authored — cached so the per-tick
        // hover solve does not allocate a single-element array every FixedUpdate.
        private Transform[] _selfHoverRayPoints;

        private void Awake()
        {
            _selfHoverRayPoints = new[] { transform };
            _rigidbody = GetComponent<Rigidbody>();
            _turbulenceSeed = Random.Range(0f, 100f);
            CacheOwnColliders();
            ResolveVisualRoot();
        }

        private void Start()
        {
            if (!Application.isPlaying)
                return;

            ResolveVisualRoot();
            ApplyProfileSettings();
            SetOccupied(false);
            SnapToHoverAltitude(true);
        }

        private void OnEnable()
        {
            CacheOwnColliders();
        }

        private void CacheOwnColliders()
        {
            _ownColliders = GetComponentsInChildren<Collider>(true);
        }

        private bool IsOwnCollider(Collider collider)
        {
            if (collider == null || _ownColliders == null)
                return false;

            for (int i = 0; i < _ownColliders.Length; i++)
            {
                if (_ownColliders[i] == collider)
                    return true;
            }

            return false;
        }

        public void Configure(HovercraftProfile hoverProfile, Transform[] rayPoints, Transform visual = null)
        {
            profile = hoverProfile;
            hoverRayPoints = rayPoints;
            if (visual != null)
                visualRoot = visual;

            ResolveVisualRoot();
            ApplyProfileSettings();
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot != null)
            {
                _visualBaseLocalEuler = visualRoot.localEulerAngles;
                return;
            }

            Transform namedVisual = transform.Find("Visual");
            if (namedVisual != null)
            {
                visualRoot = namedVisual;
                _visualBaseLocalEuler = visualRoot.localEulerAngles;
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Transform candidate = renderer.transform;
                while (candidate.parent != null && candidate.parent != transform)
                    candidate = candidate.parent;

                if (candidate.parent != transform)
                    continue;

                visualRoot = candidate;
                _visualBaseLocalEuler = visualRoot.localEulerAngles;
                return;
            }
        }

        public void SetDriveInput(Vector2 moveInput, float verticalInput, bool boosterActive, bool steerWithYaw = false)
        {
            _moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            _verticalInput = Mathf.Clamp(verticalInput, -1f, 1f);
            _boosterActive = boosterActive;
            _steerWithYaw = steerWithYaw;
        }

        public void SetOccupied(bool occupied)
        {
            _isOccupied = occupied;
            if (profile == null)
                return;

            if (occupied)
            {
                _targetAltitudeAboveGround = profile.hoverHeight;
                return;
            }

            _moveInput = Vector2.zero;
            _verticalInput = 0f;
            _boosterActive = false;
            _steerWithYaw = false;
            _targetAltitudeAboveGround = profile.parkedAltitudeAboveGround;

            if (_rigidbody == null || _rigidbody.isKinematic)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.x *= 0.35f;
            velocity.z *= 0.35f;
            TrySetLinearVelocity(velocity);
        }

        /// <summary>Current craft height above sampled ground, or -1 when ground cannot be resolved.</summary>
        public float GetAltitudeAboveGround()
        {
            if (!TrySampleGroundHeight(out float groundY))
                return -1f;

            return transform.position.y - groundY;
        }

        public float TargetAltitudeAboveGround => _targetAltitudeAboveGround;

        private void ApplyProfileSettings()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null || !Application.isPlaying)
                return;

            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            if (profile == null)
                return;

            _rigidbody.linearDamping = profile.linearDrag;
            _rigidbody.angularDamping = profile.angularDrag;
            RefreshTargetAltitude();
        }

        private void RefreshTargetAltitude()
        {
            if (profile == null)
                return;

            _targetAltitudeAboveGround = _isOccupied
                ? profile.hoverHeight
                : profile.parkedAltitudeAboveGround;
            _targetAltitudeAboveGround = Mathf.Clamp(
                _targetAltitudeAboveGround,
                GetMinAltitudeLimit(),
                GetMaxAltitudeLimit());
        }

        private float GetMinAltitudeLimit()
        {
            if (profile == null)
                return 0.05f;

            return _isOccupied
                ? profile.minAltitudeAboveGround
                : Mathf.Max(0.05f, profile.parkedAltitudeAboveGround * 0.5f);
        }

        private float GetMaxAltitudeLimit()
        {
            if (profile == null)
                return 10f;

            return _isOccupied
                ? profile.maxAltitudeAboveGround
                : profile.maxAltitudeAboveGround;
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || profile == null || _rigidbody == null)
                return;

            // Kinematic bodies reject velocity writes and ignore forces (parked / pre-deploy state).
            if (_rigidbody.isKinematic)
                return;

            UpdateTargetAltitude();
            ApplyHoverForces();
            ApplyDriveForces();
            EnforceAltitudeBand();
            ClampPlanarSpeed();
            UpdateTurbulenceMetrics();
        }

        private bool TrySetLinearVelocity(Vector3 velocity)
        {
            if (_rigidbody == null || _rigidbody.isKinematic)
                return false;

            _rigidbody.linearVelocity = velocity;
            return true;
        }

        private bool TrySetAngularVelocity(Vector3 velocity)
        {
            if (_rigidbody == null || _rigidbody.isKinematic)
                return false;

            _rigidbody.angularVelocity = velocity;
            return true;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            ApplyVisualTurbulence();
        }

        private void UpdateTargetAltitude()
        {
            if (!_isOccupied || Mathf.Abs(_verticalInput) < 0.01f)
                return;

            _targetAltitudeAboveGround += _verticalInput * profile.verticalAdjustSpeed * Time.fixedDeltaTime;
            _targetAltitudeAboveGround = Mathf.Clamp(
                _targetAltitudeAboveGround,
                GetMinAltitudeLimit(),
                GetMaxAltitudeLimit());
        }

        private void ApplyHoverForces()
        {
            Transform[] points = hoverRayPoints;
            if (points == null || points.Length == 0)
                points = _selfHoverRayPoints ??= new[] { transform };

            int activeRays = 0;
            Vector3 totalForce = Vector3.zero;

            for (int i = 0; i < points.Length; i++)
            {
                Transform point = points[i];
                if (point == null)
                    continue;

                if (!TryRaycastGround(point.position, profile.rayLength, out RaycastHit hit))
                    continue;

                activeRays++;
                float altitudeError = _targetAltitudeAboveGround - hit.distance;
                Vector3 pointVelocity = _rigidbody.GetPointVelocity(point.position);
                float springForce = altitudeError * profile.springStrength;
                float dampingForce = Vector3.Dot(pointVelocity, hit.normal) * profile.springDamping;
                totalForce += hit.normal * (springForce - dampingForce);
            }

            if (activeRays > 0)
            {
                _rigidbody.AddForce(totalForce / activeRays, ForceMode.Force);
                return;
            }

            if (TrySampleGroundHeight(out float groundY))
            {
                float targetY = groundY + _targetAltitudeAboveGround;
                float liftError = targetY - transform.position.y;
                _rigidbody.AddForce(
                    Vector3.up * (liftError * profile.springStrength * 0.25f - _rigidbody.linearVelocity.y * profile.springDamping),
                    ForceMode.Force);
            }
        }

        private void ApplyDriveForces()
        {
            float forwardInput = _moveInput.y;
            float lateralInput = _moveInput.x;
            bool steeringWithThrottle = _steerWithYaw || Mathf.Abs(forwardInput) >= profile.forwardSteerThreshold;

            if (Mathf.Abs(forwardInput) >= 0.01f)
            {
                float thrustPerMass = forwardInput >= 0f ? profile.forwardThrust : profile.reverseThrust;
                float signedThrust = forwardInput * thrustPerMass;

                if (_boosterActive && forwardInput > 0f)
                    signedThrust *= profile.boosterMultiplier;

                _rigidbody.AddForce(transform.forward * signedThrust, ForceMode.Force);
            }

            if (steeringWithThrottle)
            {
                if (Mathf.Abs(lateralInput) >= 0.01f)
                    _rigidbody.AddTorque(transform.up * (lateralInput * profile.yawTorque), ForceMode.Force);
            }
            else if (Mathf.Abs(lateralInput) >= 0.01f)
            {
                _rigidbody.AddForce(transform.right * (lateralInput * profile.strafeThrust), ForceMode.Force);
                _rigidbody.AddTorque(transform.up * (lateralInput * profile.yawTorque * 0.35f), ForceMode.Force);
            }
        }

        private void EnforceAltitudeBand()
        {
            if (!TrySampleGroundHeight(out float groundY))
                return;

            float minY = groundY + GetMinAltitudeLimit();
            float maxY = groundY + GetMaxAltitudeLimit();
            Vector3 position = transform.position;

            if (position.y >= minY && position.y <= maxY)
                return;

            position.y = Mathf.Clamp(position.y, minY, maxY);
            transform.position = position;

            if (_rigidbody.isKinematic)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            if (position.y <= minY + 0.01f && velocity.y < 0f)
                velocity.y = 0f;
            if (position.y >= maxY - 0.01f && velocity.y > 0f)
                velocity.y = 0f;
            TrySetLinearVelocity(velocity);
        }

        public void SnapToHoverAltitude(bool hardSnap = false)
        {
            if (!Application.isPlaying || profile == null || _rigidbody == null)
                return;

            if (!TrySampleGroundHeight(out float groundY))
                return;

            float targetY = groundY + _targetAltitudeAboveGround;
            float minY = groundY + GetMinAltitudeLimit();
            Vector3 position = transform.position;
            bool needsSnap = hardSnap || position.y < minY - 0.05f;

            if (!needsSnap)
                return;

            position.y = Mathf.Max(targetY, minY);
            transform.position = position;
            TrySetLinearVelocity(Vector3.zero);
            TrySetAngularVelocity(Vector3.zero);
        }

        private bool TryRaycastGround(Vector3 origin, float maxDistance, out RaycastHit bestHit)
        {
            bestHit = default;
            if (profile == null)
                return false;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                _raycastHits,
                maxDistance,
                profile.groundMask,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];
                if (IsOwnCollider(hit.collider))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }

            return found;
        }

        private void ClampPlanarSpeed()
        {
            if (_rigidbody.isKinematic)
                return;

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 planar = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float forwardSpeed = Vector3.Dot(planar, transform.forward);
            float strafeSpeed = Vector3.Dot(planar, transform.right);

            float clampedForward = Mathf.Clamp(forwardSpeed, -profile.maxReverseSpeed, profile.maxForwardSpeed);
            float clampedStrafe = Mathf.Clamp(strafeSpeed, -profile.maxStrafeSpeed, profile.maxStrafeSpeed);
            Vector3 clampedPlanar = transform.forward * clampedForward + transform.right * clampedStrafe;
            TrySetLinearVelocity(clampedPlanar + Vector3.up * velocity.y);
        }

        private void UpdateTurbulenceMetrics()
        {
            Vector3 planar = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, Vector3.up);
            CurrentPlanarSpeed = planar.magnitude;
            CurrentSpeedRatio = profile.maxForwardSpeed > 0.01f
                ? Mathf.Clamp01(CurrentPlanarSpeed / profile.maxForwardSpeed)
                : 0f;

            float multiplier = Mathf.Lerp(1f, profile.turbulenceMaxMultiplier, CurrentSpeedRatio);
            TurbulenceAmplitude = profile.turbulenceBaseAmplitude * multiplier;
        }

        private void ApplyVisualTurbulence()
        {
            if (visualRoot == null || profile == null)
                return;

            UpdateDriveTilt();

            float time = Time.time * profile.turbulenceFrequency;
            float turbPitch = Mathf.Sin(time + _turbulenceSeed) * TurbulenceAmplitude * profile.visualTurbulenceRotation * 60f;
            float turbRoll = Mathf.Cos(time * 1.17f + _turbulenceSeed) * TurbulenceAmplitude * profile.visualTurbulenceRotation * 60f;

            visualRoot.localRotation = Quaternion.Euler(
                _visualBaseLocalEuler.x + _currentDrivePitch + turbPitch,
                _visualBaseLocalEuler.y,
                _visualBaseLocalEuler.z + _currentDriveRoll + turbRoll);
        }

        private void UpdateDriveTilt()
        {
            float forwardInput = _moveInput.y;
            float lateralInput = _moveInput.x;
            float yawRate = Vector3.Dot(_rigidbody.angularVelocity, transform.up);
            float speedFactor = Mathf.Clamp01(_rigidbody.linearVelocity.magnitude / Mathf.Max(profile.maxForwardSpeed, 1f));
            float speedBankBoost = Mathf.Lerp(0.65f, 1f, speedFactor);

            float targetPitch = 0f;
            if (forwardInput > 0.05f)
            {
                _forwardDriveTimer += Time.deltaTime;
                float settle = profile.drivePitchSettleSeconds > 0.01f
                    ? Mathf.Exp(-_forwardDriveTimer / profile.drivePitchSettleSeconds)
                    : 0f;
                targetPitch = -profile.drivePitchMax * forwardInput * settle;
            }
            else if (forwardInput < -0.05f)
            {
                _forwardDriveTimer = 0f;
                targetPitch = profile.drivePitchMax * 0.45f * Mathf.Abs(forwardInput);
            }
            else
            {
                _forwardDriveTimer = 0f;
            }

            // Input-driven roll responds immediately; yaw rate adds extra lean in hard turns.
            float inputRoll = -lateralInput * profile.driveRollMax * speedBankBoost;
            float yawRoll = -yawRate * profile.yawRollFactor * speedBankBoost;
            float targetRoll = Mathf.Clamp(inputRoll + yawRoll, -profile.maxTotalBank, profile.maxTotalBank);

            float smooth = 1f - Mathf.Exp(-profile.driveTiltSmooth * Time.deltaTime);
            _currentDrivePitch = Mathf.Lerp(_currentDrivePitch, targetPitch, smooth);
            _currentDriveRoll = Mathf.Lerp(_currentDriveRoll, targetRoll, smooth);
        }

        public Vector3 SampleTurbulenceOffset()
        {
            if (profile == null || TurbulenceAmplitude <= 0f)
                return Vector3.zero;

            float time = Time.time * profile.turbulenceFrequency;
            return new Vector3(
                Mathf.Sin(time * 1.31f + _turbulenceSeed) * TurbulenceAmplitude,
                Mathf.Cos(time + _turbulenceSeed) * TurbulenceAmplitude,
                Mathf.Sin(time * 0.87f + _turbulenceSeed) * TurbulenceAmplitude * 0.35f);
        }

        public bool TrySampleGroundHeight(out float groundY)
        {
            groundY = 0f;
            Vector3 origin = transform.position + Vector3.up * (profile != null ? profile.maxAltitudeAboveGround + 4f : 12f);
            float maxDistance = profile != null ? profile.rayLength + profile.maxAltitudeAboveGround + 8f : 32f;

            if (TryRaycastGround(origin, maxDistance, out RaycastHit hit))
            {
                groundY = hit.point.y;
                return true;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return false;

            groundY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
            return true;
        }
    }
}
