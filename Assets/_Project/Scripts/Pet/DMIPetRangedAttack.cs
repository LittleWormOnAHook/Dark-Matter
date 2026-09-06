using Project.AI;
using Project.Combat;
using Project.Companions;
using Project.Core;
using Project.Creatures;
using Project.Player;
using Project.Progression;
using Project.Survival;
using UnityEngine;

namespace Project.Pet
{
    /// <summary>
    /// Choosable pet ranged attack kinds for Pet Manager / Brimmy-style pets.
    /// Extensible — add new entries and map them in <see cref="DMIPetRangedAttack"/>.
    /// </summary>
    public enum DMIPetRangedAttackKind
    {
        None = 0,
        Fireball = 1,
    }

    /// <summary>
    /// Simple pet ranger: fires a pooled projectile at the player's current combat lock target
    /// (<see cref="CombatFocusController.LockedTarget"/>) on a randomized cadence. Follow AI stays
    /// on <see cref="PetController"/> — this only shoots while a valid player target exists.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMIPetRangedAttack : MonoBehaviour
    {
        public const string FireballProjectilePath =
            "Assets/_Project/Prefabs/Combat/Projectiles/FireBall Lite Variant.prefab";

        /// <summary>
        /// Default explosion used by FireBall Lite's Malbers HitEffect — played through
        /// <see cref="CombatHitResolver"/> so every collider hit (including point-blank) gets VFX.
        /// </summary>
        public const string FireballImpactVfxPath =
            "Assets/Malbers Animations/Common/Particles/Prefabs/Explosion.prefab";

        [Header("Attack Kind")]
        [SerializeField] private DMIPetRangedAttackKind attackKind = DMIPetRangedAttackKind.Fireball;
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("Impact/explosion VFX spawned via CombatHitResolver on every collider hit.")]
        [SerializeField] private GameObject impactVfxPrefab;

        [Header("Damage (base 5–10, scales with player level)")]
        [Tooltip("Inclusive min base damage before level scaling.")]
        [SerializeField] private float minBaseDamage = 5f;
        [Tooltip("Inclusive max base damage before level scaling.")]
        [SerializeField] private float maxBaseDamage = 10f;
        [Tooltip("Per level above 1: damage *= (1 + (level-1) * this). Default 0.05 = +5%/level (matches GetLevelStatMultiplier).")]
        [SerializeField] private float damageBonusPerLevel = 0.05f;

        [Header("Cadence")]
        [SerializeField] private float minAttackInterval = 3f;
        [SerializeField] private float maxAttackInterval = 8f;

        [Header("Range / Aim")]
        [SerializeField] private float maxAttackRange = 22f;
        [Tooltip("World-space muzzle height above pet root (avoids huge local offsets on scaled pets like Brimmy).")]
        [SerializeField] private float muzzleWorldHeight = 0.45f;
        [SerializeField] private float muzzleForwardWorld = 0.2f;
        [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private float aimHeightOffset = 0.6f;
        [SerializeField] private float faceTurnSpeed = 10f;

        [Header("Leash / Abandon")]
        [Tooltip("If owner is farther than this from the target, start the abandon timer.")]
        [SerializeField] private float ownerLeashDistance = 16f;
        [Tooltip("Seconds owner may stay beyond leash before pet stops attacking and resumes follow-only.")]
        [SerializeField] private float abandonAfterSeconds = 6f;

        private PetController _pet;
        private CombatFocusController _playerFocus;
        private Transform _owner;
        private EnemyHealth _activeTarget;
        private EnemyHealth _assistTarget;
        private float _assistExpireTime;
        private float _nextThreatScanTime;
        private float _nextFireTime;
        private float _abandonTimer;
        private bool _hadPlayerSideAggro;

        private const float AssistWindowSeconds = 6f;
        private const float ThreatScanInterval = 0.35f;

        public DMIPetRangedAttackKind AttackKind => attackKind;
        public bool IsAttacking => _activeTarget != null && !_activeTarget.IsDead;

        private void Awake()
        {
            _pet = GetComponent<PetController>();
            EnsureProjectilePrefab();
            EnsureImpactVfxPrefab();
            ScheduleNextFire(immediateReady: true);
        }

        private void OnEnable()
        {
            ClearAttack();
            ScheduleNextFire(immediateReady: true);
            PlayerCombatEvents.OnPlayerAttackedBy += HandlePlayerAttackedBy;
            PlayerCombatEvents.OnCompanionAttackedBy += HandlePlayerAttackedBy;
        }

        private void OnDisable()
        {
            PlayerCombatEvents.OnPlayerAttackedBy -= HandlePlayerAttackedBy;
            PlayerCombatEvents.OnCompanionAttackedBy -= HandlePlayerAttackedBy;
            ClearAttack();
        }

        private void HandlePlayerAttackedBy(EnemyHealth attacker)
        {
            if (!IsUsableTarget(attacker))
                return;

            _assistTarget = attacker;
            _assistExpireTime = Time.time + AssistWindowSeconds;
        }

        private void Update()
        {
            if (!CanOperate())
            {
                ClearAttack();
                return;
            }

            CacheOwnerAndFocus();
            if (_owner == null)
            {
                ClearAttack();
                return;
            }

            EnemyHealth playerTarget = ResolvePlayerCurrentTarget();
            if (!IsUsableTarget(playerTarget))
            {
                ClearAttack();
                return;
            }

            if (_activeTarget != playerTarget)
            {
                _activeTarget = playerTarget;
                _abandonTimer = 0f;
                _hadPlayerSideAggro = HasPlayerSideAggro(_activeTarget);
            }

            if (!IsUsableTarget(_activeTarget))
            {
                ClearAttack();
                return;
            }

            // Aggro lost: enemy had player-side threat, then dropped it / switched away.
            bool hasAggro = HasPlayerSideAggro(_activeTarget);
            if (hasAggro)
                _hadPlayerSideAggro = true;
            else if (_hadPlayerSideAggro && TargetHasAggroSystem(_activeTarget))
            {
                if (_assistTarget == _activeTarget)
                {
                    _assistTarget = null;
                    _assistExpireTime = 0f;
                }

                ClearAttack();
                return;
            }

            if (UpdateAbandonLeash(_activeTarget))
            {
                ClearAttack();
                return;
            }

            float distance = HorizontalDistance(transform.position, _activeTarget.transform.position);
            if (distance > maxAttackRange)
                return;

            FaceTarget(_activeTarget.transform.position);

            if (Time.time < _nextFireTime)
                return;

            if (TryFire(_activeTarget))
                ScheduleNextFire(immediateReady: false);
        }

        private bool CanOperate()
        {
            if (attackKind == DMIPetRangedAttackKind.None)
                return false;

            if (_pet == null)
                _pet = GetComponent<PetController>();

            if (_pet == null)
                return false;

            return _pet.IsOwned && _pet.CompanionActive;
        }

        private void CacheOwnerAndFocus()
        {
            Transform owner = _pet != null ? _pet.Owner : null;
            if (owner == null)
                owner = PlayerReference.ResolveTransform();

            if (owner != _owner)
            {
                _owner = owner;
                _playerFocus = owner != null ? owner.GetComponent<CombatFocusController>() : null;
            }
            else if (_playerFocus == null && _owner != null)
            {
                _playerFocus = _owner.GetComponent<CombatFocusController>();
            }
        }

        private EnemyHealth ResolvePlayerCurrentTarget()
        {
            // 1) Explicit combat lock (CombatFocusController — used when ECM2 focus path is live).
            if (_playerFocus != null)
            {
                // Component may be disabled under Invector bootstrap; LockedTarget still readable
                // if UpdateFocus was driven elsewhere, but often null — fall through.
                EnemyHealth locked = _playerFocus.LockedTarget;
                if (IsUsableTarget(locked))
                    return locked;
            }

            // 2) Enemy that just hit the player / companions (PlayerCombatEvents).
            if (IsUsableTarget(_assistTarget) && Time.time < _assistExpireTime)
                return _assistTarget;

            _assistTarget = null;

            // 3) Enemy actively aggro'd on the player (same idea as companion assist).
            if (Time.time >= _nextThreatScanTime)
            {
                _nextThreatScanTime = Time.time + ThreatScanInterval;
                EnemyHealth threatening = FindEnemyThreateningOwner();
                if (IsUsableTarget(threatening))
                {
                    _assistTarget = threatening;
                    _assistExpireTime = Time.time + AssistWindowSeconds;
                    return threatening;
                }
            }

            return null;
        }

        private EnemyHealth FindEnemyThreateningOwner()
        {
            if (_owner == null)
                return null;

            EnemyHealth[] enemies = SceneComponentCache.GetAll<EnemyHealth>(FindObjectsInactive.Exclude);
            EnemyHealth best = null;
            float bestDistance = maxAttackRange;

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (!IsUsableTarget(enemy))
                    continue;

                float distance = HorizontalDistance(transform.position, enemy.transform.position);
                if (distance > bestDistance)
                    continue;

                if (HasPlayerSideAggro(enemy))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private bool UpdateAbandonLeash(EnemyHealth target)
        {
            if (_owner == null || target == null)
                return true;

            float ownerToTarget = HorizontalDistance(_owner.position, target.transform.position);
            if (ownerToTarget <= ownerLeashDistance)
            {
                _abandonTimer = 0f;
                return false;
            }

            _abandonTimer += Time.deltaTime;
            return _abandonTimer >= abandonAfterSeconds;
        }

        private bool TryFire(EnemyHealth target)
        {
            EnsureProjectilePrefab();
            EnsureImpactVfxPrefab();
            if (projectilePrefab == null || target == null)
                return false;

            Vector3 origin = transform.position
                             + Vector3.up * muzzleWorldHeight
                             + transform.forward * muzzleForwardWorld;
            Vector3 aimPoint = target.transform.position + Vector3.up * aimHeightOffset;
            Vector3 direction = aimPoint - origin;
            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;
            direction.Normalize();

            float damage = RollScaledDamage();
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            GameObject instance = PoolManager.Spawn(projectilePrefab, origin, rotation);
            if (instance == null)
                return false;

            PrepareProjectileForSharedCombat(instance);

            CombatProjectile projectile = instance.GetComponent<CombatProjectile>();
            if (projectile == null)
                projectile = instance.AddComponent<CombatProjectile>();

            // Shared FireBall Lite Variant is VFX-only; damage + explosion go through CombatProjectile.
            projectile.Launch(
                gameObject,
                direction,
                damage,
                AmmoType.Fire,
                ammoItemData: null,
                speedOverride: projectileSpeed,
                critical: false,
                weaponItemData: null,
                impactVfxPrefabOverride: impactVfxPrefab);

            return true;
        }

        /// <summary>
        /// FireBall Lite ships with Malbers MProjectile + non-kinematic Rigidbody. Disable those so
        /// <see cref="CombatProjectile"/> owns travel/hit/VFX (Malbers HitEffect only fires after
        /// travel collision and misses point-blank / spawn-inside targets).
        /// </summary>
        private static void PrepareProjectileForSharedCombat(GameObject instance)
        {
            if (instance == null)
                return;

            MonoBehaviour[] behaviours = instance.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "MProjectile" || typeName == "MDamager")
                    behaviour.enabled = false;
            }

            Rigidbody body = instance.GetComponent<Rigidbody>();
            if (body != null)
            {
                // Unity errors if linear/angular velocity is set on a kinematic body (pooled
                // FireBall Lite may already be kinematic from a previous Prepare pass).
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            Collider ownCollider = instance.GetComponent<Collider>();
            if (ownCollider != null)
                ownCollider.enabled = false;
        }

        private float RollScaledDamage()
        {
            float min = Mathf.Min(minBaseDamage, maxBaseDamage);
            float max = Mathf.Max(minBaseDamage, maxBaseDamage);
            float rolled = Random.Range(min, max);

            int level = 1;
            PlayerProgressionManager progression = PlayerProgressionManager.Instance;
            if (progression != null)
                level = Mathf.Max(1, progression.Level);

            float multiplier = 1f + (level - 1) * Mathf.Max(0f, damageBonusPerLevel);
            return Mathf.Max(1f, rolled * multiplier);
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

        private void ScheduleNextFire(bool immediateReady)
        {
            float min = Mathf.Min(minAttackInterval, maxAttackInterval);
            float max = Mathf.Max(minAttackInterval, maxAttackInterval);
            if (immediateReady)
            {
                // Small delay so summon/adopt doesn't instantly spit a fireball.
                _nextFireTime = Time.time + Random.Range(Mathf.Min(1f, min), min);
                return;
            }

            _nextFireTime = Time.time + Random.Range(min, max);
        }

        private void ClearAttack()
        {
            _activeTarget = null;
            _abandonTimer = 0f;
            _hadPlayerSideAggro = false;
        }

        private void EnsureProjectilePrefab()
        {
            if (projectilePrefab != null)
                return;

            if (attackKind != DMIPetRangedAttackKind.Fireball)
                return;

#if UNITY_EDITOR
            projectilePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FireballProjectilePath);
#else
            // Runtime fallback: Resources copy if authored under Resources; otherwise leave null.
            projectilePrefab = Resources.Load<GameObject>("Combat/Projectiles/FireBall Lite Variant");
#endif
        }

        private void EnsureImpactVfxPrefab()
        {
            if (impactVfxPrefab != null)
                return;

            if (attackKind != DMIPetRangedAttackKind.Fireball)
                return;

#if UNITY_EDITOR
            impactVfxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FireballImpactVfxPath);
#else
            impactVfxPrefab = Resources.Load<GameObject>("Combat/Projectiles/Explosion");
#endif
        }

        /// <summary>Editor / Pet Manager: apply kind + default fireball prefab + impact VFX.</summary>
        public void ConfigureAttackKind(DMIPetRangedAttackKind kind, GameObject prefab = null, GameObject impactPrefab = null)
        {
            attackKind = kind;
            if (prefab != null)
                projectilePrefab = prefab;
            else if (kind == DMIPetRangedAttackKind.Fireball)
                EnsureProjectilePrefab();
            else
                projectilePrefab = null;

            if (impactPrefab != null)
                impactVfxPrefab = impactPrefab;
            else if (kind == DMIPetRangedAttackKind.Fireball)
                EnsureImpactVfxPrefab();
            else
                impactVfxPrefab = null;
        }

        /// <summary>Editor / Pet Manager: write key ranged settings onto this component.</summary>
        public void ConfigureSettings(
            DMIPetRangedAttackKind kind,
            GameObject prefab,
            GameObject impactPrefab,
            float minDamage,
            float maxDamage,
            float damagePerLevel,
            float minInterval,
            float maxInterval,
            float attackRange,
            float leashDistance,
            float abandonSeconds,
            float speed)
        {
            ConfigureAttackKind(kind, prefab, impactPrefab);
            minBaseDamage = Mathf.Max(0f, minDamage);
            maxBaseDamage = Mathf.Max(minBaseDamage, maxDamage);
            damageBonusPerLevel = Mathf.Max(0f, damagePerLevel);
            minAttackInterval = Mathf.Max(0.25f, minInterval);
            maxAttackInterval = Mathf.Max(minAttackInterval, maxInterval);
            maxAttackRange = Mathf.Max(1f, attackRange);
            ownerLeashDistance = Mathf.Max(1f, leashDistance);
            abandonAfterSeconds = Mathf.Max(0.1f, abandonSeconds);
            projectileSpeed = Mathf.Max(1f, speed);
        }

        private static bool IsUsableTarget(EnemyHealth enemy)
        {
            return enemy != null && !enemy.IsDead && enemy.isActiveAndEnabled;
        }

        private bool HasPlayerSideAggro(EnemyHealth enemy)
        {
            if (enemy == null || _owner == null)
                return false;

            EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
            if (combat != null && combat.CurrentTarget != null)
                return IsPlayerSideTransform(combat.CurrentTarget);

            DMICreatureBridge bridge = enemy.GetComponent<DMICreatureBridge>();
            if (bridge != null && bridge.CurrentThreat != null)
                return IsPlayerSideTransform(bridge.CurrentThreat);

            return false;
        }

        private static bool TargetHasAggroSystem(EnemyHealth enemy)
        {
            if (enemy == null)
                return false;

            return enemy.GetComponent<EnemyCombat>() != null
                   || enemy.GetComponent<DMICreatureBridge>() != null;
        }

        private bool IsPlayerSideTransform(Transform candidate)
        {
            if (candidate == null || _owner == null)
                return false;

            if (candidate == _owner || candidate.IsChildOf(_owner) || _owner.IsChildOf(candidate))
                return true;

            if (candidate.GetComponentInParent<PetController>() != null)
                return true;

            if (candidate.GetComponentInParent<CompanionHealth>() != null)
                return true;

            SurvivalStats stats = candidate.GetComponentInParent<SurvivalStats>();
            if (stats != null && stats.transform == _owner)
                return true;

            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            minBaseDamage = Mathf.Max(0f, minBaseDamage);
            maxBaseDamage = Mathf.Max(minBaseDamage, maxBaseDamage);
            minAttackInterval = Mathf.Max(0.25f, minAttackInterval);
            maxAttackInterval = Mathf.Max(minAttackInterval, maxAttackInterval);
            maxAttackRange = Mathf.Max(1f, maxAttackRange);
            ownerLeashDistance = Mathf.Max(1f, ownerLeashDistance);
            abandonAfterSeconds = Mathf.Max(0.1f, abandonAfterSeconds);
            projectileSpeed = Mathf.Max(1f, projectileSpeed);

            if (attackKind == DMIPetRangedAttackKind.Fireball)
            {
                if (projectilePrefab == null)
                    EnsureProjectilePrefab();
                if (impactVfxPrefab == null)
                    EnsureImpactVfxPrefab();
            }
        }
#endif
    }
}
