using Project.AI;
using Project.Combat;
using Project.Companions.Invector;
using Project.Core;
using Project.Data;
using Project.Player;
using Project.Pioneers;
using Project.Survival;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Attacks the engaged enemy with seed-driven timing, reduced damage, and combat step-back.
    /// </summary>
    public class CompanionCombatController : MonoBehaviour
    {
        private const float DamageMultiplier = 0.25f;
        private const float RangedAimAlignDegrees = 14f;
        private const float MeleeAimAlignDegrees = 35f;
        private const float RangedMinAttackInterval = 1.6f;
        private const float MeleeContactMin = 1.1f;
        private const float MeleeContactMax = 1.65f;
        private const float UnarmedContactRange = 1.25f;
        private const float LegacyRangedStandoffRangeFactor = 0.6f;
        private const float RifleStandoffScale = 0.5f;

        // ResolveTarget()/FindEnemyThreateningPlayer() each do a full-scene FindObjectsByType<EnemyHealth>()
        // scan; with several companions doing this every single Update() frame the cost multiplies
        // fast. Throttling to ~7Hz (matching EnemySenses' vision-refresh cadence) is imperceptible for
        // target acquisition/handoff but cuts the scan cost by roughly 85% at 60fps.
        private const float TargetScanInterval = 0.15f;

        [SerializeField] private float attackRange = 2.5f;
        [SerializeField] private float proximityAggroRange = 3.75f;
        [SerializeField] private float faceTurnSpeed = 12f;
        [SerializeField] private float attackWindupDelay = 0.18f;

        [Header("Player Assist")]
        [Tooltip("If the player or another companion is attacked by an enemy within this range, this companion engages it even if outside normal aggro/sense range, distance to the ally under attack, or visual line of sight. Sized generously since squadmates can be spread across the follow leash.")]
        [SerializeField] private float assistAlertRange = 22f;
        [Tooltip("How long the companion keeps chasing the assist target before it can expire if it never comes into normal range.")]
        [SerializeField] private float assistWindowSeconds = 6f;

        private CompanionEquipmentVisual equipmentVisual;
        private CompanionInvectorLoadoutBridge invectorLoadout;
        private CompanionInvectorCombatBridge invectorCombat;
        private CompanionFollowController followController;
        private CompanionThreatSensor threatSensor;
        private CombatFocusController playerFocus;
        private float nextAttackTime;
        private float pendingAttackReleaseTime;
        private EnemyHealth currentTarget;
        private string pioneerSeed = string.Empty;
        private float personalAttackBias = 0.72f;
        private float personalIntervalMultiplier = 1f;
        private float personalStandoffBias = 1f;
        private bool attackPending;
        private bool damageApplied;
        private bool skipManualDamage;
        private bool wasEngaged;
        private float attackFinishTime;
        private EnemyHealth pendingDamageTarget;
        private EnemyHealth assistTarget;
        private float assistTargetExpireTime;
        private float nextTargetScanTime;
        private PioneerBehaviorProfile behaviorProfile = new PioneerBehaviorProfile();
        private SkilledPioneerClass pioneerClass = SkilledPioneerClass.CombatTactician;
        private float preferredCombatDistance = 2.4f;
        private float selfTargetPriority = 0.35f;

        public string PioneerSeed => pioneerSeed;
        public SkilledPioneerClass PioneerClass => pioneerClass;
        public EnemyHealth CurrentTarget => currentTarget;
        public float AttackRange => attackRange;
        public bool IsEngagedInCombat => currentTarget != null && !currentTarget.IsDead;
        public bool IsAttackPending => attackPending;
        public ItemData EquippedWeapon => ResolveEquippedWeapon();

        /// <summary>
        /// Preferred horizontal combat spacing for the equipped weapon (melee contact or ranged standoff).
        /// </summary>
        public float ResolveEngagementDistance(ItemData weapon)
        {
            if (weapon != null && weapon.IsRangedWeapon)
                return ResolveRangedStandoffDistance(weapon);

            return ResolveMeleeContactRange(weapon) * 0.88f;
        }

        private void Awake()
        {
            equipmentVisual = GetComponent<CompanionEquipmentVisual>();
            invectorLoadout = GetComponent<CompanionInvectorLoadoutBridge>();
            invectorCombat = GetComponent<CompanionInvectorCombatBridge>();
            followController = GetComponent<CompanionFollowController>();
            threatSensor = GetComponent<CompanionThreatSensor>();
        }

        private void OnEnable()
        {
            CompanionCombatCoordinator.EnsureExists(this)?.Register(this);
            ResolvePlayerFocus();
            PlayerCombatEvents.OnPlayerAttackedBy += HandleAllyAttacked;
            PlayerCombatEvents.OnCompanionAttackedBy += HandleAllyAttacked;
        }

        private void OnDisable()
        {
            PlayerCombatEvents.OnPlayerAttackedBy -= HandleAllyAttacked;
            PlayerCombatEvents.OnCompanionAttackedBy -= HandleAllyAttacked;

            CompanionCombatCoordinator coordinator = CompanionCombatCoordinator.Instance;
            if (coordinator != null)
            {
                coordinator.EndAttack(this);
                if (wasEngaged)
                    coordinator.NotifyEngagementChanged(false);
            }

            followController?.ClearCombatEngagement();
        }

        /// <summary>
        /// Reacts to the player or another companion taking damage from a nearby enemy by
        /// adopting it as an assist target, so this companion fights back even when the attacker
        /// is outside its own proximity aggro/threat-cone range (e.g. an enemy flanking or
        /// attacking from behind, or attacking a squadmate this companion can't see/isn't near).
        /// </summary>
        private void HandleAllyAttacked(EnemyHealth attacker)
        {
            if (attacker == null || attacker.IsDead)
                return;

            if (HorizontalDistance(transform.position, attacker.transform.position) > assistAlertRange)
                return;

            assistTarget = attacker;
            assistTargetExpireTime = Time.time + assistWindowSeconds;
        }

        private EnemyHealth ResolveActiveAssistTarget()
        {
            if (assistTarget == null || assistTarget.IsDead || Time.time >= assistTargetExpireTime)
            {
                assistTarget = null;
                return null;
            }

            return assistTarget;
        }

        /// <summary>
        /// Engage when an enemy is actively attacking/chasing the player even if no damage event
        /// fired yet (missed shots, windup, or the companion is outside its own proximity cone).
        /// </summary>
        private EnemyHealth FindEnemyThreateningPlayer()
        {
            GameObject playerObject = PlayerLocator.FindPlayerObject();
            if (playerObject == null)
                return null;

            Transform playerRoot = playerObject.transform;
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>();
            EnemyHealth best = null;
            float bestDistance = assistAlertRange;

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                float distance = HorizontalDistance(transform.position, enemy.transform.position);
                if (distance > assistAlertRange || (best != null && distance >= bestDistance))
                    continue;

                EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
                if (combat == null || !combat.HasLivingTarget())
                    continue;

                Transform target = combat.CurrentTarget;
                if (target == null || !IsPlayerCombatRoot(target, playerRoot))
                    continue;

                EnemyAiController ai = enemy.GetComponent<EnemyAiController>();
                if (ai != null && !ai.IsEngagedWithTarget)
                    continue;

                best = enemy;
                bestDistance = distance;
            }

            if (best != null)
            {
                assistTarget = best;
                assistTargetExpireTime = Time.time + assistWindowSeconds;
            }

            return best;
        }

        private static bool IsPlayerCombatRoot(Transform candidate, Transform playerRoot)
        {
            if (candidate == null || playerRoot == null)
                return false;

            if (candidate == playerRoot || candidate.IsChildOf(playerRoot))
                return true;

            return candidate.GetComponentInParent<SurvivalStats>() != null;
        }

        public void Initialize(string pioneerId)
        {
            pioneerSeed = string.IsNullOrEmpty(pioneerId) ? name : pioneerId;
            int hash = pioneerSeed.GetHashCode();
            personalAttackBias = 0.55f + (Mathf.Abs(hash) % 1000) / 1000f * 0.35f;
            personalIntervalMultiplier = 0.85f + (Mathf.Abs(hash >> 8) % 1000) / 1000f * 0.3f;
            personalStandoffBias = 0.9f + (Mathf.Abs(hash >> 4) % 1000) / 1000f * 0.2f;
        }

        public void ApplyBehaviorProfile(PioneerBehaviorProfile profile, SkilledPioneerClass skilledClass)
        {
            pioneerClass = skilledClass;
            behaviorProfile = profile != null ? profile.Clone() : new PioneerBehaviorProfile();
            preferredCombatDistance = behaviorProfile.ResolvePreferredCombatDistance(pioneerClass);
            selfTargetPriority = behaviorProfile.followMode == PioneerFollowMode.FollowSelf ? 0.68f : 0.35f;
            // Ranged standoff may widen detect/fire range. Melee contact is owned by
            // ResolveMeleeContactRange — never stretch it to preferred spacing.
            if (behaviorProfile.PrefersRangedSpacing(pioneerClass))
                attackRange = Mathf.Max(attackRange, preferredCombatDistance * 0.82f);
        }

        public float GetPersonalAttackBias() => personalAttackBias;

        public float GetPersonalIntervalMultiplier() => personalIntervalMultiplier;

        public void RefreshLoadoutWeapon(string weaponItemId)
        {
            if (invectorLoadout != null)
            {
                invectorLoadout.ApplyWeapon(weaponItemId, false);
                return;
            }

            bool drawn = CompanionCombatCoordinator.Instance != null &&
                         CompanionCombatCoordinator.Instance.IsCombatEngaged;
            equipmentVisual?.ApplyWeapon(weaponItemId, drawn);
        }

        private ItemData ResolveEquippedWeapon()
        {
            if (invectorLoadout != null)
                return invectorLoadout.ActiveItem;

            return equipmentVisual != null ? equipmentVisual.EquippedWeapon : null;
        }

        private void SetWeaponDrawn(bool drawn)
        {
            if (invectorLoadout != null)
                invectorLoadout.SetDrawn(drawn);
            else
                equipmentVisual?.SetDrawn(drawn);
        }

        private void Update()
        {
            ResolvePlayerFocus();
            ResolveTarget();
            UpdateCombatDrawState();

            if (attackPending && !damageApplied && !skipManualDamage && Time.time >= pendingAttackReleaseTime)
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

            ItemData equippedWeapon = ResolveEquippedWeapon();
            float effectiveRange = ResolveAttackEligibilityRange(equippedWeapon);
            bool isRanged = equippedWeapon != null && equippedWeapon.IsRangedWeapon;

            // Attack contact is part of the brain: melee only swings inside hit range.
            // Positioning is owned by the follow controller's combat-engagement ring.
            if (distance > effectiveRange)
                return;

            float alignDegrees = isRanged ? RangedAimAlignDegrees : MeleeAimAlignDegrees;
            if (!IsFacingTarget(currentTarget.transform.position, alignDegrees))
                return;

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
                nextAttackTime = Time.time + (forceAttack ? 0.15f : 0.2f);
                return;
            }

            attackPending = true;
            damageApplied = false;
            skipManualDamage = false;
            pendingDamageTarget = currentTarget;
            SetWeaponDrawn(true);

            ItemData weapon = ResolveEquippedWeapon();
            float swingDuration = attackWindupDelay;

            if (!UsesInvectorCombatPath())
            {
                attackPending = false;
                pendingDamageTarget = null;
                CompanionCombatCoordinator.Instance?.EndAttack(this);
                nextAttackTime = Time.time + 0.75f;
                return;
            }

            skipManualDamage = true;
            damageApplied = true;

            if (!invectorCombat.TryBeginAttack(currentTarget.transform, weapon, out swingDuration))
            {
                attackPending = false;
                pendingDamageTarget = null;
                CompanionCombatCoordinator.Instance?.EndAttack(this);
                nextAttackTime = Time.time + 0.35f;
                return;
            }

            pendingAttackReleaseTime = Time.time + swingDuration * (skipManualDamage ? 1f : 0.42f);
            attackFinishTime = Time.time + swingDuration + 0.12f;
            nextAttackTime = Time.time + ResolveAttackCooldown(weapon, coordinator);
        }

        private float ResolveAttackCooldown(ItemData weapon, CompanionCombatCoordinator coordinator)
        {
            float interval = coordinator.GetScaledAttackInterval(this);
            if (weapon != null && weapon.IsRangedWeapon)
                interval = Mathf.Max(interval * 1.6f, RangedMinAttackInterval);

            return interval;
        }

        private bool UsesInvectorCombatPath()
        {
            return invectorCombat != null && invectorLoadout != null;
        }

        private void FinishAttack()
        {
            attackPending = false;
            damageApplied = false;
            skipManualDamage = false;
            pendingDamageTarget = null;
            CompanionCombatCoordinator.Instance?.EndAttack(this);
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
            // Keep chasing/attacking whatever we've already locked onto every frame (cheap), but
            // only re-run the expensive full-scene scans on the throttled cadence — unless we have
            // no target at all, in which case rescan immediately so acquisition doesn't feel laggy.
            bool haveLiveTarget = currentTarget != null && !currentTarget.IsDead;
            if (haveLiveTarget && Time.time < nextTargetScanTime)
                return;

            nextTargetScanTime = Time.time + TargetScanInterval;

            EnemyHealth selfTarget = null;
            if (behaviorProfile != null && behaviorProfile.followMode == PioneerFollowMode.FollowSelf)
                selfTarget = FindNearestEnemyWithin(proximityAggroRange * 1.35f, requireThreatCone: false);

            EnemyHealth locked = playerFocus != null ? playerFocus.LockedTarget : null;
            // Assisting the player against whoever just hit them takes priority over an idle
            // fallback, but the player's own locked target (if any) still wins so companions
            // don't yank away from the enemy the player is actively fighting.
            EnemyHealth assist = ResolveActiveAssistTarget() ?? FindEnemyThreateningPlayer();
            EnemyHealth priorityTarget = (locked != null && !locked.IsDead) ? locked : assist;

            if (selfTarget != null && (priorityTarget == null || Random.value < selfTargetPriority))
            {
                currentTarget = selfTarget;
                return;
            }

            if (priorityTarget != null)
            {
                currentTarget = priorityTarget;
                return;
            }

            if (currentTarget != null && !currentTarget.IsDead)
                return;

            currentTarget = FindNearestEnemyInRange();
        }

        private void UpdateCombatDrawState()
        {
            bool engaged = currentTarget != null && !currentTarget.IsDead;
            if (engaged != wasEngaged)
            {
                CompanionCombatCoordinator.Instance?.NotifyEngagementChanged(engaged);
                wasEngaged = engaged;
            }

            if (followController != null)
            {
                if (engaged)
                {
                    ItemData weapon = ResolveEquippedWeapon();
                    float strikeRange = ResolveEffectiveAttackRange(weapon);
                    bool isRanged = weapon != null && weapon.IsRangedWeapon;
                    followController.SetCombatEngagement(
                        currentTarget.transform,
                        ResolveEngagementRingDistance(weapon, strikeRange),
                        strikeRange,
                        isRanged);

                    if (!wasEngaged)
                    {
                        EnemyNoiseEvents.RaiseNoise(transform.position, 8f, gameObject);
                        NotifyEnemyAggro(currentTarget);
                    }
                }
                else
                    followController.ClearCombatEngagement();
            }

            SetWeaponDrawn(engaged);
        }

        /// <summary>
        /// Distance of the combat comfort ring: melee closes to contact range so swings
        /// connect; ranged classes hold their preferred standoff distance.
        /// </summary>
        private float ResolveEngagementRingDistance(ItemData weapon, float strikeRange)
        {
            if (weapon != null && weapon.IsRangedWeapon)
                return ResolveRangedStandoffDistance(weapon);

            // Hold slightly inside strike range so the follow deadband never exceeds swing reach.
            return strikeRange * 0.92f * personalStandoffBias;
        }

        /// <summary>
        /// Attack gate: ranged pioneers may fire from engagement ring distance, not only raw weapon max range.
        /// </summary>
        private float ResolveAttackEligibilityRange(ItemData weapon)
        {
            float strikeRange = ResolveEffectiveAttackRange(weapon);
            if (weapon == null || !weapon.IsRangedWeapon)
                return strikeRange;

            float ringDistance = ResolveEngagementRingDistance(weapon, strikeRange);
            return Mathf.Max(strikeRange, ringDistance * 0.95f);
        }

        /// <summary>
        /// Rifles hold at half the legacy standoff; pistols at half rifle distance on the same weapon curve.
        /// Per-pioneer and per-weapon jitter keeps spacing from feeling identical.
        /// </summary>
        private float ResolveRangedStandoffDistance(ItemData weapon)
        {
            float legacyFromWeapon = Mathf.Max(
                preferredCombatDistance,
                weapon.rangedRange * LegacyRangedStandoffRangeFactor);

            float gripScale = weapon.weaponGrip == WeaponGrip.OneHanded
                ? RifleStandoffScale * 0.5f
                : RifleStandoffScale;

            return legacyFromWeapon * gripScale * personalStandoffBias * ResolveWeaponStandoffJitter(weapon);
        }

        private float ResolveWeaponStandoffJitter(ItemData weapon)
        {
            string key = weapon != null ? weapon.itemName : string.Empty;
            int jitterHash = (pioneerSeed + key).GetHashCode();
            return 0.92f + (Mathf.Abs(jitterHash) % 1000) / 1000f * 0.16f;
        }

        private void NotifyEnemyAggro(EnemyHealth enemy)
        {
            if (enemy == null)
                return;

            EnemyAiController ai = enemy.GetComponent<EnemyAiController>();
            ai?.NotifyAggroFromThreat(transform);
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

            ItemData weapon = ResolveEquippedWeapon();
            if (weapon != null && weapon.IsRangedWeapon)
            {
                TryFireRangedProjectile(target, weapon);
                return;
            }

            float damage = weapon != null ? weapon.RollMeleeDamage() : 8f;
            damage *= DamageMultiplier;

            // Squad-wide combat synergy from the active trio's data-asset buffs (see
            // CompanionGroupBuffService) — every companion in the field hits a little harder when
            // any of them carries a combat-synergy buff, not just the buff's owner.
            damage *= 1f + CompanionGroupBuffService.Current.CombatSynergyBonus;

            target.TakeDamage(damage, gameObject, isCritical: false);
        }

        private void TryFireRangedProjectile(EnemyHealth target, ItemData weapon)
        {
            if (target == null || weapon == null)
                return;

            Vector3 origin = transform.position + Vector3.up * 1.25f;
            Vector3 direction = target.transform.position - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;

            direction.Normalize();

            GameObject muzzleProxy = new GameObject("CompanionMuzzleProxy");
            muzzleProxy.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, Vector3.up));
            CombatProjectileSpawner.Spawn(
                gameObject,
                muzzleProxy.transform,
                weapon,
                null,
                direction,
                weapon.projectileSpreadDegrees * 1.35f);
            Destroy(muzzleProxy);
        }

        private float ResolveEffectiveAttackRange(ItemData weapon)
        {
            if (weapon != null && weapon.IsRangedWeapon)
                return Mathf.Max(attackRange, weapon.rangedRange * 0.82f);

            return ResolveMeleeContactRange(weapon);
        }

        /// <summary>
        /// Horizontal distance at which a melee swing is allowed to start. Item meleeRange
        /// values are generous for player feel; companion hitboxes only connect closer in.
        /// </summary>
        private static float ResolveMeleeContactRange(ItemData weapon)
        {
            if (weapon == null || weapon.itemType != ItemType.MeleeWeapon)
                return UnarmedContactRange;

            float contact = weapon.meleeRange > 0.1f
                ? weapon.meleeRange * 0.65f
                : UnarmedContactRange;
            return Mathf.Clamp(contact, MeleeContactMin, MeleeContactMax);
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

        private bool IsFacingTarget(Vector3 worldPosition, float maxAngleDegrees)
        {
            Vector3 toTarget = worldPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
                return true;

            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            return angle <= maxAngleDegrees;
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
