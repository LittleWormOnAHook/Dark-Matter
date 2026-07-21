using ECM2;
using Invector.vCharacterController;
using Project.AI;
using Project.Companions.Invector;
using Project.Crafting;
using Project.Data;
using Project.Interaction;
using Project.Pet;
using Project.Pioneers;
using Project.Player;
using UnityEngine;

namespace Project.Companions
{
    // The raw locomotion engine every other partial calls into: MoveTowards (the shared step/face
    // routine), capsule-collision sliding + step-up-onto-ledges, player/companion/pet avoidance
    // push, terrain/interior-surface grounding, and stuck detection/recovery (sidestep + player
    // path-trail backtracking). Split out of CompanionFollowController.cs — this is the largest,
    // most self-contained cluster in the original file.
    public partial class CompanionFollowController
    {
        private void MoveTowards(Vector3 target, float speed, bool allowIdleRest, bool faceMovement = true)
        {
            if (useTrailWhenPathBlocked && trailRecoveryUntil <= 0f && Time.time >= stepBackUntil)
                target = ResolveTrailAwareTarget(target);

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
                SyncInvectorRigidbody();
                currentSpeed = speed;
            }
            else
            {
                currentSpeed = 0f;
            }

            TryRecoverFromStuck(previousPosition, distance);

            Vector3 frameDelta = transform.position - previousPosition;
            frameDelta.y = 0f;
            if (frameDelta.sqrMagnitude > 0.0001f)
            {
                currentMoveDirection = frameDelta.normalized;
                if (trailRecoveryUntil > 0f && Time.time < trailRecoveryUntil)
                    consecutiveStuckCount = 0;
            }
            else if (toTarget.sqrMagnitude > 0.01f)
                currentMoveDirection = toTarget.normalized;

            if (!faceMovement)
                return;

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
            if (UsesInvectorMotor())
            {
                if (bodyCollider == null)
                    bodyCollider = gameObject.AddComponent<CapsuleCollider>();

                Rigidbody existingBody = GetComponent<Rigidbody>();
                if (existingBody != null)
                {
                    existingBody.isKinematic = true;
                    existingBody.useGravity = false;
                    existingBody.constraints = RigidbodyConstraints.FreezeRotation;
                }

                FollowerCollisionUtility.Register(bodyCollider);
                return;
            }

            if (bodyCollider == null)
                bodyCollider = gameObject.AddComponent<CapsuleCollider>();

            bodyCollider.radius = bodyRadius;
            bodyCollider.height = bodyHeight;
            bodyCollider.center = new Vector3(0f, bodyHeight * 0.5f, 0f);

            Rigidbody legacyBody = GetComponent<Rigidbody>();
            if (legacyBody != null)
                Object.Destroy(legacyBody);

            FollowerCollisionUtility.Register(bodyCollider);
        }

        private static bool UsesInvectorMotor(Component component)
        {
            return CompanionInvectorBootstrap.HasInvectorStack(component);
        }

        private bool UsesInvectorMotor()
        {
            return UsesInvectorMotor(this);
        }

        private void SyncInvectorRigidbody()
        {
            if (!UsesInvectorMotor())
                return;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null || !body.isKinematic)
                return;

            body.MovePosition(transform.position);
            body.MoveRotation(transform.rotation);
        }

        private void GetCapsulePoints(Vector3 worldPosition, out Vector3 bottom, out Vector3 top)
        {
            float halfHeight = Mathf.Max(bodyRadius, bodyHeight * 0.5f - bodyRadius);
            bottom = worldPosition + Vector3.up * (bodyRadius + collisionSkin);
            top = worldPosition + Vector3.up * (bodyHeight - bodyRadius - collisionSkin);
            if (top.y < bottom.y)
                top.y = bottom.y + 0.01f;
        }

        private void SyncLocomotionLimitsFromOwner()
        {
            if (owner == null)
                return;

            vThirdPersonController ownerMotor = owner.GetComponent<vThirdPersonController>();
            if (ownerMotor != null)
            {
                stepOffset = Mathf.Max(stepOffset, ownerMotor.stepOffsetMaxHeight + stepOffsetBonus);
                return;
            }

            if (ownerCharacter == null)
                return;

            CharacterMovement movement = ownerCharacter.GetComponent<CharacterMovement>();
            if (movement == null)
                return;

            stepOffset = Mathf.Max(stepOffset, movement.stepOffset + stepOffsetBonus);
            slopeLimit = movement.slopeLimit;
        }

        private bool IsWalkableNormal(Vector3 normal)
        {
            if (normal.y <= 0.001f)
                return false;

            return Vector3.Angle(normal, Vector3.up) <= slopeLimit + 0.01f;
        }

        private Vector3 ResolveMovement(Vector3 desiredStep)
        {
            if (desiredStep.sqrMagnitude < 0.0001f)
                return desiredStep;

            Vector3 start = transform.position;
            Vector3 direct = ResolveCapsuleMovementFrom(start, desiredStep);
            if (direct.sqrMagnitude >= desiredStep.sqrMagnitude * 0.95f)
                return direct;

            Vector3 stepped = TryStepUpMovement(start, desiredStep);
            if (stepped.sqrMagnitude > direct.sqrMagnitude)
                return stepped;

            if (direct.sqrMagnitude < desiredStep.sqrMagnitude * 0.35f)
            {
                Vector3 retryStep = TryStepUpMovement(start, desiredStep.normalized * desiredStep.magnitude);
                if (retryStep.sqrMagnitude > direct.sqrMagnitude)
                    return retryStep;
            }

            return direct;
        }

        private Vector3 TryStepUpMovement(Vector3 startPosition, Vector3 desiredStep)
        {
            float[] stepHeights =
            {
                stepOffset * 0.35f,
                stepOffset * 0.55f,
                stepOffset * 0.75f,
                stepOffset,
                stepOffset * 1.1f
            };

            Vector3 bestStep = Vector3.zero;
            for (int i = 0; i < stepHeights.Length; i++)
            {
                Vector3 candidate = TryStepUpAtHeight(startPosition, desiredStep, stepHeights[i]);
                if (candidate.sqrMagnitude > bestStep.sqrMagnitude)
                    bestStep = candidate;
            }

            if (bestStep.sqrMagnitude > 0.0001f)
                return bestStep;

            if (TryProbeBlockingHit(startPosition, desiredStep, out RaycastHit blockHit))
                return TryStepUpFromObstacleHit(startPosition, desiredStep, blockHit);

            return Vector3.zero;
        }

        private Vector3 TryStepUpAtHeight(Vector3 startPosition, Vector3 desiredStep, float stepUpHeight)
        {
            if (stepUpHeight <= 0.01f)
                return Vector3.zero;

            Vector3 raised = startPosition + Vector3.up * stepUpHeight;
            if (IsCapsuleObstructedAt(raised, ignoreWalkableGround: true))
                return Vector3.zero;

            Vector3 elevatedDelta = ResolveCapsuleMovementFrom(raised, desiredStep);
            if (elevatedDelta.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            Vector3 forwardPosition = raised + elevatedDelta;
            if (!TryRaycastGroundDetailed(forwardPosition, out float groundY, out Vector3 groundNormal, allowStepUp: true))
                return Vector3.zero;

            if (!IsWalkableNormal(groundNormal))
                return Vector3.zero;

            float heightDelta = groundY - startPosition.y;
            if (heightDelta < -collisionSkin || heightDelta > stepUpHeight + collisionSkin)
                return Vector3.zero;

            Vector3 finalPosition = new Vector3(forwardPosition.x, groundY, forwardPosition.z);
            if (IsCapsuleObstructedAt(finalPosition, ignoreWalkableGround: true))
                return Vector3.zero;

            return finalPosition - startPosition;
        }

        private Vector3 TryStepUpFromObstacleHit(Vector3 startPosition, Vector3 desiredStep, RaycastHit blockHit)
        {
            if (blockHit.collider == null || ShouldIgnoreCollider(blockHit.collider))
                return Vector3.zero;

            if (blockHit.normal.y > 0.35f && IsWalkableNormal(blockHit.normal))
            {
                float ledgeY = blockHit.point.y + groundOffset;
                float heightDelta = ledgeY - startPosition.y;
                if (heightDelta >= -collisionSkin && heightDelta <= stepOffset + collisionSkin)
                {
                    Vector3 ledgePosition = new Vector3(blockHit.point.x, ledgeY, blockHit.point.z);
                    if (!IsCapsuleObstructedAt(ledgePosition, ignoreWalkableGround: true))
                        return ledgePosition - startPosition;
                }
            }

            Vector3 forward = desiredStep.normalized;
            Vector3 stepProbe = startPosition + forward * Mathf.Max(blockHit.distance, bodyRadius);
            return TryStepUpAtHeight(startPosition, stepProbe - startPosition, stepOffset);
        }

        private bool TryProbeBlockingHit(Vector3 fromPosition, Vector3 desiredStep, out RaycastHit hit)
        {
            hit = default;
            if (desiredStep.sqrMagnitude < 0.0001f)
                return false;

            GetCapsulePoints(fromPosition, out Vector3 bottom, out Vector3 top);
            return Physics.CapsuleCast(
                bottom,
                top,
                Mathf.Max(0.05f, bodyRadius - collisionSkin),
                desiredStep.normalized,
                out hit,
                desiredStep.magnitude + collisionSkin,
                obstructionLayers,
                QueryTriggerInteraction.Ignore)
                && !ShouldIgnoreCollider(hit.collider);
        }

        private Vector3 ResolveCapsuleMovementFrom(Vector3 fromPosition, Vector3 desiredStep)
        {
            Vector3 position = fromPosition;
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

                Vector3 slide = hit.normal.y > 0.1f && !IsWalkableNormal(hit.normal)
                    ? Vector3.ProjectOnPlane(remaining, Vector3.up)
                    : Vector3.ProjectOnPlane(remaining, hit.normal);
                slide.y = 0f;
                remaining = slide;
            }

            return position - fromPosition;
        }

        private bool IsCapsuleObstructedAt(Vector3 worldPosition, bool ignoreWalkableGround = false)
        {
            GetCapsulePoints(worldPosition, out Vector3 bottom, out Vector3 top);
            float radius = Mathf.Max(0.05f, bodyRadius - collisionSkin);
            int overlapCount = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                OverlapBuffer,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                if (!ShouldIgnoreCollider(OverlapBuffer[i], ignoreWalkableGround))
                    return true;
            }

            return false;
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

        private bool ShouldIgnoreCollider(Collider collider, bool ignoreWalkableGround = false)
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
                return true;

            if (ignoreWalkableGround && IsWalkableGroundCollider(collider))
                return true;

            if (IsWorldItemCollider(collider))
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

        private static void CacheWorldItemLayers()
        {
            if (itemLayer < 0)
                itemLayer = LayerMask.NameToLayer("Item");

            if (resourceLayer < 0)
                resourceLayer = LayerMask.NameToLayer("Resource");
        }

        private static bool IsWorldItemCollider(Collider collider)
        {
            if (collider.GetComponentInParent<ItemPickup>() != null)
                return true;

            if (collider.GetComponentInParent<ResourceNode>() != null)
                return true;

            if (collider.GetComponentInParent<RecipePickup>() != null)
                return true;

            int layer = collider.gameObject.layer;
            if (itemLayer >= 0 && layer == itemLayer)
                return true;

            if (resourceLayer >= 0 && layer == resourceLayer)
                return true;

            return false;
        }

        private Vector3 ResolveTrailAwareTarget(Vector3 directTarget)
        {
            if (owner == null)
                return directTarget;

            float distance = HorizontalDistance(transform.position, directTarget);
            if (distance < 1.25f)
                return directTarget;

            if (!IsDirectPathBlocked(transform.position, directTarget))
                return directTarget;

            PlayerPathTrail trail = PlayerPathTrail.Instance ?? PlayerPathTrail.EnsureExists();
            if (trail == null || trail.PointCount < 2)
                return directTarget;

            if (trail.TryGetTrailFollowTarget(
                    transform.position,
                    trailFollowMinLookahead,
                    trailFollowMaxLookahead,
                    obstructionLayers,
                    out Vector3 trailTarget))
            {
                trailTarget.y = SampleTerrainHeight(trailTarget);
                return trailTarget;
            }

            return directTarget;
        }

        private bool IsDirectPathBlocked(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float distance = delta.magnitude;
            if (distance < 0.75f)
                return false;

            GetCapsulePoints(from, out Vector3 bottom, out Vector3 top);
            float castDistance = Mathf.Max(0.05f, distance - stopDistance * 0.5f);
            return Physics.CapsuleCast(
                bottom,
                top,
                Mathf.Max(0.05f, bodyRadius - collisionSkin),
                delta / distance,
                castDistance,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void TryRecoverFromStuck(Vector3 previousPosition, float distanceToTarget)
        {
            if (currentSpeed < 0.05f || distanceToTarget <= stopDistance)
            {
                lastStuckSamplePosition = transform.position;
                return;
            }

            if (Time.time < nextStuckSampleTime)
                return;

            nextStuckSampleTime = Time.time + stuckSampleInterval;
            Vector3 progress = transform.position - lastStuckSamplePosition;
            progress.y = 0f;
            lastStuckSamplePosition = transform.position;

            if (progress.sqrMagnitude >= stuckMinProgress * stuckMinProgress)
            {
                consecutiveStuckCount = Mathf.Max(0, consecutiveStuckCount - 1);
                return;
            }

            consecutiveStuckCount++;

            if (trailAttemptsThisEpisode < maxTrailAttemptsBeforeSidestep && TryBeginTrailRecovery())
                return;

            Vector3 forward = currentMoveDirection.sqrMagnitude > 0.01f
                ? currentMoveDirection
                : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            Vector3 sidestep = Vector3.Cross(Vector3.up, forward.normalized) * (stuckSidestepSign * stuckRecoverySidestep);
            stuckSidestepSign = -stuckSidestepSign;

            transform.position += ResolveMovement(sidestep);
            Depenetrate();
            trailAttemptsThisEpisode = 0;
        }

        private bool TryBeginTrailRecovery()
        {
            PlayerPathTrail trail = PlayerPathTrail.Instance ?? PlayerPathTrail.EnsureExists();
            if (trail == null)
                return false;

            if (!trail.TryGetBacktrackTarget(
                    transform.position,
                    trailRecoveryMinLookback,
                    trailRecoveryMaxLookback,
                    consecutiveStuckCount,
                    obstructionLayers,
                    out Vector3 backtrackTarget))
            {
                return false;
            }

            trailRecoveryTarget = backtrackTarget;
            trailRecoveryTarget.y = SampleTerrainHeight(trailRecoveryTarget);
            trailRecoveryUntil = Time.time + trailRecoveryMaxDuration;
            trailAttemptsThisEpisode++;
            return true;
        }

        private void EndTrailRecovery(bool resumeFollow)
        {
            trailRecoveryUntil = 0f;
            trailAttemptsThisEpisode = 0;
            if (resumeFollow)
                consecutiveStuckCount = Mathf.Max(0, consecutiveStuckCount - 2);
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

        private void SnapToTerrain()
        {
            if (owner != null && IsOwnerStationary() && isNearFormation && !isWandering)
                return;

            Vector3 pos = transform.position;
            bool allowStepUp = currentSpeed > 0.05f || catchUpActive || trailRecoveryUntil > 0f;
            float walkableY = ResolveWalkableGroundY(pos, allowStepUp: allowStepUp);
            float baselineY = GetTerrainBaselineY(pos);

            if (!IsOnInteriorWalkableSurface(pos, walkableY))
                walkableY = Mathf.Min(walkableY, baselineY + maxHeightAboveTerrain);

            if (pos.y > walkableY + 0.02f)
                pos.y = walkableY;
            else if (walkableY - pos.y > 0.02f && walkableY - pos.y <= stepOffset + collisionSkin)
                pos.y = walkableY;

            transform.position = pos;
            Depenetrate();
            SyncInvectorRigidbody();
        }

        private bool IsOnInteriorWalkableSurface(Vector3 worldPosition, float surfaceY)
        {
            if (Mathf.Abs(worldPosition.y - surfaceY) > stepOffset + 0.15f)
                return false;

            float baselineY = GetTerrainBaselineY(worldPosition);
            if (surfaceY <= baselineY + maxHeightAboveTerrain + 0.05f)
                return false;

            Vector3 probe = new Vector3(worldPosition.x, surfaceY + 0.05f, worldPosition.z);
            if (!Physics.Raycast(
                    probe,
                    Vector3.down,
                    out RaycastHit hit,
                    stepOffset + 0.35f,
                    obstructionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return IsInteriorWalkableCollider(hit.collider);
        }

        private float SampleTerrainHeight(Vector3 worldPosition)
        {
            return ResolveWalkableGroundY(worldPosition, allowStepUp: true);
        }

        private float ResolveWalkableGroundY(Vector3 worldPosition, bool allowStepUp)
        {
            if (TryRaycastGroundDetailed(worldPosition, out float groundY, out _, allowStepUp))
                return groundY;

            return GetTerrainBaselineY(worldPosition);
        }

        private float GetTerrainBaselineY(Vector3 worldPosition)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return worldPosition.y;

            return terrain.SampleHeight(worldPosition) + terrain.transform.position.y + groundOffset;
        }

        private bool TryRaycastGround(Vector3 worldPosition, out float groundY)
        {
            return TryRaycastGroundDetailed(worldPosition, out groundY, out _, allowStepUp: false);
        }

        private bool TryRaycastGroundDetailed(
            Vector3 worldPosition,
            out float groundY,
            out Vector3 groundNormal,
            bool allowStepUp)
        {
            groundY = GetTerrainBaselineY(worldPosition);
            groundNormal = Vector3.up;

            float originY = worldPosition.y + groundProbeHeight;
            float baselineY = GetTerrainBaselineY(worldPosition);
            originY = Mathf.Max(originY, baselineY + groundProbeHeight);

            Vector3 origin = new Vector3(worldPosition.x, originY, worldPosition.z);
            float rayLength = (originY - worldPosition.y) + groundProbeDistance;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                rayLength,
                obstructionLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            float maxAllowedY = allowStepUp
                ? worldPosition.y + stepOffset + collisionSkin
                : worldPosition.y + collisionSkin;

            bool found = false;
            float bestScore = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || ShouldIgnoreCollider(collider) || !IsWalkableGroundCollider(collider))
                    continue;

                if (!IsWalkableNormal(hits[i].normal))
                    continue;

                float candidateY = hits[i].point.y + groundOffset;
                bool interior = IsInteriorWalkableCollider(collider);
                float ceilingY = interior
                    ? baselineY + maxInteriorHeightAboveTerrain
                    : baselineY + maxHeightAboveTerrain;
                if (candidateY > ceilingY + 0.05f)
                    continue;

                if (candidateY > maxAllowedY + 0.01f)
                    continue;

                float score = Mathf.Abs(candidateY - baselineY);
                if (candidateY > worldPosition.y + 0.05f)
                    score += interior ? 0.15f : 0.75f;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                groundY = candidateY;
                groundNormal = hits[i].normal;
                found = true;
            }

            if (!found && !allowStepUp && baselineY <= maxAllowedY + 0.01f)
            {
                groundY = baselineY;
                return true;
            }

            return found;
        }

        private static bool IsInteriorWalkableCollider(Collider collider)
        {
            if (collider == null || collider is TerrainCollider)
                return false;

            if (collider.CompareTag("Walkable") || collider.CompareTag("Dirt"))
                return true;

            return IsWalkableGeometryName(collider.name);
        }

        private static bool IsWalkableGroundCollider(Collider collider)
        {
            if (collider is TerrainCollider)
                return true;

            if (collider.CompareTag("Dirt") || collider.CompareTag("Walkable"))
                return true;

            if (collider.CompareTag("Building"))
                return false;

            return IsWalkableGeometryName(collider.name);
        }

        private static bool IsWalkableGeometryName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            string lower = objectName.ToLowerInvariant();
            return lower.Contains("ramp")
                || lower.Contains("stair")
                || lower.Contains("step")
                || lower.Contains("floor")
                || lower.Contains("walkway")
                || lower.Contains("platform")
                || lower.Contains("porch")
                || lower.Contains("deck");
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawTrailRecoveryGizmos || !Application.isPlaying || trailRecoveryUntil <= 0f)
                return;

            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.9f);
            Gizmos.DrawSphere(trailRecoveryTarget + Vector3.up * 0.12f, 0.22f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.85f, trailRecoveryTarget + Vector3.up * 0.85f);
        }
#endif
    }
}
