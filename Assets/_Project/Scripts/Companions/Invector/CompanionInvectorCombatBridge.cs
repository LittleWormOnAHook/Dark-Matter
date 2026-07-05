using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.AI;
using Project.Companions;
using Project.Data;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Drives Invector melee/shooter attacks for expedition companions.
    /// Outgoing damage is applied by Invector hitboxes/projectiles and scaled via
    /// <see cref="CompanionInvectorDamageBridge"/> — not by manual TakeDamage calls.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public class CompanionInvectorCombatBridge : MonoBehaviour
    {
        [SerializeField] private float defaultMeleeDuration = 0.85f;
        [SerializeField] private float defaultRangedDuration = 0.35f;
        [SerializeField] private float defaultUnarmedDuration = 0.55f;

        private vThirdPersonController _controller;
        private vMeleeManager _meleeManager;
        private vShooterManager _shooterManager;
        private CompanionInvectorLoadoutBridge _loadoutBridge;
        private CompanionCombatController _combatController;
        private Animator _animator;

        private static readonly int AttackIdHash = Animator.StringToHash("AttackID");
        private static readonly int WeakAttackHash = Animator.StringToHash("WeakAttack");
        private static readonly int MoveSetIdHash = Animator.StringToHash("MoveSet_ID");

        /// <summary>
        /// When true, CompanionCombatController must not apply parallel manual damage.
        /// </summary>
        public bool UsesInvectorDamageApplication => true;

        public bool IsMaintainingRangedAim { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<vThirdPersonController>();
            _meleeManager = GetComponent<vMeleeManager>();
            _shooterManager = GetComponent<vShooterManager>();
            _loadoutBridge = GetComponent<CompanionInvectorLoadoutBridge>();
            _combatController = GetComponent<CompanionCombatController>();
            _animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);
        }

        private void FixedUpdate()
        {
            UpdateRangedAimPose();
        }

        public bool TryBeginAttack(Transform target, ItemData weapon, out float duration)
        {
            duration = defaultMeleeDuration;

            if (weapon == null)
                return TryBeginUnarmedMeleeAttack(out duration);

            if (weapon.IsRangedWeapon)
                return TryBeginRangedAttack(target, weapon, out duration);

            if (weapon.itemType == ItemType.MeleeWeapon)
                return TryBeginMeleeAttack(weapon, out duration);

            return false;
        }

        private bool TryBeginUnarmedMeleeAttack(out float duration)
        {
            duration = defaultUnarmedDuration;
            if (_animator == null)
                return false;

            EnsureDrawnForCombat(null);
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

            EnsureDrawnForCombat(weapon);
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

            EnsureDrawnForCombat(weapon);
            SnapBodyToward(target);
            _loadoutBridge?.PulseRangedFirePose();

            if (_controller != null)
                _controller.isStrafing = true;

            Vector3 aimPoint = ResolveAimPoint(target);
            _shooterManager.Shoot(aimPoint, applyHipfirePrecision: false);
            EnemyNoiseEvents.RaiseNoise(transform.position, 12f, gameObject);

            // Animation beat only — real cadence is owned by CompanionCombatController cooldown.
            duration = defaultRangedDuration;
            return true;
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
            Collider body = target.GetComponentInChildren<Collider>();
            if (body != null && body.enabled)
                return body.bounds.center;

            return target.position + Vector3.up * 1.2f;
        }

        private void UpdateRangedAimPose()
        {
            bool shouldAim = ShouldMaintainRangedAim();
            IsMaintainingRangedAim = shouldAim;

            if (_loadoutBridge != null)
                _loadoutBridge.SyncRangedAimPose(shouldAim);

            if (_controller == null)
                return;

            _controller.isStrafing = shouldAim;
        }

        private bool ShouldMaintainRangedAim()
        {
            if (_loadoutBridge == null || !_loadoutBridge.IsDrawn)
                return false;

            ItemData weapon = _loadoutBridge.ActiveItem;
            if (weapon == null || !weapon.IsRangedWeapon)
                return false;

            if (_combatController == null)
                return false;

            var target = _combatController.CurrentTarget;
            return target != null && !target.IsDead;
        }

        private void EnsureDrawnForCombat(ItemData weapon)
        {
            if (_loadoutBridge == null)
                return;

            if (weapon == null || _loadoutBridge.ActiveItem == weapon)
                _loadoutBridge.SetDrawn(true);
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
    }
}
