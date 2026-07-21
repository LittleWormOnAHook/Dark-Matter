using Invector.vShooter;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Keeps Pioneer WeaponAmmoState authoritative while syncing magazine counts to Invector weapons,
    /// and drives the actual reload cycle. Invector's own native ammo/reload bookkeeping
    /// (vAmmoManager/extraAmmo/WeaponHasUnloadedAmmo) is never fed by us — weapons are kept
    /// isInfinityAmmo so that system can never gate or interfere. Instead we decide, from
    /// WeaponAmmoState alone, when a shot is allowed and when a reload should start; we still ride
    /// Invector's own reload ANIMATION/timing (vShooterManager.ReloadWeapon/AddAmmoToWeapon/
    /// onFinishReloadWeapon) since isInfinityAmmo makes its internal reserve-gating a no-op.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PioneerInvectorBootstrap))]
    [RequireComponent(typeof(WeaponAmmoState))]
    [RequireComponent(typeof(EquipmentController))]
    public class PioneerInvectorAmmoBridge : MonoBehaviour
    {
        private PioneerInvectorBootstrap _bootstrap;
        private WeaponAmmoState _ammoState;
        private EquipmentController _equipment;
        private vShooterManager _shooterManager;
        private int _lastSyncedSlot = -1;

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
            _ammoState = GetComponent<WeaponAmmoState>();
            _equipment = GetComponent<EquipmentController>();
            _shooterManager = GetComponent<vShooterManager>();
        }

        private void OnEnable()
        {
            if (_equipment != null)
                _equipment.OnSelectedHotbarChanged += HandleHotbarChanged;

            if (_shooterManager != null)
            {
                _shooterManager.onFinishReloadWeapon.AddListener(HandleReloadFinished);
                _shooterManager.onStartReloadWeapon.AddListener(HandleReloadStarted);
            }
        }

        private void OnDisable()
        {
            if (_equipment != null)
                _equipment.OnSelectedHotbarChanged -= HandleHotbarChanged;

            if (_shooterManager != null)
            {
                _shooterManager.onFinishReloadWeapon.RemoveListener(HandleReloadFinished);
                _shooterManager.onStartReloadWeapon.RemoveListener(HandleReloadStarted);
            }
        }

        private void Update()
        {
            if (!_bootstrap.IsActive || _shooterManager == null || _equipment == null)
                return;

            int slot = _equipment.ActiveWeaponHotbarSlot;
            if (slot != _lastSyncedSlot)
            {
                _lastSyncedSlot = slot;
                SyncMagazineFromPioneer();
            }
        }

        private void HandleHotbarChanged(int _)
        {
            SyncMagazineFromPioneer();
        }

        /// <summary>
        /// Single authoritative "is this trigger pull actually allowed to fire a round" decision.
        /// Called synchronously and directly by PioneerInvectorProjectileBridge.HandleShot before it
        /// decides whether to spawn a projectile — deliberately NOT wired as its own onShot listener,
        /// so there's no ordering ambiguity between the two bridges reacting to the same event.
        /// Blocks fire entirely while a reload is already in progress (pauses shooting), consumes a
        /// round from the current magazine only (no silent mid-shot refill), keeps Invector's native
        /// ammo counter in sync for animator/UI purposes, and starts Invector's own reload animation
        /// the moment the magazine is empty and more reserve of the same ammo type is available.
        /// </summary>
        public bool TryProcessShotAmmo()
        {
            if (_ammoState == null || _shooterManager == null || _equipment == null)
                return true; // Fail open rather than silently break fire if wiring is missing.

            if (_shooterManager.isReloadingWeapon)
                return false;

            bool consumed = _ammoState.TryConsumeActiveRound();
            SyncMagazineFromPioneer();

            if (!consumed)
            {
                // Tried to fire on an already-empty magazine — start a reload now if we can, rather
                // than leaving the weapon permanently dry until the player happens to try again.
                TryStartReloadIfEmpty();
                return false;
            }

            if (_ammoState.GetActiveLoadedAmmo() <= 0)
                TryStartReloadIfEmpty(); // This shot just emptied the magazine — reload immediately.

            return true;
        }

        private void TryStartReloadIfEmpty()
        {
            int slot = _equipment.ActiveWeaponHotbarSlot;
            if (_ammoState.GetActiveLoadedAmmo() > 0)
                return;

            if (_ammoState.IsInfiniteAmmoForSlot(slot))
                return;

            if (_ammoState.GetReserveAmmoCount(slot) <= 0)
                return;

            _shooterManager.ReloadWeapon();
        }

        private void HandleReloadStarted(vShooterWeapon weapon)
        {
            SuppressRecoilState();
        }

        private void HandleReloadFinished(vShooterWeapon weapon)
        {
            if (_equipment == null || _ammoState == null)
                return;

            ItemData item = _equipment.DrawnWeaponItem;
            if (item == null || !item.IsRangedWeapon)
                return;

            _ammoState.EnsureWeaponInitialized(_equipment.ActiveWeaponHotbarSlot, item);
            SyncMagazineFromPioneer();
            SuppressRecoilState();
        }

        private void SuppressRecoilState()
        {
            if (_shooterManager is PioneerShooterManager pioneerShooter)
                pioneerShooter.SuppressNativeRecoil();
            else
                PioneerInvectorRecoilUtility.SuppressInvectorNativeRecoil(_shooterManager);

            ItemData item = _equipment != null ? _equipment.DrawnWeaponItem : null;
            GameObject weaponRoot = _shooterManager != null && _shooterManager.CurrentWeapon != null
                ? _shooterManager.CurrentWeapon.gameObject
                : null;
            PioneerInvectorRecoilUtility.ApplyWeaponRecoilTuning(weaponRoot, item);
        }

        private void SyncMagazineFromPioneer()
        {
            if (_shooterManager == null || _equipment == null || _ammoState == null)
                return;

            vShooterWeapon weapon = _shooterManager.CurrentWeapon;
            if (weapon == null)
                return;

            int slot = _equipment.ActiveWeaponHotbarSlot;
            int targetAmmo = _ammoState.GetLoadedAmmo(slot);
            int delta = targetAmmo - weapon.ammoCount;
            if (delta > 0)
                weapon.AddAmmo(delta);
            else if (delta < 0)
                weapon.UseAmmo(-delta);
        }
    }
}
