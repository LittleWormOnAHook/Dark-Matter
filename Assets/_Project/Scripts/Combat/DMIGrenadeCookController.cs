using System.Reflection;
using Invector.Throw;
using Invector.vCharacterController;
using Project.Core;
using Project.Data;
using Project.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Combat
{
    /// <summary>
    /// Grenade cook: hold LT (gamepad) / RMB or Left Ctrl (KBM) while G-aiming to start a 10s fuse.
    /// Blocks weapon fire while holding/cooking a grenade. Keeps existing G / hold-G throw flow.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
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
        private DMIGrenadeExplosive _activeCookExplosive;
        private MethodInfo _disableAimMode;
        private MethodInfo _exitThrowMode;

        /// <summary>True while a grenade is armed for throw and/or cooking — blocks weapon fire.</summary>
        public bool BlocksWeaponFire => _holdingGrenade || IsCooking;

        public bool IsHoldingGrenade => _holdingGrenade;
        public bool IsCooking => _activeCookExplosive != null && _activeCookExplosive.IsFuseRunning;

        private void Awake()
        {
            ResolveRefs();
            CacheThrowManagerMethods();
        }

        private void OnEnable()
        {
            ResolveRefs();
            BindThrowEvents(true);
        }

        private void OnDisable()
        {
            BindThrowEvents(false);
            ClearCookState(cancelExplosive: true);
            _holdingGrenade = false;
            _throwCommitted = false;
        }

        private void Update()
        {
            if (!Application.isPlaying || !GameSession.HasStarted || Time.timeScale <= 0f)
                return;

            UpdateCookInput();
        }

        private void ResolveRefs()
        {
            if (throwBridge == null)
                throwBridge = GetComponent<DMIGrenadeThrowBridge>() ?? GetComponentInChildren<DMIGrenadeThrowBridge>(true);

            if (throwManager == null)
                throwManager = GetComponentInChildren<vThrowManager>(true);

            if (inventory == null)
                inventory = GetComponent<InventorySystem>() ?? GetComponentInParent<InventorySystem>();
        }

        private void CacheThrowManagerMethods()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            _disableAimMode = typeof(vThrowManagerBase).GetMethod("DisableAimMode", flags);
            _exitThrowMode = typeof(vThrowManagerBase).GetMethod("ExitThrowMode", flags);
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
            // Release-G cancel: abort cook fuse if the pin was pulled but not thrown.
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
            // Thrown instance keeps its own fuse/beep; clear player-side cook tracking.
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

        private void UpdateCookInput()
        {
            bool held = ReadCookInputHeld();
            bool pressedThisFrame = held && !_cookInputHeld;
            _cookInputHeld = held;

            if (!pressedThisFrame)
                return;

            if (!_holdingGrenade || _throwCommitted)
                return;

            TryStartCook();
        }

        private bool ReadCookInputHeld()
        {
            Gamepad pad = Gamepad.current;
            if (pad != null && pad.leftTrigger.ReadValue() >= cookTriggerThreshold)
                return true;

            // KBM cook while grenade is ready: RMB (Block/Aim parallel to LT) or Left Ctrl.
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                return true;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.leftCtrlKey.isPressed)
                return true;

            return false;
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
            // Normal throws consume on onStartThrowObject; in-hand cook detonation never reaches that.
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

            try
            {
                _disableAimMode?.Invoke(throwManager, null);
                _exitThrowMode?.Invoke(throwManager, null);
            }
            catch (System.Exception)
            {
                // Fallback: unlock locomotion if reflection fails mid-domain-reload.
                vThirdPersonInput tpInput = GetComponentInParent<vThirdPersonInput>() ?? GetComponent<vThirdPersonInput>();
                if (tpInput != null)
                {
                    tpInput.SetLockAllInput(false);
                    tpInput.SetStrafeLocomotion(false);
                    if (tpInput.animator != null)
                        tpInput.animator.SetInteger("ActionState", 0);
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
    }
}
