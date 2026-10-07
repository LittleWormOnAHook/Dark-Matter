using System.Collections.Generic;
using Project.Core;
using Project.Player;
using UnityEngine;

namespace Project.AI
{
    /// <summary>Per-enemy engagement bookkeeping, owned by DMEnemyEngagementDirector.</summary>
    public sealed class DMEngagementAgentState
    {
        public DMEngagementRole Role = DMEngagementRole.None;
        internal DMEnemyEngagementDirector.Ring Ring;
        public float JoinTime;
        public float LastTokenTime;
        public float ReawardBlockedUntil;
        public float HandoffGraceUntil;
        public int SequencesThisToken;
        public float LastSequenceEndTime = -999f;

        // World-anchored hold point (holders only).
        public bool HasAnchor;
        public Vector3 Anchor;
        public Vector3 AnchorOrigin;
        public float AnchorTime = -999f;

        public void ClearAnchor()
        {
            HasAnchor = false;
        }
    }

    /// <summary>
    /// Scene-wide melee attack token director (Spacing plan §5.3–5.4, merged into Combat Plan Phase 3).
    /// One ring per target (player, pioneers). Exactly one Engager per ring holds the melee token; everyone
    /// else is a Holder on a world-anchored outer ring. Re-scores at directorTickHz with anti-thrash rules
    /// (minimum hold, switch margin + confirm time, global hand-off rate limit, post-swing lock, max hold).
    /// Static, no GameObject: ticked by whichever enemy updates first each frame. No per-tick allocations.
    /// </summary>
    public static class DMEnemyEngagementDirector
    {
        internal sealed class Ring
        {
            public Transform Target;
            public CombatFocusController Focus;
            public readonly List<EnemyAiController> Members = new List<EnemyAiController>(8);
            public EnemyAiController Holder;
            public float HolderSince;
            public float LastHandoffTime = -999f;
            public EnemyAiController Challenger;
            public float ChallengerSince;
            public int ActiveHolderActions;
            public float HolderActionsEnd = -999f;
            public Vector3 LastTargetPosition;
            public float LastSampleTime = -1f;
            public float TargetSpeed;

            public void Reset()
            {
                Target = null;
                Focus = null;
                Members.Clear();
                Holder = null;
                HolderSince = 0f;
                LastHandoffTime = -999f;
                Challenger = null;
                ChallengerSince = 0f;
                ActiveHolderActions = 0;
                HolderActionsEnd = -999f;
                LastSampleTime = -1f;
                TargetSpeed = 0f;
            }
        }

        private static readonly List<Ring> Rings = new List<Ring>(4);
        private static readonly Stack<Ring> RingPool = new Stack<Ring>(4);
        private static float nextTickTime;
        private static int lastTickFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Rings.Clear();
            RingPool.Clear();
            nextTickTime = 0f;
            lastTickFrame = -1;
        }

        private static DM_EnemyEngagementProfile Profile => DM_EnemyEngagementProfile.Live;

        public static bool IsEnabled
        {
            get
            {
                DM_EnemyEngagementProfile p = Profile;
                return p != null && p.enableEngagementDirector;
            }
        }

        /// <summary>Joins (or keeps) the ring for <paramref name="target"/> and returns this enemy's role.</summary>
        public static DMEngagementRole UpdateMembership(EnemyAiController agent, Transform target)
        {
            if (agent == null || target == null)
                return DMEngagementRole.None;

            DMEngagementAgentState state = agent.EngagementState;
            Ring ring = state.Ring;
            if (ring == null || ring.Target != target)
            {
                Leave(agent);
                ring = GetOrCreateRing(target);
                ring.Members.Add(agent);
                state.Ring = ring;
                state.Role = DMEngagementRole.Holder;
                state.JoinTime = Time.time;
                state.LastTokenTime = Time.time;
                state.SequencesThisToken = 0;
                state.ClearAnchor();
                if (ring.Holder == null)
                    TickRing(ring, Profile, Time.time);
            }

            TickIfDue();
            return state.Role;
        }

        public static void Leave(EnemyAiController agent)
        {
            if (agent == null)
                return;

            DMEngagementAgentState state = agent.EngagementState;
            Ring ring = state.Ring;
            state.Ring = null;
            state.Role = DMEngagementRole.None;
            state.ClearAnchor();
            if (ring == null)
                return;

            ring.Members.Remove(agent);
            if (ring.Challenger == agent)
                ring.Challenger = null;
            if (ring.Holder == agent)
            {
                ring.Holder = null;
                state.LastTokenTime = Time.time;
                // Fill the gap right away so the fight doesn't stall when the Engager dies or leaves.
                if (ring.Members.Count > 0)
                    TickRing(ring, Profile, Time.time);
            }
        }

        /// <summary>Called by EnemyCombat when this enemy starts an attack sequence.</summary>
        public static void NotifyAttackBegan(EnemyAiController agent)
        {
            if (agent == null)
                return;
            agent.EngagementState.SequencesThisToken++;
        }

        public static bool IsHandoffGraceActive(EnemyAiController agent)
        {
            return agent != null && Time.time < agent.EngagementState.HandoffGraceUntil;
        }

        public static float GetTargetSpeed(EnemyAiController agent)
        {
            Ring ring = agent != null ? agent.EngagementState.Ring : null;
            return ring != null ? ring.TargetSpeed : 0f;
        }

        public static EnemyAiController GetEngager(EnemyAiController agent)
        {
            Ring ring = agent != null ? agent.EngagementState.Ring : null;
            return ring != null ? ring.Holder : null;
        }

        /// <summary>Group cap for holder idle actions (max concurrent + gap between actions).</summary>
        public static bool TryBeginHolderAction(EnemyAiController agent, float duration)
        {
            Ring ring = agent != null ? agent.EngagementState.Ring : null;
            if (ring == null)
                return true;

            DM_EnemyEngagementProfile p = Profile;
            float now = Time.time;
            if (now >= ring.HolderActionsEnd)
                ring.ActiveHolderActions = 0;

            int maxConcurrent = Mathf.Max(1, p.holderMaxConcurrentActions);
            if (ring.ActiveHolderActions >= maxConcurrent)
                return false;

            if (ring.ActiveHolderActions == 0 && now < ring.HolderActionsEnd + p.holderGroupActionGap)
                return false;

            ring.ActiveHolderActions++;
            ring.HolderActionsEnd = Mathf.Max(ring.HolderActionsEnd, now + Mathf.Max(0.1f, duration));
            return true;
        }

        /// <summary>
        /// Picks a world-anchored hold point on the outer ring near <paramref name="preferredFromTarget"/>,
        /// away from other holders' anchors and out of the target→Engager line.
        /// </summary>
        public static Vector3 ResolveHoldAnchor(EnemyAiController agent, Transform target, Vector3 preferredFromTarget, float radius)
        {
            DM_EnemyEngagementProfile p = Profile;
            Vector3 center = target.position;
            preferredFromTarget.y = 0f;
            if (preferredFromTarget.sqrMagnitude < 0.0001f)
                preferredFromTarget = -target.forward;
            preferredFromTarget.Normalize();

            Ring ring = agent.EngagementState.Ring;
            Vector3 engagerDir = Vector3.zero;
            if (ring != null && ring.Holder != null && ring.Holder != agent)
            {
                engagerDir = ring.Holder.transform.position - center;
                engagerDir.y = 0f;
                if (engagerDir.sqrMagnitude > 0.0001f)
                    engagerDir.Normalize();
            }

            Vector3 best = center + preferredFromTarget * radius;
            for (int step = 0; step < 13; step++)
            {
                // 0, +20, -20, +40, -40 … ±120 degrees from the preferred bearing.
                int k = (step + 1) / 2;
                float sign = (step & 1) == 1 ? 1f : -1f;
                float angle = step == 0 ? 0f : sign * k * 20f;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * preferredFromTarget;
                Vector3 candidate = center + dir * radius;

                if (engagerDir.sqrMagnitude > 0.5f && Vector3.Angle(dir, engagerDir) < p.holderEngagerLineHalfAngle)
                    continue;

                if (IsAnchorCrowded(agent, ring, candidate, p.holderMinSpacing))
                    continue;

                best = candidate;
                break;
            }

            return best;
        }

        private static bool IsAnchorCrowded(EnemyAiController agent, Ring ring, Vector3 candidate, float minSpacing)
        {
            if (ring == null)
                return false;

            float minSq = minSpacing * minSpacing;
            for (int i = 0; i < ring.Members.Count; i++)
            {
                EnemyAiController other = ring.Members[i];
                if (other == null || other == agent)
                    continue;

                DMEngagementAgentState os = other.EngagementState;
                Vector3 otherPoint = os.HasAnchor && os.Role == DMEngagementRole.Holder ? os.Anchor : other.transform.position;
                Vector3 delta = otherPoint - candidate;
                delta.y = 0f;
                if (delta.sqrMagnitude < minSq)
                    return true;
            }

            return false;
        }

        /// <summary>True when another ring member's anchor/body sits within minSpacing of this holder.</summary>
        public static bool IsHolderOverlapping(EnemyAiController agent, float minSpacing)
        {
            Ring ring = agent != null ? agent.EngagementState.Ring : null;
            if (ring == null)
                return false;

            Vector3 pos = agent.transform.position;
            float minSq = minSpacing * minSpacing;
            for (int i = 0; i < ring.Members.Count; i++)
            {
                EnemyAiController other = ring.Members[i];
                if (other == null || other == agent || other.EngagementState.Role != DMEngagementRole.Holder)
                    continue;

                Vector3 delta = other.transform.position - pos;
                delta.y = 0f;
                if (delta.sqrMagnitude < minSq)
                    return true;
            }

            return false;
        }

        private static Ring GetOrCreateRing(Transform target)
        {
            for (int i = 0; i < Rings.Count; i++)
            {
                if (Rings[i].Target == target)
                    return Rings[i];
            }

            Ring ring = RingPool.Count > 0 ? RingPool.Pop() : new Ring();
            ring.Reset();
            ring.Target = target;
            ring.Focus = target.GetComponent<CombatFocusController>();
            ring.LastTargetPosition = target.position;
            ring.LastSampleTime = Time.time;
            Rings.Add(ring);
            return ring;
        }

        private static void TickIfDue()
        {
            int frame = Time.frameCount;
            if (frame == lastTickFrame)
                return;

            float now = Time.time;
            if (now < nextTickTime)
                return;

            lastTickFrame = frame;
            DM_EnemyEngagementProfile p = Profile;
            nextTickTime = now + 1f / Mathf.Max(1f, p.directorTickHz);

            for (int i = Rings.Count - 1; i >= 0; i--)
            {
                Ring ring = Rings[i];
                TickRing(ring, p, now);
                if (ring.Target == null || ring.Members.Count == 0)
                {
                    for (int m = 0; m < ring.Members.Count; m++)
                    {
                        if (ring.Members[m] != null)
                        {
                            ring.Members[m].EngagementState.Ring = null;
                            ring.Members[m].EngagementState.Role = DMEngagementRole.None;
                        }
                    }

                    ring.Reset();
                    Rings.RemoveAt(i);
                    RingPool.Push(ring);
                }
            }
        }

        private static void TickRing(Ring ring, DM_EnemyEngagementProfile p, float now)
        {
            // Prune members that died, disabled, or switched targets.
            for (int i = ring.Members.Count - 1; i >= 0; i--)
            {
                EnemyAiController m = ring.Members[i];
                if (m == null || !m.isActiveAndEnabled || m.EngagementState.Ring != ring)
                {
                    if (m != null && m.EngagementState.Ring == ring)
                    {
                        m.EngagementState.Ring = null;
                        m.EngagementState.Role = DMEngagementRole.None;
                    }

                    ring.Members.RemoveAt(i);
                    if (ring.Holder == m)
                        ring.Holder = null;
                    if (ring.Challenger == m)
                        ring.Challenger = null;
                }
            }

            if (ring.Target == null || ring.Members.Count == 0)
                return;

            SampleTargetSpeed(ring, now);

            EnemyAiController holder = ring.Holder;
            if (holder != null && !holder.IsTokenEligible(ring.Target, p, asCurrentHolder: true))
            {
                Release(ring, p, now, "ineligible");
                holder = null;
            }

            if (holder == null)
            {
                EnemyAiController first = BestCandidate(ring, p, now, null, respectReaward: true, out _)
                                          ?? BestCandidate(ring, p, now, null, respectReaward: false, out _);
                if (first != null)
                    Award(ring, p, now, first, now - ring.LastHandoffTime < 4f, "free token");
                return;
            }

            DMEngagementAgentState hs = holder.EngagementState;
            bool locked = holder.IsInAttackSequence || now < hs.LastSequenceEndTime + p.postSwingLockSeconds;
            float held = now - ring.HolderSince;

            if (!locked && (hs.SequencesThisToken >= Mathf.Max(1, p.tokenHoldMaxSequences) || held >= p.tokenHoldMaxSeconds))
            {
                EnemyAiController next = BestCandidate(ring, p, now, holder, respectReaward: true, out _);
                if (next != null)
                {
                    Handoff(ring, p, now, next, "max hold");
                    return;
                }
            }

            EnemyAiController challenger = BestCandidate(ring, p, now, holder, respectReaward: true, out float challengerScore);
            if (challenger == null)
            {
                ring.Challenger = null;
                return;
            }

            float holderScore = Score(ring, p, now, holder);
            float challengerHit = challenger.LastHitByTargetTime;
            bool hitImmediate = challengerHit > 0f && now - challengerHit <= p.playerHitMemory &&
                                challengerHit > holder.LastHitByTargetTime;
            bool marginOk = challengerScore >= holderScore + p.switchScoreMargin;

            if (!marginOk && !hitImmediate)
            {
                ring.Challenger = null;
                return;
            }

            if (ring.Challenger != challenger)
            {
                ring.Challenger = challenger;
                ring.ChallengerSince = now;
            }

            bool allowed = !locked && held >= p.tokenMinHoldSeconds && now - ring.LastHandoffTime >= p.minSecondsBetweenHandoffs;
            if (!allowed)
                return;

            if (hitImmediate || now - ring.ChallengerSince >= p.switchConfirmSeconds)
                Handoff(ring, p, now, challenger, hitImmediate ? "player hit holder" : "focus score");
        }

        private static void SampleTargetSpeed(Ring ring, float now)
        {
            float dt = now - ring.LastSampleTime;
            Vector3 pos = ring.Target.position;
            if (ring.LastSampleTime >= 0f && dt > 0.02f)
            {
                Vector3 delta = pos - ring.LastTargetPosition;
                delta.y = 0f;
                float speed = delta.magnitude / dt;
                ring.TargetSpeed = Mathf.Lerp(ring.TargetSpeed, speed, 0.6f);
            }

            ring.LastTargetPosition = pos;
            ring.LastSampleTime = now;
        }

        private static EnemyAiController BestCandidate(Ring ring, DM_EnemyEngagementProfile p, float now,
            EnemyAiController exclude, bool respectReaward, out float bestScore)
        {
            EnemyAiController best = null;
            bestScore = float.NegativeInfinity;
            for (int i = 0; i < ring.Members.Count; i++)
            {
                EnemyAiController m = ring.Members[i];
                if (m == null || m == exclude)
                    continue;

                if (respectReaward && now < m.EngagementState.ReawardBlockedUntil)
                    continue;

                if (!m.IsTokenEligible(ring.Target, p, asCurrentHolder: false))
                    continue;

                float s = Score(ring, p, now, m);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = m;
                }
            }

            return best;
        }

        /// <summary>Focus score (§5.4): player hit / targeting / facing / positioning / wait / punish + brain bias.</summary>
        internal static float Score(Ring ring, DM_EnemyEngagementProfile p, float now, EnemyAiController m)
        {
            Transform target = ring.Target;
            Vector3 targetPos = target.position;
            Vector3 toEnemy = m.transform.position - targetPos;
            toEnemy.y = 0f;
            float distance = toEnemy.magnitude;
            float score = 0f;

            float hitTime = m.LastHitByTargetTime;
            if (hitTime > 0f && p.playerHitMemory > 0f)
                score += p.weightPlayerHit * Mathf.Clamp01(1f - (now - hitTime) / p.playerHitMemory);

            if (ring.Focus != null && ring.Focus.LockedTarget != null && ring.Focus.LockedTarget.gameObject == m.gameObject)
                score += p.weightPlayerTargeting;

            if (distance > 0.01f)
            {
                Vector3 dir = toEnemy / distance;
                float angle = FlatAngle(target.forward, dir);
                Camera cam = PlayerReference.Transform == target ? PlayerReference.Camera : null;
                if (cam != null)
                    angle = Mathf.Min(angle, FlatAngle(cam.transform.forward, dir));

                float full = Mathf.Max(0f, p.facingConeFull);
                float zero = Mathf.Max(full + 0.1f, p.facingConeZero);
                float facing01 = angle <= full ? 1f : Mathf.Clamp01(1f - (angle - full) / (zero - full));
                score += p.weightPlayerFacing * facing01;
            }

            if (HasClearLine(ring, m, targetPos, distance))
                score += p.weightPositioning * Mathf.Clamp01(1f - distance / Mathf.Max(0.5f, p.positioningRange));

            float waited = ring.Holder == m ? 0f : now - m.EngagementState.LastTokenTime;
            score += Mathf.Min(p.weightWaitMax, Mathf.Max(0f, waited) * p.weightWaitPerSecond);

            if (m.HasPunishPriority)
                score += p.weightPunishPriority;

            score += m.BrainTokenBias;
            return score;
        }

        private static float FlatAngle(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            if (a.sqrMagnitude < 0.0001f || b.sqrMagnitude < 0.0001f)
                return 180f;
            return Vector3.Angle(a, b);
        }

        /// <summary>No other ring member stands between the target and this enemy (cheap 2D segment test).</summary>
        private static bool HasClearLine(Ring ring, EnemyAiController m, Vector3 targetPos, float distance)
        {
            if (distance < 0.5f)
                return true;

            Vector3 a = targetPos;
            a.y = 0f;
            Vector3 b = m.transform.position;
            b.y = 0f;
            Vector3 ab = b - a;
            float abLenSq = ab.sqrMagnitude;
            for (int i = 0; i < ring.Members.Count; i++)
            {
                EnemyAiController o = ring.Members[i];
                if (o == null || o == m)
                    continue;

                Vector3 c = o.transform.position;
                c.y = 0f;
                float t = Vector3.Dot(c - a, ab) / abLenSq;
                if (t <= 0.05f || t >= 0.95f)
                    continue;

                Vector3 closest = a + ab * t;
                if ((c - closest).sqrMagnitude < 0.36f)
                    return false;
            }

            return true;
        }

        private static void Award(Ring ring, DM_EnemyEngagementProfile p, float now, EnemyAiController agent, bool withGrace, string reason)
        {
            ring.Holder = agent;
            ring.HolderSince = now;
            ring.LastHandoffTime = now;
            ring.Challenger = null;

            DMEngagementAgentState s = agent.EngagementState;
            s.Role = DMEngagementRole.Engager;
            s.SequencesThisToken = 0;
            s.ClearAnchor();
            s.HandoffGraceUntil = withGrace ? now + p.RandomRange(p.handoffGraceBeforeSwing) : 0f;

            for (int i = 0; i < ring.Members.Count; i++)
            {
                EnemyAiController m = ring.Members[i];
                if (m != null && m != agent && m.EngagementState.Role != DMEngagementRole.Holder)
                    m.EngagementState.Role = DMEngagementRole.Holder;
            }

            agent.OnEngagementTokenAwarded(withGrace);
            Log(p, ring, agent, "awarded (" + reason + ")");
        }

        private static void Release(Ring ring, DM_EnemyEngagementProfile p, float now, string reason)
        {
            EnemyAiController old = ring.Holder;
            ring.Holder = null;
            if (old == null)
                return;

            DMEngagementAgentState s = old.EngagementState;
            s.Role = DMEngagementRole.Holder;
            s.ReawardBlockedUntil = now + p.reawardCooldown;
            s.LastTokenTime = now;
            s.HandoffGraceUntil = 0f;
            s.ClearAnchor();
            Log(p, ring, old, "released (" + reason + ")");
        }

        private static void Handoff(Ring ring, DM_EnemyEngagementProfile p, float now, EnemyAiController next, string reason)
        {
            Release(ring, p, now, reason);
            Award(ring, p, now, next, true, reason);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void Log(DM_EnemyEngagementProfile p, Ring ring, EnemyAiController agent, string message)
        {
            if (p == null || !p.debugLogHandoffs)
                return;

            Debug.Log("[DMEngagement] " + (ring.Target != null ? ring.Target.name : "?") + " token " + message +
                      " -> " + agent.name + " (" + ring.Members.Count + " in ring)", agent);
        }
    }
}
