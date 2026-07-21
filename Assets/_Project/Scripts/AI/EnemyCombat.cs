using Project.AI.Invector;
using Project.Companions;
using Project.Combat;
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
        [Tooltip("Extra reach tolerance when the target is a pioneer (they shuffle during windup).")]
        [SerializeField] private float pioneerRangeGraceMultiplier = 1.3f;

        private Transform target;
        private SurvivalStats targetStats;
        private CompanionHealth targetCompanionHealth;
        private EnemyAiController aiController;
        private EnemyInvectorCombatBridge invectorCombat;
        private float nextAttackTime;
        private float windupEndTime;
        private bool attackPending;

        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public bool IsAttacking => attackPending;
        public Transform CurrentTarget => target;
        public bool IsTargetingPioneer => targetCompanionHealth != null;

        private bool pendingInvectorAttack;

        private void Awake()
        {
            aiController = GetComponent<EnemyAiController>();
            invectorCombat = GetComponent<EnemyInvectorCombatBridge>();
        }

        public void SetTarget(Transform newTarget)
        {
            newTarget = ResolveCombatTargetRoot(newTarget);

            if (newTarget != null && aiController != null && !aiController.AllowsCombatTarget(newTarget))
                newTarget = null;

            if (newTarget != target && attackPending)
            {
                attackPending = false;
                pendingInvectorAttack = false;
            }

            target = newTarget;
            targetStats = newTarget != null ? newTarget.GetComponentInParent<SurvivalStats>() : null;
            targetCompanionHealth = newTarget != null ? newTarget.GetComponentInParent<CompanionHealth>() : null;
        }

        public bool HasLivingTarget()
        {
            if (target == null)
                return false;

            if (targetStats != null)
                return !targetStats.IsDead;

            if (targetCompanionHealth != null)
                return !targetCompanionHealth.IsDead;

            return false;
        }

        public bool IsTargetInRange()
        {
            return IsTargetWithin(ResolveEffectiveAttackRange());
        }

        public bool IsTargetInEffectiveRange()
        {
            if (target == null)
                return false;

            if (IsTargetInRange())
                return true;

            return invectorCombat != null
                && invectorCombat.IsArmedRangedPreferred()
                && HorizontalDistance(transform.position, target.position) <= invectorCombat.RangedEngageRange;
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

            if (!IsTargetInEffectiveRange())
                return;

            if (aiController != null && !aiController.AllowsCombatTarget(target))
            {
                attackPending = false;
                return;
            }

            if (Time.time < nextAttackTime)
                return;

            nextAttackTime = Time.time + attackCooldown;

            if (invectorCombat != null && invectorCombat.TryBeginAttack(target, out float invectorDuration))
            {
                attackPending = true;
                pendingInvectorAttack = true;
                windupEndTime = Time.time + invectorDuration;
                return;
            }

            pendingInvectorAttack = false;
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

            if (pendingInvectorAttack && invectorCombat != null && invectorCombat.UsesInvectorDamageApplication)
            {
                pendingInvectorAttack = false;
                return;
            }

            pendingInvectorAttack = false;

            if (!HasLivingTarget())
                return;

            float releaseRange = ResolveEffectiveAttackRange() * 1.15f;
            if (!IsTargetWithin(releaseRange))
                return;

            if (aiController != null && !aiController.AllowsCombatTarget(target))
                return;

            ApplyDamageToTarget(attackDamage);
        }

        public bool IsInAttackRange(Transform candidate)
        {
            if (candidate == null)
                return false;

            candidate = ResolveCombatTargetRoot(candidate);
            return HorizontalDistance(transform.position, candidate.position) <= ResolveEffectiveAttackRange(candidate);
        }

        public float ResolveEffectiveAttackRange(Transform candidate)
        {
            if (candidate == null)
                return attackRange;

            return candidate.GetComponentInParent<CompanionHealth>() != null
                ? attackRange * pioneerRangeGraceMultiplier
                : attackRange;
        }

        private float ResolveEffectiveAttackRange()
        {
            return ResolveEffectiveAttackRange(target);
        }

        private void ApplyDamageToTarget(float damage)
        {
            Transform hitTarget = target;
            if (hitTarget == null)
                return;

            string attackerName = name;
            string victimName = hitTarget.name;
            SurvivalStats stats = hitTarget.GetComponentInParent<SurvivalStats>();
            CompanionHealth companionHealth = hitTarget.GetComponentInParent<CompanionHealth>();

            if (stats != null)
            {
                if (stats.IsDead || stats.HasEnemyCombatImmunity)
                    return;

                float healthBefore = stats.CurrentHealth;
                stats.ApplyDamage(damage, attackerName);
                CombatHitVfx.SpawnIncomingEnemyHit(transform, stats.transform, damage);
#if UNITY_EDITOR
                float healthAfter = stats.CurrentHealth;
                Debug.Log(
                    $"[EnemyDamage] {attackerName} hit Player for {damage:0.#} " +
                    $"(health {healthBefore:0.#} → {healthAfter:0.#})");
#endif
                return;
            }

            if (companionHealth == null || companionHealth.IsDead)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    $"[EnemyDamage] {attackerName} swing at '{victimName}' had no CompanionHealth receiver.");
#endif
                return;
            }

            float pioneerHealthBefore = companionHealth.CurrentHealth;
            companionHealth.ApplyDamage(damage);
            CombatHitVfx.SpawnIncomingEnemyHit(transform, companionHealth.transform, damage);
#if UNITY_EDITOR
            float pioneerHealthAfter = companionHealth.CurrentHealth;
            Debug.Log(
                $"[EnemyDamage] {attackerName} hit Pioneer '{victimName}' for {damage:0.#} " +
                $"(health {pioneerHealthBefore:0.#} → {pioneerHealthAfter:0.#})");
#endif
        }

        private static Transform ResolveCombatTargetRoot(Transform candidate)
        {
            if (candidate == null)
                return null;

            CompanionHealth companionHealth = candidate.GetComponentInParent<CompanionHealth>();
            if (companionHealth != null)
                return companionHealth.transform;

            SurvivalStats stats = candidate.GetComponentInParent<SurvivalStats>();
            if (stats != null)
                return stats.transform;

            return candidate;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
