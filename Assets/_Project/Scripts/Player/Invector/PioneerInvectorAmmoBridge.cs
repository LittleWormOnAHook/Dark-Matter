using System.Collections;
using Invector.vShooter;
using Project.CameraFx;
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
        private const string ShellDropClipPath = "Assets/Audio/Player Weapons/shellDrop1.wav";
        private const string SharedEmptyClickClipPath = "Assets/Invector-3rdPersonController/Shooter/Audio/Weapons/EmptyClip_A.mp3";

        [Header("Empty Reload Deny")]
        [SerializeField] private AudioClip emptyReloadDenyClip;
        [SerializeField, Range(0.05f, 1f)] private float emptyReloadDenyVolume = 0.85f;
        [SerializeField, Range(0.05f, 1f)] private float emptyReloadHeadShakeTrauma = 0.28f;
        [SerializeField] private float emptyReloadDenyCooldown = 0.55f;

        [Header("Empty Fire Click")]
        [Tooltip("Shared dry-fire / empty-mag click used by all player ranged weapons (matches pistol EmptyClip_A).")]
        [SerializeField] private AudioClip sharedEmptyClickClip;
        [SerializeField, Range(0.05f, 1f)] private float emptyClickVolume = 0.9f;
        [SerializeField] private float emptyClickCooldown = 0.18f;

        private PioneerInvectorBootstrap _bootstrap;
        private WeaponAmmoState _ammoState;
        private EquipmentController _equipment;
        private vShooterManager _shooterManager;
        private int _lastSyncedSlot = -1;
        private float _nextEmptyReloadDenyTime;
        private float _nextEmptyClickTime;
        private Coroutine _headShakeRoutine;

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
            _ammoState = GetComponent<WeaponAmmoState>();
            _equipment = GetComponent<EquipmentController>();
            _shooterManager = GetComponent<vShooterManager>();
            EnsureEmptyReloadDenyClip();
            EnsureSharedEmptyClickClip();
        }

        private void OnEnable()
        {
            if (_equipment != null)
                _equipment.OnSelectedHotbarChanged += HandleHotbarChanged;

            if (_ammoState != null)
                _ammoState.OnAmmoChanged += HandleAmmoChanged;

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

            if (_ammoState != null)
                _ammoState.OnAmmoChanged -= HandleAmmoChanged;

            if (_shooterManager != null)
            {
                _shooterManager.onFinishReloadWeapon.RemoveListener(HandleReloadFinished);
                _shooterManager.onStartReloadWeapon.RemoveListener(HandleReloadStarted);
            }

            if (_headShakeRoutine != null)
            {
                StopCoroutine(_headShakeRoutine);
                _headShakeRoutine = null;
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
        /// Silent magazine sync after CreditAmmoPickup / pioneer ammo changes.
        /// Uses vShooterWeapon.AddAmmo only — never ReloadWeapon — so world ammo top-ups
        /// do not play reload SFX.
        /// </summary>
        private void HandleAmmoChanged()
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

            ItemData equipped = _equipment.EquippedItem;
            if (equipped != null && equipped.isMiningTool)
                return true; // Mining charge is drained by DMIMiningController, not shot events.

            if (_shooterManager.isReloadingWeapon)
                return false;

            bool consumed = _ammoState.TryConsumeActiveRound();
            SyncMagazineFromPioneer();

            if (!consumed)
            {
                // isInfinityAmmo keeps Invector from playing its native emptyClip — play the shared
                // pistol empty-click here so rifle/pistol dry-fire sound the same.
                PlayEmptyFireClick();
                TryStartReloadIfEmpty();
                return false;
            }

            if (_ammoState.GetActiveLoadedAmmo() <= 0)
                TryStartReloadIfEmpty(); // This shot just emptied the magazine — reload immediately.

            return true;
        }

        /// <summary>
        /// Manual R / auto-reload entry. Returns true if Invector ReloadWeapon should run.
        /// When there is no reserve ammo, plays shellDrop deny SFX + a single head-shake and skips
        /// the reload animation/audio entirely (Invector would otherwise reload because isInfinityAmmo).
        /// </summary>
        public bool TryRequestReload(bool playEmptyDenyFeedback)
        {
            if (_ammoState == null || _shooterManager == null || _equipment == null)
                return false;

            if (_shooterManager.isReloadingWeapon)
                return false;

            ItemData weapon = _equipment.DrawnWeaponItem;
            if (weapon == null || !weapon.IsRangedWeapon)
                return false;

            int slot = _equipment.ActiveWeaponHotbarSlot;
            int loaded = _ammoState.GetLoadedAmmo(slot);
            int magSize = WeaponAmmoState.GetMagazineCapacity(weapon);
            if (loaded >= magSize)
                return false;

            if (weapon.isMiningTool)
            {
                if (_ammoState.CountPlasmaFuelInInventory() > 0)
                    return true;

                if (playEmptyDenyFeedback)
                    PlayEmptyReloadDeny();

                return false;
            }

            if (_ammoState.GetReserveAmmoCount(slot) > 0)
                return true;

            if (playEmptyDenyFeedback)
                PlayEmptyReloadDeny();

            return false;
        }

        private void TryStartReloadIfEmpty()
        {
            if (!TryRequestReload(playEmptyDenyFeedback: false))
                return;

            _shooterManager.ReloadWeapon();
        }

        private void PlayEmptyReloadDeny()
        {
            if (Time.unscaledTime < _nextEmptyReloadDenyTime)
                return;

            _nextEmptyReloadDenyTime = Time.unscaledTime + Mathf.Max(0.15f, emptyReloadDenyCooldown);
            EnsureEmptyReloadDenyClip();

            if (emptyReloadDenyClip != null)
                AudioSource.PlayClipAtPoint(emptyReloadDenyClip, transform.position, emptyReloadDenyVolume);

            if (_headShakeRoutine != null)
                StopCoroutine(_headShakeRoutine);
            _headShakeRoutine = StartCoroutine(HeadShakeNoRoutine());
        }

        private IEnumerator HeadShakeNoRoutine()
        {
            // Quick left-right-left "no" using directional trauma (one deny attempt).
            CameraShake.ShakeDirectional(transform.right, emptyReloadHeadShakeTrauma);
            yield return new WaitForSecondsRealtime(0.06f);
            CameraShake.ShakeDirectional(-transform.right, emptyReloadHeadShakeTrauma * 0.9f);
            yield return new WaitForSecondsRealtime(0.06f);
            CameraShake.ShakeDirectional(transform.right, emptyReloadHeadShakeTrauma * 0.55f);
            _headShakeRoutine = null;
        }

        public void PlayDryFireClick()
        {
            PlayEmptyFireClick();
        }

        private void PlayEmptyFireClick()
        {
            if (Time.unscaledTime < _nextEmptyClickTime)
                return;

            _nextEmptyClickTime = Time.unscaledTime + Mathf.Max(0.05f, emptyClickCooldown);
            EnsureSharedEmptyClickClip();

            vShooterWeapon weapon = _shooterManager != null ? _shooterManager.CurrentWeapon : null;
            if (weapon != null)
            {
                // Keep every player gun on the same empty-click asset as the pistol.
                if (sharedEmptyClickClip != null)
                    weapon.emptyClip = sharedEmptyClickClip;

                if (weapon.source != null &&
                    weapon.gameObject.activeInHierarchy &&
                    !weapon.source.enabled)
                {
                    weapon.source.enabled = true;
                }

                if (weapon.emptyClip != null && weapon.source != null && weapon.source.enabled)
                {
                    weapon.source.PlayOneShot(weapon.emptyClip, emptyClickVolume);
                    return;
                }
            }

            if (sharedEmptyClickClip != null)
                AudioSource.PlayClipAtPoint(sharedEmptyClickClip, transform.position, emptyClickVolume);
        }

        private void EnsureSharedEmptyClickClip()
        {
            if (sharedEmptyClickClip != null)
                return;

#if UNITY_EDITOR
            sharedEmptyClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(SharedEmptyClickClipPath);
#endif
        }

        private void EnsureEmptyReloadDenyClip()
        {
            if (emptyReloadDenyClip != null)
                return;

#if UNITY_EDITOR
            emptyReloadDenyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(ShellDropClipPath);
#endif
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

            int slot = _equipment.ActiveWeaponHotbarSlot;
            if (item.isMiningTool)
                _ammoState.TryReloadMiningWithPlasmaFuel(slot);
            else
                _ammoState.EnsureWeaponInitialized(slot, item);

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
