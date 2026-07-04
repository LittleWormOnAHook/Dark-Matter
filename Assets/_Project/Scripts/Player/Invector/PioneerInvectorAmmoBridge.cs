using Invector.vShooter;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Keeps Pioneer WeaponAmmoState authoritative while syncing magazine counts to Invector weapons.
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
                _shooterManager.onShot.AddListener(HandleShot);
                _shooterManager.onFinishReloadWeapon.AddListener(HandleReloadFinished);
            }
        }

        private void OnDisable()
        {
            if (_equipment != null)
                _equipment.OnSelectedHotbarChanged -= HandleHotbarChanged;

            if (_shooterManager != null)
            {
                _shooterManager.onShot.RemoveListener(HandleShot);
                _shooterManager.onFinishReloadWeapon.RemoveListener(HandleReloadFinished);
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

        private void HandleShot(vShooterWeapon _)
        {
            if (_ammoState != null)
                _ammoState.TryConsumeActiveRound();

            SyncMagazineFromPioneer();
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
