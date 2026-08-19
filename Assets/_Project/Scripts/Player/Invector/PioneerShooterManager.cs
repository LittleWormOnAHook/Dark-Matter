using Invector.vCamera;
using Invector.vCharacterController;
using Invector.vItemManager;
using Invector.vShooter;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Player shooter manager — disables Invector's built-in camera recoil (inverted pitch) and
    /// prevents Start() from auto-equipping stale hand-bone weapons that carry arcade recoil values.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PioneerShooterManager : vShooterManager
    {
        private EquipmentController _equipment;
        private WeaponAmmoState _ammoState;
        private bool _nativeRecoilSuppressed;

        public override void Start()
        {
            _equipment = GetComponent<EquipmentController>();
            _ammoState = GetComponent<WeaponAmmoState>();
            animator = GetComponent<Animator>();
            tpCamera = FindAnyObjectByType<vThirdPersonCamera>();
            ammoManager = GetComponent<vAmmoManager>();
            if (ammoManager != null)
                ammoManager.updateTotalAmmo = new vAmmoManager.OnUpdateTotalAmmo(AmmoManagerWasUpdated);

            var tpInput = GetComponent<vThirdPersonController>();
            usingThirdPersonController = tpInput;

            if (usingThirdPersonController && useCancelReload)
                tpInput.onReceiveDamage.AddListener(CancelReload);

            if (useAmmoDisplay)
                GetAmmoDisplays();

            if (animator)
                ShotLayer = animator.GetLayerIndex("Shot");

            // Do not auto-equip vShooterWeapon found on hand bones — PioneerInvectorWeaponBridge
            // equips the authored drawn slot and keeps recoil fields suppressed there.

            if (!ignoreTags.Contains(gameObject.tag))
                ignoreTags.Add(gameObject.tag);

            if (useAmmoDisplay)
            {
                if (ammoDisplayR)
                    ammoDisplayR.UpdateDisplay(string.Empty);
                if (ammoDisplayL)
                    ammoDisplayL.UpdateDisplay(string.Empty);
            }

            PioneerInvectorRecoilUtility.ApplyShooterManagerDefaults(this);
            SuppressNativeRecoil();
            UpdateTotalAmmo();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            // Invector can re-enable native recoil on equip — refresh only while armed or firing.
            bool needsNativeSuppress = isShooting || isReloading || CurrentWeapon != null || applyRecoilToCamera;
            if (needsNativeSuppress || !_nativeRecoilSuppressed)
            {
                PioneerInvectorRecoilUtility.ApplyShooterManagerDefaults(this);
                PioneerInvectorRecoilUtility.SuppressInvectorNativeRecoil(this);
                _nativeRecoilSuppressed = CurrentWeapon != null && !applyRecoilToCamera;
            }

            if (_equipment == null)
                _equipment = GetComponent<EquipmentController>();

            ItemData weaponItem = _equipment != null ? _equipment.DrawnWeaponItem : null;

            if (PioneerInvectorRecoilUtility.HasActiveRecoil(tpCamera) || isShooting)
                PioneerInvectorRecoilUtility.TickRecoilRecovery(tpCamera, weaponItem, Time.deltaTime);
        }

        public override void Shoot(Vector3 aimPosition, bool applyHipfirePrecision = false, bool scopeViewMode = false)
        {
            SuppressNativeRecoil();

            if (_equipment == null)
                _equipment = GetComponent<EquipmentController>();
            if (_ammoState == null)
                _ammoState = GetComponent<WeaponAmmoState>();

            // Mining shares the handgun Invector prefab. Never call Invector Shoot for mining —
            // DMIMiningController owns continuous beam audio/VFX and empty-charge clicks.
            // base.Shoot would still run ShotEffect (fireClip / anim shot pulse) at fireRate 12
            // and sounds like pistol gunfire whether plasma is empty or charged.
            ItemData drawn = _equipment != null ? _equipment.DrawnWeaponItem : null;
            if (drawn != null && drawn.isMiningTool)
            {
                vShooterWeapon weapon = CurrentWeapon;
                if (weapon != null)
                {
                    weapon.fireClip = null;
                    weapon.emittShurykenParticle = null;
                    weapon.lightOnShot = null;
                    weapon.isInfinityAmmo = true;
                }

                if (_ammoState != null && _ammoState.GetActiveLoadedAmmo() <= 0)
                    GetComponent<PioneerInvectorAmmoBridge>()?.PlayDryFireClick();

                return;
            }

            base.Shoot(aimPosition, applyHipfirePrecision, scopeViewMode);
        }

        public override void ApplyRecoil()
        {
            PioneerInvectorRecoilUtility.ZeroWeaponRecoil(CurrentWeapon);

            ItemData weaponItem = _equipment != null ? _equipment.DrawnWeaponItem : null;
            ItemData ammoItem = null;
            if (_ammoState == null)
                _ammoState = GetComponent<WeaponAmmoState>();
            if (_ammoState != null && _equipment != null)
                ammoItem = _ammoState.GetLoadedAmmoItem(_equipment.ActiveWeaponHotbarSlot);

            // Laser / low-weight ammo may still use a subtle animation pulse from ammoRecoilProfile.
            if (!PioneerInvectorRecoilUtility.ShouldSkipAnimationRecoil(weaponItem, ammoItem))
                ApplyAnimationRecoil();

            if (weaponItem != null && weaponItem.IsRangedWeapon)
                PioneerInvectorRecoilUtility.ApplyPlayerShotRecoil(this, weaponItem, ammoItem);
        }

        public override void CameraSway()
        {
            // Invector sway writes tpCamera.offsetMouse and reads weapon.cameraStability — disabled for Pioneer.
        }

        protected override void ApplyCameraRecoil()
        {
            // Pioneer recoil is applied in ApplyRecoil via PioneerInvectorRecoilUtility.
        }

        public void SuppressNativeRecoil()
        {
            PioneerInvectorRecoilUtility.SuppressInvectorNativeRecoil(this);
        }

        public override int GetShotID()
        {
            return PioneerInvectorRecoilUtility.MildShotAnimationId;
        }
    }
}
