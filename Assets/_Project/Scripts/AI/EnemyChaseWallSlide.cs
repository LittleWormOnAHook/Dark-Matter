using System.Collections.Generic;
using Project.Building;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Non-NavMesh chase step adjustment: capsule cast against world blockers, slide along hit normals,
    /// then tangential wall-follow toward a world target when head-on blocked.
    /// </summary>
    public static class EnemyChaseWallSlide
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[8];
        private static readonly Collider[] OverlapHits = new Collider[16];
        private static readonly Vector3[] EscapeNodes = new Vector3[18];
        private static readonly float[] EscapeCosts = new float[18];
        private static readonly int[] EscapeParents = new int[18];
        private static readonly bool[] EscapeVisited = new bool[18];

        // Rare loop recovery only, not a predictive fan or a replacement pathfinder.
        // Try a bounded route around the current collider's expanded footprint.
        // Every edge is capsule-checked against ALL blockers, including neighbouring rocks.
        internal static bool TryBuildLocalEscape(Transform self, Collider obstacle, Vector3 goal,
            DM_EnemyBrainProfile profile, List<Vector3> points)
        {
            points.Clear();
            ResolveCapsule(self, profile, out float radius, out _, out Vector3 bottom, out Vector3 top);
            Bounds bounds = obstacle.bounds;
            float padding = radius + ResolveStandoff(profile) + 0.25f;
            EscapeNodes[0] = self.position;
            EscapeNodes[1] = goal;
            for (int i = 0; i < 4; i++)
                EscapeNodes[i + 2] = new Vector3(i % 2 == 0 ? bounds.min.x - padding : bounds.max.x + padding,
                    self.position.y, i < 2 ? bounds.min.z - padding : bounds.max.z + padding);
            int nodeCount = 6;
            int mask = ChaseObstructionMask;
            int neighbours = Physics.OverlapSphereNonAlloc(self.position, 10f, OverlapHits, mask, QueryTriggerInteraction.Ignore);
            for (int h = 0; h < neighbours && nodeCount + 4 <= EscapeNodes.Length; h++)
            {
                Collider neighbour = OverlapHits[h];
                if (neighbour == null || neighbour == obstacle || IsSelfCollider(neighbour, self) ||
                    (!(neighbour is BoxCollider) && !(neighbour is MeshCollider)) ||
                    neighbour.GetComponentInParent<EnemyAiController>() != null ||
                    neighbour.bounds.max.y < self.position.y + radius * 2f) continue;
                Bounds nearby = neighbour.bounds;
                for (int corner = 0; corner < 4; corner++)
                    EscapeNodes[nodeCount++] = new Vector3(corner % 2 == 0 ? nearby.min.x - padding : nearby.max.x + padding,
                        self.position.y, corner < 2 ? nearby.min.z - padding : nearby.max.z + padding);
            }
            for (int i = 0; i < nodeCount; i++)
            {
                EscapeCosts[i] = i == 0 ? 0f : float.PositiveInfinity;
                EscapeParents[i] = -1;
                EscapeVisited[i] = false;
            }
            for (int iteration = 0; iteration < nodeCount; iteration++)
            {
                int next = -1;
                for (int i = 0; i < nodeCount; i++)
                    if (!EscapeVisited[i] && !float.IsPositiveInfinity(EscapeCosts[i]) &&
                        (next < 0 || EscapeCosts[i] < EscapeCosts[next])) next = i;
                if (next < 0) break;
                if (next == 1)
                {
                    for (int at = 1; at > 0; at = EscapeParents[at]) points.Add(EscapeNodes[at]);
                    points.Reverse();
                    return points.Count > 0;
                }
                EscapeVisited[next] = true;
                Vector3 offset = EscapeNodes[next] - self.position; offset.y = 0f;
                for (int to = 1; to < nodeCount; to++)
                {
                    if (EscapeVisited[to] || to == next) continue;
                    Vector3 delta = EscapeNodes[to] - EscapeNodes[next]; delta.y = 0f;
                    float length = delta.magnitude;
                    if (length < 0.01f || EscapeCosts[next] + length >= EscapeCosts[to]) continue;
                    if (TryCapsuleCast(self, bottom + offset, top + offset, radius + 0.05f,
                        delta / length, length, mask, out _)) continue;
                    // Casts may miss a collider containing the endpoint.
                    Vector3 endOffset = EscapeNodes[to] - self.position; endOffset.y = 0f;
                    int overlaps = Physics.OverlapCapsuleNonAlloc(bottom + endOffset, top + endOffset,
                        radius, OverlapHits, mask, QueryTriggerInteraction.Ignore);
                    bool occupied = overlaps == OverlapHits.Length;
                    for (int h = 0; h < overlaps && !occupied; h++)
                    {
                        Collider hit = OverlapHits[h];
                        if (hit != null && !IsSelfCollider(hit, self) && !(hit is TerrainCollider) &&
                            hit.bounds.max.y > self.position.y + radius * 2f) occupied = true;
                    }
                    if (occupied) continue;
                    EscapeCosts[to] = EscapeCosts[next] + length;
                    EscapeParents[to] = next;
                }
            }
            return false;
        }
        private static readonly Vector3[] BufferProbeDirections =
        {
            Vector3.forward, Vector3.back, Vector3.right, Vector3.left
        };

        public struct AdjustResult
        {
            public Vector3 Step;
            public bool PreferFaceSlideDirection;
            public Vector3 FaceDirection;
            public bool InWallBufferContact;
            public Vector3 WallContactNormal;
        }

        public static int ChaseObstructionMask
        {
            get
            {
                int mask = DMBuildingLayers.OverlapMask;
                if (mask == 0)
                    mask = Physics.DefaultRaycastLayers;
                return mask & ~(1 << DMBuildingLayers.IgnoreRaycastLayer);
            }
        }

        public static AdjustResult AdjustStep(
            Transform self,
            Vector3 desiredStep,
            Vector3 worldFollowTarget,
            DM_EnemyBrainProfile profile,
            EnemyAiController ai)
        {
            AdjustResult result = default;
            result.Step = desiredStep;

            if (self == null || ai == null)
                return result;

            if (TryExtractFromOverlap(self, profile, out Vector3 extractEarly, out Vector3 extractNormalEarly))
            {
                result.Step = extractEarly;
                result.InWallBufferContact = true;
                result.WallContactNormal = extractNormalEarly;
                ai.chaseWallLastContactNormal = extractNormalEarly;
                MaybeSetFaceFromStep(ref result, profile, extractEarly);
                return result;
            }

            if (desiredStep.sqrMagnitude < 0.000001f)
                return result;

            if (profile == null || !profile.enableChaseWallSlide)
            {
                result.Step = ClampStepToGeometry(self, desiredStep);
                return result;
            }

            ResolveCapsule(self, profile, out float radius, out float _, out Vector3 bottom, out Vector3 top);
            float standoff = ResolveStandoff(profile);
            int passes = Mathf.Clamp(profile.chaseWallSlidePasses, 1, 4);
            int mask = ChaseObstructionMask;
            float minProgress = Mathf.Max(0.0005f, profile.chaseWallMinProgress);
            float desiredMag = desiredStep.magnitude;

            Vector3 lockedNormal = Time.time < ai.ChaseWallFollowSideLockUntil &&
                                   ai.chaseWallLastContactNormal.sqrMagnitude > 0.0001f
                ? ai.chaseWallLastContactNormal
                : Vector3.zero;

            Vector3 standoffPush = ComputeStandoffSeparation(
                self, bottom, top, radius, standoff, mask, profile, lockedNormal, out bool inBuffer, out Vector3 bufferNormal);
            result.InWallBufferContact = inBuffer;
            result.WallContactNormal = bufferNormal;
            if (bufferNormal.sqrMagnitude > 0.0001f)
                ai.chaseWallLastContactNormal = bufferNormal;

            if (standoffPush.sqrMagnitude > 0.000001f)
            {
                if (desiredStep.sqrMagnitude > 0.000001f &&
                    Vector3.Dot(standoffPush.normalized, desiredStep.normalized) < -0.25f)
                    standoffPush *= 0.35f;
                standoffPush = SlideCapsuleStep(self, bottom, top, radius, standoffPush, standoff, 1, mask);
            }

            Vector3 desiredForSlide = desiredStep;
            if (TryResolveFanSteerDirection(
                    self,
                    bottom,
                    top,
                    radius,
                    desiredStep,
                    worldFollowTarget,
                    profile,
                    mask,
                    ai,
                    out Vector3 fanDir))
            {
                desiredForSlide = fanDir * desiredMag;
            }

            Vector3 forward = desiredForSlide.sqrMagnitude > 0.0001f ? desiredForSlide.normalized : desiredStep.normalized;
            RaycastHit forwardHit = default;
            bool headOnBlock = forward.sqrMagnitude > 0.0001f &&
                TryCapsuleCast(self, bottom, top, radius, forward.normalized,
                    profile.chaseWallProbeDistance, mask, out forwardHit);
            Vector3 probedNormal = headOnBlock ? forwardHit.normal : Vector3.zero;
            Collider contactSurface = headOnBlock ? forwardHit.collider : null;

            // Retain contact across short-probe misses until the destination corridor
            // clears. The diagonal probe bridges rounded capsule contact at corners.
            Vector3 previousNormal = ai.chaseWallLastContactNormal;
            previousNormal.y = 0f;
            if (!headOnBlock && ai.ChaseWallFollowSideSign != 0 && previousNormal.sqrMagnitude > 0.0001f)
            {
                Vector3 toDestination = worldFollowTarget - self.position;
                toDestination.y = 0f;
                bool corridorBlocked = toDestination.sqrMagnitude > 0.0001f &&
                    TryCapsuleCast(self, bottom, top, radius, toDestination.normalized,
                        toDestination.magnitude + standoff, mask, out _);
                if (corridorBlocked)
                {
                    Vector3 tangent = Vector3.Cross(Vector3.up, previousNormal.normalized) * ai.ChaseWallFollowSideSign;
                    float contactProbe = Mathf.Max(profile.chaseWallProbeDistance, standoff * 1.5f + 0.1f);
                    if (TryCapsuleCast(self, bottom, top, radius, -previousNormal.normalized,
                            contactProbe, mask, out RaycastHit sideHit) ||
                        TryCapsuleCast(self, bottom, top, radius, (-previousNormal.normalized - tangent).normalized,
                            contactProbe, mask, out sideHit))
                    {
                        probedNormal = sideHit.normal;
                        contactSurface = sideHit.collider;
                        headOnBlock = true;
                    }
                }
                else
                {
                    ai.ChaseWallFollowSideSign = 0;
                    ai.ChaseWallFollowSideLockUntil = 0f;
                    ai.chaseWallLastContactNormal = Vector3.zero;
                    ai.ChaseWallSurface = null;
                    ai.ChaseWallSurfaceNormal = Vector3.zero;
                }
            }

            Vector3 slid = SlideCapsuleStep(self, bottom, top, radius, desiredForSlide, standoff, passes, mask);
            Vector3 faceTravel = slid;
            if (!headOnBlock)
            {
                slid += standoffPush;
                if (slid.sqrMagnitude > 0.000001f)
                    slid = SlideCapsuleStep(self, bottom, top, radius, slid, standoff, 1, mask);
            }

            if (!headOnBlock && slid.sqrMagnitude >= minProgress * minProgress)
            {
                ai.chaseWallBlockedFrames = 0;
                ai.ChaseWallStutterFrames = 0;
                ai.ChaseWallFollowFailStreak = 0;
                result.Step = slid;
                // A clearance push is corrective translation, not a new heading.
                // Keep it out of facing when the forward probe clears at a corner.
                MaybeSetFaceFromStep(ref result, profile,
                    faceTravel.sqrMagnitude > minProgress * minProgress ? faceTravel : desiredForSlide);
                return result;
            }

            if (headOnBlock)
            {
                Vector3 toFollowImmediate = worldFollowTarget - self.position;
                toFollowImmediate.y = 0f;
                Vector3 wallNormalImmediate = probedNormal.sqrMagnitude > 0.0001f ? probedNormal : -forward;
                wallNormalImmediate.y = 0f;
                wallNormalImmediate.Normalize();
                // Around one convex box, a latched winding direction advances the
                // contact normal in one direction. A cast snapping back to the broad
                // face at a thin endpoint must not reverse that established progress.
                if (IsThinWallBox(contactSurface as BoxCollider, radius) && ai.ChaseWallSurface == contactSurface &&
                    ai.ChaseWallFollowSideSign != 0 && ai.ChaseWallSurfaceNormal.sqrMagnitude > 0.0001f &&
                    Vector3.SignedAngle(ai.ChaseWallSurfaceNormal, wallNormalImmediate, Vector3.up) *
                        ai.ChaseWallFollowSideSign < 0f)
                    wallNormalImmediate = ai.ChaseWallSurfaceNormal;
                ai.ChaseWallSurface = contactSurface;
                ai.ChaseWallSurfaceNormal = wallNormalImmediate;
                if (wallNormalImmediate.sqrMagnitude > 0.0001f)
                {
                    result.InWallBufferContact = true;
                    result.WallContactNormal = wallNormalImmediate;
                    ai.chaseWallLastContactNormal = wallNormalImmediate;
                }

                bool keepLatchedSide = ai.ChaseWallFollowSideSign != 0;
                if (!keepLatchedSide && contactSurface is BoxCollider box && IsThinWallBox(box, radius))
                {
                    Vector3 tangent = Vector3.Cross(Vector3.up, wallNormalImmediate).normalized;
                    Vector3 extent = box.size * 0.5f;
                    float support = Mathf.Abs(Vector3.Dot(tangent, box.transform.TransformVector(Vector3.right * extent.x))) +
                        Mathf.Abs(Vector3.Dot(tangent, box.transform.TransformVector(Vector3.up * extent.y))) +
                        Mathf.Abs(Vector3.Dot(tangent, box.transform.TransformVector(Vector3.forward * extent.z)));
                    float offset = Vector3.Dot(box.transform.TransformPoint(box.center) - self.position, tangent);
                    float plus = Mathf.Max(0f, offset + support + radius + standoff);
                    float minus = Mathf.Max(0f, -offset + support + radius + standoff);
                    float plusCost = plus + Vector3.Distance(self.position + tangent * plus, worldFollowTarget);
                    float minusCost = minus + Vector3.Distance(self.position - tangent * minus, worldFollowTarget);
                    ai.ChaseWallFollowSideSign = plusCost <= minusCost ? 1 : -1;
                    keepLatchedSide = true;
                }
                Vector3 followNow = BuildWallFollowStepLatched(
                    wallNormalImmediate, toFollowImmediate, desiredMag, ai, profile, allowSidePick: !keepLatchedSide);
                // A walking loop never trips the stationary-stall fallback. Try the
                // other winding only after sustained route-level lack of progress,
                // and only if that tangent is actually collision-clear. Cooldown
                // prevents this recovery from becoming another left/right oscillator.
                if (ai.WallRouteRecoveryRequested)
                {
                    Vector3 escape = SlideCapsuleStep(self, bottom, top, radius, -followNow, standoff, passes, mask);
                    if (escape.sqrMagnitude >= minProgress * minProgress &&
                        Vector3.Dot(escape, -followNow) > 0f)
                    {
                        ai.ChaseWallFollowSideSign = ai.ChaseWallFollowSideSign > 0 ? -1 : 1;
                        ai.ChaseWallSurfaceNormal = wallNormalImmediate;
                        ai.NoteWallRouteRecovery();
                        followNow = -followNow;
                    }
                }
                Vector3 followedNow = SlideCapsuleStep(self, bottom, top, radius, followNow, standoff, passes, mask);
                followedNow += standoffPush;
                if (followedNow.sqrMagnitude > 0.000001f)
                    followedNow = SlideCapsuleStep(self, bottom, top, radius, followedNow, standoff, 1, mask);

                if (followedNow.sqrMagnitude >= minProgress * minProgress)
                {
                    ai.chaseWallBlockedFrames = 0;
                    ai.ChaseWallStutterFrames = 0;
                    ai.ChaseWallFollowFailStreak = 0;
                    ExtendWallFollowSideLock(ai, profile);
                    result.Step = followedNow;
                    // Face travel along the wall, not the small outward clearance push.
                    MaybeSetFaceFromStep(ref result, profile, followNow);
                    return result;
                }

                ai.chaseWallBlockedFrames++;
                // The immediate head-on branch used to return forever without reaching
                // the fail-streak fallback below. Change side only if it actually clears.
                ai.ChaseWallFollowFailStreak++;
                if (ai.ChaseWallFollowFailStreak >= Mathf.Max(2, profile.chaseWallFollowFlipAfterFails))
                {
                    Vector3 alternate = SlideCapsuleStep(self, bottom, top, radius, -followNow, standoff, passes, mask);
                    if (alternate.sqrMagnitude >= minProgress * minProgress)
                    {
                        ai.ChaseWallFollowSideSign = ai.ChaseWallFollowSideSign > 0 ? -1 : 1;
                        ai.ChaseWallFollowFailStreak = 0;
                        ai.chaseWallBlockedFrames = 0;
                        ai.ChaseWallStutterFrames = 0;
                        ExtendWallFollowSideLock(ai, profile);
                        result.Step = alternate;
                        MaybeSetFaceFromStep(ref result, profile, -followNow);
                        return result;
                    }
                }
                RegisterStutter(ai, followedNow, minProgress);
                ExtendWallFollowSideLock(ai, profile);
                result.Step = followedNow.sqrMagnitude > 0.000001f ? followedNow : Vector3.zero;
                if (result.Step.sqrMagnitude > 0.000001f)
                    MaybeSetFaceFromStep(ref result, profile, result.Step);
                else if (profile.chaseWallFollowFaceSlide && followNow.sqrMagnitude > 0.0001f)
                {
                    result.PreferFaceSlideDirection = true;
                    result.FaceDirection = followNow.normalized;
                }

                return result;
            }

            ai.chaseWallBlockedFrames++;

            int threshold = Mathf.Max(1, profile.chaseWallBlockedFramesBeforeFollow);
            if (ai.chaseWallBlockedFrames < threshold ||
                (profile.chaseWallFollowRequiresHeadOnBlock && !headOnBlock))
            {
                result.Step = slid.sqrMagnitude > 0.000001f ? slid : Vector3.zero;
                RegisterStutter(ai, result.Step, minProgress);
                if (result.Step.sqrMagnitude > 0.000001f)
                    MaybeSetFaceFromStep(ref result, profile, result.Step);
                return result;
            }

            Vector3 toFollow = worldFollowTarget - self.position;
            toFollow.y = 0f;
            if (!TryProbeWallNormal(self, bottom, top, radius, forward, profile, mask, out Vector3 wallNormal))
                wallNormal = -forward;

            bool allowPick = ai.ChaseWallFollowSideSign == 0;
            Vector3 followStep = BuildWallFollowStepLatched(wallNormal, toFollow, desiredMag, ai, profile, allowSidePick: allowPick);
            Vector3 followed = SlideCapsuleStep(self, bottom, top, radius, followStep, standoff, passes, mask);
            if (followed.sqrMagnitude < minProgress * minProgress)
            {
                ai.ChaseWallFollowFailStreak++;
                int flipAfter = profile != null ? Mathf.Max(2, profile.chaseWallFollowFlipAfterFails) : 3;
                if (ai.ChaseWallFollowFailStreak >= flipAfter)
                {
                    ai.ChaseWallFollowFailStreak = 0;
                    Vector3 altStep = BuildWallFollowStepLatched(
                        wallNormal,
                        toFollow,
                        desiredMag,
                        ai,
                        profile,
                        allowSidePick: false,
                        forceOppositeSide: true);
                    followed = SlideCapsuleStep(self, bottom, top, radius, altStep, standoff, passes, mask);
                }
            }
            else
            {
                ai.ChaseWallFollowFailStreak = 0;
            }

            followed += standoffPush;
            if (followed.sqrMagnitude >= minProgress * minProgress)
            {
                ai.chaseWallBlockedFrames = 0;
                ai.ChaseWallStutterFrames = 0;
                ai.ChaseWallFollowFailStreak = 0;
                ExtendWallFollowSideLock(ai, profile);
                result.Step = followed;
                MaybeSetFaceFromStep(ref result, profile, followed);
                return result;
            }

            result.Step = Vector3.zero;
            RegisterStutter(ai, result.Step, minProgress);
            ExtendWallFollowSideLock(ai, profile);
            if (profile.chaseWallFollowFaceSlide)
            {
                Vector3 hintDir = followStep.sqrMagnitude > 0.0001f
                    ? followStep.normalized
                    : BuildWallFollowStepLatched(wallNormal, toFollow, 1f, ai, profile, allowSidePick: false).normalized;
                if (hintDir.sqrMagnitude > 0.0001f)
                {
                    result.PreferFaceSlideDirection = true;
                    result.FaceDirection = hintDir;
                }
            }

            if (wallNormal.sqrMagnitude > 0.0001f)
            {
                result.WallContactNormal = wallNormal;
                ai.chaseWallLastContactNormal = wallNormal;
            }

            return result;
        }

        /// <summary>Capsule slide against static geometry — used even when wall-follow is disabled.</summary>
        public static Vector3 ClampStepToGeometry(Transform self, Vector3 step)
        {
            if (self == null)
                return step;

            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            if (TryExtractFromOverlap(self, profile, out Vector3 extract, out _))
                return extract;

            if (step.sqrMagnitude < 0.000001f)
                return step;

            ResolveCapsule(self, profile, out float radius, out _, out Vector3 bottom, out Vector3 top);
            float standoff = ResolveStandoff(profile);
            int passes = profile != null ? Mathf.Clamp(profile.chaseWallSlidePasses, 1, 4) : 2;
            return SlideCapsuleStep(self, bottom, top, radius, step, standoff, passes, ChaseObstructionMask);
        }

        public static bool TryExtractFromOverlap(Transform self, DM_EnemyBrainProfile profile, out Vector3 extract, out Vector3 normal)
        {
            extract = Vector3.zero;
            normal = Vector3.zero;
            if (self == null)
                return false;

            CapsuleCollider cap = self.GetComponentInChildren<CapsuleCollider>();
            if (cap == null || cap.isTrigger)
                return false;

            ResolveCapsule(self, profile, out float radius, out _, out Vector3 bottom, out Vector3 top);
            int mask = ChaseObstructionMask;
            int count = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius,
                OverlapHits,
                mask,
                QueryTriggerInteraction.Ignore);

            float deepest = 0f;
            Vector3 bestDir = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Collider other = OverlapHits[i];
                if (other == null || other.isTrigger || IsSelfCollider(other, self))
                    continue;

                if (!Physics.ComputePenetration(
                        cap,
                        cap.transform.position,
                        cap.transform.rotation,
                        other,
                        other.transform.position,
                        other.transform.rotation,
                        out Vector3 dir,
                        out float distance))
                    continue;

                // Ground penetration must not hide a shallower wall penetration.
                // Only lateral contacts can be resolved by this planar mover.
                if (Mathf.Abs(dir.y) > 0.65f || distance <= deepest)
                    continue;

                deepest = distance;
                bestDir = dir;
            }

            if (deepest < 0.01f || bestDir.sqrMagnitude < 0.0001f)
                return false;

            bestDir.y = 0f;
            if (bestDir.sqrMagnitude < 0.0001f)
                return false;

            bestDir.Normalize();
            extract = bestDir * (deepest + 0.04f);
            extract.y = 0f;
            normal = bestDir;
            return extract.sqrMagnitude > 0.0001f;
        }

        public static bool TryBuildRetreatNudge(
            Transform self,
            DM_EnemyBrainProfile profile,
            EnemyAiController ai,
            out Vector3 nudge)
        {
            nudge = Vector3.zero;
            if (self == null || ai == null || profile == null)
                return false;

            ResolveCapsule(self, profile, out float radius, out float _, out Vector3 bottom, out Vector3 top);
            int mask = ChaseObstructionMask;
            float standoff = ResolveStandoff(profile);
            float retreat = Mathf.Max(0.2f, profile.chaseWallRetreatStepMeters);
            float sidestep = Mathf.Max(0f, profile.chaseWallRetreatSidestepMeters);

            Vector3 wallNormal = ai.chaseWallLastContactNormal;
            wallNormal.y = 0f;
            if (wallNormal.sqrMagnitude < 0.0001f)
            {
                Vector3 probeDir = self.forward;
                probeDir.y = 0f;
                if (probeDir.sqrMagnitude < 0.0001f ||
                    !TryProbeWallNormal(self, bottom, top, radius, probeDir, profile, mask, out wallNormal))
                    return false;
            }

            wallNormal.Normalize();
            Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = self.right;
            tangent.Normalize();

            int side = ai.ChaseWallFollowSideSign;
            if (side == 0)
                side = 1;

            nudge = wallNormal * retreat + tangent * (sidestep * side);
            nudge.y = 0f;
            if (nudge.sqrMagnitude < 0.0001f)
                return false;

            Vector3 validated = SlideCapsuleStep(self, bottom, top, radius, nudge, standoff, 2, mask);
            if (validated.sqrMagnitude < 0.05f)
            {
                validated = SlideCapsuleStep(
                    self,
                    bottom,
                    top,
                    radius,
                    wallNormal * Mathf.Min(retreat, 0.45f),
                    standoff,
                    2,
                    mask);
            }

            nudge = validated;
            ai.chaseWallLastContactNormal = wallNormal;
            return nudge.sqrMagnitude > 0.0001f;
        }

        private static float ResolveStandoff(DM_EnemyBrainProfile profile)
        {
            if (profile == null)
                return 0.5f;

            float standoff = profile.chaseWallStandoffMeters;
            if (standoff <= 0.01f)
                standoff = 0.5f;

            return Mathf.Max(standoff, Mathf.Max(0.01f, profile.chaseWallSlideSkin));
        }

        private static Vector3 ComputeStandoffSeparation(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            float standoff,
            int mask,
            DM_EnemyBrainProfile profile,
            Vector3 lockedNormal,
            out bool inBuffer,
            out Vector3 dominantNormal)
        {
            inBuffer = false;
            dominantNormal = Vector3.zero;
            if (self == null)
                return Vector3.zero;

            Vector3 push = Vector3.zero;
            float probe = standoff + 0.2f;
            float bestDeficit = 0f;
            lockedNormal.y = 0f;
            bool hasLockedNormal = lockedNormal.sqrMagnitude > 0.0001f;
            int probeCount = hasLockedNormal ? 1 : BufferProbeDirections.Length;
            for (int i = 0; i < probeCount; i++)
            {
                Vector3 dir = hasLockedNormal ? -lockedNormal.normalized : BufferProbeDirections[i];
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    continue;

                dir.Normalize();
                if (!TryCapsuleCast(self, bottom, top, radius, dir, probe, mask, out RaycastHit hit))
                    continue;

                if (hit.distance >= standoff - 0.04f)
                    continue;

                inBuffer = true;
                float deficit = standoff - hit.distance;
                float minDeficit = profile != null ? profile.chaseWallStandoffPushMinDeficit : 0.07f;
                if (deficit < minDeficit)
                    continue;
                Vector3 n = hit.normal;
                n.y = 0f;
                if (n.sqrMagnitude < 0.0001f)
                    continue;

                n.Normalize();
                if (deficit > bestDeficit)
                {
                    bestDeficit = deficit;
                    push = n * deficit;
                    dominantNormal = n;
                }
            }

            push.y = 0f;
            return push;
        }

        private static void RegisterStutter(EnemyAiController ai, Vector3 step, float minProgress)
        {
            if (step.sqrMagnitude < minProgress * minProgress)
                ai.ChaseWallStutterFrames++;
            else
                ai.ChaseWallStutterFrames = 0;
        }

        private static void TryFaceHintFromWall(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 desiredStep,
            Vector3 worldFollowTarget,
            DM_EnemyBrainProfile profile,
            int mask,
            EnemyAiController ai,
            ref AdjustResult result)
        {
            Vector3 toFollowEarly = worldFollowTarget - self.position;
            toFollowEarly.y = 0f;
            Vector3 desiredForward = desiredStep.normalized;
            if (!TryProbeWallNormal(self, bottom, top, radius, desiredForward, profile, mask, out Vector3 wallNormalEarly))
                return;

            Vector3 hint = BuildWallFollowStepLatched(wallNormalEarly, toFollowEarly, 1f, ai, profile, allowSidePick: true);
            if (hint.sqrMagnitude > 0.0001f)
            {
                result.PreferFaceSlideDirection = true;
                result.FaceDirection = hint.normalized;
            }
        }

        private static void ExtendWallFollowSideLock(EnemyAiController ai, DM_EnemyBrainProfile profile)
        {
            if (ai == null)
                return;

            float lockSeconds = profile != null ? profile.chaseWallFollowMinSideSeconds : 0.55f;
            ai.ChaseWallFollowSideLockUntil = Time.time + lockSeconds * 0.85f;
            if (profile != null && profile.enableChaseWallPredictiveFan && ai.ChaseFanLatchedDirection.sqrMagnitude > 0.0001f)
                ai.ChaseFanLatchUntil = Time.time + profile.chaseWallFanLatchSeconds;
        }

        private static void MaybeSetFaceFromStep(ref AdjustResult result, DM_EnemyBrainProfile profile, Vector3 step)
        {
            if (profile == null || !profile.chaseWallFollowFaceSlide || step.sqrMagnitude < 0.000001f)
                return;

            result.PreferFaceSlideDirection = true;
            result.FaceDirection = step.normalized;
        }

        private static Vector3 BuildWallFollowStepLatched(
            Vector3 wallNormal,
            Vector3 toFollow,
            float magnitude,
            EnemyAiController ai,
            DM_EnemyBrainProfile profile,
            bool allowSidePick,
            bool forceOppositeSide = false)
        {
            wallNormal.y = 0f;
            if (wallNormal.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            wallNormal.Normalize();
            Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal);
            if (tangent.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            tangent.Normalize();

            if (forceOppositeSide)
            {
                ai.ChaseWallFollowSideSign = ai.ChaseWallFollowSideSign > 0 ? -1 : 1;
                if (ai.ChaseWallFollowSideSign == 0)
                    ai.ChaseWallFollowSideSign = 1;
                ai.ChaseWallFollowSideLockUntil = Time.time + (profile != null ? profile.chaseWallFollowMinSideSeconds : 0.55f);
            }
            else if (allowSidePick && Time.time >= ai.ChaseWallFollowSideLockUntil)
            {
                float hysteresis = profile != null ? profile.chaseWallFollowSideHysteresis : 0.18f;
                float lockSeconds = profile != null ? profile.chaseWallFollowMinSideSeconds : 0.55f;
                bool targetThroughWall = toFollow.sqrMagnitude > 0.01f &&
                                         Vector3.Dot(toFollow.normalized, wallNormal) < -0.35f;
                if (ai.ChaseWallFollowSideSign != 0 && targetThroughWall)
                {
                    ai.ChaseWallFollowSideLockUntil = Time.time + lockSeconds;
                }
                else if (toFollow.sqrMagnitude > 0.01f)
                {
                    Vector3 toNorm = toFollow.normalized;
                    float dotPos = Vector3.Dot(tangent, toNorm);
                    float dotNeg = Vector3.Dot(-tangent, toNorm);
                    float delta = dotPos - dotNeg;
                    if (ai.ChaseWallFollowSideSign == 0)
                        ai.ChaseWallFollowSideSign = delta >= 0f ? 1 : -1;
                    else if (Mathf.Abs(delta) >= hysteresis)
                        ai.ChaseWallFollowSideSign = delta >= 0f ? 1 : -1;
                    ai.ChaseWallFollowSideLockUntil = Time.time + lockSeconds;
                }
                else
                {
                    ai.ChaseWallFollowSideLockUntil = Time.time + lockSeconds;
                }
            }

            int sign = ai.ChaseWallFollowSideSign;
            if (sign == 0)
                sign = 1;

            return tangent * sign * magnitude;
        }

        private static bool IsThinWallBox(BoxCollider box, float radius)
        {
            if (box == null) return false;
            Vector3 scale = box.transform.lossyScale;
            float x = Mathf.Abs(box.size.x * scale.x);
            float z = Mathf.Abs(box.size.z * scale.z);
            float thickness = Mathf.Min(x, z);
            // Endpoint contacts can sample both broad faces when the wall is thinner
            // than the mover's diameter. Keep ordinary box/corner steering unchanged.
            return thickness < radius * 2f && Mathf.Max(x, z) > thickness * 2f;
        }

        private static bool TryProbeWallNormal(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 forward,
            DM_EnemyBrainProfile profile,
            int mask,
            out Vector3 wallNormal)
        {
            wallNormal = Vector3.zero;
            if (forward.sqrMagnitude < 0.0001f)
                return false;

            float probe = profile != null ? profile.chaseWallProbeDistance : 0.6f;
            if (TryCapsuleCast(self, bottom, top, radius, forward.normalized, probe, mask, out RaycastHit hit))
            {
                wallNormal = hit.normal;
                return wallNormal.sqrMagnitude > 0.0001f;
            }

            return false;
        }

        private static Vector3 SlideCapsuleStep(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 step,
            float skin,
            int passes,
            int mask)
        {
            Vector3 remaining = step;
            remaining.y = 0f;
            Vector3 total = Vector3.zero;

            for (int pass = 0; pass < passes && remaining.sqrMagnitude > 0.000001f; pass++)
            {
                Vector3 dir = remaining.normalized;
                float distance = remaining.magnitude;
                // Sweep from the position reached by the preceding pass. Include the
                // clearance in the sweep, otherwise short frames enter the buffer and
                // alternate between moving in and being pushed back out.
                if (!TryCapsuleCast(self, bottom + total, top + total, radius, dir, distance + skin, mask, out RaycastHit hit))
                {
                    total += remaining;
                    break;
                }

                float allowed = Mathf.Clamp(hit.distance - skin, 0f, distance);
                if (allowed > 0.0001f)
                {
                    Vector3 chunk = dir * allowed;
                    total += chunk;
                    remaining -= chunk;
                }

                Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
                slide.y = 0f;
                if (slide.sqrMagnitude < 0.000001f)
                    break;

                remaining = slide;
            }

            return total;
        }

        private static bool TryCapsuleCast(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 direction,
            float distance,
            int mask,
            out RaycastHit bestHit)
        {
            bestHit = default;
            int count = Physics.CapsuleCastNonAlloc(
                bottom,
                top,
                radius,
                direction,
                Hits,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);

            float nearest = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider c = Hits[i].collider;
                if (c == null || IsSelfCollider(c, self))
                    continue;

                // Floor/support contacts and surfaces we are leaving are not planar
                // obstructions. Zero-distance tangent hits otherwise pin wall slides.
                if (Mathf.Abs(Hits[i].normal.y) > 0.65f ||
                    Vector3.Dot(direction, Hits[i].normal) >= -0.0001f)
                    continue;

                if (Hits[i].distance < nearest)
                {
                    nearest = Hits[i].distance;
                    bestHit = Hits[i];
                    found = true;
                }
            }

            return found;
        }

        private static bool IsSelfCollider(Collider c, Transform self)
        {
            return c.transform == self || c.transform.IsChildOf(self);
        }

        private static bool TryResolveFanSteerDirection(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 desiredStep,
            Vector3 worldFollowTarget,
            DM_EnemyBrainProfile profile,
            int mask,
            EnemyAiController ai,
            out Vector3 steerDir)
        {
            steerDir = Vector3.zero;
            if (profile == null || !profile.enableChaseWallPredictiveFan || ai == null)
                return false;

            Vector3 wish = worldFollowTarget - self.position;
            wish.y = 0f;
            if (wish.sqrMagnitude < 0.01f)
            {
                wish = desiredStep;
                wish.y = 0f;
            }

            if (wish.sqrMagnitude < 0.0001f)
                return false;

            wish.Normalize();
            TryBlendLongRangeWish(self, bottom, top, radius, worldFollowTarget, profile, mask, ref wish);

            float lookAhead = Mathf.Max(0.25f, profile.chaseWallProbeDistance * profile.chaseWallFanLookAheadScale);
            float minProgress = Mathf.Max(0.0005f, profile.chaseWallMinProgress);
            float minRetarget = Mathf.Max(0.1f, profile.chaseWallFanMinRetargetSeconds);
            int rays = Mathf.Clamp(profile.chaseWallFanRayCount, 3, 11);
            float halfAngle = Mathf.Clamp(profile.chaseWallFanHalfAngleDegrees, 10f, 75f);
            float hysteresis = profile.chaseWallFanDirectionHysteresis * lookAhead;

            if (ai.ChaseFanLatchedDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 latched = ai.ChaseFanLatchedDirection.normalized;
                float latchedClear = MeasureDirectionClearance(self, bottom, top, radius, latched, lookAhead, mask);
                bool latchActive = Time.time < ai.ChaseFanLatchUntil;
                bool retargetCooldown = Time.time < ai.ChaseFanLastRetargetTime + minRetarget;
                if ((latchActive || retargetCooldown) && latchedClear >= minProgress * 1.5f)
                {
                    steerDir = latched;
                    return true;
                }
            }

            float bestScore = float.NegativeInfinity;
            Vector3 bestDir = wish;

            if (ai.ChaseFanLatchedDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 latched = ai.ChaseFanLatchedDirection.normalized;
                float latchedClear = MeasureDirectionClearance(self, bottom, top, radius, latched, lookAhead, mask);
                bestScore = ScoreFanDirection(latched, wish, latchedClear, lookAhead);
                bestDir = latched;
            }

            for (int i = 0; i < rays; i++)
            {
                float t = rays == 1 ? 0f : (i / (float)(rays - 1)) * 2f - 1f;
                Vector3 dir = RotateFlat(wish, t * halfAngle);
                float clear = MeasureDirectionClearance(self, bottom, top, radius, dir, lookAhead, mask);
                float score = ScoreFanDirection(dir, wish, clear, lookAhead);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            if (ai.ChaseFanLatchedDirection.sqrMagnitude > 0.0001f && Time.time < ai.ChaseFanLatchUntil)
            {
                Vector3 latched = ai.ChaseFanLatchedDirection.normalized;
                float latchedClear = MeasureDirectionClearance(self, bottom, top, radius, latched, lookAhead, mask);
                float latchedScore = ScoreFanDirection(latched, wish, latchedClear, lookAhead);
                if (latchedScore >= bestScore - hysteresis)
                    bestDir = latched;
            }

            bestDir.y = 0f;
            if (bestDir.sqrMagnitude < 0.0001f)
                return false;

            bestDir.Normalize();
            Vector3 previous = ai.ChaseFanLatchedDirection.sqrMagnitude > 0.0001f
                ? ai.ChaseFanLatchedDirection.normalized
                : Vector3.zero;
            bool directionChanged = previous.sqrMagnitude < 0.0001f || Vector3.Dot(previous, bestDir) < 0.98f;
            if (directionChanged)
            {
                ai.ChaseFanLatchedDirection = bestDir;
                ai.ChaseFanLatchUntil = Time.time + profile.chaseWallFanLatchSeconds;
                ai.ChaseFanLastRetargetTime = Time.time;
            }

            steerDir = bestDir;
            return true;
        }

        private static void TryBlendLongRangeWish(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 worldFollowTarget,
            DM_EnemyBrainProfile profile,
            int mask,
            ref Vector3 wish)
        {
            if (profile == null || !profile.enableChaseWallLongRangeFan)
                return;

            Vector3 toTarget = worldFollowTarget - self.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 3f)
                return;

            float range = Mathf.Clamp(profile.chaseWallLongRangeFanDistance, 8f, 24f);
            int rays = Mathf.Clamp(profile.chaseWallLongRangeFanRayCount, 3, 5);
            float halfAngle = Mathf.Min(profile.chaseWallFanHalfAngleDegrees, 32f);
            Vector3 baseWish = toTarget.normalized;

            float bestScore = float.NegativeInfinity;
            Vector3 bestDir = baseWish;
            for (int i = 0; i < rays; i++)
            {
                float t = rays == 1 ? 0f : (i / (float)(rays - 1)) * 2f - 1f;
                Vector3 dir = RotateFlat(baseWish, t * halfAngle);
                float clear = MeasureDirectionClearance(self, bottom, top, radius, dir, range, mask);
                float score = ScoreFanDirection(dir, baseWish, clear, range);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }

            float blend = Mathf.Clamp01(profile.chaseWallLongRangeFanBlend);
            wish = Vector3.Slerp(wish, bestDir, blend).normalized;
        }

        private static float ScoreFanDirection(Vector3 dir, Vector3 wish, float clearance, float lookAhead)
        {
            float align = Mathf.Max(0f, Vector3.Dot(dir.normalized, wish));
            float clearT = Mathf.Clamp01(clearance / Mathf.Max(0.01f, lookAhead));
            return align * 0.55f + clearT * 0.45f;
        }

        private static float MeasureDirectionClearance(
            Transform self,
            Vector3 bottom,
            Vector3 top,
            float radius,
            Vector3 direction,
            float distance,
            int mask)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return 0f;

            if (TryCapsuleCast(self, bottom, top, radius, direction.normalized, distance, mask, out RaycastHit hit))
                return hit.distance;

            return distance;
        }

        private static Vector3 RotateFlat(Vector3 direction, float degreesY)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            return (Quaternion.Euler(0f, degreesY, 0f) * direction.normalized).normalized;
        }

        private static void ResolveCapsule(
            Transform self,
            DM_EnemyBrainProfile profile,
            out float radius,
            out float height,
            out Vector3 bottom,
            out Vector3 top)
        {
            radius = profile != null ? profile.chaseWallSlideRadius : 0.35f;
            height = profile != null ? profile.chaseWallSlideHeight : 1.75f;

            CapsuleCollider cap = self.GetComponentInChildren<CapsuleCollider>();
            if (cap != null && !cap.isTrigger)
            {
                Vector3 scale = cap.transform.lossyScale;
                radius = cap.radius * Mathf.Max(scale.x, scale.z);
                height = cap.height * scale.y;
                Vector3 worldCenter = cap.transform.TransformPoint(cap.center);
                float halfCylinder = Mathf.Max(0f, height * 0.5f - radius);
                bottom = worldCenter - Vector3.up * halfCylinder;
                top = worldCenter + Vector3.up * halfCylinder;
                return;
            }

            Vector3 pos = self.position;
            float cyl = Mathf.Max(0f, height - radius * 2f);
            bottom = pos + Vector3.up * radius;
            top = bottom + Vector3.up * cyl;
        }

        /// <summary>
        /// Read-only virtual steer polyline from the live controller (no path objects created).
        /// </summary>
        public static bool TryGetVirtualSteerPolyline(EnemyAiController ai, List<Vector3> buffer)
        {
            if (ai == null || buffer == null)
                return false;

            return ai.FillVirtualSteerPolyline(buffer) >= 1;
        }
    }
}
