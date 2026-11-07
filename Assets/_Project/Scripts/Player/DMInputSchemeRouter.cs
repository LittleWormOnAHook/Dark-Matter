using global::Invector.vCharacterController;
using Project.Core;
using Project.Features.Locomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using InvInputDevice = global::Invector.vCharacterController.InputDevice;
using UnityInputDevice = UnityEngine.InputSystem.InputDevice;

namespace Project.Player
{
    /// <summary>
    /// Tracks Keyboard&amp;Mouse vs Gamepad and applies Invector GenericInput mute/unmute.
    /// Promotes scheme on deliberate device activity (not stick drift / mouse micro-delta).
    /// stamp: controller-scheme-router 1105a
    /// </summary>
    [DefaultExecutionOrder(-270)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public class DMInputSchemeRouter : MonoBehaviour
    {
        public const string GamepadScheme = "Gamepad";
        public const string KeyboardMouseScheme = "Keyboard&Mouse";
        public const string Stamp = "controller-scheme-router 1105a";

        private static DMInputSchemeRouter instance;
        private static bool deviceChangeHooked;

        private PlayerInput playerInput;
        private vThirdPersonInput shooterInput;
        private bool gamepadSchemeActive;
        private float nextSchemeSwitchTime;
        private Coroutine initialSchemeRoutine;

        public static DMInputSchemeRouter Instance => instance;

        public static bool IsGamepadScheme =>
            instance != null && instance.gamepadSchemeActive;

        public static bool IsKeyboardMouseScheme => !IsGamepadScheme;

        /// <summary>Instance mirror of <see cref="IsGamepadScheme"/> for Controls / UI hosts.</summary>
        public bool IsGamepad => gamepadSchemeActive;

        /// <summary>After Mars/IO switch or player reload, refresh scheme on the live rig.</summary>
        public static void RefreshLiveInstance()
        {
            PlayerInput live = PlayerLocator.FindLivePlayerInput();
            if (live == null)
                return;

            DMInputSchemeRouter router = live.GetComponent<DMInputSchemeRouter>();
            router?.RequestSchemeRefresh("live-player");
        }

        public void RequestSchemeRefresh(string reason)
        {
            if (!isActiveAndEnabled)
                return;

            EvaluateInitialControlScheme(reason);
            RefreshScheme(force: true);
        }

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            shooterInput = GetComponent<vThirdPersonInput>();
        }

        private void OnEnable()
        {
            instance = this;
            HookDeviceChange();
            if (playerInput != null)
                playerInput.onControlsChanged += OnControlsChanged;

            if (initialSchemeRoutine != null)
                StopCoroutine(initialSchemeRoutine);

            initialSchemeRoutine = StartCoroutine(CoEvaluateInitialControlScheme("OnEnable"));
            RefreshScheme(force: true);
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.onControlsChanged -= OnControlsChanged;

            if (initialSchemeRoutine != null)
            {
                StopCoroutine(initialSchemeRoutine);
                initialSchemeRoutine = null;
            }

            if (instance == this)
                instance = null;

            if (instance == null)
                ApplySchemeState(false, force: true);
        }

        private void Update()
        {
            if (!Application.isPlaying || playerInput == null || !playerInput.isActiveAndEnabled)
                return;

            PromoteGamepadOnLocalActivity();
            PromoteKeyboardMouseOnLocalActivity();
            RefreshScheme(force: false);
        }

        private void OnControlsChanged(PlayerInput input)
        {
            RefreshScheme(force: true);
        }

        private System.Collections.IEnumerator CoEvaluateInitialControlScheme(string reason)
        {
            yield return null;
            initialSchemeRoutine = null;
            EvaluateInitialControlScheme(reason);
            RefreshScheme(force: true);
        }

        private void EvaluateInitialControlScheme(string reason)
        {
            if (playerInput == null || playerInput.currentControlScheme == GamepadScheme)
                return;

            Gamepad pad = ResolvePreferredGamepad();
            if (pad == null)
                return;

            if (DetectDeliberateGamepad(pad))
            {
                TrySwitchScheme(GamepadScheme, pad, reason + ": deliberate-pad");
                return;
            }

            if (!DetectDeliberateKeyboardMouse() && !KeyboardMouseRecentlyActive())
                TrySwitchScheme(GamepadScheme, pad, reason + ": pad-present-idle-kbm");
        }

        private static bool KeyboardMouseRecentlyActive()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame
                    || mouse.delta.ReadValue().sqrMagnitude > 4f)
                    return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.isPressed;
        }

        private void PromoteKeyboardMouseOnLocalActivity()
        {
            if (playerInput == null || playerInput.currentControlScheme == KeyboardMouseScheme)
                return;

            if (!DetectDeliberateKeyboardMouse())
                return;

            if (Time.unscaledTime < nextSchemeSwitchTime)
                return;

            nextSchemeSwitchTime = Time.unscaledTime + DMInputSchemePromotionRules.SchemeSwitchCooldownSeconds;
            TrySwitchScheme(KeyboardMouseScheme, "kbm-activity", Keyboard.current, Mouse.current);
        }

        private void PromoteGamepadOnLocalActivity()
        {
            if (playerInput == null || playerInput.currentControlScheme == GamepadScheme)
                return;

            Gamepad pad = ResolvePreferredGamepad();
            if (pad == null || !DetectDeliberateGamepad(pad))
                return;

            if (Time.unscaledTime < nextSchemeSwitchTime)
                return;

            nextSchemeSwitchTime = Time.unscaledTime + DMInputSchemePromotionRules.SchemeSwitchCooldownSeconds;
            TrySwitchScheme(GamepadScheme, pad, "pad-activity");
        }

        private static bool DetectDeliberateKeyboardMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame)
                    return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }

        private static bool DetectDeliberateGamepad(Gamepad pad)
        {
            if (pad == null)
                return false;

            if (pad.buttonSouth.wasPressedThisFrame
                || pad.buttonEast.wasPressedThisFrame
                || pad.buttonWest.wasPressedThisFrame
                || pad.buttonNorth.wasPressedThisFrame
                || pad.leftShoulder.wasPressedThisFrame
                || pad.rightShoulder.wasPressedThisFrame
                || pad.leftTrigger.wasPressedThisFrame
                || pad.rightTrigger.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame
                || pad.selectButton.wasPressedThisFrame)
                return true;

            Vector2 ls = pad.leftStick.ReadValue();
            if (DMInputSchemePromotionRules.StickDeflects(ls.x, ls.y))
                return true;

            Vector2 rs = pad.rightStick.ReadValue();
            if (DMInputSchemePromotionRules.StickDeflects(rs.x, rs.y))
                return true;

            Vector2 dpad = pad.dpad.ReadValue();
            return dpad.sqrMagnitude > 0.25f;
        }

        private static Gamepad ResolvePreferredGamepad()
        {
            if (Gamepad.current != null)
                return Gamepad.current;

            foreach (UnityInputDevice device in InputSystem.devices)
            {
                if (device is Gamepad pad)
                    return pad;
            }

            return null;
        }

        private void TrySwitchScheme(string scheme, UnityInputDevice device, string reason)
        {
            if (device != null)
                TrySwitchScheme(scheme, reason, device);
        }

        private void TrySwitchScheme(string scheme, string reason, params UnityInputDevice[] devices)
        {
            if (playerInput == null || string.IsNullOrEmpty(scheme))
                return;

            try
            {
                EnsureInputUserReady();

                if (!playerInput.user.valid)
                    return;

                System.Collections.Generic.List<UnityInputDevice> list =
                    new System.Collections.Generic.List<UnityInputDevice>(4);
                if (devices != null)
                {
                    for (int i = 0; i < devices.Length; i++)
                    {
                        if (devices[i] != null && !list.Contains(devices[i]))
                            list.Add(devices[i]);
                    }
                }

                if (list.Count == 0)
                    return;

                for (int i = 0; i < list.Count; i++)
                    PairDeviceIfNeeded(list[i]);

                playerInput.SwitchCurrentControlScheme(scheme, list.ToArray());
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DMInputSchemeRouter] scheme switch skipped ({reason}): {ex.Message}");
            }
        }

        private void EnsureInputUserReady()
        {
            if (playerInput == null)
                return;

            if (!playerInput.inputIsActive && playerInput.isActiveAndEnabled)
                playerInput.ActivateInput();
        }

        private void PairDeviceIfNeeded(UnityInputDevice device)
        {
            if (playerInput == null || device == null || !playerInput.user.valid)
                return;

            for (int i = 0; i < playerInput.user.pairedDevices.Count; i++)
            {
                if (playerInput.user.pairedDevices[i] == device)
                    return;
            }

            InputUser.PerformPairingWithDevice(device, playerInput.user);
        }

        private void RefreshScheme(bool force)
        {
            if (playerInput == null)
                return;

            bool gamepad = playerInput.currentControlScheme == GamepadScheme;
            if (!force && gamepad == gamepadSchemeActive)
                return;

            ApplySchemeState(gamepad, force);
        }

        private void ApplySchemeState(bool gamepad, bool force)
        {
            if (!force && gamepad == gamepadSchemeActive)
                return;

            bool changed = gamepad != gamepadSchemeActive;
            gamepadSchemeActive = gamepad;

            if (shooterInput == null)
                shooterInput = GetComponent<vThirdPersonInput>();

            Project.Player.Invector.PioneerInvectorGenericInputGate.ApplyGamepadMute(shooterInput, gamepad);
            SyncInvectorInputDevice(gamepad);

            if (!gamepad)
            {
                DMLocomotionGaitController gait = GetComponent<DMLocomotionGaitController>();
                gait?.ClearGamepadLocomotionLatches();
            }

            if (changed && Application.isPlaying)
            {
                Debug.Log(
                    $"[DMInputSchemeRouter] {Stamp} scheme -> {(gamepad ? GamepadScheme : KeyboardMouseScheme)} "
                    + $"(GenericInput {(gamepad ? "muted" : "restored")})");
            }
        }

        private static void SyncInvectorInputDevice(bool gamepadScheme)
        {
            if (vInput.instance == null)
                return;

            vInput.instance.inputDevice = gamepadScheme
                ? InvInputDevice.Joystick
                : InvInputDevice.MouseKeyboard;
        }

        private static void HookDeviceChange()
        {
            if (deviceChangeHooked)
                return;

            InputSystem.onDeviceChange += OnGlobalDeviceChange;
            deviceChangeHooked = true;
        }

        private static void OnGlobalDeviceChange(UnityInputDevice device, InputDeviceChange change)
        {
            if (instance == null || device == null)
                return;

            if (change != InputDeviceChange.Added
                && change != InputDeviceChange.Reconnected
                && change != InputDeviceChange.Enabled)
                return;

            if (device is not Gamepad pad)
                return;

            instance.TrySwitchScheme(GamepadScheme, pad, "device-change:" + change);
            instance.RefreshScheme(force: true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (deviceChangeHooked)
            {
                InputSystem.onDeviceChange -= OnGlobalDeviceChange;
                deviceChangeHooked = false;
            }

            instance = null;
        }
    }
}
