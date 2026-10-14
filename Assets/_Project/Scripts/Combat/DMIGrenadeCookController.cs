using System;
using System.Reflection;
using Invector.Throw;
using Invector.vCharacterController;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Combat
{
    /// <summary>
    /// Dark Matter grenade controls (when <see cref="DM_CombatCoreProfile.enableGrenades"/> / bridge switch is on):
    /// KBM — G tap arms (Invector throw-ready); hold LMB cooks; release LMB throws (cooked or not).
    /// Gamepad — LB tap arms; hold LT cooks; release RB throws.
    /// Replaces Invector hold-G aim and RMB/Ctrl cook by unsubscribing <see cref="vThrowManagerBase.UpdateThrowInput"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(500)]
    public class DMIGrenadeCookController : MonoBehaviour
    {
        private const float CookTriggerThreshold = 0.35f;

        [Header("Refs")]
        [SerializeField] private vThrowManager throwManager;
        [SerializeField] private DMIGrenadeThrowBridge throwBridge;
        [SerializeField] private InventorySystem inventory;

        [Header("Cook")]
        [SerializeField] private float cookFuseSeconds = DMIGrenadeExplosive.DefaultCookFuseSeconds;
        [SerializeField] private float cookTriggerThreshold = CookTriggerThreshold;

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

        public bool IsGrenadeInputBlocking => InThrowFlow || IsCooking;

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
        }

        private void OnDisable()
        {
            BindThrowEvents(false);
            UnhookThrowInput(restoreDefault: true);
            ClearCookState(cancelExplosive: true);
            _holdingGrenade = false;
            _throwCommitted = false;
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

            UpdateArmInput();
            UpdateCookAndThrowInput();
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
        }

        private void HandleGrenadeAimEnabled()
        {
            _holdingGrenade = true;
            _throwCommitted = false;
        }

        private void HandleGrenadeAimCancelled()
        {
            if (!_throwCommitted)
                ClearCookState(cancelExplosive: true);

            _holdingGrenade = false;
        }

        private void HandleThrowCommitted()
        {
            _throwCommitted = true;
            if (_activeCookExplosive != null)
                _activeCookExplosive.NotifyThrown();
        }

        private void HandleThrown()
        {
            if (_activeCookExplosive != null)
            {
                _activeCookExplosive.CookFuseExpired -= HandleCookFuseExpired;
                _activeCookExplosive.NotifyThrown();
                _activeCookExplosive = null;
            }

            _holdingGrenade = false;
            _throwCommitted = false;
        }

        private void HandleThrowFinished()
        {
            if (!_throwCommitted && _activeCookExplosive != null && _activeCookExplosive.IsFuseRunning)
                ClearCookState(cancelExplosive: true);

            _holdingGrenade = false;
            _throwCommitted = false;
        }

        private void UpdateArmInput()
        {
            if (!ReadArmPressedThisFrame())
                return;

            if (IsThrowingAnim)
                return;

            if (InThrowFlow)
            {
                if (!_cookInputHeld)
                    ForceExitThrowMode();
                return;
            }

            if (!CanEnterThrow())
                return;

            try
            {
                _enterThrowMode?.Invoke(throwManager, null);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpdateCookAndThrowInput()
        {
            if (!InThrowFlow && !_holdingGrenade)
                return;

            bool cookHeld = ReadCookHeld();
            bool cookPressed = cookHeld && !_cookInputHeld;
            bool cookReleased = !cookHeld && _cookInputHeld;
            _cookInputHeld = cookHeld;

            if (cookPressed && !_throwCommitted && !IsThrowingAnim)
                TryStartCook();

            if (cookReleased && !_throwCommitted && !IsThrowingAnim)
                TryTriggerThrow();
        }

        private bool ReadArmPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
                return true;

            Gamepad pad = Gamepad.current;
            if (pad != null && pad.leftShoulder.wasPressedThisFrame)
                return true;

            return false;
        }

        private bool ReadCookHeld()
        {
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

        private void TryStartCook()
        {
            if (IsCooking)
                return;

            vThrowableObject throwable = throwManager != null ? throwManager.ObjectToThrow : null;
            if (throwable == null)
                return;

            DMIGrenadeExplosive explosive = throwable.GetComponent<DMIGrenadeExplosive>();
            if (explosive == null)
                explosive = throwable.gameObject.AddComponent<DMIGrenadeExplosive>();

            if (!explosive.BeginCook(cookFuseSeconds, gameObject))
                return;

            _activeCookExplosive = explosive;
            _activeCookExplosive.CookFuseExpired += HandleCookFuseExpired;
        }

        private void HandleCookFuseExpired(DMIGrenadeExplosive explosive)
        {
            if (explosive == null)
                return;

            explosive.CookFuseExpired -= HandleCookFuseExpired;

            bool inHand = !_throwCommitted && _holdingGrenade &&
                          (throwManager == null || throwManager.ObjectToThrow == explosive.GetComponent<vThrowableObject>());

            if (inHand)
                HandleInHandDetonation(explosive);
            else if (_activeCookExplosive == explosive)
                _activeCookExplosive = null;

            _holdingGrenade = false;
        }

        private void HandleInHandDetonation(DMIGrenadeExplosive explosive)
        {
            ConsumeGrenadeForInHandDetonation();
            ForceExitThrowMode();

            if (throwManager != null && throwManager.CurrentThrowable != null)
                throwManager.CurrentThrowable.ResetThrowable();

            if (_activeCookExplosive == explosive)
                _activeCookExplosive = null;

            _throwCommitted = false;
            _holdingGrenade = false;
        }

        private void ConsumeGrenadeForInHandDetonation()
        {
            ItemData item = throwBridge != null ? throwBridge.GrenadeItem : null;
            if (item == null)
                item = ItemRegistry.Resolve("Frag Grenade") ?? ItemRegistry.Resolve("DM_Frag_Grenade");

            if (inventory != null && item != null && inventory.CountItem(item) > 0)
            {
                inventory.RemoveItem(item, 1);
                item.TryGrantConfiguredXp();
            }

            if (throwManager != null && throwManager.CurrentThrowable != null)
            {
                vThrowManager.Throwable entry = throwManager.CurrentThrowable;
                entry.amount = Mathf.Max(0, entry.amount - 1);
            }
        }

        private void ForceExitThrowMode()
        {
            if (throwManager == null)
                return;

            ClearCookState(cancelExplosive: true);

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
            if (_activeCookExplosive != null)
            {
                _activeCookExplosive.CookFuseExpired -= HandleCookFuseExpired;
                if (cancelExplosive)
                    _activeCookExplosive.CancelCook();
                _activeCookExplosive = null;
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
