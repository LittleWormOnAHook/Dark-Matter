using UnityEngine;

namespace Project.AI
{
    /// <summary>2026-10-24a: low-health retreat stability - collider-only hold point check (no NavMesh), retreat stuck timeout,
    /// ranged back-off stuck -> stand and shoot. Settings: DM_EnemyBrainProfile "Low-health retreat stability".</summary>
    public partial class EnemyAiController
    {
        private static readonly RaycastHit[] AnchorCastHits = new RaycastHit[16];
        private static readonly Collider[] AnchorOverlapHits = new Collider[16];
        private float retreatStuckSince = -1f;
        private float retreatBestDistance;
        private Vector3 retreatTrackedAnchor;
        private bool retreatRepicked;
        private bool rangedBackingOff;
        private float rangedStuckSince = -1f;
        private float rangedStandUntil;
        private float cachedBodyRadius = -1f;

        private DM_EnemyBrainProfile StabilityProfile => brain != null && brain.Profile != null ? brain.Profile : DM_EnemyBrainProfile.Live;
        private float HolderArriveRadius => StabilityProfile != null ? StabilityProfile.holderArriveRadius : 0.5f;
        private float RetreatRepickInterval => StabilityProfile != null ? StabilityProfile.retreatRepickInterval : 0.75f;
        private float RangedRetreatHysteresis => StabilityProfile != null ? StabilityProfile.rangedRetreatHysteresis : 1f;
        private bool RangedStandActive => Time.time < rangedStandUntil;

        private float BodyRadius
        {
            get
            {
                if (cachedBodyRadius < 0f)
                {
                    CapsuleCollider cc = GetComponent<CapsuleCollider>();
                    cachedBodyRadius = cc != null ? Mathf.Clamp(cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z), 0.15f, 1.2f) : 0.4f;
                }
                return cachedBodyRadius;
            }
        }

        private bool IsIgnoredAnchorHit(Collider c)
        {
            if (c == null || c.isTrigger) return true;
            Transform t = c.transform;
            if (t == transform || t.IsChildOf(transform)) return true;
            if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) return true; // characters / loose props, not scenery
            if (c.GetComponentInParent<EnemyAiController>() != null) return true;
            Transform tgt = combat != null ? combat.CurrentTarget : null;
            return tgt != null && (t == tgt || t.IsChildOf(tgt));
        }

        /// <summary>Collider-only reachability of a hold / retreat point: clear body-radius path, free space, ground below.</summary>
        public bool IsHoldAnchorClear(Vector3 candidate)
        {
            DM_EnemyBrainProfile bp = StabilityProfile;
            if (bp == null || !bp.retreatAnchorObstacleCheck) return true;
            float r = BodyRadius;
            int mask = EnemyChaseWallSlide.ChaseObstructionMask;
            Vector3 from = transform.position + Vector3.up * (r + 0.35f);
            Vector3 to = new Vector3(candidate.x, from.y, candidate.z);
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len > 0.05f)
            {
                int n = Physics.SphereCastNonAlloc(from, r * 0.9f, d / len, AnchorCastHits, len, mask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                    if (!IsIgnoredAnchorHit(AnchorCastHits[i].collider)) return false;
            }
            int o = Physics.OverlapSphereNonAlloc(to, r, AnchorOverlapHits, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < o; i++)
                if (!IsIgnoredAnchorHit(AnchorOverlapHits[i])) return false;
            return Physics.Raycast(to, Vector3.down, r + 0.35f + 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Retreating holder making no progress: re-pick once at an offset bearing, then give up and fight. True = anchor changed.</summary>
        private bool TickRetreatStuck(Transform target, DM_EnemyEngagementProfile p, float now, float anchorDistance)
        {
            DM_EnemyBrainProfile bp = StabilityProfile;
            if (bp == null || brain == null || !brain.IsRetreating)
            {
                retreatStuckSince = -1f;
                retreatRepicked = false;
                return false;
            }
            if (anchorDistance <= bp.holderArriveRadius + 0.1f)
            {
                retreatStuckSince = -1f;
                return false;
            }
            if (retreatStuckSince < 0f || (engagementState.Anchor - retreatTrackedAnchor).sqrMagnitude > 0.01f ||
                anchorDistance < retreatBestDistance - bp.retreatMinProgress)
            {
                retreatStuckSince = now;
                retreatBestDistance = anchorDistance;
                retreatTrackedAnchor = engagementState.Anchor;
                return false;
            }
            if (now - retreatStuckSince < bp.retreatStuckSeconds)
                return false;

            retreatStuckSince = -1f;
            if (!retreatRepicked)
            {
                retreatRepicked = true;
                float deg = bp.retreatStuckRepickDegrees * (Random.value < 0.5f ? -1f : 1f);
                SetHoldAnchor(target, p, ResolveHoldRadius(p), now, false, deg);
                return true;
            }
            retreatRepicked = false;
            brain.CancelRetreat(true);
            SetHoldAnchor(target, p, ResolveHoldRadius(p), now, false);
            return true;
        }

        private void TrackRangedStuck(Vector3 before)
        {
            DM_EnemyBrainProfile bp = StabilityProfile;
            float now = Time.time;
            Vector3 moved = transform.position - before;
            moved.y = 0f;
            if (moved.sqrMagnitude > 1e-7f) { rangedStuckSince = -1f; return; }
            if (rangedStuckSince < 0f) { rangedStuckSince = now; return; }
            float limit = bp != null ? bp.retreatStuckSeconds : 1f;
            if (now - rangedStuckSince >= limit)
            {
                rangedStandUntil = now + limit * 2f;
                rangedStuckSince = -1f;
            }
        }
    }
}
