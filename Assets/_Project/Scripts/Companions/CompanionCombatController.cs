using Project.AI;
using Project.Core;
using Project.Data;
using Project.Player;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Attacks the engaged enemy with seed-driven timing, reduced damage, and combat step-back.
    /// </summary>
    public class CompanionCombatController : MonoBehaviour
    {
        private const float DamageMultiplier = 0.25f;

        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float proximityAggroRange = 3.75f;
        [SerializeField] private float faceTurnSpeed = 10f;
        [SerializeField] private float stepBackDistance = 0.85f;
        [SerializeField] private float stepBackDuration = 0.55f;
        [SerializeField] private float attackWindupDelay = 0.18f;

        private CompanionAnimationDriver animationDriver;
        private CompanionEquipmentVisual equipmentVisual;
        private CompanionFollowController followController;
        private CompanionThreatSensor threatSensor;
        private CombatFocusController playerFocus;
        private float nextAttackTime;
        private float pendingAttackReleaseTime;
        private EnemyHealth currentTarget;
        private string pioneerSeed = string.Empty;
        private float personalAttackBias = 0.72f;
        private float personalIntervalMultiplier = 1f;
        private bool attackPending;
        private bool wasEngaged;
        private PioneerBehaviorProfile behaviorProfile = new PioneerBehaviorProfile();
        private SkilledPioneerClass pioneerClass = SkilledPioneerClass.CombatTactician;
        private float preferredCombatDistance = 2.4f;
        private float selfTargetPriority = 0.35f;

        public string PioneerSeed => pioneerSeed;
        public EnemyHealth CurrentTarget => currentTarget;

        private void Awake()
        {
            animationDriver = GetComponent<CompanionAnimationDriver>();
            equipmentVisual = GetComponent<CompanionEquipmentVisual>();
            followController = GetComponent<CompanionFollowController>();
            threatSensor = GetComponent<CompanionThreatSensor>();
        }

        private void OnEnable()
        {
            CompanionCombatCoordinator.EnsureExists(this)?.Register(this);
            ResolvePlayerFocus();
        }

        private void OnDisable()
        {
            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator != null)
            {
                coordinator.EndAttack(this);
                if (wasEngaged)
                    coordinator.NotifyEngagementChanged(false);
            }
        }

        public void Initialize(string pioneerId)
        {
            pioneerSeed = string.IsNullOrEmpty(pioneerId) ? name : pioneerId;
            int hash = pioneerSeed.GetHashCode();
            personalAttackBias = 0.55f + (Mathf.Abs(hash) % 1000) / 1000f * 0.35f;
            personalIntervalMultiplier = 0.85f + (Mathf.Abs(hash >> 8) % 1000) / 1000f * 0.3f;
        }

        public void ApplyBehaviorProfile(PioneerBehaviorProfile profile, SkilledPioneerClass skilledClass)
        {
            pioneerClass = skilledClass;
            behaviorProfile = profile != null ? profile.Clone() : new PioneerBehaviorProfile();
            preferredCombatDistance = behaviorProfile.ResolvePreferredCombatDistance(pioneerClass);
            selfTargetPriority = behaviorProfile.followMode == PioneerFollowMode.FollowSelf ? 0.68f : 0.35f;
            attackRange = Mathf.Max(attackRange, preferredCombatDistance * 0.82f);
        }

        public float GetPersonalAttackBias() => personalAttackBias;

        public float GetPersonalIntervalMultiplier() => personalIntervalMultiplier;

        public void RefreshLoadoutWeapon(string weaponItemId)
        {
            equipmentVisual?.ApplyWeapon(weaponItemId, drawn: CompanionCombatCoordinator.Instance != null
                && CompanionCombatCoordinator.Instance.IsCombatEngaged);
        }

        private void Update()
        {
            ResolvePlayerFocus();
            ResolveTarget();
            UpdateCombatDrawState();

            if (attackPending && Time.time >= pendingAttackReleaseTime)
                FinishAttack();

            if (currentTarget == null)
                return;

            FaceTarget(currentTarget.transform.position);

            if (attackPending || Time.time < nextAttackTime)
                return;

            float distance = HorizontalDistance(transform.position, currentTarget.transform.position);
            MaintainCombatSpacing(currentTarget.transform.position, distance);

            if (distance > attackRange)
                return;

            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator == null)
                return;

            if (Random.value > coordinator.RollAttackChance(this))
            {
                nextAttackTime = Time.time + coordinator.GetScaledAttackInterval(this) * 0.35f;
                return;
            }

            if (!coordinator.TryBeginAttack(this))
            {
                nextAttackTime = Time.time + 0.3f;
                return;
            }

            attackPending = true;
            pendingAttackReleaseTime = Time.time + attackWindupDelay;
            nextAttackTime = Time.time + coordinator.GetScaledAttackInterval(this);

            equipmentVisual?.SetDrawn(true);
            animationDriver?.TriggerAttack();
            ApplyWeaponDamage(currentTarget);
            followController?.RequestCombatStepBack(currentTarget.transform.position, stepBackDistance, stepBackDuration);
        }

        private void FinishAttack()
        {
            attackPending = false;
            CompanionCombatCoordinator.Instance?.EndAttack(this);
        }

        private void ResolveTarget()
        {
            EnemyHealth selfTarget = null;
            if (behaviorProfile != null && behaviorProfile.followMode == PioneerFollowMode.FollowSelf)
                selfTarget = FindNearestEnemyWithin(proximityAggroRange * 1.35f, requireThreatCone: false);

            EnemyHealth locked = playerFocus != null ? playerFocus.LockedTarget : null;
            if (selfTarget != null && (locked == null || locked.IsDead || Random.value < selfTargetPriority))
            {
                currentTarget = selfTarget;
                return;
            }

            if (locked != null && !locked.IsDead)
            {
                currentTarget = locked;
                return;
            }

            if (currentTarget != null && !currentTarget.IsDead)
                return;

            currentTarget = FindNearestEnemyInRange();
        }

        private void MaintainCombatSpacing(Vector3 enemyPosition, float distance)
        {
            if (followController == null || behaviorProfile == null)
                return;

            if (!behaviorProfile.PrefersRangedSpacing(pioneerClass))
                return;

            if (distance < preferredCombatDistance * 0.72f)
            {
                followController.RequestCombatStepBack(enemyPosition, stepBackDistance * 1.15f, stepBackDuration);
                return;
            }

            if (distance > preferredCombatDistance * 1.2f)
            {
                followController.RequestCombatMaintainDistance(
                    enemyPosition,
                    preferredCombatDistance,
                    stepBackDuration * 1.35f);
            }
        }

        private void UpdateCombatDrawState()
        {
            bool engaged = currentTarget != null && !currentTarget.IsDead;
            if (engaged != wasEngaged)
            {
                CompanionCombatCoordinator.Instance?.NotifyEngagementChanged(engaged);
                wasEngaged = engaged;
            }

            equipmentVisual?.SetDrawn(engaged);
        }

        private EnemyHealth FindNearestEnemyInRange()
        {
            EnemyHealth closeEnemy = FindNearestEnemyWithin(proximityAggroRange, requireThreatCone: false);
            if (closeEnemy != null)
                return closeEnemy;

            if (threatSensor != null && playerFocus != null)
            {
                EnemyHealth sensed = threatSensor.ScanForThreat(playerFocus.transform, playerFocus);
                if (sensed != null)
                    return sensed;
            }

            float maxRange = threatSensor != null && playerFocus != null
                ? threatSensor.EffectiveDetectRange(playerFocus)
                : attackRange;

            return FindNearestEnemyWithin(maxRange, requireThreatCone: false);
        }

        private EnemyHealth FindNearestEnemyWithin(float maxRange, bool requireThreatCone)
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>();
            EnemyHealth best = null;
            float bestDistance = maxRange;

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                float distance = HorizontalDistance(transform.position, enemy.transform.position);
                if (distance > maxRange || (best != null && distance >= bestDistance))
                    continue;

                if (requireThreatCone && threatSensor != null && playerFocus != null)
                {
                    EnemyHealth sensed = threatSensor.ScanForThreat(playerFocus.transform, playerFocus);
                    if (sensed != enemy)
                        continue;
                }

                best = enemy;
                bestDistance = distance;
            }

            return best;
        }

        private void ApplyWeaponDamage(EnemyHealth target)
        {
            if (target == null || target.IsDead)
                return;

            ItemData weapon = equipmentVisual != null ? equipmentVisual.EquippedWeapon : null;
            float damage = weapon != null ? weapon.RollMeleeDamage() : 8f;
            damage *= DamageMultiplier;
            target.TakeDamage(damage, gameObject, isCritical: false);
        }

        private void FaceTarget(Vector3 worldPosition)
        {
            Vector3 toTarget = worldPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
                return;

            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, faceTurnSpeed * Time.deltaTime);
        }

        private void ResolvePlayerFocus()
        {
            if (playerFocus != null)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                playerFocus = player.GetComponent<CombatFocusController>();
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
