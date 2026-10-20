using Invector;
using Project.AI;
using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Grenade AOE: severe hit reaction + locomotion/attack lockout after Pioneer splash damage.
    /// Reuses <see cref="EnemyInvectorRagdollBridge.TryGuardBreakStagger"/> (parry-tier full-body stagger).
    /// </summary>
    public static class DMEnemyGrenadeBlastReaction
    {
        public static void TryApplyAfterDamage(
            EnemyHealth enemy,
            Collider hitCollider,
            Vector3 blastOrigin,
            float damage,
            GameObject damageSource)
        {
            if (enemy == null || enemy.IsDead)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float staggerSeconds = profile != null ? profile.grenadeEnemyStaggerSeconds : 5f;
            if (staggerSeconds <= 0f)
                return;

            EnemyInvectorRagdollBridge bridge = enemy.GetComponent<EnemyInvectorRagdollBridge>();
            if (bridge == null)
                return;

            EnemyAiController rollAi = enemy.GetComponent<EnemyAiController>();
            if (rollAi != null && rollAi.DodgeRollBlocksHitReactions)
                return;

            Vector3 away = enemy.transform.position - blastOrigin;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.up;
            away.Normalize();

            Vector3 hitPoint = hitCollider != null ? hitCollider.bounds.center : enemy.transform.position;
            Transform sender = damageSource != null ? damageSource.transform : null;
            var sourceDamage = new vDamage(Mathf.Max(1, Mathf.RoundToInt(damage)))
            {
                sender = sender,
                hitPosition = hitPoint,
                hitReaction = true,
                force = away * 0.28f
            };

            // Parry-tier full-body reaction + hitstop; duration from grenadeEnemyStaggerSeconds (default 5s).
            bridge.TryGuardBreakStagger(sourceDamage, staggerSeconds, isParry: true);

            if (profile == null || !profile.enableHitReactions)
                return;

            Vector3 flinchFrom = blastOrigin;
            flinchFrom.y = hitPoint.y;
            bridge.PlayDirectionalFlinch(
                flinchFrom,
                profile.chainStumbleLeanDegrees,
                profile.heavyWaistBendSeconds,
                twistDegrees: float.NaN);
            DMEnemyHitSlide.Begin(
                enemy.gameObject,
                away,
                profile.chainStumbleBackstepMeters * 1.15f,
                profile.chainStumbleSeconds * 0.85f,
                2,
                profile.hitSlideMaxLedgeDrop,
                profile.hitSlideMaxTotalMeters);
        }
    }
}
