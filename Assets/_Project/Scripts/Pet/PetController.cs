using UnityEngine;
using Project.Interaction;
using Project.Inventory;
using Project.UI;

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
            Idle
        }

        [Header("Profile")]
        [SerializeField] private string displayName = "Fox Cub";
        [SerializeField] private string description = "A loyal companion that gathers nearby items.";

        [Header("Owner")]
        [SerializeField] private Transform owner;
        [SerializeField] private Vector3 followOffset = new Vector3(-1.2f, 0f, -1.5f);

        [Header("Behavior")]
        [SerializeField] private bool companionActive = true;
        [SerializeField] private bool followEnabled = true;
        [SerializeField] private bool wanderEnabled = true;
        [SerializeField] private bool fetchEnabled = true;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.2f;
        [SerializeField] private float runSpeed = 4.5f;
        [SerializeField] private float turnSpeed = 8f;
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

        [Header("Fetch")]
        [SerializeField] private float fetchSearchRadius = 18f;
        [SerializeField] private float fetchPickupDistance = 1.25f;
        [SerializeField] private float fetchCheckInterval = 8f;
        [SerializeField] private float fetchAttemptChance = 0.45f;
        [SerializeField] private float fetchCooldown = 15f;

        private PetState _state = PetState.Following;
        private InventorySystem _ownerInventory;
        private UIManager _uiManager;
        private ItemPickup _fetchTarget;
        private PetAnimationController _animationController;

        private Vector3 _wanderTarget;
        private float _wanderTimer;
        private float _idleTimer;
        private float _nextWanderRollTime;
        private float _nextFetchCheckTime;
        private float _fetchCooldownUntil;
        private float _currentSpeed;

        public float CurrentSpeed => _currentSpeed;
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
                    _ => "Following"
                };
            }
        }

        private void Awake()
        {
            if (owner == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                    owner = player.transform;
            }

            _animationController = GetComponent<PetAnimationController>();
        }

        private void OnEnable()
        {
            PetManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            PetManager.Instance?.Unregister(this);
        }

        private void Start()
        {
            if (owner != null)
                _ownerInventory = owner.GetComponent<InventorySystem>();

            _uiManager = FindAnyObjectByType<UIManager>();
            _nextWanderRollTime = Time.time + Random.Range(idleBeforeWanderMin, idleBeforeWanderMax);
            _nextFetchCheckTime = Time.time + fetchCheckInterval * 0.5f;
            ApplyCompanionVisibility();
            SnapToGround();
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

            if (owner == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    owner = player.transform;
                    _ownerInventory = player.GetComponent<InventorySystem>();
                }
                else
                {
                    _currentSpeed = 0f;
                    return;
                }
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

            transform.position = GetFollowPosition();
            SnapToGround();
            SetState(followEnabled ? PetState.Following : PetState.Idle);
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
            if (!wanderEnabled)
            {
                SetState(PetState.Following);
                return;
            }

            _wanderTimer -= Time.deltaTime;
            MoveTowards(_wanderTarget, walkSpeed * 0.75f);

            if (HorizontalDistance(transform.position, _wanderTarget) <= stopDistance + 0.2f || _wanderTimer <= 0f)
                SetState(followEnabled ? PetState.Following : PetState.Idle);
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

            _wanderTarget = owner.position + Random.insideUnitSphere * wanderRadius;
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
                Vector3 step = toTarget.normalized * (speed * Time.deltaTime);
                if (step.sqrMagnitude > distance * distance)
                    step = toTarget;

                transform.position += step;
                _currentSpeed = speed;
            }
            else
            {
                _currentSpeed = 0f;
            }

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
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

            Vector3 origin = new Vector3(worldPosition.x, worldPosition.y + groundProbeHeight, worldPosition.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundProbeDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                groundY = hit.point.y + groundOffset;
                return true;
            }

            return false;
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

            if (_animationController != null)
                _animationController.enabled = companionActive;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                renderer.enabled = companionActive;
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
