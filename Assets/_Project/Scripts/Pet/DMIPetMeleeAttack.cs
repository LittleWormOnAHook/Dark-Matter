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
    /// Close-range pet melee: swings at the player's current combat target when inside engage range.
    /// Settings are authored from Pet Manager (Creature Manager–style sections).
    /// </summary>
    [DisallowMultipleComponent]
    public class DMIPetMeleeAttack : MonoBehaviour
    {
        [Header("Melee")]
        [Tooltip("When off, this component never swings (ranged-only pets).")]
        [SerializeField] private bool meleeEnabled = false;

        [Tooltip("Horizontal distance at which melee swings are allowed.")]
        [SerializeField] private float meleeEngageRange = 2.2f;

        [Tooltip("Base melee damage before level scaling.")]
        [SerializeField] private float meleeDamage = 8f;

        [Tooltip("Extra random damage added on top of Melee Damage (0 = fixed).")]
        [SerializeField] private float meleeDamageRandomRange = 4f;

        [Tooltip("Per level above 1: damage *= (1 + (level-1) * this). Default 0.05 = +5%/level.")]
        [SerializeField] private float damageBonusPerLevel = 0.05f;

        [Tooltip("Seconds between melee hits.")]
        [SerializeField] private float meleeAttackCooldown = 1.4f;

        [Tooltip("Extra random delay after each hit: wait = Melee Interval + Random(0, this).")]
        [SerializeField] [Range(0f, 10f)] private float meleeIntervalVariation = 0.35f;

        [Header("Aim / Facing")]
        [SerializeField] private float aimHeightOffset = 0.5f;
        [SerializeField] private float faceTurnSpeed = 12f;

        [Header("Leash / Abandon")]
        [Tooltip("If owner is farther than this from the target, start the abandon timer.")]
        [SerializeField] private float ownerLeashDistance = 12f;
        [Tooltip("Seconds owner may stay beyond leash before pet stops melee and resumes follow-only.")]
        [SerializeField] private float abandonAfterSeconds = 6f;

        private PetController _pet;
        private CombatFocusController _playerFocus;
        private Transform _owner;
        private EnemyHealth _activeTarget;
        private EnemyHealth _assistTarget;
        private float _assistExpireTime;
        private float _nextThreatScanTime;
        private float _nextSwingTime;
        private float _abandonTimer;
        private bool _hadPlayerSideAggro;

        private const float AssistWindowSeconds = 6f;
        private const float ThreatScanInterval = 0.35f;

        public bool MeleeEnabled => meleeEnabled;
        public bool IsAttacking => meleeEnabled && _activeTarget != null && !_activeTarget.IsDead;

        private void Awake()
        {
            _pet = GetComponent<PetController>();
            ScheduleNextSwing(immediateReady: true);
        }

        private void OnEnable()
        {
            ClearAttack();
            ScheduleNextSwing(immediateReady: true);
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
            FaceTarget(_activeTarget.transform.position);

            if (distance > meleeEngageRange)
                return;

            if (Time.time < _nextSwingTime)
                return;

            if (TrySwing(_activeTarget))
                ScheduleNextSwing(immediateReady: false);
        }

        private bool CanOperate()
        {
            if (!meleeEnabled)
                return false;

            if (_pet == null)
                _pet = GetComponent<PetController>();

            if (_pet == null)
                return false;

            return _pet.IsOwned && _pet.CompanionActive;
        }

        private void CacheOwnerAndFocus()
        {
            Transform ownerTransform = _pet != null ? _pet.Owner : null;
            if (ownerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                    ownerTransform = player.transform;
            }

            if (ownerTransform != _owner)
            {
                _owner = ownerTransform;
                _playerFocus = ownerTransform != null ? ownerTransform.GetComponent<CombatFocusController>() : null;
            }
            else if (_playerFocus == null && _owner != null)
            {
                _playerFocus = _owner.GetComponent<CombatFocusController>();
            }
        }

        private EnemyHealth ResolvePlayerCurrentTarget()
        {
            if (_playerFocus != null)
            {
                EnemyHealth locked = _playerFocus.LockedTarget;
                if (IsUsableTarget(locked))
                    return locked;
            }

            if (IsUsableTarget(_assistTarget) && Time.time < _assistExpireTime)
                return _assistTarget;

            _assistTarget = null;

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
            float bestDistance = Mathf.Max(meleeEngageRange * 3f, ownerLeashDistance);

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

        private bool TrySwing(EnemyHealth target)
        {
            if (target == null)
                return false;

            Collider hitCollider = target.GetComponentInChildren<Collider>();
            if (hitCollider == null)
                hitCollider = target.GetComponent<Collider>();
            if (hitCollider == null)
                return false;

            Vector3 aimPoint = target.transform.position + Vector3.up * aimHeightOffset;
            Vector3 travel = aimPoint - transform.position;
            if (travel.sqrMagnitude < 0.0001f)
                travel = transform.forward;

            float damage = RollScaledDamage();
            CombatHitResolver.ApplyDirectHit(
                hitCollider,
                aimPoint,
                travel,
                damage,
                isCritical: false,
                owner: gameObject);

            return true;
        }

        private float RollScaledDamage()
        {
            float min = Mathf.Max(1f, meleeDamage);
            float max = Mathf.Max(min, meleeDamage + Mathf.Max(0f, meleeDamageRandomRange));
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

        private void ScheduleNextSwing(bool immediateReady)
        {
            float baseWait = Mathf.Max(0.05f, meleeAttackCooldown);
            if (immediateReady)
            {
                _nextSwingTime = Time.time + Mathf.Min(0.75f, baseWait);
                return;
            }

            float variation = Mathf.Clamp(meleeIntervalVariation, 0f, 10f);
            _nextSwingTime = Time.time + baseWait + Random.Range(0f, variation);
        }

        private void ClearAttack()
        {
            _activeTarget = null;
            _abandonTimer = 0f;
            _hadPlayerSideAggro = false;
        }

        /// <summary>Editor / Pet Manager: apply melee toggle + combat tuning.</summary>
        public void ConfigureSettings(
            bool enabled,
            float engageRange,
            float damage,
            float damageRandom,
            float damagePerLevel,
            float interval,
            float intervalVariation,
            float leashDistance,
            float abandonSeconds)
        {
            meleeEnabled = enabled;
            meleeEngageRange = Mathf.Max(0.25f, engageRange);
            meleeDamage = Mathf.Max(0f, damage);
            meleeDamageRandomRange = Mathf.Max(0f, damageRandom);
            damageBonusPerLevel = Mathf.Max(0f, damagePerLevel);
            meleeAttackCooldown = Mathf.Max(0.05f, interval);
            meleeIntervalVariation = Mathf.Clamp(intervalVariation, 0f, 10f);
            ownerLeashDistance = Mathf.Max(1f, leashDistance);
            abandonAfterSeconds = Mathf.Max(0.1f, abandonSeconds);
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
            meleeEngageRange = Mathf.Max(0.25f, meleeEngageRange);
            meleeDamage = Mathf.Max(0f, meleeDamage);
            meleeDamageRandomRange = Mathf.Max(0f, meleeDamageRandomRange);
            meleeAttackCooldown = Mathf.Max(0.05f, meleeAttackCooldown);
            meleeIntervalVariation = Mathf.Clamp(meleeIntervalVariation, 0f, 10f);
            ownerLeashDistance = Mathf.Max(1f, ownerLeashDistance);
            abandonAfterSeconds = Mathf.Max(0.1f, abandonAfterSeconds);
        }
#endif
    }
}
