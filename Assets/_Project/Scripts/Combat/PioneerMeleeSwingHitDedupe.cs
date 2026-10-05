using System.Collections.Generic;
using Invector.vMelee;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// One damage application per target root per Invector melee damage window (out-swing and return share a window).
    /// </summary>
    public static class PioneerMeleeSwingHitDedupe
    {
        private static int _trackedSwingId = -1;
        private static readonly HashSet<int> _targetRootsThisSwing = new HashSet<int>();

        public static bool TryAcceptHit(GameObject sender, GameObject hitColliderObject)
        {
            if (sender == null || hitColliderObject == null)
                return true;

            PioneerInvectorBootstrap bootstrap = PioneerInvectorBootstrap.Instance;
            if (bootstrap == null)
                return true;

            Transform playerRoot = bootstrap.transform.root;
            if (!sender.transform.IsChildOf(playerRoot) && sender.transform.root != playerRoot)
                return true;

            int swingId = PioneerMeleeDamageWindowTracker.CurrentSwingId;
            if (swingId != _trackedSwingId)
            {
                _trackedSwingId = swingId;
                _targetRootsThisSwing.Clear();
            }

            int targetRootId = hitColliderObject.transform.root.GetInstanceID();
            return _targetRootsThisSwing.Add(targetRootId);
        }
    }

    /// <summary>
    /// Increments <see cref="CurrentSwingId"/> when Invector enables melee damage on an equipped weapon.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PioneerMeleeDamageWindowTracker : MonoBehaviour
    {
        public static int CurrentSwingId { get; private set; }

        private const string StrongSwordBPath = "Attacks.StrongAttacks.SwordAttack.B";
        private static readonly int StrongSwordBHash = Animator.StringToHash(StrongSwordBPath);

        private vMeleeManager _meleeManager;
        private PioneerShooterMeleeInput _shooterInput;
        private Animator _animator;
        private bool _wasDamageActive;

        private void Awake()
        {
            _meleeManager = GetComponent<vMeleeManager>();
            _shooterInput = GetComponent<PioneerShooterMeleeInput>();
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (ShouldForceDisableWeaponHitboxes())
            {
                ForceDisableWeaponHitboxes();
                _wasDamageActive = false;
                return;
            }

            if (TryGateStrongReleaseDamage(out bool wantStrongDamage))
            {
                if (wantStrongDamage && !_wasDamageActive)
                    CurrentSwingId++;

                SetWeaponDamageActive(wantStrongDamage);
                _wasDamageActive = wantStrongDamage;
                return;
            }

            bool active = IsMeleeDamageActive();
            if (active && !_wasDamageActive)
                CurrentSwingId++;

            _wasDamageActive = active;
        }

        private bool TryGateStrongReleaseDamage(out bool wantActive)
        {
            wantActive = false;
            if (_shooterInput == null || !_shooterInput.IsStrongMeleeDamageActive)
                return false;

            if (!TryGetStrongReleaseNormalizedTime(out float normalizedTime))
                return false;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float start = profile != null ? profile.strongMeleeDamageStartNormalized : 0.72f;
            float end = profile != null ? profile.strongMeleeDamageEndNormalized : 0.98f;
            if (end < start)
            {
                float swap = start;
                start = end;
                end = swap;
            }

            float t = normalizedTime % 1f;
            wantActive = t >= start && t <= end;
            return true;
        }

        private bool TryGetStrongReleaseNormalizedTime(out float normalizedTime)
        {
            normalizedTime = 0f;
            if (_animator == null)
                return false;

            int layer = _animator.GetLayerIndex("FullBody");
            if (layer < 0)
                return false;

            if (IsStrongReleaseState(_animator.GetCurrentAnimatorStateInfo(layer), out normalizedTime))
                return true;

            if (_animator.IsInTransition(layer)
                && IsStrongReleaseState(_animator.GetNextAnimatorStateInfo(layer), out normalizedTime))
                return true;

            return false;
        }

        private static bool IsStrongReleaseState(AnimatorStateInfo state, out float normalizedTime)
        {
            normalizedTime = state.normalizedTime;
            return state.fullPathHash == StrongSwordBHash || state.IsName(StrongSwordBPath);
        }

        private void SetWeaponDamageActive(bool active)
        {
            if (_meleeManager == null)
                return;

            if (_meleeManager.rightWeapon != null
                && _meleeManager.rightWeapon.canApplyDamage != active)
            {
                _meleeManager.rightWeapon.SetActiveDamage(active);
            }

            if (_meleeManager.leftWeapon != null
                && _meleeManager.leftWeapon.canApplyDamage != active)
            {
                _meleeManager.leftWeapon.SetActiveDamage(active);
            }
        }

        private bool ShouldForceDisableWeaponHitboxes()
        {
            if (_shooterInput != null && _shooterInput.IsStrongChargePoseActive)
                return true;

            return false;
        }

        private void ForceDisableWeaponHitboxes()
        {
            if (_meleeManager == null)
                return;

            if (_meleeManager.rightWeapon != null && _meleeManager.rightWeapon.canApplyDamage)
                _meleeManager.rightWeapon.SetActiveDamage(false);

            if (_meleeManager.leftWeapon != null && _meleeManager.leftWeapon.canApplyDamage)
                _meleeManager.leftWeapon.SetActiveDamage(false);
        }

        private bool IsMeleeDamageActive()
        {
            if (_meleeManager == null)
                return false;

            if (_meleeManager.rightWeapon != null && _meleeManager.rightWeapon.canApplyDamage)
                return true;

            if (_meleeManager.leftWeapon != null && _meleeManager.leftWeapon.canApplyDamage)
                return true;

            return false;
        }
    }
}
