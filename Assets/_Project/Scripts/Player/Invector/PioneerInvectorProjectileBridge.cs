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

            BurstAimLock aimLock = default;
            int burst = DMRangedAmmoStats.ResolveBurstCount(weaponItem, ammoItem);
            FireResolvedRound(invectorWeapon, weaponItem, ammoItem, muzzle, ref aimLock, captureLock: burst > 1);

            if (burst <= 1)
                return;

            StopBurst();
            _burstRoutine = StartCoroutine(ContinueBurst(invectorWeapon, weaponItem, ammoItem, muzzle, burst - 1, aimLock));
        }

        private IEnumerator ContinueBurst(
            vShooterWeapon invectorWeapon,
            ItemData weaponItem,
            ItemData ammoItem,
            Transform muzzle,
            int remaining,
            BurstAimLock aimLock)
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

                    FireResolvedRound(invectorWeapon, weaponItem, ammoItem, muzzle, ref aimLock, captureLock: false);
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

        /// <summary>
        /// First burst pellet captures muzzle spawn + aim; later pellets reuse that lock
        /// so recoil kick does not walk projectile origins across the burst.
        /// </summary>
        private struct BurstAimLock
        {
            public bool Valid;
            public Vector3 SpawnPosition;
            public Vector3 BaseDirection;
            public float SpreadDegrees;
        }

        private void FireResolvedRound(
            vShooterWeapon invectorWeapon,
            ItemData weaponItem,
            ItemData ammoItem,
            Transform muzzle,
            ref BurstAimLock aimLock,
            bool captureLock)
        {
            if (muzzle == null || weaponItem == null)
                return;

            Vector3 direction;
            float spread;
            Vector3? lockedSpawn = null;

            if (aimLock.Valid)
            {
                direction = aimLock.BaseDirection;
                spread = aimLock.SpreadDegrees;
                lockedSpawn = aimLock.SpawnPosition;
            }
            else
            {
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

                direction = RangedFireSolver.ResolveDirection(
                    reticleAim,
                    muzzle.forward,
                    isAiming,
                    weaponItem.hipFireMaxDeviationDegrees);

                spread = RangedFireSolver.ResolveEffectiveSpreadDegrees(
                    weaponItem,
                    ammoItem,
                    isAiming,
                    aimDistance,
                    applyPlayerSkillBonus: true);

                if (captureLock)
                {
                    Vector3 barrelForward = RangedFireSolver.ResolveWeaponAimForward(muzzle);
                    Vector3 spawnPosition = muzzle.position;
                    if (barrelForward.sqrMagnitude > 0.0001f)
                        spawnPosition += barrelForward * RangedFireSolver.ProjectileSpawnSkin;

                    aimLock = new BurstAimLock
                    {
                        Valid = true,
                        SpawnPosition = spawnPosition,
                        BaseDirection = direction,
                        SpreadDegrees = spread
                    };
                    lockedSpawn = spawnPosition;
                }
            }

            CombatProjectileSpawner.Spawn(
                gameObject,
                muzzle,
                weaponItem,
                ammoItem,
                direction,
                spread,
                lockedSpawnPosition: lockedSpawn);
        }

        private Camera ResolveGameplayCamera()
        {
            if (_player != null && _player.GameplayCamera != null)
                return _player.GameplayCamera;
            return Camera.main;
        }
    }
}
