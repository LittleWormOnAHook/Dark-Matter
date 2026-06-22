using UnityEngine;
using UnityEngine.Serialization;

namespace Project.AI
{
    [RequireComponent(typeof(EnemySenses))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyCombat))]
    public class EnemyAiController : MonoBehaviour
    {
        private enum AiState
        {
            Idle,
            Wander,
            Patrol,
            Investigate,
            ReturnHome,
            Chase,
            Attack,
            Search
        }

        [Header("Movement Mode")]
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Wander;
        [SerializeField] private EnemyPatrolMode patrolMode = EnemyPatrolMode.Loop;
        [SerializeField] private bool investigateNoise = true;
        [SerializeField] private bool chasePlayer = true;
        [SerializeField] private bool returnToHomeAfterSearch = true;
        [FormerlySerializedAs("homeLeashRadius")]
        [Tooltip("Max horizontal distance from spawn/home to pursue the player. 0 = unlimited.")]
        [SerializeField] private float chaseRadius = 0f;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float runSpeed = 4.8f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float stopDistance = 0.35f;
        [SerializeField] private float groundOffset = 0f;
        [SerializeField] private float groundProbeHeight = 40f;
        [SerializeField] private float groundProbeDistance = 80f;

        [Header("Wander")]
        [SerializeField] private float wanderRadius = 8f;
        [SerializeField] private float wanderPauseMin = 2f;
        [SerializeField] private float wanderPauseMax = 5f;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitDuration = 2f;
        [SerializeField] private float idleDuration = 3f;

        [Header("Behavior")]
        [SerializeField] private float loseTargetDelay = 4f;
        [SerializeField] private float investigateArriveDistance = 1.2f;
        [SerializeField] private float searchDuration = 6f;
        [SerializeField] private float searchRadius = 4f;

        private EnemySenses senses;
        private EnemyHealth health;
        private EnemyCombat combat;

        private AiState state = AiState.Idle;
        private Vector3 homePosition;
        private Vector3 moveTarget;
        private Vector3 lastKnownPlayerPosition;
        private float stateTimer;
        private float lostTargetTimer;
        private int patrolIndex;
        private int patrolDirection = 1;
        private bool hasPatrolRoute;
        private float currentLocomotionSpeed;

        public float CurrentLocomotionSpeed => currentLocomotionSpeed;

        public bool IsEngagedWithTarget =>
            state == AiState.Attack || state == AiState.Chase;

        private bool IsStationary => movementMode == EnemyMovementMode.Stationary;

        private bool AllowsTranslation =>
            !IsStationary || state == AiState.Chase;

        private void Awake()
        {
            senses = GetComponent<EnemySenses>();
            health = GetComponent<EnemyHealth>();
            combat = GetComponent<EnemyCombat>();
            hasPatrolRoute = patrolPoints != null && patrolPoints.Length > 0;
        }

        private void OnEnable()
        {
            homePosition = transform.position;
            currentLocomotionSpeed = 0f;

            if (health != null)
                health.Died += HandleDeath;

            EnterCalmState();
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= HandleDeath;
        }

        private void LateUpdate()
        {
            if (IsStationary)
                return;

            SnapToGround();
        }

        private void Update()
        {
            currentLocomotionSpeed = 0f;

            if (health != null && health.IsDead)
                return;

            Transform sensedTarget = senses.GetSensedTarget();
            if (sensedTarget != null)
            {
                lastKnownPlayerPosition = sensedTarget.position;
                lostTargetTimer = 0f;
                combat.SetTarget(sensedTarget);

                if (combat.IsTargetInRange())
                {
                    if (state != AiState.Attack)
                        EnterState(AiState.Attack);
                }
                else if (chasePlayer && CanChaseTarget(sensedTarget.position))
                {
                    if (state != AiState.Chase)
                        EnterState(AiState.Chase);
                }
                else if (state == AiState.Chase || state == AiState.Attack)
                {
                    GiveUpChaseAndReturnHome();
                }
            }
            else
            {
                combat.SetTarget(null);

                if (state == AiState.Chase || state == AiState.Attack)
                {
                    lostTargetTimer += Time.deltaTime;
                    if (lostTargetTimer >= loseTargetDelay)
                        GiveUpChaseAndReturnHome();
                }
            }

            switch (state)
            {
                case AiState.Idle:
                    UpdateIdle();
                    break;
                case AiState.Wander:
                    UpdateWander();
                    break;
                case AiState.Patrol:
                    UpdatePatrol();
                    break;
                case AiState.Investigate:
                    UpdateInvestigate();
                    break;
                case AiState.ReturnHome:
                    UpdateReturnHome();
                    break;
                case AiState.Chase:
                    UpdateChase();
                    break;
                case AiState.Attack:
                    UpdateAttack();
                    break;
                case AiState.Search:
                    UpdateSearch();
                    break;
            }
        }

        private void UpdateIdle()
        {
            if (ShouldInvestigateNoise())
            {
                EnterState(AiState.Investigate);
                return;
            }

            if (movementMode == EnemyMovementMode.Stationary)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            EnterCalmState();
        }

        private void UpdateWander()
        {
            if (ShouldInvestigateNoise())
            {
                EnterState(AiState.Investigate);
                return;
            }

            MoveTowards(moveTarget, walkSpeed);

            if (HorizontalDistance(transform.position, moveTarget) > stopDistance + 0.5f)
                return;

            stateTimer -= Time.deltaTime;
            if (stateTimer > 0f)
                return;

            moveTarget = PickRandomGroundPoint(homePosition, wanderRadius);
            stateTimer = Random.Range(wanderPauseMin, wanderPauseMax);
        }

        private void UpdatePatrol()
        {
            if (ShouldInvestigateNoise())
            {
                EnterState(AiState.Investigate);
                return;
            }

            if (!hasPatrolRoute)
            {
                EnterCalmState();
                return;
            }

            Transform point = patrolPoints[patrolIndex];
            if (point == null)
            {
                AdvancePatrolIndex();
                return;
            }

            moveTarget = point.position;
            MoveTowards(moveTarget, walkSpeed);

            if (HorizontalDistance(transform.position, moveTarget) <= stopDistance + 0.5f)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    AdvancePatrolIndex();
                    stateTimer = patrolWaitDuration;
                }
            }
        }

        private void UpdateInvestigate()
        {
            if (senses.TryGetHeardNoise(out Vector3 noisePosition))
                moveTarget = noisePosition;

            MoveTowards(moveTarget, walkSpeed);

            if (HorizontalDistance(transform.position, moveTarget) <= investigateArriveDistance)
                EnterState(AiState.Search);
        }

        private void UpdateReturnHome()
        {
            moveTarget = homePosition;
            MoveTowards(moveTarget, walkSpeed);

            if (HorizontalDistance(transform.position, homePosition) <= stopDistance + 0.5f)
                EnterCalmState();
        }

        private void UpdateChase()
        {
            if (!CanContinueChase(lastKnownPlayerPosition))
            {
                GiveUpChaseAndReturnHome();
                return;
            }

            moveTarget = lastKnownPlayerPosition;
            MoveTowards(moveTarget, runSpeed);
        }

        private void UpdateAttack()
        {
            Transform target = senses.GetSensedTarget();
            if (target == null)
                return;

            if (!combat.IsTargetInRange() && chasePlayer && CanChaseTarget(target.position))
            {
                EnterState(AiState.Chase);
                return;
            }

            FaceTowards(target.position);
            combat.TryAttack();
        }

        private void GiveUpChaseAndReturnHome()
        {
            lostTargetTimer = 0f;
            combat.SetTarget(null);

            if (IsStationary || !returnToHomeAfterSearch)
            {
                EnterCalmState();
                return;
            }

            EnterState(AiState.ReturnHome);
        }

        private bool CanChaseTarget(Vector3 targetPosition)
        {
            if (chaseRadius <= 0f)
                return true;

            return HorizontalDistance(homePosition, targetPosition) <= chaseRadius;
        }

        private bool CanContinueChase(Vector3 targetPosition)
        {
            if (chaseRadius <= 0f)
                return true;

            return HorizontalDistance(homePosition, transform.position) <= chaseRadius &&
                   HorizontalDistance(homePosition, targetPosition) <= chaseRadius;
        }

        private void UpdateSearch()
        {
            stateTimer -= Time.deltaTime;
            MoveTowards(moveTarget, walkSpeed * 0.85f);

            if (HorizontalDistance(transform.position, moveTarget) <= stopDistance + 0.4f)
                moveTarget = lastKnownPlayerPosition + Random.insideUnitSphere * searchRadius;

            if (stateTimer <= 0f)
            {
                if (returnToHomeAfterSearch)
                    EnterState(AiState.ReturnHome);
                else
                    EnterCalmState();
            }
        }

        private void EnterCalmState()
        {
            switch (movementMode)
            {
                case EnemyMovementMode.Stationary:
                case EnemyMovementMode.Idle:
                    EnterState(AiState.Idle);
                    break;
                case EnemyMovementMode.Wander:
                    EnterState(AiState.Wander);
                    break;
                case EnemyMovementMode.Patrol:
                    EnterState(hasPatrolRoute ? AiState.Patrol : AiState.Wander);
                    break;
            }
        }

        private void EnterState(AiState newState)
        {
            if (IsStationary && IsRelocationState(newState))
                return;

            state = newState;

            switch (newState)
            {
                case AiState.Idle:
                    stateTimer = idleDuration;
                    break;
                case AiState.Wander:
                    moveTarget = PickRandomGroundPoint(homePosition, wanderRadius);
                    stateTimer = Random.Range(wanderPauseMin, wanderPauseMax);
                    break;
                case AiState.Patrol:
                    stateTimer = patrolWaitDuration;
                    if (hasPatrolRoute)
                        moveTarget = patrolPoints[patrolIndex].position;
                    break;
                case AiState.Investigate:
                    moveTarget = senses.TryGetHeardNoise(out Vector3 noisePosition)
                        ? noisePosition
                        : transform.position;
                    break;
                case AiState.ReturnHome:
                    moveTarget = homePosition;
                    break;
                case AiState.Chase:
                    moveTarget = lastKnownPlayerPosition;
                    break;
                case AiState.Attack:
                    break;
                case AiState.Search:
                    stateTimer = searchDuration;
                    moveTarget = lastKnownPlayerPosition + Random.insideUnitSphere * searchRadius;
                    moveTarget.y = lastKnownPlayerPosition.y;
                    if (TrySampleGround(moveTarget, out float searchGroundY))
                        moveTarget.y = searchGroundY;
                    break;
            }
        }

        private bool ShouldInvestigateNoise()
        {
            return !IsStationary && investigateNoise && senses.HasRecentNoise && senses.NoiseAge < 1f;
        }

        private static bool IsRelocationState(AiState aiState)
        {
            return aiState == AiState.Wander ||
                   aiState == AiState.Patrol ||
                   aiState == AiState.Investigate ||
                   aiState == AiState.ReturnHome ||
                   aiState == AiState.Chase ||
                   aiState == AiState.Search;
        }

        private void AdvancePatrolIndex()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            if (patrolMode == EnemyPatrolMode.PingPong && patrolPoints.Length > 1)
            {
                patrolIndex += patrolDirection;
                if (patrolIndex >= patrolPoints.Length)
                {
                    patrolIndex = patrolPoints.Length - 2;
                    patrolDirection = -1;
                }
                else if (patrolIndex < 0)
                {
                    patrolIndex = 1;
                    patrolDirection = 1;
                }
            }
            else
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            }
        }

        private Vector3 PickRandomGroundPoint(Vector3 origin, float radius)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 target = origin + new Vector3(offset.x, 0f, offset.y);
            if (TrySampleGround(target, out float groundY))
                target.y = groundY;
            return target;
        }

        private void HandleDeath()
        {
            enabled = false;
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            if (!AllowsTranslation &&
                state != AiState.Chase &&
                state != AiState.Investigate &&
                state != AiState.ReturnHome &&
                state != AiState.Search)
            {
                return;
            }

            if (IsStationary)
                return;

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
                currentLocomotionSpeed = speed;
            }

            if (toTarget.sqrMagnitude > 0.01f)
                FaceTowards(flatTarget);
        }

        private void FaceTowards(Vector3 worldPosition)
        {
            Vector3 toTarget = worldPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.01f)
                return;

            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
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

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 home = Application.isPlaying ? homePosition : transform.position;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            if (movementMode == EnemyMovementMode.Wander && wanderRadius > 0f)
                Gizmos.DrawWireSphere(home, wanderRadius);

            if (chaseRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
                Gizmos.DrawWireSphere(home, chaseRadius);
            }

            if (patrolPoints == null)
                return;

            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Transform point = patrolPoints[i];
                if (point == null)
                    continue;

                Gizmos.DrawWireSphere(point.position, 0.35f);
                Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (next != null)
                    Gizmos.DrawLine(point.position, next.position);
            }
        }
    }
}
