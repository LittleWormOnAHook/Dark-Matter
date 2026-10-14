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
        private EnemySenses senses;
        private DMEnemyMeleeComboDriver comboDriver;
        private float attackStartTime;
        private float nextAttackTime;
        private float windupEndTime;
        private bool attackPending;
        private bool pendingMeleeRecovery;

        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public bool IsAttacking => attackPending;
        /// <summary>Swing sequencer (light chain / charged swing). Used by the attack interrupt evaluator.</summary>
        public DMEnemyMeleeComboDriver ComboDriver => comboDriver;
        public Transform CurrentTarget => target;

        /// <summary>Raised when the combat target changes (event-driven consumers such as the engaged health HUD).</summary>
        public event System.Action<Transform> TargetChanged;
        public bool IsTargetingPioneer => targetCompanionHealth != null;

        /// <summary>Seconds since the current attack started (-1 when no attack is pending).</summary>
        public float AttackElapsed => attackPending ? Time.time - attackStartTime : -1f;

        private bool pendingInvectorAttack;
        private bool _wasAttackPending;
        private float _meleeRepositionUntil;
        private float _forceMissStepUntil;
        private bool _intentionalMissActive;

        public bool WantsMeleeReposition => Time.time < _meleeRepositionUntil;
        public bool WantsForcedMissStep => Time.time < _forceMissStepUntil;
        public bool IsIntentionalMissActive => _intentionalMissActive;

        /// <summary>Seconds until the attack cooldown / recovery lockout ends (0 = ready). Cooldown movement reads it.</summary>
        public float AttackReadyIn => Mathf.Max(0f, nextAttackTime - Time.time);

        /// <summary>
        /// Clears in-flight swings and pushes the next attack attempt until stagger ends.
        /// </summary>
        public void InterruptAttackForStagger(float lockoutSeconds)
        {
            comboDriver?.CancelSequence();
            attackPending = false;
            pendingInvectorAttack = false;
            pendingMeleeRecovery = false;
            _intentionalMissActive = false;
            _forceMissStepUntil = 0f;
            if (lockoutSeconds > 0f)
                nextAttackTime = Mathf.Max(nextAttackTime, Time.time + lockoutSeconds);
        }

        private void Awake()
        {
            aiController = GetComponent<EnemyAiController>();
            invectorCombat = GetComponent<EnemyInvectorCombatBridge>();
            senses = GetComponent<EnemySenses>();
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

            bool changed = newTarget != target;
            target = newTarget;
            targetStats = newTarget != null ? newTarget.GetComponentInParent<SurvivalStats>() : null;
            targetCompanionHealth = newTarget != null ? newTarget.GetComponentInParent<CompanionHealth>() : null;
            if (changed)
                TargetChanged?.Invoke(newTarget);
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

        private bool HasClearLosForRangedFire(Transform threat)
        {
            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            if (profile == null || !profile.requireClearLosForRangedFire)
                return true;

            if (threat == null)
                return false;

            if (senses == null)
                senses = GetComponent<EnemySenses>();

            return senses != null && senses.CanSeeThreat(threat);
        }

        private float _gateLogNext;
        /// <summary>2026-10-13 (e2): debug, DM_EnemyBrainProfile.logRangedFireGates. Once per second per enemy, logs which TryAttack gate stopped a ranged enemy.</summary>
        private void RangedGateLog(string gate)
        {
            DM_EnemyBrainProfile p = DM_EnemyBrainProfile.Live;
            if (p == null || !p.logRangedFireGates || invectorCombat == null || !invectorCombat.HasRangedWeaponEquipped() || Time.time < _gateLogNext)
                return;
            _gateLogNext = Time.time + 1f;
            float d = target != null ? HorizontalDistance(transform.position, target.position) : -1f;
            Debug.Log($"[DMRangedGate] {name} blocked by '{gate}' | state {(aiController != null ? aiController.CurrentStateName : "?")} | dist {d:0.0} | engage {invectorCombat.RangedEngageRange:0.0} | rangedPref {invectorCombat.IsArmedRangedPreferred()} | aimReady {invectorCombat.IsAimReady} | cooldown {Mathf.Max(0f, nextAttackTime - Time.time):0.00}");
        }

        public void TryAttack()
        {
            if (!HasLivingTarget())
            { RangedGateLog("!HasLivingTarget()"); return; }

            EnemyInvectorRagdollBridge ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            if (ragdollBridge != null && ragdollBridge.IsHitStaggerActive)
            { RangedGateLog("ragdollBridge != null && ragdollBridge.IsHitStaggerActive"); return; }

            if (!IsTargetInEffectiveRange())

            { RangedGateLog("!IsTargetInEffectiveRange()"); return; }

            if (aiController != null && !aiController.AllowsCombatTarget(target))
            {
                attackPending = false;
                pendingMeleeRecovery = false;
                return;
            }

            if (Time.time < nextAttackTime)

            { RangedGateLog("Time.time < nextAttackTime"); return; }

            // Swing start distance: melee swings start only inside the weapon's real reach (the engager closes in first).
            if (aiController != null && (invectorCombat == null || !invectorCombat.HasRangedWeaponEquipped()) && !aiController.IsInSwingStartReach(target))
            { RangedGateLog("aiController != null && (invectorCombat == null || !invectorCombat.Has"); return; }

            if (comboDriver != null && comboDriver.IsSequenceActive)

            { RangedGateLog("comboDriver != null && comboDriver.IsSequenceActive"); return; }

            // Phase 3 spacing: only the melee token holder swings, after its hand-off grace, inside the facing cone.
            if (aiController != null && !aiController.CanEngagementAttack(target))
            { RangedGateLog("aiController != null && !aiController.CanEngagementAttack(target)"); return; }

            // One attacker at a time across every enemy type (ranged / android / robot included), also while another swing is still finishing.
            if (aiController != null && !aiController.CanStartAttackGlobal(target))
            { RangedGateLog("aiController != null && !aiController.CanStartAttackGlobal(target)"); return; }

            if (invectorCombat != null &&
                invectorCombat.IsArmedRangedPreferred() &&
                invectorCombat.HasRangedWeaponEquipped() &&
                !HasClearLosForRangedFire(target))
            { RangedGateLog("!HasClearLosForRangedFire"); return; }

            if (invectorCombat != null &&
                invectorCombat.IsArmedRangedPreferred() &&
                invectorCombat.HasRangedWeaponEquipped() &&
                !invectorCombat.HasRangedAmmoToFire())
            { RangedGateLog("!HasRangedAmmoToFire"); return; }

            if (IsMeleeAttackPath() && BlocksMeleeVsClimbingPlayer(target))
            { RangedGateLog("BlocksMeleeVsClimbingPlayer"); return; }

            // Ranged aim gate: draw the gun / hold the aim pose first. Never consumes the cooldown and
            // never falls through to the scripted melee fallback below.
            if (invectorCombat != null && invectorCombat.TryDeferRangedAttack(target))
            { RangedGateLog("invectorCombat != null && invectorCombat.TryDeferRangedAttack(target)"); return; }

            bool rollIntentionalMiss = ShouldRollIntentionalMeleeMiss();

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
                Project.Combat.DM_CombatCoreProfile holdProfile = Project.Combat.DM_CombatCoreProfile.Live; // 2026-10-12: shared max charged hold
                float maxHold = holdProfile != null ? holdProfile.chargedMaxHoldSeconds : 1f;
                windupEndTime = Time.time + (maxHold > 0f ? Mathf.Min(chargedHoldSeconds, maxHold) : chargedHoldSeconds);
                aiController?.NotifyEngagementAttackBegan();
                return;
            }

            if (invectorCombat != null && invectorCombat.TryBeginAttack(target, out float invectorDuration))
            {
                if (!invectorCombat.LastAttackWasRanged)
                {
                    _intentionalMissActive = rollIntentionalMiss;
                    if (comboDriver != null)
                        comboDriver.BeginLightSequence();
                }
                else
                {
                    _intentionalMissActive = false;
                }

                attackPending = true;
                pendingInvectorAttack = true;
                attackStartTime = Time.time;
                windupEndTime = Time.time + invectorDuration;
                pendingMeleeRecovery = !invectorCombat.LastAttackWasRanged;
                aiController?.NotifyEngagementAttackBegan();
                return;
            }

            pendingInvectorAttack = false;
            pendingMeleeRecovery = true;
            _intentionalMissActive = rollIntentionalMiss;
            attackPending = true;
            attackStartTime = Time.time;
            windupEndTime = Time.time + attackWindup;
            aiController?.NotifyEngagementAttackBegan();
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
            DM_EnemyEngagementProfile p = aiController != null ? aiController.EngagementProfile : DM_EnemyEngagementProfile.Live;
            float min = Mathf.Max(0f, p != null ? p.meleeRecoveryPauseMin : meleeRecoveryPauseMin);
            float max = Mathf.Max(min, p != null ? p.meleeRecoveryPauseMax : meleeRecoveryPauseMax);
            return Random.Range(min, max) * (aiController != null ? DMEnemyEngagementDirector.PaceFor(aiController) : 1f);
        }

        private void Update()
        {
            if (attackPending && IsMeleeAttackPath() && BlocksMeleeVsClimbingPlayer(target))
            {
                comboDriver?.CancelSequence();
                InterruptAttackForStagger(0.12f);
            }

            if (_wasAttackPending && !attackPending)
            {
                if (_intentionalMissActive)
                {
                    BeginMissRecoveryStep();
                    _intentionalMissActive = false;
                }
                else if (HasLivingTarget() && target != null)
                {
                    float connectRange = ResolveEffectiveAttackRange(target) * 0.88f;
                    if (HorizontalDistance(transform.position, target.position) > connectRange)
                        BeginMissRecoveryStep();
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

            if (_intentionalMissActive)
            {
                _intentionalMissActive = false;
                BeginMissRecoveryStep();
                return;
            }

            if (IsMeleeAttackPath() && BlocksMeleeVsClimbingPlayer(target))
                return;

            ApplyDamageToTarget(attackDamage);
        }

        private bool IsMeleeAttackPath()
        {
            if (invectorCombat == null)
                return true;

            if (invectorCombat.IsArmedRangedPreferred() && invectorCombat.HasRangedWeaponEquipped())
                return false;

            return true;
        }

        private bool BlocksMeleeVsClimbingPlayer(Transform threat)
        {
            return DMPlayerClimbCombatGate.BlocksMeleeAgainstTarget(threat, DM_EnemyBrainProfile.Live);
        }

        private bool ShouldRollIntentionalMeleeMiss()
        {
            if (!IsTargetInRange())
                return false;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null || profile.enemyIntentionalMeleeMissChance <= 0.001f)
                return false;

            return Random.value < profile.enemyIntentionalMeleeMissChance;
        }

        private void BeginMissRecoveryStep()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float duration = profile != null ? profile.enemyMeleeMissStepSeconds : 0.55f;
            if (duration <= 0f)
                return;

            _meleeRepositionUntil = Time.time + duration;
            _forceMissStepUntil = Time.time + duration;
            aiController?.NotifyIntentionalMeleeMiss(duration);
        }

        internal float ResolveMissStepStopDistance(Transform stepTarget)
        {
            if (stepTarget == null)
                return 0f;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float factor = profile != null ? profile.enemyMeleeMissStepStopFactor : 0.72f;
            return ResolveEffectiveAttackRange(stepTarget) * factor;
        }

        internal float ResolveMissStepSpeedMultiplier()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            return profile != null ? profile.enemyMeleeMissStepSpeedMultiplier : 0.95f;
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

            // Real weapon reach: never start a melee swing from further than the equipped weapon (or fists) can touch.
            DM_EnemyEngagementProfile ep = aiController != null ? aiController.EngagementProfile : null;
            if (ep != null && ep.capAttackRangeToWeaponReach && invectorCombat != null && !invectorCombat.HasRangedWeaponEquipped()
                && invectorCombat.TryGetKnownTipReach(ep, out float tipReach))
                range = Mathf.Min(range, Mathf.Max(0.9f, tipReach + ep.attackRangeReachSlack));
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
                stats.ApplyDamageFromSource(damage, gameObject);
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
