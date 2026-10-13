using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Swing start distance: a melee swing starts only when the target is inside the equipped weapon's real reach
    /// (DM_EnemyEngagementProfile "Swing start distance"), not at the generic attack range. The engager closes in first.
    /// </summary>
    public partial class EnemyAiController
    {
        private Vector3 swingTargetLastPos;
        private float swingTargetLastTime = -1f;
        private float swingTargetOutSpeed;

        /// <summary>Tracks how fast the target moves away from this enemy (smoothed, m/s). Called every frame by the engager.</summary>
        private void SampleSwingTarget(Transform target)
        {
            float now = Time.time;
            if (target == null)
            {
                swingTargetLastTime = -1f;
                swingTargetOutSpeed = 0f;
                return;
            }

            if (swingTargetLastTime >= 0f && now > swingTargetLastTime && now - swingTargetLastTime < 0.25f)
            {
                Vector3 v = (target.position - swingTargetLastPos) / (now - swingTargetLastTime);
                v.y = 0f;
                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                float outward = dir.sqrMagnitude > 0.0001f ? Vector3.Dot(v, dir.normalized) : 0f;
                swingTargetOutSpeed = Mathf.Lerp(swingTargetOutSpeed, outward, 0.35f);
            }
            else
            {
                swingTargetOutSpeed = 0f;
            }

            swingTargetLastPos = target.position;
            swingTargetLastTime = now;
        }

        /// <summary>Max root-to-target distance at which this enemy may start a melee swing (never above the generic effective range).</summary>
        public float ResolveSwingStartDistance(Transform target)
        {
            float effective = combat != null ? combat.ResolveEffectiveAttackRange(target) : 2f;
            DM_EnemyEngagementProfile p = EngagementProfile;
            // Only the engagement engager closes in before swinging; legacy / non-engagement attackers keep the generic range (they have no close-in step).
            // 2026-10-12: swingGateAllAttackers applies the weapon-reach gate to every melee attacker (the Robot swung from 1.44 m with 0.96 m reach as a non-engager).
            if (p == null || !p.swingStartUseWeaponReach || combatBridge == null)
                return effective;
            if (!p.swingGateAllAttackers && (!engagementActiveThisFrame || engagementState.Role != DMEngagementRole.Engager))
                return effective;

            float tip = combatBridge.WeaponTipReach(p);
            // Cut depth: approach so the blade passes at least swingCutDepthFraction of the target's body radius inside it.
            float targetRadius = 0.3f;
            if (target != null) { CapsuleCollider tc = target.GetComponent<CapsuleCollider>(); if (tc != null) targetRadius = tc.radius; }
            // 2026-10-12: tip reaches the body surface (d - r) plus cut*r: d = tip + r*(1-cut). Old: tip - max(innerMargin, cut*r) (put the hilt in the body).
            float inner = Mathf.Clamp01(p.swingCutDepthFraction) * targetRadius;
            float gate = tip + targetRadius - inner - Mathf.Max(0f, swingTargetOutSpeed) * p.swingStartLeadSeconds;
            float floor = ResolveMinCombatSeparation(target) + 0.15f;
            return Mathf.Min(effective, Mathf.Max(gate, floor));
        }

        /// <summary>EnemyCombat.TryAttack gate for melee swings.</summary>
        public bool IsInSwingStartReach(Transform target)
        {
            if (target == null)
                return true;

            return HorizontalDistance(transform.position, target.position) <= ResolveSwingStartDistance(target) + 0.001f;
        }
    }
}