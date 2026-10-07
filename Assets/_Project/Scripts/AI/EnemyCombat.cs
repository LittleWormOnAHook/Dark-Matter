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
        [SerializeField] private float attackRange = 2.35f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float attackCooldown = 1.4f;
        [Tooltip("Extra wait after a melee swing ends so attacks are not a constant loop. Randomized per swing. Standing in range still starts the next swing on its own.")]
        [SerializeField] private float meleeRecoveryPauseMin = 0.6f;
        [SerializeField] private float meleeRecoveryPauseMax = 1.8f;
        [SerializeField] private float attackWindup = 0.35f;
        [Tooltip("Extra reach tolerance when the target is a pioneer (they shuffle during windup).")]
        [SerializeField] private float pioneerRangeGraceMultiplier = 1.3f;

        private Transform target;
        private SurvivalStats targetStats;
        private CompanionHealth targetCompanionHealth;
        private EnemyAiController aiController;
        private EnemyInvectorCombatBridge invectorCombat;
        private DMEnemyMeleeComboDriver comboDriver;
        private float attackStartTime;
        private float nextAttackTime;
        private float windupEndTime;
        private bool attackPending;
        private bool pendingMeleeRecovery;

        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public bool IsAttacking => attackPending;
        public Transform CurrentTarget => target;
        public bool IsTargetingPioneer => targetCompanionHealth != null;

        private bool pendingInvectorAttack;
        private bool _wasAttackPending;
        private float _meleeRepositionUntil;

        public bool WantsMeleeReposition => Time.time < _meleeRepositionUntil;

        /// <summary>
        /// Clears in-flight swings and pushes the next attack attempt until stagger ends.
        /// </summary>
        public void InterruptAttackForStagger(float lockoutSeconds)
        {
            comboDriver?.CancelSequence();
            attackPending = false;
            pendingInvectorAttack = false;
            pendingMeleeRecovery = false;
            if (lockoutSeconds > 0f)
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + lockoutSeconds);
        }

        private void Awake()
        {
            aiController = GetComponent<EnemyAiController>();
            invectorCombat = GetComponent<EnemyInvectorCombatBridge>();
            comboDriver = GetComponent<DMEnemyMeleeComboDriver>();
            if (comboDriver == null && invectorCombat != null)
                comboDriver = gameObject.AddComponent<DMEnemyMeleeComboDriver>();
        }

        /// <summary>
        /// Combo driver callback: the swing chain (or charged swing) left its attack states,
        /// so the pending attack can end and the recovery pause can start.
        /// </summary>
        public void NotifyAttackSequenceEnded()
        {
            if (attackPending && pendingInvectorAttack)
                windupEndTime = Mathf.Min(windupEndTime, Time.time);
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
                pendingMeleeRecovery = false;
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

            EnemyInvectorRagdollBridge ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            if (ragdollBridge != null && ragdollBridge.IsHitStaggerActive)
                return;

            if (!IsTargetInEffectiveRange())
                return;

            if (aiController != null && !aiController.AllowsCombatTarget(target))
            {
                attackPending = false;
                pendingMeleeRecovery = false;
                return;
            }

            if (Time.time < nextAttackTime)
                return;

            if (comboDriver != null && comboDriver.IsSequenceActive)
                return;

            nextAttackTime = Time.time + attackCooldown;

            // Charged swing (random roll after cooldown, or forced by the punish rule). Melee range only.
            if (comboDriver != null && invectorCombat != null && IsTargetInRange() &&
                !invectorCombat.HasRangedWeaponEquipped() &&
                comboDriver.TryBeginChargedAttack(out float chargedHoldSeconds))
            {
                attackPending = true;
                pendingInvectorAttack = true;
                pendingMeleeRecovery = true;
                attackStartTime = Time.time;
                windupEndTime = Time.time + chargedHoldSeconds;
                return;
            }

            if (invectorCombat != null && invectorCombat.TryBeginAttack(target, out float invectorDuration))
            {
                if (!invectorCombat.LastAttackWasRanged && comboDriver != null)
                    comboDriver.BeginLightSequence();

                attackPending = true;
                pendingInvectorAttack = true;
                attackStartTime = Time.time;
                windupEndTime = Time.time + invectorDuration;
                pendingMeleeRecovery = !invectorCombat.LastAttackWasRanged;
                return;
            }

            pendingInvectorAttack = false;
            pendingMeleeRecovery = true;
            attackPending = true;
            windupEndTime = Time.time + attackWindup;
        }

        /// <summary>
        /// After the swing's active window, wait a random extra beat before the next melee.
        /// The existing attackCooldown still starts when the swing begins; this extends the gap once it ends.
        /// </summary>
        private void ApplyMeleeRecoveryPause()
        {
            if (!pendingMeleeRecovery)
                return;

            pendingMeleeRecovery = false;
            float extra = SampleMeleeRecoveryPause();
            if (extra > 0f)
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + extra);
        }

        private float SampleMeleeRecoveryPause()
        {
            float min = Mathf.Max(0f, meleeRecoveryPauseMin);
            float max = Mathf.Max(min, meleeRecoveryPauseMax);
            return Random.Range(min, max);
        }

        private void Update()
        {
            if (_wasAttackPending && !attackPending)
            {
                if (HasLivingTarget() && target != null)
                {
                    float connectRange = ResolveEffectiveAttackRange(target) * 0.82f;
                    if (HorizontalDistance(transform.position, target.position) > connectRange)
                        _meleeRepositionUntil = Time.time + 0.65f;
                }
            }

            _wasAttackPending = attackPending;

            if (!attackPending)
                return;

            if (Time.time < windupEndTime)
                return;

            // Keep the attack pending while a combo chain or charged swing is still playing.
            if (pendingInvectorAttack && comboDriver != null &&
                comboDriver.HoldsAttackPending(Time.time - attackStartTime))
                return;

            attackPending = false;
            ApplyMeleeRecoveryPause();

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
                return ResolveBaseAttackRange();

            float range = ResolveBaseAttackRange();
            return candidate.GetComponentInParent<CompanionHealth>() != null
                ? range * pioneerRangeGraceMultiplier
                : range;
        }

        private float ResolveBaseAttackRange()
        {
            float range = attackRange;
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile != null && profile.enemyMeleeAttackRangeMultiplier > 0f)
                range *= profile.enemyMeleeAttackRangeMultiplier;
            return range;
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
