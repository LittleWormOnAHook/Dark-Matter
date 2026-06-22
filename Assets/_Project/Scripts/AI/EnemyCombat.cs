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
        private float nextAttackTime;
        private float windupEndTime;
        private bool attackPending;

        public float AttackRange => attackRange;
        public bool IsAttacking => attackPending;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetStats = newTarget != null ? newTarget.GetComponent<SurvivalStats>() : null;
        }

        public bool IsTargetInRange()
        {
            if (target == null)
                return false;

            return HorizontalDistance(transform.position, target.position) <= attackRange;
        }

        public void TryAttack()
        {
            if (target == null || targetStats == null || targetStats.IsDead)
                return;

            if (!IsTargetInRange())
                return;

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

            if (target == null || targetStats == null || targetStats.IsDead)
                return;

            if (!IsTargetInRange())
                return;

            targetStats.ApplyDamage(attackDamage);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
