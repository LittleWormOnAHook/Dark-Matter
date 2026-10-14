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

            if (self == null || desiredStep.sqrMagnitude < 0.000001f || ai == null)
                return result;

            if (profile == null || !profile.enableChaseWallSlide)
                return result;

            ResolveCapsule(self, profile, out float radius, out float _, out Vector3 bottom, out Vector3 top);
            float standoff = ResolveStandoff(profile);
            int passes = Mathf.Clamp(profile.chaseWallSlidePasses, 1, 4);
            int mask = ChaseObstructionMask;
            float minProgress = Mathf.Max(0.0005f, profile.chaseWallMinProgress);
            float desiredMag = desiredStep.magnitude;

            Vector3 standoffPush = ComputeStandoffSeparation(
                self, bottom, top, radius, standoff, mask, out bool inBuffer, out Vector3 bufferNormal);
            result.InWallBufferContact = inBuffer;
            result.WallContactNormal = bufferNormal;
            if (bufferNormal.sqrMagnitude > 0.0001f)
                ai.chaseWallLastContactNormal = bufferNormal;

            Vector3 slid = SlideCapsuleStep(self, bottom, top, radius, desiredStep, standoff, passes, mask);
            slid += standoffPush;

            if (slid.sqrMagnitude >= minProgress * minProgress)
            {
                ai.chaseWallBlockedFrames = 0;
                ai.ChaseWallStutterFrames = 0;
                result.Step = slid;
                MaybeSetFaceFromStep(ref result, profile, slid);
                return result;
            }

            ai.chaseWallBlockedFrames++;

            int threshold = Mathf.Max(1, profile.chaseWallBlockedFramesBeforeFollow);
            if (ai.chaseWallBlockedFrames < threshold)
            {
                result.Step = slid.sqrMagnitude > 0.000001f ? slid : Vector3.zero;
                RegisterStutter(ai, result.Step, minProgress);
                if (result.Step.sqrMagnitude > 0.000001f)
                    MaybeSetFaceFromStep(ref result, profile, result.Step);
                else if (profile.chaseWallFollowFaceSlide)
                    TryFaceHintFromWall(self, bottom, top, radius, desiredStep, worldFollowTarget, profile, mask, ai, ref result);

                return result;
            }

            Vector3 toFollow = worldFollowTarget - self.position;
            toFollow.y = 0f;
            Vector3 forward = desiredStep.normalized;
            if (!TryProbeWallNormal(self, bottom, top, radius, forward, profile, mask, out Vector3 wallNormal))
                wallNormal = -forward;

            Vector3 followStep = BuildWallFollowStepLatched(wallNormal, toFollow, desiredMag, ai, profile, allowSidePick: true);
            Vector3 followed = SlideCapsuleStep(self, bottom, top, radius, followStep, standoff, passes, mask);
            if (followed.sqrMagnitude < minProgress * minProgress)
            {
                Vector3 altStep = BuildWallFollowStepLatched(wallNormal, toFollow, desiredMag, ai, profile, allowSidePick: false, forceOppositeSide: true);
                followed = SlideCapsuleStep(self, bottom, top, radius, altStep, standoff, passes, mask);
            }

            followed += standoffPush;
            if (followed.sqrMagnitude >= minProgress * minProgress)
            {
                ai.chaseWallBlockedFrames = 0;
                ai.ChaseWallStutterFrames = 0;
                result.Step = followed;
                MaybeSetFaceFromStep(ref result, profile, followed);
                return result;
            }

            result.Step = Vector3.zero;
            RegisterStutter(ai, result.Step, minProgress);
            if (profile.chaseWallFollowFaceSlide)
            {
                Vector3 hintDir = followStep.sqrMagnitude > 0.0001f
                    ? followStep.normalized
                    : BuildWallFollowStepLatched(wallNormal, toFollow, 1f, ai, profile, allowSidePick: true).normalized;
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
                validated = wallNormal * Mathf.Min(retreat, 0.45f);

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
            out bool inBuffer,
            out Vector3 dominantNormal)
        {
            inBuffer = false;
            dominantNormal = Vector3.zero;
            if (self == null)
                return Vector3.zero;

            Vector3 push = Vector3.zero;
            float probe = standoff + 0.2f;
            Vector3[] dirs =
            {
                self.forward,
                -self.forward,
                self.right,
                -self.right
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3 dir = dirs[i];
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    continue;

                dir.Normalize();
                if (!TryCapsuleCast(self, bottom, top, radius, dir, probe, mask, out RaycastHit hit))
                    continue;

                if (hit.distance >= standoff)
                    continue;

                inBuffer = true;
                float deficit = standoff - hit.distance;
                Vector3 n = hit.normal;
                n.y = 0f;
                if (n.sqrMagnitude < 0.0001f)
                    continue;

                n.Normalize();
                push += n * deficit;
                if (dominantNormal.sqrMagnitude < 0.0001f || deficit > 0.05f)
                    dominantNormal = n;
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
                if (toFollow.sqrMagnitude > 0.01f)
                {
                    Vector3 toNorm = toFollow.normalized;
                    float dotPos = Vector3.Dot(tangent, toNorm);
                    float dotNeg = Vector3.Dot(-tangent, toNorm);
                    if (Mathf.Abs(dotPos - dotNeg) >= hysteresis)
                        ai.ChaseWallFollowSideSign = dotPos >= dotNeg ? 1 : -1;
                }

                ai.ChaseWallFollowSideLockUntil = Time.time + (profile != null ? profile.chaseWallFollowMinSideSeconds : 0.55f);
            }

            int sign = ai.ChaseWallFollowSideSign;
            if (sign == 0)
                sign = 1;

            return tangent * sign * magnitude;
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
                if (!TryCapsuleCast(self, bottom, top, radius, dir, distance, mask, out RaycastHit hit))
                {
                    total += remaining;
                    break;
                }

                float allowed = Mathf.Max(0f, hit.distance - skin);
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
    }
}
