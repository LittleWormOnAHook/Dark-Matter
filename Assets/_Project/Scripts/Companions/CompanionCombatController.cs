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
        private PlayerGkcAnimatorDriver gkcAnimatorDriver;
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
        private bool damageApplied;
        private bool wasEngaged;
        private int comboIndex;
        private float lastComboAttackTime;
        private float attackFinishTime;
        private EnemyHealth pendingDamageTarget;
        private const int ComboLength = 4;
        private const float ComboResetWindow = 1.2f;
        private PioneerBehaviorProfile behaviorProfile = new PioneerBehaviorProfile();
        private SkilledPioneerClass pioneerClass = SkilledPioneerClass.CombatTactician;
        private float preferredCombatDistance = 2.4f;
        private float selfTargetPriority = 0.35f;

        public string PioneerSeed => pioneerSeed;
        public SkilledPioneerClass PioneerClass => pioneerClass;
        public EnemyHealth CurrentTarget => currentTarget;
        public float AttackRange => attackRange;

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

            if (attackPending && !damageApplied && Time.time >= pendingAttackReleaseTime)
            {
                ApplyWeaponDamage(pendingDamageTarget);
                damageApplied = true;
            }

            if (attackPending && Time.time >= attackFinishTime)
                FinishAttack();

            if (currentTarget == null)
                return;

            FaceTarget(currentTarget.transform.position);

            if (attackPending || Time.time < nextAttackTime)
                return;

            float distance = HorizontalDistance(transform.position, currentTarget.transform.position);
            MaintainCombatSpacing(currentTarget.transform.position, distance);

            if (distance > attackRange)
            {
                if (ShouldForceAggressiveAttack())
                    followController?.RequestCombatChase(currentTarget.transform.position, attackRange * 0.92f, 0.35f);
                return;
            }

            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator == null)
                return;

            bool forceAttack = ShouldForceAggressiveAttack();
            if (!forceAttack && Random.value > coordinator.RollAttackChance(this))
            {
                nextAttackTime = Time.time + coordinator.GetScaledAttackInterval(this) * 0.35f;
                return;
            }

            if (!coordinator.TryBeginAttack(this, forceAttack))
            {
                nextAttackTime = Time.time + (forceAttack ? 0.12f : 0.3f);
                return;
            }

            attackPending = true;
            damageApplied = false;
            pendingDamageTarget = currentTarget;
            equipmentVisual?.SetDrawn(true);

            ItemData weapon = equipmentVisual != null ? equipmentVisual.EquippedWeapon : null;
            float attackSpeed = weapon != null ? weapon.ResolveAttackAnimationSpeed() : 0.95f;
            ResolveGkcAnimatorDriver();

            if (Time.time - lastComboAttackTime > ComboResetWindow)
                comboIndex = 0;

            GkcCombatAction action = ResolveComboAction(weapon, comboIndex);
            float swingDuration = attackWindupDelay;
            bool animationStarted = false;

            if (gkcAnimatorDriver != null)
            {
                swingDuration = gkcAnimatorDriver.ResolveActionDuration(action, attackSpeed);
                animationStarted = gkcAnimatorDriver.RequestAction(action, attackSpeed: attackSpeed);
            }

            if (!animationStarted)
                animationDriver?.TriggerAttack();

            pendingAttackReleaseTime = Time.time + swingDuration * 0.42f;
            attackFinishTime = Time.time + swingDuration + 0.12f;
            nextAttackTime = Time.time + coordinator.GetScaledAttackInterval(this);
            comboIndex = (comboIndex + 1) % ComboLength;
            lastComboAttackTime = Time.time;

            if (!forceAttack)
                followController?.RequestCombatStepBack(currentTarget.transform.position, stepBackDistance, stepBackDuration);
        }

        private void FinishAttack()
        {
            attackPending = false;
            damageApplied = false;
            pendingDamageTarget = null;
            CompanionCombatCoordinator.Instance?.EndAttack(this);
        }

        private void ResolveGkcAnimatorDriver()
        {
            if (gkcAnimatorDriver != null)
                return;

            gkcAnimatorDriver = GetComponentInChildren<PlayerGkcAnimatorDriver>(true);
        }

        private static GkcCombatAction ResolveComboAction(ItemData item, int index)
        {
            int slot = Mathf.Clamp(index, 0, ComboLength - 1);
            if (item == null)
            {
                return slot switch
                {
                    0 => GkcCombatAction.Punch1,
                    1 => GkcCombatAction.Punch2,
                    2 => GkcCombatAction.Punch3,
                    _ => GkcCombatAction.Punch4
                };
            }

            return item.ResolveGkcWeaponKind() switch
            {
                GkcWeaponKind.TwoHand => slot switch
                {
                    0 => GkcCombatAction.Sword2HCombo1,
                    1 => GkcCombatAction.Sword2HCombo2,
                    2 => GkcCombatAction.Sword2HCombo3,
                    _ => GkcCombatAction.Sword2HCombo4
                },
                GkcWeaponKind.OneHandAxe => slot switch
                {
                    0 => GkcCombatAction.Axe1HCombo1,
                    1 => GkcCombatAction.Axe1HCombo2,
                    2 => GkcCombatAction.Axe1HCombo3,
                    _ => GkcCombatAction.Axe1HCombo4
                },
                _ => slot switch
                {
                    0 => GkcCombatAction.Sword1HCombo1,
                    1 => GkcCombatAction.Sword1HCombo2,
                    2 => GkcCombatAction.Sword1HCombo3,
                    _ => GkcCombatAction.Sword1HCombo4
                }
            };
        }

        private bool ShouldForceAggressiveAttack()
        {
            return pioneerClass == SkilledPioneerClass.CombatTactician && IsTargetWithinSenseRange();
        }

        private bool IsTargetWithinSenseRange()
        {
            if (currentTarget == null || currentTarget.IsDead)
                return false;

            float maxRange = threatSensor != null && playerFocus != null
                ? threatSensor.EffectiveDetectRange(playerFocus)
                : attackRange;

            return HorizontalDistance(transform.position, currentTarget.transform.position) <= maxRange;
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
