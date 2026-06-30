using ECM2;
using Project.AI;
using Project.Pet;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Formation follow with smoothed wander, companion spacing, terrain grounding, and obstacle sliding.
    /// </summary>
    public class CompanionFollowController : MonoBehaviour
    {
        private static readonly Vector3[] FormationOffsets =
        {
            new Vector3(-3.8f, 0f, -3.4f),
            new Vector3(3.8f, 0f, -3.4f),
            new Vector3(0f, 0f, -5.2f)
        };

        private static readonly Vector3[] IdleAnchorOffsets =
        {
            new Vector3(-2.5f, 0f, -0.8f),
            new Vector3(2.5f, 0f, -0.8f),
            new Vector3(0f, 0f, -3.4f),
            new Vector3(-1.9f, 0f, 1.5f),
            new Vector3(1.9f, 0f, 1.5f),
            new Vector3(0.8f, 0f, 2.1f),
            new Vector3(-0.8f, 0f, 2.1f)
        };

        private static readonly System.Collections.Generic.List<CompanionFollowController> ActiveCompanions =
            new System.Collections.Generic.List<CompanionFollowController>(4);

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.4f;
        [SerializeField] private float runSpeed = 8.5f;
        [SerializeField] private float catchUpSpeed = 8.5f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float stopDistance = 0.45f;
        [SerializeField] private float maxFollowDistance = 14f;
        [SerializeField] private float catchUpDistance = 5.5f;
        [SerializeField] private float teleportCatchUpDistance = 20f;
        [SerializeField] private float groundOffset = 0.05f;
        [SerializeField] private float bodyRadius = 0.42f;
        [SerializeField] private float bodyHeight = 1.75f;

        [Header("Individual Behavior")]
        [SerializeField] private float wanderRadius = 0.65f;
        [Tooltip("Scales walkSpeed while meandering near formation (1 = same as player walk).")]
        [SerializeField] [Range(0.1f, 1f)] private float wanderPaceScale = 0.3f;
        [SerializeField] private float wanderRetargetMin = 2.8f;
        [SerializeField] private float wanderRetargetMax = 4.6f;
        [SerializeField] private float wanderSmoothTime = 1.35f;
        [SerializeField] private float restFacingSpeed = 1.1f;
        [SerializeField] private float formationDriftDegreesPerSecond = 2.8f;
        [Tooltip("Formation/idle slots use travel direction, not owner body yaw (avoids orbiting when the player turns camera).")]
        [SerializeField] private float formationHeadingSmoothTime = 0.45f;
        [SerializeField] private float minOwnerSpeedForHeadingUpdate = 0.3f;

        [Header("Follow Delay")]
        [Tooltip("Seconds pioneers wait after the player starts moving before they begin following.")]
        [SerializeField] private float followMovementDelayMin = 2f;
        [SerializeField] private float followMovementDelayMax = 5f;

        [Header("Avoidance")]
        [SerializeField] private float playerAvoidRadius = 2.75f;
        [SerializeField] private float playerAvoidStrength = 1.15f;
        [SerializeField] private float companionAvoidRadius = 2.4f;
        [SerializeField] private float companionAvoidStrength = 0.7f;
        [SerializeField] private float petAvoidRadius = 2.4f;
        [SerializeField] private float petAvoidStrength = 0.65f;

        [Header("Idle Positions")]
        [SerializeField] [Range(0f, 1f)] private float idleProbability = 0.9f;
        [SerializeField] private float idleWanderRange = 10f;
        [Tooltip("Scales walkSpeed while wandering away from idle anchor when the owner is still.")]
        [SerializeField] [Range(0.1f, 1f)] private float idleWanderPaceScale = 0.3f;
        [SerializeField] private float idleRestDurationMin = 12f;
        [SerializeField] private float idleRestDurationMax = 28f;
        [SerializeField] private float idleWanderDurationMin = 3f;
        [SerializeField] private float idleWanderDurationMax = 6f;
        [SerializeField] private float idleAnchorChangeMin = 4.2f;
        [SerializeField] private float idleAnchorChangeMax = 7.5f;

        [Header("Collision")]
        [SerializeField] private LayerMask obstructionLayers = 1;
        [SerializeField] private int movementSlideIterations = 3;
        [SerializeField] private float collisionSkin = 0.03f;

        private CapsuleCollider bodyCollider;
        private static readonly Collider[] OverlapBuffer = new Collider[16];
        private Transform owner;
        private Character ownerCharacter;
        private CompanionTaskQueue taskQueue;
        private int formationSlot;
        private string pioneerSeed;
        private float currentSpeed;
        private float wanderPhase;
        private float formationDriftAngle;
        private float formationHeadingYaw;
        private float idleRestYaw;
        private float nextWanderRetargetTime;
        private Vector3 wanderTargetOffset;
        private Vector3 smoothedWanderOffset;
        private Vector3 wanderVelocity;
        private Vector3 stepBackTarget;
        private float stepBackUntil;
        private Vector3 currentMoveDirection;
        private Vector3 lastOwnerPosition;
        private Vector3 ownerTravelDelta;
        private float ownerMotionSpeed;
        private bool isNearFormation;
        private bool isWandering;
        private bool wasOwnerMoving;
        private int[] idlePositionOrder;
        private int idleOrderIndex;
        private Vector3 currentIdleAnchorLocal;
        private float nextIdleAnchorChangeTime;
        private Vector3 idleWanderWorldTarget;
        private bool idleWanderPhaseActive;
        private float idlePhaseEndsAt;
        private bool catchUpActive;
        private PioneerBehaviorProfile activeProfile = new PioneerBehaviorProfile();
        private PioneerFollowMode followMode = PioneerFollowMode.FollowPlayer;
        private SkilledPioneerClass pioneerClass = SkilledPioneerClass.CombatTactician;
        private CompanionCombatController combatController;
        private float holdFacingYaw;
        private float maintainDistanceUntil;
        private Vector3 maintainDistanceTarget;
        private float allowFollowMovementAt;
        private float scheduledFollowMovementDelay;

        public float CurrentSpeed => currentSpeed;
        public int FormationSlot => formationSlot;
        public Vector3 CurrentMoveDirection => currentMoveDirection;
        public bool IsNearFormation => isNearFormation;
        public bool IsWandering => isWandering;
        public PioneerFollowMode FollowMode => followMode;

        private void Awake()
        {
            EnsureBodyCollider();
        }

        public void Initialize(Transform followTarget, CompanionTaskQueue queue, int slotIndex, string pioneerId = null)
        {
            EnsureBodyCollider();
            owner = followTarget;
            ownerCharacter = followTarget != null ? followTarget.GetComponent<Character>() : null;
            lastOwnerPosition = followTarget != null ? followTarget.position : Vector3.zero;
            taskQueue = queue;
            formationSlot = Mathf.Clamp(slotIndex, 0, FormationOffsets.Length - 1);
            pioneerSeed = string.IsNullOrEmpty(pioneerId) ? name : pioneerId;
            combatController = GetComponent<CompanionCombatController>();
            formationHeadingYaw = followTarget != null ? followTarget.eulerAngles.y : 0f;

            int hash = pioneerSeed.GetHashCode();
            wanderPhase = (hash & 0xFFFF) / 65535f * Mathf.PI * 2f;
            formationDriftAngle = ((hash >> 16) & 0xFF) / 255f * 120f;
            idleRestYaw = ((hash >> 8) & 0xFF) / 255f * 50f - 25f;
            scheduledFollowMovementDelay = ResolveFollowMovementDelay();
            nextWanderRetargetTime = Time.time + Random.Range(wanderRetargetMin, wanderRetargetMax);
            PickNewWanderTarget();
            RepickIdlePositionRoutine();
            BeginIdlePhase();
        }

        public void ApplyBehaviorProfile(PioneerBehaviorProfile profile, SkilledPioneerClass skilledClass)
        {
            pioneerClass = skilledClass;
            ApplyBehaviorProfile(profile);
        }

        public void ApplyBehaviorProfile(PioneerBehaviorProfile profile)
        {
            activeProfile = profile != null ? profile.Clone() : new PioneerBehaviorProfile();
            followMode = activeProfile.followMode;
            walkSpeed = activeProfile.walkSpeed;
            runSpeed = activeProfile.runSpeed;
            catchUpSpeed = activeProfile.catchUpSpeed;
            catchUpDistance = activeProfile.catchUpDistance;
            maxFollowDistance = activeProfile.maxFollowDistance;
            stopDistance = activeProfile.stopDistance;
            wanderPaceScale = activeProfile.wanderPaceScale;
            idleWanderPaceScale = activeProfile.wanderPaceScale;
            formationDriftDegreesPerSecond = activeProfile.formationDriftDegreesPerSecond;
            formationHeadingSmoothTime = activeProfile.formationHeadingSmoothTime;
        }

        public void SetFollowMode(PioneerFollowMode mode)
        {
            followMode = mode;
            if (activeProfile != null)
                activeProfile.followMode = mode;
        }

        public void RequestCombatMaintainDistance(Vector3 worldPosition, float preferredDistance, float duration)
        {
            Vector3 away = transform.position - worldPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = transform.forward;

            away.Normalize();
            maintainDistanceTarget = worldPosition + away * preferredDistance;
            maintainDistanceUntil = Time.time + duration;
        }

        public void BeginIdlePhase()
        {
            idleWanderPhaseActive = false;
            idlePhaseEndsAt = Time.time + Random.Range(idleRestDurationMin, idleRestDurationMax);
            smoothedWanderOffset = Vector3.zero;
            wanderVelocity = Vector3.zero;
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            isWandering = false;
            catchUpActive = false;
        }

        private void OnEnable()
        {
            EnsureBodyCollider();
            if (!ActiveCompanions.Contains(this))
                ActiveCompanions.Add(this);
        }

        private void OnDisable()
        {
            if (bodyCollider != null)
                FollowerCollisionUtility.Unregister(bodyCollider);

            ActiveCompanions.Remove(this);
        }

        public void RequestCombatStepBack(Vector3 awayFromWorld, float distance, float duration)
        {
            Vector3 away = transform.position - awayFromWorld;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = -transform.forward;

            away.Normalize();
            stepBackTarget = transform.position + away * distance;
            stepBackUntil = Time.time + duration;
        }

        private void LateUpdate()
        {
            SnapToTerrain();
        }

        private void Update()
        {
            currentMoveDirection = Vector3.zero;
            isNearFormation = false;
            isWandering = false;

            if (owner == null || taskQueue == null)
            {
                currentSpeed = 0f;
                return;
            }

            if (Time.time < stepBackUntil)
            {
                MoveTowards(stepBackTarget, walkSpeed * 1.35f, allowIdleRest: false);
                return;
            }

            if (Time.time < maintainDistanceUntil)
            {
                MoveTowards(maintainDistanceTarget, walkSpeed, allowIdleRest: false);
                return;
            }

            SyncHoldFromTaskQueue();

            if (taskQueue.ShouldHold)
            {
                UpdateHoldBehavior();
                return;
            }

            if (TryCombatTetherReturn())
                return;

            if (!taskQueue.ShouldFollow)
            {
                currentSpeed = 0f;
                return;
            }

            UpdateOwnerMotionSpeed();
            UpdateFormationHeading();
            bool ownerStationary = IsOwnerStationary();

            if (ownerStationary && wasOwnerMoving)
            {
                RepickIdlePositionRoutine();
                BeginIdlePhase();
                ClearFollowMovementDelay();
            }

            if (!ownerStationary && !wasOwnerMoving)
                ScheduleFollowMovementDelay();

            wasOwnerMoving = !ownerStationary;

            if (ownerStationary)
            {
                UpdateIdleAnchorBehavior();
                return;
            }

            if (IsFollowMovementDelayed())
            {
                ApplyFollowMovementDelayHold();
                return;
            }

            if (!ownerStationary)
            {
                formationDriftAngle += formationDriftDegreesPerSecond * Time.deltaTime * GetDriftSign();
                if (formationDriftAngle > 360f)
                    formationDriftAngle -= 360f;
                else if (formationDriftAngle < 0f)
                    formationDriftAngle += 360f;
            }

            float driftForFormation = formationDriftAngle;
            Vector3 target = ResolveFollowTarget(driftForFormation);
            float distanceToTarget = HorizontalDistance(transform.position, target);
            float distanceToOwner = HorizontalDistance(transform.position, owner.position);

            if (TryTeleportCatchUp(distanceToOwner, distanceToTarget))
                return;

            catchUpActive = distanceToOwner > catchUpDistance || distanceToTarget > catchUpDistance;
            isNearFormation = distanceToTarget <= stopDistance + 0.2f;

            if (catchUpActive)
            {
                smoothedWanderOffset = Vector3.SmoothDamp(
                    smoothedWanderOffset,
                    Vector3.zero,
                    ref wanderVelocity,
                    wanderSmoothTime * 0.25f);

                if (distanceToOwner > catchUpDistance * 1.35f && followMode != PioneerFollowMode.FollowSelf)
                    target = owner.position;
            }
            else if (isNearFormation && distanceToOwner <= maxFollowDistance * 0.55f)
            {
                UpdateWanderOffset();
                target += smoothedWanderOffset;
                isWandering = smoothedWanderOffset.sqrMagnitude > 0.08f;
            }
            else
            {
                smoothedWanderOffset = Vector3.SmoothDamp(
                    smoothedWanderOffset,
                    Vector3.zero,
                    ref wanderVelocity,
                    wanderSmoothTime * 0.5f);
            }

            float speed = ResolveFollowSpeed(distanceToOwner, distanceToTarget, isNearFormation, isWandering);
            MoveTowards(target, speed, allowIdleRest: isNearFormation && !isWandering && !catchUpActive);
        }

        private void SyncHoldFromTaskQueue()
        {
            if (taskQueue == null || !taskQueue.HasHoldPoint)
                return;

            holdFacingYaw = taskQueue.HoldFacingYaw;
        }

        private void UpdateHoldBehavior()
        {
            if (taskQueue == null || !taskQueue.HasHoldPoint)
            {
                currentSpeed = 0f;
                ApplyHoldFacing();
                return;
            }

            Vector3 holdPoint = taskQueue.HoldPosition;
            holdPoint.y = SampleTerrainHeight(holdPoint);
            float distance = HorizontalDistance(transform.position, holdPoint);

            if (distance > stopDistance + 0.15f)
            {
                MoveTowards(holdPoint, walkSpeed, allowIdleRest: false);
                return;
            }

            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            ApplyHoldFacing();
        }

        private void ApplyHoldFacing()
        {
            Quaternion holdRotation = Quaternion.Euler(0f, holdFacingYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, holdRotation, restFacingSpeed * Time.deltaTime);
        }

        private bool TryCombatTetherReturn()
        {
            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator == null || !coordinator.IsCombatEngaged || activeProfile == null)
                return false;

            if (activeProfile.combatTetherRadius <= 0.01f)
                return false;

            Vector3 anchor = ResolveCombatAnchor();
            float distanceFromAnchor = HorizontalDistance(transform.position, anchor);
            if (distanceFromAnchor <= activeProfile.combatTetherRadius)
                return false;

            MoveTowards(anchor, catchUpSpeed * 0.9f, allowIdleRest: false);
            return true;
        }

        private Vector3 ResolveCombatAnchor()
        {
            if (taskQueue != null && taskQueue.ShouldHold && taskQueue.HasHoldPoint)
                return taskQueue.HoldPosition;

            if (followMode == PioneerFollowMode.DefendPlayer)
                return ResolveDefendPosition();

            return GetFormationPosition();
        }

        private Vector3 ResolveFollowTarget(float driftForFormation)
        {
            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator != null && coordinator.IsCombatEngaged && followMode == PioneerFollowMode.DefendPlayer)
                return ResolveDefendPosition();

            if (coordinator != null && coordinator.IsCombatEngaged && activeProfile != null
                && activeProfile.PrefersRangedSpacing(pioneerClass))
            {
                EnemyHealth target = combatController != null ? combatController.CurrentTarget : null;
                if (target != null && owner != null)
                {
                    Vector3 toTarget = target.transform.position - owner.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.01f)
                    {
                        float preferred = activeProfile.ResolvePreferredCombatDistance(pioneerClass);
                        Vector3 rangedPoint = owner.position + toTarget.normalized * preferred;
                        Vector3 lateral = Vector3.Cross(Vector3.up, toTarget.normalized)
                            * ((formationSlot - 1) * 0.85f);
                        rangedPoint += lateral;
                        rangedPoint.y = SampleTerrainHeight(rangedPoint);
                        return rangedPoint;
                    }
                }
            }

            return GetFormationPosition(driftForFormation);
        }

        private Vector3 ResolveDefendPosition()
        {
            if (owner == null)
                return transform.position;

            EnemyHealth threat = combatController != null ? combatController.CurrentTarget : null;
            if (threat == null)
                return GetFormationPosition();

            Vector3 playerPos = owner.position;
            Vector3 threatPos = threat.transform.position;
            Vector3 toThreat = threatPos - playerPos;
            toThreat.y = 0f;

            if (toThreat.sqrMagnitude < 0.01f)
                return GetFormationPosition();

            Vector3 defendDir = -toThreat.normalized;
            float slotSpread = (formationSlot - 1) * 1.15f;
            Vector3 lateral = Vector3.Cross(Vector3.up, toThreat.normalized) * slotSpread;
            Vector3 defendPoint = playerPos + defendDir * activeProfile.preferredCombatDistance + lateral;
            defendPoint.y = SampleTerrainHeight(defendPoint);
            return defendPoint;
        }

        private float ResolveFollowSpeed(float distanceToOwner, float distanceToTarget, bool nearFormation, bool wandering)
        {
            if (catchUpActive)
                return catchUpSpeed;

            if (ShouldRun(distanceToOwner, distanceToTarget))
                return runSpeed;

            if (nearFormation && wandering)
                return walkSpeed * wanderPaceScale;

            return walkSpeed;
        }

        private bool TryTeleportCatchUp(float distanceToOwner, float distanceToTarget)
        {
            if (distanceToOwner < teleportCatchUpDistance && distanceToTarget < teleportCatchUpDistance)
                return false;

            Vector3 formation = GetFormationPosition();
            formation.y = SampleTerrainHeight(formation);
            transform.position = formation;
            Depenetrate();
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            catchUpActive = false;
            return true;
        }

        private void ScheduleFollowMovementDelay()
        {
            scheduledFollowMovementDelay = ResolveFollowMovementDelay();
            allowFollowMovementAt = Time.time + scheduledFollowMovementDelay;
        }

        private void ClearFollowMovementDelay()
        {
            allowFollowMovementAt = 0f;
        }

        private bool IsFollowMovementDelayed() =>
            allowFollowMovementAt > 0f && Time.time < allowFollowMovementAt;

        private float ResolveFollowMovementDelay()
        {
            float min = Mathf.Min(followMovementDelayMin, followMovementDelayMax);
            float max = Mathf.Max(followMovementDelayMin, followMovementDelayMax);
            return Random.Range(min, max);
        }

        private void ApplyFollowMovementDelayHold()
        {
            currentSpeed = 0f;
            currentMoveDirection = Vector3.zero;
            isNearFormation = true;
            isWandering = false;
            catchUpActive = false;
            ApplyIdleRestFacing();
        }

        private void UpdateIdleAnchorBehavior()
        {
            if (owner == null)
            {
                currentSpeed = 0f;
                return;
            }

            if (Time.time >= nextIdleAnchorChangeTime)
                AdvanceIdleAnchor();

            if (Time.time >= idlePhaseEndsAt)
                RollIdlePhase();

            Vector3 anchorWorld = GetIdleAnchorWorld();

            if (!idleWanderPhaseActive)
            {
                isWandering = false;
                float distanceToAnchor = HorizontalDistance(transform.position, anchorWorld);
                isNearFormation = distanceToAnchor <= stopDistance + 0.35f;

                if (distanceToAnchor > stopDistance + 0.25f)
                {
                    MoveTowards(anchorWorld, walkSpeed * 0.75f, allowIdleRest: true);
                    return;
                }

                currentSpeed = 0f;
                currentMoveDirection = Vector3.zero;
                ApplyIdleRestFacing();
                return;
            }

            isWandering = true;
            float distanceToWanderTarget = HorizontalDistance(transform.position, idleWanderWorldTarget);
            isNearFormation = distanceToWanderTarget <= stopDistance + 0.35f;

            if (distanceToWanderTarget <= stopDistance + 0.2f)
            {
                idlePhaseEndsAt = Mathf.Min(idlePhaseEndsAt, Time.time + 0.35f);
                currentSpeed = 0f;
                ApplyIdleRestFacing();
                return;
            }

            MoveTowards(idleWanderWorldTarget, walkSpeed * idleWanderPaceScale, allowIdleRest: false);
        }

        private Vector3 GetIdleAnchorWorld()
        {
            Quaternion frame = Quaternion.Euler(0f, formationHeadingYaw, 0f);
            return owner.position + frame * currentIdleAnchorLocal;
        }

        private void RollIdlePhase()
        {
            idleWanderPhaseActive = Random.value > idleProbability;

            if (idleWanderPhaseActive)
            {
                PickIdleWanderDestination();
                idlePhaseEndsAt = Time.time + Random.Range(idleWanderDurationMin, idleWanderDurationMax);
                return;
            }

            idlePhaseEndsAt = Time.time + Random.Range(idleRestDurationMin, idleRestDurationMax);
        }

        private void PickIdleWanderDestination()
        {
            Vector3 origin = owner != null ? owner.position : transform.position;
            Vector2 offset = Random.insideUnitCircle * idleWanderRange;
            idleWanderWorldTarget = origin + new Vector3(offset.x, 0f, offset.y);
            idleWanderWorldTarget.y = SampleTerrainHeight(idleWanderWorldTarget);
        }

        private void RepickIdlePositionRoutine()
        {
            if (IdleAnchorOffsets.Length == 0)
                return;

            if (idlePositionOrder == null || idlePositionOrder.Length != IdleAnchorOffsets.Length)
                idlePositionOrder = new int[IdleAnchorOffsets.Length];

            for (int i = 0; i < idlePositionOrder.Length; i++)
                idlePositionOrder[i] = i;

            for (int i = idlePositionOrder.Length - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (idlePositionOrder[i], idlePositionOrder[swapIndex]) = (idlePositionOrder[swapIndex], idlePositionOrder[i]);
            }

            idleOrderIndex = 0;
            ApplyCurrentIdleAnchor();
        }

        private void AdvanceIdleAnchor()
        {
            idleOrderIndex = (idleOrderIndex + 1) % idlePositionOrder.Length;
            ApplyCurrentIdleAnchor();
        }

        private void ApplyCurrentIdleAnchor()
        {
            if (idlePositionOrder == null || idlePositionOrder.Length == 0)
                RepickIdlePositionRoutine();

            int anchorIndex = idlePositionOrder[Mathf.Clamp(idleOrderIndex, 0, idlePositionOrder.Length - 1)];
            currentIdleAnchorLocal = IdleAnchorOffsets[Mathf.Clamp(anchorIndex, 0, IdleAnchorOffsets.Length - 1)];
            nextIdleAnchorChangeTime = Time.time + Random.Range(idleAnchorChangeMin, idleAnchorChangeMax);
        }

        private void UpdateOwnerMotionSpeed()
        {
            if (owner == null)
            {
                ownerMotionSpeed = 0f;
                return;
            }

            Vector3 delta = owner.position - lastOwnerPosition;
            delta.y = 0f;
            ownerTravelDelta = delta;
            ownerMotionSpeed = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
            lastOwnerPosition = owner.position;
        }

        private void UpdateFormationHeading()
        {
            if (owner == null || ownerMotionSpeed < minOwnerSpeedForHeadingUpdate)
                return;

            if (ownerTravelDelta.sqrMagnitude < 0.0001f)
                return;

            float travelYaw = Mathf.Atan2(ownerTravelDelta.x, ownerTravelDelta.z) * Mathf.Rad2Deg;
            float smooth = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, formationHeadingSmoothTime));
            formationHeadingYaw = Mathf.LerpAngle(formationHeadingYaw, travelYaw, smooth);
        }

        private bool IsOwnerStationary()
        {
            if (owner == null)
                return true;

            if (ownerCharacter != null)
                return ownerCharacter.GetSpeed() < 0.12f;

            return ownerMotionSpeed < 0.12f;
        }

        public Vector3 GetFormationPosition(float driftAngleOverride = float.NaN)
        {
            float drift = float.IsNaN(driftAngleOverride) ? formationDriftAngle : driftAngleOverride;
            Vector3 target = GetFormationPosition(owner, formationSlot, drift, formationHeadingYaw);
            target.y = SampleTerrainHeight(target);
            return target;
        }

        public static Vector3 GetFormationPosition(Transform ownerTransform, int slotIndex, float driftAngle = 0f)
        {
            float headingYaw = ownerTransform != null ? ownerTransform.eulerAngles.y : 0f;
            return GetFormationPosition(ownerTransform, slotIndex, driftAngle, headingYaw);
        }

        public static Vector3 GetFormationPosition(
            Transform ownerTransform,
            int slotIndex,
            float driftAngle,
            float headingYaw)
        {
            if (ownerTransform == null)
                return Vector3.zero;

            int slot = Mathf.Clamp(slotIndex, 0, FormationOffsets.Length - 1);
            Vector3 offset = FormationOffsets[slot];
            if (Mathf.Abs(driftAngle) > 0.01f)
                offset = Quaternion.Euler(0f, driftAngle, 0f) * offset;

            Quaternion frame = Quaternion.Euler(0f, headingYaw, 0f);
            return ownerTransform.position + frame * offset;
        }

        private void PickNewWanderTarget()
        {
            float radius = wanderRadius * (0.65f + GetPersonalityFactor() * 0.35f);
            wanderPhase += Random.Range(0.8f, 1.6f);
            wanderTargetOffset = new Vector3(
                Mathf.Cos(wanderPhase) * radius,
                0f,
                Mathf.Sin(wanderPhase * 0.81f) * radius);
            nextWanderRetargetTime = Time.time + Random.Range(wanderRetargetMin, wanderRetargetMax);
        }

        private void UpdateWanderOffset()
        {
            if (Time.time >= nextWanderRetargetTime)
                PickNewWanderTarget();

            smoothedWanderOffset = Vector3.SmoothDamp(
                smoothedWanderOffset,
                wanderTargetOffset,
                ref wanderVelocity,
                wanderSmoothTime);
        }

        private void MoveTowards(Vector3 target, float speed, bool allowIdleRest)
        {
            Vector3 flatTarget = target;
            flatTarget.y = SampleTerrainHeight(flatTarget);

            Vector3 toTarget = flatTarget - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            toTarget += ComputeAvoidanceOffset();
            if (toTarget.sqrMagnitude > 0.0001f)
                distance = toTarget.magnitude;
            Vector3 previousPosition = transform.position;

            if (distance > stopDistance)
            {
                Vector3 direction = toTarget.normalized;
                Vector3 step = direction * (speed * Time.deltaTime);
                if (step.sqrMagnitude > distance * distance)
                    step = toTarget;

                step = ResolveMovement(step);
                transform.position += step;
                Depenetrate();
                currentSpeed = speed;
            }
            else
            {
                currentSpeed = 0f;
            }

            Vector3 frameDelta = transform.position - previousPosition;
            frameDelta.y = 0f;
            if (frameDelta.sqrMagnitude > 0.0001f)
                currentMoveDirection = frameDelta.normalized;
            else if (toTarget.sqrMagnitude > 0.01f)
                currentMoveDirection = toTarget.normalized;

            if (toTarget.sqrMagnitude > 0.01f && currentSpeed > 0.05f)
            {
                Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
            else if (allowIdleRest)
            {
                ApplyIdleRestFacing();
            }
        }

        private bool ShouldRun(float distanceToOwner, float distanceToTarget)
        {
            return distanceToOwner > maxFollowDistance * 0.42f || distanceToTarget > 2.5f;
        }

        private Vector3 ComputeAvoidanceOffset()
        {
            Vector3 push = Vector3.zero;

            if (owner != null && !catchUpActive)
            {
                Vector3 delta = transform.position - owner.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance > 0.01f && distance < playerAvoidRadius)
                {
                    float weight = 1f - distance / playerAvoidRadius;
                    push += delta.normalized * (weight * playerAvoidStrength);
                }
            }

            for (int i = 0; i < ActiveCompanions.Count; i++)
            {
                CompanionFollowController other = ActiveCompanions[i];
                if (other == null || other == this)
                    continue;

                Vector3 delta = transform.position - other.transform.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance <= 0.01f || distance >= companionAvoidRadius)
                    continue;

                float weight = 1f - distance / companionAvoidRadius;
                push += delta.normalized * (weight * companionAvoidStrength);
            }

            PetManager petManager = PetManager.Instance;
            if (petManager != null)
            {
                System.Collections.Generic.IReadOnlyList<PetController> pets = petManager.Pets;
                for (int i = 0; i < pets.Count; i++)
                {
                    PetController pet = pets[i];
                    if (pet == null || !pet.CompanionActive || !pet.gameObject.activeInHierarchy)
                        continue;

                    Vector3 delta = transform.position - pet.transform.position;
                    delta.y = 0f;
                    float distance = delta.magnitude;
                    if (distance <= 0.01f || distance >= petAvoidRadius)
                        continue;

                    float weight = 1f - distance / petAvoidRadius;
                    push += delta.normalized * (weight * petAvoidStrength);
                }
            }

            return push;
        }

        private void EnsureBodyCollider()
        {
            bodyCollider = GetComponent<CapsuleCollider>();
            if (bodyCollider == null)
                bodyCollider = gameObject.AddComponent<CapsuleCollider>();

            bodyCollider.radius = bodyRadius;
            bodyCollider.height = bodyHeight;
            bodyCollider.center = new Vector3(0f, bodyHeight * 0.5f, 0f);

            Rigidbody existingBody = GetComponent<Rigidbody>();
            if (existingBody != null)
                Object.Destroy(existingBody);

            FollowerCollisionUtility.Register(bodyCollider);
        }

        private void GetCapsulePoints(Vector3 worldPosition, out Vector3 bottom, out Vector3 top)
        {
            float halfHeight = Mathf.Max(bodyRadius, bodyHeight * 0.5f - bodyRadius);
            bottom = worldPosition + Vector3.up * (bodyRadius + collisionSkin);
            top = worldPosition + Vector3.up * (bodyHeight - bodyRadius - collisionSkin);
            if (top.y < bottom.y)
                top.y = bottom.y + 0.01f;
        }

        private Vector3 ResolveMovement(Vector3 desiredStep)
        {
            if (desiredStep.sqrMagnitude < 0.0001f)
                return desiredStep;

            Vector3 position = transform.position;
            Vector3 remaining = desiredStep;

            for (int iteration = 0; iteration < movementSlideIterations; iteration++)
            {
                if (remaining.sqrMagnitude < 0.0001f)
                    break;

                GetCapsulePoints(position, out Vector3 bottom, out Vector3 top);
                Vector3 direction = remaining.normalized;
                float distance = remaining.magnitude;

                if (!Physics.CapsuleCast(
                        bottom,
                        top,
                        Mathf.Max(0.05f, bodyRadius - collisionSkin),
                        direction,
                        out RaycastHit hit,
                        distance + collisionSkin,
                        obstructionLayers,
                        QueryTriggerInteraction.Ignore)
                    || ShouldIgnoreCollider(hit.collider))
                {
                    position += remaining;
                    break;
                }

                float moveDistance = Mathf.Max(0f, hit.distance - collisionSkin);
                position += direction * moveDistance;
                remaining -= direction * moveDistance;

                Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
                slide.y = 0f;
                remaining = slide;
            }

            return position - transform.position;
        }

        private void Depenetrate()
        {
            GetCapsulePoints(transform.position, out Vector3 bottom, out Vector3 top);
            float radius = Mathf.Max(0.05f, bodyRadius - collisionSkin);
            int overlapCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                OverlapBuffer,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);

            Vector3 center = transform.position + Vector3.up * (bodyHeight * 0.5f);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider other = OverlapBuffer[i];
                if (ShouldIgnoreCollider(other))
                    continue;

                Vector3 closest = GetDepenetrationPoint(other, center);
                Vector3 push = center - closest;
                push.y = 0f;
                float distance = push.magnitude;
                if (distance < 0.0001f)
                    continue;

                float penetration = radius - distance;
                if (penetration > 0f)
                    transform.position += push.normalized * (penetration + collisionSkin);
            }
        }

        private bool ShouldIgnoreCollider(Collider collider)
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
                return true;

            Transform hitTransform = collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return true;

            if (collider.GetComponentInParent<CompanionFollowController>() != null
                && collider.GetComponentInParent<CompanionFollowController>() != this)
            {
                return true;
            }

            if (owner != null)
            {
                if (hitTransform == owner)
                    return false;

                if (hitTransform.IsChildOf(owner))
                    return true;
            }

            return false;
        }

        private static Vector3 GetDepenetrationPoint(Collider collider, Vector3 center)
        {
            if (collider == null)
                return center;

            if (collider is MeshCollider meshCollider && !meshCollider.convex)
                return meshCollider.bounds.ClosestPoint(center);

            if (collider is TerrainCollider)
                return collider.bounds.ClosestPoint(center);

            return collider.ClosestPoint(center);
        }

        private void ApplyIdleRestFacing()
        {
            float targetYaw = formationHeadingYaw + idleRestYaw;
            Quaternion restRotation = Quaternion.Euler(0f, targetYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, restRotation, restFacingSpeed * Time.deltaTime);
        }

        private float GetPersonalityFactor()
        {
            return (Mathf.Abs(pioneerSeed.GetHashCode()) % 1000) / 1000f;
        }

        private float GetDriftSign()
        {
            return (pioneerSeed.GetHashCode() & 1) == 0 ? 1f : -1f;
        }

        private void SnapToTerrain()
        {
            if (owner != null && IsOwnerStationary() && isNearFormation && !isWandering)
                return;

            Vector3 pos = transform.position;
            float terrainY = SampleTerrainHeight(pos);
            if (Mathf.Abs(pos.y - terrainY) > 0.02f)
                pos.y = terrainY;
            transform.position = pos;
            Depenetrate();
        }

        private float SampleTerrainHeight(Vector3 worldPosition)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
                return terrain.SampleHeight(worldPosition) + terrain.transform.position.y + groundOffset;

            return worldPosition.y;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
