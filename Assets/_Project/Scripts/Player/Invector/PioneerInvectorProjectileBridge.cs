using System.Collections;
using Invector.vShooter;
using Project.Combat;
using Project.Data;
using Project.Inventory;
using Project.Player;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Unifies player ranged fire onto the same CombatProjectile pipeline companions and enemies
    /// use. Invector still owns aim, animation, and the trigger cycle; this bridge applies ammo
    /// profile fire-rate / burst / damage stats and spawns our projectile.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PioneerInvectorBootstrap))]
    [RequireComponent(typeof(WeaponAmmoState))]
    [RequireComponent(typeof(EquipmentController))]
    public class PioneerInvectorProjectileBridge : MonoBehaviour
    {
        private PioneerInvectorBootstrap _bootstrap;
        private WeaponAmmoState _ammoState;
        private EquipmentController _equipment;
        private vShooterManager _shooterManager;
        private PioneerInvectorAmmoBridge _ammoBridge;
        private PioneerInvectorInputBridge _inputBridge;
        private PioneerInvectorWeaponBridge _weaponBridge;
        private PlayerController _player;
        private Coroutine _burstRoutine;
        private bool _burstActive;

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
            _ammoState = GetComponent<WeaponAmmoState>();
            _equipment = GetComponent<EquipmentController>();
            _shooterManager = GetComponent<vShooterManager>();
            _ammoBridge = GetComponent<PioneerInvectorAmmoBridge>();
            _inputBridge = GetComponent<PioneerInvectorInputBridge>();
            _weaponBridge = GetComponent<PioneerInvectorWeaponBridge>();
            _player = GetComponent<PlayerController>();
        }

        private void OnEnable()
        {
            if (_shooterManager != null)
                _shooterManager.onShot.AddListener(HandleShot);
        }

        private void OnDisable()
        {
            if (_shooterManager != null)
                _shooterManager.onShot.RemoveListener(HandleShot);
            StopBurst();
        }

        private void HandleShot(vShooterWeapon invectorWeapon)
        {
            if (_burstActive)
                return;

            if (!TryPrepareShot(invectorWeapon, out ItemData weaponItem, out ItemData ammoItem, out Transform muzzle))
                return;

            PioneerInvectorRecoilUtility.ApplyRangedTiming(invectorWeapon, weaponItem, ammoItem);

            if (_ammoBridge != null && !_ammoBridge.TryProcessShotAmmo())
                return;

            FireResolvedRound(invectorWeapon, weaponItem, ammoItem, muzzle);

            int burst = DMRangedAmmoStats.ResolveBurstCount(weaponItem, ammoItem);
            if (burst <= 1)
                return;

            StopBurst();
            _burstRoutine = StartCoroutine(ContinueBurst(invectorWeapon, weaponItem, ammoItem, muzzle, burst - 1));
        }

        private IEnumerator ContinueBurst(
            vShooterWeapon invectorWeapon,
            ItemData weaponItem,
            ItemData ammoItem,
            Transform muzzle,
            int remaining)
        {
            _burstActive = true;
            try
            {
                float interval = 1f / DMRangedAmmoStats.ResolveBurstFireRate(weaponItem, ammoItem);
                for (int i = 0; i < remaining; i++)
                {
                    yield return new WaitForSeconds(interval);
                    if (invectorWeapon == null || weaponItem == null)
                        yield break;
                    if (_ammoBridge != null && !_ammoBridge.TryProcessShotAmmo())
                        yield break;

                    FireResolvedRound(invectorWeapon, weaponItem, ammoItem, muzzle);
                    if (_shooterManager != null)
                        PioneerInvectorRecoilUtility.ApplyPlayerShotRecoil(_shooterManager, weaponItem, ammoItem);
                }
            }
            finally
            {
                _burstActive = false;
                _burstRoutine = null;
            }
        }

        private void StopBurst()
        {
            if (_burstRoutine != null)
            {
                StopCoroutine(_burstRoutine);
                _burstRoutine = null;
            }

            _burstActive = false;
        }

        private bool TryPrepareShot(
            vShooterWeapon invectorWeapon,
            out ItemData weaponItem,
            out ItemData ammoItem,
            out Transform muzzle)
        {
            weaponItem = null;
            ammoItem = null;
            muzzle = null;

            if (_bootstrap != null && !_bootstrap.IsActive)
                return false;
            if (invectorWeapon == null || _equipment == null)
                return false;

            invectorWeapon.projectile = null;

            weaponItem = _equipment.DrawnWeaponItem != null
                ? _equipment.DrawnWeaponItem
                : _equipment.EquippedItem;

            if (weaponItem != null && weaponItem.isMiningTool)
            {
                invectorWeapon.fireClip = null;
                invectorWeapon.emittShurykenParticle = null;
                invectorWeapon.lightOnShot = null;
                invectorWeapon.isInfinityAmmo = true;
                PioneerInvectorRecoilUtility.ZeroWeaponRecoil(invectorWeapon);
                return false;
            }

            invectorWeapon.fireClip = null;
            invectorWeapon.emittShurykenParticle = null;
            invectorWeapon.lightOnShot = null;
            invectorWeapon.isInfinityAmmo = true;
            invectorWeapon.dontUseReload = false;
            if (invectorWeapon.reloadSource != null &&
                invectorWeapon.gameObject.activeInHierarchy &&
                !invectorWeapon.reloadSource.enabled)
            {
                invectorWeapon.reloadSource.enabled = true;
            }
            else if (invectorWeapon.source != null &&
                     invectorWeapon.gameObject.activeInHierarchy &&
                     !invectorWeapon.source.enabled)
            {
                invectorWeapon.source.enabled = true;
            }

            PioneerInvectorRecoilUtility.ZeroWeaponRecoil(invectorWeapon);

            if (weaponItem == null || !weaponItem.IsRangedWeapon)
                return false;

            ammoItem = _ammoState != null
                ? _ammoState.GetLoadedAmmoItem(_equipment.ActiveWeaponHotbarSlot)
                : null;

            if (_weaponBridge != null &&
                _weaponBridge.TryGetActiveDrawnMuzzle(weaponItem, out Transform drawnMuzzle) &&
                drawnMuzzle != null)
            {
                muzzle = drawnMuzzle;
            }
            else
            {
                muzzle = invectorWeapon.muzzle;
            }

            return muzzle != null;
        }

        private void FireResolvedRound(
            vShooterWeapon invectorWeapon,
            ItemData weaponItem,
            ItemData ammoItem,
            Transform muzzle)
        {
            if (muzzle == null || weaponItem == null)
                return;

            bool isAiming = _inputBridge != null && _inputBridge.IsAiming;
            float maxRange = DMRangedAmmoStats.ResolveRange(weaponItem, ammoItem);

            Camera cam = ResolveGameplayCamera();
            float aimDistance = maxRange;
            Vector3 reticleAim = muzzle.forward;
            if (cam != null)
            {
                reticleAim = RangedFireSolver.ResolveMuzzleToReticleDirection(
                    cam,
                    muzzle.position,
                    maxRange,
                    out aimDistance,
                    weaponMuzzle: muzzle);
            }

            Vector3 direction = RangedFireSolver.ResolveDirection(
                reticleAim,
                muzzle.forward,
                isAiming,
                weaponItem.hipFireMaxDeviationDegrees);

            float spread = RangedFireSolver.ResolveEffectiveSpreadDegrees(
                weaponItem,
                ammoItem,
                isAiming,
                aimDistance,
                applyPlayerSkillBonus: true);

            CombatProjectileSpawner.Spawn(gameObject, muzzle, weaponItem, ammoItem, direction, spread);
        }

        private Camera ResolveGameplayCamera()
        {
            if (_player != null && _player.GameplayCamera != null)
                return _player.GameplayCamera;
            return Camera.main;
        }
    }
}
