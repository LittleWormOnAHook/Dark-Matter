using Project.Companions;
using Project.Player;
using Project.Survival;
using UnityEngine;

namespace Project.AI
{
    public class EnemyCombat : MonoBehaviour
    {
        [Header("Melee")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private float attackWindup = 0.35f;

        private Transform target;
        private SurvivalStats targetStats;
        private CompanionHealth targetCompanionHealth;
        private EnemyAiController aiController;
        private float nextAttackTime;
        private float windupEndTime;
        private bool attackPending;

        public float AttackRange => attackRange;
        public bool IsAttacking => attackPending;
        public Transform CurrentTarget => target;

        private void Awake()
        {
            aiController = GetComponent<EnemyAiController>();
        }

        public void SetTarget(Transform newTarget)
        {
            if (newTarget != null && aiController != null && !aiController.AllowsCombatTarget(newTarget))
                newTarget = null;
            if (newTarget != target && attackPending)
                attackPending = false;

            target = newTarget;
            targetStats = newTarget != null ? newTarget.GetComponent<SurvivalStats>() : null;
            targetCompanionHealth = newTarget != null ? newTarget.GetComponent<CompanionHealth>() : null;
        }

        public bool HasLivingTarget()
        {
            if (target == null)
                return false;

            if (targetStats != null)
                return !targetStats.IsDead;

            if (targetCompanionHealth != null)
                return !targetCompanionHealth.IsDead;

            return true;
        }

        public bool IsTargetInRange()
        {
            return IsTargetWithin(attackRange);
        }

        private bool IsTargetWithin(float range)
        {
            if (target == null)
                return false;

            return HorizontalDistance(transform.position, target.position) <= range;
        }

        public void TryAttack()
        {
            if (!HasLivingTarget())
                return;

            if (!IsTargetInRange())
                return;

            // Re-check legality at swing start so a bystander player never gets a pending hit
            // after aggro flips to a pioneer mid-fight.
            if (aiController != null && !aiController.AllowsCombatTarget(target))
            {
                attackPending = false;
                return;
            }

            if (Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + attackCooldown;
            attackPending = true;
            windupEndTime = Time.time + attackWindup;
        }

        private void Update()
        {
            if (!attackPending)
                return;

            if (Time.time < windupEndTime)
                return;

            attackPending = false;

            if (!HasLivingTarget())
                return;

            // Grace window on the post-windup range check: companions shuffle/step during
            // the windup, and a strict re-check made nearly every enemy hit whiff.
            if (!IsTargetWithin(attackRange * 1.5f))
                return;

            // Final gate: aggro may have flipped to a pioneer during windup.
            if (aiController != null && !aiController.AllowsCombatTarget(target))
                return;

            ApplyDamageToTarget(attackDamage);
        }

        public bool IsInAttackRange(Transform candidate)
        {
            if (candidate == null)
                return false;

            return HorizontalDistance(transform.position, candidate.position) <= attackRange;
        }

        private void ApplyDamageToTarget(float damage)
        {
            // Snapshot everything before ApplyDamage. PlayerDied listeners call SetTarget(null)
            // on this same enemy, which nulls target/targetStats and used to NRE the log.
            Transform hitTarget = target;
            if (hitTarget == null)
                return;

            string attackerName = name;
            string victimName = hitTarget.name;
            SurvivalStats stats = hitTarget.GetComponent<SurvivalStats>();
            CompanionHealth companionHealth = hitTarget.GetComponent<CompanionHealth>();

            if (stats != null)
            {
                if (stats.IsDead || stats.HasEnemyCombatImmunity)
                    return;

                float healthBefore = stats.CurrentHealth;
                stats.ApplyDamage(damage, attackerName);
                float healthAfter = stats != null ? stats.CurrentHealth : 0f;
                Debug.Log(
                    $"[EnemyDamage] {attackerName} hit Player for {damage:0.#} " +
                    $"(health {healthBefore:0.#} → {healthAfter:0.#})");
                return;
            }

            if (companionHealth == null || companionHealth.IsDead)
                return;

            float pioneerHealthBefore = companionHealth.CurrentHealth;
            companionHealth.ApplyDamage(damage);
            float pioneerHealthAfter = companionHealth != null ? companionHealth.CurrentHealth : 0f;
            Debug.Log(
                $"[EnemyDamage] {attackerName} hit Pioneer '{victimName}' for {damage:0.#} " +
                $"(health {pioneerHealthBefore:0.#} → {pioneerHealthAfter:0.#})");
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
