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
    /// use, so tracers/particles/muzzle flashes/hit-fx/elemental status effects and ammo types all
    /// behave identically no matter who pulled the trigger. Invector's vShooterManager keeps
    /// owning aim, animation, recoil, and fire-rate gating (onShot fires after a successful shot);
    /// this bridge just spawns our own projectile from the same muzzle. Invector's own hit
    /// detection is prevented from double-dealing damage separately in
    /// PioneerInvectorDamageBridge.ResolveOutgoingDamage (returns 0 for ranged weapons — the
    /// spawned CombatProjectile is now the sole damage source for ranged shots).
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
        private PlayerController _player;

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
            _ammoState = GetComponent<WeaponAmmoState>();
            _equipment = GetComponent<EquipmentController>();
            _shooterManager = GetComponent<vShooterManager>();
            _ammoBridge = GetComponent<PioneerInvectorAmmoBridge>();
            _inputBridge = GetComponent<PioneerInvectorInputBridge>();
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
        }

        private void HandleShot(vShooterWeapon invectorWeapon)
        {
            if (_bootstrap != null && !_bootstrap.IsActive)
                return;

            if (invectorWeapon == null || invectorWeapon.muzzle == null || _equipment == null)
                return;

            // Invector's own vShooterWeapon.ShootBullet spawns its own physical bullet (with a
            // TrailRenderer + its own hit-damage) whenever its "projectile" field is assigned —
            // independent of and in addition to the CombatProjectile we spawn below. Since we've
            // fully unified ranged fire onto our own ammo-driven pipeline, keep it cleared so only
            // our ammo-specific tracer/visual shows and Invector's generic bullet never fires or
            // double-deals damage.
            invectorWeapon.projectile = null;

            // Invector plays its own bundled fireClip on every shot (source.PlayOneShot) on top of
            // whatever ammoItem/weapon fire sound we resolve below — clear it so only one gunshot
            // isInfinityAmmo stays true so Invector's unfed native reserve (vAmmoManager) never
            // gates shots — WeaponAmmoState is the sole authority for finite player magazines.
            // Companions/enemies also set isInfinityAmmo, but they intentionally never consume
            // Pioneer inventory ammo. dontUseReload stays false so reload animation still runs.
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

            ItemData weaponItem = _equipment.EquippedItem;
            if (weaponItem == null || !weaponItem.IsRangedWeapon)
                return;

            // Single authoritative ammo decision for this trigger pull: blocks fire while a reload
            // is already in progress (pauses shooting), consumes a round (or starts a reload and
            // reports failure if the magazine just ran dry), and keeps Invector's native counter
            // synced. No projectile spawns unless this actually consumed a real round.
            if (_ammoBridge != null && !_ammoBridge.TryProcessShotAmmo())
                return;

            ItemData ammoItem = _ammoState != null
                ? _ammoState.GetLoadedAmmoItem(_equipment.ActiveWeaponHotbarSlot)
                : null;

            Transform muzzle = invectorWeapon.muzzle;
            bool isAiming = _inputBridge != null && _inputBridge.IsAiming;
            float maxRange = ResolveMaxRange(weaponItem, ammoItem);

            Camera cam = ResolveGameplayCamera();
            float aimDistance = maxRange;
            Vector3 reticleAim = muzzle.forward;
            if (cam != null)
            {
                reticleAim = RangedFireSolver.ResolveMuzzleToReticleDirection(
                    cam,
                    muzzle.position,
                    maxRange,
                    out aimDistance);
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

        private static float ResolveMaxRange(ItemData weapon, ItemData ammo)
        {
            float range = weapon != null ? weapon.rangedRange : 45f;
            if (ammo != null && ammo.rangedRange > 0.01f)
                range = ammo.rangedRange;
            return Mathf.Max(1f, range);
        }
    }
}
