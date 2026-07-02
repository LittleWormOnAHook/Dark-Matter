using ECM2;
using Project.AI;
using Project.Interaction;
using Project.Survival;
using UnityEngine;

namespace Project.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(MeleeCombatController))]
    [RequireComponent(typeof(SurvivalStats))]
    public class CombatFocusController : MonoBehaviour
    {
        [Header("Target Selection")]
        [SerializeField] private float focusRange = 3.5f;
        [SerializeField] private float acquireFacingAngle = 35f;
        [SerializeField] private float enemySearchInterval = 0.25f;

        [Header("Combat Context")]
        [SerializeField] private float attackContextSeconds = 1.35f;
        [SerializeField] private float damagedContextSeconds = 2f;

        [Header("Focus Rotation")]
        [SerializeField] private float yawSmoothLambda = 14f;
        [SerializeField] private float turnVariationDegrees = 3.5f;
        [SerializeField] private float turnVariationSpeed = 1.1f;

        [Header("Break Lock")]
        [SerializeField] private float breakLookAwayDegrees = 25f;
        [SerializeField] private float breakMoveInputThreshold = 0.6f;
        [SerializeField] private float breakMoveAwayDot = -0.2f;
        [SerializeField] private float lookAwayDecayRate = 0.35f;

        private PlayerController _player;
        private Character _character;
        private MeleeCombatController _melee;
        private SurvivalStats _survival;

        private EnemyHealth _lockedTarget;
        private Character.RotationMode _savedRotationMode;
        private float _lookAwayAccum;
        private float _focusReferenceYaw;
        private float _variationSeed;
        private float _nextEnemySearchTime;
        private EnemyHealth[] _cachedEnemies = System.Array.Empty<EnemyHealth>();

        public bool IsLocked => _lockedTarget != null;
        public EnemyHealth LockedTarget => _lockedTarget;
        public float FocusRange => focusRange;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _character = GetComponent<Character>();
            _melee = GetComponent<MeleeCombatController>();
            _survival = GetComponent<SurvivalStats>();
            _variationSeed = Random.Range(0f, 1000f);
        }

        public void UpdateFocus()
        {
            if (_player == null || _character == null)
                return;

            if (_melee != null && _melee.IsBlocking)
            {
                ReleaseLock();
                return;
            }

            if (_player.BlocksCombatInput || _player.IsGameplayPaused || (_survival != null && _survival.IsDead))
            {
                ReleaseLock();
                return;
            }

            RefreshEnemyCacheIfNeeded();

            bool inCombatContext = HasCombatContext();
            EnemyHealth nearest = FindNearestLivingEnemy();

            if (_lockedTarget != null)
            {
                if (!IsValidLockTarget(_lockedTarget, inCombatContext, nearest))
                    ReleaseLock();
                else
                    UpdateLockedFocus();
            }
            else if (inCombatContext && nearest != null && CanAcquireLock(nearest))
            {
                AcquireLock(nearest);
                UpdateLockedFocus();
            }
        }

        private bool HasCombatContext()
        {
            if (_melee != null && _melee.IsAttackInputActive)
                return true;

            if (_melee != null && Time.time - _melee.LastAttackTime <= attackContextSeconds)
                return true;

            if (_survival != null && Time.time - _survival.LastDamageTime <= damagedContextSeconds)
                return true;

            return false;
        }

        private void RefreshEnemyCacheIfNeeded()
        {
            if (Time.time < _nextEnemySearchTime)
                return;

            _nextEnemySearchTime = Time.time + enemySearchInterval;
            _cachedEnemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
        }

        private EnemyHealth FindNearestLivingEnemy()
        {
            Vector3 origin = transform.position;
            float bestDistance = focusRange;
            EnemyHealth best = null;

            for (int i = 0; i < _cachedEnemies.Length; i++)
            {
                EnemyHealth enemy = _cachedEnemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                float distance = PlanarDistance(origin, enemy.transform.position);
                if (distance > bestDistance)
                    continue;

                bestDistance = distance;
                best = enemy;
            }

            return best;
        }

        private bool CanAcquireLock(EnemyHealth enemy)
        {
            float enemyYaw = GetYawToward(enemy.transform.position);
            if (Mathf.Abs(Mathf.DeltaAngle(_player.CameraYaw, enemyYaw)) <= acquireFacingAngle)
                return true;

            float characterYaw = transform.eulerAngles.y;
            return Mathf.Abs(Mathf.DeltaAngle(characterYaw, enemyYaw)) <= acquireFacingAngle;
        }

        private bool IsValidLockTarget(EnemyHealth enemy, bool inCombatContext, EnemyHealth nearest)
        {
            if (enemy == null || enemy.IsDead)
                return false;

            if (!inCombatContext)
                return false;

            if (PlanarDistance(transform.position, enemy.transform.position) > focusRange)
                return false;

            if (nearest != null && nearest != enemy)
            {
                float lockedDistance = PlanarDistance(transform.position, enemy.transform.position);
                float nearestDistance = PlanarDistance(transform.position, nearest.transform.position);
                if (nearestDistance + 0.35f < lockedDistance)
                    return false;
            }

            if (_lookAwayAccum >= breakLookAwayDegrees)
                return false;

            if (IsMovingAwayFromEnemy(enemy))
                return false;

            return true;
        }

        private void AcquireLock(EnemyHealth enemy)
        {
            _lockedTarget = enemy;
            _lookAwayAccum = 0f;
            _focusReferenceYaw = GetYawToward(enemy.transform.position);
            _savedRotationMode = _character.rotationMode;
            _character.rotationMode = Character.RotationMode.OrientRotationToViewDirection;
        }

        private void ReleaseLock()
        {
            if (_lockedTarget == null)
                return;

            _lockedTarget = null;
            _lookAwayAccum = 0f;
            _character.rotationMode = _savedRotationMode;
        }

        private void UpdateLockedFocus()
        {
            if (_lockedTarget == null)
                return;

            TrackLookAwayBreak();

            if (_melee != null && _melee.IsBlocking)
                return;

            if (_lookAwayAccum >= breakLookAwayDegrees || IsMovingAwayFromEnemy(_lockedTarget))
            {
                ReleaseLock();
                return;
            }

            Vector3 enemyPosition = _lockedTarget.transform.position;
            float idealYaw = GetYawToward(enemyPosition);
            _focusReferenceYaw = idealYaw;
            float targetYaw = idealYaw + GetTurnVariation();
            _player.ApplyCombatFocusYaw(targetYaw, yawSmoothLambda);
        }

        private void TrackLookAwayBreak()
        {
            float lookDelta = _player.LastLookYawDelta;
            if (Mathf.Abs(lookDelta) < 0.001f)
            {
                _lookAwayAccum = Mathf.Max(0f, _lookAwayAccum - lookAwayDecayRate * Time.deltaTime);
                return;
            }

            float currentYaw = _player.CameraYaw;
            float previousYaw = MathLib.ClampAngle(currentYaw - lookDelta, -180f, 180f);
            float previousOffset = Mathf.Abs(Mathf.DeltaAngle(previousYaw, _focusReferenceYaw));
            float currentOffset = Mathf.Abs(Mathf.DeltaAngle(currentYaw, _focusReferenceYaw));

            if (currentOffset > previousOffset)
                _lookAwayAccum += Mathf.Abs(lookDelta);
            else
                _lookAwayAccum = Mathf.Max(0f, _lookAwayAccum - Mathf.Abs(lookDelta) * lookAwayDecayRate);
        }

        private bool IsMovingAwayFromEnemy(EnemyHealth enemy)
        {
            Vector3 moveDirection = _player.GetCameraRelativeMoveDirection();
            if (moveDirection.sqrMagnitude < breakMoveInputThreshold * breakMoveInputThreshold)
                return false;

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude < 0.0001f)
                return false;

            toEnemy.Normalize();
            return Vector3.Dot(moveDirection.normalized, toEnemy) <= breakMoveAwayDot;
        }

        private float GetTurnVariation()
        {
            float noise = Mathf.PerlinNoise(_variationSeed, Time.time * turnVariationSpeed);
            return (noise * 2f - 1f) * turnVariationDegrees;
        }

        private float GetYawToward(Vector3 worldPosition)
        {
            Vector3 toTarget = worldPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return _player.CameraYaw;

            toTarget.Normalize();
            return Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
