using System;
using System.Reflection;
using Invector.Throw;
using Invector.vCharacterController;
using Project.Building;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player.Invector;
using Project.Progression;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Combat
{
    /// <summary>
    /// Dark Matter grenade controls (when <see cref="DM_CombatCoreProfile.enableGrenades"/> / bridge switch is on):
    /// KBM — Hot Cross G hold (~1s, DM_CombatCoreProfile.hotCrossGrenadeLoadHoldSeconds) loads grenade from focused BR stack; holsters drawn weapons first.
    /// Hold LMB cooks; release LMB throws (cooked or not). Tap G cycles BR focus (GameplayKeyboardShortcuts).
    /// Gamepad — LB hold loads; hold LT cooks; release RB throws.
    /// Replaces Invector hold-G aim and RMB/Ctrl cook by unsubscribing <see cref="vThrowManagerBase.UpdateThrowInput"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(500)]
    public class DMIGrenadeCookController : MonoBehaviour
    {
        private const float CookTriggerThreshold = 0.35f;
        /// <summary>LMB/LT must be held this long before throw transfers cook fuse (tap = land fuse).</summary>
        private const float MinCookHoldBeforeThrowTransferSeconds = 0.12f;

        [Header("Refs")]
        [SerializeField] private vThrowManager throwManager;
        [SerializeField] private DMIGrenadeThrowBridge throwBridge;
        [SerializeField] private InventorySystem inventory;

        [Header("Cook")]
        [SerializeField] private float cookTriggerThreshold = CookTriggerThreshold;

        private static DMIGrenadeCookController _playerCookInputOwner;

        private bool _holdingGrenade;
        private bool _throwCommitted;
        private bool _cookInputHeld;
        private bool _throwInputHooked;
        private bool _lastGrenadesEnabled;
        private DMIGrenadeExplosive _activeCookExplosive;

        private vThirdPersonInput _tpInput;
        private vThirdPersonInput.OnUpdateEvent _defaultThrowInputUpdate;

        private MethodInfo _disableAimMode;
        private MethodInfo _exitThrowMode;
        private MethodInfo _enterThrowMode;
        private MethodInfo _getThrowConditions;
        private MethodInfo _pressThrowInputSetter;
        private PropertyInfo _isAimingProp;
        private PropertyInfo _inEnterThrowModeProp;
        private PropertyInfo _isThrowingProp;

        /// <summary>True while a grenade is armed for throw and/or cooking — blocks weapon fire.</summary>
        public bool BlocksWeaponFire => IsGrenadeInputBlocking;

        /// <summary>True from grenade equip through throw finish or cook-out cleanup — blocks weapon draw/fire.</summary>
        public bool IsGrenadeInputBlocking => InThrowFlow || IsCooking || _holdingGrenade;

        /// <summary>Live player grenade flow (Hot Cross weapon arm must defer).</summary>
        public static bool PlayerBlocksWeaponDraw =>
            _playerCookInputOwner != null && _playerCookInputOwner.IsGrenadeInputBlocking;

        public bool IsHoldingGrenade => _holdingGrenade;
        public bool IsCooking => _activeCookExplosive != null && _activeCookExplosive.IsFuseRunning;

        private bool InThrowFlow =>
            IsAiming || InEnterThrowMode || IsThrowingAnim;

        private bool IsAiming => ReadBoolProperty(_isAimingProp);
        private bool InEnterThrowMode => ReadBoolProperty(_inEnterThrowModeProp);
        private bool IsThrowingAnim => ReadBoolProperty(_isThrowingProp);

        private void Awake()
        {
            ResolveRefs();
            CacheThrowManagerMethods();
        }

        private void OnEnable()
        {
            ResolveRefs();
            BindThrowEvents(true);
            _lastGrenadesEnabled = DMIGrenadeThrowBridge.GrenadesEnabled;
            TryHookThrowInput();
            RegisterPlayerInputOwner(true);
        }

        private void OnDisable()
        {
            BindThrowEvents(false);
            UnhookThrowInput(restoreDefault: true);
            ClearCookState(cancelExplosive: true);
            _holdingGrenade = false;
            _throwCommitted = false;
            DMUiToolkitHotCross.SetGrenadeQuadrantArmed(false);
            RegisterPlayerInputOwner(false);
        }

        /// <summary>G hold load: companion follow defers while arming (see PioneerExpeditionCommandInput).</summary>
        public static bool CompanionCommandsShouldDeferGKey =>
            GameplayKeyboardShortcuts.HotCrossGrenadeLoadHoldActive
            || (_playerCookInputOwner != null && _playerCookInputOwner.ShouldDeferCompanionGKey());

        private void RegisterPlayerInputOwner(bool enable)
        {
            if (GetComponentInParent<PioneerShooterMeleeInput>(true) == null)
                return;

            if (enable)
                _playerCookInputOwner = this;
            else if (_playerCookInputOwner == this)
                _playerCookInputOwner = null;
        }

        private bool ShouldDeferCompanionGKey()
        {
            if (!DMIGrenadeThrowBridge.GrenadesEnabled)
                return false;

            return InThrowFlow || _holdingGrenade || IsCooking;
        }

        private void Update()
        {
            if (!Application.isPlaying || !GameSession.HasStarted || Time.timeScale <= 0f)
                return;

            bool enabledNow = DMIGrenadeThrowBridge.GrenadesEnabled;
            if (enabledNow != _lastGrenadesEnabled)
            {
                _lastGrenadesEnabled = enabledNow;
                if (enabledNow)
                    TryHookThrowInput();
                else
                {
                    UnhookThrowInput(restoreDefault: true);
                    ClearCookState(cancelExplosive: true);
                    _holdingGrenade = false;
                }
            }

            if (!enabledNow)
                return;

            TryHookThrowInput();
            if (_throwInputHooked && _defaultThrowInputUpdate != null && _tpInput != null)
                _tpInput.onUpdate -= _defaultThrowInputUpdate;

            if (!_throwInputHooked)
                return;

            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
                return;

            if (DMBuildingMode.BlocksWeaponFireInput)
            {
                if (InThrowFlow || _holdingGrenade || IsCooking)
                    ForceExitThrowMode();
                return;
            }

            UpdateCookAndThrowInput();
        }

        /// <summary>Called from Hot Cross G / LB hold — not from tap cycle.</summary>
        public void RequestArmFromHotCross()
        {
            if (!DMIGrenadeThrowBridge.GrenadesEnabled)
                return;

            if (!_throwInputHooked)
            {
                TryHookThrowInput();
                if (!_throwInputHooked)
                {
                    PickupToastUI.Show("Grenade throw input is not ready — holster weapons and try again.");
                    return;
                }
            }

            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
                return;

            if (IsThrowingAnim)
                return;

            if (InThrowFlow)
            {
                if (!_cookInputHeld)
                    ForceExitThrowMode();
                return;
            }

            throwBridge?.SyncThrowAmountFromHotCrossFocus();

            if (!CanEnterThrow())
            {
                MaybeToastMissingHotbarGrenade();
                MaybeToastBlockedThrowConditions();
                return;
            }

            HolsterDrawnWeapon();

            try
            {
                _enterThrowMode?.Invoke(throwManager, null);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void ResolveRefs()
        {
            if (throwBridge == null)
                throwBridge = GetComponent<DMIGrenadeThrowBridge>() ?? GetComponentInChildren<DMIGrenadeThrowBridge>(true);

            if (throwManager == null)
                throwManager = GetComponentInChildren<vThrowManager>(true);

            if (inventory == null)
                inventory = GetComponent<InventorySystem>() ?? GetComponentInParent<InventorySystem>();

            if (throwManager != null && _tpInput == null)
                _tpInput = throwManager.GetComponentInParent<vThirdPersonInput>();
        }

        private void CacheThrowManagerMethods()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type baseType = typeof(vThrowManagerBase);
            _disableAimMode = baseType.GetMethod("DisableAimMode", flags);
            _exitThrowMode = baseType.GetMethod("ExitThrowMode", flags);
            _enterThrowMode = baseType.GetMethod("EnterThrowMode", flags);
            _getThrowConditions = baseType.GetMethod("get_ThrowConditions", flags);
            PropertyInfo pressProp = baseType.GetProperty("pressThrowInput", flags);
            if (pressProp != null)
                _pressThrowInputSetter = pressProp.GetSetMethod(true);

            _isAimingProp = baseType.GetProperty("isAiming", flags);
            _inEnterThrowModeProp = baseType.GetProperty("inEnterThrowMode", flags);
            _isThrowingProp = baseType.GetProperty("isThrowing", flags);
        }

        private void TryHookThrowInput()
        {
            if (_throwInputHooked || throwManager == null || !DMIGrenadeThrowBridge.GrenadesEnabled)
                return;

            ResolveRefs();
            if (_tpInput == null)
                return;

            MethodInfo updateMi = typeof(vThrowManagerBase).GetMethod("UpdateThrowInput",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (updateMi == null)
                return;

            try
            {
                _defaultThrowInputUpdate = (vThirdPersonInput.OnUpdateEvent)Delegate.CreateDelegate(
                    typeof(vThirdPersonInput.OnUpdateEvent), throwManager, updateMi);
            }
            catch (Exception)
            {
                return;
            }

            _tpInput.onUpdate -= _defaultThrowInputUpdate;
            throwManager.aimHoldingButton = false;
            _throwInputHooked = true;
        }

        private void UnhookThrowInput(bool restoreDefault)
        {
            if (!_throwInputHooked || _tpInput == null || _defaultThrowInputUpdate == null)
            {
                _throwInputHooked = false;
                return;
            }

            if (restoreDefault)
                _tpInput.onUpdate += _defaultThrowInputUpdate;

            _throwInputHooked = false;
        }

        private void BindThrowEvents(bool bind)
        {
            if (throwManager == null)
                return;

            if (bind)
            {
                throwManager.onEquipThrowable.AddListener(HandleGrenadeEquipped);
                throwManager.onEnableAim.AddListener(HandleGrenadeAimEnabled);
                throwManager.onCancelAim.AddListener(HandleGrenadeAimCancelled);
                throwManager.onStartThrowObject.AddListener(HandleThrowCommitted);
                throwManager.onThrowObject.AddListener(HandleThrown);
                throwManager.onFinishThrow.AddListener(HandleThrowFinished);
            }
            else
            {
                throwManager.onEquipThrowable.RemoveListener(HandleGrenadeEquipped);
                throwManager.onEnableAim.RemoveListener(HandleGrenadeAimEnabled);
                throwManager.onCancelAim.RemoveListener(HandleGrenadeAimCancelled);
                throwManager.onStartThrowObject.RemoveListener(HandleThrowCommitted);
                throwManager.onThrowObject.RemoveListener(HandleThrown);
                throwManager.onFinishThrow.RemoveListener(HandleThrowFinished);
            }
        }

        private void HandleGrenadeEquipped()
        {
            _holdingGrenade = true;
            _throwCommitted = false;
            EnsureHandThrowableVisible();
            DMUiToolkitHotCross.SetGrenadeQuadrantArmed(true);
        }

        private void HandleGrenadeAimEnabled()
        {
            _holdingGrenade = true;
            _throwCommitted = false;
            EnsureHandThrowableVisible();
            DMUiToolkitHotCross.SetGrenadeQuadrantArmed(true);
        }

        private void HandleGrenadeAimCancelled()
        {
            if (!_throwCommitted)
                ClearCookState(cancelExplosive: true);

            _holdingGrenade = false;
            throwBridge?.CleanupInactiveHandGrenade();
            DMUiToolkitHotCross.SetGrenadeQuadrantArmed(false);
        }

        private void HandleThrowCommitted()
        {
            _throwCommitted = true;
            if (_activeCookExplosive == null)
                return;

            if (_activeCookExplosive.IsFuseRunning)
            {
                float elapsed = _activeCookExplosive.FuseDuration - _activeCookExplosive.FuseRemaining;
                if (elapsed >= MinCookHoldBeforeThrowTransferSeconds)
                    DMIGrenadeExplosive.StageThrownCookFuse(_activeCookExplosive.FuseRemaining);
                else
                    _activeCookExplosive.CancelCook();
            }

            _activeCookExplosive.NotifyThrown();
        }

        private void HandleThrown()
        {
            if (_activeCookExplosive != null)
            {
                _activeCookExplosive.CookFuseExpired -= HandleCookFuseExpired;
                // Invector reuses ObjectToThrow as the world projectile — do not CancelCook here
                // or land/cook fuses are cleared on the live grenade (no explosion + scene clutter).
                _activeCookExplosive = null;
            }

            _holdingGrenade = false;
            _throwCommitted = false;
            DMUiToolkitHotCross.SetGrenadeQuadrantArmed(false);
        }

        private void HandleThrowFinished()
        {
            if (!_throwCommitted && _activeCookExplosive != null && _activeCookExplosive.IsFuseRunning)
                ClearCookState(cancelExplosive: true);

            _holdingGrenade = false;
            _throwCommitted = false;
            DMUiToolkitHotCross.SetGrenadeQuadrantArmed(false);
        }

        private void UpdateCookAndThrowInput()
        {
            if (!InThrowFlow && !_holdingGrenade)
                return;

            bool cookHeld = ReadCookHeld();
            bool cookPressed = cookHeld && !_cookInputHeld;
            bool cookReleased = !cookHeld && _cookInputHeld;
            _cookInputHeld = cookHeld;

            if (InThrowFlow || _holdingGrenade)
                EnsureHandThrowableVisible();

            if (cookPressed && !_throwCommitted && !IsThrowingAnim)
                TryStartCook();

            if (cookReleased && !_throwCommitted && !IsThrowingAnim)
                TryTriggerThrow();
        }

        private bool ReadCookHeld()
        {
            if (DMBuildingMode.BlocksWeaponFireInput)
                return false;

            Gamepad pad = Gamepad.current;
            if (pad != null && pad.leftTrigger.ReadValue() >= cookTriggerThreshold)
                return true;

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
                return true;

            return false;
        }

        private bool CanEnterThrow()
        {
            if (throwManager == null)
                return false;

            try
            {
                if (_getThrowConditions != null && _getThrowConditions.Invoke(throwManager, null) is bool ok)
                    return ok;
            }
            catch (Exception)
            {
                return false;
            }

            return throwManager.CurrentThrowAmount > 0;
        }

        private void TryTriggerThrow()
        {
            if (!IsAiming && !InEnterThrowMode && !_holdingGrenade)
                return;

            SetPressThrowInput(true);
        }

        private void SetPressThrowInput(bool value)
        {
            if (throwManager == null)
                return;

            try
            {
                _pressThrowInputSetter?.Invoke(throwManager, new object[] { value });
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void EnsureHandThrowableVisible()
        {
            if (throwManager == null)
                return;

            vThrowableObject throwable = throwManager.ObjectToThrow;
            if (throwable == null)
                return;

            GameObject handObject = throwable.gameObject;
            if (!handObject.activeSelf)
                handObject.SetActive(true);
        }

        private void TryStartCook()
        {
            if (IsCooking)
                return;

            EnsureHandThrowableVisible();

            vThrowableObject throwable = throwManager != null ? throwManager.ObjectToThrow : null;
            if (throwable == null)
                return;

            DMIGrenadeExplosive explosive = throwable.GetComponent<DMIGrenadeExplosive>();
            if (explosive == null)
                explosive = throwable.gameObject.AddComponent<DMIGrenadeExplosive>();

            if (!explosive.BeginCook(ResolveMaxCookSeconds(), gameObject))
                return;

            _activeCookExplosive = explosive;
            _activeCookExplosive.CookFuseExpired += HandleCookFuseExpired;
        }

        /// <summary>Called from <see cref="DMIGrenadeExplosive"/> immediately before cook-out detonation (not after).</summary>
        public static bool TryPrepareCookOutDetonation(DMIGrenadeExplosive explosive)
        {
            if (explosive == null || _playerCookInputOwner == null)
                return false;

            return _playerCookInputOwner.PrepareCookOutDetonationIfInHand(explosive);
        }

        private bool PrepareCookOutDetonationIfInHand(DMIGrenadeExplosive explosive)
        {
            if (explosive == null)
                return false;

            bool inHand = !_throwCommitted && _holdingGrenade &&
                          (throwManager == null ||
                           throwManager.ObjectToThrow == explosive.GetComponent<vThrowableObject>());

            if (inHand)
                explosive.PrepareDetonationDamagesPlayer(true);

            return inHand;
        }

        private void HandleCookFuseExpired(DMIGrenadeExplosive explosive)
        {
            if (explosive == null)
                return;

            explosive.CookFuseExpired -= HandleCookFuseExpired;

            bool inHand = !_throwCommitted && _holdingGrenade &&
                          (throwManager == null ||
                           throwManager.ObjectToThrow == explosive.GetComponent<vThrowableObject>());

            if (inHand)
                HandleInHandDetonation(explosive);
            else if (_activeCookExplosive == explosive)
                _activeCookExplosive = null;

            _holdingGrenade = false;
        }

        private void HandleInHandDetonation(DMIGrenadeExplosive explosive)
        {
            ConsumeGrenadeForInHandDetonation();
            ForceExitThrowMode(cancelActiveCook: false);

            if (throwManager != null && throwManager.CurrentThrowable != null)
                throwManager.CurrentThrowable.ResetThrowable();

            if (_activeCookExplosive == explosive)
                _activeCookExplosive = null;

            _throwCommitted = false;
            _holdingGrenade = false;
            DMUiToolkitHotCross.SetGrenadeQuadrantArmed(false);
        }

        private void ConsumeGrenadeForInHandDetonation()
        {
            ItemData item = throwBridge != null ? throwBridge.GrenadeItem : null;
            if (item == null)
                item = ItemRegistry.Resolve("Frag Grenade") ?? ItemRegistry.Resolve("DM_Frag_Grenade");

            if (inventory != null && item != null
                && DMIGrenadeHotbarUtility.TryConsumeOneFromHotbar(
                    inventory, item, DMUiToolkitHotCross.GrenadeLocalIndex))
                item.TryGrantConfiguredXp();

            if (throwManager != null && throwManager.CurrentThrowable != null)
            {
                vThrowManager.Throwable entry = throwManager.CurrentThrowable;
                entry.amount = Mathf.Max(0, entry.amount - 1);
            }
        }

        private void ForceExitThrowMode(bool cancelActiveCook = true)
        {
            if (throwManager == null)
                return;

            if (cancelActiveCook)
                ClearCookState(cancelExplosive: true);

            throwBridge?.CleanupInactiveHandGrenade();

            try
            {
                _disableAimMode?.Invoke(throwManager, null);
                _exitThrowMode?.Invoke(throwManager, null);
            }
            catch (Exception)
            {
                if (_tpInput != null)
                {
                    _tpInput.SetLockAllInput(false);
                    _tpInput.SetStrafeLocomotion(false);
                    if (_tpInput.animator != null)
                        _tpInput.animator.SetInteger("ActionState", 0);
                }
            }
        }

        private void ClearCookState(bool cancelExplosive)
        {
            if (_activeCookExplosive == null)
                return;

            DMIGrenadeExplosive explosive = _activeCookExplosive;
            explosive.CookFuseExpired -= HandleCookFuseExpired;
            _activeCookExplosive = null;

            if (cancelExplosive)
                explosive.CancelCook();
        }

        private static float ResolveMaxCookSeconds() => PlayerSkillAllocator.GetGrenadeMaxCookSeconds();

        private bool HasGrenadeOnHotbar(ItemData item)
        {
            if (inventory == null || item == null)
                return false;

            return DMIGrenadeHotbarUtility.TryGetHotbarStackAtLocal(
                       inventory,
                       DMUiToolkitHotCross.GrenadeLocalIndex,
                       item,
                       out int amount,
                       out _)
                   && amount > 0;
        }

        private void HolsterDrawnWeapon()
        {
            if (inventory == null)
                return;

            EquipmentController equipment = inventory.GetComponent<EquipmentController>()
                ?? inventory.GetComponentInChildren<EquipmentController>(true);
            if (equipment == null || !equipment.IsWeaponDrawn)
                return;

            equipment.HolsterWeapon();
        }

        private void MaybeToastMissingHotbarGrenade()
        {
            if (throwBridge == null || inventory == null)
                return;

            ItemData item = throwBridge.GrenadeItem;
            if (item == null)
                return;

            if (!LevelUnlockUtility.PassesUseGate(item, showToast: false))
                return;

            if (inventory.CountItem(item) <= 0)
                return;

            if (HasGrenadeOnHotbar(item))
                return;

            UIManager ui = FindAnyObjectByType<UIManager>();
            ui?.ShowTimedInteractionPrompt("Place frag grenades on the hotbar (lower-right Hot Cross slot).");
        }

        private void MaybeToastBlockedThrowConditions()
        {
            if (throwBridge == null || inventory == null || throwManager == null)
                return;

            ItemData item = throwBridge.GrenadeItem;
            if (item == null || !HasGrenadeOnHotbar(item))
                return;

            if (DMBuildingMode.BlocksWeaponFireInput)
            {
                PickupToastUI.Show("Exit build mode to arm a grenade.");
                return;
            }

            if (_tpInput != null && !_tpInput.enabled)
            {
                PickupToastUI.Show("Cannot arm grenade while movement is locked.");
                return;
            }

            if (_tpInput != null && _tpInput.cc != null && _tpInput.cc.customAction)
            {
                PickupToastUI.Show("Finish the current action before arming a grenade.");
            }
        }

        private bool ReadBoolProperty(PropertyInfo prop)
        {
            if (prop == null || throwManager == null)
                return false;

            try
            {
                return prop.GetValue(throwManager) is bool b && b;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
