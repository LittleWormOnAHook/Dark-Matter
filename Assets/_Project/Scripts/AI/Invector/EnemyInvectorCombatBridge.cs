using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.AI;
using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Drives Invector melee/shooter attacks for humanoid enemies. Damage flows through Invector hitboxes.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public class EnemyInvectorCombatBridge : MonoBehaviour
    {
        [SerializeField] private float defaultMeleeDuration = 0.85f;
        [SerializeField] private float defaultRangedDuration = 0.35f;
        [SerializeField] private float defaultUnarmedDuration = 0.55f;
        [Tooltip("Distance at which ranged enemies stop chasing and start shooting.")]
        [SerializeField] private float rangedEngageRange = 12f;
        [Tooltip("Seconds the aim pose is held after the enemy leaves a ranged engagement.")]
        [SerializeField] private float aimHoldDuration = 1.5f;
        [Tooltip("Max degrees the chest bone tilts up/down to track the target vertically while aiming. Keeps the rifle pointed at the player's torso rather than straight ahead.")]
        [SerializeField] [Range(0f, 40f)] private float aimChestPitchLimit = 25f;
        [Tooltip("How fast the chest pitch blends in and out (degrees per second).")]
        [SerializeField] private float aimChestPitchSpeed = 90f;
        [Tooltip("0 = perfect aim every shot, 1 = always misses. Scales a random lateral+vertical offset applied to each shot's aim point.")]
        [SerializeField] [Range(0f, 1f)] private float missRate = 0.25f;
        [SerializeField] private ItemData enemyPistolAmmo;
        [SerializeField] private ItemData enemyRifleAmmo;

        private const string StandardAmmoPath = "Assets/_Project/Data/Items/ammo/Standard.asset";
        private const string PlasmaAmmoPath = "Assets/_Project/Data/Items/ammo/Plasma.asset";

        private vThirdPersonController _controller;
        private vMeleeManager _meleeManager;
        private vShooterManager _shooterManager;
        private EnemyInvectorLoadoutBridge _loadoutBridge;
        private EnemyInvectorBootstrap _bootstrap;
        private EnemyCombat _enemyCombat;
        private EnemyAiController _aiController;
        private Animator _animator;
        private bool _isAimStanceActive;
        private float _aimHoldTimer;
        private float _chestPitchCurrent;
        private Transform _chestBone;

        private static readonly int AttackIdHash    = Animator.StringToHash("AttackID");
        private static readonly int WeakAttackHash  = Animator.StringToHash("WeakAttack");
        private static readonly int MoveSetIdHash   = Animator.StringToHash("MoveSet_ID");
        private static readonly int IsAimingHash    = Animator.StringToHash("IsAiming");
        private static readonly int CanAimHash      = Animator.StringToHash("CanAim");
        private static readonly int UpperBodyIdHash = Animator.StringToHash("UpperBody_ID");
        private static readonly int ShotIdHash      = Animator.StringToHash("Shot_ID");

        public bool UsesInvectorDamageApplication => true;
        public bool LastAttackWasRanged { get; private set; }

        private void Awake()
        {
            _bootstrap = GetComponent<EnemyInvectorBootstrap>();
            _controller = GetComponent<vThirdPersonController>();
            _meleeManager = GetComponent<vMeleeManager>();
            _shooterManager = GetComponent<vShooterManager>();
            _loadoutBridge = GetComponent<EnemyInvectorLoadoutBridge>();
            _enemyCombat = GetComponent<EnemyCombat>();
            _aiController = GetComponent<EnemyAiController>();
            _animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);

            // Disable any leftover EnemyWeaponAimIK — the UpperBody animator layer handles
            // the rifle aim pose; procedural bone rotation fights the animator and breaks the pose.
            EnemyWeaponAimIK aimIK = GetComponent<EnemyWeaponAimIK>();
            if (aimIK != null) aimIK.enabled = false;

            _bootstrap?.EnsureInvectorInitialized();
            EnsureEnemyAmmoReferences();
        }

        private void EnsureEnemyAmmoReferences()
        {
#if UNITY_EDITOR
            if (enemyPistolAmmo == null)
                enemyPistolAmmo = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(StandardAmmoPath);
            if (enemyRifleAmmo == null)
                enemyRifleAmmo = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(PlasmaAmmoPath);
#endif
        }

        private void Update()
        {
            UpdateRangedAimStance();
        }

        // Runs after the Animator so we can post-process the chest bone pitch without
        // fighting the animator each frame. Only active while the aim stance is held.
        private void LateUpdate()
        {
            UpdateChestAimPitch();
        }

        // Keeps isStrafing, UpperBody_ID, IsAiming and CanAim in sync with the AI's engagement
        // state. The aim pose is held for aimHoldDuration seconds after the engagement ends so
        // the enemy doesn't snap out of the aiming animation immediately after each shot.
        private void UpdateRangedAimStance()
        {
            if (_controller == null || _animator == null) return;

            bool inEngagement = _aiController != null && _aiController.IsInRangedEngagement;

            if (inEngagement)
            {
                // Reset the hold timer whenever actively engaged — it only counts down after.
                _aimHoldTimer = aimHoldDuration;
            }
            else if (_aimHoldTimer > 0f)
            {
                _aimHoldTimer -= Time.deltaTime;
            }

            bool shouldAim = inEngagement || _aimHoldTimer > 0f;

            if (shouldAim == _isAimStanceActive) return;
            _isAimStanceActive = shouldAim;

            if (shouldAim)
            {
                float weaponId = ResolveUpperBodyId();
                _animator.SetFloat(UpperBodyIdHash, weaponId);
                _animator.SetFloat(ShotIdHash,      weaponId);
                _animator.SetBool(IsAimingHash, true);
                _animator.SetBool(CanAimHash,   true);
                _controller.isStrafing = true;
            }
            else
            {
                _animator.SetBool(IsAimingHash, false);
                _animator.SetBool(CanAimHash,   false);
                _controller.isStrafing = false;
            }
        }

        // Post-animator chest pitch so the weapon visually tracks the target vertically.
        // Only the chest bone is rotated (no spine) to avoid the full-body lean seen previously.
        private void UpdateChestAimPitch()
        {
            if (_animator == null) return;

            if (_chestBone == null)
                _chestBone = _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (_chestBone == null) return;

            float targetPitch = 0f;

            if (_isAimStanceActive)
            {
                Transform target = _enemyCombat?.CurrentTarget;
                if (target != null)
                {
                    Vector3 aimOrigin = _chestBone.position;
                    Vector3 toTarget  = ResolveAimPoint(target) - aimOrigin;
                    if (toTarget.sqrMagnitude > 0.01f)
                    {
                        float vertical   = toTarget.y;
                        float horizontal = new Vector2(toTarget.x, toTarget.z).magnitude;
                        float angle      = Mathf.Atan2(vertical, horizontal) * Mathf.Rad2Deg;
                        targetPitch      = Mathf.Clamp(angle, -aimChestPitchLimit, aimChestPitchLimit);
                    }
                }
            }

            _chestPitchCurrent = Mathf.MoveTowards(
                _chestPitchCurrent, targetPitch, aimChestPitchSpeed * Time.deltaTime);

            if (Mathf.Abs(_chestPitchCurrent) > 0.1f)
                _chestBone.rotation = Quaternion.AngleAxis(-_chestPitchCurrent, transform.right) * _chestBone.rotation;
        }

        public float RangedEngageRange => rangedEngageRange;

        public bool HasRangedWeaponEquipped()
        {
            ItemData weapon = _loadoutBridge != null ? _loadoutBridge.ActiveItem : null;
            return weapon != null && weapon.IsRangedWeapon;
        }

        public bool IsArmedRangedPreferred()
        {
            return HasRangedWeaponEquipped()
                && _loadoutBridge != null
                && _loadoutBridge.PrefersRangedAtRange;
        }

        public bool TryBeginBlock(float blockDuration, out float duration)
        {
            duration = blockDuration > 0f ? blockDuration : defensiveBlockDurationFallback;
            if (_controller == null || _animator == null)
                return false;

            _controller.isStrafing = true;
            _animator.SetBool(vAnimatorParameters.IsBlocking, true);
            return true;
        }

        public void EndBlock()
        {
            if (_animator != null)
                _animator.SetBool(vAnimatorParameters.IsBlocking, false);

            if (_controller != null)
                _controller.isStrafing = false;
        }

        public bool TryBeginDodgeRoll(Transform threat, out float duration)
        {
            duration = 0.65f;
            if (_controller == null || threat == null)
                return false;

            Vector3 toThreat = threat.position - transform.position;
            toThreat.y = 0f;
            if (toThreat.sqrMagnitude < 0.01f)
                toThreat = transform.forward;
            toThreat.Normalize();

            Vector3 away = -toThreat;
            Vector3 right = Vector3.Cross(Vector3.up, toThreat);
            if (right.sqrMagnitude < 0.0001f)
                right = transform.right;
            right.Normalize();

            // Side and backward only — never roll toward the threat.
            int pick = Random.Range(0, 3);
            Vector3 rollDir = pick switch
            {
                0 => away,
                1 => right,
                _ => -right,
            };

            if (Vector3.Dot(rollDir, toThreat) > 0.01f)
                rollDir = away;

            rollDir.Normalize();
            _controller.input = transform.InverseTransformDirection(rollDir);
            _controller.moveDirection = rollDir;
            _controller.Roll();
            return true;
        }

        private const float defensiveBlockDurationFallback = 1.2f;

        public bool TryBeginAttack(Transform target, out float duration)
        {
            duration = defaultMeleeDuration;
            if (target == null)
                return false;

            ItemData weapon = ResolveWeaponForTarget(target);
            if (weapon == null)
                return TryBeginUnarmedMeleeAttack(out duration);

            if (weapon.IsRangedWeapon)
                return TryBeginRangedAttack(target, weapon, out duration);

            if (weapon.itemType == ItemType.MeleeWeapon)
                return TryBeginMeleeAttack(weapon, out duration);

            return false;
        }

        private ItemData ResolveWeaponForTarget(Transform target)
        {
            float meleeRange = _enemyCombat != null ? _enemyCombat.AttackRange : 1.8f;
            float distance = HorizontalDistance(transform.position, target.position);
            if (_loadoutBridge != null)
                return _loadoutBridge.ResolveWeaponForTargetDistance(distance, meleeRange);

            return null;
        }

        private bool TryBeginUnarmedMeleeAttack(out float duration)
        {
            duration = defaultUnarmedDuration;
            if (_animator == null)
                return false;

            LastAttackWasRanged = false;
            SyncMeleeAnimatorParams(useWeaponMoveSet: false);
            _animator.SetInteger(AttackIdHash, 0);
            _animator.SetTrigger(WeakAttackHash);
            return true;
        }

        private bool TryBeginMeleeAttack(ItemData weapon, out float duration)
        {
            duration = defaultMeleeDuration;
            if (_animator == null || _meleeManager == null)
                return false;

            LastAttackWasRanged = false;
            _loadoutBridge?.EquipSpecificWeapon(weapon);
            SyncMeleeAnimatorParams(useWeaponMoveSet: true);
            _animator.SetInteger(AttackIdHash, _meleeManager.GetAttackID());
            _animator.SetTrigger(WeakAttackHash);
            duration = ResolveMeleeDuration(weapon);
            EnemyNoiseEvents.RaiseNoise(transform.position, 5f, gameObject);
            return true;
        }

        private bool TryBeginRangedAttack(Transform target, ItemData weapon, out float duration)
        {
            duration = defaultRangedDuration;
            if (_shooterManager == null || target == null)
                return false;

            LastAttackWasRanged = true;

            _loadoutBridge?.EquipSpecificWeapon(weapon);
            SnapBodyToward(target);

            // Ensure both UpperBody and Shot layers use the correct weapon-type animation.
            if (_animator != null)
            {
                float weaponId = ResolveUpperBodyId();
                _animator.SetFloat(UpperBodyIdHash, weaponId);
                _animator.SetFloat(ShotIdHash,      weaponId);
            }

            PrepareShooterWeaponForFire();
            Vector3 aimPoint = ApplyMissOffset(ResolveAimPoint(target), target);
            _shooterManager.Shoot(aimPoint, applyHipfirePrecision: false);
            SpawnRangedProjectile(weapon, aimPoint);
            EnemyNoiseEvents.RaiseNoise(transform.position, 12f, gameObject);

            return true;
        }

        private void PrepareShooterWeaponForFire()
        {
            vShooterWeapon weapon = _shooterManager != null ? _shooterManager.CurrentWeapon : null;
            if (weapon == null)
                return;

            weapon.projectile = null;
            weapon.fireClip = null;
            weapon.emittShurykenParticle = null;
            weapon.lightOnShot = null;
            weapon.isInfinityAmmo = true;
            weapon.dontUseReload = true;

            int clip = weapon.clipSize > 0 ? weapon.clipSize : 999;
            if (weapon.ammo <= 0)
                weapon.AddAmmo(clip);
        }

        private Transform fallbackMuzzle;

        private void SpawnRangedProjectile(ItemData weapon, Vector3 aimPoint)
        {
            if (weapon == null || !weapon.IsRangedWeapon)
                return;

            Transform muzzle = ResolveProjectileMuzzle();
            if (muzzle == null)
                muzzle = EnsureFallbackMuzzle();

            Vector3 direction = aimPoint - muzzle.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = muzzle.forward;
            direction.Normalize();

            ItemData ammoItem = ResolveEnemyAmmoItem(weapon);
            float damage = ResolveRangedProjectileDamage(weapon, ammoItem);
            CombatProjectileSpawner.Spawn(
                gameObject,
                muzzle,
                weapon,
                ammoItem,
                direction,
                weapon.projectileSpreadDegrees,
                damage);
        }

        private ItemData ResolveEnemyAmmoItem(ItemData weapon)
        {
            if (weapon == null)
                return null;

            // Enemies use Standard for pistols and Plasma for rifles.
            if (weapon.weaponGrip == WeaponGrip.TwoHanded)
                return enemyRifleAmmo != null ? enemyRifleAmmo : weapon.defaultAmmoItem;

            return enemyPistolAmmo != null ? enemyPistolAmmo : weapon.defaultAmmoItem;
        }

        private float ResolveRangedProjectileDamage(ItemData weaponItem, ItemData ammoItem)
        {
            float rolled = ammoItem != null ? ammoItem.RollRangedDamage() : weaponItem.RollRangedDamage();
            if (_enemyCombat != null)
                rolled = Mathf.Max(rolled, _enemyCombat.AttackDamage);

            return Mathf.Max(1f, rolled);
        }

        private Transform ResolveProjectileMuzzle()
        {
            vShooterWeapon shooterWeapon = _shooterManager != null ? _shooterManager.CurrentWeapon : null;
            if (shooterWeapon != null && shooterWeapon.muzzle != null)
                return shooterWeapon.muzzle;

            GameObject drawn = _loadoutBridge != null ? _loadoutBridge.ActiveDrawnInstance : null;
            if (drawn != null)
            {
                vShooterWeapon nestedWeapon = drawn.GetComponentInChildren<vShooterWeapon>(true);
                if (nestedWeapon != null && nestedWeapon.muzzle != null)
                    return nestedWeapon.muzzle;

                ItemData activeItem = _loadoutBridge != null ? _loadoutBridge.ActiveItem : null;
                string socketName = activeItem != null && !string.IsNullOrWhiteSpace(activeItem.muzzleSocketName)
                    ? activeItem.muzzleSocketName
                    : "Muzzle";

                Transform socket = drawn.transform.Find(socketName);
                if (socket == null)
                    socket = FindDeepChild(drawn.transform, socketName);

                if (socket != null)
                    return socket;

                return drawn.transform;
            }

            return EnsureFallbackMuzzle();
        }

        private Transform EnsureFallbackMuzzle()
        {
            if (fallbackMuzzle == null)
            {
                GameObject host = new GameObject("EnemyProjectileMuzzleFallback");
                fallbackMuzzle = host.transform;
                fallbackMuzzle.SetParent(transform, false);
            }

            fallbackMuzzle.position = transform.position + Vector3.up * 1.35f + transform.forward * 0.45f;
            fallbackMuzzle.rotation = transform.rotation;
            return fallbackMuzzle;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        // Returns the UpperBody_ID / Shot_ID value for the active ranged weapon.
        // Matches the "Aiming Fire Weapon" and Shot blend tree thresholds:
        //   1 = pistol (one-handed)
        //   2 = rifle / shotgun (two-handed)
        private float ResolveUpperBodyId()
        {
            ItemData weapon = _loadoutBridge?.ActiveItem;
            if (weapon == null) return 1f;
            return weapon.weaponGrip == WeaponGrip.TwoHanded ? 2f : 1f;
        }

        private void SnapBodyToward(Transform target)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
                return;

            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        private static Vector3 ResolveAimPoint(Transform target)
        {
            // Prefer the root CapsuleCollider (character controller body) — its bounds.center
            // sits at mid-torso. Avoid trigger colliders and child hitboxes.
            CapsuleCollider capsule = target.GetComponent<CapsuleCollider>();
            if (capsule != null && capsule.enabled && !capsule.isTrigger)
                return capsule.bounds.center;

            // Fallback: scan children for a non-trigger capsule (some rigs put it one level down).
            CapsuleCollider[] caps = target.GetComponentsInChildren<CapsuleCollider>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (caps[i].enabled && !caps[i].isTrigger)
                    return caps[i].bounds.center;
            }

            // Last resort: fixed offset that lands at roughly mid-chest height.
            return target.position + Vector3.up * 1.0f;
        }

        // Decides whether the shot misses and deflects the aim point accordingly.
        // A "miss" rolls against missRate; if it hits, a smaller natural spread is still applied.
        private Vector3 ApplyMissOffset(Vector3 aimPoint, Transform target)
        {
            if (missRate <= 0f) return aimPoint;

            float distance = HorizontalDistance(transform.position, target.position);
            bool isMiss = Random.value < missRate;

            if (isMiss)
            {
                // Guarantee the shot clears the target by pushing well outside body width (~0.5 m).
                float deflect = Mathf.Lerp(0.6f, 2f, missRate) * Mathf.Max(1f, distance / 10f);
                Vector2 dir   = Random.insideUnitCircle.normalized * deflect;
                EnemyFloatingText.ShowMiss(target);
                return aimPoint + new Vector3(dir.x, dir.y, 0f);
            }

            // Small natural jitter even on hit shots.
            float spread = 0.08f * Mathf.Max(1f, distance / 10f);
            Vector2 jitter = Random.insideUnitCircle * spread;
            return aimPoint + new Vector3(jitter.x, jitter.y, 0f);
        }

        private void SyncMeleeAnimatorParams(bool useWeaponMoveSet)
        {
            if (_animator == null || _meleeManager == null)
                return;

            float moveSetId = useWeaponMoveSet ? _meleeManager.GetMoveSetID() : 0f;
            _animator.SetFloat(MoveSetIdHash, moveSetId);
        }

        private float ResolveMeleeDuration(ItemData weapon)
        {
            float speed = weapon != null ? weapon.ResolveAttackAnimationSpeed() : 0.95f;
            return defaultMeleeDuration / Mathf.Max(0.35f, speed);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
