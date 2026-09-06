using UnityEngine;
using MalbersAnimations.PathCreation;
using Project.AI;
using Project.Companions;
using Project.Core;
using Project.Interaction;
using Project.Inventory;
using Project.UI;
using Project.World;

namespace Project.Pet
{
    /// <summary>
    /// Companion pet that follows the player, wanders nearby, and occasionally fetches items.
    /// </summary>
    public class PetController : MonoBehaviour
    {
        private enum PetState
        {
            Following,
            Wandering,
            Fetching,
            Idle,
            PathFollowing
        }

        private const int GroundHitBufferSize = 16;
        private static readonly RaycastHit[] GroundHitBuffer = new RaycastHit[GroundHitBufferSize];

        [Header("Profile")]
        [SerializeField] private PetDefinition definition;
        [SerializeField] private string petId = "fox_cub";
        [SerializeField] private string displayName = "Fox Cub";
        [SerializeField] private string description = "A loyal companion that gathers nearby items.";
        [SerializeField] private Sprite inventoryIcon;

        [Header("Owner")]
        [SerializeField] private Transform owner;
        [SerializeField] private Vector3 followOffset = new Vector3(-1.2f, 0f, -1.5f);

        [Header("Behavior")]
        [SerializeField] private bool isOwned;
        [SerializeField] private bool companionActive = true;
        [SerializeField] private bool followEnabled = true;
        [SerializeField] private bool wanderEnabled = true;
        [SerializeField] private bool fetchEnabled = true;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.2f;
        [SerializeField] private float runSpeed = 4.5f;
        [Tooltip("Legacy turn factor (≈ deg/sec × 18). Lower = slower, more natural yaw.")]
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float stopDistance = 0.35f;
        [SerializeField] private float maxFollowDistance = 12f;
        [SerializeField] private float groundOffset = 0f;
        [SerializeField] private float groundProbeHeight = 40f;
        [SerializeField] private float groundProbeDistance = 80f;

        [Header("Wander")]
        [SerializeField] private float wanderRadius = 4f;
        [SerializeField] private float wanderDuration = 3f;
        [SerializeField] private float idleBeforeWanderMin = 4f;
        [SerializeField] private float idleBeforeWanderMax = 10f;
        [SerializeField] private float wanderChance = 0.35f;

        [Header("Wild Wander")]
        [SerializeField] private float wildHomeRadius = 3.5f;
        [SerializeField] private float wildWanderDurationMin = 1.4f;
        [SerializeField] private float wildWanderDurationMax = 3.2f;
        [SerializeField] private float wildPauseMin = 2.2f;
        [SerializeField] private float wildPauseMax = 5.5f;

        [Header("Fetch")]
        [SerializeField] private float fetchSearchRadius = 18f;
        [SerializeField] private float fetchPickupDistance = 1.25f;
        [SerializeField] private float fetchCheckInterval = 8f;
        [SerializeField] private float fetchAttemptChance = 0.45f;
        [SerializeField] private float fetchCooldown = 15f;

        [Header("Path Follow / Patrol")]
        [Tooltip("When enabled, pet patrols the assigned Path Creator until CallToOwner / combat clears it.")]
        [SerializeField] private bool pathFollowEnabled;
        [Tooltip("Path Creator (or Path Creator Variant) to follow when Path Follow is enabled.")]
        [SerializeField] private PathCreator patrolPath;
        [Tooltip("Optional explicit provider. If empty, resolved from Patrol Path.")]
        [SerializeField] private DMIPathFollowProvider patrolPathProvider;
        [Tooltip("Loop = next anchor in order. PingPong = random next anchor (pet path-follow).")]
        [SerializeField] private DMIPathPatrolMode pathPatrolMode = DMIPathPatrolMode.Loop;
        [Tooltip("Seconds to idle at each anchor before moving to the next.")]
        [SerializeField] private float pathPatrolWaitDuration = 2f;

        private PetState _state = PetState.Following;
        private InventorySystem _ownerInventory;
        private UIManager _uiManager;
        private ItemPickup _fetchTarget;
        private PetAnimationController _animationController;

        private Vector3 _wanderTarget;
        private Vector3 _wildHomePosition;
        private float _wanderTimer;
        private float _idleTimer;
        private float _nextWanderRollTime;
        private float _nextFetchCheckTime;
        private float _fetchCooldownUntil;
        private float _currentSpeed;

        private Vector3[] _pathPoints;
        private int _pathIndex;
        private DMIPathPatrolMode _pathPatrolMode = DMIPathPatrolMode.Loop;
        private float _pathArrivalDistance = 0.75f;
        private float _pathWaitDuration = 2f;
        private float _pathWaitTimer;
        private bool _resumeOwnerFollowAfterPath;

        public float CurrentSpeed => _currentSpeed;
        public string PetId => string.IsNullOrWhiteSpace(petId) ? name : petId;
        public PetDefinition Definition => definition;
        public Sprite InventoryIcon => inventoryIcon;
        public bool IsOwned => isOwned;
        public Transform Owner => owner;
        public string DefaultDisplayName => displayName;
        public string Description => description;

        public string DisplayName
        {
            get => displayName;
            set
            {
                displayName = string.IsNullOrWhiteSpace(value) ? "Pet" : value.Trim();
                PetManager.Instance?.NotifyPetChanged();
            }
        }

        public bool CompanionActive
        {
            get => companionActive;
            set
            {
                companionActive = value;
                ApplyCompanionVisibility();
                if (!companionActive)
                    ResetMotion();

                PetManager.Instance?.NotifyPetChanged();
            }
        }

        public bool FollowEnabled
        {
            get => followEnabled;
            set
            {
                followEnabled = value;
                if (!followEnabled && _state == PetState.Following)
                    SetState(PetState.Idle);

                PetManager.Instance?.NotifyPetChanged();
            }
        }

        public bool WanderEnabled
        {
            get => wanderEnabled;
            set
            {
                wanderEnabled = value;
                if (!wanderEnabled && _state == PetState.Wandering)
                    SetState(PetState.Following);

                PetManager.Instance?.NotifyPetChanged();
            }
        }

        public bool FetchEnabled
        {
            get => fetchEnabled;
            set
            {
                fetchEnabled = value;
                if (!fetchEnabled && _state == PetState.Fetching)
                    SetState(PetState.Following);

                PetManager.Instance?.NotifyPetChanged();
            }
        }

        public string CurrentBehaviorLabel
        {
            get
            {
                if (!companionActive) return "Dismissed";
                return _state switch
                {
                    PetState.Following => followEnabled ? "Following" : "Idle",
                    PetState.Wandering => "Wandering",
                    PetState.Fetching => "Fetching",
                    PetState.Idle => "Idle",
                    PetState.PathFollowing => "Path",
                    _ => "Following"
                };
            }
        }

        public bool IsPathFollowing => _state == PetState.PathFollowing;

        /// <summary>
        /// Optional patrol along world anchor points (e.g. from <c>DMIPathFollowProvider</c>).
        /// Does not break combat/owner systems — call <see cref="ClearPathFollow"/> or CallToOwner to resume.
        /// Loop = ordered anchors. PingPong = random next anchor. Wait uses <paramref name="waitDuration"/> when &gt;= 0, else pet field.
        /// </summary>
        public void AssignPathFollow(
            Vector3[] worldPoints,
            DMIPathPatrolMode mode,
            float arrivalDistance = 0.75f,
            float waitDuration = -1f)
        {
            if (worldPoints == null || worldPoints.Length < 2)
                return;

            _pathPoints = worldPoints;
            _pathPatrolMode = mode;
            pathPatrolMode = mode;
            _pathArrivalDistance = Mathf.Max(0.05f, arrivalDistance);
            _pathWaitDuration = waitDuration >= 0f
                ? Mathf.Max(0f, waitDuration)
                : Mathf.Max(0f, pathPatrolWaitDuration);
            pathPatrolWaitDuration = _pathWaitDuration;
            _pathIndex = 0;
            _pathWaitTimer = 0f;
            _resumeOwnerFollowAfterPath = isOwned && followEnabled;
            _fetchTarget = null;
            SetState(PetState.PathFollowing);
        }

        /// <summary>Legacy bool overload: pingPong true → <see cref="DMIPathPatrolMode.PingPong"/> (random next for pets).</summary>
        public void AssignPathFollow(Vector3[] worldPoints, bool pingPong, float arrivalDistance = 0.75f)
        {
            AssignPathFollow(
                worldPoints,
                pingPong ? DMIPathPatrolMode.PingPong : DMIPathPatrolMode.Loop,
                arrivalDistance);
        }

        /// <summary>Assign Path Creator for path-follow / patrol and register with bezier anchors.</summary>
        public void SetPatrolPath(PathCreator path, DMIPathFollowProvider provider = null, bool enable = true)
        {
            patrolPath = path;
            patrolPathProvider = provider;
            pathFollowEnabled = enable;
            TryBindAssignedPatrolPath();
        }

        public void ConfigurePathPatrol(DMIPathPatrolMode mode, float waitDuration)
        {
            pathPatrolMode = mode;
            pathPatrolWaitDuration = Mathf.Max(0f, waitDuration);
            if (_state == PetState.PathFollowing)
            {
                _pathPatrolMode = pathPatrolMode;
                _pathWaitDuration = pathPatrolWaitDuration;
            }
        }

        public DMIPathPatrolMode PathPatrolMode => pathPatrolMode;
        public float PathPatrolWaitDuration => pathPatrolWaitDuration;

        public bool PathFollowEnabled
        {
            get => pathFollowEnabled;
            set
            {
                pathFollowEnabled = value;
                if (pathFollowEnabled)
                    TryBindAssignedPatrolPath();
                else
                    ClearPathFollow();
            }
        }

        public PathCreator PatrolPath => patrolPath;
        public DMIPathFollowProvider PatrolPathProvider => patrolPathProvider;

        public void ClearPathFollow()
        {
            // Unregister only — do not call UnassignPet (avoids recursion).
            if (patrolPathProvider != null)
                patrolPathProvider.UnregisterPet(this);

            _pathPoints = null;
            _pathIndex = 0;
            _pathWaitTimer = 0f;

            if (!companionActive)
            {
                SetState(PetState.Idle);
                return;
            }

            if (isOwned && (_resumeOwnerFollowAfterPath || followEnabled) && owner != null)
                SetState(PetState.Following);
            else
                SetState(PetState.Idle);

            _resumeOwnerFollowAfterPath = false;
        }

        private void TryBindAssignedPatrolPath()
        {
            if (!pathFollowEnabled)
                return;

            DMIPathFollowProvider provider = patrolPathProvider;
            if (provider == null)
                provider = DMIPathFollowBinding.Resolve((Object)patrolPath ?? patrolPathProvider);

            if (provider == null)
                return;

            patrolPathProvider = provider;
            if (patrolPath == null)
                patrolPath = provider.PathCreator;

            provider.TryAssignPet(this);
        }

        private void Awake()
        {
            ApplyDefinition();
            EnsureAdoptableComponent();

            if (owner == null && isOwned)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                    owner = player.transform;
            }

            _animationController = GetComponent<PetAnimationController>();
            ConfigureNonBlockingColliders();
        }

        private void ConfigureNonBlockingColliders()
        {
            FollowerCollisionUtility.RegisterHierarchyColliders(gameObject);

            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] != null)
                    Destroy(bodies[i]);
            }
        }

        private void EnsureAdoptableComponent()
        {
            if (isOwned || GetComponent<PetWorldAdoptable>() != null)
                return;

            gameObject.AddComponent<PetWorldAdoptable>();
        }

        public void ApplyDefinition(PetDefinition source = null)
        {
            if (source != null)
                definition = source;

            if (definition == null && !string.IsNullOrWhiteSpace(petId))
                definition = PetCatalog.Resolve(petId);

            if (definition == null)
                return;

            if (!string.IsNullOrWhiteSpace(definition.petId))
                petId = definition.petId;
            if (!string.IsNullOrWhiteSpace(definition.displayName))
                displayName = definition.displayName;
            if (!string.IsNullOrWhiteSpace(definition.description))
                description = definition.description;
            if (definition.inventoryIcon != null)
                inventoryIcon = definition.inventoryIcon;
        }

        public void BindOwner(Transform playerTransform)
        {
            owner = playerTransform;
            _ownerInventory = playerTransform != null
                ? playerTransform.GetComponent<InventorySystem>()
                : null;
        }

        public void SummonToOwner()
        {
            if (owner == null)
                return;

            transform.position = GetFollowPosition();
            SnapToGround();
            _state = followEnabled ? PetState.Following : PetState.Idle;
            _currentSpeed = 0f;
            _fetchTarget = null;
        }

        public void SetOwned(bool owned)
        {
            isOwned = owned;
            if (owned)
            {
                companionActive = false;
            }
            else
            {
                companionActive = true;
                followEnabled = false;
            }

            ApplyCompanionVisibility();
            PetManager.Instance?.NotifyPetChanged();
        }

        public void RefreshCompanionVisibility()
        {
            ApplyCompanionVisibility();
        }

        private void OnEnable()
        {
            PetManager.Instance?.Register(this);
            ApplyCompanionVisibility();
        }

        private void OnDestroy()
        {
            FollowerCollisionUtility.UnregisterHierarchyColliders(gameObject);
            PetManager.Instance?.Unregister(this);
        }

        private void Start()
        {
            if (owner != null)
                _ownerInventory = owner.GetComponent<InventorySystem>();

            _uiManager = FindAnyObjectByType<UIManager>();
            _nextWanderRollTime = Time.time + Random.Range(idleBeforeWanderMin, idleBeforeWanderMax);
            _nextFetchCheckTime = Time.time + fetchCheckInterval * 0.5f;

            if (!isOwned)
            {
                _wildHomePosition = transform.position;
                owner = null;
                followEnabled = false;
                fetchEnabled = false;
                companionActive = true;
                _state = PetState.Idle;
                _nextWanderRollTime = Time.time + Random.Range(wildPauseMin, wildPauseMax);
            }

            ApplyCompanionVisibility();
            SnapToGround();
            TryBindAssignedPatrolPath();
        }

        private void LateUpdate()
        {
            if (!companionActive)
                return;

            SnapToGround();
        }

        private void Update()
        {
            if (!companionActive)
            {
                _currentSpeed = 0f;
                return;
            }

            if (_state == PetState.PathFollowing)
            {
                UpdatePathFollowing();
                return;
            }

            if (!isOwned)
            {
                UpdateWildBehavior();
                return;
            }

            if (owner == null)
            {
                Transform player = PlayerReference.ResolveTransform();
                if (player == null)
                {
                    _currentSpeed = 0f;
                    return;
                }

                owner = player;
                _ownerInventory = player.GetComponent<InventorySystem>();
            }

            switch (_state)
            {
                case PetState.Following:
                    UpdateFollowing();
                    break;
                case PetState.Wandering:
                    UpdateWandering();
                    break;
                case PetState.Fetching:
                    UpdateFetching();
                    break;
                case PetState.Idle:
                    _currentSpeed = 0f;
                    break;
            }
        }

        public void CallToOwner()
        {
            if (!companionActive || owner == null)
                return;

            ClearPathFollow();
            SummonToOwner();
            SetState(followEnabled ? PetState.Following : PetState.Idle);
        }

        private void UpdatePathFollowing()
        {
            if (_pathPoints == null || _pathPoints.Length < 2)
            {
                ClearPathFollow();
                return;
            }

            if (_pathWaitTimer > 0f)
            {
                _pathWaitTimer -= Time.deltaTime;
                _currentSpeed = 0f;
                if (_pathWaitTimer > 0f)
                    return;

                AdvancePathFollowIndex();
                return;
            }

            Vector3 target = _pathPoints[Mathf.Clamp(_pathIndex, 0, _pathPoints.Length - 1)];
            MoveTowards(target, walkSpeed);

            if (HorizontalDistance(transform.position, target) > _pathArrivalDistance)
                return;

            // Idle at this anchor, then pick next (Loop ordered / PingPong random).
            if (_pathWaitDuration > 0f)
            {
                _pathWaitTimer = _pathWaitDuration;
                _currentSpeed = 0f;
                return;
            }

            AdvancePathFollowIndex();
        }

        private void AdvancePathFollowIndex()
        {
            if (_pathPoints == null || _pathPoints.Length < 2)
                return;

            if (_pathPatrolMode == DMIPathPatrolMode.PingPong)
            {
                // Pet PingPong = random next anchor (not reverse ping-pong used by enemies/creatures).
                int count = _pathPoints.Length;
                if (count <= 1)
                    return;

                int next = Random.Range(0, count - 1);
                if (next >= _pathIndex)
                    next++;
                _pathIndex = next;
            }
            else
            {
                _pathIndex = (_pathIndex + 1) % _pathPoints.Length;
            }
        }

        private void UpdateFollowing()
        {
            if (!followEnabled)
            {
                SetState(PetState.Idle);
                return;
            }

            if (fetchEnabled)
                TryStartFetch();

            if (wanderEnabled)
                TryStartWander();

            Vector3 target = GetFollowPosition();
            float distanceToOwner = HorizontalDistance(transform.position, owner.position);
            float speed = distanceToOwner > maxFollowDistance * 0.6f ? runSpeed : walkSpeed;
            MoveTowards(target, speed);
        }

        private void UpdateWandering()
        {
            if (!wanderEnabled && isOwned)
            {
                SetState(PetState.Following);
                return;
            }

            _wanderTimer -= Time.deltaTime;
            MoveTowards(_wanderTarget, walkSpeed * 0.75f);

            if (HorizontalDistance(transform.position, _wanderTarget) <= stopDistance + 0.2f || _wanderTimer <= 0f)
            {
                if (!isOwned)
                {
                    _currentSpeed = 0f;
                    _state = PetState.Idle;
                    _nextWanderRollTime = Time.time + Random.Range(wildPauseMin, wildPauseMax);
                    return;
                }

                SetState(followEnabled ? PetState.Following : PetState.Idle);
            }
        }

        private void UpdateWildBehavior()
        {
            if (Time.time < _nextWanderRollTime)
            {
                _currentSpeed = 0f;
                return;
            }

            if (_state == PetState.Wandering)
            {
                UpdateWandering();
                return;
            }

            _wanderTarget = _wildHomePosition + Random.insideUnitSphere * wildHomeRadius;
            _wanderTarget.y = _wildHomePosition.y;
            if (TrySampleGround(_wanderTarget, out float wanderGroundY))
                _wanderTarget.y = wanderGroundY;

            _wanderTimer = Random.Range(wildWanderDurationMin, wildWanderDurationMax);
            _state = PetState.Wandering;
        }

        private void UpdateFetching()
        {
            if (!fetchEnabled)
            {
                SetState(followEnabled ? PetState.Following : PetState.Idle);
                return;
            }

            if (_fetchTarget == null || _fetchTarget.IsPickedUp)
            {
                SetState(followEnabled ? PetState.Following : PetState.Idle);
                return;
            }

            Vector3 target = _fetchTarget.transform.position;
            float distance = HorizontalDistance(transform.position, target);
            MoveTowards(target, runSpeed);

            if (distance <= fetchPickupDistance)
                CompleteFetch();
        }

        private void TryStartWander()
        {
            if (!wanderEnabled || !followEnabled)
                return;

            if (Time.time < _nextWanderRollTime)
                return;

            _idleTimer += Time.deltaTime;
            if (_idleTimer < idleBeforeWanderMin)
                return;

            if (owner.GetComponent<ECM2.Character>() is { } character && character.GetSpeed() > 0.5f)
            {
                _idleTimer = 0f;
                return;
            }

            if (Random.value > wanderChance)
                return;

            float companionWanderRadius = Mathf.Min(wanderRadius, 4f);
            _wanderTarget = owner.position + Random.insideUnitSphere * companionWanderRadius;
            _wanderTarget.y = owner.position.y;
            if (TrySampleGround(_wanderTarget, out float wanderGroundY))
                _wanderTarget.y = wanderGroundY;
            _wanderTimer = wanderDuration;
            _idleTimer = 0f;
            _nextWanderRollTime = Time.time + Random.Range(idleBeforeWanderMin, idleBeforeWanderMax);
            SetState(PetState.Wandering);
        }

        private void TryStartFetch()
        {
            if (!fetchEnabled || !followEnabled)
                return;

            if (Time.time < _nextFetchCheckTime || Time.time < _fetchCooldownUntil)
                return;

            _nextFetchCheckTime = Time.time + fetchCheckInterval;

            if (Random.value > fetchAttemptChance || _ownerInventory == null)
                return;

            ItemPickup pickup = FindNearestPickup();
            if (pickup == null)
                return;

            _fetchTarget = pickup;
            SetState(PetState.Fetching);
        }

        private void CompleteFetch()
        {
            if (_fetchTarget != null && _ownerInventory != null)
            {
                string itemName = _fetchTarget.itemData != null ? _fetchTarget.itemData.itemName : "item";
                if (_fetchTarget.TryCollectFor(_ownerInventory, showPlayerPrompt: false))
                {
                    if (_uiManager != null)
                        _uiManager.ShowPetFetchMessage(itemName);

                    _fetchCooldownUntil = Time.time + fetchCooldown;
                }
            }

            _fetchTarget = null;
            SetState(followEnabled ? PetState.Following : PetState.Idle);
        }

        private ItemPickup FindNearestPickup()
        {
            ItemPickup[] pickups = FindObjectsByType<ItemPickup>();
            ItemPickup nearest = null;
            float nearestDistance = fetchSearchRadius;

            foreach (ItemPickup pickup in pickups)
            {
                if (pickup == null || pickup.IsPickedUp || pickup.itemData == null)
                    continue;

                float distance = HorizontalDistance(transform.position, pickup.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = pickup;
                }
            }

            return nearest;
        }

        private Vector3 GetFollowPosition()
        {
            Vector3 offset = owner.TransformDirection(followOffset);
            Vector3 target = owner.position + offset;

            if (TrySampleGround(target, out float groundY))
                target.y = groundY;
            else
                target.y = transform.position.y;

            return target;
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            Vector3 flatTarget = target;
            if (TrySampleGround(flatTarget, out float groundY))
                flatTarget.y = groundY;
            else
                flatTarget.y = transform.position.y;

            Vector3 toTarget = flatTarget - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance > stopDistance)
            {
                float moveScale = DMILocomotionFacing.FacingMoveScale(transform, toTarget);
                float scaledSpeed = speed * moveScale;
                Vector3 step = toTarget.normalized * (scaledSpeed * Time.deltaTime);
                if (step.sqrMagnitude > distance * distance)
                    step = toTarget;

                transform.position += step;
                _currentSpeed = scaledSpeed;
            }
            else
            {
                _currentSpeed = 0f;
            }

            if (toTarget.sqrMagnitude > 0.01f)
                DMILocomotionFacing.FaceToward(transform, flatTarget, turnSpeed);
        }

        private void SnapToGround()
        {
            if (TrySampleGround(transform.position, out float groundY))
            {
                Vector3 pos = transform.position;
                pos.y = groundY;
                transform.position = pos;
            }
        }

        private bool TrySampleGround(Vector3 worldPosition, out float groundY)
        {
            groundY = worldPosition.y;

            if (TryRaycastGround(worldPosition, out float rayGroundY))
            {
                groundY = rayGroundY;
                return true;
            }

            if (TrySampleTerrain(worldPosition, out float terrainGroundY))
            {
                groundY = terrainGroundY;
                return true;
            }

            return false;
        }

        private bool TrySampleTerrain(Vector3 worldPosition, out float groundY)
        {
            groundY = worldPosition.y;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return false;

            groundY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y + groundOffset;
            return true;
        }

        private bool TryRaycastGround(Vector3 worldPosition, out float groundY)
        {
            groundY = worldPosition.y;

            float originY = worldPosition.y + groundProbeHeight;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                originY = Mathf.Max(originY, terrainY + groundProbeHeight);
            }

            Vector3 origin = new Vector3(worldPosition.x, originY, worldPosition.z);
            float rayLength = (originY - worldPosition.y) + groundProbeDistance;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                GroundHitBuffer,
                rayLength,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            float bestY = 0f;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = GroundHitBuffer[i];
                if (hit.collider == null)
                    continue;

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestY = hit.point.y;
            }

            if (bestDistance == float.MaxValue)
                return false;

            groundY = bestY + groundOffset;
            return true;
        }

        private void SetState(PetState newState)
        {
            _state = newState;

            if (newState == PetState.Following || newState == PetState.Idle)
                _fetchTarget = null;
        }

        private void ApplyCompanionVisibility()
        {
            if (_animationController == null)
                _animationController = GetComponent<PetAnimationController>();

            bool visible = companionActive && gameObject.activeSelf;
            if (_animationController != null)
                _animationController.enabled = visible;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }

        private void ResetMotion()
        {
            _currentSpeed = 0f;
            _fetchTarget = null;
            _state = PetState.Idle;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDrawGizmosSelected()
        {
            if (owner == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(owner.position, wanderRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, fetchSearchRadius);
        }
    }
}
